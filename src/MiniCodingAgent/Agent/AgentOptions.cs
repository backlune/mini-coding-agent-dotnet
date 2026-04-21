using MiniCodingAgent.Tools;

namespace MiniCodingAgent.Agent;

/// <summary>
/// Options consumed by <see cref="MiniAgent"/>. Kept as a record with init-only
/// properties so construction sites read like Python keyword arguments.
/// </summary>
public sealed record AgentOptions
{
    /// <summary>Policy applied to risky tool calls.</summary>
    public ApprovalPolicy ApprovalPolicy { get; init; } = ApprovalPolicy.Ask;

    /// <summary>Maximum number of successful tool calls allowed per user turn.</summary>
    public int MaxSteps { get; init; } = 6;

    /// <summary>Maximum tokens requested from the model per call.</summary>
    public int MaxNewTokens { get; init; } = 512;

    /// <summary>Current delegation depth. 0 for top-level agents.</summary>
    public int Depth { get; init; } = 0;

    /// <summary>Hard limit on delegation depth.</summary>
    public int MaxDepth { get; init; } = 1;

    /// <summary>When true, risky tools are refused regardless of approval policy.</summary>
    public bool ReadOnly { get; init; } = false;
}
