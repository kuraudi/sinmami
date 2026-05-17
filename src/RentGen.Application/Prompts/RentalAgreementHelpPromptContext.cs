using RentGen.Domain.Enums;

namespace RentGen.Application.Prompts;

public sealed class RentalAgreementHelpPromptContext
{
    public SubscriptionPlan Plan { get; init; }
    public string UserQuestion { get; init; } = string.Empty;
    public string? StepKey { get; init; }
    public string? StepTitle { get; init; }
    public string? StepQuestionText { get; init; }
    public string? StepHelpText { get; init; }
    public string? StepInputType { get; init; }
    public string StepOptionsText { get; init; } = string.Empty;
    public string CollectedFactsText { get; init; } = string.Empty;
    public string MissingRequiredStepsText { get; init; } = string.Empty;
}
