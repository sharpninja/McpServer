# McpServer Agent Plugin Feature Matrix

This document provides a feature comparison matrix for the eight `mcpserver-*-plugin` packages that integrate the local McpServer workflow surface (session logging, TODO management, requirements traceability, GraphRAG, and workspace lifecycle) with different AI coding agents and platforms.

All plugins share the core contract defined by `AGENTS-README-FIRST.yaml`: marker-based discovery with HMAC-SHA256 signature verification, health nonce challenges, `mcpserver-repl --agent-stdio` transport (or equivalent), and offline YAML failsafe caching.

## Plugins

| Plugin | Target Platform | Repository (local) |
|--------|-----------------|---------------------|
| mcpserver-claude-code-plugin | Claude Code | `F:\GitHub\mcpserver-claude-code-plugin` |
| mcpserver-claude-cowork-plugin | Claude Cowork / Claude Desktop | `F:\GitHub\mcpserver-claude-cowork-plugin` |
| mcpserver-cline-plugin | Cline (classic, VS Code) | `F:\GitHub\mcpserver-cline-plugin` |
| mcpserver-cline-v2-plugin | Cline V2 (AgentPlugin API) | `F:\GitHub\mcpserver-cline-v2-plugin` |
| mcpserver-codex-plugin | OpenAI Codex CLI | `F:\GitHub\mcpserver-codex-plugin` |
| mcpserver-copilot-plugin | GitHub Copilot | `F:\GitHub\mcpserver-copilot-plugin` |
| mcpserver-grok-plugin | Grok 4.3 CLI / TUI | `F:\GitHub\mcpserver-grok-plugin` |
| mcpserver-opencode-plugin | OpenCode | `F:\GitHub\mcpserver-opencode-plugin` |

## Feature Matrix

