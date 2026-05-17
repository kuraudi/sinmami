using RentGen.Domain.Abstractions;
using RentGen.Domain.Enums;

namespace RentGen.Domain.Entities;

public sealed class Document : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? DraftId { get; set; }
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public SubscriptionPlan PlanSnapshot { get; set; } = SubscriptionPlan.Free;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string StructuredDataJson { get; set; } = "{}";
    public DateTimeOffset? GeneratedAtUtc { get; set; }

    public User? User { get; set; }
    public DocumentDraft? Draft { get; set; }
    public ICollection<AppendixDocument> Appendices { get; set; } = new List<AppendixDocument>();
    public GeneratedGuide? Guide { get; set; }
}
