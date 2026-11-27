# TR-MCP-REPL

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-REPL-001

**YAML Envelope Protocol** — The REPL host SHALL parse incoming STDIO lines as YAML-formatted command envelopes containing `type`, `payload` with method-specific parameters, and optional `correlationId`/`requestId`. Response envelopes SHALL contain `type` (`result`/`error`/`event`), `payload` with result data or error details, and echoed identifiers. Malformed YAML SHALL emit structured error responses rather than crashing the process.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`IYamlEnvelope`, `IYamlSerializer`, `IReplProtocol`)
Scope: layer-1+

## TR-MCP-REPL-002

**DI-Integrated REPL Host** — The REPL host SHALL use DI composition for workflow and service registration. The command loop SHALL inject scoped service instances per command invocation and SHALL NOT instantiate services via `new` or `ActivatorUtilities.CreateInstance` outside DI registration paths. Workflows SHALL be registered as scoped services and resolved from the service provider.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Host` (`ServiceCollectionExtensions`, `Program.cs`), `McpServer.Repl.Core` workflow interfaces
Scope: layer-1+

## TR-MCP-REPL-003

**Command Loop Lifecycle** — The REPL host SHALL support graceful startup with command loop initialization, interactive STDIO processing, structured error handling with typed error codes, and clean shutdown on EOF or explicit exit. The command loop SHALL read YAML envelopes from stdin, dispatch to workflow handlers, serialize responses as YAML to stdout, and maintain session context across commands. Unhandled exceptions SHALL emit structured error responses and continue the loop.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Host` (`Program.cs`, `AgentStdioHandler`, `InteractiveHandler`), `McpServer.Repl.Core` (`SessionLogErrorEnvelope`)
Scope: layer-1+

## TR-MCP-REPL-004

**Command Registry and Dispatcher** — Workflow handlers SHALL implement typed interfaces (`ITodoWorkflow`, `ISessionLogWorkflow`, `IRequirementsWorkflow`, `IGenericClientPassthrough`) with async operation methods. Command dispatch SHALL resolve workflow instances from DI per invocation and SHALL pass deserialized parameters as strongly typed method arguments via YamlDotNet model binding. Command routing SHALL map YAML method names to workflow operations.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`ITodoWorkflow`, `ISessionLogWorkflow`, `IRequirementsWorkflow`, `IGenericClientPassthrough`), `McpServer.Repl.Host` (`TodoWorkflow`, `SessionLogWorkflow`, `RequirementsWorkflow`, `GenericClientPassthrough`)
Scope: layer-1+

## TR-MCP-REPL-005

**Namespace Organization and Handler Parity** — Command names SHALL use dot-delimited namespaces: `workflow.todo.*`, `workflow.session.*`, `workflow.requirements.*`, `client.*`. Handler implementations SHALL delegate to existing client contracts (`TodoClient`, `SessionLogClient`, `RequirementsClient`, `ContextClient`, `RepoClient`, `DesktopClient`) without duplicating business logic. Workflows SHALL maintain stateful context (TODO selection, session state) within the REPL process.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`TodoCommandShapes`, `SessionLogCommandShapes`, `RequirementsCommandShapes`, `ClientCommandShapes`), `McpServer.Repl.Host` (`TodoWorkflow`, `SessionLogWorkflow`, `RequirementsWorkflow`, `GenericClientPassthrough`)
Scope: layer-1+

## TR-MCP-REPL-006

**Trust Bootstrap and Token Validation** — The REPL host SHALL implement marker-file trust bootstrap with signature verification and health nonce challenge before accepting operational commands. API key authentication SHALL use per-workspace token semantics from marker files. The host SHALL detect API key rotation between commands via marker file watch and SHALL emit warnings when tokens become stale. Trust verification SHALL use the same contract as PowerShell modules.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`ITrustBootstrapService`, `IMarkerFileReader`, `IAuthRotationHandler`), `McpServer.Repl.Host` (`AgentStdioHandler`)
Scope: layer-1+

## TR-MCP-REPL-007

**State Query Commands** — The REPL host SHALL expose commands for querying workspace state via generic client passthrough: context search, repository operations, desktop launch validation, and requirements operations. Handlers SHALL query current service state snapshots through typed client interfaces without blocking on long-running operations. All client operations SHALL support the generic passthrough pattern for extensibility.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`IGenericClientPassthrough`, `ClientCommandShapes`), `McpServer.Repl.Host` (`GenericClientPassthrough`)
Scope: layer-1+

