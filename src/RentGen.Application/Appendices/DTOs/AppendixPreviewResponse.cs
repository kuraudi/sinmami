using RentGen.Application.Common.Models;
using RentGen.Domain.Enums;

namespace RentGen.Application.Appendices.DTOs;

public sealed class AppendixPreviewResponse
{
    public Guid DocumentId { get; set; }
    public AppendixType AppendixType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<ValidationIssue> Errors { get; set; } = new();
    public List<string> MissingSteps { get; set; } = new();
    public Dictionary<string, object?> NormalizedAnswers { get; set; } = new();
}
