# FR-MCP-REPL

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-REPL-001 YAML Protocol STDIO REPL Host

The server shall provide a YAML-envelope STDIO REPL host that accepts structured commands over standard input, executes operations against workspace services, and returns structured YAML responses over standard output. The REPL host shall support the same trust bootstrap, authentication, and workspace resolution semantics as the HTTP and MCP STDIO transports.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-001](./Technical-Requirements.md#tr-mcp-repl-001) | [TR-MCP-REPL-002](./Technical-Requirements.md#tr-mcp-repl-002) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Core` (`IReplProtocol`, `IYamlEnvelope`, `IYamlSerializer`, `IMarkerFileReader`, `ITrustBootstrapService`, `IAuthRotationHandler`, `IWorkspaceSelector`), `McpServer.Repl.Host` (`Program.cs`, `AgentStdioHandler`, `InteractiveHandler`, `ServiceCollectionExtensions`)
Scope: layer-1+

## FR-MCP-REPL-002 REPL Lifecycle Management

The REPL host shall support graceful startup, interactive command loop, structured error handling with typed error codes, and clean shutdown on EOF or explicit exit commands. The host shall maintain session context across commands within a single process invocation and emit lifecycle events for observability.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-003](./Technical-Requirements.md#tr-mcp-repl-003) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Host` (`Program.cs`, `AgentStdioHandler`, `InteractiveHandler`), `McpServer.Repl.Core` (`SessionLogErrorEnvelope`)
Scope: layer-1+

## FR-MCP-REPL-003 Command Namespace Parity

The REPL command surface shall provide namespace-organized commands with functional parity to HTTP REST endpoints and MCP STDIO tools for TODO operations, session log operations, context operations, requirements management, workspace management, and agent pool operations. Command routing shall reuse existing service contracts without duplicating business logic.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-004](./Technical-Requirements.md#tr-mcp-repl-004) | [TR-MCP-REPL-005](./Technical-Requirements.md#tr-mcp-repl-005) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Core` (`ITodoWorkflow`, `TodoCommandShapes`, `ISessionLogWorkflow`, `SessionLogCommandShapes`, `SessionLogModels`, `IRequirementsWorkflow`, `RequirementsCommandShapes`, `RequirementsCommandModels`, `IGenericClientPassthrough`, `ClientCommandShapes`), `McpServer.Repl.Host` (`TodoWorkflow`, `RequirementsWorkflow`, `SessionLogWorkflow`, `GenericClientPassthrough`)
Scope: layer-1+

## FR-MCP-REPL-004 Trust Bootstrap and Auth Rotation

The REPL host shall implement marker-file trust bootstrap with signature verification and health nonce challenge before accepting operational commands. API key authentication shall use the same per-workspace token semantics as HTTP endpoints. The host shall detect API key rotation between commands and emit warnings when tokens become stale.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-006](./Technical-Requirements.md#tr-mcp-repl-006) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Core` (`ITrustBootstrapService`, `IMarkerFileReader`, `IAuthRotationHandler`), `McpServer.Repl.Host` (`AgentStdioHandler`)
Scope: layer-1+

## FR-MCP-REPL-005 Orchestration State Visibility

The REPL host shall expose commands for querying agent pool state, active voice sessions, queued one-shot requests, and workspace notification subscriptions. State queries shall return current snapshots without blocking on long-running operations.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-007](./Technical-Requirements.md#tr-mcp-repl-007) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Core` (`IGenericClientPassthrough`, `ClientCommandShapes`), `McpServer.Repl.Host` (`GenericClientPassthrough`)

---

## REPL v1.0 Requirements Freeze

**Freeze Tag:** `REPL-v1.0-FREEZE` | **Date:** 2025-01-04

All REPL functional requirements (FR-MCP-REPL-001 through FR-MCP-REPL-005) are complete and frozen for v1.0 delivery. Full source code traceability comments have been added to all `McpServer.Repl.Core` and `McpServer.Repl.Host` files. All iteration 1-6 unit tests and integration tests pass. No defects remain.


---
Scope: layer-1+

## FR-MCP-REPL-006 Deprecate workflow.* REPL namespaces in favor of client.* passthrough

The REPL SHALL mark every workflow.* response deprecated and route workflow.sessionlog lifecycle verbs with explicit identifiers statelessly through the SessionLog client.
Scope: layer-1+

## FR-MCP-REPL-007 REPL Credential Discovery Diagnostics

`mcpserver-repl --agent-stdio` shall expose `--workspace-path` and `--marker-file` CLI overrides for credential resolution. When marker discovery fails, the diagnostic message shall enumerate every directory searched and distinguish "marker not found" from "marker signature mismatch". The diagnostic is forwarded into the `McpServerClient` and appended to the "Authentication required" exception so callers see the root cause rather than the generic message.

**Covered by:** `MarkerFileClientOptionsResolver.TryResolveWithDiagnostics`, `Program.cs` (`--workspace-path` / `--marker-file`), `McpClientBase.EnsureAuthenticated`
Scope: layer-1+

## FR-MCP-REPL-008 Per-agent REPL isolation

Agent REPL callers must identify the invoking agent on every mcpserver-repl invocation so session, cache, and verified marker state are isolated per agent and workspace.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Every plugin REPL launcher passes --agent, including the default agent value. (evidence: plugins/core/lib-sh/repl-invoke.sh; plugins/core/lib-ps/repl-invoke.ps1; plugins/core/lib-sh/repl-daemon.js; plugins/core/lib-node/src/transport/repl-bridge.ts)
- [x] Verified marker cache entries are keyed by canonical agent identity and workspace. (evidence: src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs; tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)
- [x] Named-agent child process invocation is covered by integration tests. (evidence: tests/McpServer.Repl.IntegrationTests/ReplChildProcessHelper.cs; tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)

## FR-MCP-REPL-009 Degraded session-log persistence isolation

The MCP REPL SHALL isolate plugins from MCP Session Log service degradation during session and in-progress turn operations. When a turn closes while the primary service is degraded, the REPL SHALL durably persist the complete turn through an independent failsafe strategy and SHALL notify the caller with the failsafe path.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Open, begin, update, appendDialog, and appendActions remain successful when primary MCP Session Log persistence fails but failsafe persistence succeeds. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] The failsafe artifact contains a replayable session-log persistence envelope including the attempted turn data and status. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] completeTurn and failTurn report degraded persistence, the independent strategy name, and the absolute failsafe path without claiming primary MCP persistence succeeded. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Primary persistence success produces no pending failsafe artifact and reports normal MCP persistence. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-MCP-REPL-010 Plugin session-log title refinement surfaces

