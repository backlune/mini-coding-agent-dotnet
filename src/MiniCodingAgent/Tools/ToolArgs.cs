using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools;

/// <summary>
/// Small convenience helpers for reading values out of <see cref="JsonObject"/>
/// tool argument blobs without writing the same null-checking everywhere.
/// </summary>
public static class ToolArgs
{
    public static string RequireString(JsonObject args, string key)
    {
        if (!args.TryGetPropertyValue(key, out var node) || node is null)
        {
            throw new ArgumentException(key);
        }

        var value = node switch
        {
            JsonValue jv when jv.TryGetValue(out string? s) => s,
            _ => node.ToString(),
        };

        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException($"{key} must not be empty");
        }
        return value;
    }

    public static string GetString(JsonObject args, string key, string fallback)
    {
        if (!args.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }
        return node switch
        {
            JsonValue jv when jv.TryGetValue(out string? s) => s ?? fallback,
            _ => node.ToString(),
        };
    }

    public static int GetInt(JsonObject args, string key, int fallback)
    {
        if (!args.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }
        if (node is JsonValue jv)
        {
            if (jv.TryGetValue(out int i))
            {
                return i;
            }
            if (jv.TryGetValue(out long l))
            {
                return checked((int)l);
            }
            if (jv.TryGetValue(out double d))
            {
                return (int)d;
            }
            if (jv.TryGetValue(out string? s) && int.TryParse(s, out var parsed))
            {
                return parsed;
            }
        }
        throw new ArgumentException($"{key} must be an integer");
    }

    public static bool Contains(JsonObject args, string key)
    {
        return args.TryGetPropertyValue(key, out _);
    }
}
