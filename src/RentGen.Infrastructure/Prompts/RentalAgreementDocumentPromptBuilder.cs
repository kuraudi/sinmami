using System.Globalization;
using System.Text;
using System.Text.Json;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Application.Prompts;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Drafts;

namespace RentGen.Infrastructure.Prompts;

public sealed class RentalAgreementDocumentPromptBuilder : IPromptBuilder<RentalAgreementDocumentPromptContext>
{
    public PromptBuildResult Build(RentalAgreementDocumentPromptContext context)
    {
        var answers = DraftAnswerReader.Parse(context.AnswersJson);
        var appendixRules = BuildAppendixRules(context.Plan, answers);
        var facts = BuildFactSheet(answers);

        var systemPrompt = """
            Ты помогаешь составить черновик договора аренды жилого помещения на русском языке.

            Требования к тексту:
            1. Верни только текст договора без пояснений и без markdown.
            2. Структура основного договора:
               - заголовок;
               - город и дата подписания;
               - раздел 1. Стороны договора;
               - раздел 2. Предмет договора;
               - раздел 3. Срок аренды;
               - раздел 4. Плата и расчеты;
               - раздел 5. Порядок передачи помещения;
               - раздел 6. Заключительные положения;
               - блок "Реквизиты и подписи сторон".
            3. Не создавай разделы после раздела 6.
            4. Если каких-то реквизитов не хватает, оставляй плейсхолдеры ___.
            5. Не придумывай факты, суммы, даты, адреса и данные сторон.
            6. Подробные условия по животным, залогу, правилам проживания, описи имущества и акту приема-передачи не расписывай внутри основного договора. Такие темы можно только кратко упоминать как отдельные приложения, если они действительно включены.
            7. Стиль должен быть юридическим, но понятным.
            """;

        var userPrompt = $"""
            Тариф: {context.Plan}.

            Правила по приложениям:
            {appendixRules}

            Извлеченные данные:
            {facts}

            Сырые данные черновика:
            {context.AnswersJson}
            """;

        return new PromptBuildResult(systemPrompt, userPrompt);
    }

    private static string BuildFactSheet(IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Шапка договора:");
        builder.AppendLine($"- город: {DraftAnswerReader.GetString(answers, "agreement_city") ?? "___"}");
        builder.AppendLine($"- дата подписания: {FormatDate(DraftAnswerReader.GetString(answers, "agreement_date"))}");
        builder.AppendLine();

        builder.AppendLine("Арендодатель:");
        builder.AppendLine($"- тип: {FormatPartyType(DraftAnswerReader.GetString(answers, "landlord_type"))}");
        builder.AppendLine($"- ФИО / наименование: {DraftAnswerReader.GetString(answers, "landlord_name") ?? "___"}");
        builder.AppendLine($"- данные документа: {BuildPassportDetails(answers, "landlord")}");
        builder.AppendLine($"- адрес регистрации: {DraftAnswerReader.GetString(answers, "landlord_registration_address") ?? "___"}");
        builder.AppendLine($"- телефон: {DraftAnswerReader.GetString(answers, "landlord_phone") ?? "___"}");
        builder.AppendLine();

        builder.AppendLine("Арендатор:");
        builder.AppendLine($"- тип: {FormatPartyType(DraftAnswerReader.GetString(answers, "tenant_type"))}");
        builder.AppendLine($"- ФИО / наименование: {DraftAnswerReader.GetString(answers, "tenant_name") ?? "___"}");
        builder.AppendLine($"- данные документа: {BuildPassportDetails(answers, "tenant")}");
        builder.AppendLine($"- адрес регистрации: {DraftAnswerReader.GetString(answers, "tenant_registration_address") ?? "___"}");
        builder.AppendLine($"- телефон: {DraftAnswerReader.GetString(answers, "tenant_phone") ?? "___"}");
        builder.AppendLine();

        builder.AppendLine("Объект аренды:");
        builder.AppendLine($"- тип объекта: {DraftAnswerReader.GetString(answers, "property_type") ?? "___"}");
        builder.AppendLine($"- адрес: {DraftAnswerReader.GetString(answers, "property_address") ?? "___"}");
        builder.AppendLine($"- описание: {DraftAnswerReader.GetString(answers, "property_description") ?? "___"}");
        builder.AppendLine($"- площадь: {DraftAnswerReader.GetDecimal(answers, "property_area_sqm")?.ToString(CultureInfo.InvariantCulture) ?? "___"}");
        builder.AppendLine($"- количество комнат: {DraftAnswerReader.GetInt32(answers, "property_rooms_count")?.ToString(CultureInfo.InvariantCulture) ?? "___"}");
        builder.AppendLine();

        builder.AppendLine("Срок и расчеты:");
        builder.AppendLine($"- дата начала аренды: {FormatDate(DraftAnswerReader.GetString(answers, "lease_start_date"))}");
        builder.AppendLine($"- дата окончания аренды: {FormatDate(DraftAnswerReader.GetString(answers, "lease_end_date"))}");
        builder.AppendLine($"- сумма аренды: {DraftAnswerReader.GetDecimal(answers, "rent_amount")?.ToString(CultureInfo.InvariantCulture) ?? "___"}");
        builder.AppendLine($"- день оплаты: {DraftAnswerReader.GetInt32(answers, "payment_due_day")?.ToString(CultureInfo.InvariantCulture) ?? "___"}");
        builder.AppendLine($"- способ оплаты: {FormatPaymentMethod(DraftAnswerReader.GetString(answers, "payment_method"))}");
        builder.AppendLine($"- коммунальные платежи: {DraftAnswerReader.GetString(answers, "utilities_payment_terms") ?? "___"}");
        builder.AppendLine($"- цель использования: {DraftAnswerReader.GetString(answers, "lease_purpose") ?? "___"}");
        builder.AppendLine();

        builder.AppendLine("Отдельные приложения:");
        builder.AppendLine($"- нужен залог: {FormatBoolean(DraftAnswerReader.GetBoolean(answers, "deposit_required"))}");
        builder.AppendLine($"- нужны условия о животных: {FormatBoolean(DraftAnswerReader.GetBoolean(answers, "has_pets_clause"))}");
        builder.AppendLine($"- субаренда разрешена: {FormatBoolean(DraftAnswerReader.GetBoolean(answers, "sublease_allowed"))}");

        return builder.ToString().TrimEnd();
    }

