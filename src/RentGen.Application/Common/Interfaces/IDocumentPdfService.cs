using RentGen.Application.Documents.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IDocumentPdfService
{
    Task<DocumentPdfResult> GetDocumentPdfAsync(Guid documentId, CancellationToken cancellationToken);
    Task<DocumentPreviewImageResult> GetDocumentPreviewImageAsync(Guid documentId, CancellationToken cancellationToken);
}
