using System.Text;
using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools.Implementations;

/// <summary>
/// <c>write_file(path, content)</c>: creates or overwrites a UTF-8 file.
/// Marked as risky: the agent consults the approval handler before running it.
/// </summary>
public sealed class WriteFileTool : ITool
{
    private readonly WorkspacePathResolver _paths;

    public WriteFileTool(WorkspacePathResolver paths) => _paths = paths;

    public string Name => "write_file";
    public string Description => "Write a text file.";
    public bool IsRisky => true;
    public IReadOnlyList<ToolParameter> Schema => new[]
    {
        new ToolParameter("path", "str"),
        new ToolParameter("content", "str"),
    };
    public string Example =>
        "<tool name=\"write_file\" path=\"binary_search.py\"><content>def binary_search(nums, target):\n    return -1\n</content></tool>";

    public void Validate(JsonObject args)
    {
        var path = _paths.Resolve(ToolArgs.RequireString(args, "path"));
        if (Directory.Exists(path))
        {
            throw new ArgumentException("path is a directory");
        }
        if (!ToolArgs.Contains(args, "content"))
        {
            throw new ArgumentException("missing content");
        }
    }

    public string Run(JsonObject args)
    {
        var path = _paths.Resolve(ToolArgs.RequireString(args, "path"));
        var content = ToolArgs.GetString(args, "content", string.Empty);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return $"wrote {_paths.Relative(path)} ({content.Length} chars)";
    }
}
