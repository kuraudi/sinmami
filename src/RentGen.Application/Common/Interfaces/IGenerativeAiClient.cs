using RentGen.Application.Common.Models;

namespace RentGen.Application.Common.Interfaces;

public interface IGenerativeAiClient
{
    Task<AiCompletionResult> GenerateAsync(AiCompletionRequest request, CancellationToken cancellationToken);
}
