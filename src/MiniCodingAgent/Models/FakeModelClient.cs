namespace MiniCodingAgent.Models;

/// <summary>
/// Test double that replays a pre-recorded list of completions. Captures every
/// prompt it receives so tests can assert on what the agent sent.
/// </summary>
public sealed class FakeModelClient : IModelClient
{
    private readonly Queue<string> _outputs;

    public FakeModelClient(IEnumerable<string> outputs)
    {
        _outputs = new Queue<string>(outputs);
    }

    public List<string> Prompts { get; } = new();

    public string Complete(string prompt, int maxNewTokens)
    {
        Prompts.Add(prompt);
        if (_outputs.Count == 0)
        {
            throw new InvalidOperationException("fake model ran out of outputs");
        }
        return _outputs.Dequeue();
    }
}
