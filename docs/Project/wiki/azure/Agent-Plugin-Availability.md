# Agent Plugin Availability

This guide is for operators and agents that need the audited MCP workflow surface for session log, TODO, requirements, import/export, traceability, and agent memory operations. All eight official plugins now ship memory skill + descriptor + always-on required-memory injection (or a documented host path), plus Perplexity research-policy docs.

## Source Of Truth

The workspace marker file, `AGENTS-README-FIRST.yaml`, is the runtime source of truth. Its `agent_plugins` section declares the required plugin policy, per-agent plugin names, startup commands, unavailable failure codes, tool expectations, and local root hints.

Agents must verify marker signature and health nonce first. During bootstrap, acquire the matching plugin through the MCP Server tool registry before relying on local root hints: search `/mcpserver/tools/search?keyword=<plugin_name>` for an exact `name` match, install it from `/mcpserver/tools/buckets/official/install?toolName=<plugin_name>` if it is missing, then execute the returned `commandTemplate` with the target parent directory. If the matching plugin remains unavailable after registry acquisition, the agent must stop MCP mutations, record `MCP_PLUGIN_UNAVAILABLE:<Agent>` when a trusted session-log path is available, and continue only with non-MCP local diagnosis.

## Available Agent Plugins

- Codex uses `mcpserver-codex-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-codex-plugin
  - Typical local root: `F:\GitHub\mcpserver-codex-plugin`
  - Status wrapper: `Invoke-CodexMcpPlugin.ps1 -Command Status`
  - Workflow wrapper: `Invoke-CodexMcpPlugin.ps1 -Command Invoke -Method <method> -Params <yaml>`
  - Completion wrapper: `Invoke-CodexMcpPlugin.ps1 -Command CompleteTurn -Response <text>`
  - Memory: `skills/memory/SKILL.md` + root `memory-descriptor.json`. Always-on required-memory injection via `hooks/scripts/memory-context.ps1`.
  - Research: `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md`.

- Claude Code uses `mcpserver-claude-code-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-claude-code-plugin
  - Typical local root: `F:\GitHub\mcpserver-claude-code-plugin`
  - Status helper: `lib/mcp.claude.status.sh`
  - PowerShell wrapper: `Invoke-ClaudeMcpPlugin.ps1`
  - Memory: `skills/memory/SKILL.md` + root `memory-descriptor.json`. Always-on required-memory injection via `hooks/scripts/memory-context.ps1`.
  - Research: `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md`.

- GitHub Copilot uses `mcpserver-copilot-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-copilot-plugin
  - Typical local root: `F:\GitHub\mcpserver-copilot-plugin`
  - Status helper: `lib/mcp.copilot.status.sh`
  - PowerShell wrapper: `Invoke-CopilotMcpPlugin.ps1`
  - Memory: `skills/memory/SKILL.md` + root `memory-descriptor.json`. Always-on required-memory injection via `hooks/scripts/memory-context.ps1`.
  - Research: `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md`.

- Cline uses `mcpserver-cline-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-cline-plugin
  - Typical local root: `F:\GitHub\mcpserver-cline-plugin`
  - Runtime: Cline MCP server from `server.json`, built with `npm run build`.
  - Memory: `skills/memory/SKILL.md` + root `memory-descriptor.json`. Always-on required-memory injection on the documented host path (`hooks/scripts/memory-context.ps1`).
  - Research: `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md`.

