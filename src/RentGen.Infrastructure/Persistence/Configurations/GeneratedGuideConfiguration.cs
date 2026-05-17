using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentGen.Domain.Entities;

namespace RentGen.Infrastructure.Persistence.Configurations;

public sealed class GeneratedGuideConfiguration : IEntityTypeConfiguration<GeneratedGuide>
{
    public void Configure(EntityTypeBuilder<GeneratedGuide> builder)
    {
        builder.ToTable("generated_guides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.HasIndex(x => x.DocumentId).IsUnique();
        builder.HasOne(x => x.Document)
            .WithOne(x => x.Guide)
            .HasForeignKey<GeneratedGuide>(x => x.DocumentId);
    }
}
