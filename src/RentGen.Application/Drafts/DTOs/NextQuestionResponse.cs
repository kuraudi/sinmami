using RentGen.Domain.Enums;

namespace RentGen.Application.Drafts.DTOs;

public sealed class NextQuestionResponse
{
    public Guid DraftId { get; set; }
    public string Section { get; set; } = string.Empty;
    public string StepKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public List<string> Options { get; set; } = new();
    public FeatureCode? FeatureCode { get; set; }
    public bool IsAvailableForCurrentPlan { get; set; }
}
