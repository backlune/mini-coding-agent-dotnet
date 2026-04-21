using System.Text.Json;
using System.Text.Json.Nodes;

namespace MiniCodingAgent.Sessions;

/// <summary>
/// Filesystem-backed persistence for <see cref="Session"/> objects.
/// This is <b>Component 5: Session Memory</b> from the Python reference.
/// </summary>
public sealed class SessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public SessionStore(string root)
    {
        Root = root;
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string PathFor(string sessionId) => Path.Combine(Root, sessionId + ".json");

    public string Save(Session session)
    {
        var path = PathFor(session.Id);
        var json = JsonSerializer.Serialize(session, SerializerOptions);
        File.WriteAllText(path, json);
        return path;
    }

    public Session Load(string sessionId)
    {
        var path = PathFor(sessionId);
        var json = File.ReadAllText(path);
        var node = JsonNode.Parse(json)
            ?? throw new InvalidDataException($"Session file is empty or invalid: {path}");

        var session = node.Deserialize<Session>(SerializerOptions)
            ?? throw new InvalidDataException($"Could not deserialize session: {path}");

        // Preserve structured args as JsonObject for each tool history entry.
        if (node is JsonObject root && root["history"] is JsonArray historyArray)
        {
            for (var i = 0; i < historyArray.Count && i < session.History.Count; i++)
            {
                if (historyArray[i] is JsonObject item
                    && item["args"] is JsonObject args
                    && session.History[i].Args is null)
                {
                    session.History[i].Args = (JsonObject)args.DeepClone();
                }
            }
        }

        return session;
    }

    /// <summary>
    /// Returns the id of the most-recently-modified session, or <c>null</c>
    /// when no sessions exist.
    /// </summary>
    public string? Latest()
    {
        if (!Directory.Exists(Root))
        {
            return null;
        }

        var files = Directory.EnumerateFiles(Root, "*.json")
            .Select(path => new FileInfo(path))
            .OrderBy(info => info.LastWriteTimeUtc)
            .ToList();

        return files.Count == 0 ? null : Path.GetFileNameWithoutExtension(files[^1].Name);
    }
}
