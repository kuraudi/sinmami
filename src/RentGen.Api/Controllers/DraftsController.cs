using Microsoft.AspNetCore.Mvc;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Drafts.DTOs;
using RentGen.Application.Documents.DTOs;

namespace RentGen.Api.Controllers;

[ApiController]
[Route("api/drafts")]
public sealed class DraftsController(
    IDraftService draftService,
    IDraftValidationService draftValidationService,
    IDocumentGenerationService documentGenerationService,
    IAiHelpService aiHelpService) : ControllerBase
{
    private readonly IDraftService _draftService = draftService;
    private readonly IDraftValidationService _draftValidationService = draftValidationService;
    private readonly IDocumentGenerationService _documentGenerationService = documentGenerationService;
    private readonly IAiHelpService _aiHelpService = aiHelpService;

    [HttpPost]
    public async Task<ActionResult<CreateDraftResponse>> Create([FromBody] CreateDraftRequest request, CancellationToken cancellationToken)
    {
        var response = await _draftService.CreateAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DraftDetailsResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await _draftService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/steps")]
    public async Task<ActionResult<IReadOnlyCollection<DraftStepItemResponse>>> GetSteps(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _draftService.GetStepsAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/answers")]
    public async Task<ActionResult<SaveAnswerResponse>> SaveAnswer(Guid id, [FromBody] SaveAnswerRequest request, CancellationToken cancellationToken)
    {
        var response = await _draftService.SaveAnswerAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/next-question")]
    public async Task<ActionResult<NextQuestionResponse>> GetNextQuestion(Guid id, CancellationToken cancellationToken)
    {
        var response = await _draftService.GetNextQuestionAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/validate")]
    public async Task<ActionResult<ValidateDraftResponse>> Validate(Guid id, CancellationToken cancellationToken)
    {
        var response = await _draftValidationService.ValidateAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/generate")]
    public async Task<ActionResult<GenerateDocumentResponse>> Generate(Guid id, [FromBody] GenerateDocumentRequest request, CancellationToken cancellationToken)
    {
        var response = await _documentGenerationService.GenerateAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/ask")]
    public async Task<ActionResult<AskAiResponse>> Ask(Guid id, [FromBody] AskAiRequest request, CancellationToken cancellationToken)
    {
        var response = await _aiHelpService.AskAsync(id, request, cancellationToken);
        return Ok(response);
    }
}
