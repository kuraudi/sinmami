using RentGen.Domain.Enums;

namespace RentGen.Application.Documents.DTOs;

public sealed class GenerateDocumentResponse
{
    public Guid DocumentId { get; set; }
    public DocumentStatus Status { get; set; }
    public GuideType GuideMode { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public List<AppendixType> AvailableAppendices { get; set; } = new();
}
