using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools;

/// <summary>
/// Decides whether a risky tool invocation is allowed to run. The console
/// implementation prompts the user; tests provide static yes/no stubs.
/// </summary>
public interface IApprovalHandler
{
    bool Approve(string toolName, JsonObject args);
}

/// <summary>
/// Always returns the same answer. Handy for "never" policies and tests.
/// </summary>
public sealed class StaticApprovalHandler : IApprovalHandler
{
    private readonly bool _result;
    public StaticApprovalHandler(bool result) => _result = result;
    public bool Approve(string toolName, JsonObject args) => _result;
}
