using RentGen.Domain.Enums;

namespace RentGen.Application.Prompts;

public sealed class RentalAgreementDocumentPromptContext
{
    public SubscriptionPlan Plan { get; init; }
    public string AnswersJson { get; init; } = "{}";
}
