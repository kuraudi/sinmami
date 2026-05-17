using Microsoft.AspNetCore.Mvc;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Documents.DTOs;
using RentGen.Application.Guides.DTOs;

namespace RentGen.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(
    IDocumentQueryService documentQueryService,
    IGuideService guideService,
    IAppendixService appendixService) : ControllerBase
{
    private readonly IDocumentQueryService _documentQueryService = documentQueryService;
    private readonly IGuideService _guideService = guideService;
    private readonly IAppendixService _appendixService = appendixService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DocumentListItemResponse>>> GetDocuments(CancellationToken cancellationToken)
    {
        return Ok(await _documentQueryService.GetListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDetailsResponse>> GetDocument(Guid id, CancellationToken cancellationToken)
    {
        var response = await _documentQueryService.GetDetailsAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/guide")]
    public async Task<ActionResult<GuideResponse>> GetGuide(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _guideService.GetGuideAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/guide/pdf")]
    public async Task<IActionResult> DownloadGuidePdf(
        Guid id,
        [FromServices] IGuidePdfService guidePdfService,
        CancellationToken cancellationToken)
    {
        var pdf = await guidePdfService.GetGuidePdfAsync(id, cancellationToken);
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadDocumentPdf(
        Guid id,
        [FromServices] IDocumentPdfService documentPdfService,
        CancellationToken cancellationToken)
    {
        var pdf = await documentPdfService.GetDocumentPdfAsync(id, cancellationToken);
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }

    [HttpGet("{id:guid}/pdf/inline")]
    public async Task<IActionResult> OpenDocumentPdf(
        Guid id,
        [FromServices] IDocumentPdfService documentPdfService,
        CancellationToken cancellationToken)
    {
        var pdf = await documentPdfService.GetDocumentPdfAsync(id, cancellationToken);
        return File(pdf.Content, pdf.ContentType);
    }

    [HttpGet("{id:guid}/pdf/preview")]
    public async Task<IActionResult> GetDocumentPdfPreview(
        Guid id,
        [FromServices] IDocumentPdfService documentPdfService,
        CancellationToken cancellationToken)
    {
        var image = await documentPdfService.GetDocumentPreviewImageAsync(id, cancellationToken);
        return File(image.Content, image.ContentType, image.FileName);
    }

    [HttpGet("{id:guid}/appendices")]
    public async Task<IActionResult> GetAppendices(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _appendixService.GetByDocumentAsync(id, cancellationToken));
    }
}
