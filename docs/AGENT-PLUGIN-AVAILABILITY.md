# Agent Plugin Availability

This guide is for operators and agents that need the audited MCP workflow surface for session log, TODO, requirements, import/export, and traceability operations.

## Source Of Truth

The workspace marker file, `AGENTS-README-FIRST.yaml`, is the runtime source of truth. Its `agent_plugins` section declares the required plugin policy, per-agent plugin names, expected roots, startup commands, unavailable failure codes, and tool expectations.

Agents must verify marker signature and health nonce first. If the matching plugin is unavailable, the agent must stop MCP mutations, record `MCP_PLUGIN_UNAVAILABLE:<Agent>` when a trusted session-log path is available, and continue only with non-MCP local diagnosis.

## Available Agent Plugins

- Codex uses `mcpserver-codex-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-codex-plugin
  - Typical local root: `F:\GitHub\mcpserver-codex-plugin`
  - Status wrapper: `Invoke-CodexMcpPlugin.ps1 -Command Status`
  - Workflow wrapper: `Invoke-CodexMcpPlugin.ps1 -Command Invoke -Method <method> -Params <yaml>`
  - Completion wrapper: `Invoke-CodexMcpPlugin.ps1 -Command CompleteTurn -Response <text>`

- Claude Code uses `mcpserver-claude-code-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-claude-code-plugin
  - Typical local root: `F:\GitHub\mcpserver-claude-code-plugin`
  - Status helper: `lib/mcp.claude.status.sh`
  - PowerShell wrapper: `Invoke-ClaudeMcpPlugin.ps1`

- GitHub Copilot uses `mcpserver-copilot-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-copilot-plugin
  - Typical local root: `F:\GitHub\mcpserver-copilot-plugin`
  - Status helper: `lib/mcp.copilot.status.sh`
  - PowerShell wrapper: `Invoke-CopilotMcpPlugin.ps1`

- Cline uses `mcpserver-cline-plugin`.
  - Repository: https://github.com/sharpninja/mcpserver-cline-plugin
  - Typical local root: `F:\GitHub\mcpserver-cline-plugin`
  - Runtime: Cline MCP server from `server.json`, built with `npm run build`.

## Codex Quick Check

```powershell
pwsh.exe -NoLogo -NoProfile -Command "& 'F:\GitHub\mcpserver-codex-plugin\Invoke-CodexMcpPlugin.ps1' -Command Status -WorkspacePath '<workspace-path>' -PluginRoot 'F:\GitHub\mcpserver-codex-plugin'"
```

The status output must show marker trust, health nonce verification, workspace path, session id, current turn, and supported namespaces before Codex performs MCP mutations.

## REPL Relationship

`mcpserver-repl --agent-stdio` is the protocol host used by plugins and by implementation diagnostics. It is not a substitute for the required per-agent plugin during normal audited work. Direct REPL use is acceptable for plugin implementation, plugin troubleshooting, and fallback diagnosis after plugin verification fails.

When direct `--agent-stdio` is used, send one YAML request envelope per YAML document separated with `---`. Do not wrap multiple requests in `type: batch`; unsupported batch envelopes are rejected with `unsupported_batch_envelope`.

## Related Docs

- `docs/REPL-AGENT-GUIDE.md`
- `docs/REPL-USER-GUIDE.md`
- `docs/REPL-MIGRATION-GUIDE.md`
- `docs/context/federation.md`
