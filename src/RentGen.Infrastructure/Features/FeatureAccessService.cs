using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Domain.Enums;

namespace RentGen.Infrastructure.Features;

public sealed class FeatureAccessService : IFeatureAccessService
{
    public FeatureSet GetFeatureSet(SubscriptionPlan plan, DocumentType documentType)
    {
        var features = plan == SubscriptionPlan.Premium
            ? new[] { FeatureCode.ExtendedRentalSections, FeatureCode.PersonalizedGuide, FeatureCode.Appendices }
            : Array.Empty<FeatureCode>();

        return new FeatureSet(plan, features);
    }

    public bool HasFeature(SubscriptionPlan plan, FeatureCode featureCode)
    {
        return plan == SubscriptionPlan.Premium;
    }

    public bool CanCreateAppendix(SubscriptionPlan plan, AppendixType appendixType)
    {
        return plan == SubscriptionPlan.Premium;
    }
}
