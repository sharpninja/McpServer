# r/aiprojects

**Suggested title:** I built an MCP server that gives 8 different AI agents shared memory plus a triage system for their own bugs

**Flair:** Project / Show-and-tell (check the sub's current flairs)

---

PASTE BELOW THIS LINE

---

I have been building McpServer, and I want to show one feature that turned out better than I expected: a triage system that lets AI agents report their own infrastructure bugs without derailing whatever you asked them to do.

**First, what the project is.** [McpServer](https://github.com/sharpninja/McpServer) is an open-source (Apache 2.0) ASP.NET Core 9 server that gives AI coding agents a shared, persistent backend over the open Model Context Protocol. Agents connect over HTTP REST (Swagger) or MCP STDIO and get local semantic search over your code, a queryable TODO list, session logging with a full audit trail, requirements traceability, and GitHub sync. It is self-hosted, multi-tenant (one process, many workspaces), and uses local ONNX embeddings, so semantic search needs no cloud API key.

**The part I want feedback on is the plugin ecosystem.** There are eight plugins: Claude Code, Claude Cowork, Cline, Cline v2, OpenAI Codex, GitHub Copilot, Grok, and OpenCode. They all expose the same workflow surface through one shared contract: a signed marker file (`AGENTS-README-FIRST.yaml`) with HMAC-SHA256 trust verification, and a REPL bridge (`mcpserver-repl --agent-stdio`) that gives every host the same session, TODO, requirements, and workspace tools even when the host's native plugin API is thin. Side-by-side comparison: [AGENT-PLUGIN-FEATURE-MATRIX.md](https://github.com/sharpninja/McpServer/blob/main/docs/AGENT-PLUGIN-FEATURE-MATRIX.md).

**Triage is the showcase.** It solves a problem that only shows up once you run the same plugin across eight hosts: incidental failures, where something in the plugin or server breaks while the agent is doing your actual work.

Four steps:

1. The agent detects an incidental plugin or server failure during normal work.
2. It submits a structured report (failing command or endpoint, observed error, workspace path, component, plugin or agent identity).
3. It writes a local failsafe YAML record regardless of whether submission succeeds.
4. It continues your task after a successful submission, and stops only if triage itself is unavailable.

Then the server groups related reports into a workspace-scoped queue, and a research agent converts a batch into remediation work.

**What made it worth writing up is the discipline it forced.** Running triage across all those hosts surfaced a class of bugs normal feature work never catches: stale plugin cache versus marker metadata, hook installation drift, split cache roots, REPL surface drift, and shell runtime drift. Each one became a written requirement, then an observable acceptance criterion, then a test, rather than a one-off fix. The full case study walks through every edge case: [Triage Plugin Code Quality Case Study](https://github.com/sharpninja/McpServer/blob/main/docs/case-studies/Triage-Plugin-Code-Quality-Case-Study.md).

If you want to poke at it: the repo is [github.com/sharpninja/McpServer](https://github.com/sharpninja/McpServer), the plugin lineup and how to acquire each is [here](https://github.com/sharpninja/McpServer/blob/main/docs/AGENT-PLUGIN-AVAILABILITY.md), and the shared core that all plugins build on is [here](https://github.com/sharpninja/McpServer/blob/main/plugins/core/README.md).

This is my own project, so I am biased. For people who have built multi-agent or multi-host tooling: how do you handle your agents' own tooling failures, and does the marker-plus-REPL-bridge approach seem sane or overbuilt? Genuinely want the critique. Happy to answer anything about the architecture.
