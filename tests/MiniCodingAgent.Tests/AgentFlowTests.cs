using System.Text.Json.Nodes;
using MiniCodingAgent.Agent;
using MiniCodingAgent.Models;
using MiniCodingAgent.Sessions;
using MiniCodingAgent.Tests.Helpers;
using MiniCodingAgent.Tools;

namespace MiniCodingAgent.Tests;

public sealed class AgentFlowTests
{
    [Fact]
    public void Agent_runs_tool_then_final()
    {
        using var tmp = new TempDirectory();
        File.WriteAllText(tmp.Combine("hello.txt"), "alpha\nbeta\n");
        var agent = TestHarness.BuildAgent(tmp.Path, new[]
        {
            "<tool>{\"name\":\"read_file\",\"args\":{\"path\":\"hello.txt\",\"start\":1,\"end\":2}}</tool>",
            "<final>Read the file successfully.</final>",
        });

        var answer = agent.Ask("Inspect hello.txt");

        Assert.Equal("Read the file successfully.", answer);
        Assert.Contains(agent.Session.History, item => item.Role == HistoryRoles.Tool && item.Name == "read_file");
        Assert.Contains("hello.txt", agent.Session.Memory.Files);
    }

    [Fact]
    public void Agent_retries_after_empty_model_output()
    {
        using var tmp = new TempDirectory();
        var agent = TestHarness.BuildAgent(tmp.Path, new[]
        {
            string.Empty,
            "<final>Recovered after retry.</final>",
        });

        var answer = agent.Ask("Do the task");

        Assert.Equal("Recovered after retry.", answer);
        var notices = agent.Session.History
            .Where(item => item.Role == HistoryRoles.Assistant)
            .Select(item => item.Content)
            .ToList();
        Assert.Contains(notices, text => text.Contains("empty response"));
    }

    [Fact]
    public void Agent_retries_after_malformed_tool_payload()
    {
        using var tmp = new TempDirectory();
        File.WriteAllText(tmp.Combine("hello.txt"), "alpha\n");
        var agent = TestHarness.BuildAgent(tmp.Path, new[]
        {
            "<tool>{\"name\":\"read_file\",\"args\":\"bad\"}</tool>",
            "<tool>{\"name\":\"read_file\",\"args\":{\"path\":\"hello.txt\",\"start\":1,\"end\":1}}</tool>",
            "<final>Recovered after malformed tool output.</final>",
        });

        var answer = agent.Ask("Inspect hello.txt");

        Assert.Equal("Recovered after malformed tool output.", answer);
        Assert.Contains(agent.Session.History, item => item.Role == HistoryRoles.Tool && item.Name == "read_file");
        var notices = agent.Session.History
            .Where(item => item.Role == HistoryRoles.Assistant)
            .Select(item => item.Content)
            .ToList();
        Assert.Contains(notices, text => text.Contains("valid <tool> call"));
    }

    [Fact]
    public void Agent_accepts_xml_write_file_tool()
    {
        using var tmp = new TempDirectory();
        var agent = TestHarness.BuildAgent(tmp.Path, new[]
        {
            "<tool name=\"write_file\" path=\"hello.py\"><content>print(\"hi\")\n</content></tool>",
            "<final>Done.</final>",
        });

        var answer = agent.Ask("Create hello.py");

        Assert.Equal("Done.", answer);
        Assert.Equal("print(\"hi\")\n", File.ReadAllText(tmp.Combine("hello.py")));
    }

    [Fact]
    public void Retries_do_not_consume_the_whole_budget()
    {
        using var tmp = new TempDirectory();
        var agent = TestHarness.BuildAgent(tmp.Path, new[]
        {
            string.Empty,
            string.Empty,
            "<final>Recovered after several retries.</final>",
        }, maxSteps: 1);

        var answer = agent.Ask("Do the task");

        Assert.Equal("Recovered after several retries.", answer);
    }

