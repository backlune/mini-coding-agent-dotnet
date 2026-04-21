using System.Text.Json.Nodes;
using MiniCodingAgent.Models;
using MiniCodingAgent.Sessions;
using MiniCodingAgent.Tools;
using MiniCodingAgent.Tools.Implementations;
using MiniCodingAgent.Utilities;
using MiniCodingAgent.Workspace;

namespace MiniCodingAgent.Agent;

/// <summary>
/// Top-level orchestrator. Ties together every component:
/// workspace context, prompt building, tool execution, approval, transcript
/// persistence, and bounded delegation.
/// </summary>
public sealed class MiniAgent : ISubAgentRunner
{
    private readonly IModelClient _modelClient;
    private readonly SessionStore _sessionStore;
    private readonly IApprovalHandler _approvalHandler;
    private readonly IClock _clock;
    private readonly ToolRegistry _tools;
    private readonly PromptBuilder _promptBuilder;
    private readonly WorkspacePathResolver _paths;

    public MiniAgent(
        IModelClient modelClient,
        WorkspaceContext workspace,
        SessionStore sessionStore,
        AgentOptions? options = null,
        Session? session = null,
        IApprovalHandler? approvalHandler = null,
        IClock? clock = null)
    {
        _modelClient = modelClient;
        Workspace = workspace;
        _sessionStore = sessionStore;
        Options = options ?? new AgentOptions();
        _clock = clock ?? SystemClock.Instance;
        _approvalHandler = approvalHandler ?? DefaultApprovalHandler(Options.ApprovalPolicy);

        _paths = new WorkspacePathResolver(workspace.RepoRoot);
        _tools = BuildTools(_paths, this, Options);
        _promptBuilder = new PromptBuilder(_tools, workspace);

        Session = session ?? CreateSession(workspace);
        SessionPath = _sessionStore.Save(Session);
    }

    public WorkspaceContext Workspace { get; }
    public AgentOptions Options { get; }
    public Session Session { get; }
    public string SessionPath { get; private set; }
    public ToolRegistry Tools => _tools;

    /// <summary>
    /// Construct an agent from a persisted session id. Delegates to the main
    /// constructor after loading; exposed for tests and the CLI <c>--resume</c>.
    /// </summary>
    public static MiniAgent FromSession(
        IModelClient modelClient,
        WorkspaceContext workspace,
        SessionStore sessionStore,
        string sessionId,
        AgentOptions? options = null,
        IApprovalHandler? approvalHandler = null,
        IClock? clock = null)
    {
        var session = sessionStore.Load(sessionId);
        return new MiniAgent(modelClient, workspace, sessionStore, options, session, approvalHandler, clock);
    }

    /// <summary>
    /// Main entry point. Runs the tool/model loop until the model emits a
    /// <c>&lt;final&gt;</c> answer or the step/retry budget is exhausted.
    /// </summary>
    public string Ask(string userMessage)
    {
        var memory = Session.Memory;
        if (string.IsNullOrEmpty(memory.Task))
        {
            memory.Task = TextHelpers.Clip((userMessage ?? string.Empty).Trim(), 300);
        }

        Record(HistoryItem.UserMessage(userMessage ?? string.Empty, Now()));

        var toolSteps = 0;
        var attempts = 0;
        var maxAttempts = Math.Max(Options.MaxSteps * 3, Options.MaxSteps + 4);

        while (toolSteps < Options.MaxSteps && attempts < maxAttempts)
        {
            attempts++;

            var prompt = _promptBuilder.BuildPrompt(
                memory,
                HistoryFormatter.Render(Session.History),
                userMessage ?? string.Empty);

            var raw = _modelClient.Complete(prompt, Options.MaxNewTokens);
            var parsed = ResponseParser.Parse(raw);

            if (parsed.Kind == ParsedResponseKind.Tool)
            {
                toolSteps++;
                var name = parsed.ToolName!;
                var args = parsed.ToolArgs ?? new JsonObject();
                var result = RunTool(name, args);
                Record(HistoryItem.ToolMessage(name, (JsonObject)args.DeepClone(), result, Now()));
                NoteTool(name, args, result);
                continue;
            }

            if (parsed.Kind == ParsedResponseKind.Retry)
            {
                Record(HistoryItem.AssistantMessage(parsed.RetryNotice ?? string.Empty, Now()));
                continue;
            }

            var final = (parsed.Final ?? raw ?? string.Empty).Trim();
            Record(HistoryItem.AssistantMessage(final, Now()));
            Remember(memory.Notes, TextHelpers.Clip(final, 220), 5);
            return final;
        }

        var stopped = (attempts >= maxAttempts && toolSteps < Options.MaxSteps)
            ? "Stopped after too many malformed model responses without a valid tool call or final answer."
            : "Stopped after reaching the step limit without a final answer.";
        Record(HistoryItem.AssistantMessage(stopped, Now()));
        return stopped;
    }

