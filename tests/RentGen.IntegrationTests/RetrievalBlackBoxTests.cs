using System.Net;

namespace RentGen.IntegrationTests;

public class RetrievalBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public RetrievalBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMissingDraft_ShouldReturnNotFound()
    {
        using var response = await _client.GetAsync($"/api/drafts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
