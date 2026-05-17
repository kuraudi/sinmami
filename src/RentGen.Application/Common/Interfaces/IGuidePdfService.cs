using RentGen.Application.Guides.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IGuidePdfService
{
    Task<GuidePdfResult> GetGuidePdfAsync(Guid documentId, CancellationToken cancellationToken);
}
