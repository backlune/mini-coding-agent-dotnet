namespace MiniCodingAgent.Tools;

/// <summary>
/// Resolves tool-supplied paths while enforcing that they stay inside the
/// workspace root. Rejects <c>..</c> traversal and absolute paths outside root.
/// </summary>
public sealed class WorkspacePathResolver
{
    public WorkspacePathResolver(string root)
    {
        Root = Path.GetFullPath(root);
    }

    public string Root { get; }

    public string Resolve(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            throw new ArgumentException("path must not be empty", nameof(rawPath));
        }

        var combined = Path.IsPathRooted(rawPath)
            ? rawPath
            : Path.Combine(Root, rawPath);

        var resolved = Path.GetFullPath(combined);

        var rootWithSeparator = Root.EndsWith(Path.DirectorySeparatorChar)
            ? Root
            : Root + Path.DirectorySeparatorChar;

        if (!string.Equals(resolved, Root, StringComparison.OrdinalIgnoreCase)
            && !resolved.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"path escapes workspace: {rawPath}");
        }

        return resolved;
    }

    /// <summary>
    /// Converts an absolute path inside the workspace back to a repo-relative
    /// POSIX-style path suitable for display to the user or the model.
    /// </summary>
    public string Relative(string absolutePath)
    {
        var rel = Path.GetRelativePath(Root, absolutePath);
        return rel.Replace('\\', '/');
    }
}
