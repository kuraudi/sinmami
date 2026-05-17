using Microsoft.EntityFrameworkCore;
using RentGen.Application.Common.Exceptions;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Documents.DTOs;
using RentGen.Domain.Entities;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Drafts;
using RentGen.Infrastructure.Persistence;

namespace RentGen.Infrastructure.Documents;

public sealed class DocumentGenerationService(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IGuideService guideService,
    IDraftValidationService draftValidationService) : IDocumentGenerationService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IGuideService _guideService = guideService;
    private readonly IDraftValidationService _draftValidationService = draftValidationService;

    public async Task<GenerateDocumentResponse> GenerateAsync(Guid draftId, GenerateDocumentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _draftValidationService.ValidateAsync(draftId, cancellationToken);
        if (!validation.IsValid)
        {
            throw new BusinessValidationException(
                "Для формирования предпросмотра не хватает данных. Проверьте незаполненные шаги и замечания валидации.",
                validation.Errors);
        }

        var draft = await _dbContext.DocumentDrafts
            .FirstAsync(x => x.Id == draftId && x.UserId == _currentUserService.GetUserId(), cancellationToken);

        draft.Status = DraftStatus.Generating;
        draft.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var plan = _currentUserService.GetCurrentPlan();
        if (request.RequestedAppendices.Count > 0 && plan != SubscriptionPlan.Premium)
        {
            throw new UnauthorizedAccessException("Requested appendices require Premium plan.");
        }

        var answers = DraftAnswerReader.Parse(draft.AnswersJson);
        var document = new Document
        {
            UserId = draft.UserId,
            DraftId = draft.Id,
            DocumentType = draft.DocumentType,
            Status = DocumentStatus.Generated,
            PlanSnapshot = plan,
            Title = BuildDocumentTitle(answers),
            Content = RentalAgreementTemplateRenderer.Render(answers, plan),
            StructuredDataJson = draft.AnswersJson,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Documents.Add(document);
        draft.Status = DraftStatus.Generated;
        draft.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.IncludeGuide)
        {
            await _guideService.GetGuideAsync(document.Id, cancellationToken);
        }

        return new GenerateDocumentResponse
        {
            DocumentId = document.Id,
            Status = document.Status,
            GuideMode = plan == SubscriptionPlan.Premium ? GuideType.Personalized : GuideType.Standard,
            GeneratedAtUtc = document.GeneratedAtUtc ?? DateTimeOffset.UtcNow,
            AvailableAppendices = plan == SubscriptionPlan.Premium
                ? Enum.GetValues<AppendixType>().ToList()
                : new List<AppendixType>()
        };
    }

    private static string BuildDocumentTitle(IReadOnlyDictionary<string, System.Text.Json.JsonElement> answers)
    {
        var address = DraftAnswerReader.GetString(answers, "property_address");
        return string.IsNullOrWhiteSpace(address)
            ? "Черновик договора аренды"
            : $"Черновик договора аренды - {address}";
    }
}
