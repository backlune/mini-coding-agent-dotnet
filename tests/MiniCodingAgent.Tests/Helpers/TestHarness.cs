using MiniCodingAgent.Agent;
using MiniCodingAgent.Models;
using MiniCodingAgent.Sessions;
using MiniCodingAgent.Tools;
using MiniCodingAgent.Workspace;

namespace MiniCodingAgent.Tests.Helpers;

/// <summary>
/// Shared helpers that build a <see cref="WorkspaceContext"/> and a
/// <see cref="MiniAgent"/> fed by a <see cref="FakeModelClient"/>. Mirrors the
/// <c>build_workspace</c>/<c>build_agent</c> helpers in the Python test suite.
/// </summary>
public static class TestHarness
{
    public static WorkspaceContext BuildWorkspace(string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "README.md"), "demo\n");
        return new WorkspaceContextBuilder(new NullGitRunner(directory)).Build(directory);
    }

    public static MiniAgent BuildAgent(
        string directory,
        IEnumerable<string> outputs,
        ApprovalPolicy approval = ApprovalPolicy.Auto,
        int? maxSteps = null,
        int? maxDepth = null)
    {
        var workspace = BuildWorkspace(directory);
        var store = new SessionStore(Path.Combine(directory, ".mini-coding-agent", "sessions"));
        var options = new AgentOptions
        {
            ApprovalPolicy = approval,
            MaxSteps = maxSteps ?? 6,
            MaxDepth = maxDepth ?? 1,
        };
        return new MiniAgent(
            modelClient: new FakeModelClient(outputs),
            workspace: workspace,
            sessionStore: store,
            options: options);
    }
}

/// <summary>
/// Git runner stub used by tests: returns blank output for every invocation so
/// the suite doesn't shell out to the host machine's <c>git</c> (which might
/// fail on a bare <see cref="Path.GetTempPath"/> directory).
/// </summary>
internal sealed class NullGitRunner : IGitCommandRunner
{
    private readonly string _fallbackRoot;
    public NullGitRunner(string fallbackRoot) => _fallbackRoot = fallbackRoot;

    public string Run(string workingDirectory, string[] args, string fallback = "")
    {
        if (args.Length >= 2 && args[0] == "rev-parse" && args[1] == "--show-toplevel")
        {
            return _fallbackRoot;
        }
        return fallback;
    }
}
