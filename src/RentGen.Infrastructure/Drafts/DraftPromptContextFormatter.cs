using System.Globalization;
using System.Text;
using System.Text.Json;
using RentGen.Application.Common.Models;

namespace RentGen.Infrastructure.Drafts;

internal static class DraftPromptContextFormatter
{
    public static string FormatCollectedFacts(
        IEnumerable<ScenarioStep> scenarioSteps,
        IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = new StringBuilder();

        foreach (var step in scenarioSteps)
        {
            if (!DraftAnswerReader.HasValue(answers, step.Key) || !answers.TryGetValue(step.Key, out var value))
            {
                continue;
            }

            builder.Append("- ");
            builder.Append(step.Title);
            builder.Append(": ");
            builder.AppendLine(FormatValue(value));
        }

        return builder.Length == 0 ? "Пока нет сохраненных данных." : builder.ToString().TrimEnd();
    }

    public static string FormatMissingRequiredSteps(
        IEnumerable<ScenarioStep> visibleSteps,
        IReadOnlyDictionary<string, JsonElement> answers)
    {
        var missing = visibleSteps
            .Where(step => step.Required && !DraftAnswerReader.HasValue(answers, step.Key))
            .Select(step => step.Title)
            .ToList();

        return missing.Count == 0
            ? "Все обязательные шаги уже заполнены."
            : string.Join(", ", missing);
    }

    public static string FormatOptions(ScenarioStep step)
    {
        if (step.Options.Count == 0)
        {
            return "Нет фиксированных вариантов.";
        }

        return string.Join(", ", step.Options.Select(option => $"{option} ({GetOptionLabel(option)})"));
    }

    public static string FormatReadableOptions(ScenarioStep step)
    {
        if (step.Options.Count == 0)
        {
            return "Нет фиксированных вариантов.";
        }

        return string.Join(", ", step.Options.Select(GetOptionLabel));
    }

    public static string ToDisplayString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            bool boolValue => boolValue ? "Да" : "Нет",
            string stringValue => stringValue,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static string FormatValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => "Да",
            JsonValueKind.False => "Нет",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.String => value.GetString() ?? string.Empty,
            _ => value.ToString()
        };
    }

    internal static string GetOptionLabel(string option) => option switch
    {
        "individual" => "физическое лицо",
        "entrepreneur" => "индивидуальный предприниматель",
        "company" => "организация",
        "apartment" => "квартира",
        "house" => "дом",
        "room" => "комната",
        "commercial_space" => "коммерческое помещение",
        "bank_transfer" => "банковский перевод",
        "cash" => "наличные",
        "mixed" => "смешанный способ",
        "true" => "да",
        "false" => "нет",
        "monthly" => "ежемесячно",
        "quarterly" => "ежеквартально",
        "custom" => "по отдельному графику",
        _ => option
    };
}
