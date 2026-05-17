using RentGen.Application.Prompts;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Prompts;

namespace RentGen.UnitTests;

public class PromptBuilderTests
{
    [Fact]
    public void DocumentPromptBuilder_FreePlan_ShouldLimitOutputToBaseAgreement()
    {
        var builder = new RentalAgreementDocumentPromptBuilder();

        var result = builder.Build(new RentalAgreementDocumentPromptContext
        {
            Plan = SubscriptionPlan.Free,
            AnswersJson = "{\"rent_amount\":50000}"
        });

        Assert.Contains("Тариф: Free.", result.UserPrompt);
        Assert.Contains("не упоминай премиальные приложения", result.UserPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Не создавай разделы после раздела 6.", result.SystemPrompt);
    }

    [Fact]
    public void DocumentPromptBuilder_PremiumPlan_ShouldAllowAppendixReferencesOnly()
    {
        var builder = new RentalAgreementDocumentPromptBuilder();

        var result = builder.Build(new RentalAgreementDocumentPromptContext
        {
            Plan = SubscriptionPlan.Premium,
            AnswersJson = "{\"rent_amount\":50000,\"has_pets_clause\":true}"
        });

        Assert.Contains("Тариф: Premium.", result.UserPrompt);
        Assert.Contains("проживание с животными", result.UserPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("подробные условия по животным", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GuidePromptBuilder_ShouldAskForPlainRussianGuideWithoutMarkdown()
    {
        var builder = new RentalAgreementGuidePromptBuilder();

        var result = builder.Build(new RentalAgreementGuidePromptContext
        {
            Plan = SubscriptionPlan.Premium,
            DocumentContent = "Draft",
            AnswersJson = "{}"
        });

        Assert.Contains("Write in Russian.", result.SystemPrompt);
        Assert.Contains("Do not use markdown", result.SystemPrompt);
        Assert.Contains("plain text only", result.SystemPrompt);
    }

    [Fact]
    public void HelpPromptBuilder_ShouldRequestPlainContextAwareRussianAnswer()
    {
        var builder = new RentalAgreementHelpPromptBuilder();

        var result = builder.Build(new RentalAgreementHelpPromptContext
        {
            Plan = SubscriptionPlan.Premium,
            StepKey = "deposit_required",
            StepTitle = "Обеспечительный платеж",
            StepQuestionText = "Нужно ли включить условие об обеспечительном платеже?",
            StepHelpText = "Укажи, нужен ли залог по договору.",
            StepInputType = "boolean",
            StepOptionsText = "true (да), false (нет)",
            CollectedFactsText = "- Арендная плата: 50000",
            MissingRequiredStepsText = "Срок аренды",
            UserQuestion = "Что писать про обеспечительный платеж?"
        });

        Assert.Contains("markdown", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reasoning", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deposit_required", result.UserPrompt);
        Assert.Contains("обеспечительный платеж", result.UserPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("50000", result.UserPrompt);
    }
}
