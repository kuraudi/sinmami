using RentGen.Application.Documents.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IDocumentQueryService
{
    Task<IReadOnlyCollection<DocumentListItemResponse>> GetListAsync(CancellationToken cancellationToken);
    Task<DocumentDetailsResponse?> GetDetailsAsync(Guid documentId, CancellationToken cancellationToken);
}
