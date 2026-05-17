using RentGen.Domain.Enums;

namespace RentGen.Application.Common.Interfaces;

public interface IStandardGuideProvider
{
    string GetGuide(DocumentType documentType);
}
