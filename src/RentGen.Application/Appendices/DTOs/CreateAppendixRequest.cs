using RentGen.Domain.Enums;

namespace RentGen.Application.Appendices.DTOs;

public sealed class CreateAppendixRequest
{
    public AppendixType AppendixType { get; set; }
    public Dictionary<string, object?> Answers { get; set; } = new();
}
