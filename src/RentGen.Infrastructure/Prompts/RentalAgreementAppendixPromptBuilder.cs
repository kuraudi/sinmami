using System.Globalization;
using System.Text;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Application.Prompts;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Drafts;

namespace RentGen.Infrastructure.Prompts;

public sealed class RentalAgreementAppendixPromptBuilder : IPromptBuilder<RentalAgreementAppendixPromptContext>
{
    public PromptBuildResult Build(RentalAgreementAppendixPromptContext context)
    {
        var answers = DraftAnswerReader.Parse(context.AnswersJson);

        return new PromptBuildResult(
            """
            You are a legal drafting assistant.
            Draft a Russian appendix document linked to the main residential rental agreement.

            Mandatory appendix structure:
            - appendix title;
            - reference to the main agreement;
            - place and date if available;
            - brief identification of the parties;
            - numbered substantive clauses for this appendix only;
            - final block "Реквизиты и подписи сторон".

            Drafting rules:
            1. Return only the appendix text in Russian.
            2. Use formal legal language suitable for review and signing.
            3. Do not invent facts. Use only the extracted facts and the main agreement context.
            4. Always include signature lines for both parties.
            5. When passport or registration details are available, include them in the final block. If they are missing, keep placeholders.
            6. Keep the appendix focused only on its own topic. Do not reproduce the full main agreement.
            """,
            $"""
            Requested appendix type:
            {GetAppendixName(context.AppendixType)}

            Drafting instructions:
            {GetAppendixInstructions(context.AppendixType)}

            Main agreement title:
            {context.ParentDocumentTitle}

            Extracted facts:
            {BuildFactSheet(answers)}

            Main agreement text:
            {context.ParentDocumentContent}

            Raw structured draft data:
            {context.AnswersJson}
            """);
    }

