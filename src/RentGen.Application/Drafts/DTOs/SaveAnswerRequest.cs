namespace RentGen.Application.Drafts.DTOs;

public sealed class SaveAnswerRequest
{
    public string StepKey { get; set; } = string.Empty;
    public object? Value { get; set; }
}
