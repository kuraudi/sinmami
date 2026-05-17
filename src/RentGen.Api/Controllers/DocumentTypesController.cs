using Microsoft.AspNetCore.Mvc;
using RentGen.Application.Common.Interfaces;
using RentGen.Domain.Enums;

namespace RentGen.Api.Controllers;

[ApiController]
[Route("api/document-types")]
public sealed class DocumentTypesController(
    IScenarioProvider scenarioProvider,
    ICurrentUserService currentUserService,
    IFeatureAccessService featureAccessService) : ControllerBase
{
    private readonly IScenarioProvider _scenarioProvider = scenarioProvider;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IFeatureAccessService _featureAccessService = featureAccessService;

    [HttpGet]
    public IActionResult GetTypes()
    {
        return Ok(new[]
        {
            new
            {
                type = DocumentType.RentalAgreement,
                title = "Rental agreement",
                supportsDialog = true
            }
        });
    }

    [HttpGet("{type}/scenario")]
    public async Task<IActionResult> GetScenario(DocumentType type, CancellationToken cancellationToken)
    {
        var scenario = await _scenarioProvider.GetScenarioAsync(type, cancellationToken);
        var plan = _currentUserService.GetCurrentPlan();
        scenario.Steps = scenario.Steps
            .Where(step => step.FeatureCode is null || _featureAccessService.HasFeature(plan, step.FeatureCode.Value))
            .ToList();

        return Ok(scenario);
    }
}
