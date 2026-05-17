using System.Text.Json;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Domain.Enums;

namespace RentGen.Infrastructure.Scenarios;

internal static class ScenarioStepResolver
{
    public static List<ScenarioStep> GetVisibleSteps(
        ScenarioDefinition scenario,
        SubscriptionPlan plan,
        IFeatureAccessService featureAccessService,
        IReadOnlyDictionary<string, JsonElement>? answers = null)
    {
        return scenario.Steps
            .Where(step => step.FeatureCode is null || featureAccessService.HasFeature(plan, step.FeatureCode.Value))
            .Where(step => AreDependenciesMet(step, answers))
            .ToList();
    }

    public static bool AreDependenciesMet(ScenarioStep step, IReadOnlyDictionary<string, JsonElement>? answers)
    {
        if (step.DependsOn is null || step.DependsOn.Count == 0)
        {
            return true;
        }

        if (answers is null)
        {
            return false;
        }

        foreach (var dependency in step.DependsOn)
        {
            if (!answers.TryGetValue(dependency.Key, out var actualValue))
            {
                return false;
            }

            var normalizedActual = NormalizeValue(actualValue);
            if (!string.Equals(normalizedActual, dependency.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            JsonValueKind.Number => value.ToString(),
            _ => value.ToString()
        };
    }
}