| Feature | Claude Code | Claude Cowork | Cline | Cline v2 | Codex | Copilot | Grok | OpenCode |
|---------|-------------|---------------|-------|----------|-------|---------|------|----------|
| **Target Platform** | Claude Code | Claude Cowork/Desktop | Cline (VS Code MCP) | Cline V2 AgentPlugin | Codex CLI | GitHub Copilot | Grok 4.3 CLI/TUI | OpenCode |
| **Integration Mechanism** | Claude hooks + plugin manifest + skills | .claude-plugin (mcpServers + skills + userConfig) | MCP Server (stdio, MCP SDK) | AgentPlugin (createTool + hooks cap) | .codex-plugin (skillsPath) + lib scripts | plugin.json (skills[] + hooks + mcpServers) | Grok/Claude-compatible plugin manifests + native SKILL.md + hooks + mcpServers | OpenCode plugin SDK (createMcpServerPlugin) |
| **Core Workflow Tools** | Full (TODO, Session, Reqs, GraphRAG, Workspace) | Full (same 5) | Full (via MCP tools) | Full (5 tools) | Full (5 + guidance) | Full (5) | Full (5) | Full (many explicit tools) |
| **Additional Dedicated Skills** | - | - | - | - | device, enforcement, workflow | - | - | - |
| **Native SKILL.md Files** | Yes (5) | Yes (5) | Minimal (workspace only) | Minimal (workspace only) | Yes (8) | Yes (5) | Yes (5, native-first) | No (empty skills/) |
| **Automatic Hook Support** | Yes (rich) | Partial (degrades to handoff/cache) | No | Partial (capabilities include hooks) | No (Codex has limited hook surface) | Yes (rich) | Yes (dual claude/codex manifests) | No |
| **Hook Events Supported** | SessionStart/End, UserPromptSubmit, Stop, PostToolUse (plan+edit), Pre/PostCompact, SubagentComplete | Limited (Cowork hook env) | N/A | Via V2 hooks cap | N/A (uses scripts) | Session*, Compact*, UserPromptSubmit, Stop, PostToolUse | Same as Claude Code + Codex | N/A |
| **Manual Enforcement Scripts** | Yes (lib/ + hooks) | Yes (lib/ + handoff) | Yes (3-phase: user-prompt-submit, code-verify, stop-gate) | Yes (same 3 scripts + ENFORCEMENT.md) | Yes (strong: session-start, code-verify, stop-gate + dedicated skill) | Yes (lib/ scripts) | Yes (full lib/ scripts) | Limited (cache only; no enforcement scripts in lib/) |
| **Per-Turn Build Verification + Stop Gate** | Yes (via hooks + scripts) | Yes | Yes (enforcement protocol) | Yes (enforcement protocol) | Yes (strong emphasis) | Yes (via hooks + scripts) | Yes | Partial (failsafe only) |
| **Plan Tracking (auto-TODO on approve/edit)** | Yes (plan-approved, plan-modified hooks) | Yes | Via scripts | Via scripts | Via scripts | Yes (PostToolUse matchers) | Yes | No dedicated |
| **Offline Cache / Failsafe Replay** | Yes (cache/pending + flush on reconnect) | Yes (special handoff + repaired behavior) | Yes (.mcpServer/failsafe/cline) | Yes (.mcpServer/failsafe/cline-v2) | Yes (cache + JSONL recovery) | Yes | Yes (full) | Yes (.mcpServer/failsafe/opencode) |
| **Subagent / JSONL Transcript Capture** | Yes (subagent-import hook + codex-jsonl helpers) | No | No | No | Yes (codex-jsonl-enrich, final-response, subagent handling) | No | Yes (inherits claude/codex scripts) | No |
| **Android Device Validation (adb_step loops)** | No | No | No | No | Yes (dedicated device skill + guidance) | No | No | No |
| **Marker + HMAC Signature + Nonce Bootstrap** | Yes | Yes (userConfig.workspace_path + strict contract) | Yes (fullBootstrap) | Yes | Yes | Yes | Yes (skills + hooks) | Yes |
| **REPL Transport (mcpserver-repl --agent-stdio)** | Yes (primary) | Yes (stdio connector) | Yes (ReplBridge) | Yes (ReplBridge) | Yes | Yes (declared in mcpServers) | Via plugin shim helpers; sidecar .mcp.json uses Streamable HTTP MCP because agent-stdio is not an MCP transport | Yes (ReplBridge) |
| **Unique Capabilities** | Auto-connect, rich hook surface, subagent import | Cowork-specific packaging, userConfig prompt, local stdio emphasis, cowork-contract | Classic MCP server bridge, explicit ENFORCEMENT.md | V2 AgentPlugin surface, full TS types | Workflow guidance, device loops, JSONL enrichment, batch req validate | Explicit mcpServers + skills declaration, Copilot status helper | Native Grok skills priority + PWSh modules + Grok/Claude-compatible manifests + GROK-USAGE.md | Long explicit tool surface in README, OpenCode SDK |
| **Primary Implementation** | Bash/Pwsh + Node helpers + SKILL.md | Node scripts + SKILL.md + .mcp.json | TypeScript (MCP SDK server) | TypeScript (Cline V2 AgentPlugin) | Shell + Node (Codex JSONL) + SKILL.md | Bash + SKILL.md + hooks.json | SKILL.md (primary) + Bash/Pwsh/Node | TypeScript (OpenCode plugin) |
| **Test Framework** | bats + PowerShell Pester | bats + PowerShell Pester | Jest (TS) | Jest (TS, incl. workspace) | bats + JS | bats + helpers | bats + PowerShell Pester | Jest (TS) |
| **Dedicated README** | Yes | Yes (detailed Cowork install + contract) | No (ENFORCEMENT.md + Plugin-Validation-Testing-Plan) | Yes (brief) | No (AGENTS.md + docs/plan) | No (Plugin-Validation-Testing-Plan) | Yes + GROK-USAGE.md | Yes |
| **Plugin Manifest(s)** | .claude-plugin/plugin.json + .codex-plugin/plugin.json | .claude-plugin/plugin.json (with skills + mcpServers + userConfig) | package.json (MCP server) | package.json (cline.plugins + capabilities) | .codex-plugin/plugin.json (skillsPath) | plugin.json (skills + hooks + mcpServers) | .grok-plugin/plugin.json + .claude-plugin/plugin.json + .mcp.json | package.json (peer @opencode-ai/plugin) |
| **Has Validation / Testing Plan** | Yes | No | Yes | No | Yes | Yes | Yes | No |

