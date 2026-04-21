using System.Text.Json.Nodes;

namespace MiniCodingAgent.Agent;

/// <summary>
/// Outcome of parsing a single model response. Three shapes are distinguished:
/// a structured tool call, a final answer, or a recoverable retry notice.
/// </summary>
public enum ParsedResponseKind
{
    Tool,
    Final,
    Retry,
}

public readonly record struct ParsedResponse(
    ParsedResponseKind Kind,
    string? Final,
    string? RetryNotice,
    string? ToolName,
    JsonObject? ToolArgs)
{
    public static ParsedResponse ForTool(string name, JsonObject args) =>
        new(ParsedResponseKind.Tool, null, null, name, args);

    public static ParsedResponse ForFinal(string answer) =>
        new(ParsedResponseKind.Final, answer, null, null, null);

    public static ParsedResponse ForRetry(string notice) =>
        new(ParsedResponseKind.Retry, null, notice, null, null);
}
