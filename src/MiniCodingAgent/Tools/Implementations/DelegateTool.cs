using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools.Implementations;

/// <summary>
/// <c>delegate(task, max_steps=3)</c>: spawns a bounded, read-only child agent.
/// Only registered when the parent still has delegation depth available.
/// </summary>
public sealed class DelegateTool : ITool
{
    private readonly ISubAgentRunner _runner;

    public DelegateTool(ISubAgentRunner runner) => _runner = runner;

    public string Name => "delegate";
    public string Description => "Ask a bounded read-only child agent to investigate.";
    public bool IsRisky => false;
    public IReadOnlyList<ToolParameter> Schema => new[]
    {
        new ToolParameter("task", "str"),
        new ToolParameter("max_steps", "int=3"),
    };
    public string Example =>
        "<tool>{\"name\":\"delegate\",\"args\":{\"task\":\"inspect README.md\",\"max_steps\":3}}</tool>";

    public void Validate(JsonObject args)
    {
        var task = ToolArgs.GetString(args, "task", string.Empty).Trim();
        if (task.Length == 0)
        {
            throw new ArgumentException("task must not be empty");
        }
    }

    public string Run(JsonObject args)
    {
        var task = ToolArgs.GetString(args, "task", string.Empty).Trim();
        var maxSteps = ToolArgs.GetInt(args, "max_steps", 3);
        return "delegate_result:\n" + _runner.Run(task, maxSteps);
    }
}
