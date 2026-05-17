using RentGen.Domain.Enums;

namespace RentGen.Application.Drafts.DTOs;

public sealed class DraftDetailsResponse
{
    public Guid DraftId { get; set; }
    public DocumentType DocumentType { get; set; }
    public DraftStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ScenarioVersion { get; set; } = string.Empty;
    public string? CurrentStepKey { get; set; }
    public int CompletionPercent { get; set; }
    public string AnswersJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