    [Fact]
    public void Agent_saves_and_resumes_session()
    {
        using var tmp = new TempDirectory();
        var agent = TestHarness.BuildAgent(tmp.Path, new[] { "<final>First pass.</final>" });
        Assert.Equal("First pass.", agent.Ask("Start a session"));

        var resumed = MiniAgent.FromSession(
            modelClient: new FakeModelClient(new[] { "<final>Resumed.</final>" }),
            workspace: agent.Workspace,
            sessionStore: new SessionStore(Path.Combine(tmp.Path, ".mini-coding-agent", "sessions")),
            sessionId: agent.Session.Id,
            options: new AgentOptions { ApprovalPolicy = ApprovalPolicy.Auto });

        Assert.Equal("Start a session", resumed.Session.History[0].Content);
        Assert.Equal("Resumed.", resumed.Ask("Continue"));
    }

    [Fact]
    public void Delegate_uses_child_agent()
    {
        using var tmp = new TempDirectory();
        var agent = TestHarness.BuildAgent(tmp.Path, new[]
        {
            "<tool>{\"name\":\"delegate\",\"args\":{\"task\":\"inspect README\",\"max_steps\":2}}</tool>",
            "<final>Child result.</final>",
            "<final>Parent incorporated the child result.</final>",
        });

        var answer = agent.Ask("Use delegation");

        Assert.Equal("Parent incorporated the child result.", answer);
        var toolEvents = agent.Session.History.Where(item => item.Role == HistoryRoles.Tool).ToList();
        Assert.Equal("delegate", toolEvents[0].Name);
        Assert.Contains("delegate_result", toolEvents[0].Content);
    }

    [Fact]
    public void Patch_file_replaces_exact_match()
    {
        using var tmp = new TempDirectory();
        var filePath = tmp.Combine("sample.txt");
        File.WriteAllText(filePath, "hello world\n");
        var agent = TestHarness.BuildAgent(tmp.Path, Array.Empty<string>());

        var result = agent.RunTool("patch_file", new JsonObject
        {
            ["path"] = "sample.txt",
            ["old_text"] = "world",
            ["new_text"] = "agent",
        });

        Assert.Equal("patched sample.txt", result);
        Assert.Equal("hello agent\n", File.ReadAllText(filePath));
    }

    [Fact]
    public void Invalid_risky_tool_does_not_prompt_for_approval()
    {
        using var tmp = new TempDirectory();
        var workspace = TestHarness.BuildWorkspace(tmp.Path);
        var store = new SessionStore(Path.Combine(tmp.Path, ".mini-coding-agent", "sessions"));
        var approval = new CountingApprovalHandler();
        var agent = new MiniAgent(
            modelClient: new FakeModelClient(Array.Empty<string>()),
            workspace: workspace,
            sessionStore: store,
            options: new AgentOptions { ApprovalPolicy = ApprovalPolicy.Ask },
            approvalHandler: approval);

        var result = agent.RunTool("write_file", new JsonObject());

        Assert.StartsWith("error: invalid arguments for write_file: path", result);
        Assert.Contains("example: <tool name=\"write_file\"", result);
        Assert.Equal(0, approval.Calls);
    }

    [Fact]
    public void List_files_hides_internal_agent_state()
    {
        using var tmp = new TempDirectory();
        Directory.CreateDirectory(tmp.Combine(".mini-coding-agent"));
        Directory.CreateDirectory(tmp.Combine(".git"));
        File.WriteAllText(tmp.Combine("hello.txt"), "hi\n");
        var agent = TestHarness.BuildAgent(tmp.Path, Array.Empty<string>());

        var result = agent.RunTool("list_files", new JsonObject());

        Assert.DoesNotContain(".mini-coding-agent", result);
        Assert.DoesNotContain(".git", result);
        Assert.Contains("[F] hello.txt", result);
    }

    [Fact]
    public void Repeated_identical_tool_call_is_rejected()
    {
        using var tmp = new TempDirectory();
        var agent = TestHarness.BuildAgent(tmp.Path, Array.Empty<string>());

        // Prime the session with two identical tool events.
        agent.Session.History.Add(HistoryItem.ToolMessage("list_files", new JsonObject(), "(empty)", "1"));
        agent.Session.History.Add(HistoryItem.ToolMessage("list_files", new JsonObject(), "(empty)", "2"));

        var result = agent.RunTool("list_files", new JsonObject());

        Assert.Equal(
            "error: repeated identical tool call for list_files; choose a different tool or return a final answer",
            result);
    }

    private sealed class CountingApprovalHandler : IApprovalHandler
    {
        public int Calls { get; private set; }
        public bool Approve(string toolName, JsonObject args)
        {
            Calls++;
            return true;
        }
    }
}
