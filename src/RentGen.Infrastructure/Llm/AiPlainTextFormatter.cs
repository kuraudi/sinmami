using System.Text.RegularExpressions;

namespace RentGen.Infrastructure.Llm;

internal static class AiPlainTextFormatter
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        text = Regex.Replace(text, @"<think>.*?</think>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^\s*(think|thought|reasoning|размышление|ход мыслей|ход рассуждений)\s*:.*$", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\[(.+?)\]\((.+?)\)", "$1 ($2)");
        text = Regex.Replace(text, @"(\*\*|__)(.+?)(\*\*|__)", "$2");
        text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "$1");
        text = text.Replace("`", string.Empty, StringComparison.Ordinal);

        var lines = text
            .Split('\n')
            .Select(RewriteLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return Regex.Replace(string.Join('\n', lines), @"\n{3,}", "\n\n").Trim();
    }

    private static string RewriteLine(string line)
    {
        var rewritten = line.TrimEnd();

        rewritten = Regex.Replace(rewritten, @"^\s{0,3}#{1,6}\s*", string.Empty);
        rewritten = Regex.Replace(rewritten, @"^\s*[-*+]\s+", "- ");
        rewritten = Regex.Replace(rewritten, @"^\s*(\d+)\)\s+", "$1. ");
        rewritten = Regex.Replace(rewritten, @"^\s*(think|thought|reasoning|размышление|ход мыслей|ход рассуждений)\s*:?\s*", string.Empty, RegexOptions.IgnoreCase);
        rewritten = rewritten.Replace("**", string.Empty, StringComparison.Ordinal);

        return rewritten.Trim();
    }
}
