using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentGen.Domain.Entities;

namespace RentGen.Infrastructure.Persistence.Configurations;

public sealed class DocumentDraftConfiguration : IEntityTypeConfiguration<DocumentDraft>
{
    public void Configure(EntityTypeBuilder<DocumentDraft> builder)
    {
        builder.ToTable("document_drafts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ScenarioVersion).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CurrentStepKey).HasMaxLength(128);
        builder.Property(x => x.AnswersJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.LastValidationJson).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasOne(x => x.User)
            .WithMany(x => x.Drafts)
            .HasForeignKey(x => x.UserId);
    }
}
