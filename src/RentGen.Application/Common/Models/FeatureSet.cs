using RentGen.Domain.Enums;

namespace RentGen.Application.Common.Models;

public sealed record FeatureSet(
    SubscriptionPlan Plan,
    IReadOnlyCollection<FeatureCode> Features);
