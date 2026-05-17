using RentGen.Domain.Abstractions;
using RentGen.Domain.Enums;

namespace RentGen.Domain.Entities;

public sealed class AppendixDocument : AuditableEntity
{
    public Guid ParentDocumentId { get; set; }
    public Guid UserId { get; set; }
    public AppendixType AppendixType { get; set; }
    public AppendixStatus Status { get; set; } = AppendixStatus.Draft;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string StructuredDataJson { get; set; } = "{}";
    public DateTimeOffset? GeneratedAtUtc { get; set; }

    public Document? ParentDocument { get; set; }
    public User? User { get; set; }
}
