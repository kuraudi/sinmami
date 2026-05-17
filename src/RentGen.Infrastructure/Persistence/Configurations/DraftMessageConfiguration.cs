using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentGen.Domain.Entities;

namespace RentGen.Infrastructure.Persistence.Configurations;

public sealed class DraftMessageConfiguration : IEntityTypeConfiguration<DraftMessage>
{
    public void Configure(EntityTypeBuilder<DraftMessage> builder)
    {
        builder.ToTable("draft_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StepKey).HasMaxLength(128);
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.HasIndex(x => new { x.DraftId, x.CreatedAtUtc });
        builder.HasOne(x => x.Draft)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.DraftId);
    }
}
