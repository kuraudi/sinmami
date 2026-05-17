using RentGen.Domain.Enums;

namespace RentGen.Application.Documents.DTOs;

public sealed class GenerateDocumentRequest
{
    public bool IncludeGuide { get; set; } = true;
    public List<AppendixType> RequestedAppendices { get; set; } = new();
}
