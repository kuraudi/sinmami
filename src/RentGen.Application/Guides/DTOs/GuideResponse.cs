using RentGen.Domain.Enums;

namespace RentGen.Application.Guides.DTOs;

public sealed class GuideResponse
{
    public Guid DocumentId { get; set; }
    public GuideType GuideType { get; set; }
    public string Content { get; set; } = string.Empty;
}
