using RentGen.Application.Common.Models;
using RentGen.Domain.Enums;

namespace RentGen.Application.Common.Interfaces;

public interface IScenarioProvider
{
    Task<ScenarioDefinition> GetScenarioAsync(DocumentType documentType, CancellationToken cancellationToken);
}
