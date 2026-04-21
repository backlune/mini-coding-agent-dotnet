namespace MiniCodingAgent.Tools;

/// <summary>
/// Policy applied to "risky" tools such as shell execution and file writes.
/// </summary>
public enum ApprovalPolicy
{
    /// <summary>Prompt the human before executing each risky call.</summary>
    Ask,

    /// <summary>Allow all risky calls without prompting. Useful for unattended runs.</summary>
    Auto,

    /// <summary>Deny every risky call. Used by delegated sub-agents and tests.</summary>
    Never,
}

public static class ApprovalPolicyExtensions
{
    public static string ToCliString(this ApprovalPolicy policy) => policy switch
    {
        ApprovalPolicy.Ask => "ask",
        ApprovalPolicy.Auto => "auto",
        ApprovalPolicy.Never => "never",
        _ => policy.ToString().ToLowerInvariant(),
    };

    public static bool TryParse(string? value, out ApprovalPolicy policy)
    {
        switch ((value ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "ask":
                policy = ApprovalPolicy.Ask;
                return true;
            case "auto":
                policy = ApprovalPolicy.Auto;
                return true;
            case "never":
                policy = ApprovalPolicy.Never;
                return true;
            default:
                policy = ApprovalPolicy.Ask;
                return false;
        }
    }
}
