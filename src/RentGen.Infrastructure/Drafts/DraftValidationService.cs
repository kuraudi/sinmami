using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Application.Drafts.DTOs;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Persistence;
using RentGen.Infrastructure.Scenarios;

namespace RentGen.Infrastructure.Drafts;

public sealed class DraftValidationService(
    AppDbContext dbContext,
    IScenarioProvider scenarioProvider,
    ICurrentUserService currentUserService,
    IFeatureAccessService featureAccessService) : IDraftValidationService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IScenarioProvider _scenarioProvider = scenarioProvider;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IFeatureAccessService _featureAccessService = featureAccessService;

    public async Task<ValidateDraftResponse> ValidateAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await _dbContext.DocumentDrafts
            .FirstAsync(x => x.Id == draftId && x.UserId == _currentUserService.GetUserId(), cancellationToken);

        var scenario = await _scenarioProvider.GetScenarioAsync(draft.DocumentType, cancellationToken);
        var plan = _currentUserService.GetCurrentPlan();
        var answers = DraftAnswerReader.Parse(draft.AnswersJson);
        var errors = new List<ValidationIssue>();
        var missing = new List<string>();

        var availableSteps = ScenarioStepResolver.GetVisibleSteps(scenario, plan, _featureAccessService, answers);

        foreach (var step in availableSteps.Where(x => x.Required))
        {
            if (!DraftAnswerReader.HasValue(answers, step.Key))
            {
                missing.Add(step.Key);
                errors.Add(new ValidationIssue(
                    "required",
                    $"Поле «{step.Title}» обязательно для заполнения.",
                    step.MapsTo,
                    step.Key));
            }
        }

        ValidateRentalAgreementRules(draft.DocumentType, answers, errors);

        draft.Status = errors.Count == 0 ? DraftStatus.ReadyForGeneration : DraftStatus.InProgress;
        draft.LastValidationJson = JsonSerializer.Serialize(errors);
        draft.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ValidateDraftResponse
        {
            IsValid = errors.Count == 0,
            Status = draft.Status,
            CompletionPercent = draft.CompletionPercent,
            Errors = errors,
            MissingSteps = missing
        };
    }

    private static void ValidateRentalAgreementRules(
        DocumentType documentType,
        IReadOnlyDictionary<string, JsonElement> answers,
        ICollection<ValidationIssue> errors)
    {
        if (documentType != DocumentType.RentalAgreement)
        {
            return;
        }

        if (TryGetDate(answers, "lease_start_date", out var startDate) &&
            TryGetDate(answers, "lease_end_date", out var endDate) &&
            endDate <= startDate)
        {
            errors.Add(new ValidationIssue(
                "date_range_invalid",
                "Дата окончания аренды должна быть позже даты начала.",
                "term.endDate",
                "lease_end_date"));
        }

        ValidateDateIfProvided(answers, errors, "agreement_date", "Дата подписания договора");
        ValidateDateIfProvided(answers, errors, "lease_start_date", "Дата начала аренды");
        ValidateDateIfProvided(answers, errors, "lease_end_date", "Дата окончания аренды");
        ValidateDateIfProvided(answers, errors, "landlord_passport_issue_date", "Дата выдачи документа арендодателя");
        ValidateDateIfProvided(answers, errors, "tenant_passport_issue_date", "Дата выдачи документа арендатора");

        var rentAmount = DraftAnswerReader.GetDecimal(answers, "rent_amount");
        if (rentAmount is <= 0)
        {
            errors.Add(new ValidationIssue(
                "positive_number_required",
                "Размер арендной платы должен быть больше нуля.",
                "payment.rentAmount",
                "rent_amount"));
        }

        var paymentDueDay = DraftAnswerReader.GetInt32(answers, "payment_due_day");
        if (paymentDueDay is < 1 or > 31)
        {
            errors.Add(new ValidationIssue(
                "payment_day_invalid",
                "День оплаты должен быть в диапазоне от 1 до 31.",
                "payment.dueDay",
                "payment_due_day"));
        }

        var propertyArea = DraftAnswerReader.GetDecimal(answers, "property_area_sqm");
        if (propertyArea is not null && propertyArea <= 0)
        {
            errors.Add(new ValidationIssue(
                "positive_number_required",
                "Площадь объекта должна быть больше нуля.",
                "property.areaSqm",
                "property_area_sqm"));
        }

        ValidatePartyBlock(answers, errors, "landlord");
        ValidatePartyBlock(answers, errors, "tenant");
        ValidatePhoneIfProvided(answers, errors, "landlord_phone", "Контакт арендодателя");
        ValidatePhoneIfProvided(answers, errors, "tenant_phone", "Контакт арендатора");
    }

    private static void ValidateDateIfProvided(
        IReadOnlyDictionary<string, JsonElement> answers,
        ICollection<ValidationIssue> errors,
        string key,
        string title)
    {
        if (!DraftAnswerReader.HasValue(answers, key))
        {
            return;
        }

        if (!TryGetDate(answers, key, out _))
        {
            errors.Add(new ValidationIssue(
                "date_invalid",
                $"Поле «{title}» должно быть в формате дд.мм.гггг.",
                key,
                key));
        }
    }

    private static void ValidatePartyBlock(
        IReadOnlyDictionary<string, JsonElement> answers,
        ICollection<ValidationIssue> errors,
        string prefix)
    {
        var partyType = DraftAnswerReader.GetString(answers, $"{prefix}_type");

        switch (partyType)
        {
            case "individual":
                ValidateIndividualBlock(answers, errors, prefix);
                break;
            case "entrepreneur":
                ValidateEntrepreneurBlock(answers, errors, prefix);
                break;
            case "company":
                ValidateCompanyBlock(answers, errors, prefix);
                break;
        }
    }

    private static void ValidateIndividualBlock(
        IReadOnlyDictionary<string, JsonElement> answers,
        ICollection<ValidationIssue> errors,
        string prefix)
    {
        if (DraftAnswerReader.GetBoolean(answers, $"{prefix}_rf_citizenship_confirmed") != true)
        {
            errors.Add(new ValidationIssue(
                "rf_citizenship_required",
                $"Для сценария с {GetPartyLabel(prefix).ToLowerInvariant()}-физлицом нужно подтвердить гражданство Российской Федерации.",
                $"{prefix}.rfCitizenshipConfirmed",
                $"{prefix}_rf_citizenship_confirmed"));
        }

        var passportNumber = DraftAnswerReader.GetString(answers, $"{prefix}_passport_number");
        if (!string.IsNullOrWhiteSpace(passportNumber))
        {
            var normalized = Regex.Replace(passportNumber, @"\s+", " ").Trim();
            if (!Regex.IsMatch(normalized, @"^\d{2}\s\d{2}\s\d{6}$"))
            {
                errors.Add(new ValidationIssue(
                    "passport_number_invalid",
                    $"Поле «{GetPartyLabel(prefix)}: номер документа» должно быть в формате 00 00 000000.",
                    $"{prefix}.passport.number",
                    $"{prefix}_passport_number"));
            }
        }

        var unitCode = DraftAnswerReader.GetString(answers, $"{prefix}_passport_unit_code");
        if (!string.IsNullOrWhiteSpace(unitCode) && !Regex.IsMatch(unitCode.Trim(), @"^\d{3}-\d{3}$"))
        {
            errors.Add(new ValidationIssue(
                "passport_unit_code_invalid",
                $"Поле «{GetPartyLabel(prefix)}: код подразделения» должно быть в формате 000-000.",
                $"{prefix}.passport.unitCode",
                $"{prefix}_passport_unit_code"));
        }
    }

    private static void ValidateEntrepreneurBlock(
        IReadOnlyDictionary<string, JsonElement> answers,
        ICollection<ValidationIssue> errors,
        string prefix)
    {
        var inn = DraftAnswerReader.GetString(answers, $"{prefix}_entrepreneur_inn");
        if (!string.IsNullOrWhiteSpace(inn) && !Regex.IsMatch(inn.Trim(), @"^\d{12}$"))
        {
            errors.Add(new ValidationIssue(
                "entrepreneur_inn_invalid",
                $"Поле «{GetPartyLabel(prefix)}: ИНН ИП» должно содержать 12 цифр.",
                $"{prefix}.entrepreneur.inn",
                $"{prefix}_entrepreneur_inn"));
        }

        var ogrnip = DraftAnswerReader.GetString(answers, $"{prefix}_entrepreneur_ogrnip");
        if (!string.IsNullOrWhiteSpace(ogrnip) && !Regex.IsMatch(ogrnip.Trim(), @"^\d{15}$"))
        {
            errors.Add(new ValidationIssue(
                "entrepreneur_ogrnip_invalid",
                $"Поле «{GetPartyLabel(prefix)}: ОГРНИП» должно содержать 15 цифр.",
                $"{prefix}.entrepreneur.ogrnip",
                $"{prefix}_entrepreneur_ogrnip"));
        }
    }

    private static void ValidateCompanyBlock(
        IReadOnlyDictionary<string, JsonElement> answers,
        ICollection<ValidationIssue> errors,
        string prefix)
    {
        var inn = DraftAnswerReader.GetString(answers, $"{prefix}_company_inn");
        if (!string.IsNullOrWhiteSpace(inn) && !Regex.IsMatch(inn.Trim(), @"^\d{10}$"))
        {
            errors.Add(new ValidationIssue(
                "company_inn_invalid",
                $"Поле «{GetPartyLabel(prefix)}: ИНН организации» должно содержать 10 цифр.",
                $"{prefix}.company.inn",
                $"{prefix}_company_inn"));
        }

        var ogrn = DraftAnswerReader.GetString(answers, $"{prefix}_company_ogrn");
        if (!string.IsNullOrWhiteSpace(ogrn) && !Regex.IsMatch(ogrn.Trim(), @"^\d{13}$"))
        {
            errors.Add(new ValidationIssue(
                "company_ogrn_invalid",
                $"Поле «{GetPartyLabel(prefix)}: ОГРН организации» должно содержать 13 цифр.",
                $"{prefix}.company.ogrn",
                $"{prefix}_company_ogrn"));
        }
    }

    private static void ValidatePhoneIfProvided(
        IReadOnlyDictionary<string, JsonElement> answers,
        ICollection<ValidationIssue> errors,
        string key,
        string title)
    {
        var phone = DraftAnswerReader.GetString(answers, key);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return;
        }

        if (!Regex.IsMatch(phone.Trim(), @"^\+7\d{10}$"))
        {
            errors.Add(new ValidationIssue(
                "phone_invalid",
                $"Поле «{title}» должно быть в формате +7XXXXXXXXXX.",
                key,
                key));
        }
    }

    private static bool TryGetDate(IReadOnlyDictionary<string, JsonElement> answers, string key, out DateOnly value)
    {
        value = default;
        if (!answers.TryGetValue(key, out var rawValue) || rawValue.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        return DraftDateParser.TryParse(rawValue.GetString(), out value);
    }

    private static string GetPartyLabel(string prefix) => prefix switch
    {
        "landlord" => "Арендодатель",
        "tenant" => "Арендатор",
        _ => "Сторона"
    };
}