    /// <summary>
    /// Executes a single tool by name. Intended for agent use; exposed for
    /// targeted tool tests.
    /// </summary>
    public string RunTool(string name, JsonObject args)
    {
        var tool = _tools.Get(name);
        if (tool is null)
        {
            return $"error: unknown tool '{name}'";
        }

        try
        {
            tool.Validate(args);
        }
        catch (Exception ex)
        {
            var message = $"error: invalid arguments for {name}: {ex.Message}";
            if (!string.IsNullOrEmpty(tool.Example))
            {
                message += $"\nexample: {tool.Example}";
            }
            return message;
        }

        if (IsRepeatedToolCall(name, args))
        {
            return $"error: repeated identical tool call for {name}; choose a different tool or return a final answer";
        }

        if (tool.IsRisky && !Approve(tool, args))
        {
            return $"error: approval denied for {name}";
        }

        try
        {
            return TextHelpers.Clip(tool.Run(args), AgentConstants.MaxToolOutput);
        }
        catch (Exception ex)
        {
            return $"error: tool {name} failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Wipes the session transcript and distilled memory but keeps the session
    /// id, so subsequent writes append to the same JSON file.
    /// </summary>
    public void Reset()
    {
        Session.History.Clear();
        Session.Memory = new SessionMemory();
        _sessionStore.Save(Session);
    }

    string ISubAgentRunner.Run(string task, int maxSteps)
    {
        if (Options.Depth >= Options.MaxDepth)
        {
            throw new InvalidOperationException("delegate depth exceeded");
        }

        var childOptions = new AgentOptions
        {
            ApprovalPolicy = ApprovalPolicy.Never,
            MaxSteps = maxSteps,
            MaxNewTokens = Options.MaxNewTokens,
            Depth = Options.Depth + 1,
            MaxDepth = Options.MaxDepth,
            ReadOnly = true,
        };
        var child = new MiniAgent(
            _modelClient,
            Workspace,
            _sessionStore,
            childOptions,
            session: null,
            approvalHandler: new StaticApprovalHandler(false),
            clock: _clock);

        child.Session.Memory.Task = task;
        child.Session.Memory.Notes = new List<string>
        {
            TextHelpers.Clip(HistoryFormatter.Render(Session.History), 300),
        };

        return child.Ask(task);
    }

    private void Record(HistoryItem item)
    {
        Session.History.Add(item);
        SessionPath = _sessionStore.Save(Session);
    }

    private void NoteTool(string name, JsonObject args, string result)
    {
        var memory = Session.Memory;
        var path = args["path"]?.ToString();
        if (!string.IsNullOrEmpty(path) && (name == "read_file" || name == "write_file" || name == "patch_file"))
        {
            Remember(memory.Files, path!, 8);
        }
        var note = $"{name}: {TextHelpers.Clip(result.Replace('\n', ' '), 220)}";
        Remember(memory.Notes, note, 5);
    }

    private bool IsRepeatedToolCall(string name, JsonObject args)
    {
        var toolEvents = Session.History.Where(item => item.Role == HistoryRoles.Tool).ToList();
        if (toolEvents.Count < 2)
        {
            return false;
        }
        var last = toolEvents[^1];
        var previous = toolEvents[^2];
        return last.Name == name && previous.Name == name
            && last.ArgsEqual(args) && previous.ArgsEqual(args);
    }

    private bool Approve(ITool tool, JsonObject args)
    {
        if (Options.ReadOnly || Options.ApprovalPolicy == ApprovalPolicy.Never)
        {
            return false;
        }
        if (Options.ApprovalPolicy == ApprovalPolicy.Auto)
        {
            return true;
        }
        return _approvalHandler.Approve(tool.Name, args);
    }

    private static void Remember(List<string> bucket, string item, int limit)
    {
        if (string.IsNullOrEmpty(item))
        {
            return;
        }
        bucket.Remove(item);
        bucket.Add(item);
        if (bucket.Count > limit)
        {
            bucket.RemoveRange(0, bucket.Count - limit);
        }
    }

    private string Now() => _clock.UtcNow().ToString("o");

    private static Session CreateSession(WorkspaceContext workspace)
    {
        var id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6];
        return new Session
        {
            Id = id,
            CreatedAt = DateTimeOffset.UtcNow.ToString("o"),
            WorkspaceRoot = workspace.RepoRoot,
            History = new List<HistoryItem>(),
            Memory = new SessionMemory(),
        };
    }

    private static IApprovalHandler DefaultApprovalHandler(ApprovalPolicy policy) =>
        policy == ApprovalPolicy.Ask ? new ConsoleApprovalHandler() : new StaticApprovalHandler(policy == ApprovalPolicy.Auto);

    private static ToolRegistry BuildTools(WorkspacePathResolver paths, MiniAgent agent, AgentOptions options)
    {
        var tools = new List<ITool>
        {
            new ListFilesTool(paths),
            new ReadFileTool(paths),
            new SearchTool(paths),
            new RunShellTool(paths),
            new WriteFileTool(paths),
            new PatchFileTool(paths),
        };
        if (options.Depth < options.MaxDepth)
        {
            tools.Add(new DelegateTool(agent));
        }
        return new ToolRegistry(tools);
    }
}
