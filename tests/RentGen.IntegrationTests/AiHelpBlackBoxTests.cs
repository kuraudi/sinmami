using System.Net;
using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class AiHelpBlackBoxTests
{
    private readonly TestApiFactory _factory = new();

    [Fact]
    public async Task AskEndpoint_ShouldReturnFallbackHelp_WhenProviderIsUnavailable()
    {
        using var client = _factory.CreateClientWithOverrides(new Dictionary<string, string?>
        {
            ["DeepSeek:ApiKey"] = "test-key",
            ["DeepSeek:BaseUrl"] = "http://127.0.0.1:1",
            ["DeepSeek:MaxRetries"] = "0"
        });

        client.DefaultRequestHeaders.Add("X-Plan", "Premium");

        var draft = await PostAsync<CreateDraftPayload>(client, "/api/drafts", new { documentType = 1 });
        _ = await PostAsync<SaveAnswerPayload>(client, $"/api/drafts/{draft.DraftId}/answers", new
        {
            stepKey = "landlord_type",
            value = "физлицо"
        });

        using var response = await client.PostAsJsonAsync($"/api/drafts/{draft.DraftId}/ask", new
        {
            question = "Какая разница, физлицо или ИП?",
            stepKey = "landlord_type"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("физ", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ип", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object payload)
    {
        using var response = await client.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>();
        return result!;
    }

    private sealed class CreateDraftPayload
    {
        public Guid DraftId { get; set; }
    }

    private sealed class SaveAnswerPayload
    {
        public Guid DraftId { get; set; }
    }
}
