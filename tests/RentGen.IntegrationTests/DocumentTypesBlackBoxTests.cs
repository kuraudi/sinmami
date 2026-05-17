using System.Net;
using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class DocumentTypesBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public DocumentTypesBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Scenario_ForFreePlan_ShouldHidePremiumSteps()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/document-types/1/scenario");
        request.Headers.Add("X-Plan", "Free");

        using var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<DocumentScenarioPayload>();

        Assert.NotNull(payload);
        Assert.DoesNotContain(payload!.Steps, x => x.FeatureCode == 1);
        Assert.DoesNotContain(payload.Steps, x => x.Key == "deposit_required");
    }

    [Fact]
    public async Task Scenario_ForPremiumPlan_ShouldKeepMainFlowBaseOnly()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/document-types/1/scenario");
        request.Headers.Add("X-Plan", "Premium");

        using var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<DocumentScenarioPayload>();

        Assert.NotNull(payload);
        Assert.DoesNotContain(payload!.Steps, x => x.FeatureCode == 1);
        Assert.DoesNotContain(payload.Steps, x => x.Key == "deposit_required");
    }

    [Fact]
    public async Task Scenario_ShouldReturnRussianQuestionTexts()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/document-types/1/scenario");
        request.Headers.Add("X-Plan", "Free");

        using var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<DocumentScenarioPayload>();

        Assert.NotNull(payload);
        var firstStep = payload!.Steps.First(x => x.Key == "landlord_type");
        Assert.Equal("Стороны договора", firstStep.Section);
        Assert.Equal("Тип арендодателя", firstStep.Title);
        Assert.Equal("Кто выступает арендодателем?", firstStep.QuestionText);
    }

    private sealed class DocumentScenarioPayload
    {
        public string Version { get; set; } = string.Empty;
        public List<ScenarioStepPayload> Steps { get; set; } = [];
    }

    private sealed class ScenarioStepPayload
    {
        public string Section { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public int? FeatureCode { get; set; }
    }
}
