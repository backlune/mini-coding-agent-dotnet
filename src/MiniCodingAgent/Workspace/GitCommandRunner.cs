using System.Diagnostics;

namespace MiniCodingAgent.Workspace;

/// <summary>
/// Tiny wrapper around the <c>git</c> CLI. Swallows failures and returns
/// fallback text so that the agent keeps running in non-git workspaces.
/// </summary>
public interface IGitCommandRunner
{
    string Run(string workingDirectory, string[] args, string fallback = "");
}

public sealed class GitCommandRunner : IGitCommandRunner
{
    private readonly TimeSpan _timeout;

    public GitCommandRunner(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public string Run(string workingDirectory, string[] args, string fallback = "")
    {
        try
        {
            var startInfo = new ProcessStartInfo("git")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return fallback;
            }

            if (!process.WaitForExit((int)_timeout.TotalMilliseconds))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
                return fallback;
            }

            if (process.ExitCode != 0)
            {
                return fallback;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            return string.IsNullOrEmpty(output) ? fallback : output;
        }
        catch
        {
            return fallback;
        }
    }
}