## TR-MCP-REPL-008

`MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(workspacePathOverride, markerPathOverride, out options, out error)` returns success/failure plus a human-readable diagnostic. The diagnostic enumerates every directory walked, names the marker file when found, and distinguishes "not found" from "malformed" and "signature mismatch". `FindMarkerFile(startPath, out searchedPaths)` exposes the same path list for callers that want raw enumeration. The legacy parameterless `Resolve()` remains for back-compat.
**Covered by:** FR: FR-MCP-REPL-007; TEST: TEST-MCP-REPL-007-1, TEST-MCP-REPL-007-2, TEST-MCP-REPL-007-3
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-009

**Explicit agent identity propagation** — REPL host, plugin launchers, and verified marker cache resolution must use a single agent identity precedence and canonical cache key so Codex, Claude, Grok, and other agents cannot share stale trust or cache state accidentally.
**Covered by:** FR: FR-MCP-REPL-008; TEST: TEST-MCP-REPL-018
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Agent resolution precedence is explicit agent, MCP_AGENT_NAME, PLUGIN_AGENT_NAME, PLUGIN_AGENT_DEFAULT, MCP_PLUGIN_HOST, then default. (evidence: src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs; plugins/core/lib-sh/repl-invoke.sh; plugins/core/lib-ps/repl-invoke.ps1; plugins/core/lib-sh/repl-daemon.js; plugins/core/lib-node/src/transport/repl-bridge.ts)
- [x] Production resolver calls do not persist supplied --agent values through the mutable AgentOverride test hook. (evidence: src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs; tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)
- [x] Verified marker cache writes use a bounded inter-process lock and atomic replacement to avoid concurrent agent read-modify-write races. (evidence: src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs)
- [x] Resolver fallback surfaces a construction-time diagnostic before legacy resolution is used. (evidence: src/McpServer.Repl.Host/Program.cs)

## TR-MCP-REPL-010

**Independent REPL session-log persistence strategies** — McpServer.Repl.Core SHALL define separate primary MCP and filesystem failsafe session-log persistence strategies plus a failover coordinator. REPL session-log persistence calls SHALL route through the coordinator, suppress degraded notifications for non-terminal plugin operations after durable fallback, and return terminal persistence details. The failsafe strategy SHALL atomically write a replayable session-log envelope to the V4 workspace-and-agent-scoped pending path.
**Covered by:** FR: FR-MCP-REPL-009; TEST: TEST-MCP-REPL-025, TEST-MCP-REPL-026, TEST-MCP-REPL-027, TEST-MCP-REPL-028
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Primary and failsafe persistence implementations are independently mockable and neither implementation contains fallback orchestration. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Failover does not catch explicit caller cancellation and propagates an error when both primary and failsafe persistence fail. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Failsafe writes use an atomic replace or move and return the final absolute artifact path. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Terminal dispatcher results expose degraded, persistenceStrategy, failsafePath, and message fields. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-MCP-REPL-011

**PascalCase session-id agent + openSession persistence** — Composed plugin session ids SHALL use a PascalCase source-type agent segment (never lowercase 'default' or a lowercase host key) so they satisfy the server sessionId regex ^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$; and workflow.sessionlog.openSession SHALL persist an explicit valid sessionId into session-state.yaml instead of being a success no-op. Acceptance Criteria: (AC1) Get-ReplCanonicalAgentName maps 'default' to 'Default' (regex-valid), 'claude-code'/'claudecode' to 'ClaudeCode', 'codex' to 'Codex', 'grok' to 'GrokCode', and its output always matches ^[A-Z][A-Za-z0-9]*$; New-ReplPluginSessionId composes the agent segment through it; (AC2) Invoke-WorkflowOpenSession given an explicit sessionId writes status=verified + that sessionId into session-state.yaml and returns true. Origin: BUG-TRIAGE-085 (AgentName fell back to lowercase 'default' producing server-rejected ids; openSession never persisted an explicit id). Applied to plugins/core/lib-ps/repl-invoke.ps1 (canonical) and mcpserver-claude-code-plugin/lib; do not relax server validation. Validated by TEST-MCP-REPL-011.
**Covered by:** FR: FR-MCP-REPL-009; TEST: TEST-MCP-REPL-025, TEST-MCP-REPL-026, TEST-MCP-REPL-027, TEST-MCP-REPL-028
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Get-ReplCanonicalAgentName maps default to Default, claude-code/claudecode to ClaudeCode, codex to Codex, grok to GrokCode, and output always matches ^[A-Z][A-Za-z0-9]*$. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Invoke-WorkflowOpenSession given an explicit sessionId writes status=verified plus that sessionId into session-state.yaml and returns true. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-MCP-REPL-012