    private static string BuildFactSheet(IReadOnlyDictionary<string, System.Text.Json.JsonElement> answers)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"- agreement city: {DraftAnswerReader.GetString(answers, "agreement_city") ?? "___"}");
        builder.AppendLine($"- agreement date: {FormatDate(DraftAnswerReader.GetString(answers, "agreement_date"))}");
        builder.AppendLine($"- landlord name: {DraftAnswerReader.GetString(answers, "landlord_name") ?? "___"}");
        builder.AppendLine($"- landlord passport: {BuildPassportDetails(answers, "landlord")}");
        builder.AppendLine($"- landlord registration: {DraftAnswerReader.GetString(answers, "landlord_registration_address") ?? "___"}");
        builder.AppendLine($"- landlord phone: {DraftAnswerReader.GetString(answers, "landlord_phone") ?? "___"}");
        builder.AppendLine($"- tenant name: {DraftAnswerReader.GetString(answers, "tenant_name") ?? "___"}");
        builder.AppendLine($"- tenant passport: {BuildPassportDetails(answers, "tenant")}");
        builder.AppendLine($"- tenant registration: {DraftAnswerReader.GetString(answers, "tenant_registration_address") ?? "___"}");
        builder.AppendLine($"- tenant phone: {DraftAnswerReader.GetString(answers, "tenant_phone") ?? "___"}");
        builder.AppendLine($"- property address: {DraftAnswerReader.GetString(answers, "property_address") ?? "___"}");
        builder.AppendLine($"- property description: {DraftAnswerReader.GetString(answers, "property_description") ?? "___"}");
        builder.AppendLine($"- rent amount: {DraftAnswerReader.GetDecimal(answers, "rent_amount")?.ToString(CultureInfo.InvariantCulture) ?? "___"}");
        builder.AppendLine($"- lease start: {FormatDate(DraftAnswerReader.GetString(answers, "lease_start_date"))}");
        builder.AppendLine($"- lease end: {FormatDate(DraftAnswerReader.GetString(answers, "lease_end_date"))}");
        builder.AppendLine($"- deposit amount: {DraftAnswerReader.GetDecimal(answers, "deposit_amount")?.ToString(CultureInfo.InvariantCulture) ?? "___"}");
        builder.AppendLine($"- deposit return terms: {DraftAnswerReader.GetString(answers, "deposit_return_terms") ?? "___"}");
        builder.AppendLine($"- pets details: {DraftAnswerReader.GetString(answers, "pets_details") ?? "___"}");
        builder.AppendLine($"- pet type: {DraftAnswerReader.GetString(answers, "pet_type") ?? "___"}");
        builder.AppendLine($"- pet count: {DraftAnswerReader.GetInt32(answers, "pet_count")?.ToString(CultureInfo.InvariantCulture) ?? "___"}");
        builder.AppendLine($"- pet residence rules: {DraftAnswerReader.GetString(answers, "pet_residence_rules") ?? "___"}");
        builder.AppendLine($"- handover date: {FormatDate(DraftAnswerReader.GetString(answers, "handover_transfer_date"))}");
        builder.AppendLine($"- handover condition: {DraftAnswerReader.GetString(answers, "handover_property_condition") ?? "___"}");
        builder.AppendLine($"- visible defects: {DraftAnswerReader.GetString(answers, "handover_visible_defects") ?? "___"}");
        builder.AppendLine($"- keys transferred: {DraftAnswerReader.GetString(answers, "handover_keys_transferred") ?? "___"}");
        builder.AppendLine($"- meter readings: {DraftAnswerReader.GetString(answers, "handover_meter_readings") ?? "___"}");

        return builder.ToString().TrimEnd();
    }

    private static string GetAppendixName(AppendixType appendixType) => appendixType switch
    {
        AppendixType.HandoverAct => "Акт приема-передачи",
        AppendixType.InventoryList => "Опись имущества",
        AppendixType.PetAddendum => "Приложение о проживании с животными",
        AppendixType.PaymentSchedule => "График арендных платежей",
        AppendixType.DepositAgreement => "Соглашение об обеспечительном платеже",
        AppendixType.HouseRules => "Правила проживания",
        _ => appendixType.ToString()
    };

    private static string GetAppendixInstructions(AppendixType appendixType) => appendixType switch
    {
        AppendixType.HandoverAct =>
            """
            Prepare a formal handover act.
            Include: transfer date, property condition, visible defects, transferred keys, meter readings, and a short statement that the parties have inspected the premises.
            """,
        AppendixType.InventoryList =>
            """
            Prepare an inventory appendix.
            Include a structured list of transferred furniture, appliances, and other property with condition notes if available.
            """,
        AppendixType.PetAddendum =>
            """
            Prepare a pet residence addendum.
            Include the type and quantity of animals, conditions of residence, sanitary and damage responsibility, and a signature block.
            """,
        AppendixType.PaymentSchedule =>
            """
            Prepare a payment schedule appendix.
            Include the monthly amount, due date pattern, and any practical notes required for payments.
            """,
        AppendixType.DepositAgreement =>
            """
            Prepare a deposit addendum.
            Include the deposit amount, legal purpose of the deposit, return conditions, and grounds for удержание if available.
            """,
        AppendixType.HouseRules =>
            """
            Prepare a house rules appendix.
            Include residence rules, quiet hours, smoking rules, guest restrictions, and other conduct rules that are available from the draft data.
            """,
        _ => "Prepare a legally styled appendix linked to the main rental agreement."
    };

    private static string FormatDate(string? rawDate)
    {
        return DateOnly.TryParse(rawDate, out var parsed)
            ? parsed.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : rawDate ?? "___";
    }

    private static string BuildPassportDetails(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement> answers,
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

        if (!string.IsNullOrWhiteSpace(unitCode))
        {
            parts.Add($"код подразделения {unitCode}");
        }

        if (!string.IsNullOrWhiteSpace(issueDate) && issueDate != "___")
        {
            parts.Add($"дата выдачи {issueDate}");
        }

        return parts.Count == 0 ? "___" : string.Join(", ", parts);
    }
}
