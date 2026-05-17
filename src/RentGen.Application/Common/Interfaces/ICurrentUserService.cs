using RentGen.Domain.Enums;

namespace RentGen.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid GetUserId();
    SubscriptionPlan GetCurrentPlan();
}
