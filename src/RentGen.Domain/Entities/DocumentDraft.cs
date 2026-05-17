using RentGen.Domain.Abstractions;
using RentGen.Domain.Enums;

namespace RentGen.Domain.Entities;

public sealed class DocumentDraft : AuditableEntity
{
    public Guid UserId { get; set; }
    public DocumentType DocumentType { get; set; }
    public DraftStatus Status { get; set; } = DraftStatus.Draft;
    public string Title { get; set; } = string.Empty;
    public string ScenarioVersion { get; set; } = "rental-basic-v1";
    public string? CurrentStepKey { get; set; }
    public int CompletionPercent { get; set; }
    public string AnswersJson { get; set; } = "{}";
    public string? LastValidationJson { get; set; }

    public User? User { get; set; }
    public ICollection<DraftMessage> Messages { get; set; } = new List<DraftMessage>();
}
