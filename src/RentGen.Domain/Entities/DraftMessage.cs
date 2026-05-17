using RentGen.Domain.Abstractions;
using RentGen.Domain.Enums;

namespace RentGen.Domain.Entities;

public sealed class DraftMessage : Entity
{
    public Guid DraftId { get; set; }
    public ChatRole Role { get; set; }
    public DraftMessageType MessageType { get; set; }
    public string? StepKey { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DocumentDraft? Draft { get; set; }
}
