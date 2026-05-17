namespace RentGen.Application.Common.Models;

public sealed record PromptBuildResult(
    string SystemPrompt,
    string UserPrompt);
