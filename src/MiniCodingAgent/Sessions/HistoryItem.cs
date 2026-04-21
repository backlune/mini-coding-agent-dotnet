using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MiniCodingAgent.Sessions;

/// <summary>
/// Roles used in the transcript. Kept as strings on disk so older sessions
/// deserialise cleanly even if new roles are added.
/// </summary>
public static class HistoryRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string Tool = "tool";
}

/// <summary>
/// A single turn in the transcript. <see cref="Name"/>/<see cref="Args"/> are
/// populated only for tool events; all other roles leave them blank.
/// </summary>
public sealed class HistoryItem
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Tool arguments as originally passed by the model. Stored as a raw JSON
    /// object so we can compare structurally in <c>repeated_tool_call</c>.
    /// </summary>
    [JsonPropertyName("args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonObject? Args { get; set; }

    public static HistoryItem UserMessage(string content, string createdAt) => new()
    {
        Role = HistoryRoles.User,
        Content = content,
        CreatedAt = createdAt,
    };

    public static HistoryItem AssistantMessage(string content, string createdAt) => new()
    {
        Role = HistoryRoles.Assistant,
        Content = content,
        CreatedAt = createdAt,
    };

    public static HistoryItem ToolMessage(string name, JsonObject args, string content, string createdAt) => new()
    {
        Role = HistoryRoles.Tool,
        Name = name,
        Args = args,
        Content = content,
        CreatedAt = createdAt,
    };

    /// <summary>
    /// Compares the <see cref="Args"/> object to <paramref name="other"/> by
    /// re-serialising both sides with sorted keys. Mirrors Python's dict equality.
    /// </summary>
    public bool ArgsEqual(JsonObject? other)
    {
        var left = Canonicalise(Args);
        var right = Canonicalise(other);
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    public static string Canonicalise(JsonObject? obj)
    {
        if (obj is null)
        {
            return "{}";
        }
        var ordered = new JsonObject();
        foreach (var key in obj.Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal))
        {
            ordered[key] = obj[key]?.DeepClone();
        }
        return ordered.ToJsonString(JsonWriterOptions);
    }

    private static readonly JsonSerializerOptions JsonWriterOptions = new()
    {
        WriteIndented = false,
    };
}
