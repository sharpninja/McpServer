# r/GrokBuild

**Suggested title:** A build process where incidental bugs become requirements, acceptance criteria, and tests (MCP triage + Grok)

**Flair:** Build / Workflow (check the sub's current flairs)

---

PASTE BELOW THIS LINE

---

[McpServer](https://github.com/sharpninja/McpServer) is an open-source (Apache 2.0) ASP.NET Core 9 server that gives AI agents a shared backend over the Model Context Protocol: semantic search, TODOs, session logging, requirements traceability, and GitHub sync, over HTTP REST or MCP STDIO. There is a Grok plugin that wires it into a Grok agent through Grok-compatible and Claude-compatible manifests, SKILL.md files, and hooks.

This post is less about the server and more about a build-process question: what do you do with the incidental bugs your agent tooling throws while it is supposed to be doing something else?

My answer is triage, and the point is the loop it creates, not just the bug capture.

**The flow, in four steps:**

1. The agent detects an incidental plugin or server failure during normal work.
2. It submits a structured report (failing command, observed error, workspace path, component, agent identity).
3. It writes a local failsafe YAML record no matter what.
4. It continues your task after a successful submission; it stops only if triage itself is down.

That keeps your active work moving. But the part that matters for how I build is what happens next. Each triaged edge case does not just get patched. It becomes:

- a **requirement** (broad intent rewritten as an enforceable contract),
- an **acceptance criterion** (an externally observable result, for example: "hook installation is not complete unless the active host settings actually contain the hook entries, not merely because the package ships hook files"),
- and a **test** (the executable form of that requirement).

Concrete examples that went through this loop: stale plugin cache versus marker metadata, hook installation drift, split cache roots (an append that silently no-ops), REPL surface parity, and shell runtime drift that pushed the whole plugin runtime to PowerShell-native so behavior stops diverging across hosts.

This is the Byrd Development Process applied to infrastructure bugs: tests first, red before green, and requirements drive the tests rather than the other way around. Process: [Development-Process-draft-v4.md](https://github.com/sharpninja/McpServer/blob/main/docs/Development-Process-draft-v4.md). The requirements that came out of it live here: [Functional](https://github.com/sharpninja/McpServer/blob/main/docs/Project/Functional-Requirements.md), [Technical](https://github.com/sharpninja/McpServer/blob/main/docs/Project/Technical-Requirements.md), [Testing](https://github.com/sharpninja/McpServer/blob/main/docs/Project/Testing-Requirements.md). The full narrative with every edge case: [Triage Plugin Code Quality Case Study](https://github.com/sharpninja/McpServer/blob/main/docs/case-studies/Triage-Plugin-Code-Quality-Case-Study.md).

Grok plugin if you want to build on it: [mcpserver-grok-plugin](https://github.com/sharpninja/mcpserver-grok-plugin).

I built this, so grain of salt. For people building with Grok: do you convert your agent's own failures into tests, or do they stay as one-off fixes? Curious how others close that loop. Happy to go deep on the process or the triage internals.
