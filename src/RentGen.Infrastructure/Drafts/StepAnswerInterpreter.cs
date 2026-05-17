using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Infrastructure.Llm;

namespace RentGen.Infrastructure.Drafts;

public sealed class StepAnswerInterpreter(
    IGenerativeAiClient generativeAiClient) : IStepAnswerInterpreter
{
    private readonly IGenerativeAiClient _generativeAiClient = generativeAiClient;

    public async Task<StepAnswerInterpretationResult> InterpretAsync(
        ScenarioStep step,
        object? rawValue,
        IReadOnlyDictionary<string, JsonElement> answers,
        CancellationToken cancellationToken)
    {
        rawValue = UnwrapValue(rawValue);

        if (rawValue is null)
        {
            return new StepAnswerInterpretationResult
            {
                IsSuccess = true,
                Value = null
            };
        }

        if (rawValue is not string rawText)
        {
            return new StepAnswerInterpretationResult
            {
                IsSuccess = true,
                Value = rawValue
            };
        }

        var trimmed = rawText.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new StepAnswerInterpretationResult
            {
                IsSuccess = true,
                Value = null
            };
        }

        if (step.InputType is "text" or "textarea")
        {
            var llmTextAttempt = await TryInterpretWithLlmAsync(step, trimmed, answers, cancellationToken);
            if (llmTextAttempt.IsSuccess)
            {
                return llmTextAttempt;
            }

            return InterpretTextFallback(step, trimmed);
        }

        var heuristic = TryInterpretHeuristically(step, trimmed);
        if (heuristic.IsSuccess)
        {
            return heuristic;
        }

        var llmAttempt = await TryInterpretWithLlmAsync(step, trimmed, answers, cancellationToken);
        if (llmAttempt.IsSuccess)
        {
            return llmAttempt;
        }

        return new StepAnswerInterpretationResult
        {
            IsSuccess = false,
            ErrorMessage = llmAttempt.ErrorMessage
                ?? heuristic.ErrorMessage
                ?? $"Не удалось уверенно распознать ответ для шага «{step.Title}». Попробуйте сформулировать его чуть точнее."
        };
    }

    private static StepAnswerInterpretationResult TryInterpretHeuristically(ScenarioStep step, string rawText)
    {
        return step.InputType switch
        {
            "boolean" => InterpretBoolean(rawText),
            "number" => InterpretNumber(step, rawText),
            "date" => InterpretDate(rawText),
            "select" => InterpretSelect(step, rawText),
            _ => new StepAnswerInterpretationResult
            {
                IsSuccess = true,
                Value = rawText
            }
        };
    }

    private async Task<StepAnswerInterpretationResult> TryInterpretWithLlmAsync(
        ScenarioStep step,
        string rawText,
        IReadOnlyDictionary<string, JsonElement> answers,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _generativeAiClient.GenerateAsync(
                new AiCompletionRequest(
                    "step_answer_interpretation",
                    """
                    Ты извлекаешь одно структурированное значение из свободного ответа пользователя для анкеты договора аренды.

                    Верни только компактный JSON без markdown и пояснений:
                    {"value": ...}

                    Правила:
                    - если шаг типа select, верни один из допустимых ключей опции
                    - если шаг типа boolean, верни true или false
                    - если шаг типа date, верни дату в формате DD.MM.YYYY
                    - если шаг типа number, верни JSON-число
                    - если шаг типа text или textarea, верни нормализованную строку без markdown, звездочек, reasoning и служебных пояснений
                    - для телефона старайся вернуть формат +7XXXXXXXXXX
                    - для серии и номера документа возвращай формат 00 00 000000
                    - для кода подразделения возвращай формат 000-000
                    - для ИНН ИП возвращай только 12 цифр
                    - для ИНН организации возвращай только 10 цифр
                    - для ОГРНИП возвращай только 15 цифр
                    - для ОГРН возвращай только 13 цифр
                    - учитывай разговорные сокращения и сленг: «нал», «наличка», «безнал», «без нал», «физовик», «юрик», «ипшник»
                    - если значение извлечь нельзя, верни {"value": null}
                    """,
                    $"""
                    Ситуация:
                    Пользователь заполняет анкету для договора аренды. Нужно извлечь только одно значение из его ответа.

                    Текущий шаг:
                    - ключ: {step.Key}
                    - название: {step.Title}
                    - вопрос: {step.QuestionText}
                    - тип ответа: {step.InputType}
                    - допустимые варианты: {DraftPromptContextFormatter.FormatReadableOptions(step)}

                    Уже собранные факты:
                    {DraftPromptContextFormatter.FormatCollectedFacts(new[] { step }, answers)}

                    Ответ пользователя:
                    {rawText}
                    """,
                    0,
                    300),
                cancellationToken);

            if (response.Content.StartsWith("[LLM stub:", StringComparison.Ordinal))
            {
                return new StepAnswerInterpretationResult { IsSuccess = false };
            }

            using var document = JsonDocument.Parse(response.Content);
            if (!document.RootElement.TryGetProperty("value", out var valueNode))
            {
                return new StepAnswerInterpretationResult { IsSuccess = false };
            }

            if (valueNode.ValueKind == JsonValueKind.Null)
            {
                return new StepAnswerInterpretationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Не удалось уверенно распознать ответ для шага «{step.Title}»."
                };
            }

            object? rawInterpretedValue = valueNode.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when valueNode.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number => valueNode.GetDecimal(),
                JsonValueKind.String => valueNode.GetString(),
                _ => valueNode.ToString()
            };

            return NormalizeInterpretedValue(step, rawInterpretedValue);
        }
        catch
        {
            return new StepAnswerInterpretationResult { IsSuccess = false };
        }
    }

    private static StepAnswerInterpretationResult NormalizeInterpretedValue(ScenarioStep step, object? rawValue)
    {
        if (rawValue is null)
        {
            return new StepAnswerInterpretationResult
            {
                IsSuccess = true,
                Value = null
            };
        }

        if (rawValue is string stringValue)
        {
            var normalizedText = AiPlainTextFormatter.Normalize(stringValue);

            if (step.InputType == "select")
            {
                var selectValue = InterpretSelect(step, normalizedText);
                if (selectValue.IsSuccess)
                {
                    return selectValue;
                }
            }

            if (step.InputType == "boolean")
            {
                var booleanValue = InterpretBoolean(normalizedText);
                if (booleanValue.IsSuccess)
                {
                    return booleanValue;
                }
            }

            if (step.InputType == "date")
            {
                var dateValue = InterpretDate(normalizedText);
                if (dateValue.IsSuccess)
                {
                    return dateValue;
                }
            }

            if (step.InputType == "number")
            {
                var numberValue = InterpretNumber(step, normalizedText);
                if (numberValue.IsSuccess)
                {
                    return numberValue;
                }
            }

            if (step.InputType is "text" or "textarea")
            {
                return InterpretTextFallback(step, normalizedText);
            }

            return new StepAnswerInterpretationResult
            {
                IsSuccess = true,
                Value = normalizedText
            };
        }

        return new StepAnswerInterpretationResult
        {
            IsSuccess = true,
            Value = rawValue
        };
    }

    private static StepAnswerInterpretationResult InterpretTextFallback(ScenarioStep step, string rawText)
    {
        if (step.Key.EndsWith("_phone", StringComparison.Ordinal))
        {
            var normalizedPhone = NormalizePhone(rawText);
            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                return new StepAnswerInterpretationResult { IsSuccess = true, Value = normalizedPhone };
            }
        }

        if (step.Key.EndsWith("_passport_unit_code", StringComparison.Ordinal))
        {
            var normalizedUnitCode = NormalizePassportUnitCode(rawText);
            if (!string.IsNullOrWhiteSpace(normalizedUnitCode))
            {
                return new StepAnswerInterpretationResult { IsSuccess = true, Value = normalizedUnitCode };
            }
        }

        if (step.Key.EndsWith("_passport_number", StringComparison.Ordinal))
        {
            var normalizedPassportNumber = NormalizePassportNumber(rawText);
            if (!string.IsNullOrWhiteSpace(normalizedPassportNumber))
            {
                return new StepAnswerInterpretationResult { IsSuccess = true, Value = normalizedPassportNumber };
            }
        }

        if (step.Key.EndsWith("_entrepreneur_inn", StringComparison.Ordinal))
        {
            var normalizedInn = NormalizeDigits(rawText, 12);
            if (!string.IsNullOrWhiteSpace(normalizedInn))
            {
                return new StepAnswerInterpretationResult { IsSuccess = true, Value = normalizedInn };
            }
        }

        if (step.Key.EndsWith("_company_inn", StringComparison.Ordinal))
        {
            var normalizedInn = NormalizeDigits(rawText, 10);
            if (!string.IsNullOrWhiteSpace(normalizedInn))
            {
                return new StepAnswerInterpretationResult { IsSuccess = true, Value = normalizedInn };
            }
        }

        if (step.Key.EndsWith("_entrepreneur_ogrnip", StringComparison.Ordinal))
        {
            var normalizedOgrnip = NormalizeDigits(rawText, 15);
            if (!string.IsNullOrWhiteSpace(normalizedOgrnip))
            {
                return new StepAnswerInterpretationResult { IsSuccess = true, Value = normalizedOgrnip };
            }
        }

        if (step.Key.EndsWith("_company_ogrn", StringComparison.Ordinal))
        {
            var normalizedOgrn = NormalizeDigits(rawText, 13);
            if (!string.IsNullOrWhiteSpace(normalizedOgrn))
            {
                return new StepAnswerInterpretationResult { IsSuccess = true, Value = normalizedOgrn };
            }
        }

        return new StepAnswerInterpretationResult
        {
            IsSuccess = true,
            Value = rawText.Trim()
        };
    }

    private static string? NormalizePhone(string rawText)
    {
        var digits = Regex.Replace(rawText, @"\D", string.Empty);
        if (digits.Length == 11 && (digits.StartsWith('7') || digits.StartsWith('8')))
        {
            return $"+7{digits[1..]}";
        }

        if (digits.Length == 10)
        {
            return $"+7{digits}";
        }

        return null;
    }

    private static string? NormalizePassportUnitCode(string rawText)
    {
        var digits = Regex.Replace(rawText, @"\D", string.Empty);
        if (digits.Length != 6)
        {
            return null;
        }

        return $"{digits[..3]}-{digits[3..]}";
    }

    private static string? NormalizePassportNumber(string rawText)
    {
        var digits = Regex.Replace(rawText, @"\D", string.Empty);
        if (digits.Length != 10)
        {
            return null;
        }

        return $"{digits[..2]} {digits.Substring(2, 2)} {digits[4..]}";
    }

    private static string? NormalizeDigits(string rawText, int expectedLength)
    {
        var digits = Regex.Replace(rawText, @"\D", string.Empty);
        return digits.Length == expectedLength ? digits : null;
    }

    private static StepAnswerInterpretationResult InterpretBoolean(string rawText)
    {
        var normalized = NormalizeText(rawText);
        var falseWords = new[]
        {
            "нет", "неа", "не нужно", "не надо", "не требуется", "не требуется проживание", "проживание не требуется",
            "не нужно проживание", "запрещено", "нельзя", "не разрешено", "не хочу", "без этого", "не будет"
        };
        var trueWords = new[]
        {
            "да", "ага", "угу", "нужно", "надо", "есть", "разрешено", "можно", "конечно", "ок", "окей", "требуется", "будет"
        };

        if (ContainsAny(normalized, falseWords))
        {
            return new StepAnswerInterpretationResult { IsSuccess = true, Value = false };
        }

        if (ContainsAny(normalized, trueWords))
        {
            return new StepAnswerInterpretationResult { IsSuccess = true, Value = true };
        }

        return new StepAnswerInterpretationResult
        {
            IsSuccess = false,
            ErrorMessage = "Не смог понять ответ как «да» или «нет». Например: «да, нужно» или «нет, не нужно»."
        };
    }

    private static StepAnswerInterpretationResult InterpretNumber(ScenarioStep step, string rawText)
    {
        var match = Regex.Match(rawText.Replace(" ", string.Empty, StringComparison.Ordinal), @"-?\d+([.,]\d+)?");
        if (!match.Success)
        {
            return new StepAnswerInterpretationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Не удалось выделить число для шага «{step.Title}»."
            };
        }

        var normalized = match.Value.Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
        {
            return new StepAnswerInterpretationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Не удалось распознать число для шага «{step.Title}»."
            };
        }

        if (number == Math.Truncate(number))
        {
            return new StepAnswerInterpretationResult { IsSuccess = true, Value = (int)number };
        }

        return new StepAnswerInterpretationResult { IsSuccess = true, Value = number };
    }

    private static StepAnswerInterpretationResult InterpretDate(string rawText)
    {
        if (DraftDateParser.TryParse(rawText.Trim(), out var parsed))
        {
            return new StepAnswerInterpretationResult
            {
                IsSuccess = true,
                Value = parsed.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            };
        }

        return new StepAnswerInterpretationResult
        {
            IsSuccess = false,
            ErrorMessage = "Не удалось распознать дату. Напишите ее, например, так: 10.04.2026."
        };
    }

    private static StepAnswerInterpretationResult InterpretSelect(ScenarioStep step, string rawText)
    {
        var normalized = NormalizeText(rawText);

        var orderedOptions = step.Options
            .OrderBy(option => option == "false" ? 0 : option == "true" ? 1 : 2)
            .ToList();

        foreach (var option in orderedOptions)
        {
            if (MatchesOption(option, normalized))
            {
                return new StepAnswerInterpretationResult
                {
                    IsSuccess = true,
                    Value = option
                };
            }
        }

        return new StepAnswerInterpretationResult
        {
            IsSuccess = false,
            ErrorMessage = $"Не удалось понять вариант ответа для шага «{step.Title}». Допустимые варианты: {DraftPromptContextFormatter.FormatReadableOptions(step)}."
        };
    }

    private static bool MatchesOption(string option, string normalized)
    {
        var optionWords = new List<string> { option };

        optionWords.AddRange(option switch
        {
            "individual" => new[]
            {
                "individual", "физ", "физлицо", "физ лицо", "физическое лицо", "гражданин", "человек", "физовик", "физик"
            },
            "entrepreneur" => new[]
            {
                "entrepreneur", "ип", "ипшник", "ипэшник", "предприниматель", "индивидуальный предприниматель"
            },
            "company" => new[]
            {
                "company", "организация", "юрлицо", "юр лицо", "юридическое лицо", "компания", "юрик", "ооо", "зао", "ао", "пао"
            },
            "apartment" => new[] { "apartment", "квартира" },
            "house" => new[] { "house", "дом", "коттедж" },
            "room" => new[] { "room", "комната" },
            "commercial_space" => new[] { "commercial", "коммерческое", "офис", "нежилое", "помещение", "склад", "магазин" },
            "bank_transfer" => new[]
            {
                "перевод", "банковский перевод", "банк", "безнал", "без нал", "безналом", "без нала", "безналичный",
                "на карту", "по реквизитам", "на счет", "на счёт", "на расчетный счет"
            },
            "cash" => new[] { "нал", "налом", "наличка", "налик", "наличные", "кэш", "кеш" },
            "mixed" => new[]
            {
                "смеш", "оба", "и так и так", "комбинирован", "нал безнал", "нал/безнал", "нал + безнал", "и наличкой и переводом"
            },
            "true" => new[] { "да", "нужно", "можно", "разрешено", "есть", "требуется", "будет" },
            "false" => new[] { "нет", "не нужно", "не надо", "не требуется", "нельзя", "запрещено", "не разрешено", "не будет" },
            "monthly" => new[] { "ежемесячно", "каждый месяц", "раз в месяц", "monthly" },
            "quarterly" => new[] { "ежеквартально", "раз в квартал", "квартально", "quarterly" },
            "custom" => new[] { "индивидуально", "по договоренности", "по отдельному графику", "custom" },
            _ => new[] { option }
        });

        return ContainsAny(normalized, optionWords.Select(NormalizeText));
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace('ё', 'е');
    }

    private static bool ContainsAny(string text, IEnumerable<string> words)
    {
        var compactText = CompactText(text);

        return words.Any(word =>
        {
            var normalizedWord = NormalizeText(word);
            var compactWord = CompactText(normalizedWord);

            return text.Contains(normalizedWord, StringComparison.Ordinal)
                || (!string.IsNullOrWhiteSpace(compactWord) &&
                    compactText.Contains(compactWord, StringComparison.Ordinal));
        });
    }

    private static string CompactText(string value)
    {
        return Regex.Replace(value, @"[\s\-/\\]+", string.Empty);
    }

    private static object? UnwrapValue(object? rawValue)
    {
        if (rawValue is not JsonElement jsonElement)
        {
            return rawValue;
        }

        return jsonElement.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number when jsonElement.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when jsonElement.TryGetDecimal(out var decimalValue) => decimalValue,
            _ => jsonElement.ToString()
        };
    }
}
