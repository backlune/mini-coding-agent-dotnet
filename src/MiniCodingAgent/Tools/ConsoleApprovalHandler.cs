using System.Text.Json.Nodes;

namespace MiniCodingAgent.Tools;

/// <summary>
/// Approval handler that reads <c>y</c>/<c>yes</c> from <see cref="Console.In"/>.
/// Treats closed stdin as "deny" to avoid hanging unattended runs.
/// </summary>
public sealed class ConsoleApprovalHandler : IApprovalHandler
{
    public bool Approve(string toolName, JsonObject args)
    {
        var json = args.ToJsonString();
        Console.Write($"approve {toolName} {json}? [y/N] ");
        var answer = Console.ReadLine();
        if (answer is null)
        {
            return false;
        }
        answer = answer.Trim().ToLowerInvariant();
        return answer == "y" || answer == "yes";
    }
}
