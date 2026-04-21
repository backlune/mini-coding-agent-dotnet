using MiniCodingAgent.Agent;

namespace MiniCodingAgent.Ui;

/// <summary>
/// REPL loop and slash-command dispatch. Isolated from <c>Program.cs</c> so
/// the wiring code stays short and the loop can be driven in tests if needed.
/// </summary>
public static class InteractiveRepl
{
    public static int Run(MiniAgent agent)
    {
        while (true)
        {
            Console.Write("\nmini-coding-agent> ");
            string? input;
            try
            {
                input = Console.ReadLine();
            }
            catch (IOException)
            {
                Console.WriteLine();
                return 0;
            }

            if (input is null)
            {
                Console.WriteLine();
                return 0;
            }

            input = input.Trim();
            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            if (input is "/exit" or "/quit")
            {
                return 0;
            }
            if (input == "/help")
            {
                Console.WriteLine(AgentConstants.HelpDetails);
                continue;
            }
            if (input == "/memory")
            {
                Console.WriteLine(PromptBuilder.RenderMemory(agent.Session.Memory));
                continue;
            }
            if (input == "/session")
            {
                Console.WriteLine(agent.SessionPath);
                continue;
            }
            if (input == "/reset")
            {
                agent.Reset();
                Console.WriteLine("session reset");
                continue;
            }

            Console.WriteLine();
            try
            {
                Console.WriteLine(agent.Ask(input));
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
