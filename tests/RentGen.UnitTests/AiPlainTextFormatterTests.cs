using RentGen.Infrastructure.Llm;

namespace RentGen.UnitTests;

public class AiPlainTextFormatterTests
{
    [Fact]
    public void Normalize_ShouldStripMarkdownMarkers()
    {
        var input = """
            1. **Внимательно проверьте данные**

            * Подпишите договор
            """;

        var result = AiPlainTextFormatter.Normalize(input);

        Assert.DoesNotContain("**", result);
        Assert.DoesNotContain("* Подпишите", result);
        Assert.Contains("1. Внимательно проверьте данные", result);
        Assert.Contains("- Подпишите договор", result);
    }
}
