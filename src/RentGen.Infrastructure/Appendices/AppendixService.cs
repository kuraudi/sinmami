using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentGen.Application.Appendices.DTOs;
using RentGen.Application.Common.Exceptions;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Application.Drafts.DTOs;
using RentGen.Application.Prompts;
using RentGen.Domain.Entities;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Drafts;
using RentGen.Infrastructure.Llm;
using RentGen.Infrastructure.Persistence;

namespace RentGen.Infrastructure.Appendices;

public sealed class AppendixService(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IFeatureAccessService featureAccessService,
    IStepAnswerInterpreter stepAnswerInterpreter,
    IPromptBuilder<RentalAgreementHelpPromptContext> promptBuilder,
    IGenerativeAiClient generativeAiClient) : IAppendixService
{
    private const string MissingAppendixDataMessage = "Для создания приложения не хватает данных.";

    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IFeatureAccessService _featureAccessService = featureAccessService;
    private readonly IStepAnswerInterpreter _stepAnswerInterpreter = stepAnswerInterpreter;
    private readonly IPromptBuilder<RentalAgreementHelpPromptContext> _promptBuilder = promptBuilder;
    private readonly IGenerativeAiClient _generativeAiClient = generativeAiClient;

    public async Task<IReadOnlyCollection<AppendixSummaryResponse>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return await _dbContext.AppendixDocuments
            .AsNoTracking()
            .Where(x => x.ParentDocumentId == documentId && x.UserId == _currentUserService.GetUserId())
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AppendixSummaryResponse
            {
                AppendixId = x.Id,
                AppendixType = x.AppendixType,
                Status = x.Status,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AppendixFlowResponse> GetFlowAsync(Guid documentId, AppendixType appendixType, CancellationToken cancellationToken)
    {
        var parent = await GetParentDocumentAsync(documentId, cancellationToken);
        var parentAnswers = DraftAnswerReader.Parse(parent.StructuredDataJson);
        var existingAppendixJson = await _dbContext.AppendixDocuments
            .AsNoTracking()
            .Where(x =>
                x.ParentDocumentId == documentId &&
                x.UserId == _currentUserService.GetUserId() &&
                x.AppendixType == appendixType)
            .Select(x => x.StructuredDataJson)
            .FirstOrDefaultAsync(cancellationToken);
        var appendixAnswers = string.IsNullOrWhiteSpace(existingAppendixJson)
            ? parentAnswers
            : DraftAnswerReader.Parse(existingAppendixJson);
        var definition = AppendixFlowDefinitionProvider.GetDefinition(appendixType);

        return new AppendixFlowResponse
        {
            DocumentId = documentId,
            AppendixType = appendixType,
            Title = definition.Title,
            Description = definition.Description,
            Steps = definition.Steps
                .Select(step => new AppendixFlowStepResponse
                {
                    Section = step.Section,
                    StepKey = step.Key,
                    Title = step.Title,
                    QuestionText = step.QuestionText,
                    InputType = step.InputType,
                    Required = step.Required,
                    Placeholder = step.Placeholder,
                    HelpText = step.HelpText,
                    Options = step.Options,
                    Value = appendixAnswers.TryGetValue(step.Key, out var value) ? value : null
                })
                .ToList()
        };
    }

    public async Task<AppendixPreviewResponse> PreviewAsync(Guid documentId, AppendixPreviewRequest request, CancellationToken cancellationToken)
    {
        var parent = await GetParentDocumentAsync(documentId, cancellationToken);
        var definition = AppendixFlowDefinitionProvider.GetDefinition(request.AppendixType);
        var preview = await BuildAppendixAsync(parent, request.AppendixType, request.Answers, definition, cancellationToken);

        return new AppendixPreviewResponse
        {
            DocumentId = documentId,
            AppendixType = request.AppendixType,
            Title = definition.Title,
            Description = definition.Description,
            IsReady = preview.Errors.Count == 0 && preview.MissingSteps.Count == 0,
            Content = preview.Content,
            Errors = preview.Errors,
            MissingSteps = preview.MissingSteps,
            NormalizedAnswers = preview.NormalizedAnswers
        };
    }

    public async Task<AskAiResponse> AskAsync(Guid documentId, AskAppendixAiRequest request, CancellationToken cancellationToken)
    {
        var parent = await GetParentDocumentAsync(documentId, cancellationToken);
        var definition = AppendixFlowDefinitionProvider.GetDefinition(request.AppendixType);
        var currentStep = definition.Steps.FirstOrDefault(x => x.Key == request.StepKey) ?? definition.Steps.FirstOrDefault();
        var mergedAnswers = MergeAnswers(parent.StructuredDataJson, request.AppendixType, request.Answers);

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
            CollectedFactsText = DraftPromptContextFormatter.FormatCollectedFacts(definition.Steps, mergedAnswers),
            MissingRequiredStepsText = DraftPromptContextFormatter.FormatMissingRequiredSteps(definition.Steps, mergedAnswers)
        });

        string answer;
        string source;
        string disclaimer;

        try
        {
            var result = await _generativeAiClient.GenerateAsync(
                new AiCompletionRequest("appendix_help", prompt.SystemPrompt, prompt.UserPrompt, 0.15, 700),
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

        return new AskAiResponse
        {
            RelatedStepKey = currentStep?.Key ?? request.StepKey,
            Answer = answer,
            Disclaimer = disclaimer,
            Source = source
        };
    }

    public async Task<CreateAppendixResponse> CreateAsync(Guid documentId, CreateAppendixRequest request, CancellationToken cancellationToken)
    {
        var plan = _currentUserService.GetCurrentPlan();
        if (!_featureAccessService.CanCreateAppendix(plan, request.AppendixType))
        {
            throw new UnauthorizedAccessException("Current plan does not allow creating appendices.");
        }

        var parent = await GetParentDocumentAsync(documentId, cancellationToken);
        var definition = AppendixFlowDefinitionProvider.GetDefinition(request.AppendixType);
        var prepared = await BuildAppendixAsync(parent, request.AppendixType, request.Answers, definition, cancellationToken);

        if (prepared.Errors.Count > 0)
        {
            throw new BusinessValidationException(MissingAppendixDataMessage, prepared.Errors);
        }

        var existing = await _dbContext.AppendixDocuments
            .FirstOrDefaultAsync(
                x => x.ParentDocumentId == documentId && x.UserId == _currentUserService.GetUserId() && x.AppendixType == request.AppendixType,
                cancellationToken);

        if (existing is null)
        {
            existing = new AppendixDocument
            {
                ParentDocumentId = parent.Id,
                UserId = parent.UserId,
                AppendixType = request.AppendixType,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            _dbContext.AppendixDocuments.Add(existing);
        }

        existing.Status = AppendixStatus.Generated;
        existing.Title = definition.Title;
        existing.Content = prepared.Content;
        existing.StructuredDataJson = JsonSerializer.Serialize(prepared.MergedAnswers);
        existing.GeneratedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateAppendixResponse
        {
            AppendixId = existing.Id,
            AppendixType = existing.AppendixType,
            Status = existing.Status,
            Title = existing.Title
        };
    }

    public async Task<AppendixDetailsResponse?> GetAsync(Guid appendixId, CancellationToken cancellationToken)
    {
        return await _dbContext.AppendixDocuments
            .AsNoTracking()
            .Where(x => x.Id == appendixId && x.UserId == _currentUserService.GetUserId())
            .Select(x => new AppendixDetailsResponse
            {
                AppendixId = x.Id,
                ParentDocumentId = x.ParentDocumentId,
                AppendixType = x.AppendixType,
                Status = x.Status,
                Title = x.Title,
                Content = x.Content,
                GeneratedAtUtc = x.GeneratedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<PreparedAppendixResult> BuildAppendixAsync(
        Document parent,
        AppendixType appendixType,
        IReadOnlyDictionary<string, object?> rawAnswers,
        AppendixFlowDefinition definition,
        CancellationToken cancellationToken)
    {
        var parentAnswers = JsonSerializer.Deserialize<Dictionary<string, object?>>(parent.StructuredDataJson) ?? new Dictionary<string, object?>();
        var combinedJson = DraftAnswerReader.Parse(parent.StructuredDataJson);
        var normalizedAnswers = new Dictionary<string, object?>();
        var errors = new List<ValidationIssue>();
        var missingSteps = new List<string>();

        ApplyImplicitFlags(appendixType, parentAnswers);

        foreach (var step in definition.Steps)
        {
            if (rawAnswers.TryGetValue(step.Key, out var rawValue))
            {
                var interpretation = await _stepAnswerInterpreter.InterpretAsync(step, rawValue, combinedJson, cancellationToken);
                if (!interpretation.IsSuccess)
                {
                    errors.Add(new ValidationIssue(
                        "answer_not_understood",
                        interpretation.ErrorMessage ?? $"Не удалось понять ответ для шага «{step.Title}».",
                        step.MapsTo ?? step.Key,
                        step.Key));
                    continue;
                }

                normalizedAnswers[step.Key] = interpretation.Value;
                parentAnswers[step.Key] = interpretation.Value;
                combinedJson = DraftAnswerReader.Parse(JsonSerializer.Serialize(parentAnswers));

                if (step.Required && !DraftAnswerReader.HasValue(combinedJson, step.Key))
                {
                    missingSteps.Add(step.Key);
                    errors.Add(new ValidationIssue(
                        "appendix_data_required",
                        $"Для приложения «{definition.Title}» нужно заполнить шаг «{step.Title}».",
                        step.MapsTo ?? step.Key,
                        step.Key));
                }

                continue;
            }

            if (step.Required && !DraftAnswerReader.HasValue(combinedJson, step.Key))
            {
                missingSteps.Add(step.Key);
                errors.Add(new ValidationIssue(
                    "appendix_data_required",
                    $"Для приложения «{definition.Title}» нужно заполнить шаг «{step.Title}».",
                    step.MapsTo ?? step.Key,
                    step.Key));
            }
        }

        ApplyImplicitFlags(appendixType, parentAnswers);

        var mergedAnswersJson = JsonSerializer.Serialize(parentAnswers);
        var mergedAnswers = DraftAnswerReader.Parse(mergedAnswersJson);
        errors.AddRange(GetCrossFieldIssues(appendixType, mergedAnswers));

        return new PreparedAppendixResult(
            parentAnswers,
            normalizedAnswers,
            RentalAgreementAppendixTemplateRenderer.Render(parent.Title, appendixType, mergedAnswers),
            errors
                .GroupBy(issue => $"{issue.Code}:{issue.StepKey}:{issue.Field}:{issue.Message}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList(),
            missingSteps.Distinct(StringComparer.Ordinal).ToList());
    }

    private async Task<Document> GetParentDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return await _dbContext.Documents
            .FirstAsync(x => x.Id == documentId && x.UserId == _currentUserService.GetUserId(), cancellationToken);
    }

    private static Dictionary<string, JsonElement> MergeAnswers(
        string structuredDataJson,
        AppendixType appendixType,
        IReadOnlyDictionary<string, object?> rawAnswers)
    {
        var parentAnswers = JsonSerializer.Deserialize<Dictionary<string, object?>>(structuredDataJson) ?? new Dictionary<string, object?>();
        ApplyImplicitFlags(appendixType, parentAnswers);

        foreach (var pair in rawAnswers)
        {
            parentAnswers[pair.Key] = pair.Value;
        }

        return DraftAnswerReader.Parse(JsonSerializer.Serialize(parentAnswers));
    }

    private static void ApplyImplicitFlags(AppendixType appendixType, IDictionary<string, object?> answers)
    {
        switch (appendixType)
        {
            case AppendixType.PetAddendum:
                answers["has_pets_clause"] = true;
                break;
            case AppendixType.DepositAgreement:
                answers["deposit_required"] = true;
                break;
        }
    }

    private static IEnumerable<ValidationIssue> GetCrossFieldIssues(
        AppendixType appendixType,
        IReadOnlyDictionary<string, JsonElement> answers)
    {
        if (appendixType == AppendixType.PaymentSchedule)
        {
            if (!DraftAnswerReader.HasValue(answers, "rent_amount"))
            {
                yield return new ValidationIssue(
                    "appendix_data_required",
                    "Для графика платежей в основном договоре должна быть указана сумма аренды.",
                    "rent_amount",
                    "rent_amount");
            }

            if (!DraftAnswerReader.HasValue(answers, "payment_method"))
            {
                yield return new ValidationIssue(
                    "appendix_data_required",
                    "Для графика платежей в основном договоре должен быть указан способ оплаты.",
                    "payment_method",
                    "payment_method");
            }
        }
    }

    private sealed record PreparedAppendixResult(
        IReadOnlyDictionary<string, object?> MergedAnswers,
        Dictionary<string, object?> NormalizedAnswers,
        string Content,
        List<ValidationIssue> Errors,
        List<string> MissingSteps);
}
