namespace RentGen.Application.Documents.DTOs;

public sealed class DocumentPreviewImageResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/png";
    public byte[] Content { get; set; } = [];
}
