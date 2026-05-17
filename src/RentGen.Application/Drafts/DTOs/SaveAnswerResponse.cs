using RentGen.Domain.Enums;

namespace RentGen.Application.Drafts.DTOs;

public sealed class SaveAnswerResponse
{
    public Guid DraftId { get; set; }
    public DraftStatus Status { get; set; }
    public string SavedStepKey { get; set; } = string.Empty;
    public string? NextStepKey { get; set; }
    public int CompletionPercent { get; set; }
}
