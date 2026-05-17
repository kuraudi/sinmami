using Microsoft.AspNetCore.Http;
using RentGen.Application.Common.Interfaces;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Services;

namespace RentGen.Api.Services;

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid GetUserId() => DemoCurrentUserDefaults.UserId;

    public SubscriptionPlan GetCurrentPlan()
    {
        var rawValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Plan"].ToString();
        return Enum.TryParse<SubscriptionPlan>(rawValue, true, out var plan)
            ? plan
            : SubscriptionPlan.Free;
    }
}
