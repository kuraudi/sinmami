using RentGen.Domain.Enums;

namespace RentGen.Application.Common.Models;

public sealed class ScenarioStep
{
    public string Section { get; init; } = string.Empty;
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string QuestionText { get; init; }
    public required string InputType { get; init; }
    public bool Required { get; init; }
    public string? Placeholder { get; init; }
    public string? HelpText { get; init; }
    public string? MapsTo { get; init; }
    public FeatureCode? FeatureCode { get; init; }
    public Dictionary<string, string>? DependsOn { get; init; }
    public List<string> Options { get; init; } = new();
}
