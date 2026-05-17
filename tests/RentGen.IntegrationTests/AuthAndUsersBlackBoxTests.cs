using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class AuthAndUsersBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public AuthAndUsersBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnTokenAndFreePlan()
    {
        var payload = await ApiTestHelper.PostAsync<AuthPayload>(_client, "/api/auth/register", new
        {
            email = "user@example.com",
            password = "password123"
        });

        Assert.Equal("user@example.com", payload.Email);
        Assert.Equal("mvp-token", payload.Token);
        Assert.Equal(1, payload.Plan);
    }

    [Fact]
    public async Task Login_ShouldReturnToken()
    {
        var payload = await ApiTestHelper.PostAsync<AuthPayload>(_client, "/api/auth/login", new
        {
            email = "user@example.com",
            password = "password123"
        });

        Assert.Equal("user@example.com", payload.Email);
        Assert.Equal("mvp-token", payload.Token);
    }

    [Fact]
    public async Task UsersMe_ShouldReturnDemoUser()
    {
        var payload = await _client.GetFromJsonAsync<UserMePayload>("/api/users/me");

        Assert.NotNull(payload);
        Assert.Equal("demo@rentgen.local", payload!.Email);
        Assert.Equal(1, payload.Plan);
    }

    private sealed class AuthPayload
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int Plan { get; set; }
    }

    private sealed class UserMePayload
    {
        public string Email { get; set; } = string.Empty;
        public int Plan { get; set; }
    }
}
