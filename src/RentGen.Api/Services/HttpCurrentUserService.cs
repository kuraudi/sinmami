using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RentGen.Application.Common.Interfaces;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Services;

namespace RentGen.Api.Services;

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid GetUserId()
    {
        var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

        return Guid.TryParse(sub, out var id) ? id : DemoCurrentUserDefaults.UserId;
    }

    public SubscriptionPlan GetCurrentPlan()
    {
        // Try JWT claim first
        var planClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue("plan");
        if (!string.IsNullOrEmpty(planClaim) && Enum.TryParse<SubscriptionPlan>(planClaim, true, out var jwtPlan))
            return jwtPlan;

        // Fallback to X-Plan header
        var rawValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Plan"].ToString();
        return Enum.TryParse<SubscriptionPlan>(rawValue, true, out var plan)
            ? plan
            : SubscriptionPlan.Free;
    }
}
