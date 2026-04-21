using System.Globalization;
using MiniCodingAgent.Tools;

namespace MiniCodingAgent.Cli;

/// <summary>
/// Minimal hand-rolled CLI parser. Avoids a runtime dependency on
/// <c>System.CommandLine</c> while still supporting both <c>--foo bar</c> and
/// <c>--foo=bar</c>.
/// </summary>
public static class CliArgumentParser
{
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        var positional = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (token is "-h" or "--help")
            {
                options.HelpRequested = true;
                continue;
            }

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(token);
                continue;
            }

            var key = token[2..];
            string? value = null;
            var eq = key.IndexOf('=');
            if (eq >= 0)
            {
                value = key[(eq + 1)..];
                key = key[..eq];
            }
            else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = args[++i];
            }

            ApplyFlag(options, key, value);
        }

        if (positional.Count > 0)
        {
            options.Prompt = string.Join(" ", positional);
        }
        return options;
    }

    public static string HelpText() =>
        "Minimal coding agent for Ollama models.\n\n" +
        "usage: mini-coding-agent [prompt...] [options]\n\n" +
        "options:\n" +
        "  --cwd <dir>              Workspace directory (default: .)\n" +
        "  --model <name>           Ollama model name (default: qwen3.5:4b)\n" +
        "  --host <url>             Ollama server URL (default: http://127.0.0.1:11434)\n" +
        "  --ollama-timeout <secs>  Ollama request timeout (default: 300)\n" +
        "  --resume <id|latest>     Resume a saved session (default: start new session)\n" +
        "  --approval <mode>        Approval policy: ask, auto, never (default: ask)\n" +
        "  --max-steps <n>          Maximum tool/model iterations per request (default: 6)\n" +
        "  --max-new-tokens <n>     Maximum model output tokens per step (default: 512)\n" +
        "  --temperature <val>      Sampling temperature (default: 0.2)\n" +
        "  --top-p <val>            Top-p sampling value (default: 0.9)\n" +
        "  -h, --help               Show this help message.";

    private static void ApplyFlag(CliOptions options, string key, string? value)
    {
        switch (key)
        {
            case "cwd":
                options.Cwd = RequireValue(key, value);
                break;
            case "model":
                options.Model = RequireValue(key, value);
                break;
            case "host":
                options.Host = RequireValue(key, value);
                break;
            case "ollama-timeout":
                options.OllamaTimeoutSeconds = ParseInt(key, value);
                break;
            case "resume":
                options.Resume = RequireValue(key, value);
                break;
            case "approval":
                if (!ApprovalPolicyExtensions.TryParse(value, out var policy))
                {
                    throw new ArgumentException($"--approval must be one of: ask, auto, never (got '{value}')");
                }
                options.Approval = policy;
                break;
            case "max-steps":
                options.MaxSteps = ParseInt(key, value);
                break;
            case "max-new-tokens":
                options.MaxNewTokens = ParseInt(key, value);
                break;
            case "temperature":
                options.Temperature = ParseDouble(key, value);
                break;
            case "top-p":
                options.TopP = ParseDouble(key, value);
                break;
            default:
                throw new ArgumentException($"Unknown option: --{key}");
        }
    }

    private static string RequireValue(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException($"--{key} requires a value");
        }
        return value;
    }

    private static int ParseInt(string key, string? value)
    {
        if (!int.TryParse(RequireValue(key, value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"--{key} must be an integer (got '{value}')");
        }
        return result;
    }

    private static double ParseDouble(string key, string? value)
    {
        if (!double.TryParse(RequireValue(key, value), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"--{key} must be a number (got '{value}')");
        }
        return result;
    }
}
