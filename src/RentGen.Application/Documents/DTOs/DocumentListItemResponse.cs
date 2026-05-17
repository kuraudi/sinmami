using RentGen.Domain.Enums;

namespace RentGen.Application.Documents.DTOs;

public sealed class DocumentListItemResponse
{
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public bool HasGuide { get; set; }
    public int AppendicesCount { get; set; }
}
