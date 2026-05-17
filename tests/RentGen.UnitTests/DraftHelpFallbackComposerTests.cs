using RentGen.Application.Common.Models;
using RentGen.Infrastructure.Drafts;

namespace RentGen.UnitTests;

public class DraftHelpFallbackComposerTests
{
    [Fact]
    public void Compose_ForPartyTypeWhyQuestion_ShouldExplainWhyFieldIsNeeded()
    {
        var step = new ScenarioStep
        {
            Key = "landlord_type",
            Title = "Тип арендодателя",
            QuestionText = "Кто выступает арендодателем?",
            InputType = "select",
            Required = true
        };

        var result = DraftHelpFallbackComposer.Compose(step, "Зачем в договоре указывать кто есть кто?");

        Assert.Contains("какие реквизиты стороны включать", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Можно отвечать коротко", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_ForPartyTypeHowQuestion_ShouldExplainAvailableVariants()
    {
        var step = new ScenarioStep
        {
            Key = "tenant_type",
            Title = "Тип арендатора",
            QuestionText = "Кто выступает арендатором?",
            InputType = "select",
            Required = true
        };

        var result = DraftHelpFallbackComposer.Compose(step, "Кого тут выбирать?");

        Assert.Contains("физлицо", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ип", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("организация", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_ForCitizenshipQuestion_ShouldExplainRussianCitizenshipRequirement()
    {
        var step = new ScenarioStep
        {
            Key = "tenant_rf_citizenship_confirmed",
            Title = "Гражданство РФ арендатора",
            QuestionText = "Подтвердите, что арендатор является гражданином Российской Федерации.",
            InputType = "boolean",
            Required = true
        };

        var result = DraftHelpFallbackComposer.Compose(step, "Это обязательно?");

        Assert.Contains("граждан", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("обязател", result, StringComparison.OrdinalIgnoreCase);
    }
}