The MCP REPL plugin SHALL expose setTurnTitle and setSessionTitle workflow methods that durably update the active turn's title and the session title through explicit server title-update paths, so an agent-refined title survives the whole-session re-submit the plugin issues on every subsequent persist. Scope: layer-1+. Acceptance Criteria: (1) workflow.sessionlog.setTurnTitle updates current-turn.yaml queryTitle and persists the new title to the server for the active turn; (2) workflow.sessionlog.setSessionTitle writes session-state.yaml title and persists the new session title to the server; (3) a subsequent supersede/complete/append whole-session re-submit does not revert either title.
Scope: layer-1+

## FR-MCP-REPL-011 Plugin failsafe queue drains instead of accumulating

BUG-TRIAGE-097. The MCP plugin PowerShell runtime writes every session-log submit to a failsafe queue on disk before calling the backend, so a crash or an unreachable server cannot lose the turn. Until now nothing ever replayed those records: the queue only grew, and plugin Status reported pendingCount 0 while captured turns sat undrained (33 records in F:/GitHub/McpServer as of 2026-07-20, oldest 2026-07-14, eight added in a single session). An agent or operator MUST be able to get queued turns into the session log once the backend is reachable again, automatically on the first proven-reachable call and on demand through an explicit drain verb, without ever losing a record: a record leaves the queue only after its submission is confirmed, a record the backend rejects does not block the newer records behind it, and a record that cannot be replayed at all is set aside for inspection rather than deleted or retried forever. Plugin Status MUST report the real queue depth.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each session-log submit is written to the workspace and agent scoped failsafe queue before backend submission, and both automatic proven-reachable replay and explicit drain process records oldest first.
- [ ] A recovery record is deleted only after the backend confirms durable persistence for the exact workspace, agent, session, and request.
- [ ] A backend-rejected record does not block newer records, and a structurally unreplayable or exhausted record is quarantined with diagnostics rather than deleted or retried forever.
- [ ] Plugin status reports the real pending and quarantined queue depth for the active workspace and agent.
- [ ] Drain submission uses the 120-second default helper budget and honors a larger REPL_TIMEOUT; an ordinary two-second nested helper budget is not applied to replay. Automatic nested drain defers while a REPL call is in flight and a timeout abort leaves drainAttempts and completion latch unchanged.

