namespace MiniCodingAgent.Models;

/// <summary>
/// Abstraction over a language model backend. The agent only needs a single
/// <c>complete</c>-style call, which keeps mocking trivial in tests.
/// </summary>
public interface IModelClient
{
    /// <summary>
    /// Generate a single completion for <paramref name="prompt"/>, capped at
    /// <paramref name="maxNewTokens"/> tokens on the model side.
    /// </summary>
    string Complete(string prompt, int maxNewTokens);
}
