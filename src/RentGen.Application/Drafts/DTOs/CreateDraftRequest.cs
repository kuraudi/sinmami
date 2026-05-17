using RentGen.Domain.Enums;

namespace RentGen.Application.Drafts.DTOs;

public sealed class CreateDraftRequest
{
    public DocumentType DocumentType { get; set; } = DocumentType.RentalAgreement;
}
