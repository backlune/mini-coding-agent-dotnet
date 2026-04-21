using System.Text;
using MiniCodingAgent.Sessions;
using MiniCodingAgent.Tools;
using MiniCodingAgent.Workspace;

namespace MiniCodingAgent.Agent;

/// <summary>
/// Assembles the stable prefix and the per-turn prompt sent to the model.
/// The prefix is built once per session so prompt caches can be reused.
/// This is <b>Component 2: Prompt Shape And Cache Reuse</b>.
/// </summary>
public sealed class PromptBuilder
{
    private readonly string _prefix;

    public PromptBuilder(ToolRegistry tools, WorkspaceContext workspace)
    {
        _prefix = BuildPrefix(tools, workspace);
    }

    public string Prefix => _prefix;

    public string BuildPrompt(SessionMemory memory, string transcript, string userMessage)
    {
        var builder = new StringBuilder();
        builder.AppendLine(_prefix).AppendLine();
        builder.AppendLine(RenderMemory(memory)).AppendLine();
        builder.AppendLine("Transcript:");
        builder.AppendLine(transcript).AppendLine();
        builder.AppendLine("Current user request:");
        builder.Append(userMessage);
        return builder.ToString().TrimEnd();
    }

    public static string RenderMemory(SessionMemory memory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Memory:");
        builder.AppendLine($"- task: {(string.IsNullOrEmpty(memory.Task) ? "-" : memory.Task)}");
        builder.AppendLine($"- files: {(memory.Files.Count == 0 ? "-" : string.Join(", ", memory.Files))}");
        builder.AppendLine("- notes:");
        if (memory.Notes.Count == 0)
        {
            builder.Append("  - none");
        }
        else
        {
            for (var i = 0; i < memory.Notes.Count; i++)
            {
                builder.Append("- ").Append(memory.Notes[i]);
                if (i < memory.Notes.Count - 1)
                {
                    builder.AppendLine();
                }
            }
        }
        return builder.ToString();
    }

    private static string BuildPrefix(ToolRegistry tools, WorkspaceContext workspace)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are Mini-Coding-Agent, a small local coding agent running through Ollama.");
        builder.AppendLine();
        builder.AppendLine("Rules:");
        builder.AppendLine("- Use tools instead of guessing about the workspace.");
        builder.AppendLine("- Return exactly one <tool>...</tool> or one <final>...</final>.");
        builder.AppendLine("- Tool calls must look like:");
        builder.AppendLine("  <tool>{\"name\":\"tool_name\",\"args\":{...}}</tool>");
        builder.AppendLine("- For write_file and patch_file with multi-line text, prefer XML style:");
        builder.AppendLine("  <tool name=\"write_file\" path=\"file.py\"><content>...</content></tool>");
        builder.AppendLine("- Final answers must look like:");
        builder.AppendLine("  <final>your answer</final>");
        builder.AppendLine("- Never invent tool results.");
        builder.AppendLine("- Keep answers concise and concrete.");
        builder.AppendLine("- If the user asks you to create or update a specific file and the path is clear, use write_file or patch_file instead of repeatedly listing files.");
        builder.AppendLine("- Before writing tests for existing code, read the implementation first.");
        builder.AppendLine("- When writing tests, match the current implementation unless the user explicitly asked you to change the code.");
        builder.AppendLine("- New files should be complete and runnable, including obvious imports.");
        builder.AppendLine("- Do not repeat the same tool call with the same arguments if it did not help. Choose a different tool or return a final answer.");
        builder.AppendLine("- Required tool arguments must not be empty. Do not call read_file, write_file, patch_file, run_shell, or delegate with args={}.");
        builder.AppendLine();
        builder.AppendLine("Tools:");
        foreach (var tool in tools.All)
        {
            var fields = string.Join(", ", tool.Schema.Select(p => $"{p.Name}: {p.Signature}"));
            var risk = tool.IsRisky ? "approval required" : "safe";
            builder.AppendLine($"- {tool.Name}({fields}) [{risk}] {tool.Description}");
        }
        builder.AppendLine();
        builder.AppendLine("Valid response examples:");
        builder.AppendLine("<tool>{\"name\":\"list_files\",\"args\":{\"path\":\".\"}}</tool>");
        builder.AppendLine("<tool>{\"name\":\"read_file\",\"args\":{\"path\":\"README.md\",\"start\":1,\"end\":80}}</tool>");
        builder.AppendLine("<tool name=\"write_file\" path=\"binary_search.py\"><content>def binary_search(nums, target):\n    return -1\n</content></tool>");
        builder.AppendLine("<tool name=\"patch_file\" path=\"binary_search.py\"><old_text>return -1</old_text><new_text>return mid</new_text></tool>");
        builder.AppendLine("<tool>{\"name\":\"run_shell\",\"args\":{\"command\":\"dotnet test\",\"timeout\":20}}</tool>");
        builder.AppendLine("<final>Done.</final>");
        builder.AppendLine();
        builder.Append(workspace.ToPromptText());
        return builder.ToString().TrimEnd();
    }
}
