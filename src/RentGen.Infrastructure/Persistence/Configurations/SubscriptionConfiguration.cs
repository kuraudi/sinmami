using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentGen.Domain.Entities;
using RentGen.Domain.Enums;

namespace RentGen.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User)
            .WithOne(x => x.Subscription)
            .HasForeignKey<Subscription>(x => x.UserId);

        builder.HasData(new Subscription
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Active,
            StartedAtUtc = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero),
            CreatedAtUtc = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
