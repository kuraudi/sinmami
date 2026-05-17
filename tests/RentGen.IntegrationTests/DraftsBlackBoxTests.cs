using System.Net;
using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

public class DraftsBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public DraftsBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAndGetDraft_ShouldReturnDraftDetails()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        var draft = await ApiTestHelper.GetAsync<DraftPayload>(_client, $"/api/drafts/{draftId}");

        Assert.Equal(draftId, draft.DraftId);
        Assert.Equal(1, draft.Status);
        Assert.False(string.IsNullOrWhiteSpace(draft.Title));
    }

    [Fact]
    public async Task NextQuestion_ForNewFreeDraft_ShouldReturnFirstBaseStep()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        var nextQuestion = await ApiTestHelper.GetAsync<NextQuestionPayload>(_client, $"/api/drafts/{draftId}/next-question");

        Assert.Equal("landlord_type", nextQuestion.StepKey);
        Assert.Equal("Тип арендодателя", nextQuestion.Title);
        Assert.Equal("Кто выступает арендодателем?", nextQuestion.QuestionText);
    }

    [Fact]
    public async Task DraftSteps_ShouldReturnVisibleStepsAndSavedValues()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Premium");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.SaveAnswerAsync(_client, draftId, "landlord_type", "individual");
        await ApiTestHelper.SaveAnswerAsync(_client, draftId, "landlord_name", "Ivan Ivanov");

        var steps = await ApiTestHelper.GetAsync<List<DraftStepPayload>>(_client, $"/api/drafts/{draftId}/steps");

        Assert.Contains(steps, x => x.StepKey == "landlord_name" && x.IsAnswered);
        Assert.DoesNotContain(steps, x => x.StepKey == "deposit_required");
    }

    [Fact]
    public async Task SavingSlangSelectAnswer_ShouldUnlockDependentSteps()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Premium");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.SaveAnswerAsync(_client, draftId, "landlord_type", "физовик");

        var steps = await ApiTestHelper.GetAsync<List<DraftStepPayload>>(_client, $"/api/drafts/{draftId}/steps");

        Assert.Contains(steps, x => x.StepKey == "landlord_passport_number");
        Assert.Contains(steps, x => x.StepKey == "landlord_passport_issued_by");
        Assert.Contains(steps, x => x.StepKey == "landlord_passport_unit_code");
        Assert.Contains(steps, x => x.StepKey == "landlord_passport_issue_date");
        Assert.Contains(steps, x => x.StepKey == "landlord_registration_address");
    }

    [Fact]
    public async Task SavingEntrepreneurSlang_ShouldUnlockEntrepreneurSteps()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Premium");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.SaveAnswerAsync(_client, draftId, "landlord_type", "ипшник");

        var steps = await ApiTestHelper.GetAsync<List<DraftStepPayload>>(_client, $"/api/drafts/{draftId}/steps");

        Assert.Contains(steps, x => x.StepKey == "landlord_entrepreneur_ogrnip");
        Assert.Contains(steps, x => x.StepKey == "landlord_entrepreneur_inn");
        Assert.Contains(steps, x => x.StepKey == "landlord_entrepreneur_registration_address");
        Assert.DoesNotContain(steps, x => x.StepKey == "landlord_passport_number");
    }

    [Fact]
    public async Task Validate_InvalidDraft_ShouldReturnMissingSteps()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        var validation = await ApiTestHelper.PostAsync<ValidatePayload>(_client, $"/api/drafts/{draftId}/validate", new { });

        Assert.False(validation.IsValid);
        Assert.NotEmpty(validation.MissingSteps);
    }

    [Fact]
    public async Task Generate_InvalidDraft_ShouldReturnValidationDetails()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);

        using var response = await _client.PostAsJsonAsync($"/api/drafts/{draftId}/generate", new
        {
            includeGuide = true,
            requestedAppendices = Array.Empty<int>()
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("business_validation_error", body);
    }

    [Fact]
    public async Task SavingRemovedMainFlowStep_ShouldReturnConflict()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        using var response = await _client.PostAsJsonAsync($"/api/drafts/{draftId}/answers", new
        {
            stepKey = "deposit_required",
            value = true
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_operation", body);
    }

    [Fact]
    public async Task FreePlan_GenerateWithRequestedAppendices_ShouldReturnForbidden()
    {
        _client.DefaultRequestHeaders.Remove("X-Plan");
        _client.DefaultRequestHeaders.Add("X-Plan", "Free");

        var draftId = await ApiTestHelper.CreateDraftAsync(_client);
        await ApiTestHelper.FillBaseRentalAnswersAsync(_client, draftId);

        using var response = await _client.PostAsJsonAsync($"/api/drafts/{draftId}/generate", new
        {
            includeGuide = true,
            requestedAppendices = new[] { 1 }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("feature_access_denied", body);
    }

    private sealed class DraftPayload
    {
        public Guid DraftId { get; set; }
        public int Status { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private sealed class NextQuestionPayload
    {
        public string StepKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
    }

    private sealed class ValidatePayload
    {
        public bool IsValid { get; set; }
        public List<string> MissingSteps { get; set; } = [];
    }

    private sealed class DraftStepPayload
    {
        public string StepKey { get; set; } = string.Empty;
        public bool IsAnswered { get; set; }
    }
}
