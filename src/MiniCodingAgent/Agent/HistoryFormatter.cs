using System.Text.Json;
using System.Text.Json.Nodes;
using MiniCodingAgent.Sessions;
using MiniCodingAgent.Utilities;

namespace MiniCodingAgent.Agent;

/// <summary>
/// Renders a session's history into the compact form used in the model prompt.
/// This is <b>Component 4: Context Reduction And Output Management</b>.
/// </summary>
public static class HistoryFormatter
{
    public static string Render(IReadOnlyList<HistoryItem> history)
    {
        if (history.Count == 0)
        {
            return "- empty";
        }

        var lines = new List<string>();
        var seenReads = new HashSet<string>(StringComparer.Ordinal);
        var recentStart = Math.Max(0, history.Count - 6);

        for (var index = 0; index < history.Count; index++)
        {
            var item = history[index];
            var recent = index >= recentStart;

            if (item.Role == HistoryRoles.Tool && item.Name == "read_file" && !recent)
            {
                var path = item.Args is null ? string.Empty : item.Args["path"]?.ToString() ?? string.Empty;
                if (seenReads.Contains(path))
                {
                    continue;
                }
                seenReads.Add(path);
            }

            if (item.Role == HistoryRoles.Tool)
            {
                var limit = recent ? 900 : 180;
                var argsText = HistoryItem.Canonicalise(item.Args);
                lines.Add($"[tool:{item.Name}] {argsText}");
                lines.Add(TextHelpers.Clip(item.Content, limit));
            }
            else
            {
                var limit = recent ? 900 : 220;
                lines.Add($"[{item.Role}] {TextHelpers.Clip(item.Content, limit)}");
            }
        }

        return TextHelpers.Clip(string.Join("\n", lines), AgentConstants.MaxHistory);
    }
}
