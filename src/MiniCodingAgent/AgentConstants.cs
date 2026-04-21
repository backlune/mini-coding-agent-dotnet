namespace MiniCodingAgent;

/// <summary>
/// Global constants used across the agent. Keeping them in one place mirrors
/// the top-of-file constants from the Python reference implementation.
/// </summary>
public static class AgentConstants
{
    /// <summary>Names of project documents surfaced in the workspace snapshot.</summary>
    public static readonly IReadOnlyList<string> DocNames = new[]
    {
        "AGENTS.md",
        "README.md",
        "pyproject.toml",
        "package.json",
    };

    /// <summary>Maximum characters kept from a single tool result.</summary>
    public const int MaxToolOutput = 4000;

    /// <summary>Maximum characters kept in the transcript passed to the model.</summary>
    public const int MaxHistory = 12000;

    /// <summary>Directory and archive names excluded from listings and searches.</summary>
    public static readonly IReadOnlySet<string> IgnoredPathNames = new HashSet<string>(StringComparer.Ordinal)
    {
        ".git",
        ".mini-coding-agent",
        "__pycache__",
        ".pytest_cache",
        ".ruff_cache",
        ".venv",
        "venv",
        "bin",
        "obj",
    };

    /// <summary>One-liner summary of slash commands for the welcome banner.</summary>
    public const string HelpText = "/help, /memory, /session, /reset, /exit";

    /// <summary>Cat ASCII art shown in the welcome banner.</summary>
    public static readonly IReadOnlyList<string> WelcomeArt = new[]
    {
        @"/\     /\",
        @"{  `---'  }",
        @"{  O   O  }",
        @"~~>  V  <~~",
        @"\\  \|/  /",
        @"`-----'__",
    };

    /// <summary>Detailed slash command help shown on <c>/help</c>.</summary>
    public const string HelpDetails =
        "Commands:\n" +
        "/help    Show this help message.\n" +
        "/memory  Show the agent's distilled working memory.\n" +
        "/session Show the path to the saved session file.\n" +
        "/reset   Clear the current session history and memory.\n" +
        "/exit    Exit the agent.";
}
