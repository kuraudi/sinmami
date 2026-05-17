using System.Text.Json;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Documents;

namespace RentGen.UnitTests;

public class RentalAgreementTemplateRendererTests
{
    [Fact]
    public void Render_ForIndividual_ShouldMentionRussianCitizenship()
    {
        var answers = ParseAnswers(
            """
            {
              "agreement_city": "Москва",
              "agreement_date": "10.04.2026",
              "landlord_type": "individual",
              "landlord_name": "Иванов Иван Иванович",
              "landlord_rf_citizenship_confirmed": true,
              "landlord_passport_number": "45 01 123456",
              "landlord_passport_issued_by": "ОВД района",
              "landlord_passport_unit_code": "770-001",
              "landlord_passport_issue_date": "15.07.2018",
              "landlord_registration_address": "Москва, ул. Лесная, д. 1",
              "tenant_type": "individual",
              "tenant_name": "Петров Петр Петрович",
              "tenant_rf_citizenship_confirmed": true,
              "tenant_passport_number": "45 02 654321",
              "tenant_passport_issued_by": "ОВД района",
              "tenant_passport_unit_code": "770-002",
              "tenant_passport_issue_date": "22.03.2020",
              "tenant_registration_address": "Москва, ул. Полевая, д. 2",
              "property_type": "apartment",
              "property_address": "Москва, ул. Примерная, д. 1, кв. 10",
              "property_description": "Квартира",
              "lease_start_date": "01.05.2026",
              "lease_end_date": "30.04.2027",
              "rent_amount": 50000,
              "payment_due_day": 5,
              "payment_method": "bank_transfer",
              "utilities_payment_terms": "по квитанциям"
            }
            """);

        var text = RentalAgreementTemplateRenderer.Render(answers, SubscriptionPlan.Free);

        Assert.Contains("гражданин Российской Федерации", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_ForEntrepreneur_ShouldUseOgrnipAndInn()
    {
        var answers = ParseAnswers(
            """
            {
              "agreement_city": "Москва",
              "agreement_date": "10.04.2026",
              "landlord_type": "entrepreneur",
              "landlord_name": "Иванов Иван Иванович",
              "landlord_entrepreneur_ogrnip": "318774600000001",
              "landlord_entrepreneur_inn": "770123456789",
              "landlord_entrepreneur_registration_address": "Москва, ул. Лесная, д. 1",
              "tenant_type": "individual",
              "tenant_name": "Петров Петр Петрович",
              "tenant_rf_citizenship_confirmed": true,
              "tenant_passport_number": "45 02 654321",
              "tenant_passport_issued_by": "ОВД района",
              "tenant_passport_unit_code": "770-002",
              "tenant_passport_issue_date": "22.03.2020",
              "tenant_registration_address": "Москва, ул. Полевая, д. 2",
              "property_type": "apartment",
              "property_address": "Москва, ул. Примерная, д. 1, кв. 10",
              "property_description": "Квартира",
              "lease_start_date": "01.05.2026",
              "lease_end_date": "30.04.2027",
              "rent_amount": 50000,
              "payment_due_day": 5,
              "payment_method": "bank_transfer",
              "utilities_payment_terms": "по квитанциям"
            }
            """);

        var text = RentalAgreementTemplateRenderer.Render(answers, SubscriptionPlan.Free);

        Assert.Contains("индивидуальный предприниматель", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ОГРНИП 318774600000001", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ИНН 770123456789", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("данные документа: ___", text, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, JsonElement> ParseAnswers(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone());
    }
}