- Grok uses `mcpserver-grok-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-grok-plugin
  - Typical local root: `F:\GitHub\mcpserver-grok-plugin`
  - Runtime: Grok-compatible plugin manifests, enabled plugin skills, a Streamable HTTP MCP declaration, and PowerShell helpers from the plugin root.
  - Discovery check: `grok inspect`, `grok mcp doctor mcpserver`, or the `/mcps` TUI view should show the plugin MCP server when the plugin is loaded. The discoverable MCP tools are the server's native names, including `sessionlog_*`, `todo_*`, `requirements_*`, and `memory_*` (`memory_remember`, `memory_recall`, `memory_explore`, `memory_consolidate`, `memory_promote`, `memory_revert`, plus compat `memory_list|get|add|update|remove`). `mcp_*` names are hosted-agent aliases, and `workflow.sessionlog.*`, `workflow.todo.*`, `workflow.requirements.*`, and `workflow.memory.*` are plugin shim/REPL method names, not literal Grok `search_tool` results. When those workflow names are needed, invoke the plugin helper (`lib\repl-invoke.ps1` or `lib/repl-invoke.sh`) through the Grok plugin instructions instead of treating their absence from tool discovery as proof that the plugin is unavailable.
  - Memory: `skills/memory/SKILL.md` + root `memory-descriptor.json`. Always-on required-memory injection on UserPromptSubmit via `hooks/scripts/memory-context.ps1`.
  - Research: `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md` (also linked from `GROK-USAGE.md`).
  - Bench default: Grok remains the `./build.ps1 BenchMemory` default lane. After H7a `agree:true`, `-Plugin all` is unblocked. See `docs/context/memory.md` and `docs/benchmarks/README.md`.

- Claude Cowork uses `mcpserver-claude-cowork-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-claude-cowork-plugin
  - Typical local root: `F:\GitHub\mcpserver-claude-cowork-plugin`
  - Runtime: `.claude-plugin` manifest (mcpServers + skills + userConfig.workspace_path) with a local stdio connector and failsafe handoff. Never bypasses marker trust.
  - Memory: `skills/memory/SKILL.md` + root `memory-descriptor.json`. Always-on required-memory injection via `hooks/scripts/memory-context.ps1`.
  - Research: `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md`.

- Cline v2 uses `mcpserver-cline-v2-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-cline-v2-plugin
  - Typical local root: `F:\GitHub\mcpserver-cline-v2-plugin`
  - Runtime: Cline V2 AgentPlugin (createTool + hooks capability), built with `npm run build`. Shares the ReplBridge + marker-resolver + cache core.
  - Memory: `skills/memory/SKILL.md` + root `memory-descriptor.json`. Always-on required-memory injection on the documented host path (`hooks/scripts/memory-context.ps1`).
  - Research: `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md`.

- OpenCode uses `mcpserver-opencode-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-opencode-plugin
  - Typical local root: `F:\GitHub\mcpserver-opencode-plugin`
  - Runtime: OpenCode plugin SDK (createMcpServerPlugin), built with `npm run build`. Shares the ReplBridge + marker-resolver + cache core.
  - Memory: `skills/memory/SKILL.md` + root `memory-descriptor.json`. Always-on required-memory injection on the documented host path (`hooks/scripts/memory-context.ps1` and `src/memory-context.ts`).
  - Research: `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md`.

## MCP Client Verification

For workspace-scoped Copilot discovery, this repository uses `.github/mcp.json` with the flat HTTP entry for `mcpserver`. Do not place that flat entry in root `.mcp.json`: Claude Code also parses root `.mcp.json` and expects an `mcpServers` object there.

Current local validation commands for external clients:

- `codex mcp get mcpserver` should show `transport: streamable_http` and `url: http://localhost:7147/mcp-transport` from global Codex MCP config.
- `claude mcp get mcpserver` should show a user-scoped HTTP server connected to `http://localhost:7147/mcp-transport`.
- `grok mcp doctor mcpserver` should report handshake OK and discovered tools.
- `copilot mcp get mcpserver` should report `Source: Workspace (<workspace>\.github\mcp.json)`.
- `cline config mcp` should list `mcpserver` and `PowerShell.MCP`.
- `opencode mcp list` should report `mcpserver` connected; the supported add command writes to the user OpenCode config.

## Memory (all eight plugins)

