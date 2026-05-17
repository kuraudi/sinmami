namespace RentGen.Application.Drafts.DTOs;

public sealed class AskAiRequest
{
    public string Question { get; set; } = string.Empty;
    public string? StepKey { get; set; }
}
