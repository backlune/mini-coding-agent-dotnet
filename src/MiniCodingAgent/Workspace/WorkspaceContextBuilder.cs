using MiniCodingAgent.Utilities;

namespace MiniCodingAgent.Workspace;

/// <summary>
/// Builds <see cref="WorkspaceContext"/> snapshots by shelling out to <c>git</c>
/// and reading a handful of well-known project documents.
/// </summary>
public sealed class WorkspaceContextBuilder
{
    private readonly IGitCommandRunner _git;

    public WorkspaceContextBuilder(IGitCommandRunner? git = null)
    {
        _git = git ?? new GitCommandRunner();
    }

    public WorkspaceContext Build(string cwd)
    {
        var resolvedCwd = Path.GetFullPath(cwd);
        var repoRootRaw = _git.Run(resolvedCwd, new[] { "rev-parse", "--show-toplevel" }, resolvedCwd);
        var repoRoot = Path.GetFullPath(repoRootRaw);

        var docs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var baseDir in new[] { repoRoot, resolvedCwd })
        {
            foreach (var name in AgentConstants.DocNames)
            {
                var path = Path.Combine(baseDir, name);
                if (!File.Exists(path))
                {
                    continue;
                }

                var key = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                if (docs.ContainsKey(key))
                {
                    continue;
                }

                var content = TryReadAllText(path);
                docs[key] = TextHelpers.Clip(content, 1200);
            }
        }

        var branch = _git.Run(resolvedCwd, new[] { "branch", "--show-current" }, "-");
        if (string.IsNullOrEmpty(branch))
        {
            branch = "-";
        }

        var defaultBranchRaw = _git.Run(
            resolvedCwd,
            new[] { "symbolic-ref", "--short", "refs/remotes/origin/HEAD" },
            "origin/main");
        var defaultBranch = defaultBranchRaw.StartsWith("origin/", StringComparison.Ordinal)
            ? defaultBranchRaw["origin/".Length..]
            : defaultBranchRaw;

        var statusRaw = _git.Run(resolvedCwd, new[] { "status", "--short" }, "clean");
        var status = TextHelpers.Clip(string.IsNullOrEmpty(statusRaw) ? "clean" : statusRaw, 1500);

        var commitsRaw = _git.Run(resolvedCwd, new[] { "log", "--oneline", "-5" });
        var recentCommits = string.IsNullOrEmpty(commitsRaw)
            ? Array.Empty<string>()
            : commitsRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return new WorkspaceContext(
            cwd: resolvedCwd,
            repoRoot: repoRoot,
            branch: branch,
            defaultBranch: defaultBranch,
            status: status,
            recentCommits: recentCommits,
            projectDocs: docs);
    }

    private static string TryReadAllText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }
}
