using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools.Implementations;

/// <summary>
/// <c>list_files(path='.')</c>: shallow, sorted directory listing that hides
/// common cache/internal directories (<c>.git</c>, <c>.mini-coding-agent</c>, ...).
/// </summary>
public sealed class ListFilesTool : ITool
{
    private readonly WorkspacePathResolver _paths;

    public ListFilesTool(WorkspacePathResolver paths) => _paths = paths;

    public string Name => "list_files";
    public string Description => "List files in the workspace.";
    public bool IsRisky => false;
    public IReadOnlyList<ToolParameter> Schema => new[]
    {
        new ToolParameter("path", "str='.'"),
    };
    public string Example => "<tool>{\"name\":\"list_files\",\"args\":{\"path\":\".\"}}</tool>";

    public void Validate(JsonObject args)
    {
        var path = _paths.Resolve(ToolArgs.GetString(args, "path", "."));
        if (!Directory.Exists(path))
        {
            throw new ArgumentException("path is not a directory");
        }
    }

    public string Run(JsonObject args)
    {
        var path = _paths.Resolve(ToolArgs.GetString(args, "path", "."));

        var entries = new DirectoryInfo(path)
            .EnumerateFileSystemInfos()
            .Where(entry => !AgentConstants.IgnoredPathNames.Contains(entry.Name))
            .OrderBy(entry => entry is FileInfo)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToList();

        if (entries.Count == 0)
        {
            return "(empty)";
        }

        return string.Join("\n", entries.Select(entry =>
        {
            var kind = entry is DirectoryInfo ? "[D]" : "[F]";
            return $"{kind} {_paths.Relative(entry.FullName)}";
        }));
    }
}
