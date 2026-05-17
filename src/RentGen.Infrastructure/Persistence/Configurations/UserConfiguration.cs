using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentGen.Domain.Entities;
using RentGen.Domain.Enums;

namespace RentGen.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(256);
        builder.HasIndex(x => x.Email).IsUnique();

        builder.HasData(new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "demo@rentgen.local",
            PasswordHash = "demo-hash",
            FullName = "Demo User",
            CreatedAtUtc = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
