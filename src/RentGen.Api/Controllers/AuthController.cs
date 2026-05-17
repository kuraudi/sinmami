using Microsoft.AspNetCore.Mvc;
using RentGen.Application.Auth.DTOs;
using RentGen.Domain.Enums;

namespace RentGen.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    public ActionResult<AuthResponse> Register([FromBody] RegisterRequest request)
    {
        return Ok(new AuthResponse
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            Plan = SubscriptionPlan.Free,
            Token = "mvp-token"
        });
    }

    [HttpPost("login")]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest request)
    {
        return Ok(new AuthResponse
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = request.Email,
            Plan = SubscriptionPlan.Free,
            Token = "mvp-token"
        });
    }
}
