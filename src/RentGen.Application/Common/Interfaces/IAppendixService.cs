using RentGen.Application.Appendices.DTOs;
using RentGen.Application.Drafts.DTOs;
using RentGen.Domain.Enums;

namespace RentGen.Application.Common.Interfaces;

public interface IAppendixService
{
    Task<IReadOnlyCollection<AppendixSummaryResponse>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken);
    Task<AppendixFlowResponse> GetFlowAsync(Guid documentId, AppendixType appendixType, CancellationToken cancellationToken);
    Task<AppendixPreviewResponse> PreviewAsync(Guid documentId, AppendixPreviewRequest request, CancellationToken cancellationToken);
    Task<AskAiResponse> AskAsync(Guid documentId, AskAppendixAiRequest request, CancellationToken cancellationToken);
    Task<CreateAppendixResponse> CreateAsync(Guid documentId, CreateAppendixRequest request, CancellationToken cancellationToken);
    Task<AppendixDetailsResponse?> GetAsync(Guid appendixId, CancellationToken cancellationToken);
}
