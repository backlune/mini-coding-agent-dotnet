&nbsp;
# Mini-Coding-Agent (.NET)

A minimal, local coding agent implemented in C# / .NET 10. It is a port of the
original Python reference at [rasbt/mini-coding-agent](https://github.com/rasbt/mini-coding-agent),
restructured into idiomatic .NET to make it easier developers more familiar with .NET to get faimiliar with coding agents.

The agent still does the same six things:

1. **Live repo context** — collects a workspace snapshot from `git`
2. **Prompt shape** — builds a stable prefix so the model prompt cache stays warm
3. **Structured tools** — validated, permissioned operations the model can call
4. **Context reduction** — trims tool output and transcripts to a budget
5. **Session memory** — persists transcripts and distilled memory to disk
6. **Bounded delegation** — spawns a read-only child agent for scoped research

The default model backend is [LM Studio](https://lmstudio.ai) via its OpenAI-compatible local server. [Ollama](https://ollama.com) is supported as an alternative via `--backend ollama`.

&nbsp;
## Project layout

```
MiniCodingAgent.slnx
src/
  MiniCodingAgent/
    Program.cs                   # entry point: arg parsing + REPL wiring only
    AgentConstants.cs            # shared constants (ignored paths, limits, art)
    Agent/
      MiniAgent.cs               # orchestrator; ties every component together
      AgentOptions.cs            # init-only options record
      PromptBuilder.cs           # component 2 — stable prefix + per-turn prompt
      HistoryFormatter.cs        # component 4 — transcript shrink / dedupe
      ResponseParser.cs          # parses <tool>/<final>/retry
      ParsedResponse.cs
    Cli/
      CliArgumentParser.cs       # hand-rolled arg parser (mirrors argparse)
      CliOptions.cs
    Models/
      IModelClient.cs            # single-method abstraction
      LmStudioModelClient.cs     # HttpClient backed OpenAI /v1/chat/completions
      OllamaModelClient.cs       # HttpClient backed /api/generate
      FakeModelClient.cs         # test double
    Sessions/
      Session.cs                 # JSON-persisted session DTO
      SessionMemory.cs
      HistoryItem.cs             # one transcript turn (user / assistant / tool)
      SessionStore.cs            # component 5 — save / load / latest
    Tools/
      ITool.cs                   # tool contract
      ToolRegistry.cs
      ToolArgs.cs                # JsonObject read helpers
      WorkspacePathResolver.cs   # path jail, prevents escapes
      ApprovalPolicy.cs          # ask / auto / never
      IApprovalHandler.cs
      ConsoleApprovalHandler.cs
      ISubAgentRunner.cs         # seam used by DelegateTool
      Implementations/
        ListFilesTool.cs
        ReadFileTool.cs
        SearchTool.cs
        RunShellTool.cs
        WriteFileTool.cs
        PatchFileTool.cs
        DelegateTool.cs          # component 6 — bounded sub-agent
    Ui/
      WelcomeBanner.cs           # boxed ASCII banner
      InteractiveRepl.cs         # REPL loop + slash commands
    Utilities/
      TextHelpers.cs             # Clip / Middle
      Clock.cs                   # IClock abstraction
    Workspace/
      WorkspaceContext.cs        # component 1 — repo snapshot DTO
      WorkspaceContextBuilder.cs
      GitCommandRunner.cs        # thin wrapper around `git`
tests/
  MiniCodingAgent.Tests/         # xUnit test suite mirroring the Python tests
```

Each of the six components has a matching folder or file — the comments from
the original Python are preserved as XML doc comments near the component that
owns the responsibility.

&nbsp;
## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [LM Studio](https://lmstudio.ai/download) (default backend) **or** [Ollama](https://ollama.com/download)

&nbsp;
## Set up LM Studio (default)

1. Install [LM Studio](https://lmstudio.ai/download).
2. Download a model that follows instructions well (e.g. a Qwen 3.5 variant).
3. Open the **Developer** tab and **Start Server** — the default base URL is
   `http://127.0.0.1:1234`.
4. Load the model you downloaded so requests can be served.

Pass the loaded model's identifier with `--model`. Larger Qwen 3.5 variants
follow the `<tool>` / `<final>` formatting more reliably.

&nbsp;
## Set up Ollama (alternative)

```bash
ollama --help
ollama serve
ollama pull qwen3.5:4b
```

Then run the agent with `--backend ollama`.

&nbsp;
## Build & run

```bash
dotnet build
dotnet run --project src/MiniCodingAgent
```

Or publish a single executable:

```bash
dotnet publish src/MiniCodingAgent -c Release -o publish
./publish/mini-coding-agent        # Linux/macOS
publish\mini-coding-agent.exe      # Windows
```

&nbsp;
## CLI flags

```text
mini-coding-agent [prompt...] [options]

  --cwd <dir>              Workspace directory (default: .)
  --backend <name>         Model backend: lmstudio, ollama (default: lmstudio)
  --model <name>           Model name (default: qwen3.5:4b)
  --host <url>             Server URL (default: lmstudio=http://127.0.0.1:1234,
                                              ollama=http://127.0.0.1:11434)
  --timeout <secs>         Request timeout in seconds (default: 300)
  --resume <id|latest>     Resume a saved session (default: start new session)
  --approval <mode>        Approval policy: ask, auto, never (default: ask)
  --max-steps <n>          Maximum tool/model iterations per request (default: 6)
  --max-new-tokens <n>     Maximum model output tokens per step (default: 512)
  --temperature <val>      Sampling temperature (default: 0.2)
  --top-p <val>            Top-p sampling value (default: 0.9)
  -h, --help               Show this help message.
```

&nbsp;
## Approval modes

Risky tools (`write_file`, `patch_file`, `run_shell`) are gated by approval.

- `--approval ask` — prompts before each risky action (default)
- `--approval auto` — allows risky actions automatically
- `--approval never` — denies risky actions (used by delegated sub-agents)

&nbsp;
## Resume sessions

Sessions live under `.mini-coding-agent/sessions/` in the workspace root.

```bash
dotnet run --project src/MiniCodingAgent -- --resume latest
dotnet run --project src/MiniCodingAgent -- --resume 20260421-104025-2dd0aa
```

&nbsp;
## Interactive commands

Inside the REPL, slash commands are handled locally and not sent to the model:

- `/help` — show slash commands
- `/memory` — print distilled session memory
- `/session` — print the session file path
- `/reset` — clear session history and memory
- `/exit` / `/quit` — leave the REPL

&nbsp;
## Tests

```bash
dotnet test
```

The xUnit suite covers agent flow, tool validation, delegation, welcome
banner shape, and the LM Studio and Ollama HTTP contracts.

&nbsp;
## Notes

- The agent expects the model to emit either `<tool>...</tool>` or
  `<final>...</final>`. Weaker models may need tighter prompting.
- The agent is intentionally small and optimised for readability.
- The original Python reference is preserved at
  [rasbt/mini-coding-agent](https://github.com/rasbt/mini-coding-agent); this
  repository is the .NET port.
