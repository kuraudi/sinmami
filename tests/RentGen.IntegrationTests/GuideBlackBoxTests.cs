using System.Text;

namespace RentGen.IntegrationTests;

public class GuideBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public GuideBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");
    }

    [Fact]
    public async Task GuideEndpoint_ShouldBeIdempotent_ForGeneratedDocument()
    {
        var documentId = await ApiTestHelper.CreateValidFreeDocumentAsync(_client);

        var firstGuide = await ApiTestHelper.GetAsync<GuidePayload>(_client, $"/api/documents/{documentId}/guide");
        var secondGuide = await ApiTestHelper.GetAsync<GuidePayload>(_client, $"/api/documents/{documentId}/guide");

        Assert.Equal(firstGuide.GuideType, secondGuide.GuideType);
        Assert.Equal(firstGuide.Content, secondGuide.Content);
    }

    [Fact]
    public async Task FreeGuide_ShouldContainReadableRussianText()
    {
        var documentId = await ApiTestHelper.CreateValidFreeDocumentAsync(_client);
        var guide = await ApiTestHelper.GetAsync<GuidePayload>(_client, $"/api/documents/{documentId}/guide");

        Assert.Contains("\u041f\u0440\u043e\u0432\u0435\u0440\u044c\u0442\u0435", guide.Content);
    }

    [Fact]
    public async Task GuidePdfEndpoint_ShouldReturnDownloadablePdf()
    {
        var documentId = await ApiTestHelper.CreateValidFreeDocumentAsync(_client);

        using var response = await _client.GetAsync($"/api/documents/{documentId}/guide/pdf");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Content.Headers.ContentDisposition);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var signature = Encoding.ASCII.GetString(bytes.Take(5).ToArray());

        Assert.Equal("%PDF-", signature);
    }

    private sealed class GuidePayload
    {
        public int GuideType { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
