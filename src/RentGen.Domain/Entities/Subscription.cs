using RentGen.Domain.Abstractions;
using RentGen.Domain.Enums;

namespace RentGen.Domain.Entities;

public sealed class Subscription : AuditableEntity
{
    public Guid UserId { get; set; }
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public User? User { get; set; }
}
