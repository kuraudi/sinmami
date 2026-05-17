using RentGen.Domain.Enums;

namespace RentGen.Application.Appendices.DTOs;

public sealed class AppendixFlowResponse
{
    public Guid DocumentId { get; set; }
    public AppendixType AppendixType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<AppendixFlowStepResponse> Steps { get; set; } = new();
}
