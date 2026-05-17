namespace RentGen.Application.Drafts.DTOs;

public sealed class AskAiResponse
{
    public string Answer { get; set; } = string.Empty;
    public string? RelatedStepKey { get; set; }
    public string Disclaimer { get; set; } = string.Empty;
    public string Source { get; set; } = "fallback";
}
