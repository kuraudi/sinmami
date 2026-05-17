namespace RentGen.Application.Common.Models;

public sealed record AiCompletionRequest(
    string TaskType,
    string SystemPrompt,
    string UserPrompt,
    double Temperature = 0.2,
    int MaxTokens = 3000);
