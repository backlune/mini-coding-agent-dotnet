using System.Text;
using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools.Implementations;

/// <summary>
/// <c>read_file(path, start=1, end=200)</c>: returns a 1-based line slice of a
/// UTF-8 file, prefixed with line numbers so the model can reference lines.
/// </summary>
public sealed class ReadFileTool : ITool
{
    private readonly WorkspacePathResolver _paths;

    public ReadFileTool(WorkspacePathResolver paths) => _paths = paths;

    public string Name => "read_file";
    public string Description => "Read a UTF-8 file by line range.";
    public bool IsRisky => false;
    public IReadOnlyList<ToolParameter> Schema => new[]
    {
        new ToolParameter("path", "str"),
        new ToolParameter("start", "int=1"),
        new ToolParameter("end", "int=200"),
    };
    public string Example => "<tool>{\"name\":\"read_file\",\"args\":{\"path\":\"README.md\",\"start\":1,\"end\":80}}</tool>";

    public void Validate(JsonObject args)
    {
        var path = _paths.Resolve(ToolArgs.RequireString(args, "path"));
        if (!File.Exists(path))
        {
            throw new ArgumentException("path is not a file");
        }
        var start = ToolArgs.GetInt(args, "start", 1);
        var end = ToolArgs.GetInt(args, "end", 200);
        if (start < 1 || end < start)
        {
            throw new ArgumentException("invalid line range");
        }
    }

    public string Run(JsonObject args)
    {
        var path = _paths.Resolve(ToolArgs.RequireString(args, "path"));
        var start = ToolArgs.GetInt(args, "start", 1);
        var end = ToolArgs.GetInt(args, "end", 200);

        var lines = File.ReadAllLines(path, Encoding.UTF8);
        var builder = new StringBuilder();
        builder.Append("# ").AppendLine(_paths.Relative(path));
        var last = Math.Min(end, lines.Length);
        for (var i = start; i <= last; i++)
        {
            builder.Append(i.ToString().PadLeft(4)).Append(": ").AppendLine(lines[i - 1]);
        }
        return builder.ToString().TrimEnd('\n', '\r');
    }
}
