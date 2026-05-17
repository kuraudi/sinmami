namespace RentGen.Application.Common.Models;

public sealed class StepAnswerInterpretationResult
{
    public object? Value { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
