# TEST-MCP-REPL

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-REPL

### TEST-MCP-REPL-001

✅ **Complete** - Given a REPL host process, when a well-formed YAML command envelope is sent to stdin, then a YAML response envelope is emitted to stdout with `type: result` and the expected result payload. **Covered by:** `Iteration1_IntegrationTests`, `YamlFramingTests`, `YamlEnvelopeShapeTests`


### TEST-MCP-REPL-002

✅ **Complete** - Given a REPL host process, when malformed YAML is sent to stdin, then a structured error response is emitted with `type: error` and descriptive error details, without crashing the host process. **Covered by:** `FakeYamlSerializerTests`, `YamlFramingTests`


### TEST-MCP-REPL-003

✅ **Complete** - Given a REPL host with no bootstrap invocation, when an operational command is sent, then the response contains `type: error` and appropriate error code. **Covered by:** `ProtocolHandshakeTests`, `TrustBootstrapFlowTests`


### TEST-MCP-REPL-004

✅ **Complete** - Given a REPL host, when trust bootstrap is invoked with a valid marker file, then the host verifies the marker signature, performs the health nonce challenge, caches the API key, and returns success. **Covered by:** `TrustBootstrapFlowTests`, `MarkerFileTrustTests`, `MockTrustBootstrapServiceTests`


### TEST-MCP-REPL-005

✅ **Complete** - Given a REPL host with completed bootstrap, when the API key in the marker file is rotated, then the host detects rotation via marker file watch and emits appropriate notifications. **Covered by:** `AuthRotationTests`, `AuthKeyAndWorkspaceTests`, `StubAuthRotationHandlerTests`


### TEST-MCP-REPL-006

✅ **Complete** - Given bootstrapped REPL commands for TODO operations (`workflow.todo.*`), when invoked with valid args, then results match the equivalent client operation semantics. **Covered by:** `TodoWorkflowTests`, `Iteration3IntegrationTests`, `TodoWorkflowTestExtensions`


### TEST-MCP-REPL-007

✅ **Complete** - Given bootstrapped REPL commands for session log operations (`workflow.session.*`), when invoked with valid args, then results match the equivalent client operation semantics. **Covered by:** `SessionLogWorkflowTests`, `SessionLogWorkflowIntegration2Tests`, `SessionLogWorkflowProductionTests`, `Iteration2IntegrationTests`


### TEST-MCP-REPL-007-1

Given `TryResolveWithDiagnostics` with a workspace path containing no marker file, when called, then the error message enumerates every directory walked from the start path to its root. **Covered by:** `MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_WhenMarkerMissing_EnumeratesSearchedPaths`


### TEST-MCP-REPL-007-2

Given an explicit `workspacePathOverride` pointing to a workspace with a valid marker, when `TryResolveWithDiagnostics` is called, then resolution succeeds and the returned options carry the marker's API key. **Covered by:** `MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_AcceptsExplicitWorkspaceArgument`


### TEST-MCP-REPL-007-3

Given a marker file whose canonicalization is tampered, when `TryResolveWithDiagnostics` is called, then the error names the marker path and identifies "signature" failure. **Covered by:** `MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_WhenSignatureFails_ReportsReason`


### TEST-MCP-REPL-007-4

