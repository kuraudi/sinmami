using RentGen.Application.Appendices.DTOs;
using RentGen.Application.Guides.DTOs;
using RentGen.Domain.Enums;

namespace RentGen.Application.Documents.DTOs;

public sealed class DocumentDetailsResponse
{
    public Guid DocumentId { get; set; }
    public Guid? DraftId { get; set; }
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; }
    public DateTimeOffset? GeneratedAtUtc { get; set; }
    public GuideResponse? Guide { get; set; }
    public List<AppendixSummaryResponse> Appendices { get; set; } = new();
}
