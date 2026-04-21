using System.Text.Json.Serialization;

namespace MiniCodingAgent.Sessions;

/// <summary>
/// On-disk representation of a single agent run. Stored as JSON under
/// <c>.mini-coding-agent/sessions/&lt;id&gt;.json</c>.
/// </summary>
public sealed class Session
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("workspace_root")]
    public string WorkspaceRoot { get; set; } = string.Empty;

    [JsonPropertyName("history")]
    public List<HistoryItem> History { get; set; } = new();

    [JsonPropertyName("memory")]
    public SessionMemory Memory { get; set; } = new();
}
