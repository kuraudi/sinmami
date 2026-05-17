using System.Net;
using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class DocumentsAndAppendicesBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public DocumentsAndAppendicesBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Plan", "Premium");
    }

    [Fact]
    public async Task DocumentsList_ShouldContainGeneratedDocument()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumDocumentAsync(_client);
        var documents = await ApiTestHelper.GetAsync<List<DocumentListPayload>>(_client, "/api/documents");

        Assert.Contains(documents, x => x.DocumentId == documentId);
    }

    [Fact]
    public async Task GetMissingDocument_ShouldReturnNotFound()
    {
        using var response = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMissingAppendix_ShouldReturnNotFound()
    {
        using var response = await _client.GetAsync($"/api/appendices/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSameAppendixTwice_ShouldUpdateExistingDocument()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumDocumentAsync(_client);

        var first = await ApiTestHelper.PostAsync<CreateAppendixPayload>(
            _client,
            $"/api/documents/{documentId}/appendices",
            new
            {
                appendixType = 1,
                answers = new
                {
                    handover_transfer_date = "10.04.2026",
                    handover_property_condition = "Квартира чистая и готова к передаче.",
                    handover_keys_transferred = "2 ключа от квартиры.",
                    handover_meter_readings = "Электричество 15420 кВт·ч."
                }
            });

        var second = await ApiTestHelper.PostAsync<CreateAppendixPayload>(
            _client,
            $"/api/documents/{documentId}/appendices",
            new
            {
                appendixType = 1,
                answers = new
                {
                    handover_transfer_date = "11.04.2026",
                    handover_property_condition = "Квартира передана после дополнительной уборки.",
                    handover_keys_transferred = "2 ключа от квартиры и 1 брелок.",
                    handover_meter_readings = "Электричество 15430 кВт·ч."
                }
            });

        Assert.Equal(first.AppendixId, second.AppendixId);

        var list = await ApiTestHelper.GetAsync<List<AppendixSummaryPayload>>(_client, $"/api/documents/{documentId}/appendices");
        Assert.Single(list, x => x.AppendixId == first.AppendixId);
    }

    [Fact]
    public async Task AppendicesList_ShouldReflectCreatedAppendix()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumDocumentAsync(_client);
        var appendix = await ApiTestHelper.PostAsync<CreateAppendixPayload>(
            _client,
            $"/api/documents/{documentId}/appendices",
            new
            {
                appendixType = 1,
                answers = new
                {
                    handover_transfer_date = "10.04.2026",
                    handover_property_condition = "Квартира чистая и готова к передаче.",
                    handover_keys_transferred = "2 ключа от квартиры.",
                    handover_meter_readings = "Электричество 15420 кВт·ч."
                }
            });

        var list = await ApiTestHelper.GetAsync<List<AppendixSummaryPayload>>(_client, $"/api/documents/{documentId}/appendices");
        Assert.Contains(list, x => x.AppendixId == appendix.AppendixId);
    }

    [Fact]
    public async Task AppendicesList_BeforeCreation_ShouldBeEmpty()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumDocumentAsync(_client);
        var list = await ApiTestHelper.GetAsync<List<AppendixSummaryPayload>>(_client, $"/api/documents/{documentId}/appendices");
        Assert.Empty(list);
    }

    [Fact]
    public async Task AppendixDetails_ShouldContainLegalStructuredContent()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumDocumentAsync(_client);
        var appendix = await ApiTestHelper.PostAsync<CreateAppendixPayload>(
            _client,
            $"/api/documents/{documentId}/appendices",
            new
            {
                appendixType = 1,
                answers = new
                {
                    handover_transfer_date = "10.04.2026",
                    handover_property_condition = "Квартира чистая и готова к передаче.",
                    handover_keys_transferred = "2 ключа от квартиры.",
                    handover_meter_readings = "Электричество 15420 кВт·ч."
                }
            });

        var details = await ApiTestHelper.GetAsync<AppendixDetailsPayload>(_client, $"/api/appendices/{appendix.AppendixId}");

        Assert.False(string.IsNullOrWhiteSpace(details.Content));
        Assert.Equal(1, details.AppendixType);
        Assert.Contains("АКТ ПРИЕМА-ПЕРЕДАЧИ ЖИЛОГО ПОМЕЩЕНИЯ", details.Content);
        Assert.Contains("Во исполнение условий договора аренды", details.Content);
        Assert.Contains("РЕКВИЗИТЫ И ПОДПИСИ СТОРОН", details.Content);
        Assert.Contains("Подпись:", details.Content);
    }

    [Fact]
    public async Task CreatedAppendix_ShouldReturnReadableRussianTitle()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumDocumentAsync(_client);
        var appendix = await ApiTestHelper.PostAsync<CreateAppendixPayload>(
            _client,
            $"/api/documents/{documentId}/appendices",
            new
            {
                appendixType = 1,
                answers = new
                {
                    handover_transfer_date = "10.04.2026",
                    handover_property_condition = "Квартира чистая и готова к передаче.",
                    handover_keys_transferred = "2 ключа от квартиры.",
                    handover_meter_readings = "Электричество 15420 кВт·ч."
                }
            });

        Assert.Equal("Акт приема-передачи", appendix.Title);
    }

    [Fact]
    public async Task PetAppendix_ShouldBeCreated_WhenPremiumPetDataIsPresent()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumPetDocumentAsync(_client);
        var appendix = await ApiTestHelper.PostAsync<CreateAppendixPayload>(
            _client,
            $"/api/documents/{documentId}/appendices",
            new
            {
                appendixType = 3,
                answers = new
                {
                    pet_type = "Кошка",
                    pet_count = 1,
                    pets_details = "Домашняя кошка, стерилизована, проживает постоянно.",
                    pet_residence_rules = "Арендатор следит за чистотой и компенсирует возможный ущерб."
                }
            });

        var details = await ApiTestHelper.GetAsync<AppendixDetailsPayload>(_client, $"/api/appendices/{appendix.AppendixId}");

        Assert.False(string.IsNullOrWhiteSpace(details.Content));
        Assert.Equal(3, details.AppendixType);
        Assert.Contains("СОГЛАШЕНИЕ О ПРОЖИВАНИИ С ДОМАШНИМИ ЖИВОТНЫМИ", details.Content);
        Assert.Contains("РЕКВИЗИТЫ И ПОДПИСИ СТОРОН", details.Content);
        Assert.Contains("Кошка", details.Content);
    }

    [Fact]
    public async Task HandoverAppendix_ShouldRequireSupportingData()
    {
        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.FillBaseRentalAnswersAsync(_client, draftId);

        var generation = await ApiTestHelper.PostAsync<ApiTestHelper.GenerateDocumentPayload>(
            _client,
            $"/api/drafts/{draftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });

        using var response = await _client.PostAsJsonAsync($"/api/documents/{generation.DocumentId}/appendices", new { appendixType = 1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("business_validation_error", body);
        Assert.Contains("handover_transfer_date", body);
    }

    private sealed class DocumentListPayload
    {
        public Guid DocumentId { get; set; }
    }

    private sealed class CreateAppendixPayload
    {
        public Guid AppendixId { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private sealed class AppendixSummaryPayload
    {
        public Guid AppendixId { get; set; }
    }

    private sealed class AppendixDetailsPayload
    {
        public int AppendixType { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
