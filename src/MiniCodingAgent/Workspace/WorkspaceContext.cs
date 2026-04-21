using System.Text;

namespace MiniCodingAgent.Workspace;

/// <summary>
/// Immutable snapshot of the workspace used to ground the model in the
/// real repository state. This is <b>Component 1: Live Repo Context</b>
/// from the Python reference.
/// </summary>
public sealed class WorkspaceContext
{
    public WorkspaceContext(
        string cwd,
        string repoRoot,
        string branch,
        string defaultBranch,
        string status,
        IReadOnlyList<string> recentCommits,
        IReadOnlyDictionary<string, string> projectDocs)
    {
        Cwd = cwd;
        RepoRoot = repoRoot;
        Branch = branch;
        DefaultBranch = defaultBranch;
        Status = status;
        RecentCommits = recentCommits;
        ProjectDocs = projectDocs;
    }

    public string Cwd { get; }
    public string RepoRoot { get; }
    public string Branch { get; }
    public string DefaultBranch { get; }
    public string Status { get; }
    public IReadOnlyList<string> RecentCommits { get; }
    public IReadOnlyDictionary<string, string> ProjectDocs { get; }

    /// <summary>
    /// Human-readable rendering of the snapshot embedded in the model prompt.
    /// </summary>
    public string ToPromptText()
    {
        var commits = RecentCommits.Count == 0
            ? "- none"
            : string.Join("\n", RecentCommits.Select(line => $"- {line}"));

        var docs = ProjectDocs.Count == 0
            ? "- none"
            : string.Join("\n", ProjectDocs.Select(kvp => $"- {kvp.Key}\n{kvp.Value}"));

        var builder = new StringBuilder();
        builder.AppendLine("Workspace:");
        builder.AppendLine($"- cwd: {Cwd}");
        builder.AppendLine($"- repo_root: {RepoRoot}");
        builder.AppendLine($"- branch: {Branch}");
        builder.AppendLine($"- default_branch: {DefaultBranch}");
        builder.AppendLine("- status:");
        builder.AppendLine(Status);
        builder.AppendLine("- recent_commits:");
        builder.AppendLine(commits);
        builder.AppendLine("- project_docs:");
        builder.Append(docs);
        return builder.ToString();
    }
}
