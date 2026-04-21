using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools.Implementations;

/// <summary>
/// <c>search(pattern, path='.')</c>: delegates to <c>rg</c> when installed and
/// otherwise falls back to a naive case-insensitive substring scan.
/// </summary>
public sealed class SearchTool : ITool
{
    private readonly WorkspacePathResolver _paths;

    public SearchTool(WorkspacePathResolver paths) => _paths = paths;

    public string Name => "search";
    public string Description => "Search the workspace with rg or a simple fallback.";
    public bool IsRisky => false;
    public IReadOnlyList<ToolParameter> Schema => new[]
    {
        new ToolParameter("pattern", "str"),
        new ToolParameter("path", "str='.'"),
    };
    public string Example => "<tool>{\"name\":\"search\",\"args\":{\"pattern\":\"binary_search\",\"path\":\".\"}}</tool>";

    public void Validate(JsonObject args)
    {
        var pattern = ToolArgs.GetString(args, "pattern", string.Empty).Trim();
        if (pattern.Length == 0)
        {
            throw new ArgumentException("pattern must not be empty");
        }
        _paths.Resolve(ToolArgs.GetString(args, "path", "."));
    }

    public string Run(JsonObject args)
    {
        var pattern = ToolArgs.GetString(args, "pattern", string.Empty).Trim();
        var path = _paths.Resolve(ToolArgs.GetString(args, "path", "."));

        var rg = FindOnPath("rg");
        if (rg is not null)
        {
            return RunRipgrep(rg, pattern, path);
        }

        return RunFallback(pattern, path);
    }

    private static string RunRipgrep(string rgExecutable, string pattern, string path)
    {
        var startInfo = new ProcessStartInfo(rgExecutable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in new[] { "-n", "--smart-case", "--max-count", "200", pattern, path })
        {
            startInfo.ArgumentList.Add(arg);
        }
        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var stdoutTrim = stdout.Trim();
        if (!string.IsNullOrEmpty(stdoutTrim))
        {
            return stdoutTrim;
        }
        var stderrTrim = stderr.Trim();
        return string.IsNullOrEmpty(stderrTrim) ? "(no matches)" : stderrTrim;
    }

    private string RunFallback(string pattern, string path)
    {
        var matches = new List<string>();
        IEnumerable<string> files = File.Exists(path)
            ? new[] { path }
            : EnumerateTextFiles(path);

        foreach (var file in files)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file, Encoding.UTF8);
            }
            catch
            {
                continue;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add($"{_paths.Relative(file)}:{i + 1}:{lines[i]}");
                    if (matches.Count >= 200)
                    {
                        return string.Join("\n", matches);
                    }
                }
            }
        }

        return matches.Count == 0 ? "(no matches)" : string.Join("\n", matches);
    }

    private static IEnumerable<string> EnumerateTextFiles(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            var parts = Path.GetRelativePath(path, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Any(part => AgentConstants.IgnoredPathNames.Contains(part)))
            {
                continue;
            }
            yield return file;
        }
    }

    private static string? FindOnPath(string name)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
        {
            return null;
        }

        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE").Split(';')
            : new[] { string.Empty };

        foreach (var directory in pathVar.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(directory, name + ext);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        return null;
    }
}
