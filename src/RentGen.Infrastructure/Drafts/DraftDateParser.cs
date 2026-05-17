using System.Globalization;

namespace RentGen.Infrastructure.Drafts;

internal static class DraftDateParser
{
    private static readonly string[] SupportedFormats =
    {
        "dd.MM.yyyy",
        "d.M.yyyy",
        "dd.MM.yy",
        "d.M.yy",
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "d/M/yyyy"
    };

    public static bool TryParse(string? rawValue, out DateOnly value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var trimmed = rawValue.Trim();
        var russianCulture = CultureInfo.GetCultureInfo("ru-RU");

        return DateOnly.TryParseExact(trimmed, SupportedFormats, russianCulture, DateTimeStyles.None, out value) ||
               DateOnly.TryParse(trimmed, russianCulture, DateTimeStyles.None, out value) ||
               DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    public static string ToRussianDate(string? rawValue, string fallback = "___")
    {
        return TryParse(rawValue, out var value)
            ? value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : fallback;
    }

    public static string ToRussianContractDate(string? rawValue)
    {
        if (!TryParse(rawValue, out var value))
        {
            return "«___» __________ 20___ г.";
        }

        var month = value.Month switch
        {
            1 => "января",
            2 => "февраля",
            3 => "марта",
            4 => "апреля",
            5 => "мая",
            6 => "июня",
            7 => "июля",
            8 => "августа",
            9 => "сентября",
            10 => "октября",
            11 => "ноября",
            _ => "декабря"
        };

        return $"«{value.Day:00}» {month} {value.Year} г.";
    }
}
