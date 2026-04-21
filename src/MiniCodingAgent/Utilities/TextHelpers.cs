namespace MiniCodingAgent.Utilities;

/// <summary>
/// String helpers used to keep prompts, transcripts, and tool output within budget.
/// Mirrors the <c>clip</c>/<c>middle</c> utilities from the Python reference.
/// </summary>
public static class TextHelpers
{
    /// <summary>
    /// Truncates <paramref name="text"/> to <paramref name="limit"/> characters,
    /// appending a notice describing how many characters were dropped.
    /// </summary>
    public static string Clip(string? text, int limit)
    {
        var value = text ?? string.Empty;
        if (value.Length <= limit)
        {
            return value;
        }

        var dropped = value.Length - limit;
        return value[..limit] + $"\n...[truncated {dropped} chars]";
    }

    /// <summary>
    /// Shortens <paramref name="text"/> to fit in <paramref name="limit"/> characters by
    /// preserving the beginning and end with a ellipsis in the middle.
    /// </summary>
    public static string Middle(string? text, int limit)
    {
        var value = (text ?? string.Empty).Replace("\n", " ", StringComparison.Ordinal);
        if (value.Length <= limit)
        {
            return value;
        }
        if (limit <= 3)
        {
            return value[..limit];
        }

        var left = (limit - 3) / 2;
        var right = limit - 3 - left;
        return value[..left] + "..." + value[^right..];
    }
}
