using System.Globalization;
using System.Text;
using System.Text.Json;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Drafts;

namespace RentGen.Infrastructure.Appendices;

internal static class RentalAgreementAppendixTemplateRenderer
{
    public static string Render(
        string parentDocumentTitle,
        AppendixType appendixType,
        IReadOnlyDictionary<string, JsonElement> answers)
    {
        return appendixType switch
        {
            AppendixType.HandoverAct => RenderHandoverAct(parentDocumentTitle, answers),
            AppendixType.InventoryList => RenderInventoryList(parentDocumentTitle, answers),
            AppendixType.PetAddendum => RenderPetAddendum(parentDocumentTitle, answers),
            AppendixType.PaymentSchedule => RenderPaymentSchedule(parentDocumentTitle, answers),
            AppendixType.DepositAgreement => RenderDepositAgreement(parentDocumentTitle, answers),
            AppendixType.HouseRules => RenderHouseRules(parentDocumentTitle, answers),
            _ => RenderGenericAppendix(parentDocumentTitle, appendixType, answers)
        };
    }

    private static string RenderHandoverAct(string parentDocumentTitle, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = CreateActHeader("АКТ ПРИЕМА-ПЕРЕДАЧИ ЖИЛОГО ПОМЕЩЕНИЯ", parentDocumentTitle, answers);
        builder.AppendLine("1. Во исполнение условий договора аренды Арендодатель передает, а Арендатор принимает жилое помещение по адресу:");
        builder.AppendLine($"   {GetString(answers, "property_address")}.");
        builder.AppendLine($"2. Дата фактической передачи помещения: {DraftDateParser.ToRussianDate(GetString(answers, "handover_transfer_date"))}.");
        builder.AppendLine($"3. Состояние помещения на момент передачи: {GetString(answers, "handover_property_condition")}.");
        builder.AppendLine($"4. Видимые недостатки и замечания: {GetString(answers, "handover_visible_defects", "не указаны")}.");
        builder.AppendLine($"5. Переданные ключи и средства доступа: {GetString(answers, "handover_keys_transferred")}.");
        builder.AppendLine($"6. Показания приборов учета на дату передачи: {GetString(answers, "handover_meter_readings")}.");
        builder.AppendLine("7. Стороны подтверждают, что помещение осмотрено, фактически передано и принято с учетом сведений, указанных в настоящем акте.");
        builder.AppendLine("8. Настоящий акт составлен в двух экземплярах, имеющих одинаковую юридическую силу, по одному для каждой из Сторон.");
        AppendPartyFooter(builder, answers, false);
        return builder.ToString().TrimEnd();
    }

    private static string RenderInventoryList(string parentDocumentTitle, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = CreateAppendixHeader("ОПИСЬ ИМУЩЕСТВА", parentDocumentTitle, answers);
        builder.AppendLine("1. Настоящая опись составлена в отношении имущества, находящегося в жилом помещении по адресу:");
        builder.AppendLine($"   {GetString(answers, "property_address")}.");
        builder.AppendLine("2. Указанное имущество передается Арендатору вместе с помещением и используется на условиях договора аренды.");
        builder.AppendLine($"3. Перечень имущества: {GetString(answers, "inventory_items_description")}.");
        builder.AppendLine($"4. Состояние имущества и замечания: {GetString(answers, "inventory_items_condition", "отдельные замечания не указаны")}.");
        builder.AppendLine("5. Настоящая опись применяется при передаче и возврате помещения и используется совместно с актом приема-передачи, если он подписан Сторонами.");
        AppendPartyFooter(builder, answers, false);
        return builder.ToString().TrimEnd();
    }

    private static string RenderPetAddendum(string parentDocumentTitle, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = CreateAppendixHeader("СОГЛАШЕНИЕ О ПРОЖИВАНИИ С ДОМАШНИМИ ЖИВОТНЫМИ", parentDocumentTitle, answers);
        builder.AppendLine("1. Стороны согласовали возможность проживания в арендуемом помещении домашнего животного.");
        builder.AppendLine($"1.1. Вид животного: {GetString(answers, "pet_type")}.");
        builder.AppendLine($"1.2. Количество животных: {DraftAnswerReader.GetInt32(answers, "pet_count")?.ToString(CultureInfo.InvariantCulture) ?? "___"}.");
        builder.AppendLine($"1.3. Описание животного: {GetString(answers, "pets_details")}.");
        builder.AppendLine($"2. Дополнительные правила проживания с животным: {GetString(answers, "pet_residence_rules")}.");
        builder.AppendLine("3. Арендатор обязан обеспечивать надлежащий уход за животным, соблюдать санитарные требования, поддерживать помещение в надлежащем состоянии и не нарушать права соседей.");
        builder.AppendLine("4. Арендатор несет имущественную ответственность за вред, причиненный животным помещению, имуществу Арендодателя, общему имуществу дома и третьим лицам.");
        builder.AppendLine("5. Настоящее соглашение применяется совместно с договором аренды и действует в течение всего срока проживания животного в объекте.");
        AppendPartyFooter(builder, answers, false);
        return builder.ToString().TrimEnd();
    }

