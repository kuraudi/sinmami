using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentGen.Application.Common.Interfaces;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Persistence;

namespace RentGen.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = currentUser.GetUserId();

        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return Ok(new { userId = (Guid?)null, email = (string?)null, plan = SubscriptionPlan.Free, isAuthenticated = false });

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName,
            plan = user.Subscription?.Plan ?? SubscriptionPlan.Free,
            isAuthenticated = true,
        });
    }
}
