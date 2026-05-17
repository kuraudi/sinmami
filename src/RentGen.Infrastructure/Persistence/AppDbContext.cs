using Microsoft.EntityFrameworkCore;
using RentGen.Domain.Entities;

namespace RentGen.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<DocumentDraft> DocumentDrafts => Set<DocumentDraft>();
    public DbSet<DraftMessage> DraftMessages => Set<DraftMessage>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AppendixDocument> AppendixDocuments => Set<AppendixDocument>();
    public DbSet<GeneratedGuide> GeneratedGuides => Set<GeneratedGuide>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
