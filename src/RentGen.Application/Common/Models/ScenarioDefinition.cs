using RentGen.Domain.Enums;

namespace RentGen.Application.Common.Models;

public sealed class ScenarioDefinition
{
    public required string Version { get; init; }
    public required DocumentType DocumentType { get; init; }
    public List<ScenarioStep> Steps { get; set; } = new();
}
