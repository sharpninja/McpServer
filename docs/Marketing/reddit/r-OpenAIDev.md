# r/OpenAIDev

**Suggested title:** MCP backend plus non-blocking triage for OpenAI Codex agents

**Flair:** Project / Tooling (check the sub's current flairs)

---

PASTE BELOW THIS LINE

---

[McpServer](https://github.com/sharpninja/McpServer) is an open-source (Apache 2.0) ASP.NET Core 9 server that gives AI coding agents a shared, persistent backend over the Model Context Protocol: local semantic search, a queryable TODO list, session logging with a full audit trail, requirements traceability, and GitHub sync. One local process, HTTP REST (Swagger) or MCP STDIO.

There is a Codex plugin for the OpenAI Codex CLI. Beyond the shared workflow surface (session, TODO, requirements, workspace), it gives the model a first-class way to write audited session-log turns directly, so the agent's own work history becomes queryable context rather than something that scrolls away.

This post is about one feature: triage, which lets the agent report its own infrastructure bugs without hijacking your task.

**The problem.** The plugin runs across eight hosts with different hook, cache, and shell behavior. When the plugin or server fails mid-task, the old failure modes were bad: the agent either stopped your work to repair plumbing, or worked around the failure with ad-hoc REST calls that hid the real defect.

**Triage, in four steps:**

1. Detect an incidental plugin or server failure during normal work.
2. Submit a structured report: the failing command or endpoint, the observed error, the workspace path, the component, and the agent identity.
3. Write a local failsafe YAML record regardless of whether submission succeeds.
4. Continue your actual request after a successful submission; stop and notify you only if triage itself is down.

**What it surfaced** is the useful part: stale plugin cache versus marker metadata, hook installation drift, split cache roots (a session-log append that silently no-ops), REPL surface drift, and shell runtime drift. Each one became a written requirement, then an observable acceptance criterion, then a test, instead of a one-off fix that gets forgotten. Full writeup: [Triage Plugin Code Quality Case Study](https://github.com/sharpninja/McpServer/blob/main/docs/case-studies/Triage-Plugin-Code-Quality-Case-Study.md).

Codex plugin: [mcpserver-codex-plugin](https://github.com/sharpninja/mcpserver-codex-plugin). How the eight plugins compare (integration mechanism, hooks, session-log ownership, and more): [AGENT-PLUGIN-FEATURE-MATRIX.md](https://github.com/sharpninja/McpServer/blob/main/docs/AGENT-PLUGIN-FEATURE-MATRIX.md).

Disclosure: this is my project. If you run Codex CLI agents against real repos, what infrastructure failures would you want captured automatically, and how would you want them surfaced? Happy to answer anything about the Codex integration, the MCP surface, or the triage design.
