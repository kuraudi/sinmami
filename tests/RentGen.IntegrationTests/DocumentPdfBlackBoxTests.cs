using System.Text;

namespace RentGen.IntegrationTests;

public class DocumentPdfBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public DocumentPdfBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");
    }

    [Fact]
    public async Task DocumentPdfEndpoint_ShouldReturnDownloadablePdf()
    {
        var documentId = await ApiTestHelper.CreateValidFreeDocumentAsync(_client);

        using var response = await _client.GetAsync($"/api/documents/{documentId}/pdf");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Content.Headers.ContentDisposition);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var signature = Encoding.ASCII.GetString(bytes.Take(5).ToArray());

        Assert.Equal("%PDF-", signature);
    }

    [Fact]
    public async Task DocumentPdfPreviewEndpoint_ShouldReturnPngImage()
    {
        var documentId = await ApiTestHelper.CreateValidFreeDocumentAsync(_client);

        using var response = await _client.GetAsync($"/api/documents/{documentId}/pdf/preview");
        response.EnsureSuccessStatusCode();

        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 8);
        Assert.Equal("89-50-4E-47", BitConverter.ToString(bytes.Take(4).ToArray()));
    }
}
