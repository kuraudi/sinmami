using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentGen.Application.Common.Exceptions;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Application.Drafts.DTOs;
using RentGen.Domain.Entities;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Persistence;
using RentGen.Infrastructure.Scenarios;

namespace RentGen.Infrastructure.Drafts;

public sealed class DraftService(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IScenarioProvider scenarioProvider,
    IFeatureAccessService featureAccessService,
    IStepAnswerInterpreter stepAnswerInterpreter) : IDraftService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IScenarioProvider _scenarioProvider = scenarioProvider;
    private readonly IFeatureAccessService _featureAccessService = featureAccessService;
    private readonly IStepAnswerInterpreter _stepAnswerInterpreter = stepAnswerInterpreter;

    public async Task<CreateDraftResponse> CreateAsync(CreateDraftRequest request, CancellationToken cancellationToken)
    {
        var scenario = await _scenarioProvider.GetScenarioAsync(request.DocumentType, cancellationToken);
        var plan = _currentUserService.GetCurrentPlan();
        var steps = ScenarioStepResolver.GetVisibleSteps(scenario, plan, _featureAccessService);
        var firstStep = steps.FirstOrDefault();

        var draft = new DocumentDraft
        {
            UserId = _currentUserService.GetUserId(),
            DocumentType = request.DocumentType,
            Status = DraftStatus.Draft,
            Title = "Rental agreement draft",
            ScenarioVersion = scenario.Version,
            CurrentStepKey = firstStep?.Key
        };

        _dbContext.DocumentDrafts.Add(draft);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateDraftResponse
        {
            DraftId = draft.Id,
            Status = draft.Status,
            DocumentType = draft.DocumentType,
            ScenarioVersion = draft.ScenarioVersion,
            CurrentStepKey = draft.CurrentStepKey
        };
    }

    public async Task<DraftDetailsResponse?> GetAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await _dbContext.DocumentDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == draftId && x.UserId == _currentUserService.GetUserId(), cancellationToken);

        if (draft is null)
        {
            return null;
        }

        return new DraftDetailsResponse
        {
            DraftId = draft.Id,
            DocumentType = draft.DocumentType,
            Status = draft.Status,
            Title = draft.Title,
            ScenarioVersion = draft.ScenarioVersion,
            CurrentStepKey = draft.CurrentStepKey,
            CompletionPercent = draft.CompletionPercent,
            AnswersJson = draft.AnswersJson,
            CreatedAtUtc = draft.CreatedAtUtc,
            UpdatedAtUtc = draft.UpdatedAtUtc
        };
    }

    public async Task<IReadOnlyCollection<DraftStepItemResponse>> GetStepsAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await _dbContext.DocumentDrafts
            .AsNoTracking()
            .FirstAsync(x => x.Id == draftId && x.UserId == _currentUserService.GetUserId(), cancellationToken);

        var scenario = await _scenarioProvider.GetScenarioAsync(draft.DocumentType, cancellationToken);
        var answers = DraftAnswerReader.Parse(draft.AnswersJson);
        var visibleSteps = ScenarioStepResolver.GetVisibleSteps(
            scenario,
            _currentUserService.GetCurrentPlan(),
            _featureAccessService,
            answers);

        return visibleSteps
            .Select(step =>
            {
                answers.TryGetValue(step.Key, out var value);

                return new DraftStepItemResponse
                {
                    Section = step.Section,
                    StepKey = step.Key,
                    Title = step.Title,
                    QuestionText = step.QuestionText,
                    InputType = step.InputType,
                    Required = step.Required,
                    IsAnswered = DraftAnswerReader.HasValue(answers, step.Key),
                    IsCurrent = string.Equals(step.Key, draft.CurrentStepKey, StringComparison.Ordinal),
                    Placeholder = step.Placeholder,
                    HelpText = step.HelpText,
                    Options = step.Options,
                    FeatureCode = step.FeatureCode,
                    Value = answers.ContainsKey(step.Key) ? value : null
                };
            })
            .ToList();
    }

    public async Task<SaveAnswerResponse> SaveAnswerAsync(Guid draftId, SaveAnswerRequest request, CancellationToken cancellationToken)
    {
        var draft = await _dbContext.DocumentDrafts
            .FirstAsync(x => x.Id == draftId && x.UserId == _currentUserService.GetUserId(), cancellationToken);

        var scenario = await _scenarioProvider.GetScenarioAsync(draft.DocumentType, cancellationToken);
        var currentAnswers = DraftAnswerReader.Parse(draft.AnswersJson);
        var availableSteps = ScenarioStepResolver.GetVisibleSteps(
            scenario,
            _currentUserService.GetCurrentPlan(),
            _featureAccessService,
            currentAnswers);

        var step = availableSteps.FirstOrDefault(x => x.Key == request.StepKey);
        if (step is null)
        {
            throw new InvalidOperationException($"Step '{request.StepKey}' is not available for current plan.");
        }

        var interpretation = await _stepAnswerInterpreter.InterpretAsync(step, request.Value, currentAnswers, cancellationToken);
        if (!interpretation.IsSuccess)
        {
            throw new BusinessValidationException(
                "Не удалось сохранить ответ на шаг.",
                new[]
                {
                    new ValidationIssue(
                        "answer_not_understood",
                        interpretation.ErrorMessage ?? "Не удалось понять ответ пользователя.",
                        step.MapsTo,
                        step.Key)
                });
        }

        var answers = JsonSerializer.Deserialize<Dictionary<string, object?>>(draft.AnswersJson) ?? new Dictionary<string, object?>();
        answers[request.StepKey] = interpretation.Value;
        ApplyDefaultCitizenship(answers, request.StepKey, interpretation.Value);

        draft.AnswersJson = JsonSerializer.Serialize(answers);
        draft.Status = DraftStatus.InProgress;
        draft.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var updatedAnswers = DraftAnswerReader.Parse(draft.AnswersJson);
        var visibleSteps = ScenarioStepResolver.GetVisibleSteps(
            scenario,
            _currentUserService.GetCurrentPlan(),
            _featureAccessService,
            updatedAnswers);

        var nextStep = visibleSteps.FirstOrDefault(item => !DraftAnswerReader.HasValue(updatedAnswers, item.Key));

        draft.CurrentStepKey = nextStep?.Key;
        draft.CompletionPercent = visibleSteps.Count == 0
            ? 0
            : (int)Math.Round((double)visibleSteps.Count(item => DraftAnswerReader.HasValue(updatedAnswers, item.Key)) / visibleSteps.Count * 100);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SaveAnswerResponse
        {
            DraftId = draft.Id,
            Status = draft.Status,
            SavedStepKey = request.StepKey,
            NextStepKey = nextStep?.Key,
            CompletionPercent = draft.CompletionPercent
        };
    }

    public async Task<NextQuestionResponse?> GetNextQuestionAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await _dbContext.DocumentDrafts
            .FirstAsync(x => x.Id == draftId && x.UserId == _currentUserService.GetUserId(), cancellationToken);

        var scenario = await _scenarioProvider.GetScenarioAsync(draft.DocumentType, cancellationToken);
        var plan = _currentUserService.GetCurrentPlan();
        var answers = DraftAnswerReader.Parse(draft.AnswersJson);
        var availableSteps = ScenarioStepResolver.GetVisibleSteps(scenario, plan, _featureAccessService, answers);
        var step = availableSteps.FirstOrDefault(x => x.Key == draft.CurrentStepKey)
            ?? availableSteps.FirstOrDefault(x => !DraftAnswerReader.HasValue(answers, x.Key));

        if (step is null)
        {
            return null;
        }

        return new NextQuestionResponse
        {
            DraftId = draft.Id,
            Section = step.Section,
            StepKey = step.Key,
            Title = step.Title,
            QuestionText = step.QuestionText,
            InputType = step.InputType,
            Required = step.Required,
            Placeholder = step.Placeholder,
            HelpText = step.HelpText,
            Options = step.Options,
            FeatureCode = step.FeatureCode,
            IsAvailableForCurrentPlan = true
        };
    }

    private static void ApplyDefaultCitizenship(IDictionary<string, object?> answers, string stepKey, object? value)
    {
        if (stepKey is "landlord_type" or "tenant_type")
        {
            var partyPrefix = stepKey.StartsWith("landlord", StringComparison.Ordinal) ? "landlord" : "tenant";
            var citizenshipKey = $"{partyPrefix}_rf_citizenship_confirmed";

            if (value is string textValue && string.Equals(textValue, "individual", StringComparison.Ordinal))
            {
                if (!answers.ContainsKey(citizenshipKey))
                {
                    answers[citizenshipKey] = true;
                }
            }
            else
            {
                answers.Remove(citizenshipKey);
            }
        }
    }
}
