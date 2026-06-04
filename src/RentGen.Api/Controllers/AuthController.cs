using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentGen.Application.Auth.DTOs;
using RentGen.Domain.Entities;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Persistence;
using RentGen.Api.Services;

namespace RentGen.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext db, JwtService jwtService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            return Conflict(new { message = "Пользователь с таким email уже существует." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Active,
            StartedAtUtc = DateTime.UtcNow,
        };

        db.Users.Add(user);
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        user.Subscription = subscription;
        var token = jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Plan = SubscriptionPlan.Free,
            Token = token,
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Неверный email или пароль." });

        var token = jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Plan = user.Subscription?.Plan ?? SubscriptionPlan.Free,
            Token = token,
        });
    }
}
