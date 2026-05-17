using System.Globalization;
using System.Text;
using System.Text.Json;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Drafts;

namespace RentGen.Infrastructure.Documents;

internal static class RentalAgreementTemplateRenderer
{
    public static string Render(
        IReadOnlyDictionary<string, JsonElement> answers,
        SubscriptionPlan plan)
    {
        _ = plan;

        var builder = new StringBuilder();

        builder.AppendLine("ДОГОВОР АРЕНДЫ ЖИЛОГО ПОМЕЩЕНИЯ");
        builder.AppendLine();
        builder.AppendLine($"г. {GetString(answers, "agreement_city", "___")}");
        builder.AppendLine(DraftDateParser.ToRussianContractDate(GetString(answers, "agreement_date")));
        builder.AppendLine();

        builder.AppendLine("1. СТОРОНЫ ДОГОВОРА");
        builder.AppendLine($"1.1. Арендодатель: {BuildPartyIntro(answers, "landlord")}.");
        builder.AppendLine($"1.2. Арендатор: {BuildPartyIntro(answers, "tenant")}.");
        builder.AppendLine("1.3. Арендодатель и Арендатор совместно именуются «Стороны», а по отдельности — «Сторона».");
        builder.AppendLine();

        builder.AppendLine("2. ПРЕДМЕТ ДОГОВОРА");
        builder.AppendLine($"2.1. Арендодатель предоставляет Арендатору за плату во временное владение и пользование {GetPropertyTypeLabel(GetString(answers, "property_type"))} по адресу: {GetString(answers, "property_address", "___")}.");
        builder.AppendLine($"2.2. Характеристика объекта: {FormatSentenceValue(GetString(answers, "property_description", "___"))}.");

        var area = DraftAnswerReader.GetDecimal(answers, "property_area_sqm");
        if (area is not null)
        {
            builder.AppendLine($"2.3. Дополнительные характеристики объекта: площадь — {area.Value.ToString(CultureInfo.InvariantCulture)} кв. м.");
        }
        else
        {
            builder.AppendLine("2.3. Дополнительные характеристики объекта при необходимости уточняются Сторонами в акте приема-передачи или иных подписанных документах.");
        }

        builder.AppendLine();

        builder.AppendLine("3. СРОК АРЕНДЫ");
        builder.AppendLine($"3.1. Настоящий Договор заключен на срок с {DraftDateParser.ToRussianDate(GetString(answers, "lease_start_date"))} по {DraftDateParser.ToRussianDate(GetString(answers, "lease_end_date"))}.");
        builder.AppendLine("3.2. По соглашению Сторон срок аренды может быть изменен или продлен в письменной форме.");
        builder.AppendLine();

        builder.AppendLine("4. ПЛАТА И РАСЧЕТЫ");
        builder.AppendLine($"4.1. Размер арендной платы составляет {FormatMoney(DraftAnswerReader.GetDecimal(answers, "rent_amount"))} в месяц.");
        builder.AppendLine($"4.2. Арендная плата вносится Арендатором ежемесячно не позднее {DraftAnswerReader.GetInt32(answers, "payment_due_day")?.ToString(CultureInfo.InvariantCulture) ?? "___"}-го числа месяца {GetPaymentMethodText(GetString(answers, "payment_method"))}.");
        builder.AppendLine($"4.3. Коммунальные и иные текущие расходы оплачиваются следующим образом: {FormatSentenceValue(GetString(answers, "utilities_payment_terms", "в порядке, дополнительно согласованном Сторонами"))}.");
        builder.AppendLine();

        builder.AppendLine("5. ОСОБЫЕ И ДОПОЛНИТЕЛЬНЫЕ УСЛОВИЯ");
        builder.AppendLine("5.1. Передача помещения Арендатору осуществляется в дату, согласованную Сторонами. При необходимости Стороны подписывают отдельный акт приема-передачи.");
        builder.AppendLine("5.2. Порядок проживания с животными, вопросы обеспечительного платежа и иные дополнительные условия при необходимости оформляются отдельными документами или письменными соглашениями Сторон.");
        builder.AppendLine("5.3. Субаренда и передача помещения третьим лицам без письменного согласия Арендодателя не допускаются.");
        builder.AppendLine();

        builder.AppendLine("6. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ");
        builder.AppendLine("6.1. Все изменения и дополнения к настоящему Договору действительны только при совершении в письменной форме и подписании обеими Сторонами.");
        builder.AppendLine("6.2. Споры, возникающие из настоящего Договора, разрешаются путем переговоров, а при недостижении согласия — в судебном порядке в соответствии с законодательством Российской Федерации.");
        builder.AppendLine("6.3. Настоящий Договор составлен в двух экземплярах, имеющих одинаковую юридическую силу, по одному для каждой из Сторон.");
        builder.AppendLine("6.4. При необходимости Стороны вправе оформлять отдельные акты, приложения и иные письменные соглашения, связанные с настоящим Договором.");
        builder.AppendLine();

        builder.AppendLine("РЕКВИЗИТЫ И ПОДПИСИ СТОРОН");
        builder.AppendLine();
        builder.AppendLine("Арендодатель:");
        builder.AppendLine(BuildPartyDetailsBlock(answers, "landlord"));
        builder.AppendLine();
        builder.AppendLine("Арендатор:");
        builder.AppendLine(BuildPartyDetailsBlock(answers, "tenant"));

        return builder.ToString().TrimEnd();
    }