Given a marker whose HMAC payload is signed with LF-only (`\n`) line endings (matching the production server's `MarkerFileService.AppendPayloadLine`), when `TryResolveWithDiagnostics` is called on Windows or any platform where `Environment.NewLine` differs from `\n`, then signature verification succeeds. **Covered by:** `MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_VerifiesSignatureBuiltWithLfLineEndings`


### TEST-MCP-REPL-008

✅ **Complete** - Given bootstrapped REPL commands for context operations (`client.context.*`), when invoked with valid args, then results match the equivalent client operation semantics. **Covered by:** `GenericClientPassthroughTests`, `Iteration5IntegrationTests`


### TEST-MCP-REPL-009

✅ **Complete** - Given bootstrapped REPL commands for requirements management (`workflow.requirements.*`), when invoked with valid args, then results match the equivalent client operation semantics. **Covered by:** `RequirementsWorkflowTests`, `Iteration4IntegrationTests`


### TEST-MCP-REPL-010

✅ **Complete** - Given bootstrapped REPL commands for workspace selection, when invoked with valid workspace paths, then workspace context resolution matches expected behavior. **Covered by:** `WorkspaceSelectionTests`, `AuthKeyAndWorkspaceTests`


### TEST-MCP-REPL-011

✅ **Complete** - Given generic client passthrough operations, when invoked with valid method/args, then operations delegate to the correct client type and method without duplicating logic. **Covered by:** `GenericClientPassthroughTests`, `Iteration5IntegrationTests`


### TEST-MCP-REPL-012

✅ **Complete** - Given streaming TODO operations (`streamStatus`, `streamPlan`, `streamImplement`), when invoked, then events stream correctly with proper cancellation handling. **Covered by:** `TodoWorkflowTests` (streaming event tests)


### TEST-MCP-REPL-013

✅ **Complete** - Given a REPL host, when EOF is received on stdin, then the host terminates gracefully. **Covered by:** `EndToEndFlowTests`


### TEST-MCP-REPL-014

✅ **Complete** - Given a workflow operation that throws an exception, when the command is executed, then a structured error response is emitted and the command loop continues. **Covered by:** `SessionLogWorkflowTests`, `TodoWorkflowTests` (error handling paths)


### TEST-MCP-REPL-015

✅ **Complete** - Given request IDs in command envelopes, when commands are executed, then response envelopes echo the same request ID for request/response matching. **Covered by:** `RequestResponseCorrelationTests`, `YamlEnvelopeShapeTests`


### TEST-MCP-REPL-016

✅ **Complete** - Given workflow implementations, when registered in DI, then all dependencies are resolved from the container and no services are instantiated via `new` outside DI. **Covered by:** `McpServerClientIntegrationTests`, DI registration tests


### TEST-MCP-REPL-017

✅ **Complete** - Given workspace selection via workspace selector, when commands target specific workspaces, then workspace context is properly scoped. **Covered by:** `WorkspaceSelectionTests`, `AuthKeyAndWorkspaceTests`


### TEST-MCP-REPL-018

Tests must verify canonical agent keying, explicit --agent propagation, absence of production AgentOverride leakage, and named-agent child process launch behavior.

**Acceptance Criteria:**
- [x] Canonicalization theory covers Codex, ClaudeCode, GrokCode, and OpenCode-style names. (evidence: tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)
- [x] Resolver test proves cache record agent key is canonical and AgentOverride remains unset after production resolution. (evidence: tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)
- [x] Child process helper test proves --agent and the named value are present in the launched REPL argument list. (evidence: tests/McpServer.Repl.IntegrationTests/ReplChildProcessHelper.cs; tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)

### TEST-MCP-REPL-019

✅ **Complete** - Given namespace-organized command shapes, when workflows execute, then operations delegate to typed client contracts without duplicating business logic. **Covered by:** `TodoWorkflowTests`, `SessionLogWorkflowTests`, `RequirementsWorkflowTests`, `GenericClientPassthroughTests`


### TEST-MCP-REPL-020

✅ **Complete** - Given concurrent REPL operations, when workflows maintain stateful context, then session state and TODO selection are properly isolated per workflow instance. **Covered by:** `SessionLogWorkflowTests` (state management), `TodoWorkflowTests` (selection state)


### TEST-MCP-REPL-021

When TryResolveWithDiagnostics is called on a workspace path containing no marker file, the error message enumerates every directory walked from the start path to its root. This verifies diagnostic usability when marker files cannot be found during workspace resolution.

**Acceptance Criteria:**
- [x] Error message enumerates all directories walked from start path to root
- [x] Test passes with implementation in MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_WhenMarkerMissing_EnumeratesSearchedPaths

### TEST-MCP-REPL-022

When an explicit workspacePathOverride pointing to a workspace with a valid marker is provided to TryResolveWithDiagnostics, resolution succeeds and the returned options carry the marker's API key.

**Acceptance Criteria:**
- [x] Resolution succeeds when explicit workspace path points to valid marker
- [x] Returned options contain the marker's API key

### TEST-MCP-REPL-023

When a marker file's canonicalization is tampered and TryResolveWithDiagnostics is called, the error names the marker path and identifies 'signature' failure. This verifies secure marker validation and error reporting.

**Acceptance Criteria:**
- [x] Error message names the marker path
- [x] Error identifies 'signature' as the failure reason

### TEST-MCP-REPL-024

Given a marker whose HMAC payload is signed with LF-only (\n) line endings (matching production MarkerFileService.AppendPayloadLine), when TryResolveWithDiagnostics is called on Windows or any platform where Environment.NewLine differs from \n, signature verification succeeds. This ensures cross-platform marker compatibility.

**Acceptance Criteria:**
- [x] Signature verification succeeds for LF-only signed payload on all platforms
- [x] Test validates cross-platform Environment.NewLine compatibility

### TEST-MCP-REPL-025

Mock-backed unit and real-filesystem integration tests SHALL prove primary and failsafe strategy isolation, non-terminal degradation isolation, terminal notification, replay artifact fidelity, V4 path scoping, atomic writes, cancellation, dual failure, and normal primary behavior with zero failures and zero skips.

**Acceptance Criteria:**
- [x] A failing primary mock does not fail non-terminal plugin persistence when the failsafe mock succeeds. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)
- [x] Closing a degraded turn returns the exact failsafe path and the artifact round-trips the attempted turn payload. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)
- [x] Primary success, explicit cancellation, and dual-failure paths are covered. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)
- [x] Current and prior McpServer.Repl.Core scopes complete with zero failures and zero skips. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)

