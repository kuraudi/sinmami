using System.Text.Json;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Domain.Enums;

namespace RentGen.Infrastructure.Scenarios;

public sealed class JsonScenarioProvider : IScenarioProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ScenarioDefinition> GetScenarioAsync(DocumentType documentType, CancellationToken cancellationToken)
    {
        var fileName = documentType switch
        {
            DocumentType.RentalAgreement => "rental-agreement.v1.json",
            _ => throw new NotSupportedException($"Scenario for {documentType} is not supported.")
        };

        var fullPath = ResolveScenarioPath(fileName);
        await using var stream = File.OpenRead(fullPath);
        var scenario = await JsonSerializer.DeserializeAsync<ScenarioDefinition>(stream, SerializerOptions, cancellationToken);

        return scenario ?? throw new InvalidOperationException("Failed to deserialize scenario definition.");
    }

    private static string ResolveScenarioPath(string fileName)
    {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Scenarios", fileName);
        if (File.Exists(runtimePath))
        {
            return runtimePath;
        }

        var sourcePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "RentGen.Infrastructure",
            "Scenarios",
            fileName));

        if (File.Exists(sourcePath))
        {
            return sourcePath;
        }

        throw new FileNotFoundException($"Scenario file '{fileName}' was not found.", runtimePath);
    }
}
