using System.Text.Json;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Infrastructure.Drafts;

namespace RentGen.UnitTests;

public class StepAnswerInterpreterTests
{
    [Fact]
    public async Task InterpretAsync_ShouldUnderstandCashSlang()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "payment_method",
            Title = "Способ оплаты",
            QuestionText = "Как обычно будет вноситься арендная плата?",
            InputType = "select",
            Options = new List<string> { "bank_transfer", "cash", "mixed" }
        };

        var result = await interpreter.InterpretAsync(step, "наличка", new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("cash", result.Value);
    }

    [Fact]
    public async Task InterpretAsync_ShouldUnderstandBankTransferSlangWithSeparatedWords()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "payment_method",
            Title = "РЎРїРѕСЃРѕР± РѕРїР»Р°С‚С‹",
            QuestionText = "РљР°Рє РѕР±С‹С‡РЅРѕ Р±СѓРґРµС‚ РІРЅРѕСЃРёС‚СЊСЃСЏ Р°СЂРµРЅРґРЅР°СЏ РїР»Р°С‚Р°?",
            InputType = "select",
            Options = new List<string> { "bank_transfer", "cash", "mixed" }
        };

        var result = await interpreter.InterpretAsync(step, "без нал", new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("bank_transfer", result.Value);
    }

    [Fact]
    public async Task InterpretAsync_ShouldUnderstandPhysicalPersonSlang()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "tenant_type",
            Title = "Тип арендатора",
            QuestionText = "Кто выступает арендатором?",
            InputType = "select",
            Options = new List<string> { "individual", "entrepreneur", "company" }
        };

        var result = await interpreter.InterpretAsync(step, "физовик", new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("individual", result.Value);
    }

    [Fact]
    public async Task InterpretAsync_ShouldUnderstandEntrepreneurSlang()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "tenant_type",
            Title = "Тип арендатора",
            QuestionText = "Кто выступает арендатором?",
            InputType = "select",
            Options = new List<string> { "individual", "entrepreneur", "company" }
        };

        var result = await interpreter.InterpretAsync(step, "ипшник", new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("entrepreneur", result.Value);
    }

    [Fact]
    public async Task InterpretAsync_ShouldParseRussianDateWithoutSwappingDayAndMonth()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "agreement_date",
            Title = "Дата подписания договора",
            QuestionText = "Укажите дату подписания договора.",
            InputType = "date"
        };

        var result = await interpreter.InterpretAsync(step, "10.04.2026", new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("10.04.2026", result.Value);
    }

    [Fact]
    public async Task InterpretAsync_ShouldNormalizePassportUnitCode()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "tenant_passport_unit_code",
            Title = "Код подразделения арендатора",
            QuestionText = "Укажите код подразделения документа арендатора.",
            InputType = "text"
        };

        var result = await interpreter.InterpretAsync(step, "770001", new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("770-001", result.Value);
    }

    [Fact]
    public async Task InterpretAsync_ShouldNormalizePassportNumber()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "tenant_passport_number",
            Title = "Номер документа арендатора",
            QuestionText = "Укажите серию и номер документа арендатора.",
            InputType = "text"
        };

        var result = await interpreter.InterpretAsync(step, "4502123456", new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("45 02 123456", result.Value);
    }

    [Fact]
    public async Task InterpretAsync_ShouldNormalizeEntrepreneurInn()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "tenant_entrepreneur_inn",
            Title = "ИНН арендатора",
            QuestionText = "Укажите ИНН арендатора.",
            InputType = "text"
        };

        var result = await interpreter.InterpretAsync(step, "ИНН 770123456789", new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("770123456789", result.Value);
    }

    [Fact]
    public async Task InterpretAsync_ShouldUnderstandCashSlangInsideJsonElement()
    {
        var interpreter = new StepAnswerInterpreter(new FakeAiClient());
        var step = new ScenarioStep
        {
            Key = "payment_method",
            Title = "Способ оплаты",
            QuestionText = "Как обычно будет вноситься арендная плата?",
            InputType = "select",
            Options = new List<string> { "bank_transfer", "cash", "mixed" }
        };

        using var document = JsonDocument.Parse("\"наличка\"");
        var rawValue = document.RootElement.Clone();

        var result = await interpreter.InterpretAsync(step, rawValue, new Dictionary<string, JsonElement>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("cash", result.Value);
    }

    private sealed class FakeAiClient : IGenerativeAiClient
    {
        public Task<AiCompletionResult> GenerateAsync(AiCompletionRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Heuristic path should not call the LLM in this test.");
        }
    }
}