**Per-method REPL invocation timeout** — REPL invocation timeouts SHALL be resolved per method: long-running requirement/agent methods (workflow.todo.analyzeRequirements, workflow.requirements.generateDocument/ingestDocument/analyze*, client.Requirements.Analyze*) SHALL use an extended, env-configurable budget (REPL_LONG_TIMEOUT, default 300s) while all other methods keep the short default (REPL_TIMEOUT, default 30s). Acceptance Criteria: (AC1) Get-ReplMethodTimeoutSeconds returns >30 for analyzeRequirements/generateDocument and 30 for sessionlog methods; (AC2) REPL_TIMEOUT overrides the short default and REPL_LONG_TIMEOUT overrides the long budget; Invoke-ReplRaw uses Get-ReplMethodTimeoutSeconds. Origin: BUG-TRIAGE-072 (workflow.todo.analyzeRequirements, which blocks on an external Copilot CLI, was killed at the flat 30s REPL_TIMEOUT). Applied to plugins/core/lib-ps/repl-invoke.ps1 (canonical) and mcpserver-claude-code-plugin/lib. Validated by TEST-MCP-REPL-027.
**Covered by:** FR: FR-MCP-REPL-009, FR-MCP-REPL-011; TEST: TEST-MCP-REPL-025, TEST-MCP-REPL-026, TEST-MCP-REPL-027, TEST-MCP-REPL-028, TEST-MCP-REPL-031, TEST-MCP-REPL-032, TEST-MCP-REPL-033, TEST-MCP-REPL-034, TEST-MCP-REPL-035, TEST-MCP-REPL-036, TEST-MCP-REPL-037, TEST-MCP-REPL-038, TEST-MCP-REPL-039, TEST-MCP-SESSIONLIFE-005
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Get-ReplMethodTimeoutSeconds returns greater than 30 for analyzeRequirements/generateDocument and 30 for sessionlog workflow methods completeTurn and beginTurn. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] REPL_TIMEOUT overrides the short default and REPL_LONG_TIMEOUT overrides the long budget; Invoke-ReplRaw uses Get-ReplMethodTimeoutSeconds. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] client.SessionLog.SubmitAsync while ReplFailsafeDraining uses REPL_FAILSAFE_DRAIN_TIMEOUT default 120 or REPL_TIMEOUT when that is greater; this does not raise completeTurn/beginTurn above 30s. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-MCP-REPL-013

**Marker-bearing current directory outranks inherited workspace env for repl resolution** — A marker-bearing current directory SHALL outrank inherited workspace environment variables when the repl bridge resolves the active workspace, so a hook launched from the correct repo binds to that repo's marker rather than a stale inherited MCP_WORKSPACE_PATH. AC1: with a marker at cwd and a conflicting MCP_WORKSPACE_PATH, resolution returns cwd. Guards BUG-TRIAGE-077 via TEST-MCP-REPL-028 (ReplWorkspaceResolution.Tests.ps1).
**Covered by:** FR: FR-MCP-REPL-009; TEST: TEST-MCP-REPL-025, TEST-MCP-REPL-026, TEST-MCP-REPL-027, TEST-MCP-REPL-028
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] With a marker at cwd and a conflicting MCP_WORKSPACE_PATH, resolution returns cwd. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-MCP-REPL-014