## Legend and Notes

- **Core Workflow Tools**: Always includes TODO management (with streaming plan/implement/status), Session Log (beginTurn/completeTurn/appendActions/query), Requirements (FR/TR/TEST + mappings + document gen), GraphRAG (entities/rels + ingest/query), and Workspace initialization/lifecycle.
- **Enforcement Scripts**: The three-phase per-user-message protocol (open turn on prompt, verify build after edits, stop-gate before final output) required by AGENTS-README-FIRST.yaml Rule 2/10 when the host lacks reliable hooks.
- **Subagent Capture**: Codex JSONL transcript import as first-class session turns (subagentComplete / final-response handling).
- **Offline Resilience**: Writes are cached locally when the MCP server or REPL is unreachable; flushed opportunistically or on session end.
- All plugins enforce the same canonical TODO ID format (`ISSUE-\d+` or `^[A-Z]+-[A-Z0-9]+-\d{3}$`) and requestId format (`req-YYYYMMDDTHHMMSSZ-...`).

## Cross-Plugin Observations

- **Hook-rich agents** (Claude Code, Copilot, Grok via manifests): Prefer declarative hooks for SessionStart, UserPromptSubmit, Stop, PostToolUse, compact events, and plan tracking. Enforcement scripts serve as fallback or diagnostics.
- **Hook-poor agents** (Cline, Codex, OpenCode): Rely on explicit per-turn script orchestration or agent prompt instructions (ENFORCEMENT.md). Codex adds dedicated guidance skills.
- **Native skill agents** (Grok, Claude variants, Copilot, Codex): Consume SKILL.md files directly; the REPL bridge remains available for advanced scenarios and cross-agent parity. For Grok, discoverable MCP tools use the Streamable HTTP names such as `sessionlog_*`, `todo_*`, and `requirements_*`; `workflow.*` names are shim/REPL method names used by the plugin skills and helpers, not literal `search_tool` results.
- **SDK plugin agents** (Cline v2, OpenCode): Register tools via host-specific createTool/plugin surfaces; implementation lives in src/tools/*.ts with shared ReplBridge + marker-resolver + cache logic.
- **Cowork special case**: Strongest emphasis on failsafe handoff files and explicit local-only stdio connector packaging. Never bypasses marker trust.

## Source of Truth

The authoritative behavioral contract for all plugins is the `AGENTS-README-FIRST.yaml` file present in every enabled workspace, combined with the shared REPL tool surface (`workflow.todo.*`, `workflow.sessionlog.*`, `workflow.requirements.*`, `workflow.graphrag.*`, `client.Workspace.*`).

## Related Documents

- [AGENT-PLUGIN-AVAILABILITY.md](./AGENT-PLUGIN-AVAILABILITY.md) - Operator guidance for acquiring and invoking the plugins
- [plans/plan-agent-plugin-operational-parity-v1.0.md](./plans/plan-agent-plugin-operational-parity-v1.0.md) — Detailed Byrd-compliant plan to eliminate all parity gaps identified in this matrix (tests-first, shared core, per-plugin adoption, full harness)
- [REPL-AGENT-GUIDE.md](./REPL-AGENT-GUIDE.md) - Direct REPL usage and envelope format
- Individual plugin READMEs and ENFORCEMENT.md / GROK-USAGE.md files
- Plugin Validation Testing Plans in each plugin's docs/ or root

---

*Generated for the McpServer ecosystem. All plugins are MIT licensed and follow the Byrd Development Process (tests first with mocks, then implementation, all tests green).*