    private static string RenderPaymentSchedule(string parentDocumentTitle, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = CreateAppendixHeader("ГРАФИК АРЕНДНЫХ ПЛАТЕЖЕЙ", parentDocumentTitle, answers);
        builder.AppendLine("1. Настоящий график определяет порядок и ориентировочные сроки внесения арендной платы по договору аренды.");
        builder.AppendLine($"2. Размер арендной платы: {FormatMoney(DraftAnswerReader.GetDecimal(answers, "rent_amount"))}.");
        builder.AppendLine($"3. Основной срок внесения платежа: не позднее {DraftAnswerReader.GetInt32(answers, "payment_due_day")?.ToString(CultureInfo.InvariantCulture) ?? "___"}-го числа каждого месяца.");
        builder.AppendLine($"4. Способ оплаты: {GetPaymentMethodText(GetString(answers, "payment_method", "bank_transfer"))}.");
        builder.AppendLine($"5. Дата первого платежа: {DraftDateParser.ToRussianDate(GetString(answers, "payment_schedule_first_payment_date"))}.");
        builder.AppendLine($"6. Периодичность платежей: {GetPaymentFrequencyText(GetString(answers, "payment_schedule_frequency", "monthly"))}.");
        builder.AppendLine($"7. Дополнительные примечания по платежам: {GetString(answers, "payment_schedule_notes", "отдельно не установлены")}.");
        builder.AppendLine("8. Настоящий график применяется совместно с условиями договора аренды о расчетах.");
        AppendPartyFooter(builder, answers, true);
        return builder.ToString().TrimEnd();
    }

    private static string RenderDepositAgreement(string parentDocumentTitle, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = CreateAppendixHeader("СОГЛАШЕНИЕ ОБ ОБЕСПЕЧИТЕЛЬНОМ ПЛАТЕЖЕ", parentDocumentTitle, answers);
        builder.AppendLine($"1. Арендатор вносит Арендодателю обеспечительный платеж в размере {FormatMoney(DraftAnswerReader.GetDecimal(answers, "deposit_amount"))}.");
        builder.AppendLine("2. Обеспечительный платеж служит обеспечением исполнения обязательств Арендатора по договору аренды, включая оплату аренды, коммунальных платежей и возмещение ущерба.");
        builder.AppendLine($"3. Условия возврата обеспечительного платежа: {GetString(answers, "deposit_return_terms")}.");
        builder.AppendLine($"4. Условия удержания обеспечительного платежа: {GetString(answers, "deposit_retention_terms", "при наличии задолженности, ущерба или иных документально подтвержденных нарушений обязательств Арендатора")}.");
        builder.AppendLine("5. Оставшаяся после удержаний сумма подлежит возврату Арендатору в порядке и сроки, согласованные Сторонами.");
        builder.AppendLine("6. Настоящее соглашение применяется совместно с договором аренды.");
        AppendPartyFooter(builder, answers, true);
        return builder.ToString().TrimEnd();
    }

    private static string RenderHouseRules(string parentDocumentTitle, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = CreateAppendixHeader("ПРАВИЛА ПРОЖИВАНИЯ", parentDocumentTitle, answers);
        builder.AppendLine("1. Настоящие правила обязательны для Арендатора, членов его семьи, гостей и иных лиц, допущенных в помещение по воле Арендатора.");
        builder.AppendLine($"2. Правило о курении: {GetString(answers, "smoking_policy")}.");
        builder.AppendLine($"3. Правило о шуме и режиме тишины: {GetString(answers, "noise_policy")}.");
        builder.AppendLine($"4. Правила о гостях и третьих лицах: {GetString(answers, "guest_policy")}.");
        builder.AppendLine($"5. Субаренда и передача помещения третьим лицам: {(DraftAnswerReader.GetBoolean(answers, "sublease_allowed") == true ? "допускается только с предварительного письменного согласия Арендодателя" : "не допускается без предварительного письменного согласия Арендодателя")}.");
        builder.AppendLine($"6. Дополнительные правила проживания: {GetString(answers, "additional_rules", "отдельно не установлены")}.");
        builder.AppendLine("7. Нарушение настоящих правил рассматривается как нарушение условий договора аренды и может повлечь применение мер ответственности, предусмотренных договором и законодательством Российской Федерации.");
        AppendPartyFooter(builder, answers, false);
        return builder.ToString().TrimEnd();
    }