### TEST-MCP-REPL-026

Validates TR-MCP-REPL-011 (PascalCase session-id agent + openSession persistence). mcpserver-claude-code-plugin/tests/SessionIdCanonicalAgent.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1: asserts Get-ReplCanonicalAgentName('default')='Default' and matches ^[A-Z][A-Za-z0-9]*$, 'claude-code'/'claudecode'='ClaudeCode', 'codex'='Codex', 'grok'='GrokCode'; and Invoke-WorkflowOpenSession with 'sessionId: ClaudeCode-...-explicit' writes status=verified + that sessionId into session-state.yaml and returns true. Red before implementation (functions did not exist / openSession was a no-op), green after. Note: uses id 026 because TEST-MCP-REPL-011 was already taken.

**Acceptance Criteria:**
- [x] Get-ReplCanonicalAgentName default is Default and matches PascalCase regex; claude-code is ClaudeCode; codex is Codex; grok is GrokCode. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)
- [x] Invoke-WorkflowOpenSession with explicit ClaudeCode sessionId writes status=verified and that sessionId into session-state.yaml and returns true. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)

### TEST-MCP-REPL-027

Validates TR-MCP-REPL-012. mcpserver-claude-code-plugin/tests/ReplMethodTimeout.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1: asserts Get-ReplMethodTimeoutSeconds returns >30 for workflow.todo.analyzeRequirements and workflow.requirements.generateDocument and exactly 30 for workflow.sessionlog.completeTurn/beginTurn; and with REPL_TIMEOUT=45/REPL_LONG_TIMEOUT=600 set, returns 45 for sessionlog and 600 for analyzeRequirements. Red before implementation (function absent), green after; Invoke-ReplRaw now uses Get-ReplMethodTimeoutSeconds.

**Acceptance Criteria:**
- [x] Get-ReplMethodTimeoutSeconds returns greater than 30 for analyzeRequirements and generateDocument and exactly 30 for completeTurn and beginTurn. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)
- [x] With REPL_TIMEOUT=45 and REPL_LONG_TIMEOUT=600, sessionlog methods return 45 and analyzeRequirements returns 600. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)
- [x] Drain SubmitAsync timeout exception is covered by TEST-MCP-195 AC5-7 and does not change completeTurn/beginTurn expected 30s. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)

### TEST-MCP-REPL-028

