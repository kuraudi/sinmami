using RentGen.Application.Common.Models;
using RentGen.Domain.Enums;

namespace RentGen.Application.Drafts.DTOs;

public sealed class ValidateDraftResponse
{
    public bool IsValid { get; set; }
    public DraftStatus Status { get; set; }
    public int CompletionPercent { get; set; }
    public List<ValidationIssue> Errors { get; set; } = new();
    public List<string> MissingSteps { get; set; } = new();
}
