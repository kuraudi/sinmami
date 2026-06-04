using RentGen.Domain.Enums;

namespace RentGen.Application.Auth.DTOs;

public sealed class AuthResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public string Token { get; set; } = string.Empty;
}
