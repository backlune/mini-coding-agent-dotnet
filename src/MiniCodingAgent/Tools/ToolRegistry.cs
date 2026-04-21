namespace MiniCodingAgent.Tools;

/// <summary>
/// Tiny indexed collection of registered <see cref="ITool"/> instances.
/// Built once per <c>MiniAgent</c>; avoids the need for dependency injection
/// while still letting tests swap individual tools.
/// </summary>
public sealed class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;
    private readonly List<ITool> _ordered;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _ordered = tools.ToList();
        _tools = _ordered.ToDictionary(tool => tool.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<ITool> All => _ordered;

    public ITool? Get(string name) => _tools.TryGetValue(name, out var tool) ? tool : null;

    public bool Contains(string name) => _tools.ContainsKey(name);
}
