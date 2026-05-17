namespace RentGen.Application.Appendices.DTOs;

public sealed class AppendixPdfResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = [];
}