ReplWorkspaceResolution.Tests.ps1: a marker-bearing current directory outranks an inherited MCP_WORKSPACE_PATH when the repl bridge resolves the workspace. Validates TR-MCP-REPL-013 / BUG-TRIAGE-077.

**Acceptance Criteria:**
- [x] A marker-bearing current directory outranks an inherited MCP_WORKSPACE_PATH when the repl bridge resolves the workspace. (evidence: tests/McpServer.Repl.Core.Tests; plugins/core test-fixtures; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6; TEST-MCP-195 already reconciled)

### TEST-MCP-REPL-029

Validates TR-MCP-REPL-014. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1 (Example-B override pattern): workflow.sessionlog.setTurnTitle updates current-turn.yaml queryTitle and invokes the server turn title-update path; workflow.sessionlog.setSessionTitle writes session-state.yaml title and invokes the server session title-update path; both fail closed (non-zero) when no session/turn cache is present. Red before the handlers, green after. Scope: layer-1+.


### TEST-MCP-REPL-030

Validates TR-MCP-REPL-015. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1 (Example-B override pattern capturing Invoke-ReplPersistTurn args): an incidental re-submit (appendActions/appendDialog/completeTurn/supersede) with no explicit queryTitle param omits the turn title (and session title) from the persisted payload so a server-preserved title is not clobbered; an explicit queryTitle param still sends and updates the turn title; beginTurn seeds the turn title and seeds the session title only when session-state has none. Red before the omit change, green after. Scope: layer-1+.


### TEST-MCP-REPL-031

Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1, Describe "TEST-MCP-REPL-031 failsafe queue drain". Fixture: a throwaway queue supplied through MCPSERVER_FAILSAFE_DIR plus MCP_CACHE_DIR_OVERRIDE, seeded with three synthetic client.SessionLog.SubmitAsync records stamped 20260714T154733Z, 20260716T112949Z, 20260720T230559Z; Invoke-ReplRaw is overridden to record the replay order and report success, so no backend and no real queue is touched. Asserts Invoke-ReplFailsafeDrain returns scanned 3, replayed 3, failed 0, quarantined 0, aborted false; that the three replays arrive in ascending timestamp order carrying the original requestIds; and that the queue directory is empty afterwards. Red before Invoke-ReplFailsafeDrain existed (CommandNotFoundException), green after. Scope: layer-1+.


### TEST-MCP-REPL-032

Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: three synthetic records in a MCPSERVER_FAILSAFE_DIR sandbox; the overridden Invoke-ReplRaw returns a server-level "type: error / code: validation_failed" result for the middle record and success for the other two. Asserts all three records are attempted (the rejected one does not dam the queue), the summary reports replayed 2 and failed 1 and aborted false, the rejected record is still on disk while the other two are gone, and the retained record now carries drainAttempts 1. Red before the drain existed, green after. Scope: layer-1+.


### TEST-MCP-REPL-033

Validates TR-MCP-REPL-017. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: a MCPSERVER_FAILSAFE_DIR sandbox holding one deliberately unparseable YAML file plus one well-formed record; Invoke-ReplRaw is overridden to count submissions and report success. Asserts exactly one submission happens (the malformed record is never replayed), the summary reports quarantined 1 and replayed 1, the malformed file is gone from the queue root, the quarantine directory holds one .yaml plus one non-empty .reason.txt sidecar, and the queue root is empty. Red before the quarantine path existed, green after. Scope: layer-1+.


### TEST-MCP-REPL-034

Validates TR-MCP-REPL-017. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: a single synthetic record seeded with drainAttempts 5 in a MCPSERVER_FAILSAFE_DIR sandbox; Invoke-ReplRaw is overridden to count submissions. Asserts Invoke-ReplFailsafeDrain -MaxAttempts 5 makes zero submissions, reports quarantined 1 and replayed 0, removes the record from the queue root, and leaves exactly one record in the quarantine directory: an attempt-exhausted record is set aside, never retried forever and never deleted. Red before the attempt budget existed, green after. Scope: layer-1+.


