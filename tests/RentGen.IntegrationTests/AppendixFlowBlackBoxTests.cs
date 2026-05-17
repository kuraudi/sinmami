using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class AppendixFlowBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public AppendixFlowBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Plan", "Premium");
    }

    [Fact]
    public async Task GetAppendixFlow_ShouldReturnQuestionnaireSteps()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumDocumentAsync(_client);
        var flow = await ApiTestHelper.GetAsync<AppendixFlowPayload>(_client, $"/api/documents/{documentId}/appendices/flow/1");

        Assert.Equal(1, flow.AppendixType);
        Assert.Contains("Акт", flow.Title);
        Assert.Contains(flow.Steps, step => step.StepKey == "handover_transfer_date");
        Assert.Contains(flow.Steps, step => step.StepKey == "handover_property_condition");
    }

    [Fact]
    public async Task PreviewAppendix_ShouldShowMissingFields_WhenDataIsNotEnough()
    {
        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.FillBaseRentalAnswersAsync(_client, draftId);

        var generation = await ApiTestHelper.PostAsync<ApiTestHelper.GenerateDocumentPayload>(
            _client,
            $"/api/drafts/{draftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });

        var preview = await ApiTestHelper.PostAsync<AppendixPreviewPayload>(
            _client,
            $"/api/documents/{generation.DocumentId}/appendices/preview",
            new { appendixType = 1, answers = new { } });

        Assert.False(preview.IsReady);
        Assert.Contains("handover_transfer_date", preview.MissingSteps);
    }

    [Fact]
    public async Task PreviewAndCreateAppendix_ShouldWork_WithMiniQuestionnaireAnswers()
    {
        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.FillBaseRentalAnswersAsync(_client, draftId);

        var generation = await ApiTestHelper.PostAsync<ApiTestHelper.GenerateDocumentPayload>(
            _client,
            $"/api/drafts/{draftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });

        var answers = new
        {
            handover_transfer_date = "10.04.2026",
            handover_property_condition = "Квартира чистая, техника исправна, объект готов к передаче.",
            handover_visible_defects = "Незначительная потертость на межкомнатной двери.",
            handover_keys_transferred = "2 ключа от квартиры и 1 брелок от домофона.",
            handover_meter_readings = "Электричество 15420 кВт·ч, холодная вода 124 м3, горячая вода 98 м3."
        };

        var preview = await ApiTestHelper.PostAsync<AppendixPreviewPayload>(
            _client,
            $"/api/documents/{generation.DocumentId}/appendices/preview",
            new { appendixType = 1, answers });

        Assert.True(preview.IsReady);
        Assert.Contains("АКТ ПРИЕМА-ПЕРЕДАЧИ", preview.Content);

        var created = await ApiTestHelper.PostAsync<CreateAppendixPayload>(
            _client,
            $"/api/documents/{generation.DocumentId}/appendices",
            new { appendixType = 1, answers });

        Assert.NotEqual(Guid.Empty, created.AppendixId);
    }

    [Fact]
    public async Task GetAppendixFlow_ShouldPrefillSavedAnswers_WhenAppendixAlreadyExists()
    {
        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.FillBaseRentalAnswersAsync(_client, draftId);

        var generation = await ApiTestHelper.PostAsync<ApiTestHelper.GenerateDocumentPayload>(
            _client,
            $"/api/drafts/{draftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });

        var answers = new
        {
            handover_transfer_date = "10.04.2026",
            handover_property_condition = "Квартира передается в исправном состоянии.",
            handover_visible_defects = "Без замечаний.",
            handover_keys_transferred = "2 ключа и 1 брелок.",
            handover_meter_readings = "Электричество 15420 кВт·ч."
        };

        await ApiTestHelper.PostAsync<CreateAppendixPayload>(
            _client,
            $"/api/documents/{generation.DocumentId}/appendices",
            new { appendixType = 1, answers });

        var flow = await ApiTestHelper.GetAsync<AppendixFlowPayload>(
            _client,
            $"/api/documents/{generation.DocumentId}/appendices/flow/1");

        Assert.Contains(flow.Steps, step => step.StepKey == "handover_transfer_date" && step.Value?.ToString() == "10.04.2026");
        Assert.Contains(flow.Steps, step => step.StepKey == "handover_keys_transferred" && step.Value?.ToString() == "2 ключа и 1 брелок.");
    }

    private sealed class AppendixFlowPayload
    {
        public int AppendixType { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<AppendixFlowStepPayload> Steps { get; set; } = new();
    }

    private sealed class AppendixFlowStepPayload
    {
        public string StepKey { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    private sealed class AppendixPreviewPayload
    {
        public bool IsReady { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<string> MissingSteps { get; set; } = new();
    }

    private sealed class CreateAppendixPayload
    {
        public Guid AppendixId { get; set; }
    }
}
