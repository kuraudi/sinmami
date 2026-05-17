using RentGen.Domain.Enums;

namespace RentGen.Application.Appendices.DTOs;

public sealed class AppendixDetailsResponse
{
    public Guid AppendixId { get; set; }
    public Guid ParentDocumentId { get; set; }
    public AppendixType AppendixType { get; set; }
    public AppendixStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset? GeneratedAtUtc { get; set; }
}
