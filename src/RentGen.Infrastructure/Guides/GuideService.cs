using Microsoft.EntityFrameworkCore;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Guides.DTOs;
using RentGen.Application.Prompts;
using RentGen.Domain.Entities;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Llm;
using RentGen.Infrastructure.Persistence;

namespace RentGen.Infrastructure.Guides;

public sealed class GuideService(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IStandardGuideProvider standardGuideProvider,
    IPromptBuilder<RentalAgreementGuidePromptContext> promptBuilder,
    IGenerativeAiClient generativeAiClient) : IGuideService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IStandardGuideProvider _standardGuideProvider = standardGuideProvider;
    private readonly IPromptBuilder<RentalAgreementGuidePromptContext> _promptBuilder = promptBuilder;
    private readonly IGenerativeAiClient _generativeAiClient = generativeAiClient;

    public async Task<GuideResponse> GetGuideAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var document = await _dbContext.Documents
            .FirstAsync(x => x.Id == documentId && x.UserId == userId, cancellationToken);
        var existing = await _dbContext.GeneratedGuides
            .FirstOrDefaultAsync(x => x.DocumentId == documentId, cancellationToken);

        if (existing is not null)
        {
            return new GuideResponse { DocumentId = documentId, GuideType = existing.GuideType, Content = existing.Content };
        }

        var plan = document.PlanSnapshot;
        string content;
        GuideType guideType;

        if (plan == SubscriptionPlan.Premium)
        {
            try
            {
                var prompt = _promptBuilder.Build(new RentalAgreementGuidePromptContext
                {
                    Plan = plan,
                    DocumentContent = document.Content,
                    AnswersJson = document.StructuredDataJson
                });

                var completion = await _generativeAiClient.GenerateAsync(
                    new Application.Common.Models.AiCompletionRequest("guide_generation", prompt.SystemPrompt, prompt.UserPrompt),
                    cancellationToken);

                content = AiPlainTextFormatter.Normalize(completion.Content);
                guideType = GuideType.Personalized;
            }
            catch
            {
                content = _standardGuideProvider.GetGuide(document.DocumentType);
                guideType = GuideType.Standard;
            }
        }
        else
        {
            content = _standardGuideProvider.GetGuide(document.DocumentType);
            guideType = GuideType.Standard;
        }

        var guide = new GeneratedGuide
        {
            DocumentId = documentId,
            GuideType = guideType,
            Content = content
        };

        _dbContext.GeneratedGuides.Add(guide);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GuideResponse
        {
            DocumentId = documentId,
            GuideType = guideType,
            Content = content
        };
    }
}
