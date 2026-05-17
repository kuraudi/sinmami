using RentGen.Domain.Enums;

namespace RentGen.Application.Prompts;

public sealed class RentalAgreementAppendixPromptContext
{
    public SubscriptionPlan Plan { get; init; }
    public AppendixType AppendixType { get; init; }
    public string ParentDocumentTitle { get; init; } = string.Empty;
    public string ParentDocumentContent { get; init; } = string.Empty;
    public string AnswersJson { get; init; } = "{}";
}
