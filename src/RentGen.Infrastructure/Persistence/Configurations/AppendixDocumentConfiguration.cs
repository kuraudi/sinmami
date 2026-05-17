using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentGen.Domain.Entities;

namespace RentGen.Infrastructure.Persistence.Configurations;

public sealed class AppendixDocumentConfiguration : IEntityTypeConfiguration<AppendixDocument>
{
    public void Configure(EntityTypeBuilder<AppendixDocument> builder)
    {
        builder.ToTable("appendix_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.StructuredDataJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.ParentDocumentId);
        builder.HasOne(x => x.ParentDocument)
            .WithMany(x => x.Appendices)
            .HasForeignKey(x => x.ParentDocumentId);
    }
}
