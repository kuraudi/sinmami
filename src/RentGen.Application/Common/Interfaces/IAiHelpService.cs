using RentGen.Application.Drafts.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IAiHelpService
{
    Task<AskAiResponse> AskAsync(Guid draftId, AskAiRequest request, CancellationToken cancellationToken);
}
