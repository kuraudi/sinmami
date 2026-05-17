using RentGen.Domain.Enums;
using RentGen.Infrastructure.Features;

namespace RentGen.UnitTests;

public class FeatureAccessServiceTests
{
    private readonly FeatureAccessService _service = new();

    [Fact]
    public void FreePlan_ShouldNotExposePremiumFeatures()
    {
        var featureSet = _service.GetFeatureSet(SubscriptionPlan.Free, DocumentType.RentalAgreement);

        Assert.Empty(featureSet.Features);
        Assert.False(_service.CanCreateAppendix(SubscriptionPlan.Free, AppendixType.HandoverAct));
        Assert.False(_service.HasFeature(SubscriptionPlan.Free, FeatureCode.ExtendedRentalSections));
    }

    [Fact]
    public void PremiumPlan_ShouldExposePremiumFeatures()
    {
        var featureSet = _service.GetFeatureSet(SubscriptionPlan.Premium, DocumentType.RentalAgreement);

        Assert.Contains(FeatureCode.ExtendedRentalSections, featureSet.Features);
        Assert.Contains(FeatureCode.PersonalizedGuide, featureSet.Features);
        Assert.Contains(FeatureCode.Appendices, featureSet.Features);
        Assert.True(_service.CanCreateAppendix(SubscriptionPlan.Premium, AppendixType.InventoryList));
    }
}
