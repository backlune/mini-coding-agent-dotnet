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
    public ModelBackend Backend { get; set; } = ModelBackend.LmStudio;
    public string Model { get; set; } = "qwen3.5:4b";

    /// <summary>
    /// Host URL for the selected backend. When null, <see cref="Program"/>
    /// picks the backend-appropriate default (LM Studio: 1234, Ollama: 11434).
    /// </summary>
    public string? Host { get; set; }

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 300;
    public string? Resume { get; set; }
    public ApprovalPolicy Approval { get; set; } = ApprovalPolicy.Ask;
    public int MaxSteps { get; set; } = 6;
    public int MaxNewTokens { get; set; } = 512;
    public double Temperature { get; set; } = 0.2;
    public double TopP { get; set; } = 0.9;
    public bool HelpRequested { get; set; }
}

/// <summary>Supported local model backends.</summary>
public enum ModelBackend
{
    LmStudio,
    Ollama,
}

internal static class ModelBackendExtensions
{
    public static bool TryParse(string? value, out ModelBackend backend)
    {
        switch ((value ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "lmstudio":
            case "lm-studio":
            case "lm_studio":
                backend = ModelBackend.LmStudio;
                return true;
            case "ollama":
                backend = ModelBackend.Ollama;
                return true;
            default:
                backend = default;
                return false;
        }
    }

    public static string DefaultHost(this ModelBackend backend) => backend switch
    {
        ModelBackend.LmStudio => "http://127.0.0.1:1234",
        ModelBackend.Ollama => "http://127.0.0.1:11434",
        _ => throw new ArgumentOutOfRangeException(nameof(backend)),
    };
}
