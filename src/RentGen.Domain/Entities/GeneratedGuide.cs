using RentGen.Domain.Abstractions;
using RentGen.Domain.Enums;

namespace RentGen.Domain.Entities;

public sealed class GeneratedGuide : Entity
{
    public Guid DocumentId { get; set; }
    public GuideType GuideType { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Document? Document { get; set; }
}
