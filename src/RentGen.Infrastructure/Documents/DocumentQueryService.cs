using Microsoft.EntityFrameworkCore;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Appendices.DTOs;
using RentGen.Application.Documents.DTOs;
using RentGen.Application.Guides.DTOs;
using RentGen.Infrastructure.Persistence;

namespace RentGen.Infrastructure.Documents;

public sealed class DocumentQueryService(
    AppDbContext dbContext,
    ICurrentUserService currentUserService) : IDocumentQueryService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<IReadOnlyCollection<DocumentListItemResponse>> GetListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Documents
            .AsNoTracking()
            .Where(x => x.UserId == _currentUserService.GetUserId())
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DocumentListItemResponse
            {
                DocumentId = x.Id,
                Title = x.Title,
                DocumentType = x.DocumentType,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc,
                HasGuide = x.Guide != null,
                AppendicesCount = x.Appendices.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentDetailsResponse?> GetDetailsAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .Include(x => x.Guide)
            .Include(x => x.Appendices)
            .FirstOrDefaultAsync(x => x.Id == documentId && x.UserId == _currentUserService.GetUserId(), cancellationToken);

        if (document is null)
        {
            return null;
        }

        return new DocumentDetailsResponse
        {
            DocumentId = document.Id,
            DraftId = document.DraftId,
            DocumentType = document.DocumentType,
            Status = document.Status,
            Title = document.Title,
            Content = document.Content,
            Plan = document.PlanSnapshot,
            GeneratedAtUtc = document.GeneratedAtUtc,
            Guide = document.Guide is null
                ? null
                : new GuideResponse
                {
                    DocumentId = document.Id,
                    GuideType = document.Guide.GuideType,
                    Content = document.Guide.Content
                },
            Appendices = document.Appendices.Select(x => new AppendixSummaryResponse
            {
                AppendixId = x.Id,
                AppendixType = x.AppendixType,
                Status = x.Status,
                Title = x.Title
            }).ToList()
        };
    }
}