    private static string BuildAppendixRules(
        SubscriptionPlan plan,
        IReadOnlyDictionary<string, JsonElement> answers)
    {
        if (plan != SubscriptionPlan.Premium)
        {
            return "- не упоминай премиальные приложения и расширенные приложения в основном договоре";
        }

        var items = new List<string>();

        if (DraftAnswerReader.GetBoolean(answers, "deposit_required") == true)
        {
            items.Add("- в основном договоре можно кратко указать, что обеспечительный платеж предусмотрен и подробные условия вынесены в отдельное соглашение");
        }

        if (DraftAnswerReader.GetBoolean(answers, "has_pets_clause") == true)
        {
            items.Add("- в основном договоре можно кратко указать, что проживание с животными оформляется отдельным приложением");
        }

        if (DraftAnswerReader.GetBoolean(answers, "sublease_allowed").HasValue)
        {
            items.Add("- режим субаренды излагай кратко, без отдельного большого раздела");
        }

        return items.Count == 0
            ? "- дополнительные приложения отдельно не упоминаются"
            : string.Join(Environment.NewLine, items);
    }

    private static string BuildPassportDetails(
        IReadOnlyDictionary<string, JsonElement> answers,
        string prefix)
    {
        var legacy = DraftAnswerReader.GetString(answers, $"{prefix}_passport_details");
        if (!string.IsNullOrWhiteSpace(legacy))
        {
            return legacy;
        }

        var parts = new List<string>();
        var number = DraftAnswerReader.GetString(answers, $"{prefix}_passport_number");
        var issuedBy = DraftAnswerReader.GetString(answers, $"{prefix}_passport_issued_by");
        var unitCode = DraftAnswerReader.GetString(answers, $"{prefix}_passport_unit_code");
        var issueDate = FormatDate(DraftAnswerReader.GetString(answers, $"{prefix}_passport_issue_date"));

        if (!string.IsNullOrWhiteSpace(number))
        {
            parts.Add(number);
        }

        if (!string.IsNullOrWhiteSpace(issuedBy))
        {
            parts.Add($"выдан {issuedBy}");
        }

        if (!string.IsNullOrWhiteSpace(unitCode) && unitCode != "___")
        {
            parts.Add($"код подразделения {unitCode}");
        }

        if (!string.IsNullOrWhiteSpace(issueDate) && issueDate != "___")
        {
            parts.Add($"дата выдачи {issueDate}");
        }

        return parts.Count == 0 ? "___" : string.Join(", ", parts);
    }

    private static string FormatDate(string? rawDate)
    {
        return DateOnly.TryParse(rawDate, out var parsed)
            ? parsed.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : rawDate ?? "___";
    }

    private static string FormatBoolean(bool? value) => value switch
    {
        true => "да",
        false => "нет",
        null => "___"
    };

    private static string FormatPartyType(string? value) => value switch
    {
        "individual" => "физлицо",
        "company" => "организация",
        "ip" => "ИП",
        _ => value ?? "___"
    };

    private static string FormatPaymentMethod(string? value) => value switch
    {
        "cash" => "наличные",
        "bank_transfer" => "банковский перевод",
        "mixed" => "смешанный способ",
        _ => value ?? "___"
    };
}
