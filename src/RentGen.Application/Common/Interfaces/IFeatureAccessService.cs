using RentGen.Application.Common.Models;
using RentGen.Domain.Enums;

namespace RentGen.Application.Common.Interfaces;

public interface IFeatureAccessService
{
    FeatureSet GetFeatureSet(SubscriptionPlan plan, DocumentType documentType);
    bool HasFeature(SubscriptionPlan plan, FeatureCode featureCode);
    bool CanCreateAppendix(SubscriptionPlan plan, AppendixType appendixType);
}
