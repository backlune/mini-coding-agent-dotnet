using System.Text.Json.Serialization;

namespace MiniCodingAgent.Sessions;

/// <summary>
/// Distilled working memory kept alongside the transcript. Carefully sized to
/// fit in the model prompt without re-sending the entire history.
/// </summary>
public sealed class SessionMemory
{
    [JsonPropertyName("task")]
    public string Task { get; set; } = string.Empty;

    [JsonPropertyName("files")]
    public List<string> Files { get; set; } = new();

    [JsonPropertyName("notes")]
    public List<string> Notes { get; set; } = new();
}
