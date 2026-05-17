using RentGen.Domain.Enums;

namespace RentGen.Application.Appendices.DTOs;

public sealed class AskAppendixAiRequest
{
    public AppendixType AppendixType { get; set; }
    public string? StepKey { get; set; }
    public string Question { get; set; } = string.Empty;
    public Dictionary<string, object?> Answers { get; set; } = new();
}
