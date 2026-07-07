# r/ClaudeCode

**Suggested title:** How we use MCP triage to catch Claude Code plugin bugs without derailing the current task

**Flair:** Showcase / Project (check the sub's current flairs)

---

PASTE BELOW THIS LINE

---

[McpServer](https://github.com/sharpninja/McpServer) is an open-source (Apache 2.0) ASP.NET Core 9 server that gives AI coding agents a shared, persistent backend over the Model Context Protocol: local semantic search over your code, a queryable TODO list, session logging with a full audit trail, requirements traceability, and GitHub sync. It runs locally on one port and speaks both HTTP REST (with Swagger) and MCP STDIO.

There is a Claude Code plugin for it. It wires the server into Claude Code through hooks and skills, so session logging, TODO updates, and bug triage happen on the turns you already take (SessionStart/End, UserPromptSubmit, Stop, PostToolUse plan+edit, the compaction events, and subagent completion) instead of being something you have to remember to do by hand.

This post is about one piece of that: triage.

**The problem.** The plugin runs across eight host platforms (Claude Code, Claude Cowork, Cline, Cline v2, Codex, Copilot, Grok, OpenCode). When the plugin or the server itself hiccups in the middle of your actual task, the last thing you want is for the agent to drop your work and start repairing infrastructure, or to paper over the failure with ad-hoc REST calls. Both happened before triage was the default path.

**How triage works, in four steps:**

1. The agent detects an incidental plugin or server failure during normal work.
2. It submits a structured triage report: the failing command or endpoint, the observed error, the workspace path, the component, and which plugin or agent hit it.
3. It writes a local failsafe YAML record of the failure no matter what, so the evidence survives even if submission fails.
4. It continues your actual request after a successful submission. Only if triage itself is down does it stop and tell you.

**What it surfaced** is the interesting part. The edge case that bit Claude Code hardest was hook installation drift: the plugin package shipped hooks, but Claude's active user settings had not actually wired them into the running session. Skills were installed, yet enforcement did not run consistently. Triage turned that into a [hook-validation skill](https://github.com/sharpninja/McpServer/blob/main/docs/claude-hook-validation-skill.md), triggered from the marker file, that inspects your active settings, clears stale plugin cache, and installs the required hooks.

Other classes it caught: stale plugin cache versus marker metadata, split cache roots (a session-log append that silently no-ops because the turn cache was missing), REPL surface drift, and shell runtime drift. The throughline: every edge case became a written requirement, the requirement got an observable acceptance criterion, and the acceptance criterion got a test. Full writeup: [Triage Plugin Code Quality Case Study](https://github.com/sharpninja/McpServer/blob/main/docs/case-studies/Triage-Plugin-Code-Quality-Case-Study.md).

If you want to try the Claude Code plugin: [mcpserver-claude-code-plugin](https://github.com/sharpninja/mcpserver-claude-code-plugin). Availability and trust bootstrap for all the plugins: [AGENT-PLUGIN-AVAILABILITY.md](https://github.com/sharpninja/McpServer/blob/main/docs/AGENT-PLUGIN-AVAILABILITY.md).

I built this, so I am biased. What would you want an MCP triage flow to catch in your Claude Code setup, and where would automatic hooks feel like too much magic? Happy to go deep on the hook chain, the plugin, or the process.
