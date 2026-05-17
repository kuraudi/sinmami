using System.Net.Http.Json;

namespace RentGen.IntegrationTests;

internal static class ApiTestHelper
{
    public static async Task<Guid> CreateDraftAsync(HttpClient client, int documentType = 1)
    {
        var payload = await PostAsync<CreateDraftPayload>(client, "/api/drafts", new { documentType });
        return payload.DraftId;
    }

    public static async Task SaveAnswerAsync(HttpClient client, Guid draftId, string stepKey, object value)
    {
        _ = await PostAsync<SaveAnswerPayload>(client, $"/api/drafts/{draftId}/answers", new { stepKey, value });
    }

    public static async Task<Guid> CreateValidFreeDocumentAsync(HttpClient client)
    {
        var draftId = await CreateDraftAsync(client);
        await FillBaseRentalAnswersAsync(client, draftId);
        var generation = await PostAsync<GenerateDocumentPayload>(
            client,
            $"/api/drafts/{draftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });
        return generation.DocumentId;
    }

    public static async Task<Guid> CreateValidPremiumDocumentAsync(HttpClient client)
    {
        var draftId = await CreateDraftAsync(client);
        await FillBaseRentalAnswersAsync(client, draftId);
        var generation = await PostAsync<GenerateDocumentPayload>(
            client,
            $"/api/drafts/{draftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });
        return generation.DocumentId;
    }

    public static async Task<Guid> CreateValidPremiumPetDocumentAsync(HttpClient client)
    {
        var draftId = await CreateDraftAsync(client);
        await FillBaseRentalAnswersAsync(client, draftId);

        var generation = await PostAsync<GenerateDocumentPayload>(
            client,
            $"/api/drafts/{draftId}/generate",
            new { includeGuide = true, requestedAppendices = Array.Empty<int>() });
        return generation.DocumentId;
    }

    public static async Task FillBaseRentalAnswersAsync(HttpClient client, Guid draftId)
    {
        await SaveAnswerAsync(client, draftId, "landlord_type", "individual");
        await SaveAnswerAsync(client, draftId, "landlord_name", "Иван Иванов");
        await SaveAnswerAsync(client, draftId, "landlord_rf_citizenship_confirmed", true);
        await SaveAnswerAsync(client, draftId, "landlord_passport_number", "45 01 123456");
        await SaveAnswerAsync(client, draftId, "landlord_passport_issued_by", "ОВД Тверского района г. Москвы");
        await SaveAnswerAsync(client, draftId, "landlord_passport_unit_code", "770-001");
        await SaveAnswerAsync(client, draftId, "landlord_passport_issue_date", "15.07.2018");
        await SaveAnswerAsync(client, draftId, "landlord_registration_address", "Москва, ул. Лесная, д. 10, кв. 5");
        await SaveAnswerAsync(client, draftId, "landlord_phone", "+79000000001");

        await SaveAnswerAsync(client, draftId, "tenant_type", "individual");
        await SaveAnswerAsync(client, draftId, "tenant_name", "Петр Петров");
        await SaveAnswerAsync(client, draftId, "tenant_rf_citizenship_confirmed", true);
        await SaveAnswerAsync(client, draftId, "tenant_passport_number", "45 02 654321");
        await SaveAnswerAsync(client, draftId, "tenant_passport_issued_by", "ОВД Пресненского района г. Москвы");
        await SaveAnswerAsync(client, draftId, "tenant_passport_unit_code", "770-002");
        await SaveAnswerAsync(client, draftId, "tenant_passport_issue_date", "22.03.2020");
        await SaveAnswerAsync(client, draftId, "tenant_registration_address", "Санкт-Петербург, Невский проспект, д. 1, кв. 7");
        await SaveAnswerAsync(client, draftId, "tenant_phone", "+79000000002");

        await SaveAnswerAsync(client, draftId, "agreement_city", "Москва");
        await SaveAnswerAsync(client, draftId, "agreement_date", "25.03.2026");
        await SaveAnswerAsync(client, draftId, "property_type", "apartment");
        await SaveAnswerAsync(client, draftId, "property_address", "Москва, ул. Примерная, д. 1, кв. 10");
        await SaveAnswerAsync(client, draftId, "property_description", "Однокомнатная меблированная квартира.");
        await SaveAnswerAsync(client, draftId, "property_area_sqm", 42);
        await SaveAnswerAsync(client, draftId, "lease_start_date", "01.04.2026");
        await SaveAnswerAsync(client, draftId, "lease_end_date", "31.03.2027");
        await SaveAnswerAsync(client, draftId, "rent_amount", 50000);
        await SaveAnswerAsync(client, draftId, "payment_due_day", 5);
        await SaveAnswerAsync(client, draftId, "payment_method", "bank_transfer");
        await SaveAnswerAsync(client, draftId, "utilities_payment_terms", "Арендатор оплачивает коммунальные услуги по квитанциям и показаниям счетчиков.");
    }

    public static Task FillPremiumStandardAnswersAsync(HttpClient client, Guid draftId)
    {
        _ = client;
        _ = draftId;
        return Task.CompletedTask;
    }

    public static async Task<T> PostAsync<T>(HttpClient client, string url, object payload)
    {
        using var response = await client.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"POST {url} failed with {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<T>();
        return result!;
    }

    public static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        return (await client.GetFromJsonAsync<T>(url))!;
    }

    internal sealed class CreateDraftPayload
    {
        public Guid DraftId { get; set; }
    }

    internal sealed class SaveAnswerPayload
    {
        public Guid DraftId { get; set; }
    }

    internal sealed class GenerateDocumentPayload
    {
        public Guid DocumentId { get; set; }
        public int Status { get; set; }
    }
}
