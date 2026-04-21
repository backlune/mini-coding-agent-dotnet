using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools.Implementations;

/// <summary>
/// <c>run_shell(command, timeout=20)</c>: executes a command in a platform-appropriate
/// shell, capped at <c>timeout</c> seconds. Marked as risky so the approval
/// handler decides whether to run it.
/// </summary>
public sealed class RunShellTool : ITool
{
    private readonly WorkspacePathResolver _paths;

    public RunShellTool(WorkspacePathResolver paths) => _paths = paths;

    public string Name => "run_shell";
    public string Description => "Run a shell command in the repo root.";
    public bool IsRisky => true;
    public IReadOnlyList<ToolParameter> Schema => new[]
    {
        new ToolParameter("command", "str"),
        new ToolParameter("timeout", "int=20"),
    };
    public string Example =>
        "<tool>{\"name\":\"run_shell\",\"args\":{\"command\":\"dotnet test\",\"timeout\":20}}</tool>";

    public void Validate(JsonObject args)
    {
        var command = ToolArgs.GetString(args, "command", string.Empty).Trim();
        if (command.Length == 0)
        {
            throw new ArgumentException("command must not be empty");
        }
        var timeout = ToolArgs.GetInt(args, "timeout", 20);
        if (timeout < 1 || timeout > 120)
        {
            throw new ArgumentException("timeout must be in [1, 120]");
        }
    }

    public string Run(JsonObject args)
    {
        var command = ToolArgs.GetString(args, "command", string.Empty).Trim();
        var timeout = ToolArgs.GetInt(args, "timeout", 20);

        var startInfo = BuildShellProcess(command);
        startInfo.WorkingDirectory = _paths.Root;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("could not start shell process");

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdoutBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderrBuilder.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit((int)TimeSpan.FromSeconds(timeout).TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException($"command exceeded timeout of {timeout}s");
        }

        // Ensure async readers have flushed.
        process.WaitForExit();

        var stdout = stdoutBuilder.ToString().Trim();
        var stderr = stderrBuilder.ToString().Trim();
        return
            $"exit_code: {process.ExitCode}\n" +
            "stdout:\n" +
            (stdout.Length == 0 ? "(empty)" : stdout) + "\n" +
            "stderr:\n" +
            (stderr.Length == 0 ? "(empty)" : stderr);
    }

    private static ProcessStartInfo BuildShellProcess(string command)
    {
        if (OperatingSystem.IsWindows())
        {
            var psi = new ProcessStartInfo("cmd.exe");
            psi.ArgumentList.Add("/d");
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(command);
            return psi;
        }

        var shell = new ProcessStartInfo("/bin/sh");
        shell.ArgumentList.Add("-c");
        shell.ArgumentList.Add(command);
        return shell;
    }
}