MCP-MEMORY-002 S1-S7a H-done and S7b/H7b are on `develop` (PR #49, PR #50). H7a `docs/benchmarks/h7a-value-gate.json` is `agree:true`.

Every official plugin now has:

- `skills/memory/SKILL.md` (remember/recall/explore/consolidate/promote/revert plus compat CRUD)
- root `memory-descriptor.json`
- always-on required-memory injection at a host-supported request boundary, or a documented host path

Canonical McpServer payloads (for sync) live under `plugins/core/hosts/{id}/` and are applied with `plugins/core/sync/apply-memory-s7b-hosts.ps1`. Sibling plugin-repo PRs that landed this surface:

- Memory skill/descriptor: claude-code #3, claude-cowork #2, cline #2, cline-v2 #2, copilot #2, codex #2, opencode #2. Grok already had the S5 skill; grok #4 wired injection.
- Required-memory injection: claude-code #4, claude-cowork #3, cline #3, cline-v2 #3, grok #4, copilot #3, codex #3, opencode #3.

Empty Effective set still renders `REQUIRED MEMORIES` / `- None`. See `docs/context/memory.md`.

## Perplexity research policy

All eight plugin repos now include `docs/research/perplexity-research-policy.md` and `docs/research/research-to-plan-workflow.md` (merged PRs: claude-code #2, claude-cowork #1, cline #1, cline-v2 #1, grok #3, copilot #1, codex #1, opencode #1). Perplexity is the preferred external research provider for planning and substantive documentation. Ordinary McpServer tool execution does not require `PERPLEXITY_API_KEY`.

## Codex Quick Check

```powershell
pwsh.exe -NoLogo -NoProfile -NonInteractive -Command "& 'F:\GitHub\mcpserver-codex-plugin\Invoke-CodexMcpPlugin.ps1' -Command Status -WorkspacePath '<workspace-path>' -PluginRoot 'F:\GitHub\mcpserver-codex-plugin'"
```

The status output must show marker trust, health nonce verification, workspace path, session id, current turn, and supported namespaces before Codex performs MCP mutations.

## Turn transactions and keyserver

Official plugin mutations (session-log, TODO, requirements, memory, and the other first-party adapters) persist without keyserver/coordinator calls even when the live box keeps `Mcp:TurnTransactions:Enabled=true`. That is the shipped PLAN-TXNKEYSERVER-001 behavior on `develop` (`8f30caf`) and on the Linux box MCP at `/opt/mcpserver`.

Keyserver signing remains QuadBrain-only (`brain-slot.invoke`, `brain-slot.weight-update`). Do not disable `TurnTransactions.Enabled` to make plugin writes succeed. Do not treat a Linux publish/swap as Nuke `UpdateService` (Windows-only). Workspace-stamp repair stays fail-closed.

See `docs/USER-GUIDE.md` section 7f and `docs/MCP-SERVER.md` QuadBrain-only keyserver.

## REPL Relationship

`mcpserver-repl --agent-stdio` is the protocol host used by plugins and by implementation diagnostics. It is not a substitute for the required per-agent plugin during normal audited work. Direct REPL use is acceptable for plugin implementation, plugin troubleshooting, and fallback diagnosis after plugin verification fails.

When direct `--agent-stdio` is used, send one single-line JSON request envelope per stdin line. Do not send formatted YAML or wrap multiple requests in `type: batch`; unsupported batch envelopes are rejected with `unsupported_batch_envelope`.

## UserPromptSubmit and background agents

Root `UserPromptSubmit` stays on the root session while background agents run (FR-MCP-TRIAGEPLUGIN-001). A hostile-validator or other background brief does not open a new root `req-*-prompt-*` turn, does not cancel an in-progress root work turn, and does not rewrite `current-turn.yaml` after the root turn is completed. A distinct operator prompt still opens a new root turn. Operators list stale `in_progress` turns with `turnStatus=in_progress` and `staleOlderThanHours=N`; mass close is out of scope (BUG-TRIAGE-121).

## Related Docs

- `docs/REPL-AGENT-GUIDE.md`
- `docs/REPL-USER-GUIDE.md`
- `docs/REPL-MIGRATION-GUIDE.md`
- `docs/context/federation.md`
- `docs/context/memory.md`
- `docs/benchmarks/README.md`