    private static string BuildPartyIntro(
        IReadOnlyDictionary<string, JsonElement> answers,
        string prefix)
    {
        var name = GetString(answers, $"{prefix}_name", "___");
        var partyType = GetString(answers, $"{prefix}_type", "individual");

        return partyType switch
        {
            "entrepreneur" => $"индивидуальный предприниматель {name}, ОГРНИП {GetString(answers, $"{prefix}_entrepreneur_ogrnip", "___")}, ИНН {GetString(answers, $"{prefix}_entrepreneur_inn", "___")}, зарегистрированный(ая) по адресу: {GetString(answers, $"{prefix}_entrepreneur_registration_address", "___")}",
            "company" => $"{name}, в лице {GetString(answers, $"{prefix}_company_representative_name", "___")}, действующего(ей) на основании {GetString(answers, $"{prefix}_company_representative_basis", "___")}, ОГРН {GetString(answers, $"{prefix}_company_ogrn", "___")}, ИНН {GetString(answers, $"{prefix}_company_inn", "___")}, адрес местонахождения: {GetString(answers, $"{prefix}_company_registered_address", "___")}",
            _ => $"гражданин Российской Федерации {name}, данные документа: {BuildPassportDetails(answers, prefix)}, зарегистрированный(ая) по адресу: {GetString(answers, $"{prefix}_registration_address", "___")}"
        };
    }

    private static string BuildPartyDetailsBlock(
        IReadOnlyDictionary<string, JsonElement> answers,
        string prefix)
    {
        var builder = new StringBuilder();
        var partyType = GetString(answers, $"{prefix}_type", "individual");

        builder.AppendLine($"ФИО / наименование: {GetString(answers, $"{prefix}_name", "___")}");

        switch (partyType)
        {
            case "entrepreneur":
                builder.AppendLine("Статус: индивидуальный предприниматель");
                builder.AppendLine($"ОГРНИП: {GetString(answers, $"{prefix}_entrepreneur_ogrnip", "___")}");
                builder.AppendLine($"ИНН: {GetString(answers, $"{prefix}_entrepreneur_inn", "___")}");
                builder.AppendLine($"Адрес регистрации: {GetString(answers, $"{prefix}_entrepreneur_registration_address", "___")}");
                break;
            case "company":
                builder.AppendLine("Статус: организация");
                builder.AppendLine($"Представитель: {GetString(answers, $"{prefix}_company_representative_name", "___")}");
                builder.AppendLine($"Основание полномочий: {GetString(answers, $"{prefix}_company_representative_basis", "___")}");
                builder.AppendLine($"ОГРН: {GetString(answers, $"{prefix}_company_ogrn", "___")}");
                builder.AppendLine($"ИНН: {GetString(answers, $"{prefix}_company_inn", "___")}");
                builder.AppendLine($"Адрес местонахождения: {GetString(answers, $"{prefix}_company_registered_address", "___")}");
                break;
            default:
                builder.AppendLine("Статус: гражданин Российской Федерации");
                builder.AppendLine($"Данные документа: {BuildPassportDetails(answers, prefix)}");
                builder.AppendLine($"Адрес регистрации / местонахождения: {GetString(answers, $"{prefix}_registration_address", "___")}");
                break;
        }

        builder.AppendLine($"Контактный телефон: {GetString(answers, $"{prefix}_phone", "___")}");
        if (prefix == "landlord")
        {
            builder.AppendLine("Банковские реквизиты для оплаты: __________________");
        }

        builder.AppendLine("Подпись: __________________ / __________________");
        return builder.ToString().TrimEnd();
    }

    private static string BuildPassportDetails(
        IReadOnlyDictionary<string, JsonElement> answers,
        string prefix)
    {
        var legacy = GetString(answers, $"{prefix}_passport_details", string.Empty);
        if (!string.IsNullOrWhiteSpace(legacy))
        {
            return legacy;
        }

        var number = GetString(answers, $"{prefix}_passport_number", string.Empty);
        var issuedBy = GetString(answers, $"{prefix}_passport_issued_by", string.Empty);
        var unitCode = GetString(answers, $"{prefix}_passport_unit_code", string.Empty);
        var issueDate = DraftDateParser.ToRussianDate(GetString(answers, $"{prefix}_passport_issue_date"), string.Empty);

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(number))
        {
            parts.Add(number);
        }

        if (!string.IsNullOrWhiteSpace(issuedBy))
        {
            parts.Add($"выдан {issuedBy}");
        }

        if (!string.IsNullOrWhiteSpace(unitCode))
        {
            parts.Add($"код подразделения {unitCode}");
        }

        if (!string.IsNullOrWhiteSpace(issueDate))
        {
            parts.Add($"дата выдачи {issueDate}");
        }

        return parts.Count == 0 ? "___" : string.Join(", ", parts);
    }

    private static string GetPropertyTypeLabel(string? propertyType) => propertyType switch
    {
        "house" => "жилой дом",
        "room" => "комнату",
        "commercial_space" => "нежилое помещение",
        _ => "квартиру"
    };

    private static string GetPaymentMethodText(string? paymentMethod) => paymentMethod switch
    {
        "cash" => "наличными денежными средствами",
        "mixed" => "смешанным способом, согласованным Сторонами",
        _ => "путем банковского перевода"
    };

    private static string FormatMoney(decimal? amount)
    {
        return amount is null
            ? "___ рублей"
            : $"{amount.Value.ToString("N0", CultureInfo.GetCultureInfo("ru-RU"))} рублей";
    }

    private static string GetString(
        IReadOnlyDictionary<string, JsonElement> answers,
        string key,
        string fallback = "___")
    {
        var value = DraftAnswerReader.GetString(answers, key);
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().TrimEnd('.', '!', '?', ';', ':');
    }

    private static string FormatSentenceValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "___";
        }

        return value.Trim().TrimEnd('.', '!', '?', ';', ':');
    }
}
