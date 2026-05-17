namespace RentGen.Application.Common.Models;

public sealed record ValidationIssue(
    string Code,
    string Message,
    string? Field = null,
    string? StepKey = null);
