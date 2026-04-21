using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools;

/// <summary>
/// Contract implemented by every tool the agent can call.
/// Tools expose a <see cref="Schema"/> (for prompting the model), a
/// <see cref="IsRisky"/> flag (for approvals), and a <see cref="Run"/> method.
/// Validation errors must be thrown as exceptions; the agent converts them
/// into a structured <c>error: ...</c> string so the model can recover.
/// </summary>
public interface ITool
{
    string Name { get; }
    string Description { get; }
    bool IsRisky { get; }
    IReadOnlyList<ToolParameter> Schema { get; }
    string Example { get; }
    void Validate(JsonObject args);
    string Run(JsonObject args);
}

/// <summary>
/// Describes one parameter for prompt generation. The string
/// <see cref="Signature"/> matches the Python schema format (e.g. <c>"int=20"</c>).
/// </summary>
public readonly record struct ToolParameter(string Name, string Signature);
