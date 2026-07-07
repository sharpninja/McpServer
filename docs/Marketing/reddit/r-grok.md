# r/grok

**Suggested title:** Gave my Grok agent a persistent MCP backend plus a bug-triage flow that does not interrupt work

**Flair:** Project / Showcase (check the sub's current flairs)

---

PASTE BELOW THIS LINE

---

[McpServer](https://github.com/sharpninja/McpServer) is an open-source (Apache 2.0) ASP.NET Core 9 server that gives AI agents a shared, persistent backend over the Model Context Protocol: local semantic search over your code and docs, a queryable TODO list, session logging with a full audit trail, requirements traceability, and GitHub sync. One local process, one port, reachable over HTTP REST (Swagger) or MCP STDIO.

There is a Grok plugin. It ships Grok-compatible and Claude-compatible plugin manifests plus native SKILL.md files, hooks, and an mcpServers entry, so a Grok agent gets the full workflow surface (session, TODO, requirements, GraphRAG, workspace) without custom glue. The plugin repo includes a GROK-USAGE.md with the specifics.

This post is about one feature I am proud of: triage, a way for the agent to report its own infrastructure bugs without derailing your task.

**The problem.** The plugin runs across eight different hosts, each with different hook, cache, and shell behavior. When something in that stack fails mid-task, you do not want the agent to abandon your request to go fix plumbing, and you do not want the failure to vanish without enough detail to reproduce it.

**Triage, in four steps:**

1. Detect an incidental plugin or server failure during normal work.
2. Submit a structured report: the failing command or endpoint, the observed error, the workspace path, the component, and the plugin or agent identity.
3. Write a local failsafe YAML record regardless of whether submission succeeds.
4. Continue your actual request after a successful submission. Stop and notify you only if triage itself is unavailable.

Server-side, related reports group into a workspace-scoped queue, and a research agent turns a batch into remediation work instead of chasing one symptom.

**What it surfaced** was the real payoff: stale plugin cache versus marker metadata, hook installation drift, split cache roots, REPL surface drift, and shell runtime drift (which drove a PowerShell-native plugin runtime so behavior stops diverging across hosts). Each edge case became a requirement, then an observable acceptance criterion, then a test. Full writeup: [Triage Plugin Code Quality Case Study](https://github.com/sharpninja/McpServer/blob/main/docs/case-studies/Triage-Plugin-Code-Quality-Case-Study.md).

Try the Grok plugin: [mcpserver-grok-plugin](https://github.com/sharpninja/mcpserver-grok-plugin). How to acquire and trust any of the plugins: [AGENT-PLUGIN-AVAILABILITY.md](https://github.com/sharpninja/McpServer/blob/main/docs/AGENT-PLUGIN-AVAILABILITY.md).

Disclosure: I built this. If you are running a Grok agent against a real codebase, what would you want it to auto-report when its own tooling breaks? Happy to answer anything about the manifests, the MCP surface, or the triage flow.
