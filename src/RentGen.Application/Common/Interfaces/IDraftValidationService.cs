using RentGen.Application.Drafts.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IDraftValidationService
{
    Task<ValidateDraftResponse> ValidateAsync(Guid draftId, CancellationToken cancellationToken);
}
