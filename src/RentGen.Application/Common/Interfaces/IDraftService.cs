using RentGen.Application.Drafts.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IDraftService
{
    Task<CreateDraftResponse> CreateAsync(CreateDraftRequest request, CancellationToken cancellationToken);
    Task<DraftDetailsResponse?> GetAsync(Guid draftId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftStepItemResponse>> GetStepsAsync(Guid draftId, CancellationToken cancellationToken);
    Task<SaveAnswerResponse> SaveAnswerAsync(Guid draftId, SaveAnswerRequest request, CancellationToken cancellationToken);
    Task<NextQuestionResponse?> GetNextQuestionAsync(Guid draftId, CancellationToken cancellationToken);
}