**Plugin setTurnTitle and setSessionTitle handlers** — repl-invoke.ps1 Invoke-ReplMethod MUST dispatch workflow.sessionlog.setTurnTitle to a handler that updates current-turn.yaml queryTitle (Set-ReplTurnCacheField) and calls the server turn title-update path, and workflow.sessionlog.setSessionTitle to a handler that read-modify-writes session-state.yaml title and calls the server session title-update path. Both MUST fail closed (non-zero result) when no active session/turn cache is present. Validated by TEST-MCP-REPL-029. Covered by FR: FR-MCP-REPL-010. Status: pending. Scope: layer-1+.
**Covered by:** FR: FR-MCP-REPL-010; TEST: TEST-MCP-REPL-029, TEST-MCP-REPL-030
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-015

**Plugin omits titles on incidental whole-session re-submit** — Invoke-ReplPersistTurn MUST omit the turn QueryTitle and the session Title on incidental re-submits (supersede, appendActions, appendDialog, updateTurn, completeTurn) unless a title is explicitly supplied in that operation. Because the server preserves omitted fields (FR-SUPPORT-015), an agent-set or server-preserved title then survives the whole-session re-submit instead of being clobbered by the stale local cache value. beginTurn still seeds the new turn's provisional QueryTitle, and seeds the session Title only on the first turn (when session-state has no title yet); an explicit queryTitle param on appendActions/appendDialog/updateTurn/completeTurn still updates the turn title. Validated by TEST-MCP-REPL-030. Covered by FR: FR-MCP-REPL-010. Status: pending. Scope: layer-1+.
**Covered by:** FR: FR-MCP-REPL-010; TEST: TEST-MCP-REPL-029, TEST-MCP-REPL-030
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-016

**Failsafe queue drains oldest-first after a proven-reachable backend call** — BUG-TRIAGE-097. The plugin PowerShell runtime captures every session-log submit into the failsafe queue before the remote call but never replays it, so records accumulate forever (33 on disk in F:/GitHub/McpServer, oldest 2026-07-14). plugins/core/lib-ps/repl-invoke.ps1 MUST expose Invoke-ReplFailsafeDrain, which walks the failsafe directory oldest-first (the file name is prefixed with the UTC capture stamp, so a name sort is a chronological sort) and re-issues each record's captured method and params through Invoke-ReplRaw. A record MUST be deleted only after its submission succeeds; a record whose submission fails MUST stay on disk. client.SessionLog.SubmitAsync is an upsert keyed by sessionId plus requestId, so a replay is idempotent. A record rejected by the backend MUST NOT block the newer records behind it: the drain increments that record's drainAttempts counter and continues the walk. A transport-level failure (MCP_UNTRUSTED, mcpserver-repl not on PATH, timeout, refused connection) MUST abort the pass without consuming any attempt budget, because the backend and not the record is at fault. The drain MUST skip the record belonging to the submit currently in flight, so a drain triggered inside an active submit cannot double-submit or delete the live turn. The drain MUST be wired to the first successful Invoke-ReplRaw in the process (Invoke-ReplFailsafeDrainOnFirstSuccess, latched to run at most once and disabled by MCP_FAILSAFE_DRAIN_DISABLED=1) rather than to bootstrap: bootstrap only proves the marker file is fresh, does not prove the backend answers, and runs inside Invoke-ReplRaw, so draining there would recurse. Operators MUST be able to force a full pass with the plugin-local verb workflow.failsafe.drain (optional maxRecords and maxAttempts params), which prints the drain summary as YAML and exits non-zero only when the pass aborted.
**Covered by:** FR: FR-MCP-REPL-011; TEST: TEST-MCP-REPL-031, TEST-MCP-REPL-032, TEST-MCP-REPL-033, TEST-MCP-REPL-034, TEST-MCP-REPL-035, TEST-MCP-REPL-036, TEST-MCP-REPL-037, TEST-MCP-REPL-038, TEST-MCP-REPL-039, TEST-MCP-SESSIONLIFE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-017

