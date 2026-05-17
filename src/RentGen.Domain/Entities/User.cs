using RentGen.Domain.Abstractions;

namespace RentGen.Domain.Entities;

public sealed class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }

    public Subscription? Subscription { get; set; }
    public ICollection<DocumentDraft> Drafts { get; set; } = new List<DocumentDraft>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
