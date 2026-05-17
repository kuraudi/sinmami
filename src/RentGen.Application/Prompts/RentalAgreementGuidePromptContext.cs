using RentGen.Domain.Enums;

namespace RentGen.Application.Prompts;

public sealed class RentalAgreementGuidePromptContext
{
    public SubscriptionPlan Plan { get; init; }
    public string DocumentContent { get; init; } = string.Empty;
    public string AnswersJson { get; init; } = "{}";
}