**Failsafe quarantine and truthful pending queue depth in plugin status** — BUG-TRIAGE-097. A failsafe record that cannot be replayed MUST be quarantined rather than retried forever or deleted, because a captured record can be the only copy of a turn. plugins/core/lib-ps/repl-invoke.ps1 MUST move a record to a quarantine subdirectory of the failsafe directory, next to a sibling .reason.txt naming the quarantine timestamp, the original path, and the reason, when the record is not readable YAML, when its root is not a mapping, when it has no method or no params, or when its drainAttempts counter has reached the attempt budget (default 5). Quarantined records MUST NOT be counted as live queue depth. plugins/core/lib-ps/resolve-cache-dir.ps1 MUST own the single resolution of the queue location (Get-McpFailsafeDir and Get-McpFailsafeQuarantineDir, honouring MCPSERVER_FAILSAFE_DIR then MCP_FAILSAFE_DIR then the workspace cache) so the writer, the drain, and the status reporter cannot disagree. plugins/core/lib-ps/mcp-status.ps1 MUST count the failsafe directory: pendingCount previously counted only the cache pending directory and therefore reported 0 while 33 captured submits sat undrained, so pendingCount MUST be the sum of pending turn records and queued failsafe records, and the status document MUST additionally expose pendingTurnCount, failsafeDir, failsafeCount, and failsafeQuarantineCount. A read-only workflow.failsafe.status verb MUST report the same depth without replaying anything.
**Covered by:** FR: FR-MCP-REPL-011; TEST: TEST-MCP-REPL-031, TEST-MCP-REPL-032, TEST-MCP-REPL-033, TEST-MCP-REPL-034, TEST-MCP-REPL-035, TEST-MCP-REPL-036, TEST-MCP-REPL-037, TEST-MCP-REPL-038, TEST-MCP-REPL-039, TEST-MCP-SESSIONLIFE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-018

**Turn upsert accepts a deliberately empty title** — Implements FR-MCP-143 and closes BUG-TRIAGE-087, 089, 091, 098, the PowerShell half of 090 and 094, and the PowerShell half of 101. plugins/core/lib-ps/repl-invoke.ps1 declares the turn-upsert Title parameter as [Parameter(Mandatory)][string] with no [AllowEmptyString()] at line 944, while callers deliberately pass an empty string per TR-MCP-REPL-015 so a stale cached title is not resubmitted. PowerShell rejects the bind before any request is made, so appendDialog without a queryTitle, appendActions, completeTurn, and the supersede path all fail with a mandatory-parameter binding error rather than persisting. The parameter SHALL accept an empty string, and every call path that legitimately passes one SHALL persist. Any other mandatory string parameter on the same path that would reject a legitimate empty value SHALL be corrected the same way. Pester coverage SHALL prove the binding failure before the fix and the successful persist after, using a temporary cache directory so no test touches the real workspace state.
**Covered by:** FR: FR-MCP-143; TEST: TEST-MCP-194, TEST-MCP-REPL-040
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-019

**Supersede preserves a server-refined turn title** — Implements FR-MCP-143 and closes BUG-TRIAGE-086. Invoke-ReplSupersedeCurrentTurnIfInProgress in plugins/core/lib-ps/repl-invoke.ps1 reads queryTitle only from the local current-turn.yaml and then re-persists the canceled turn, so a title the agent refined server-side is overwritten by the stale local value and the session title is clobbered on every prompt. The hook cannot summarize, so it writes the prompt first line or the literal placeholder, which means the local copy is frequently the worse of the two. The supersede path SHALL prefer the server-side title when the local cache holds no title or holds only the hook's raw default, and SHALL never replace a refined title with raw prompt text. Coverage SHALL prove that a refined title survives a supersede and that a genuinely absent title still results in a persisted turn.
**Covered by:** FR: FR-MCP-143; TEST: TEST-MCP-194, TEST-MCP-REPL-040
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-020

**failTurn resolves the active turn from the plugin cache** — Implements FR-MCP-143 and closes BUG-TRIAGE-099. Plugin Status reports hasSession and hasCurrentTurn from the local cache (session-state.yaml carrying verified plus a non-empty sessionId, and the presence of current-turn.yaml), and the beginTurn path writes those files, but failTurn requires an empty in-process REPL state and otherwise returns "No active session exists". The result is that a turn the plugin reports as active cannot be closed as failed, so a wrap-up cannot truthfully record a validation failure and the turn is left in progress forever. failTurn SHALL resolve the active session and turn the same way the rest of the plugin does, falling back to the cache when the in-process state is empty, and SHALL close the turn as failed when the cache identifies one. Coverage SHALL prove the current refusal before the fix and a successful fail-close afterwards, using a temporary cache directory.
**Covered by:** FR: FR-MCP-143; TEST: TEST-MCP-194, TEST-MCP-REPL-040
**Status:** pending
Scope: layer-1+

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
