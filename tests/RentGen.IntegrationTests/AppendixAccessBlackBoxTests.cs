using System.Net;
using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class AppendixAccessBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public AppendixAccessBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");
    }

    [Fact]
    public async Task FreePlan_ShouldRejectAppendixCreation()
    {
        var draft = await PostAsync<CreateDraftPayload>("/api/drafts", new { documentType = 1 });
        await ApiTestHelper.FillBaseRentalAnswersAsync(_client, draft.DraftId);

        var generation = await PostAsync<GenerateDocumentPayload>(
            $"/api/drafts/{draft.DraftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });

        using var response = await _client.PostAsJsonAsync(
            $"/api/documents/{generation.DocumentId}/appendices",
            new { appendixType = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("feature_access_denied", body);
    }
    private async Task<T> PostAsync<T>(string url, object payload)
    {
        using var response = await _client.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>();
        return result!;
    }

    private sealed class CreateDraftPayload
    {
        public Guid DraftId { get; set; }
    }

    private sealed class GenerateDocumentPayload
    {
        public Guid DocumentId { get; set; }
    }
}
