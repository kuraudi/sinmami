namespace RentGen.Application.Common.Models;

public sealed record AiCompletionResult(
    string Content,
    string? Model = null,
    string? ProviderRequestId = null);
