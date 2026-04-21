using MiniCodingAgent.Tools;

namespace MiniCodingAgent.Cli;

/// <summary>
/// Strongly-typed result of parsing the command line. Mirrors the flags in the
/// Python argparse setup so that <c>mini-coding-agent --help</c> stays familiar.
/// </summary>
public sealed class CliOptions
{
    public string Prompt { get; set; } = string.Empty;
    public string Cwd { get; set; } = ".";
    public string Model { get; set; } = "qwen3.5:4b";
    public string Host { get; set; } = "http://127.0.0.1:11434";
    public int OllamaTimeoutSeconds { get; set; } = 300;
    public string? Resume { get; set; }
    public ApprovalPolicy Approval { get; set; } = ApprovalPolicy.Ask;
    public int MaxSteps { get; set; } = 6;
    public int MaxNewTokens { get; set; } = 512;
    public double Temperature { get; set; } = 0.2;
    public double TopP { get; set; } = 0.9;
    public bool HelpRequested { get; set; }
}
