using System.Text.Json;

namespace RentGen.Infrastructure.Drafts;

internal static class DraftAnswerReader
{
    public static Dictionary<string, JsonElement> Parse(string answersJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answersJson) ?? new Dictionary<string, JsonElement>();
    }

    public static string? GetString(IReadOnlyDictionary<string, JsonElement> answers, string key)
    {
        return answers.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    public static bool? GetBoolean(IReadOnlyDictionary<string, JsonElement> answers, string key)
    {
        if (!answers.TryGetValue(key, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    public static decimal? GetDecimal(IReadOnlyDictionary<string, JsonElement> answers, string key)
    {
        if (!answers.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public static int? GetInt32(IReadOnlyDictionary<string, JsonElement> answers, string key)
    {
        if (!answers.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public static bool HasValue(IReadOnlyDictionary<string, JsonElement> answers, string key)
    {
        if (!answers.TryGetValue(key, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Null => false,
            JsonValueKind.Undefined => false,
            JsonValueKind.String => !string.IsNullOrWhiteSpace(value.GetString()),
            JsonValueKind.Array => value.GetArrayLength() > 0,
            JsonValueKind.Object => value.EnumerateObject().Any(),
            _ => true
        };
    }
}
