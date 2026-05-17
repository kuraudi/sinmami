using RentGen.Domain.Enums;

namespace RentGen.Application.Appendices.DTOs;

public sealed class CreateAppendixResponse
{
    public Guid AppendixId { get; set; }
    public AppendixType AppendixType { get; set; }
    public AppendixStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
}
