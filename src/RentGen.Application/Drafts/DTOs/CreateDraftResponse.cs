using RentGen.Domain.Enums;

namespace RentGen.Application.Drafts.DTOs;

public sealed class CreateDraftResponse
{
    public Guid DraftId { get; set; }
    public DraftStatus Status { get; set; }
    public DocumentType DocumentType { get; set; }
    public string ScenarioVersion { get; set; } = string.Empty;
    public string? CurrentStepKey { get; set; }
}
