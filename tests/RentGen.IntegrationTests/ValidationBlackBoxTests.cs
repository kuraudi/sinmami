using System.Net;
using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class ValidationBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public ValidationBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDraft_WithInvalidDocumentType_ShouldReturnBadRequest()
    {
        using var response = await _client.PostAsJsonAsync("/api/drafts", new { documentType = 999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SaveAnswer_WithEmptyStepKey_ShouldReturnBadRequest()
    {
        var draftId = await ApiTestHelper.CreateDraftAsync(_client);

        using var response = await _client.PostAsJsonAsync($"/api/drafts/{draftId}/answers", new
        {
            stepKey = "",
            value = "test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
