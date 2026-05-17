using System.Text.Json;
using RentGen.Application.Common.Models;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Features;
using RentGen.Infrastructure.Scenarios;

namespace RentGen.UnitTests;

public class ScenarioStepResolverTests
{
    private readonly FeatureAccessService _featureAccessService = new();

    [Fact]
    public void GetVisibleSteps_ShouldHidePremiumStepsForFreePlan()
    {
        var scenario = BuildScenario();

        var visibleSteps = ScenarioStepResolver.GetVisibleSteps(
            scenario,
            SubscriptionPlan.Free,
            _featureAccessService);

        Assert.DoesNotContain(visibleSteps, x => x.Key == "premium_clause");
        Assert.DoesNotContain(visibleSteps, x => x.Key == "dependent_step");
    }

    [Fact]
    public void GetVisibleSteps_ShouldShowDependentStep_WhenConditionMatches()
    {
        var scenario = BuildScenario();
        var answers = new Dictionary<string, JsonElement>
        {
            ["premium_clause"] = JsonDocument.Parse("true").RootElement.Clone()
        };

        var visibleSteps = ScenarioStepResolver.GetVisibleSteps(
            scenario,
            SubscriptionPlan.Premium,
            _featureAccessService,
            answers);

        Assert.Contains(visibleSteps, x => x.Key == "premium_clause");
        Assert.Contains(visibleSteps, x => x.Key == "dependent_step");
    }

    private static ScenarioDefinition BuildScenario()
    {
        return new ScenarioDefinition
        {
            Version = "test.v1",
            DocumentType = DocumentType.RentalAgreement,
            Steps =
            [
                new ScenarioStep
                {
                    Key = "base_step",
                    Title = "Base",
                    QuestionText = "Base",
                    InputType = "text",
                    Required = true
                },
                new ScenarioStep
                {
                    Key = "premium_clause",
                    Title = "Premium",
                    QuestionText = "Premium",
                    InputType = "boolean",
                    FeatureCode = FeatureCode.ExtendedRentalSections
                },
                new ScenarioStep
                {
                    Key = "dependent_step",
                    Title = "Dependent",
                    QuestionText = "Dependent",
                    InputType = "text",
                    FeatureCode = FeatureCode.ExtendedRentalSections,
                    DependsOn = new Dictionary<string, string>
                    {
                        ["premium_clause"] = "true"
                    }
                }
            ]
        };
    }
}
