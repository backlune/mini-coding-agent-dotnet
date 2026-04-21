namespace MiniCodingAgent.Tools;

/// <summary>
/// Indirection used by <see cref="Implementations.DelegateTool"/> to invoke a
/// bounded child agent without taking a direct dependency on the top-level
/// <c>MiniAgent</c> type. <b>Component 6: Delegation</b> hangs off this seam.
/// </summary>
public interface ISubAgentRunner
{
    /// <summary>
    /// Run a child agent on <paramref name="task"/> with a hard cap of
    /// <paramref name="maxSteps"/> tool/model turns. Throws if delegation is
    /// disabled (for example because the parent is already a sub-agent).
    /// </summary>
    string Run(string task, int maxSteps);
}
