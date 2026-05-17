namespace RentGen.Application.Documents.DTOs;

public sealed class DocumentPdfResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = [];
}