    private static string RenderGenericAppendix(
        string parentDocumentTitle,
        AppendixType appendixType,
        IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = CreateAppendixHeader(AppendixFlowDefinitionProvider.GetTitle(appendixType).ToUpperInvariant(), parentDocumentTitle, answers);
        builder.AppendLine("1. Настоящий документ составлен в дополнение к договору аренды.");
        builder.AppendLine("2. Во всем, что прямо не урегулировано настоящим документом, Стороны руководствуются условиями договора аренды и законодательством Российской Федерации.");
        builder.AppendLine("3. Настоящий документ действует совместно с договором аренды после его подписания Сторонами.");
        AppendPartyFooter(builder, answers, false);
        return builder.ToString().TrimEnd();
    }

    private static StringBuilder CreateAppendixHeader(string title, string parentDocumentTitle, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = new StringBuilder();
        builder.AppendLine("ПРИЛОЖЕНИЕ № ___");
        builder.AppendLine("к Договору аренды жилого помещения");
        builder.AppendLine();
        builder.AppendLine(title);
        builder.AppendLine();
        builder.AppendLine($"к документу: {parentDocumentTitle}");
        builder.AppendLine($"г. {GetString(answers, "agreement_city")}");
        builder.AppendLine(DraftDateParser.ToRussianContractDate(GetString(answers, "agreement_date")));
        builder.AppendLine();
        builder.AppendLine(BuildPartyPreamble(answers));
        builder.AppendLine();
        return builder;
    }

    private static StringBuilder CreateActHeader(string title, string parentDocumentTitle, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var builder = new StringBuilder();
        builder.AppendLine(title);
        builder.AppendLine();
        builder.AppendLine($"к договору: {parentDocumentTitle}");
        builder.AppendLine($"г. {GetString(answers, "agreement_city")}");
        builder.AppendLine(DraftDateParser.ToRussianContractDate(GetString(answers, "agreement_date")));
        builder.AppendLine();
        builder.AppendLine(BuildPartyPreamble(answers));
        builder.AppendLine();
        return builder;
    }

    private static string BuildPartyPreamble(IReadOnlyDictionary<string, JsonElement> answers)
    {
        var landlordName = GetString(answers, "landlord_name");
        var tenantName = GetString(answers, "tenant_name");
        var landlordPassport = BuildPassportDetails(answers, "landlord");
        var tenantPassport = BuildPassportDetails(answers, "tenant");

        return
            $"{landlordName}, данные документа: {landlordPassport}, именуемый(ая) в дальнейшем «Арендодатель», с одной стороны, " +
            $"и {tenantName}, данные документа: {tenantPassport}, именуемый(ая) в дальнейшем «Арендатор», с другой стороны, " +
            "совместно именуемые «Стороны», составили настоящий документ о нижеследующем.";
    }

    private static void AppendPartyFooter(StringBuilder builder, IReadOnlyDictionary<string, JsonElement> answers, bool includeLandlordBankDetails)
    {
        builder.AppendLine();
        builder.AppendLine("РЕКВИЗИТЫ И ПОДПИСИ СТОРОН");
        builder.AppendLine();
        builder.AppendLine("Арендодатель:");
        builder.AppendLine($"ФИО: {GetString(answers, "landlord_name")}");
        builder.AppendLine($"Данные документа: {BuildPassportDetails(answers, "landlord")}");
        builder.AppendLine($"Адрес регистрации: {GetString(answers, "landlord_registration_address")}");
        builder.AppendLine($"Контактный телефон: {GetString(answers, "landlord_phone")}");
        if (includeLandlordBankDetails)
        {
            builder.AppendLine("Банковские реквизиты: __________________");
        }

        builder.AppendLine("Подпись: __________________ / __________________");
        builder.AppendLine();
        builder.AppendLine("Арендатор:");
        builder.AppendLine($"ФИО: {GetString(answers, "tenant_name")}");
        builder.AppendLine($"Данные документа: {BuildPassportDetails(answers, "tenant")}");
        builder.AppendLine($"Адрес регистрации: {GetString(answers, "tenant_registration_address")}");
        builder.AppendLine($"Контактный телефон: {GetString(answers, "tenant_phone")}");
        builder.AppendLine("Подпись: __________________ / __________________");
    }

    private static string BuildPassportDetails(IReadOnlyDictionary<string, JsonElement> answers, string prefix)
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

    private static string GetPaymentMethodText(string value) => value switch
    {
        "cash" => "наличными денежными средствами",
        "bank_transfer" => "путем банковского перевода",
        "mixed" => "смешанным способом",
        _ => value
    };

    private static string GetPaymentFrequencyText(string value) => value switch
    {
        "monthly" => "ежемесячно",
        "quarterly" => "ежеквартально",
        "custom" => "по индивидуальному графику",
        _ => value
    };

    private static string FormatMoney(decimal? value)
    {
        return value.HasValue
            ? $"{value.Value.ToString("N0", CultureInfo.InvariantCulture)} рублей"
            : "___ рублей";
    }

    private static string GetString(IReadOnlyDictionary<string, JsonElement> answers, string key, string fallback = "___")
    {
        var value = DraftAnswerReader.GetString(answers, key);
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().TrimEnd('.', '!', '?', ';', ':');
    }
}
