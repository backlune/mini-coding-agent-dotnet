using System.Text;
using MiniCodingAgent.Agent;
using MiniCodingAgent.Tools;
using MiniCodingAgent.Utilities;

namespace MiniCodingAgent.Ui;

/// <summary>
/// Builds the boxed ASCII welcome banner shown at the top of the REPL.
/// Pulled out of <c>Program.cs</c> so tests can assert its shape without
/// launching the agent.
/// </summary>
public static class WelcomeBanner
{
    public static string Build(MiniAgent agent, string model, string host, int? terminalWidth = null)
    {
        var width = Math.Max(68, Math.Min(terminalWidth ?? 80, 84));
        var inner = width - 4;
        const int gap = 3;
        var leftWidth = (inner - gap) / 2;
        var rightWidth = inner - gap - leftWidth;

        var builder = new StringBuilder();
        builder.AppendLine(Divider(width, '='));
        foreach (var line in AgentConstants.WelcomeArt)
        {
            builder.AppendLine(Center(inner, line));
        }
        builder.AppendLine(Center(inner, "MINI CODING AGENT"));
        builder.AppendLine(Divider(width, '-'));
        builder.AppendLine(Row(width, string.Empty));
        builder.AppendLine(Row(width, "WORKSPACE  " + TextHelpers.Middle(agent.Workspace.Cwd, inner - 11)));
        builder.AppendLine(Pair(inner, leftWidth, rightWidth, gap, "MODEL", model, "BRANCH", agent.Workspace.Branch));
        builder.AppendLine(Pair(inner, leftWidth, rightWidth, gap, "APPROVAL", agent.Options.ApprovalPolicy.ToCliString(), "SESSION", agent.Session.Id));
        builder.AppendLine(Row(width, string.Empty));
        builder.Append(Divider(width, '='));
        _ = host; // currently unused in the banner; accepted for signature parity
        return builder.ToString();
    }

    private static string Divider(int width, char ch) => "+" + new string(ch, width - 2) + "+";

    private static string Row(int width, string text)
    {
        var inner = width - 4;
        var body = TextHelpers.Middle(text, inner);
        return "| " + body.PadRight(inner) + " |";
    }

    private static string Center(int innerWidth, string text)
    {
        var body = TextHelpers.Middle(text, innerWidth);
        var padding = innerWidth - body.Length;
        var left = padding / 2;
        var right = padding - left;
        return "| " + new string(' ', left) + body + new string(' ', right) + " |";
    }

    private static string Pair(int innerWidth, int leftWidth, int rightWidth, int gap,
        string leftLabel, string leftValue, string rightLabel, string rightValue)
    {
        _ = innerWidth;
        var left = Cell(leftLabel, leftValue, leftWidth);
        var right = Cell(rightLabel, rightValue, rightWidth);
        return "| " + left + new string(' ', gap) + right + " |";
    }

    private static string Cell(string label, string value, int size)
    {
        var body = TextHelpers.Middle($"{label,-9} {value}", size);
        return body.PadRight(size);
    }
}
