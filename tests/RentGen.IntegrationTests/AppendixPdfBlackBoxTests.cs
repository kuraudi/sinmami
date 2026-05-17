using System.Net.Http.Json;
using System.Text;

namespace RentGen.IntegrationTests;

public class AppendixPdfBlackBoxTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public AppendixPdfBlackBoxTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Plan", "Premium");
    }

    [Fact]
    public async Task EveryAppendixType_ShouldBeExportableToPdf()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumPetDocumentAsync(_client);
        var appendixTypes = new[] { 1, 2, 3, 4, 5, 6 };

        foreach (var appendixType in appendixTypes)
        {
            var appendix = await ApiTestHelper.PostAsync<CreateAppendixPayload>(
                _client,
                $"/api/documents/{documentId}/appendices",
                new { appendixType, answers = BuildAppendixAnswers(appendixType) });

            using var response = await _client.GetAsync($"/api/appendices/{appendix.AppendixId}/pdf");
            response.EnsureSuccessStatusCode();

            Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var signature = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
            Assert.Equal("%PDF-", signature);
        }
    }

    [Fact]
    public async Task AppendixPreviewPdfEndpoint_ShouldReturnDownloadablePdf()
    {
        var documentId = await ApiTestHelper.CreateValidPremiumPetDocumentAsync(_client);

        using var response = await _client.PostAsJsonAsync(
            $"/api/documents/{documentId}/appendices/preview/pdf",
            new
            {
                appendixType = 1,
                answers = BuildAppendixAnswers(1)
            });

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var signature = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        Assert.Equal("%PDF-", signature);
    }

    private sealed class CreateAppendixPayload
    {
        public Guid AppendixId { get; set; }
    }

    private static object BuildAppendixAnswers(int appendixType) => appendixType switch
    {
        1 => new
        {
            handover_transfer_date = "10.04.2026",
            handover_property_condition = "Квартира чистая, техника исправна, объект готов к передаче.",
            handover_visible_defects = "Незначительная потертость на межкомнатной двери.",
            handover_keys_transferred = "2 ключа от квартиры и 1 брелок от домофона.",
            handover_meter_readings = "Электричество 15420 кВт·ч, холодная вода 124 м3, горячая вода 98 м3."
        },
        2 => new
        {
            inventory_items_description = "Диван, шкаф, стол, холодильник, стиральная машина.",
            inventory_items_condition = "Мебель и техника в рабочем состоянии, есть незначительный износ."
        },
        3 => new
        {
            pet_type = "Кошка",
            pet_count = 1,
            pets_details = "Домашняя кошка, стерилизована, проживает постоянно.",
            pet_residence_rules = "Арендатор следит за чистотой и компенсирует возможный ущерб."
        },
        4 => new
        {
            payment_schedule_first_payment_date = "10.04.2026",
            payment_schedule_frequency = "monthly",
            payment_schedule_notes = "Первый платеж в день передачи, далее ежемесячно до 5-го числа."
        },
        5 => new
        {
            deposit_amount = 50000,
            deposit_return_terms = "Возвращается в течение 5 рабочих дней после выезда при отсутствии долгов и ущерба.",
            deposit_retention_terms = "Удержание возможно при наличии задолженности или подтвержденного ущерба."
        },
        6 => new
        {
            smoking_policy = "Курение в помещении запрещено.",
            noise_policy = "После 22:00 необходимо соблюдать режим тишины.",
            guest_policy = "Длительное проживание гостей допускается только с согласия арендодателя.",
            sublease_allowed = false,
            additional_rules = "Не переставлять мебель без согласования."
        },
        _ => new { }
    };
}
