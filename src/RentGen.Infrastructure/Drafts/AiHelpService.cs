using Microsoft.EntityFrameworkCore;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Application.Drafts.DTOs;
using RentGen.Application.Prompts;
using RentGen.Domain.Entities;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Llm;
using RentGen.Infrastructure.Persistence;
using RentGen.Infrastructure.Scenarios;

namespace RentGen.Infrastructure.Drafts;

public sealed class AiHelpService(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IScenarioProvider scenarioProvider,
    IFeatureAccessService featureAccessService,
    IPromptBuilder<RentalAgreementHelpPromptContext> promptBuilder,
    IGenerativeAiClient generativeAiClient) : IAiHelpService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IScenarioProvider _scenarioProvider = scenarioProvider;
    private readonly IFeatureAccessService _featureAccessService = featureAccessService;
    private readonly IPromptBuilder<RentalAgreementHelpPromptContext> _promptBuilder = promptBuilder;
    private readonly IGenerativeAiClient _generativeAiClient = generativeAiClient;

    public async Task<AskAiResponse> AskAsync(Guid draftId, AskAiRequest request, CancellationToken cancellationToken)
    {
        var draft = await _dbContext.DocumentDrafts
            .FirstAsync(x => x.Id == draftId && x.UserId == _currentUserService.GetUserId(), cancellationToken);

        var scenario = await _scenarioProvider.GetScenarioAsync(draft.DocumentType, cancellationToken);
        var answers = DraftAnswerReader.Parse(draft.AnswersJson);
        var visibleSteps = ScenarioStepResolver.GetVisibleSteps(
            scenario,
            _currentUserService.GetCurrentPlan(),
            _featureAccessService,
            answers);

        var currentStep = visibleSteps.FirstOrDefault(x => x.Key == request.StepKey)
            ?? visibleSteps.FirstOrDefault(x => x.Key == draft.CurrentStepKey)
            ?? visibleSteps.FirstOrDefault();

        var prompt = _promptBuilder.Build(new RentalAgreementHelpPromptContext
        {
            Plan = _currentUserService.GetCurrentPlan(),
            UserQuestion = request.Question,
            StepKey = currentStep?.Key,
            StepTitle = currentStep?.Title,
            StepQuestionText = currentStep?.QuestionText,
            StepHelpText = currentStep?.HelpText,
            StepInputType = currentStep?.InputType,
            StepOptionsText = currentStep is null ? string.Empty : DraftPromptContextFormatter.FormatOptions(currentStep),
            CollectedFactsText = DraftPromptContextFormatter.FormatCollectedFacts(visibleSteps, answers),
            MissingRequiredStepsText = DraftPromptContextFormatter.FormatMissingRequiredSteps(visibleSteps, answers)
        });

        string answer;
        string source;
        string disclaimer;

        try
        {
            var result = await _generativeAiClient.GenerateAsync(
                new AiCompletionRequest("draft_help", prompt.SystemPrompt, prompt.UserPrompt, 0.15, 900),
                cancellationToken);

            if (result.Content.StartsWith("[LLM stub:", StringComparison.Ordinal))
            {
                answer = DraftHelpFallbackComposer.Compose(currentStep, request.Question);
                source = "fallback";
                disclaimer = "Сейчас показана резервная локальная подсказка: ответ от DeepSeek не был получен.";
            }
            else
            {
                answer = AiPlainTextFormatter.Normalize(result.Content);
                source = "deepseek";
                disclaimer = string.Empty;
            }
        }
        catch
        {
            answer = DraftHelpFallbackComposer.Compose(currentStep, request.Question);
            source = "fallback";
            disclaimer = "Сейчас показана резервная локальная подсказка: ответ от DeepSeek не был получен.";
        }

        _dbContext.DraftMessages.Add(new DraftMessage
        {
            DraftId = draft.Id,
            Role = ChatRole.User,
            MessageType = DraftMessageType.HelpRequest,
            StepKey = currentStep?.Key ?? request.StepKey,
            Content = request.Question
        });

        _dbContext.DraftMessages.Add(new DraftMessage
        {
            DraftId = draft.Id,
            Role = ChatRole.Assistant,
            MessageType = DraftMessageType.HelpResponse,
            StepKey = currentStep?.Key ?? request.StepKey,
            Content = answer
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AskAiResponse
        {
            RelatedStepKey = currentStep?.Key ?? request.StepKey,
            Answer = answer,
            Disclaimer = disclaimer,
            Source = source
        };
    }
}
