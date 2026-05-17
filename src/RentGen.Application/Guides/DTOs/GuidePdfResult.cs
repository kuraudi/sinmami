namespace RentGen.Application.Guides.DTOs;

public sealed class GuidePdfResult
{
    public string FileName { get; set; } = "rentgen-guide.pdf";
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = [];
}
