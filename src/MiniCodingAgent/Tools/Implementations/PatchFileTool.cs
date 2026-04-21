using System.Text;
using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools.Implementations;

/// <summary>
/// <c>patch_file(path, old_text, new_text)</c>: swap one unique occurrence of
/// <c>old_text</c> for <c>new_text</c>. Fails if the match is ambiguous or
/// missing so the model cannot silently rewrite unrelated code.
/// </summary>
public sealed class PatchFileTool : ITool
{
    private readonly WorkspacePathResolver _paths;

    public PatchFileTool(WorkspacePathResolver paths) => _paths = paths;

    public string Name => "patch_file";
    public string Description => "Replace one exact text block in a file.";
    public bool IsRisky => true;
    public IReadOnlyList<ToolParameter> Schema => new[]
    {
        new ToolParameter("path", "str"),
        new ToolParameter("old_text", "str"),
        new ToolParameter("new_text", "str"),
    };
    public string Example =>
        "<tool name=\"patch_file\" path=\"binary_search.py\"><old_text>return -1</old_text><new_text>return mid</new_text></tool>";

    public void Validate(JsonObject args)
    {
        var path = _paths.Resolve(ToolArgs.RequireString(args, "path"));
        if (!File.Exists(path))
        {
            throw new ArgumentException("path is not a file");
        }
        var oldText = ToolArgs.GetString(args, "old_text", string.Empty);
        if (string.IsNullOrEmpty(oldText))
        {
            throw new ArgumentException("old_text must not be empty");
        }
        if (!ToolArgs.Contains(args, "new_text"))
        {
            throw new ArgumentException("missing new_text");
        }

        var text = File.ReadAllText(path, Encoding.UTF8);
        var count = CountOccurrences(text, oldText);
        if (count != 1)
        {
            throw new ArgumentException($"old_text must occur exactly once, found {count}");
        }
    }

    public string Run(JsonObject args)
    {
        var path = _paths.Resolve(ToolArgs.RequireString(args, "path"));
        var oldText = ToolArgs.GetString(args, "old_text", string.Empty);
        var newText = ToolArgs.GetString(args, "new_text", string.Empty);

        var text = File.ReadAllText(path, Encoding.UTF8);
        var index = text.IndexOf(oldText, StringComparison.Ordinal);
        var replaced = text[..index] + newText + text[(index + oldText.Length)..];
        File.WriteAllText(path, replaced, new UTF8Encoding(false));
        return $"patched {_paths.Relative(path)}";
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle))
        {
            return 0;
        }
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