### TEST-MCP-REPL-035

Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: two synthetic records in a MCPSERVER_FAILSAFE_DIR sandbox; the overridden Invoke-ReplRaw returns Success false with the transport error "MCP_UNTRUSTED: marker refresh failed before REPL request". Asserts the drain stops after the first attempt rather than hammering the whole queue, reports aborted true with a non-empty abortReason and replayed 0 and quarantined 0, leaves both records on disk, and does not add a drainAttempts counter to the first record, so an outage cannot burn the attempt budget of good turns. Red before the drain existed, green after. Scope: layer-1+.


### TEST-MCP-REPL-036

Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: a record written through the production Write-ReplFailsafe into a MCPSERVER_FAILSAFE_DIR sandbox, so it is registered as in flight; Invoke-ReplRaw is overridden to count submissions. Asserts the drain makes zero submissions, reports skipped 1 and replayed 0, and leaves the in-flight record on disk so the owning submit still controls it; then asserts Clear-ReplFailsafe removes it, proving the in-flight registration is released on completion. Red before in-flight tracking existed, green after. Scope: layer-1+.


### TEST-MCP-REPL-037

Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: Invoke-ReplFailsafeDrain overridden with a call counter. Asserts three consecutive Invoke-ReplFailsafeDrainOnFirstSuccess calls produce exactly one drain (the once-per-process latch holds even after the first pass), and asserts by source inspection that the body of Invoke-ReplRaw in plugins/core/lib-ps/repl-invoke.ps1 calls Invoke-ReplFailsafeDrainOnFirstSuccess, which is the wiring point that proves backend reachability before any replay. Red before the hook existed (CommandNotFoundException), green after. Scope: layer-1+.


### TEST-MCP-REPL-038

Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: Invoke-ReplFailsafeDrain and Invoke-ReplRaw both overridden with counters. Asserts Invoke-ReplMethod -Method workflow.failsafe.drain -ParamsYaml "maxRecords: 7" dispatches locally: the drain runs exactly once, receives MaxRecords 7 from the params, Invoke-ReplRaw is never called (no server round trip for a plugin-local verb), and the returned text contains the YAML drain summary an operator reads. Red before the verb existed (drain call count 0), green after. Scope: layer-1+.


### TEST-MCP-REPL-039

Validates TR-MCP-REPL-017. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: a sandbox failsafe directory holding two queued records plus one record already in the quarantine subdirectory; plugins/core/lib-ps/mcp-status.ps1 is run as a child pwsh process with MCPSERVER_FAILSAFE_DIR and MCP_CACHE_DIR_OVERRIDE pointed at the sandbox. Asserts the emitted JSON reports failsafeDir equal to the sandbox queue, failsafeCount 2, failsafeQuarantineCount 1, and pendingCount 2, so queue depth is truthful and quarantined records are excluded. Red before the status change (failsafeDir null), green after. Scope: layer-1+.


### TEST-MCP-REPL-040

Validates FR-MCP-143 with TR-MCP-REPL-018, TR-MCP-REPL-019, TR-MCP-REPL-020, TR-MCP-CLIENT-001, and TR-MCP-HEALTH-003. Pester, plugin core: a turn upsert invoked with an empty title binds and persists, covering appendDialog without a queryTitle, appendActions, completeTurn, and the supersede path; before the fix each case fails at parameter binding with a mandatory-parameter error, which the red run quotes. Pester: a supersede where the server holds a refined title and the local cache holds only the hook's raw first line preserves the refined title, and a supersede with no local title still persists. Pester: failTurn closes a turn that exists only in the plugin cache with an empty in-process REPL state, where it previously returned "No active session exists". Unit, client: serializing each session-log request body through the real McpClientJsonContext resolver succeeds, and the audit-driven cases cover every request body found in the typed client rather than only the three known sites; before the fix the anonymous bodies throw NotSupportedException from GetTypeInfo. Unit, server: with an unreachable store the health path no longer reports plain Healthy while persistence is dead, the trust nonce still echoes exactly, and a backend-unavailable condition surfaces as one typed error across REST and the MCP tools instead of four different shapes.
