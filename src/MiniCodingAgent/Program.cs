using MiniCodingAgent;
using MiniCodingAgent.Agent;
using MiniCodingAgent.Cli;
using MiniCodingAgent.Models;
using MiniCodingAgent.Sessions;
using MiniCodingAgent.Ui;
using MiniCodingAgent.Workspace;

// Single-file Program.cs stays deliberately small: parse args, wire up the
// agent, then hand control to the REPL. Everything that resembles business
// logic lives under src/MiniCodingAgent/*.

return Run(args);

static int Run(string[] args)
{
    CliOptions options;
    try
    {
        options = CliArgumentParser.Parse(args);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Console.Error.WriteLine();
        Console.Error.WriteLine(CliArgumentParser.HelpText());
        return 2;
    }

    if (options.HelpRequested)
    {
        Console.WriteLine(CliArgumentParser.HelpText());
        return 0;
    }

    var agent = BuildAgent(options);
    Console.WriteLine(WelcomeBanner.Build(agent, options.Model, options.Host, terminalWidth: TryGetTerminalWidth()));

    if (!string.IsNullOrWhiteSpace(options.Prompt))
    {
        Console.WriteLine();
        try
        {
            Console.WriteLine(agent.Ask(options.Prompt));
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        return 0;
    }

    return InteractiveRepl.Run(agent);
}

static MiniAgent BuildAgent(CliOptions options)
{
    var workspace = new WorkspaceContextBuilder().Build(options.Cwd);
    var store = new SessionStore(Path.Combine(workspace.RepoRoot, ".mini-coding-agent", "sessions"));
    var model = new OllamaModelClient(new OllamaOptions(
        Model: options.Model,
        Host: options.Host,
        Temperature: options.Temperature,
        TopP: options.TopP,
        Timeout: TimeSpan.FromSeconds(options.OllamaTimeoutSeconds)));

    var agentOptions = new AgentOptions
    {
        ApprovalPolicy = options.Approval,
        MaxSteps = options.MaxSteps,
        MaxNewTokens = options.MaxNewTokens,
    };

    var resume = options.Resume;
    if (string.Equals(resume, "latest", StringComparison.OrdinalIgnoreCase))
    {
        resume = store.Latest();
    }

    if (!string.IsNullOrEmpty(resume))
    {
        return MiniAgent.FromSession(model, workspace, store, resume, agentOptions);
    }
    return new MiniAgent(model, workspace, store, agentOptions);
}

static int? TryGetTerminalWidth()
{
    try
    {
        return Console.WindowWidth;
    }
    catch
    {
        return null;
    }
}
