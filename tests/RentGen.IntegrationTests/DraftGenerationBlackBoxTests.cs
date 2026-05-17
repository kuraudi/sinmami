using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class DraftGenerationBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public DraftGenerationBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Plan", "Premium");
    }

    [Fact]
    public async Task DraftFlow_ShouldGenerateDocument_AndGuide()
    {
        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.FillBaseRentalAnswersAsync(_client, draftId);
        await ApiTestHelper.FillPremiumStandardAnswersAsync(_client, draftId);

        var validation = await ApiTestHelper.PostAsync<ValidateDraftPayload>(_client, $"/api/drafts/{draftId}/validate", new { });
        var generation = await ApiTestHelper.PostAsync<ApiTestHelper.GenerateDocumentPayload>(
            _client,
            $"/api/drafts/{draftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });

        var details = await ApiTestHelper.GetAsync<DocumentDetailsPayload>(_client, $"/api/documents/{generation.DocumentId}");
        var guide = await ApiTestHelper.GetAsync<GuidePayload>(_client, $"/api/documents/{generation.DocumentId}/guide");

        Assert.True(validation.IsValid);
        Assert.Equal(3, generation.Status);
        Assert.False(string.IsNullOrWhiteSpace(details.Content));
        Assert.False(string.IsNullOrWhiteSpace(guide.Content));
        Assert.Equal(2, guide.GuideType);
    }

    private sealed class DocumentDetailsPayload
    {
        public string Content { get; set; } = string.Empty;
    }

    private sealed class GuidePayload
    {
        public int GuideType { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private sealed class CreateDraftPayload
    {
        public Guid DraftId { get; set; }
    }

    private sealed class ValidateDraftPayload
    {
        public bool IsValid { get; set; }
    }
}
