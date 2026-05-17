using RentGen.Application.Appendices.DTOs;

namespace RentGen.Application.Common.Interfaces;

public interface IAppendixPdfService
{
    Task<AppendixPdfResult> GetAppendixPdfAsync(Guid appendixId, CancellationToken cancellationToken);
    Task<AppendixPdfResult> GetPreviewPdfAsync(AppendixPreviewResponse preview, CancellationToken cancellationToken);
}
