using Microsoft.AspNetCore.Mvc;
using RentGen.Domain.Enums;

namespace RentGen.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        return Ok(new
        {
            userId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            email = "demo@rentgen.local",
            plan = SubscriptionPlan.Free
        });
    }
}
