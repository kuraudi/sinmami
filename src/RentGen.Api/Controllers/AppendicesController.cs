using Microsoft.AspNetCore.Mvc;
using RentGen.Application.Appendices.DTOs;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Drafts.DTOs;
using RentGen.Domain.Enums;

namespace RentGen.Api.Controllers;

[ApiController]
public sealed class AppendicesController(IAppendixService appendixService) : ControllerBase
{
    private readonly IAppendixService _appendixService = appendixService;

    [HttpGet("api/documents/{documentId:guid}/appendices/flow/{appendixType:int}")]
    public async Task<ActionResult<AppendixFlowResponse>> GetFlow(
        Guid documentId,
        AppendixType appendixType,
        CancellationToken cancellationToken)
    {
        return Ok(await _appendixService.GetFlowAsync(documentId, appendixType, cancellationToken));
    }

    [HttpPost("api/documents/{documentId:guid}/appendices/preview")]
    public async Task<ActionResult<AppendixPreviewResponse>> Preview(
        Guid documentId,
        [FromBody] AppendixPreviewRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _appendixService.PreviewAsync(documentId, request, cancellationToken));
    }

    [HttpPost("api/documents/{documentId:guid}/appendices/ask")]
    public async Task<ActionResult<AskAiResponse>> Ask(
        Guid documentId,
        [FromBody] AskAppendixAiRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _appendixService.AskAsync(documentId, request, cancellationToken));
    }

    [HttpPost("api/documents/{documentId:guid}/appendices/preview/pdf")]
    public async Task<IActionResult> PreviewPdf(
        Guid documentId,
        [FromBody] AppendixPreviewRequest request,
        [FromServices] IAppendixPdfService appendixPdfService,
        CancellationToken cancellationToken)
    {
        var preview = await _appendixService.PreviewAsync(documentId, request, cancellationToken);
        var pdf = await appendixPdfService.GetPreviewPdfAsync(preview, cancellationToken);
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }

    [HttpPost("api/documents/{documentId:guid}/appendices")]
    public async Task<ActionResult<CreateAppendixResponse>> Create(Guid documentId, [FromBody] CreateAppendixRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _appendixService.CreateAsync(documentId, request, cancellationToken));
    }

    [HttpGet("api/appendices/{id:guid}")]
    public async Task<ActionResult<AppendixDetailsResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await _appendixService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("api/appendices/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] IAppendixPdfService appendixPdfService,
        CancellationToken cancellationToken)
    {
        var pdf = await appendixPdfService.GetAppendixPdfAsync(id, cancellationToken);
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }

    [HttpGet("api/appendices/{id:guid}/pdf/inline")]
    public async Task<IActionResult> OpenPdf(
        Guid id,
        [FromServices] IAppendixPdfService appendixPdfService,
        CancellationToken cancellationToken)
    {
        var pdf = await appendixPdfService.GetAppendixPdfAsync(id, cancellationToken);
        return File(pdf.Content, pdf.ContentType);
    }
}
