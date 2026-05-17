using RentGen.Application.Common.Models;

namespace RentGen.Application.Common.Interfaces;

public interface IStepAnswerInterpreter
{
    Task<StepAnswerInterpretationResult> InterpretAsync(
        ScenarioStep step,
        object? rawValue,
        IReadOnlyDictionary<string, System.Text.Json.JsonElement> answers,
        CancellationToken cancellationToken);
}
