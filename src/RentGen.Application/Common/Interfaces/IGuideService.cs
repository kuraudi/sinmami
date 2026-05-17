using RentGen.Application.Guides.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IGuideService
{
    Task<GuideResponse> GetGuideAsync(Guid documentId, CancellationToken cancellationToken);
}
