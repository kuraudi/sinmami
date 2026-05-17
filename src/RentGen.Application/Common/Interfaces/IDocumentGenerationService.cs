using RentGen.Application.Documents.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IDocumentGenerationService
{
    Task<GenerateDocumentResponse> GenerateAsync(Guid draftId, GenerateDocumentRequest request, CancellationToken cancellationToken);
}
