# Technical Requirements (MCP Server)

## TR-01

**TR-01** — Legacy imported identifier retained for historical traceability. Status: reserved/superseded by MCP-specific technical requirements; no active implementation work is tracked under this stub.
**Status:** deferred
Scope: layer-1+

## TR-02

**TR-02** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub; deferred in audit notes; plugin updateTr rejects non-canonical id.
**Status:** pending
Scope: layer-1+

## TR-03

**TR-03** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-04

**TR-04** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-05

**TR-05** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-06

**TR-06** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-07

**TR-07** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-08

**TR-08** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-09

**TR-09** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-10

**TR-10** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-11

**TR-11** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-12

**TR-12** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-13

**TR-13** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-14

**TR-14** — Placeholder requirement backfilled by DB-FK-001. PLAN-TODOALIGN-001: numeric stub noted; plugin updateTr rejects non-canonical id so status stays pending.
**Status:** pending
Scope: layer-1+

## TR-API-001

**TR-API-001** — Placeholder requirement backfilled for TODO link TR-API-001.
**Status:** pending
Scope: layer-1+

## TR-API-002

**TR-API-002** — Placeholder requirement backfilled for TODO link TR-API-002.
**Status:** pending
Scope: layer-1+

## TR-GRAPHRAG-ADHOC-001

**Ad-hoc text ingestion pipeline** — GraphRagService.IngestTextAsync shall accept raw text, chunk via Chunker (512 tokens), generate embeddings via IEmbeddingService (all-MiniLM-L6-v2, 384-dim), persist ContextDocumentEntity and ContextChunkEntity rows with workspace scoping, register vectors in IVectorIndexService, and optionally trigger IndexAsync. Document ID format: "adhoc-{Guid:N}". Content hash: SHA256.
**Covered by:** FR: FR-MCP-078
**Status:** pending
Scope: layer-1+

## TR-GRAPHRAG-ADHOC-002

**Explicit graph entity and relationship storage** — New EF Core entities GraphEntityEntity (Id, WorkspaceId, Name, EntityType, Description, Metadata JSON, timestamps) and GraphRelationshipEntity (Id, WorkspaceId, SourceEntityId FK, TargetEntityId FK, RelationshipType, Description, Weight default 1.0, Metadata JSON, timestamps) with workspace query filters, cascade delete from entity to relationships, and indexes on Name, EntityType, SourceEntityId, TargetEntityId, RelationshipType.
**Covered by:** FR: FR-MCP-079
**Status:** pending
Scope: layer-1+

## TR-GRAPHRAG-ADHOC-003

**Document lifecycle with cascade delete and vector cleanup** — DeleteDocumentAsync shall query chunk IDs for the document, call IVectorIndexService.RemoveVector for each chunk, then delete the ContextDocumentEntity (EF cascade removes chunks). RemoveVector removes the chunk from internal HNSW dictionaries making the node unreachable; full rebuild reclaims space. ListDocumentsAsync shall return paginated results with ChunkCount and TotalTokens computed via subquery.
**Covered by:** FR: FR-MCP-080
**Status:** pending
Scope: layer-1+

## TR-HANDOFF-AGENT-001

**Versioned HandoffTodoDraft one-shot extraction** — Use AgentPoolOneShotContext.HandoffTodoDraft and a versioned strict-JSON extraction prompt.
**Covered by:** FR: FR-HANDOFF-002; TEST: TEST-HANDOFF-003
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Extraction is invoked through AgentPoolOneShotContext.HandoffTodoDraft. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Only strict JSON matching the versioned contract is accepted; malformed output yields diagnostics and no TODO. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-HANDOFF-AUDIT-001

**Normalized ingestion-run and diagnostic persistence** — Persist normalized ingestion-run and diagnostic records separately from TODO records, without raw credentials or source content.
**Covered by:** FR: FR-HANDOFF-006; TEST: TEST-HANDOFF-007
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Every run stores normalized provenance and diagnostics independently of TODO rows. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Persisted records omit raw credentials and raw source content. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-HANDOFF-CONTRACT-001

**Documented handoff contracts** — Document request, result, draft, provenance, and diagnostic contracts for handoff ingestion.
**Covered by:** FR: FR-HANDOFF-001, FR-HANDOFF-002; TEST: TEST-HANDOFF-001, TEST-HANDOFF-002, TEST-HANDOFF-003
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] HandoffSourceKind, HandoffIngestionMode, HandoffIngestionRequest, HandoffIngestionResult, HandoffTodoDraft, HandoffProvenance, HandoffDiagnostic, and HandoffApprovalRequest exist with complete XMLDocs. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Contracts serialize and deserialize with the shared client JSON context without silent field loss. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-HANDOFF-MODES-001

**Confidence gating, review approval, and idempotent replay** — Implement DraftOnly, RequireReview, and CreateWhenConfident with confidence gating, approval revalidation, and deterministic replay unless force=true.
**Covered by:** FR: FR-HANDOFF-004, FR-HANDOFF-005; TEST: TEST-HANDOFF-004, TEST-HANDOFF-005
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] CreateWhenConfident creates only when confidence is at least 0.75 and no error diagnostic exists. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Approval revalidates the stored draft before TODO creation. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Replays of the same workspace, content hash, and prompt version are deterministic unless force=true. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-HANDOFF-SECURITY-001

**Bounded workspace-contained handoff readers** — Use bounded readers, workspace containment, and reparse-point protection for handoff source resolution.
**Covered by:** FR: FR-HANDOFF-001; TEST: TEST-HANDOFF-001, TEST-HANDOFF-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Readers refuse more than 8 MiB of decoded input and unsupported formats. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Path and artifact locators stay inside the workspace and fail closed on reparse-point escapes. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-HANDOFF-SURFACE-001

**Shared API, client, REPL, Director, MCP, and plugin delegation** — Every public surface delegates to the same IHandoffIngestionService and exposes ingest, inspect, and approval workflows.
**Covered by:** FR: FR-HANDOFF-007; TEST: TEST-HANDOFF-006
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] API, client, REPL, Director, MCP tools, and plugin skill all call IHandoffIngestionService. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Ingest, get-run, and approve operations exist on every public surface and apply workspace isolation. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-HANDOFF-TODO-001

**Exclusive TODO-service persistence** — Persist approved TODOs exclusively through the existing TODO service. AI output never writes TODO storage directly.
**Covered by:** FR: FR-HANDOFF-005; TEST: TEST-HANDOFF-005
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] The only TODO mutation path is ITodoService.CreateAsync. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Existing TODO IDs require review and are never silently renamed. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-HANDOFF-VALIDATE-001

**Pure draft validation and normalization** — Provide a pure draft validator and normalizer that emits field-specific diagnostics.
**Covered by:** FR: FR-HANDOFF-003, FR-HANDOFF-004; TEST: TEST-HANDOFF-004
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Validation and normalization have no TODO or run-store side effects. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Invalid ID, title, section, priority, estimate, description, technical details, implementation tasks, dependencies, or requirement links produce field-specific diagnostics. (evidence: tests/QBrainAi.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-LOC-001

**Localization Infrastructure** — Multi-language support for the MCP server. *(Planned - implementation scope TBD.)*
**Covered by:** FR: FR-LOC-001
**Status:** pending
Scope: layer-1+

## TR-MCP-ACKRECOVERY-001

**Deploy compatible guarded session runtime** — Deploy pinned b53ce6b8 server and paired REPL through Nuke. Preserve configuration, data, permissions and rollback evidence. Existing serializable service transaction supplies atomic terminal-history protection; existing DTOs preserve modern persistence/title receipts.
**Covered by:** FR: FR-MCP-ACKRECOVERY-001; TEST: TEST-MCP-ACKRECOVERY-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Compatible deployed server and typed REPL return valid persisted and retitled receipts through unchanged plugin. (evidence: MCP lifecycle and terminal history recovery accepted by final HV AGREE, 10 PASS/0 FAIL/0 UNKNOWN, accuracy 99 and completeness 98. Request /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.request.jsonl; response /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.response.jsonl; full verdict /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.verdict.md. Completed MCP review Codex-20261006T210855Z-hv-resolution-final / req-20261006T210855Z-hv-resolution-final, exact body verified. Live receipt /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/live/result.json; history and queue /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/reconciliation-result.json. Nuke short startup health window remains separately triaged as triage-group-521951095c164f2e; Nuke UpdateService gate is not claimed successful. The exact Nuke-deployed candidate was recovered by normal service start and verified healthy.)
- [x] Atomic server protection preserves terminal response/status/title/data under stale supersession, including concurrent completion. (evidence: MCP lifecycle and terminal history recovery accepted by final HV AGREE, 10 PASS/0 FAIL/0 UNKNOWN, accuracy 99 and completeness 98. Request /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.request.jsonl; response /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.response.jsonl; full verdict /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.verdict.md. Completed MCP review Codex-20261006T210855Z-hv-resolution-final / req-20261006T210855Z-hv-resolution-final, exact body verified. Live receipt /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/live/result.json; history and queue /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/reconciliation-result.json. Nuke short startup health window remains separately triaged as triage-group-521951095c164f2e; Nuke UpdateService gate is not claimed successful. The exact Nuke-deployed candidate was recovered by normal service start and verified healthy.)
- [x] Normal live lifecycle opens, updates, retitles, completes and starts next turn without new failsafes. (evidence: MCP lifecycle and terminal history recovery accepted by final HV AGREE, 10 PASS/0 FAIL/0 UNKNOWN, accuracy 99 and completeness 98. Request /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.request.jsonl; response /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.response.jsonl; full verdict /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.verdict.md. Completed MCP review Codex-20261006T210855Z-hv-resolution-final / req-20261006T210855Z-hv-resolution-final, exact body verified. Live receipt /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/live/result.json; history and queue /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/reconciliation-result.json. Nuke short startup health window remains separately triaged as triage-group-521951095c164f2e; Nuke UpdateService gate is not claimed successful. The exact Nuke-deployed candidate was recovered by normal service start and verified healthy.)
- [x] Retained failsafes are drained and previously verified terminal history is restored; restart trust and private marker readability verified. (evidence: MCP lifecycle and terminal history recovery accepted by final HV AGREE, 10 PASS/0 FAIL/0 UNKNOWN, accuracy 99 and completeness 98. Request /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.request.jsonl; response /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.response.jsonl; full verdict /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.verdict.md. Completed MCP review Codex-20261006T210855Z-hv-resolution-final / req-20261006T210855Z-hv-resolution-final, exact body verified. Live receipt /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/live/result.json; history and queue /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/reconciliation-result.json. Nuke short startup health window remains separately triaged as triage-group-521951095c164f2e; Nuke UpdateService gate is not claimed successful. The exact Nuke-deployed candidate was recovered by normal service start and verified healthy.)

## TR-MCP-AGENT-001

**Agent EF Core Entities** — `AgentDefinitionEntity` (agent type definitions with defaults), `AgentWorkspaceEntity` (per-workspace agent configurations with overrides, banning, isolation strategy), and `AgentEventLogEntity` (lifecycle event audit log). All stored in primary instance SQLite via `McpDbContext`. Unique index on `(AgentDefinitionId, WorkspacePath)` for workspace configs. JSON serialization for list fields (`DefaultModelsJson`, `ModelsOverrideJson`, `InstructionFilesOverrideJson`).
**Covered by:** `AgentDefinitionEntity`, `AgentWorkspaceEntity`, `AgentEventLogEntity`, `McpDbContext`
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-002

**Built-in Agent Type Defaults** — `AgentDefaults.GetBuiltInDefaults()` returns seed data for 7 built-in agent types: copilot, cline, cursor, windsurf, claude-code, aider, continue. Each includes default launch command, instruction file path, models, branch strategy, and seed prompt. `AgentService.SeedBuiltInDefaultsAsync` is idempotent - only inserts agents not already present. Built-in definitions cannot be deleted.
**Covered by:** `AgentDefaults`, `AgentService`
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-003

**Agent REST API** — `AgentController` at `/qbrainai/agents` with endpoints for: definition CRUD (`/definitions`), workspace agent CRUD (root), ban/unban (`/{agentId}/ban`, `/{agentId}/unban`), lifecycle events (`/{agentId}/events`), and YAML validation (`/validate`). Mutation endpoints require `[Authorize(Policy = "AgentManager")]` (JWT). Read endpoints use standard workspace API key auth.
**Covered by:** `AgentController`, `IAgentService`, `AgentService`
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-004

**Agent Pool Configuration Contract** — Agent pool settings SHALL bind from configuration into a validated options model that includes `AgentName`, `AgentPath`, `AgentModel`, `AgentSeed`, `AgentParameters`, `IsInteractiveDefault`, `IsTodoPlanDefault`, `IsTodoStatusDefault`, and `IsTodoImplementDefault`.
Validation SHALL enforce unique `AgentName` values (case-insensitive), required launch path, and unambiguous default-agent assignment for each intent-default flag.


**Covered by:** `AgentPoolOptions` *(planned)*, `AgentPoolDefinitionOptions` *(planned)*, `Program.cs` *(planned extension)*
**Status:** 🔴 planned
Scope: layer-1+

## TR-MCP-AGENT-005

**Pooled Runtime and Queue Dispatcher** — All agent execution SHALL flow through a singleton pool runtime service that maintains lifecycle state per configured pooled agent and dispatches queued one-shot jobs to eligible idle agents.
Pool runtime SHALL support start/stop/recycle operations, busy/idle transitions, one-shot queue states (`queued`, `processing`, `completed`, `failed`, `canceled`), and concurrent interactive attachment to agents currently processing one-shot requests.

No alternate direct-launch path is permitted for pooled workloads; pooled agents launch through the voice interactive session mechanism.


**Covered by:** `IAgentPoolService` *(planned)*, `AgentPoolService` *(planned)*, `AgentPoolQueueService` *(planned)*
**Status:** 🔴 planned
Scope: layer-1+

## TR-MCP-AGENT-006

**Hosted .NET 9 Microsoft Agent Framework Library** — The solution SHALL provide a dedicated .NET 9 class library for hosting an MCP-aware agent inside external .NET applications built on Microsoft Agent Framework. The library SHALL expose DI-friendly registration and configuration APIs for QBrain.AI connectivity, agent construction, and host lifecycle integration so host applications do not need to assemble low-level MCP session-log or TODO plumbing themselves.

**Covered by:** `ServiceCollectionExtensions`, `McpAgentOptions`, `McpAgentOptionsValidator`, `IMcpHostedAgent`, `IMcpHostedAgentFactory`, `McpHostedAgent`, `McpHostedAgentFactory`, `McpHostedAgentRegistration`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-AGENT-007

**Built-In MCP Session Log, TODO, Repository, Desktop-Launch, and PowerShell Workflow for Hosted Agents** — The hosted agent library SHALL implement built-in workflow operations for session bootstrap, turn creation/update, TODO retrieval/update, TODO plan/status/implementation flows, repository read/list/write operations, local desktop process launch using the existing QBrain.AI contracts, and persistent in-process PowerShell sessions hosted directly inside the current .NET agent process. The workflow SHALL preserve canonical ID conventions for session IDs, request IDs, and TODO IDs, SHALL keep repository access scoped to repo-relative paths, SHALL expose desktop launch through the authenticated workspace context only when the server-side desktop-launch feature gate, executable allowlist, and privileged desktop-launch token requirements are satisfied, SHALL keep PowerShell session state local to the hosted agent instance, SHALL expose the same local PowerShell session manager to host applications through `IMcpHostedAgent.PowerShellSessions`, and SHALL prefer reuse of existing client abstractions where server contracts already exist instead of duplicating transport logic.

**Covered by:** `ISessionLogWorkflow`, `SessionLogWorkflow`, `SessionLogWorkflowContext`, `SessionLogTurnContext`, `ITodoWorkflow`, `TodoWorkflow`, `IMcpHostedAgent.PowerShellSessions`, `IHostedPowerShellSessionManager`, `McpHostedAgentToolAdapter`, `HostedPowerShellSessionManager`, `HostedPowerShellSessionHost`, `PowerShellSessionCreateResult`, `PowerShellSessionCommandResult`, `PowerShellSessionCloseResult`, `QBrainAiClient`, `RepoClient`, `DesktopClient`, `IMcpSessionIdentifierFactory`, `McpSessionIdentifierFactory`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-AGENT-008

**Agent Pool Orchestration** — Reserved/planned: orchestrates a pool of agents for parallel task processing. Not yet implemented; placeholder for FR-MCP-028 / FR-MCP-050 traceability.
**Covered by:** FR: FR-MCP-028, FR-MCP-050
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-009

**Agent Plugin Discovery** — Reserved/planned: discovers installed agent plugins and validates their contracts. Not yet implemented; placeholder for FR-MCP-050 traceability.
**Covered by:** FR: FR-MCP-050
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-010

**Agent Process Lifecycle** — Reserved/planned: manages start/stop/health of agent host processes. Not yet implemented; placeholder for FR-MCP-050 traceability.
**Covered by:** FR: FR-MCP-050
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-011

**Agent State Synchronization** — Reserved/planned: synchronizes agent state across pool members. Not yet implemented; placeholder for FR-MCP-050 traceability.
**Covered by:** FR: FR-MCP-050
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-012

**Agent Notification Bus** — Reserved/planned: routes notifications between agents and the workspace event bus. Not yet implemented; placeholder for FR-MCP-050 traceability.
**Covered by:** FR: FR-MCP-050
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-013

**PowerShell McpSession Dual-Path Session Cache Resolution** — `tools/powershell/McpSession.psm1` SHALL persist the canonical current session object to `.mcpSession/current-session.json` whenever session state is saved, SHALL consult that current-session cache before falling back to the legacy `.mcpServer/session.yaml` wrapper when resolving the active session, and SHALL reuse the cached current-session `sessionId` during initialization when the cache matches the requested agent/model and the session is still active.
When a session is completed, the module SHALL remove both the legacy wrapper cache and the `.mcpSession` current-session cache so a later bootstrap does not accidentally reuse a completed session. The implementation SHALL continue supporting the legacy wrapper file for backward compatibility.


**Covered by:** `tools/powershell/McpSession.psm1`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-AGENT-014

**PowerShell McpSession Trust Bootstrap Parity** — `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, and `tools/powershell/McpContext.psm1` SHALL use the same marker-signature verification, `/health` nonce echo verification, and `MCP_UNTRUSTED` fallback semantics before any follow-on MCP calls are allowed. The trust flow SHALL be explicit enough that session bootstrap, TODO bootstrap, and context bootstrap behave identically when trust succeeds or fails, and the failure path SHALL stop additional endpoint probing.

**Covered by:** `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, `tools/powershell/McpContext.psm1`, `docs/context/module-bootstrap.md`, `docs/USER-GUIDE.md`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-AGENT-015

**ACID hosted-agent profile and sealed run contract** — The QBrainAi.McpAgent package SHALL define an ACID tightly coupled profile that applies strict McpAgentOptions defaults, filters the model-visible tool surface to approved read/audit tools, seals ChatClientAgent run options with serialized function invocation, and documents the profile as fail-closed for unproven mutation paths.
**Covered by:** FR: FR-MCP-136; TEST: TEST-MCP-186
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] McpAgentOptions exposes a UseAcidTightlyCoupledProfile helper that sets strict profile defaults without changing default registration behavior.
- [x] ACID run options preserve AllowMultipleToolCalls=false and FunctionInvokingChatClient.AllowConcurrentInvocation=false.
- [x] ACID tool exposure is generated from an allowlist and excludes unsafe generic, shell, desktop, and mutation tools by default.
- [x] All new public APIs have XMLDocs and are covered by focused tests.

## TR-MCP-AGENT-016

**Hosted-agent Quad Brain coding adapter** — RETIRED 2026-07-20 by FR-MCP-142 and TR-MCP-QB-001. This requirement specified the mcp_quadbrain_coding_execute hosted-agent tool routing to POST qbrainai/brain-slots/orchestrate through McpQuadBrainCodingAgentRouter. Both the tool and the router are removed: the tool was registered into the shared QBrainAi.McpAgent catalog for every host, which violates the rule that no QuadBrain capability is exposed outside QBAgent, and it was already non-functional for QBAgent because QuadBrainInternalToolExecutor never had a case for it, so the interceptor classified it internal and returned Fail. Removed artifacts: McpHostedAgentToolAdapter tool registration and ExecuteQuadBrainCodingTaskAsync, IMcpHostedAgent.ExecuteQuadBrainCodingTaskAsync, McpHostedAgent.ExecuteQuadBrainCodingTaskAsync, QBAgentRuntime.ExecuteCodingTaskAsync and its codingTaskExecutor parameter, QBAgentDefinition allow-list entry, McpQuadBrainCodingAgentRouter.cs, and the McpQuadBrainCodingAgentRequest DTO. Server-side QuadBrain orchestration is unchanged and remains reachable only through POST /v1/chat/completions. Note that this is a public API break in the shared QBrainAi.McpAgent assembly, accepted deliberately as the intended consequence of the ruling.
**Covered by:** FR: FR-MCP-137; TEST: TEST-MCP-187
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Coding-agent DTOs are public, XML-documented, and use System.Text.Json property names compatible with Microsoft Agent Framework function invocation.
- [x] The adapter preserves caller metadata and appends coding-agent fields including taskKind, executionProfile, and sourceType.
- [x] The adapter fails through the typed QuadBrainOrchestrationResponse status/reason contract returned by QBrain.AI and does not synthesize implicit fallback model output.
- [x] Existing non-ACID hosted-agent registration remains backward compatible aside from the additional Quad Brain coding tool.
- [x] All new public APIs have XMLDocs and are covered by focused tests.

## TR-MCP-AGENT-PARITY-010

**TR-MCP-AGENT-PARITY-010** — Legacy agent-parity TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.
**Covered by:** FR: FR-MCP-AGENT-PARITY-001
**Status:** deferred
Scope: layer-1+

## TR-MCP-AGENT-PARITY-011

**TR-MCP-AGENT-PARITY-011** — Legacy agent-parity TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.
**Covered by:** FR: FR-MCP-AGENT-PARITY-002
**Status:** deferred
Scope: layer-1+

## TR-MCP-AGENT-PARITY-012

**TR-MCP-AGENT-PARITY-012** — Legacy agent-parity TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.
**Status:** deferred
Scope: layer-1+

## TR-MCP-AGENT-PARITY-013

**TR-MCP-AGENT-PARITY-013** — Legacy agent-parity TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.
**Status:** deferred
Scope: layer-1+

## TR-MCP-AGENT-PARITY-020

**TR-MCP-AGENT-PARITY-020** — Legacy agent-parity TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.
**Status:** deferred
Scope: layer-1+

## TR-MCP-AGENT-PARITY-020..027

**TR-MCP-AGENT-PARITY-020..027** — Legacy agent-parity TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.
**Status:** pending
Scope: layer-1+

## TR-MCP-AGENT-PARITY-020-027

**TR-MCP-AGENT-PARITY-020-027** — Legacy agent-parity TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.
**Status:** deferred
Scope: layer-1+

## TR-MCP-AGENT-PARITY-030

**TR-MCP-AGENT-PARITY-030** — Legacy agent-parity TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.
**Status:** deferred
Scope: layer-1+

## TR-MCP-AIUNIT-001

**Implement CreateAiUnitClient and library-triggered Send in Nuke build for reviews** — In build/Build.cs add public CreateAiUnitClient(string reviewType) that:
- Builds IConfigurationRoot loading appsettings.aiunit.json (root preferred, fallback to tests/QBrainAi.PlanReview.Tests/appsettings.aiunit.json), env.
- Resolves ActiveStrategy.
- Instantiates and returns a client (ResilientFrontierClient or adapter implementing SendAsync(FrontierRequest)->FrontierResponse) that actually delegates to the aiUnit strategy executor (cli etc).

AiCodeReview / AiProjectReview targets (already sketched) call it and use the response to populate runlog + MD via WriteAiUnitReviewMarkdownFromData.

Update Build.Ai*.cs if needed for correct using/ctor of FrontierRequest (use named or positional consistent with lib).

Add using Microsoft.Extensions.Configuration*; ensure _build.csproj and Directory.Packages.props have the Json binder.
**Covered by:** FR: FR-MCP-138; TEST: TEST-MCP-AIUNIT-001
**Status:** pending
Scope: layer-1+

## TR-MCP-API-001

REST routes for todo/session/context/repo/github with OpenAPI.
**Covered by:** FR: FR-MCP-002, FR-MCP-005, FR-MCP-065, FR-SUPPORT-010
**Status:** pending
Scope: layer-1+

## TR-MCP-API-002

**One-Shot Submission Contract and Intent Routing** — One-shot APIs SHALL support explicit context values `Plan`, `Status`, `Implement`, and `AdHoc`.
When `AgentName` is omitted, the runtime SHALL resolve request intent from context/prompt and select the configured default agent for that intent.

Template-mode and ad-hoc-mode payload validation SHALL enforce:

- `promptTemplateId` and ad-hoc prompt text cannot both be supplied in explicit mode.
- At least one prompt source must be resolvable.
- `id` is required for template-resolved requests and optional for ad-hoc requests.


**Covered by:** `AgentPoolController` *(planned)*, `AgentPoolIntentResolver` *(planned)*, request DTO validators *(planned)*
**Status:** 🔴 planned
Scope: layer-1+

## TR-MCP-API-003

**Agent Pool Monitoring and Control APIs** — REST endpoints SHALL provide:
- Pooled agent availability snapshots.
- Runtime controls (connect, start, stop, immediate recycle).
- Queue operations (list, enqueue, cancel/remove, queued-item move up/down).
- Separate SSE notification stream emitting queue/agent lifecycle transitions with payload fields `AgentName`, `LastRequestPrompt`, and `SessionId`.
- Read-only response stream attachment supporting multiple concurrent subscribers.


**Covered by:** `AgentPoolController` *(planned)*, `AgentPoolNotificationService` *(planned)*, `AgentPoolStreamService` *(planned)*
**Status:** 🔴 planned
Scope: layer-1+

## TR-MCP-ARCH-001

ASP.NET Core 9 server with HTTP and STDIO MCP transport.
**Covered by:** FR: FR-MCP-007, FR-SUPPORT-010
**Status:** pending
Scope: layer-1+

## TR-MCP-ARCH-002

**DI Single Source of Truth and Pull-Based Change Notification** — Architecture audit and remediation across `QBrainAi.Support.Mcp` SHALL enforce:
- Stateful services, registries, managers, and providers must be DI-owned (`singleton` or `scoped`) and must not be instantiated via `new` or `ActivatorUtilities.CreateInstance` outside composition-root registration paths.
- Authoritative mutable state must have a single owner in DI; peer services must pull current state from that owner instead of receiving pushed state payloads.
- Observable state contracts must expose change signaling via `INotifyPropertyChanged` for data-availability/change notification, without embedding mutable payload transfer in event arguments.
- Race-condition remediation must prioritize ownership/lifetime design in DI (single owner + pull model); fire-and-forget propagation and ad-hoc synchronization used as state-sharing mechanisms are prohibited.
- Automated validation must cover DI registration lifetimes and notification semantics for remediated services.

**Covered by:** FR: FR-MCP-059
**Status:** 🔴 planned
Scope: layer-1+

## TR-MCP-AUDIT-001

**Audited Copilot Client** — `AuditedCopilotClient` decorates `ICopilotClient`. Before each Copilot invocation: determines affected workspaces, creates `in_progress` session log entries per workspace. After invocation: logs `completed` entries with result and actions taken. Action type: `copilot_invocation`. Registered as DI decorator so all server-initiated Copilot calls are audited.

**Covered by:** `AuditedCopilotClient`, `Program.cs` (`ICopilotClient` decorator wiring), `McpStdioHost` (`ICopilotClient` decorator wiring), `CopilotServiceCollectionExtensions`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-AUDIT-002

**Versioned compressed generic audit payloads** — Add nullable versioned GZip UTF-8 columns for the four generic audit JSON payloads on SQLite, SQL Server, and PostgreSQL. Preserve existing text columns and read legacy rows without a historical rewrite.
**Covered by:** FR: FR-MCP-AUDIT-001; TEST: TEST-MCP-AUDIT-001, TEST-MCP-AUDIT-002, TEST-MCP-AUDIT-003
**Status:** pending
Scope: layer-1+

## TR-MCP-AUDIT-003

**Restricted SQL Server audit runtime role** — The SQL Server migration creates a runtime role with ordinary application DML but denies update and delete on both audit ledgers and writes to EF migration history. With AutoMigrate false, HTTP and stdio startup reject pending migrations before serving runtime work.
**Covered by:** FR: FR-MCP-AUDIT-001; TEST: TEST-MCP-AUDIT-001, TEST-MCP-AUDIT-002, TEST-MCP-AUDIT-003
**Status:** pending
Scope: layer-1+

## TR-MCP-AUDIT-004

**Local-only audit federation and export policy** — TODO federation snapshots may use local audit history for version tokens but must omit audit rows. Wiki dump policy marks both audit tables local-only and export payloads omit their rows.
**Covered by:** FR: FR-MCP-AUDIT-001; TEST: TEST-MCP-AUDIT-001, TEST-MCP-AUDIT-002, TEST-MCP-AUDIT-003
**Status:** pending
Scope: layer-1+

## TR-MCP-AUDITACTION-001

**Validate tool execution actions at session write boundaries** — Recognize tool_execution case-insensitively without rewriting caller data. Require nonnegative order, nonblank description and nonblank status for this action type. Preserve status vocabulary for host lifecycle compatibility. Validate actions selected for persistence before whole-session submit, turn upsert, turn replacement or actions-section replacement changes tracked session/turn state. Preserve last-wins duplicate request IDs and retained terminal history: losing duplicate payloads and skipped late snapshots remain ignored. Unrelated section replacements ignore action payloads. Preserve zero as a valid explicit tool_execution order, regardless of collection position. Keep unrelated legacy/custom action validation unchanged. Do not synthesize designDecisions or decision dialog from actions; document the recognized type in docs/context/action-types.md.
**Covered by:** FR: FR-MCP-AUDITACTION-001; TEST: TEST-MCP-AUDITACTION-001
**Status:** pending
Scope: layer-1+

## TR-MCP-AUTH-001

**OIDC JWT Bearer Authentication** — ASP.NET Core JWT Bearer middleware configured with OIDC authority/issuer, audience (`mcp-server-api`), and optional client secret based on provider requirements. `OidcAuthOptions` bound from `Mcp:Auth` configuration section. Management endpoints (agent mutations) require `[Authorize(Policy = "AgentManager")]`; read endpoints fall back to existing API key auth. `RequireHttpsMetadata` configurable for local development.
**Covered by:** `OidcAuthOptions`, `Program.cs`, `AgentController`
**Status:** pending
Scope: layer-1+

## TR-MCP-AUTH-002

**GitHub Federation via OIDC Provider** — OIDC provider setup may configure GitHub as a social Identity Provider with `user:email read:org` scopes. First-login flow may auto-create users from GitHub accounts. GitHub username mapped to `github_username` user attribute. Setup scripts accept `--GitHubClientId` / `--GitHubClientSecret` parameters; GitHub federation is optional.
**Covered by:** `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`
**Status:** pending
Scope: layer-1+

## TR-MCP-AUTH-003

**Device Authorization Flow for CLI Clients** — OIDC `mcp-director` client configured as public with OAuth 2.0 Device Authorization Grant enabled. Director CLI initiates device flow, displays user code and verification URI, polls for token completion. Provider claim mapping ensures `mcp-server-api` appears in token audience and includes `realm_roles`.
**Covered by:** `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`, `QBrainAi.Director`
**Status:** pending
Scope: layer-1+

## TR-MCP-AUTH-010

**WorkspaceAuthMiddleware 503/401 gating** — The API-key branch of WorkspaceAuthMiddleware reserves StatusCodes.Status503ServiceUnavailable strictly for the case !WorkspaceTokenService.IsInitialized (no full token seeded yet); that response includes a Retry-After header and JSON body. Once the token subsystem is initialized, an unresolved workspace or non-validating/missing credential yields 401 Unauthorized.
**Covered by:** FR: FR-MCP-132; TEST: TEST-MCP-AUTH-010, TEST-MCP-AUTH-011, TEST-MCP-AUTH-012
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Status 503 Service Unavailable is reserved for !WorkspaceTokenService.IsInitialized with Retry-After header and JSON body.
- [x] Once token subsystem is initialized, unresolved workspace or non-validating credential yields 401 Unauthorized.

## TR-MCP-AUTH-011

**WorkspaceTokenService.IsInitialized** — WorkspaceTokenService exposes bool IsInitialized => !_tokens.IsEmpty (true once at least one full-access token has been generated). Consumed by WorkspaceAuthMiddleware and WorkspaceReadinessHealthCheck to distinguish genuine startup-not-ready from a credential failure.
**Covered by:** FR: FR-MCP-132; TEST: TEST-MCP-AUTH-010, TEST-MCP-AUTH-011, TEST-MCP-AUTH-012
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] WorkspaceTokenService exposes IsInitialized property that returns true when at least one full-access token has been generated.
- [x] IsInitialized is consumed by WorkspaceAuthMiddleware and WorkspaceReadinessHealthCheck to distinguish startup-not-ready from credential failure.

## TR-MCP-BATCH-001

**Robust Bash plugin batch records normalization** — Bash-style MCP server plugin wrappers SHALL normalize requirement batch records from unindented YAML sequences, indented YAML sequences, and inline JSON arrays before schema validation and typed request conversion.
**Covered by:** FR: FR-MCP-113, FR-MCP-BATCH-001, FR-MCP-PLUGIN-BATCH-001; TEST: TEST-MCP-153, TEST-MCP-BATCH-001
**Status:** pending
Scope: layer-1+

## TR-MCP-BATCH-109

**Requirements batch endpoint and workflow support** — REST controllers, RequirementsClient, repository implementations, and REPL workflow dispatch shall expose atomic per-kind and mixed requirements batch create/update operations with all-or-nothing validation and structured batch result errors.
**Covered by:** FR: FR-MCP-109; TEST: TEST-MCP-145, TEST-MCP-146
**Status:** pending
Scope: layer-1+

## TR-MCP-BATCHTS-001

**Robust TypeScript plugin batch records normalization** — TypeScript MCP server plugin tools SHALL normalize requirement batch records from object arrays and string YAML or JSON arrays before bridge request conversion while preserving nested acceptanceCriteria booleans.
**Covered by:** FR: FR-MCP-113; TEST: TEST-MCP-153
**Status:** pending
Scope: layer-1+

## TR-MCP-BYRD-001

**Workspace-Scoped Byrd Execution Store** — The server SHALL persist Byrd iteration phases, execution TODOs, and TODO checkpoints in a workspace-scoped durable store under `.mcpServer`, with stable IDs for phases, TODOs, and checkpoints. The execution store SHALL coexist with the existing TODO providers without breaking legacy TODO CRUD behavior.

**Covered by:** `src/QBrainAi.Services/Models/TodoExecutionModels.cs`, `src/QBrainAi.Services/Services/ITodoExecutionService.cs`, `src/QBrainAi.Services/Services/TodoExecutionService.cs`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-BYRD-002

**Bounded Hydration and Delta Queries** — The server SHALL hydrate a bounded execution context for the active Byrd TODO using requirement snippets, recent session-turn summaries, relevant files, artifacts, validation state, and execution pointers. It SHALL also return checkpoint-based delta context that reports only the new turns, artifacts, commits, and next action since a specified checkpoint.

**Covered by:** `src/QBrainAi.Services/Services/TodoExecutionService.cs`, `src/QBrainAi.Support.Mcp/Controllers/TodoExecutionController.cs`, `src/QBrainAi.Support.Mcp/McpStdio/McpServerMcpTools.cs`, `src/QBrainAi.Client/TodoClient.cs`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-BYRD-003

**Byrd Progression Enforcement** — The execution service SHALL enforce Byrd progression rules so implementation cannot begin before unit tests are defined, validation cannot begin without implementation evidence, blocked TODOs require an explicit resume reason, and completion requires passing validation plus satisfied acceptance criteria. Test-plan updates, checkpoints, validation results, and session-turn linking SHALL update the persisted execution pointers used for resumption.

**Covered by:** `src/QBrainAi.Services/Services/TodoExecutionService.cs`, `src/QBrainAi.Support.Mcp/Controllers/TodoExecutionController.cs`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-BYRD-004

**Structured TODO Execution Surfaces** — The server SHALL expose the Byrd execution workflow through REST endpoints, STDIO MCP tools, and typed client methods, including the safe `adb_step` action surface for Android validation. The exposed contracts SHALL remain structured and bounded for iteration phase creation, plan decomposition, active TODO selection, execution context hydration, checkpoint append, validation result recording, status progression, session-turn linking, and device actions.

**Covered by:** `src/QBrainAi.Support.Mcp/Controllers/TodoExecutionController.cs`, `src/QBrainAi.Support.Mcp/McpStdio/McpServerMcpTools.cs`, `src/QBrainAi.Client/Models/TodoModels.cs`, `src/QBrainAi.Client/TodoClient.cs`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-BYRD-005

**Byrd process plan creation requirements** — The Byrd Development Process V3 document must define plan creation requirements for decision-complete frontier-model handoff plans, including required FR/TR/TEST capture, TDD tests, expected red state, green criteria, validation scope, and acceptance criteria before implementation begins.
**Covered by:** FR: FR-MCP-104; TEST: TEST-MCP-137, TEST-MCP-141
**Status:** pending
Scope: layer-1+

## TR-MCP-CFG-001

IOptions-based configuration for all filesystem and runtime settings.
**Covered by:** FR: FR-MCP-001
**Status:** pending
Scope: layer-1+

## TR-MCP-CFG-002

Port selection from `Mcp:Port` with `PORT` env override.
**Covered by:** FR: FR-MCP-001
**Status:** pending
Scope: layer-1+

## TR-MCP-CFG-003

**Workspace Configuration Schema** — Workspace state is persisted in `appsettings.json` under `Mcp:Workspaces` (not in EF/SQLite). Each entry includes: `WorkspacePath` (required, absolute path, primary key), `Name` (required), `WorkspacePort` (required), `TodoPath` (default: `docs/todo.yaml`), `DataDirectory` (optional override for mcp.db), `TunnelProvider` (optional: `ngrok`/`cloudflare`/`frp`), `RunAs` (optional Windows identity), `IsPrimary` (default: false), `IsEnabled` (default: true), `DateTimeCreated`, `DateTimeModified`. Port uniqueness enforced; auto-assignment from `max(existing) + 1`. File written atomically via `JsonNode` patching with `IConfigurationRoot.Reload()`.
**Status:** pending
Scope: layer-1+

## TR-MCP-CFG-004

**YAML Configuration Support** — `Program.cs` calls `builder.Configuration.AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true)` using `NetEscapades.Configuration.Yaml`. YAML configuration merges with and can override `appsettings.json` values. Intended for local-only overrides not committed to source control.
**Covered by:** `Program.cs`, `NetEscapades.Configuration.Yaml`
**Status:** pending
Scope: layer-1+

## TR-MCP-CFG-005

**System-Wide Default Copilot Model Propagation** — Setting the default Copilot model for all session types requires updates to three locations:
- `CopilotClientOptions.Model` default value (in `QBrainAi.Common.Copilot`) - controls server-initiated CLI invocations via `ICopilotClient`. Configurable at runtime via `Mcp:Copilot:Model`.
- `VoiceConversationOptions.CopilotModel` default value (in `QBrainAi.Support.Mcp/Options/`) - controls voice conversation session model. Configurable via `Mcp:Voice:CopilotModel`.
- `AgentDefaults.GetBuiltInDefaults()` (in `QBrainAi.Support.Mcp/Services/`) - seed data for built-in agent type definitions including the `copilot` agent's `DefaultModelsJson`. Only affects new installations (existing agent definitions are not re-seeded).

All three share the pattern of a compile-time default overridable via `IOptions<T>` configuration binding. No new infrastructure is required - this is a default-value update propagated through existing `IOptions`-based configuration (TR-MCP-CFG-001).


**Covered by:** `CopilotClientOptions`, `VoiceConversationOptions`, `AgentDefaults`
**Status:** 🔴 planned
Scope: layer-1+

## TR-MCP-CFG-006

**Administrative Configuration Snapshot and YAML Patch API** — `ConfigurationController` SHALL expose `GET /qbrainai/configuration` returning the current flattened `IConfiguration` view as `section:key` pairs, and `PATCH /qbrainai/configuration` accepting a flattened dictionary that patches only the submitted keys into `appsettings.yaml`.
Persistence SHALL be delegated to a dedicated helper service that resolves the correct loaded `appsettings` file path, serializes concurrent mutations across the full read-modify-write cycle, writes YAML or JSON via temp-file-plus-atomic-replace semantics, and reloads `IConfigurationRoot` after successful updates. `WorkspaceController` global-prompt updates SHALL reuse the same helper so shared configuration writes obey the same durability and reload guarantees. The endpoints SHALL use standard JWT Bearer admin authorization and remain closed when OIDC is disabled.


**Covered by:** `ConfigurationController`, `AppSettingsFileService`, `Program.cs` (JWT Bearer auth setup), `WorkspaceController` (shared appsettings helper reuse)
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-CFG-007

**Encryption Configuration and Provider Settings Surface** — `Mcp:Database:Provider` and related connection-string settings SHALL support SQLite, PostgreSQL, and SQL Server selection through appsettings and environment-variable overrides. The configuration surface SHALL expose an explicit optional encryption-enabled flag plus the provider-specific connection, key, and prerequisite settings needed by the selected native at-rest encryption facility. Configuration resolution SHALL be centralized so runtime startup and design-time EF tooling can resolve the same effective provider and encryption inputs.

**Covered by:** `src/QBrainAi.Support.Mcp/Options/McpDatabaseConfigurationResolver.cs`, `src/QBrainAi.Storage/McpDbContextFactory.cs`, `src/QBrainAi.Support.Mcp/Program.cs`, `src/QBrainAi.Support.Mcp/McpStdio/McpStdioHost.cs`, `src/QBrainAi.Support.Mcp/appsettings.yaml`, `src/QBrainAi.Support.Mcp/appsettings.Staging.yaml`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-CI-001

**Azure DevOps Repository Pipeline Definition** — The repository SHALL use `azure-pipelines.yml` as the CI/CD definition for the core repo workflow. The pipeline SHALL trigger on `main` and `develop` pushes and pull requests with path filters matching the tracked source, test, docs, script, template, and pipeline-definition files. The pipeline SHALL run repository config validation, restore/build/test the support MCP test project, compute package versioning from GitVersion, publish the server build artifact, lint and link-check documentation, build the DocFX site artifact, run Windows MSIX packaging as a non-blocking job, and pack the client NuGet package.
Package publication SHALL be branch-conditional: `main` publishes to `nuget.org` only when `NuGetApiKey` is configured, while non-`main` branches publish to Azure Artifacts only when `AzureArtifactsFeedUrl` is configured. Optional docs deployment to Azure static website storage SHALL be gated behind explicit pipeline variables so the repo pipeline remains portable when deployment infrastructure is absent. The retired GitHub Actions workflow YAML files SHALL be removed from the repository as part of the migration, and retention of stale runs/artifacts SHALL move to Azure DevOps retention policy configuration rather than a repository-hosted cleanup workflow.

**Status:** ✅ Complete

**Covered by:** `azure-pipelines.yml`, `docs/AZURE-PIPELINES.md`, `README.md`, `docs/MCP-SERVER.md`, `docs/RELEASE-CHECKLIST.md`
Scope: layer-1+

## TR-MCP-CLEARSESSION-001

**clear-session skill content contract** — The clear-session SKILL.md SHALL satisfy: AC1 - end the session by finalizing the open turn via workflow.sessionlog.completeTurn and closing the session through the plugin wrapper (lib/repl-invoke.ps1 / Invoke-McpPlugin.ps1 or hooks/scripts/session-end.ps1 where present), never raw REST. AC2 - clear context best-effort programmatically (plugin cache/session-state flush), then fall back to a per-host manual clear command (/clear for Claude, /new for Codex/OpenCode, New Task for Cline, new chat for Copilot) and pause for user confirmation; never claim context cleared when only the user can do it. AC3 - reload the agent instruction file selected by host: CLAUDE.md for claude/claude-cowork, AGENTS.md otherwise, always after re-reading AGENTS-README-FIRST.yaml, carried verbatim. AC4 - execute the add-profile skill. AC5 - report a readiness summary stating whether context was actually cleared or awaits user action. The skill SHALL be PowerShell-only (no bash/node/.sh references) and present byte-identical in all 8 agent plugin repositories.
**Covered by:** FR: FR-MCP-CLEARSESSION-001; TEST: TEST-MCP-CLEARSESSION-001
**Status:** pending
Scope: layer-1+

## TR-MCP-CLIENT-001

**Typed client request bodies are registered for source-generated JSON** — Implements FR-MCP-143 and closes BUG-TRIAGE-088, 093, 095, and the client half of 090, 094, and 101. McpClientBase serializes every request through the source-generated McpClientJsonContext, whose TypeInfoResolver has no JsonTypeInfo for compiler-generated anonymous types. src/QBrainAi.Client/SessionLogClient.cs nevertheless posts anonymous bodies at line 160 (session lifecycle open, new { title, model }) and lines 236 and 247 (SetSessionTitleAsync and SetTurnTitleAsync, new { title }), so GetTypeInfo throws NotSupportedException and those endpoints never execute. Every request body in the typed client SHALL be a declared type carrying JsonPropertyName attributes and XML documentation, and SHALL be registered in McpClientJsonContext with JsonSerializable. The whole client SHALL be audited for the same pattern rather than only the three known sites, because this defect class was reported five separate times, and the audit result SHALL be reported. Coverage SHALL exercise the real McpClientJsonContext serialization path rather than a hand-rolled JsonSerializer call, so the test fails for the same reason production did.
**Covered by:** FR: FR-MCP-143; TEST: TEST-MCP-194, TEST-MCP-REPL-040
**Status:** pending
Scope: layer-1+

## TR-MCP-COMP-001

**Workspace Compliance Ban Lists** — `WorkspaceDto`, `WorkspaceCreateRequest`, and `WorkspaceUpdateRequest` include four `List<string>` properties: `BannedLicenses`, `BannedCountriesOfOrigin`, `BannedOrganizations`, `BannedIndividuals`. `MarkerFileService.BuildTemplateContext` exposes these as Handlebars context (null when empty). `DefaultPromptTemplate` uses `{{#if}}` / `{{#each}}` blocks to conditionally render compliance sections. Recognized action types: `license_violation`, `origin_violation`, `origin_review`, `entity_violation`, `dependency_add`.
**Covered by:** `IWorkspaceService.cs`, `MarkerFileService.cs`
**Status:** pending
Scope: layer-1+

## TR-MCP-COMP-002

**Agent Values Prompt Sections** — `DefaultPromptTemplate` includes five mandatory non-configurable sections: (1) Absolute Honesty, (2) Correctness Above All, (3) Complete Decision Documentation, (4) Professional Representation and Audit Trail, (5) Source Attribution. Each section specifies required session log action types (`commit`, `pr_comment`, `issue_comment`, `web_reference`, `design_decision`).
**Covered by:** `MarkerFileService.DefaultPromptTemplate`
**Status:** pending
Scope: layer-1+

## TR-MCP-COMP-003

**Session Continuity Protocol** — The `default-marker-prompt` template (YAML) includes Requirements Tracking, Design Decision Logging, and Session Continuity sections. Agents must: read marker file at session start, query recent session logs, query TODOs, read Requirements-Matrix.md, post updated session logs every ~10 interactions, and capture requirements/decisions as they emerge.
**Covered by:** `templates/prompt-templates.yaml` (`default-marker-prompt`), `PromptTemplateService`
**Status:** pending
Scope: layer-1+

## TR-MCP-CQRS-001

**Standalone CQRS Library** — `QBrainAi.Cqrs` published as NuGet package `QBrainAI.Cqrs`. Targets `net9.0`. Zero external dependencies beyond `Microsoft.Extensions.Logging.Abstractions` and `Microsoft.Extensions.DependencyInjection.Abstractions`. Provides: `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`, `Dispatcher`, `CallContext`, `CorrelationId`, `Result<T>`, `IPipelineBehavior`, and DI registration extensions. All dispatched calls are async (`Task<Result<T>>`).

**Covered by:** `QBrainAi.Cqrs` project
**Status:** ✅ complete - 37 unit tests passing
Scope: layer-1+

## TR-MCP-CQRS-002

**Decimal Correlation IDs** — `CorrelationId` uses format `{baseId}.{counter}` where `baseId` is a random 8-digit long (stable for the entire call tree) and `counter` is a thread-safe (`Interlocked.Increment`) incrementing integer. Each pipeline step or handler call advances the counter. `CorrelationId.Parse(string)` reconstitutes from string. Propagated via HTTP headers (`X-Correlation-Id`).

**Covered by:** `CorrelationId`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-CQRS-003

**Dispatcher as ILoggerProvider with Context Registry** — `Dispatcher` implements `ILoggerProvider` and maintains a `ConcurrentDictionary<long, CallContext>` of active contexts keyed by `CorrelationId.BaseId`. `DispatcherLogger` (created by the provider) extracts correlation IDs from log scopes, looks up the `CallContext`, and enriches structured log entries with decomposed fields: `correlationId`, `correlationBaseId`, `correlationStep`, `operationName`, `userId`, `roles`, `elapsed`. `CallContext` implements `ILogger` and captures log entries to an internal list.

**Covered by:** `Dispatcher`, `DispatcherLogger`, `CallContext`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-CQRS-004

**Automatic Result Monad Logging** — After handler execution, the Dispatcher inspects the `Result<T>`: success results logged at `Debug` level with elapsed time; failures with `Exception` logged at `Error` level with exception details; failures without exception logged at `Warning` level. Dispatch calls themselves logged at `Debug` with full call context. All logging includes decomposed correlation ID fields.

**Covered by:** `Dispatcher`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-CQRS-005

**Pipeline Behaviors** — `IPipelineBehavior` wraps handler execution with pre/post processing. Behaviors receive the request, `CallContext`, and a `next` delegate. Behaviors can short-circuit by returning `Result<T>.Failure()` without calling `next`. Registration order determines execution order (outermost first). Built-in behaviors: `LoggingBehavior`, `ValidationBehavior`.

**Covered by:** `IPipelineBehavior`, `Dispatcher`
**Status:** ✅ complete
Scope: layer-1+

## TR-MCP-CRYPTO-001

**Transactional Diffgram Cryptography** — Transaction manifests SHALL use canonical JSON, lowercase SHA-256 hashes, ECDSA P-256 signatures, nonces, monotonic sequence scopes, issued/expiry timestamps, diffgram body hashes, and encrypted body hashes. Protected subscriber diffgram envelopes SHALL use ECDH P-256, HKDF-SHA256, and AES-256-GCM with subscriber key-ring support for old and rotated keys.

**Covered by:** `TransactionSecurityModels`, `TransactionSecurityServices`, `TurnTransactionCoordinator`, `TransactionSecurityStateStores`, `TransactionSecurityControllerTests`, `TransactionSecurityClientTests`, `DurableTransactionSecurityStorageTests`, `SeparateTransactionServiceIntegrationTests`
**Status:** pending
Scope: layer-1+

## TR-MCP-CTX-001

**New Project Context Indexing** — Repo-local context indexing configuration must include src/McpServer.Cqrs/**/*.cs and src/McpServer.Cqrs.Mvvm/**/*.cs. The marker prompt Available Capabilities section must list only these repo-local core libraries; moved McpServer.UI.Core and McpServer.Director capabilities belong to McpServerManager.
**Covered by:** FR: FR-MCP-039
**Status:** pending
Scope: layer-1+

## TR-MCP-DATA-001

SQLite persistence for MCP metadata and optional TODO backend.
**Covered by:** FR: FR-MCP-002, FR-MCP-003, FR-SUPPORT-010
**Status:** pending
Scope: layer-1+

## TR-MCP-DATA-002

HNSW vector index with ONNX embeddings.
**Covered by:** FR: FR-MCP-004
**Status:** pending
Scope: layer-1+

## TR-MCP-DATA-003

SQLite FTS5 full-text search support and hybrid ranking.
**Covered by:** FR: FR-MCP-004
**Status:** pending
Scope: layer-1+

## TR-MCP-DB-001

**Database-authoritative workspace registry** — Workspaces must be stored in a canonical Workspaces table as the source of truth, with appsettings workspace entries generated only as informational projections after successful database commits.
**Covered by:** FR: FR-MCP-105; TEST: TEST-MCP-138, TEST-MCP-139, TEST-MCP-140, TEST-MCP-DB-006
**Status:** pending
Scope: layer-1+

## TR-MCP-DB-002

**Workspace foreign-key integrity** — Every persistent table with WorkspaceId must have a required FK to Workspaces, including global rows through a reserved empty WorkspaceId row and federation workspace mappings.
**Covered by:** FR: FR-MCP-105; TEST: TEST-MCP-138, TEST-MCP-139, TEST-MCP-140, TEST-MCP-DB-006
**Status:** pending
Scope: layer-1+

## TR-MCP-DB-003

**Soft deletes for persistent MCP data** — Persistent MCP domain deletes must be logical deletes with deletion metadata and Restrict or NoAction relationships, never physical row removal or cascade delete for durable domain state.
**Covered by:** FR: FR-MCP-105; TEST: TEST-MCP-138, TEST-MCP-139, TEST-MCP-140, TEST-MCP-DB-006
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Resubmitting a deleted session revives exactly the rows the session delete soft-deleted, those sharing the session row's DeletedAtUtc; rows removed earlier by a section replace, PUT replace, item delete or turn delete stay deleted (PLAN-SESSIONTRIAGEFIX-001 A10, triage-report-1af9a7de; SubmitAsync_AfterSessionDelete_RevivesOnlyRowsTheDeleteRemoved, ReviveFixture_ReplacedRowAndSessionDeleteRowsCarryDistinctDeleteMarks).
- [ ] A session-log submit or turn upsert that names a turn whose row is deleted is refused before any change with a 409 conflict that names the retired request id (operator decision 2026-10-13 "Refuse with clear conflict"; SubmitAndUpsert_NamingDeletedTurn_AreRefusedWithConflictAndChangeNothing, RetiredRequestAssertion_AcceptsNamedConflictAndRejectsOtherErrors).

## TR-MCP-DB-004

**Generic audit ledger for mutable data** — Every mutable persistent database entity must emit append-only audit rows with workspace, entity key, action, actor/source, timestamps, and previous/current snapshots, while TODO-specific audit history remains compatible.
**Covered by:** FR: FR-MCP-105; TEST: TEST-MCP-138, TEST-MCP-139, TEST-MCP-140, TEST-MCP-DB-006
**Status:** pending
Scope: layer-1+

## TR-MCP-DB-005

**TODO and requirement relational links** — TODO requirement references and requirement traceability links must be stored as relational rows with FKs to TODO lifecycle anchors and Requirements, with missing referenced requirements backfilled before FK enforcement.
**Covered by:** FR: FR-MCP-105; TEST: TEST-MCP-138, TEST-MCP-139, TEST-MCP-140, TEST-MCP-DB-006
**Status:** pending
Scope: layer-1+

## TR-MCP-DB-006

**Concurrent turn deletes serialized per session** — Concurrent sessionlog_delete_turn operations on a single session SHALL be serialized through a process-wide per-session gate so a burst (observed: 13 parallel deletes) cannot exhaust or poison the SQL Server connection pool; deletes on different sessions SHALL still run concurrently; the deployed SqlClient ConnectRetryCount=6/ConnectRetryInterval=10s defaults SHALL be preserved. Acceptance Criteria: (AC1) acquiring the per-session lock for the same key blocks a second acquirer until release, while a different key acquires without blocking (KeyedAsyncLock); (AC2) DeleteTurnAsync continues to soft-delete a turn and refresh the parent turn count correctly under the lock. Origin: BUG-TRIAGE-079 (concurrent gated deletes poisoned the pool with SqlClient error 19 then 40 for ~3 minutes while SQL Server stayed up). Note: pool poisoning is a SQL-Server-runtime property not unit-reproducible; the deterministic serialization proof is the KeyedAsyncLock unit test, with SessionLogService.DeleteTurnAsync wrapped in the per-session lock keyed by sourceType/sessionId. Validated by TEST-MCP-DB-006.
**Covered by:** FR: FR-MCP-105; TEST: TEST-MCP-138, TEST-MCP-139, TEST-MCP-140, TEST-MCP-DB-006
**Status:** pending
Scope: layer-1+

## TR-MCP-DB-DTO-001

**DateTimeOffset values are stored as UTC DateTime on every provider** — McpDbContext stores every DateTimeOffset property as a UTC DateTime through DateTimeOffsetToUtcDateTimeConverter, applied to all DateTimeOffset properties in ConfigureConventions, so the SQLite provider can translate timestamp predicates and ordering to SQL; every timestamp in the schema is UTC, so the conversion is lossless. Each provider's migrations create the columns the converted model maps; on SQL Server that is datetime2 (RequirementsRecoveryRuns.DeletedAtUtc was created as datetimeoffset and corrected by AddHostileReviewTables, PLAN-SESSIONTRIAGEFIX-001 B8).
**Covered by:** FR: FR-MCP-105; TEST: TEST-MCP-102, TEST-MCP-138, TEST-MCP-139, TEST-MCP-140, TEST-MCP-DB-006
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] For SQLite, PostgreSQL and SQL Server, the model configured the way startup configures it has no pending changes against that provider's migrations, so every DateTimeOffset column the migrations create matches the converted model (ProviderMigrationModelTests.ProviderMigrations_HaveNoPendingModelChanges).
- [ ] On SQL Server, RequirementsRecoveryRuns.DeletedAtUtc is datetime2 and a soft-deleted recovery run reads back its DeletedAtUtc as the same UTC instant (TEST-MCP-102, SqlServer_RequirementsRecoveryRun_SoftDelete_RoundTripsUtcDeletedAt).

## TR-MCP-DESKTOP-001

**Desktop Process Launcher** — `DesktopProcessLauncher` in `Native/` uses P/Invoke (`WTSQueryUserToken`, `DuplicateTokenEx`, `CreateProcessAsUser`) to launch processes on the interactive desktop from a LocalSystem service context. Two launch modes: `LaunchWithStdio` (redirected stdin/stdout/stderr pipes for Copilot CLI integration) and `LaunchVisible` (visible console window, no pipes). `ResolveCommandPathAsync` resolves WinGet shim paths via desktop PowerShell to find actual executable locations. Uses `CreateProcessAsUser` (not `CreateProcessWithTokenW`, which causes `STATUS_DLL_INIT_FAILED` under LocalSystem).
**Covered by:** `DesktopProcessLauncher`, `NativeMethods`
**Status:** pending
Scope: layer-1+

## TR-MCP-DIR-001

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-001)*
**Covered by:** FR: FR-MCP-030
**Status:** pending
Scope: layer-1+

## TR-MCP-DIR-002

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-002)*
**Covered by:** FR: FR-MCP-030
**Status:** pending
Scope: layer-1+

## TR-MCP-DIR-003

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-003)*
**Covered by:** FR: FR-MCP-030, FR-MCP-037
**Status:** pending
Scope: layer-1+

## TR-MCP-DIR-004

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-004)*
**Covered by:** FR: FR-MCP-057
**Status:** pending
Scope: layer-1+

## TR-MCP-DIR-005

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-005)*
**Covered by:** FR: FR-MCP-060
**Status:** pending
Scope: layer-1+

## TR-MCP-DIR-006

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-006)*
**Covered by:** FR: FR-MCP-060
**Status:** pending
Scope: layer-1+

## TR-MCP-DIR-007

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-007)*
**Covered by:** FR: FR-MCP-060
**Status:** pending
Scope: layer-1+

## TR-MCP-DIR-008

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-008)*
**Covered by:** FR: FR-MCP-060
**Status:** pending
Scope: layer-1+

## TR-MCP-DOC-001

**Marketing documentation coverage** — Marketing and agent-facing documentation shall explain McpServer purpose, supported UI and agent surfaces, plugin acquisition through the MCP tool registry, single-line JSON stdio guidance, and generated requirements wiki parity.
**Covered by:** FR: FR-MCP-064; TEST: TEST-MCP-147
**Status:** pending
Scope: layer-1+

## TR-MCP-DOC-002

**Test XML Documentation Completeness** *(DIRECTIVE)* - All test projects SHALL include XML documentation comments on test classes and test methods. Each test XML doc SHALL explicitly specify: what behavior is being tested, what test data/fixtures are used, why that data/fixtures are used, and which requirement IDs are being validated. No test project is exempt from this requirement.


**Covered by:** `.github/copilot-instructions.md`, `AGENTS.md`
**Status:** pending
Scope: layer-1+

## TR-MCP-DOCFXWIKI-001

**Typed and isolated DocFX wiki workflow execution** — DocFX workflows must be loaded through typed wiki configuration, executed without a shell through a bounded process abstraction, staged under the workspace, and merged through the existing requirements wiki renderer and atomic export writer.
**Covered by:** FR: FR-MCP-DOCFXWIKI-001; TEST: TEST-MCP-DOCFXWIKI-001
**Status:** pending
Scope: layer-1+

## TR-MCP-DRY-001

**DRY - No Duplication in Code or Scripts** *(DIRECTIVE)* - All code and scripts must follow the DRY principle without exception. Shared logic must be extracted into a single reusable location (service, helper, function, shared script module). Inline duplication of validation, parsing, formatting, or business logic across files is prohibited. Scripts must share common operations via parameterized functions or a shared module.

**Covered by:** `TodoValidator`, `MarkerFileService`, `ExcludeControllerFeatureProvider`, `Update-McpService.ps1`
**Status:** pending
Scope: layer-1+

## TR-MCP-DTO-001

**Extended Session Log Entry Fields** — `UnifiedRequestEntryDto` extended with: `designDecisions` (`List<string>`), `requirementsDiscovered` (`List<string>` of requirement IDs), `filesModified` (`List<string>` of file paths), `blockers` (`List<string>`). All fields are REQUIRED in the marker prompt session logging instructions except `blockers` which is RECOMMENDED.
**Covered by:** `UnifiedSessionLogDto.cs`
**Status:** pending
Scope: layer-1+

## TR-MCP-EVT-001

**In-Process Change Event Bus** — `ChannelChangeEventBus` SHALL be registered as a singleton `IChangeEventBus` and provide fan-out publish/subscribe semantics to independent subscribers using bounded channels (capacity 1000) with non-blocking publish behavior. When a subscriber buffer is full, delivery to that subscriber SHALL be rejected and logged at warning level instead of silently discarding already queued events.
**Covered by:** `ChannelChangeEventBus`, `IChangeEventBus`, `Program.cs`
**Status:** pending
Scope: layer-1+

## TR-MCP-EVT-002

**Service-Layer Mutation Publishing** — Mutating service operations SHALL publish change events after successful persistence, with event emission wrapped in defensive try/catch and warning-level logging on publish failures.
**Covered by:** `TodoService`, `SqliteTodoService`, `SessionLogService`, `RepoFileService`, `ToolRegistryService`, `ToolBucketService`, `WorkspaceService`, `AgentService`, `RequirementsDocumentService`, `IngestionCoordinator`, `WorkspaceProcessManager`
**Status:** pending
Scope: layer-1+

## TR-MCP-EVT-003

**SSE Delivery Endpoint** — `EventStreamController` SHALL stream notifications as `text/event-stream` with `Cache-Control: no-cache` and support optional category filtering via `?category=` query parameter.
**Covered by:** `EventStreamController`
**Status:** pending
Scope: layer-1+

## TR-MCP-EVT-004

**Change Event Contract** — Change events SHALL include `Category`, `Action`, optional `EntityId`, optional `ResourceUri`, and UTC `Timestamp` to support correlation by consumers.
**Covered by:** `ChangeEvent`, `ChangeEventActions`, `ChangeEventCategories`
**Status:** pending
Scope: layer-1+

## TR-MCP-EVT-005

**Workspace Notification Category Coverage** — The notification system SHALL support at minimum the categories: `todo`, `session_log`, `repo`, `context`, `tool_registry`, `tool_bucket`, `workspace`, `github`, `marker`, `agent`, and `requirements`.
**Covered by:** `ChangeEventCategories` and all publishing call sites in mutation services/controllers
**Status:** pending
Scope: layer-1+

## TR-MCP-FAILSAFE-001

**Failsafe drain abort markers include backend_unavailable and HTTP 503** — Test-ReplFailsafeBackendUnreachable matches backend_unavailable and HTTP 503. Invoke-ReplFailsafeDrainOnFirstSuccess does not set completed before a 503 pass and does not increment drainAttempts or quarantine on that abort. Covered by FR-MCP-FAILSAFE-001 TEST-MCP-FAILSAFE-001.
**Covered by:** FR: FR-MCP-FAILSAFE-001; TEST: TEST-MCP-FAILSAFE-001
**Status:** pending
Scope: layer-1+

## TR-MCP-FED-001

**Hub Proxy Federation Contract** — Federation configuration SHALL include Role, HubBaseUrl, ProxyId, EnrollmentToken, queue settings, and sync settings while preserving existing target/route configuration. Durable storage SHALL track proxies, proxy-hosted workspaces, operations, outbox fanout rows, and conflicts across SQLite, PostgreSQL, and SQL Server providers. Hub endpoints SHALL support proxy enrollment, heartbeat, proxy/workspace inventory, operation intake, acknowledgement, queue status, conflicts, sync, and adapter coverage. LocalProxy routing SHALL forward MCP traffic to the hub with loop-protection and operation headers, while local infrastructure and federation diagnostic endpoints remain local. Mutating LocalProxy requests SHALL queue durably when the hub is unreachable and replay through the hub intake endpoint.
**Covered by:** FR: FR-MCP-103; TEST: TEST-MCP-136, TEST-MCP-MEMORY-FED-001
**Status:** pending
Scope: layer-1+

## TR-MCP-FED-BACKFILL-001

**TR-MCP-FED-BACKFILL-001** — Placeholder requirement backfilled for TODO link TR-MCP-FED-BACKFILL-001.
**Status:** pending
Scope: layer-1+

## TR-MCP-FED-MEMORY-001

**Memory Federation Adapter Contract** — Memory federation SHALL register a memory state adapter that snapshots active memory rows by globally unique memory ID and applies signed REST-originated memory operations. The adapter SHALL preserve memory ID, scope, workspace ownership, category, raw text, timestamps, soft-delete semantics, and version tokens based on MemoryEntity.Version. Workspace-scoped memory rows SHALL only apply when the operation GlobalWorkspaceId matches the row owner. LocalProxy queueing SHALL accept POST /mcpserver/memory only when the JSON body supplies an explicit valid MEMORY-* ID, and SHALL accept PUT, PATCH, and DELETE /mcpserver/memory/{id} as replayable memory operations.
**Covered by:** FR: FR-MCP-103, FR-MCP-MEMORY-008; TEST: TEST-MCP-136, TEST-MCP-MEMORY-FED-001
**Status:** pending
Scope: layer-1+

## TR-MCP-FED-PATH-001

**Single request-path normalization entry point** — Introduce a single request-path normalization entry point (IWorkspacePathNormalizer backed by WorkspaceIdentityPath) with an injectable host-platform abstraction; all resolvers listed in section 1.2 use it. No direct Path.GetFullPath on request-supplied workspace paths outside it (enforced by an architecture test).
**Covered by:** FR: FR-MCP-FED-PATH-001; TEST: TEST-MCP-FED-PATH-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] IWorkspacePathNormalizer.Combine treats a segment with one leading backslash as root-relative on Windows: it keeps the current root's volume, the drive (C:/) or the UNC server and share (//srv/share/), and replaces the rest of the path, on a Linux hub and a Windows host (PR 81 finding P12, PR 82 review). (evidence: tests/QBrainAi.Support.Mcp.Tests/Storage/WorkspacePathNormalizerContractTests.cs (CombineResolvesRootRelativeWindowsSegmentAgainstDrive, CombineResolvesRootRelativeWindowsSegmentAgainstUncShare); WorkspacePathNormalizerMockDataTests.cs (RootRelativeNegativeControls_AreRejectedInsidePassingHarness); PR 82 commits f6768359 and dcd0269a)

## TR-MCP-FED-ROUTE-001

**TR-MCP-FED-ROUTE-001** — Placeholder requirement backfilled for TODO link TR-MCP-FED-ROUTE-001.
**Status:** pending
Scope: layer-1+

## TR-MCP-FED-SESSIONLOG-001

**Session-log federation snapshot carries the full session graph** — SessionLogFederationStateAdapter.SnapshotAsync SHALL return, for an existing session of the local store, a payload in the UnifiedSessionLogDto shape that the adapter's ApplyAsync accepts, carrying every session field and every turn with all of its fields and child collections (actions, tags, context items, processing dialog, commits with their files, design decisions, requirements discovered, files modified, blockers), equal to what the local session-log read returns for that session after the outbound session-log sanitizer. Building a snapshot reads only the local store. The snapshot version stays the session's explicit version (ContentHash, else LastUpdated in Unix milliseconds, else TurnCount).
**Covered by:** FR: FR-MCP-103; TEST: TEST-MCP-136, TEST-MCP-MEMORY-FED-001, TEST-MCP-SESSIONLOG-FED-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] For a session whose turn carries a value in every turn field and at least one item in every child collection (commits with files), the snapshot payload deserializes as UnifiedSessionLogDto and equals the local session-log read of that session field by field, child collections in order.
- [ ] A snapshot payload is applyable: applying it through the adapter's ApplyAsync to a store without that session and then reading the session there yields a DTO equal to the source read, for values the sanitizer leaves unchanged.
- [ ] A value the outbound session-log sanitizer redacts in the session-log read is redacted the same way in the snapshot payload.
- [ ] Building a snapshot makes no federation data call: a session that exists only on a peer is not snapshotted, and a local session's snapshot carries only local turns.
- [ ] A missing or soft-deleted session yields PayloadJson "null" and Version null, and an existing session's Version equals its explicit version (ContentHash, else LastUpdated in Unix milliseconds, else TurnCount), as before this requirement.

## TR-MCP-FED-WSSYNC-001

**TR-MCP-FED-WSSYNC-001** — Placeholder requirement backfilled for TODO link TR-MCP-FED-WSSYNC-001.
**Status:** pending
Scope: layer-1+

## TR-MCP-FILETOOLS-001

**Server-owned repository discovery core** — A single read-only repository discovery service must implement OpenAI-compatible read_file, list_dir, and grep_files behavior while enforcing active-workspace containment, allowlists, traversal rejection, and reparse-point safety.
**Covered by:** FR: FR-MCP-FILETOOLS-001; TEST: TEST-MCP-FILETOOLS-001, TEST-MCP-FILETOOLSINT-001
**Status:** pending
Scope: layer-1+

## TR-MCP-GH-001

**GitHub OAuth Bootstrap Configuration Contract** — The server SHALL bind GitHub integration settings from `Mcp:GitHub`, including OAuth client metadata (`ClientId`, `RedirectUri`, `AuthorizeEndpoint`, `Scopes`) and token store path/fallback policy flags. REST endpoints under `/mcpserver/gh/oauth/*` SHALL expose the effective bootstrap configuration and authorize URL composition.

**Covered by:** `GitHubIntegrationOptions`, `Program.cs` options binding/post-configure, `McpStdioHost` options binding/post-configure, `GitHubController` (`/oauth/config`, `/oauth/authorize-url`)
**Status:** pending
Scope: layer-1+

## TR-MCP-GH-002

**Encrypted Workspace GitHub Token Persistence** — Workspace GitHub tokens SHALL be stored encrypted-at-rest using ASP.NET Core Data Protection with atomic file writes and normalized workspace-path keys. The server SHALL expose `/mcpserver/gh/auth/status`, `/mcpserver/gh/auth/token` (PUT), and `/mcpserver/gh/auth/token` (DELETE) for token lifecycle management.

**Covered by:** `IGitHubWorkspaceTokenStore`, `FileGitHubWorkspaceTokenStore`, `GitHubController` auth endpoints, `Program.cs` DI registration
**Status:** pending
Scope: layer-1+

## TR-MCP-GH-003

**Authenticated GitHub CLI Execution Path with Policy-Governed Fallback** — GitHub CLI execution SHALL support per-call token overrides so workspace-stored tokens can be applied as `GH_TOKEN` when present. The execution path SHALL prefer stored tokens when configured, emit telemetry indicating selected auth mode, and reject/allow fallback based on `AllowCliFallback`. When a workspace path is known, gh commands SHALL execute with that workspace root as the working directory.

**Covered by:** `IProcessRunner` (`ProcessRunRequest` overload), `ProcessRunner`, `GitHubCliService` token resolution + auth-mode selection logs, `GitHubIntegrationOptions`
**Status:** pending
Scope: layer-1+

## TR-MCP-GH-004

**GitHub Actions Workflow Run API Surface** — The server SHALL support workflow run list/detail/rerun/cancel operations via gh CLI and expose them at `/mcpserver/gh/actions/runs*` with typed model contracts and client parity.

**Covered by:** `IGitHubCliService`, `GitHubCliService`, `GitHubController` actions endpoints, `McpServer.Client` (`GitHubClient`, `Models/GitHubModels.cs`)
**Status:** pending
Scope: layer-1+

## TR-MCP-GH-005

**Workspace-Scoped gh Repository Execution** — GitHub issue and sync operations that rely on the local gh CLI SHALL execute inside the resolved workspace root so repository-scoped gh commands run against the correct checkout. This SHALL apply to both stored-token and fallback-auth execution modes.

**Covered by:** `WorkspaceServiceAccessor`, `GitHubCliService`, `ProcessRunRequest`
**Status:** pending
Scope: layer-1+

## TR-MCP-GH-006

**Canonical GitHub Priority Labels, MCP-Authoritative Priority Sync, and ISSUE Change Comments** — TODO-to-GitHub issue sync SHALL canonicalize priority labels to `priority: HIGH|MEDIUM|LOW`, SHALL remove stale or non-canonical priority labels, and SHALL treat the MCP TODO priority as authoritative even if GitHub labels drift. GitHub-to-TODO refresh for existing `ISSUE-*` items SHALL preserve the current local priority and description, and endpoint-triggered ISSUE updates SHALL add a GitHub issue comment that summarizes the applied local change set after sync completes.

**Covered by:** `IssueTodoSyncService`, `GitHubCliService`
**Status:** pending
Scope: layer-1+

## TR-MCP-GH-007

**Generated GitHub Comment Note Sections and TODO Comment Round-Trip** — GitHub-to-TODO sync for existing `ISSUE-*` items SHALL rebuild a generated note section that contains GitHub issue comments inside explicit begin/end markers, SHALL preserve user-authored TODO note text outside that generated section, and SHALL continue to avoid mutating the established TODO description. TODO-to-GitHub comment export SHALL detect newly appended user-authored note text and publish that text as a GitHub issue comment rather than collapsing the change to a generic note-update summary. When GitHub marks the issue closed, the next GitHub-to-TODO sync SHALL reconcile the TODO as done.

**Covered by:** `IssueTodoSyncService`
**Status:** pending
Scope: layer-1+

## TR-MCP-GH-008

**Ownership-safe GitHub CLI repository selection** — GitHub CLI invocations SHALL either use an explicit configured or inferred repository selector through gh --repo without local repository discovery, or pass a command-scoped safe.directory Git configuration for the active workspace when a workspace working directory is required.
**Covered by:** FR: FR-MCP-116; TEST: TEST-MCP-156
**Status:** pending
Scope: layer-1+

## TR-MCP-HEALTH-002

**WorkspaceReadinessHealthCheck** — A new IHealthCheck registered as AddCheck<WorkspaceReadinessHealthCheck>("workspace-ready", tags: ["ready"]) and surfaced on /ready. It returns Unhealthy when !WorkspaceTokenService.IsInitialized, when no enabled workspace is registered, or when the primary workspace has no seeded token; Healthy otherwise.
**Covered by:** FR: FR-MCP-133; TEST: TEST-MCP-HEALTH-002, TEST-MCP-HEALTH-003
**Status:** pending
Scope: layer-1+

## TR-MCP-HEALTH-003

**Health reflects storage reachability and backend errors are typed** — Implements FR-MCP-143 and closes BUG-TRIAGE-096. GET /health is liveness-only (tags live: self plus FederationUpstreamHealthCheck) with no storage connectivity check, so when the database backend is unreachable the server still answers Healthy and echoes the trust nonce correctly while every persistence call fails. Observed live on 2026-07-20 when the SQL host was powered off: marker trust passed, then todo_list returned a raw SqlClient "Named Pipes Provider, error: 40" string, sessionlog_query returned a misleading "transient failure, consider EnableRetryOnFailure" hint, triage_report returned a bare "an error occurred", and the plugin wrapper returned an opaque 30-second timeout, so no caller could tell that storage was simply down. The server SHALL expose storage reachability on the health path, either by degrading the status or by an explicit storage field, WITHOUT breaking the nonce echo that marker trust bootstrap depends on. Backend-unavailable conditions SHALL surface as one typed error consistently across REST and the MCP tools, distinguishable from a malformed request. Coverage SHALL simulate an unreachable store and assert the misleading Healthy result before the fix.
**Covered by:** FR: FR-MCP-143; TEST: TEST-MCP-194, TEST-MCP-REPL-040
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-001

**Agent Help options and startup validation** — AgentHelpOptions binds from the AgentHelp configuration section; AgentHelpOptionsValidator rejects unsupported execution strategies, missing transcript or incident directories, and incomplete API key wiring.
**Covered by:** FR: FR-MCP-HELP-010; TEST: TEST-MCP-HELP-004
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-002

**Agent Help HTTP and WebSocket API surface** — AgentHelpController exposes session create, status, synchronous turn, SSE stream, transcript retrieval, and WebSocket turn streaming under /mcpserver/agent-help.
**Covered by:** FR: FR-MCP-HELP-001, FR-MCP-HELP-008; TEST: TEST-MCP-HELP-004, TEST-MCP-HELP-005, TEST-MCP-HELP-SEC-007
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-003

**Help transcript JSONL writer** — HelpTranscriptWriter appends one JSON object per line under the configured transcript directory and reads all entries for a session.
**Covered by:** FR: FR-MCP-HELP-003; TEST: TEST-MCP-HELP-001, TEST-MCP-HELP-005
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-004

**Deterministic inbound guard** — AgentHelpInboundGuard applies stable injection and benign-bypass rules before helper execution.
**Covered by:** FR: FR-MCP-HELP-002; TEST: TEST-MCP-HELP-002, TEST-MCP-HELP-SEC-001, TEST-MCP-HELP-SEC-002, TEST-MCP-HELP-SEC-003, TEST-MCP-HELP-SEC-004, TEST-MCP-HELP-SEC-005, TEST-MCP-HELP-SEC-006
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-005

**Guard incident JSON logger** — AgentHelpIncidentLogger writes one incident file per blocked inbound message and supports session-scoped reads.
**Covered by:** FR: FR-MCP-HELP-004; TEST: TEST-MCP-HELP-003, TEST-MCP-HELP-SEC-006
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-006

**Stub corpus bootstrap** — AgentHelpCorpusService returns a deterministic context pack summary without full indexing when corpus bootstrap is enabled.
**Covered by:** FR: FR-MCP-HELP-005; TEST: TEST-MCP-HELP-004
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-007

**In-memory conversation orchestration** — AgentHelpConversationService maintains the session registry, evaluates inbound guards, executes helper turns, and coordinates transcript and incident persistence.
**Covered by:** FR: FR-MCP-HELP-001; TEST: TEST-MCP-HELP-004, TEST-MCP-HELP-005
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-008

**Outcome analysis recommendations** — AgentHelpOutcomeService derives triage and documentation TODO recommendations from help session transcripts and incidents.
**Covered by:** FR: FR-MCP-HELP-005; TEST: TEST-MCP-HELP-004
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-009

**MCP STDIO and typed client adapters** — FwhMcpTools.AgentHelp exposes agent_help tools; AgentHelpClient and McpServerClient.AgentHelp mirror the REST controller contracts.
**Covered by:** FR: FR-MCP-HELP-006, FR-MCP-HELP-007, FR-MCP-HELP-009; TEST: TEST-MCP-HELP-006, TEST-MCP-HELP-007, TEST-MCP-HELP-008
**Status:** pending
Scope: layer-1+

## TR-MCP-HELP-010

**GrokCliAgentExecutionStrategy one-shot grok invocation** — GrokCliAgentExecutionStrategy SHALL run grok as a one-shot process delivering the prompt via --prompt-file and capturing plain stdout, resolve the grok executable via ProcessEnvironmentService, be registered in AddAgentExecutionStrategies, and be resolvable by name grok-cli with grok-build normalizing to grok-cli; AgentHelpOptions.DefaultExecutionStrategy defaults to grok-cli.
**Covered by:** FR: FR-MCP-HELP-011; TEST: TEST-MCP-HELP-008
**Status:** pending
Scope: layer-1+

## TR-MCP-HOSTILEREVIEW-001

**Hostile review request schema and storage** — Persist ReviewRequest and QueueItem in EF with three-provider migrations. Reject serialized request JSON over 1048576 bytes before insert. Enforce workspace isolation on submit.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-001; TEST: TEST-MCP-HOSTILEREVIEW-001
**Status:** pending
Scope: layer-1+

## TR-MCP-HOSTILEREVIEW-002

**Hostile review artifact resolver** — Resolver calls supported MCP APIs only. Diagnostics are stored on the request. Successful resolve stores identity and hash when available.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-002; TEST: TEST-MCP-HOSTILEREVIEW-002
**Status:** pending
Scope: layer-1+

## TR-MCP-HOSTILEREVIEW-003

**Hostile review execution capture** — ReviewExecution entity stores model string, effort, agent, plugin/source, template id/version, run id, start/end UTC. Token counts are optional and must not be invented.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-003; TEST: TEST-MCP-HOSTILEREVIEW-003
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Invalid HostileReviewWorker options fail host startup with an error that names HostileReviewWorker, on Linux and on Windows: RenewalInterval not less than LeaseDuration; MaximumActiveQueue, MaximumConcurrentReviews or MaximumAttempts below 1. AgentName, RequiredModel and RequiredEffort are not startup rules: none is pinned, and an unset one means the default (operator rule 2026-10-15; PLAN-SESSIONTRIAGEFIX-001 B9). The test host owns its logging providers, so an account that cannot write the Windows Application event log cannot turn the startup failure into a logging error (PLAN-SESSIONTRIAGEFIX-001 B4, triage-report-f4d47e1f, measured on PAYTON-LEGION2 2026-10-15; HostileReviewStartupAssertion_AcceptsWorkerFailureAndRejectsMaskedOrMissing, HostileReviewWorker_InvalidOptions_FailStartup, HostileReviewWorkerOptions_AnyAgentModelEffort_PassValidation).
- [ ] A reviewer output is accepted with the model and effort it reports, and the execution records them, when HostileReviewWorker RequiredModel and RequiredEffort are not configured; a configured RequiredModel or RequiredEffort is required of the output, and an output reporting a different value fails the request as ModelUnavailable (operator rule 2026-10-15; PLAN-SESSIONTRIAGEFIX-001 B9; tests: WorkerOptionsAndOutputRules_AcceptUnpinnedOrConfiguredAndRejectPinsOrIgnoredConfig, AcceptReviewerOutput_NoConfiguredModel_RecordsReportedModel, AcceptReviewerOutput_ConfiguredModelMismatch_FailsModelUnavailable). An output that omits the reviewer agent, model or effort records them empty (not reported), never a default model (operator decision 2026-10-15 "Empty defaults, in B9"; test: AcceptReviewerOutput_NoConfiguredModel_RecordsReportedModel).

## TR-MCP-HOSTILEREVIEW-004

**Findings taxonomy and request-quality schema** — Finding.category is an enum of the seven classes. Verdict is AGREE|DISAGREE|UNKNOWN. RequestQualityAssessment has five explicit numeric or ordinal fields.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-004; TEST: TEST-MCP-HOSTILEREVIEW-004
**Status:** pending
Scope: layer-1+

## TR-MCP-HOSTILEREVIEW-005

**Hostile review query indexes** — Query API applies AND across provided dimensions and returns empty list on no match, not an error.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-005; TEST: TEST-MCP-HOSTILEREVIEW-005
**Status:** pending
Scope: layer-1+

## TR-MCP-HOSTILEREVIEW-006

**No auto-mutation and surface parity** — No apply/repair endpoint is registered. REST, REPL, Director, and plugin skill expose the four verbs only and do not write product files from verdicts.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-006; TEST: TEST-MCP-HOSTILEREVIEW-006
**Status:** pending
Scope: layer-1+

## TR-MCP-HTTP-001

**MCP Streamable HTTP Endpoint** — `app.MapMcp("/mcp-transport")` maps the native MCP protocol handler at a path separate from the REST routes (`/mcpserver/*`). The endpoint requires an `Accept: application/json, text/event-stream` header and returns HTTP 406 without it. Uses `ModelContextProtocol.AspNetCore` 0.9.0-preview.1.
**Covered by:** FR: FR-MCP-016, FR-MCP-042
**Status:** pending
Scope: layer-1+

## TR-MCP-HTTP-002

**Detailed and Sanitized HTTP 500 Error Contract** — All HTTP endpoints that return status code 500 SHALL emit a structured response body containing a non-empty human-readable error description that identifies the failing operation and provides actionable diagnostic context for the caller. The contract SHALL be applied centrally so endpoint implementations do not duplicate exception-to-response formatting. Response detail SHALL be sanitized to avoid leaking secrets, tokens, connection strings, or raw stack traces, while server-side logs SHALL retain the full exception detail needed for root-cause analysis.

**Covered by:** `src/McpServer.Support.Mcp/Program.cs` `InvalidModelStateResponseFactory` (centralized RFC 7807 ProblemDetails emission for binder/validation failures, paired with `ValidationProblem` / `Problem` controller helpers for domain errors); `SessionLogController.SubmitAsync` and `GetByIdAsync` route through the centralized path. Sanitization defers to ASP.NET Core's default ProblemDetails serialization, which omits stack traces outside the Development environment.
**Status:** pending
Scope: layer-1+

## TR-MCP-HVPIPE-001

**Reviewer pool configuration and real-time management: implementation contract** — Implementation contract of FR-MCP-HVPIPE-001: HostileValidationOptions, HvReviewer, ListHvReviewersQuery, GetHvReviewerQuery, UpdateHvReviewerCommand, IHvReviewerAdapter. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-001; TEST: TEST-MCP-HVPIPE-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-001 (6 criteria), proven by TEST-MCP-HVPIPE-001.

## TR-MCP-HVPIPE-002

**Identity normalization, submitter and author exclusion, and round-robin assignment: implementation contract** — Implementation contract of FR-MCP-HVPIPE-002: AgentIdentity, HvReviewerEligibility, RoundRobinAssignment, HvWorkspaceState. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-002; TEST: TEST-MCP-HVPIPE-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-002 (9 criteria), proven by TEST-MCP-HVPIPE-002.

## TR-MCP-HVPIPE-003

**Reviewer readiness: NONCE, offline, and online: implementation contract** — Implementation contract of FR-MCP-HVPIPE-003: StartHvReviewerNonceCommand, ClaimHvRunCommand (probe), CloseHvRunOutputCommand, ReadinessProbeVerdict, HvProbeOutcome, HvWorkerHost (startup NONCE). Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-003; TEST: TEST-MCP-HVPIPE-003
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-003 (8 criteria), proven by TEST-MCP-HVPIPE-003.

## TR-MCP-HVPIPE-004

**Submission scopes and snapshot membership: implementation contract** — Implementation contract of FR-MCP-HVPIPE-004: SubmitHvRequestCommand, HvScopeResolution, TurnSnapshotBuilder, HvRequestTurn. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-004; TEST: TEST-MCP-HVPIPE-004
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-004 (6 criteria), proven by TEST-MCP-HVPIPE-004.

## TR-MCP-HVPIPE-005

**Admission eligibility and refusal with context: implementation contract** — Implementation contract of FR-MCP-HVPIPE-005: HvAdmissionEligibility, RoundRobinAssignment, SubmitHvFromPlugin. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-005; TEST: TEST-MCP-HVPIPE-005
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-005 (6 criteria), proven by TEST-MCP-HVPIPE-005.

## TR-MCP-HVPIPE-006

**Request context and requirement references: implementation contract** — Implementation contract of FR-MCP-HVPIPE-006: RequirementReferenceExtractor, HvRequestRequirementRef. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-006; TEST: TEST-MCP-HVPIPE-006
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-006 (4 criteria), proven by TEST-MCP-HVPIPE-006.

## TR-MCP-HVPIPE-007

**Durable scheduling: queue, leases, fences, and restart safety: implementation contract** — Implementation contract of FR-MCP-HVPIPE-007: ClaimHvRunCommand, RecordHvRunHeartbeatCommand, ExpireHvRunLeasesCommand, FencedLease, HvRetryDecision, HvWorkerHost. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-007; TEST: TEST-MCP-HVPIPE-007
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-007 (11 criteria), proven by TEST-MCP-HVPIPE-007.

## TR-MCP-HVPIPE-008

**Execution lifecycle: true live output, heartbeats, no time limit, cancellation: implementation contract** — Implementation contract of FR-MCP-HVPIPE-008: RunReviewerProcess, StreamReviewerOutput, IHvReviewerAdapter live lines and launch barrier, AppendHvRunOutputCommand, CancelHvRequestCommand. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-008; TEST: TEST-MCP-HVPIPE-008
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-008 (12 criteria), proven by TEST-MCP-HVPIPE-008.

## TR-MCP-HVPIPE-009

**Execution failures: incidents through triage, offline, retry bound: implementation contract** — Implementation contract of FR-MCP-HVPIPE-009: FailHvRunCommand, FileHvIncidentCommand, HvIncident, HvRetryDecision. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-009; TEST: TEST-MCP-HVPIPE-009
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-009 (8 criteria), proven by TEST-MCP-HVPIPE-009.

## TR-MCP-HVPIPE-010

**Reviewer output contract, per-turn and holistic evaluation: implementation contract** — Implementation contract of FR-MCP-HVPIPE-010: reviewer contract v1, IHvReviewerAdapter ExtractReviewerRecords, HvFinalVerification V-4 and V-8, HvTurnAssessment, HvFinding. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-010; TEST: TEST-MCP-HVPIPE-010
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-010 (10 criteria), proven by TEST-MCP-HVPIPE-010.

## TR-MCP-HVPIPE-011

**Evidence completeness, GZIP at rest, round-trip verification: implementation contract** — Implementation contract of FR-MCP-HVPIPE-011: GzipCodec, HvEvidence, HvRunOutputChunk, V-1. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-011; TEST: TEST-MCP-HVPIPE-011
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-011 (9 criteria), proven by TEST-MCP-HVPIPE-011.

## TR-MCP-HVPIPE-012

**Atomic finalization and turn finality: implementation contract** — Implementation contract of FR-MCP-HVPIPE-012: FinalizeHvRequestCommand, CommitFinalResult, HvTurnFreeze, A-07, A-08, A-11. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-012; TEST: TEST-MCP-HVPIPE-012
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-012 (9 criteria), proven by TEST-MCP-HVPIPE-012.

## TR-MCP-HVPIPE-013

**Self-review independence: implementation contract** — Implementation contract of FR-MCP-HVPIPE-013: HvAdmissionEligibility, TurnSnapshotBuilder, HvFinalVerification. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-013; TEST: TEST-MCP-HVPIPE-013
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-013 (3 criteria), proven by TEST-MCP-HVPIPE-013.

## TR-MCP-HVPIPE-014

**Verdict only: no TODO automation: implementation contract** — Implementation contract of FR-MCP-HVPIPE-014: FinalizeHvRequestCommand. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-014; TEST: TEST-MCP-HVPIPE-014
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-014 (2 criteria), proven by TEST-MCP-HVPIPE-014.

## TR-MCP-HVPIPE-015

**Results, evidence retrieval, and the standard result document: implementation contract** — Implementation contract of FR-MCP-HVPIPE-015: GetHvRequestQuery, QueryHvRequestsQuery, GetHvResultQuery, GetHvEvidenceQuery, HvResultDocument. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-015; TEST: TEST-MCP-HVPIPE-015
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-015 (4 criteria), proven by TEST-MCP-HVPIPE-015.

## TR-MCP-HVPIPE-016

**Live output stream, progress, and honest ETA: implementation contract** — Implementation contract of FR-MCP-HVPIPE-016: StreamHvRunOutputQuery, OrderedEventReplay. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-016; TEST: TEST-MCP-HVPIPE-016
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-016 (5 criteria), proven by TEST-MCP-HVPIPE-016.

## TR-MCP-HVPIPE-017

**Completion notification stream: implementation contract** — Implementation contract of FR-MCP-HVPIPE-017: NotificationOutbox, StreamHvNotificationsQuery, QueryHvNotificationsQuery, WatchHvNotifications. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-017; TEST: TEST-MCP-HVPIPE-017
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-017 (7 criteria), proven by TEST-MCP-HVPIPE-017.

## TR-MCP-HVPIPE-018

**Audit participation and hard stop: implementation contract** — Implementation contract of FR-MCP-HVPIPE-018: AuditLedgerGuard, the HV entities' `IsDeleted`. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-018; TEST: TEST-MCP-HVPIPE-018
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-018 (5 criteria), proven by TEST-MCP-HVPIPE-018.

## TR-MCP-HVPIPE-019

**Retention until explicit removal: implementation contract** — Implementation contract of FR-MCP-HVPIPE-019: RemoveHvEvidenceCommand. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-019; TEST: TEST-MCP-HVPIPE-019
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-019 (5 criteria), proven by TEST-MCP-HVPIPE-019.

## TR-MCP-HVPIPE-020

**Surface parity: REST, MCP, client, REPL, Director, and plugin: implementation contract** — Implementation contract of FR-MCP-HVPIPE-020: HostileValidationController, FwhMcpTools.HostileValidation, HostileValidationClient, HostileValidationWorkflow, HostileValidationDirectorCommands, the plugin verbs and watcher. Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-020; TEST: TEST-MCP-HVPIPE-020
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-020 (6 criteria), proven by TEST-MCP-HVPIPE-020.

## TR-MCP-HVPIPE-021

**Response grading and trust through finalization: implementation contract** — Implementation contract of FR-MCP-HVPIPE-021: HvRequestResponse, FinalizeHvRequestCommand, RecordModelGradeCommand (amended). Each named unit, policy, process, component, and entity is specified in docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md (sections 10 to 16).
**Covered by:** FR: FR-MCP-HVPIPE-021; TEST: TEST-MCP-HVPIPE-021
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The units, policies, processes, components, and entities named in this contract satisfy every acceptance criterion of FR-MCP-HVPIPE-021 (7 criteria), proven by TEST-MCP-HVPIPE-021.

## TR-MCP-HYGIENE-001

**Validation registry and result contract** — IWorkspaceValidationService returns a DTO with the required fields. Unknown rule codes emit diagnostics. Clean workspace returns zero findings.
**Covered by:** FR: FR-MCP-HYGIENE-001; TEST: TEST-MCP-HYGIENE-001
**Status:** pending
Scope: layer-1+

## TR-MCP-HYGIENE-002

**Traceability rule implementations** — Each rule emits a finding with record id and evidence. TEST exclusion only if a documented rule says so.
**Covered by:** FR: FR-MCP-HYGIENE-002; TEST: TEST-MCP-HYGIENE-002
**Status:** pending
Scope: layer-1+

## TR-MCP-HYGIENE-003

**TODO consistency rule implementations** — Each mismatch emits a finding with the TODO id and evidence of the contradicting fields.
**Covered by:** FR: FR-MCP-HYGIENE-003; TEST: TEST-MCP-HYGIENE-003
**Status:** pending
Scope: layer-1+

## TR-MCP-HYGIENE-004

**Stale-turn clock and triage domain enum** — IUtcClock is injectable. Threshold default 48h. Triage states come from the live domain model. Auth, cancellation, and pagination are enforced.
**Covered by:** FR: FR-MCP-HYGIENE-004; TEST: TEST-MCP-HYGIENE-004
**Status:** pending
Scope: layer-1+

## TR-MCP-HYGIENE-005

**Hygiene surface adapters and Director exit codes** — Four surfaces call the same service. Director maps Error/Critical to exit 1 and Warning-only to exit 0. JSON always written. No repair methods.
**Covered by:** FR: FR-MCP-HYGIENE-005; TEST: TEST-MCP-HYGIENE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-INGEST-001

Pluggable ingestors for repo/session/external/github/issues.
**Covered by:** FR: FR-MCP-003, FR-MCP-005, FR-MCP-006, FR-SUPPORT-010
**Status:** pending
Scope: layer-1+

## TR-MCP-INGEST-002

**Markdown Session Log Parser** — `MarkdownSessionLogParser.TryParse` recognizes Markdown files with a `# Session Log - {title}` or `# Copilot Session Log - {title}` header and parses them into `UnifiedSessionLogDto`. Extracts date, status, branch, model, duration, and known sections (Session Overview, Changes Made, Technical Requirements, Testing, etc.) as a summary entry. Individual `### Request` subsections are parsed as separate `UnifiedRequestEntryDto` entries. `NormalizeToStructuredText` produces a structured plain-text representation for FTS5 and vector embedding.
**Covered by:** FR: FR-MCP-006, FR-MCP-024
**Status:** pending
Scope: layer-1+

## TR-MCP-INGEST-003

**Direct Website URL Ingestion** — Add `WebsiteIngestor` with a dedicated `HttpClient` and bounded crawl behavior. Only `http`/`https` URLs are allowed. SSRF protections block localhost, loopback, RFC1918, and link-local targets (including DNS-resolved IPs). Redirects are bounded and re-validated at each hop. Per-request controls include max pages, max depth, max bytes per page, force refresh, and optional GraphRAG index trigger. Ingested pages upsert as `SourceType=external-web` with canonical URL source keys and deterministic document IDs.
**Covered by:** FR: FR-MCP-065
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] IngestWebsite_Success_UpsertsSourcesAndDeduplicatesBySourceKey: ingesting the same website source twice leaves exactly one live (not soft-deleted) context document for its source key in the workspace, carrying the newest content hash; the earlier version is soft-deleted (FR-MCP-105), not physically removed; the live-row assertion is proven first by LiveDocumentAssertion_AcceptsOneLiveAmongSoftDeletedAndRejectsTwoLiveOrNone over hand-built rows (PLAN-SESSIONTRIAGEFIX-001 B14, triage-report-61ef731c).

## TR-MCP-KEYSERVER-001

**Transaction Keyserver Service** — Provide shared keyserver services and a separate `McpServer.KeyServer` host with service-local SQLite storage, party/key registry, public-key descriptors, manifest sign/verify endpoints, replay nonce and sequence checks, expiry checks, signed manifest trace persistence/reporting, audit records, XMLDocs, typed client contracts, and health endpoint. Private signing material may be provisioned from file-backed startup configuration but must not be returned or logged.

**Covered by:** `McpServer.KeyServer`, `KeyServerController`, `KeyServerClient`, `HttpKeyServerManifestService`, `TransactionSecurityServices`, `TransactionSecurityOptions`, `TransactionSecurityServiceCollectionExtensions`, `TransactionSecurityStateStores`, `TransactionSecurityModels`, `TransactionSecurityControllerTests`, `TransactionSecurityClientTests`, `DurableTransactionSecurityStorageTests`, `SeparateTransactionServiceIntegrationTests`
**Status:** pending
Scope: layer-1+

## TR-MCP-LLMSTRATEGY-001

**Orchestration-to-strategy turn context seam** — Brain-slot factory SHALL select a completion strategy per slot provider kind so two roles can use two strategies in one process. Invocation and orchestration SHALL pass a BrainSlotTurnContext class instance by reference (original input, committed role evidence, session id, turn id, transaction id). OpenAI-compatible HTTP flattening SHALL include original input and turn identifiers in chat messages. Existing OpenAI, OpenAICompatible, and Cli providers remain flatteners. No new third-party agent-framework package is added for this TODO.
**Covered by:** FR: FR-MCP-LLMSTRATEGY-001; TEST: TEST-MCP-LLMSTRATEGY-001
**Status:** pending
Scope: layer-1+

## TR-MCP-LOG-001

**Exception Logging in Catch Blocks** *(DIRECTIVE)* - Every `catch` block that handles an exception must log the exception. Unexpected exceptions must use `LogError` with `ex.ToString()` as the message body. Expected/anticipated exceptions (e.g., `OperationCanceledException` on shutdown, `InvalidOperationException` for process-already-exited races, validation exceptions returned as HTTP 4xx) must use `LogWarning` with `ex.ToString()`. Catch blocks must not silently swallow exceptions with empty bodies or comments-only. The only permitted exception is re-throwing (`throw;`) without logging, where the exception will be logged by an outer handler.
**Status:** pending
Scope: layer-1+

## TR-MCP-LOG-002

**Identifier Naming Validation** — `TodoValidator` SHALL validate persisted TODO IDs against the canonical regex set `^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$` or `^ISSUE-\d+$` for create/update dependency paths across all configured TODO storage providers (`yaml` and `database` per TR-MCP-TODO-005). `ISSUE-NEW` SHALL remain a create-time alias handled before persistence, not a persisted TODO identifier. `SessionLogIdentifierValidator` SHALL validate session/request IDs using canonical timestamped patterns and enforce exact source-type prefix parity (`SessionId` starts with `{sourceType}-` or `{agent}-`). Invalid values return HTTP 400 at controller boundaries and `ArgumentException` for direct service invocation.

**Covered by:** `TodoValidator`, `TodoService`, `EfTodoService`, `TodoCreationService`, `SessionLogIdentifierValidator`, `SessionLogController`, `SessionLogService`
**Status:** pending
Scope: layer-1+

## TR-MCP-LOG-003

**Parseable Event Field-Cap Enforcement** — `ParseableEventFormatter` SHALL emit no more than 250 top-level fields for any individual Parseable event payload. The formatter SHALL always preserve the canonical Parseable metadata keys (`timestamp`, `level`, `message`, and `exception` when present), SHALL prevent user-supplied structured properties from overwriting those reserved keys, and SHALL drop excess non-reserved properties once the remaining field budget is exhausted. Property selection for retained non-reserved fields SHALL be deterministic so tests and operational analysis can reason about which fields survive truncation.

**Covered by:** `ParseableEventFormatter`, `ParseableBatchFormatter`
**Status:** pending
Scope: layer-1+

## TR-MCP-MARKER-004

**RemoveSingleFile deletes rather than archives; tombstone builder removed** — Implements FR-MCP-MARKER-004. MarkerFileService.RemoveSingleFile calls File.Delete on the marker path instead of File.Move to a BuildArchivedMarkerPath candidate, and BuildArchivedMarkerPath is deleted. The IOException and UnauthorizedAccessException handling and the existing log line are retained, with the message restated as a delete rather than an archive. Callers are unchanged: Program.cs registers MarkerFileService.RemoveMarker on ApplicationStopping, which is the path that produced one tombstone per graceful shutdown. The existing unit test MarkerFileServiceTests.RemoveMarker_ArchivesMarkerFileInsteadOfDeletingIt is retargeted to assert the inverse, since it encoded the defect as intended behavior and cites TR-MCP-DB-003, a requirement scoped to persistent MCP domain rows rather than filesystem artifacts. Existing tombstones in the workspace are moved to the Windows Recycle Bin rather than hard-deleted, so the cleanup itself stays reversible.
**Covered by:** FR: FR-MCP-MARKER-004; TEST: TEST-MCP-MARKER-004
**Status:** pending
Scope: layer-1+

## TR-MCP-MEMORY-001

**EF memory storage model** — Add `MemoryEntity` and `DbSet<MemoryEntity>` to the shared EF model. `Id` is unique across the memory store. `Scope` is required and constrained to `Global` or `Workspace`. `WorkspaceId` is null for Global memories and required for Workspace memories. Soft-delete metadata hides removed memories by default. Indexes exist for `Scope`, `WorkspaceId`, `Category`, and `UpdatedAtUtc`.
**Covered by:** FR: FR-MCP-MEMORY-001; TEST: TEST-MCP-MEMORY-001
**Status:** pending
Scope: layer-1+

## TR-MCP-MEMORY-002

**Provider memory migrations** — Add provider migrations for SQLite, SQL Server, and PostgreSQL. Each migration creates the memory table, unique ID constraint, scope and workspace indexes, category/update-time indexes, soft-delete metadata, and provider-appropriate constraints or service-level validation for Global rows with null `WorkspaceId` and Workspace rows with required `WorkspaceId`.
**Covered by:** FR: FR-MCP-MEMORY-002; TEST: TEST-MCP-MEMORY-002
**Status:** pending
Scope: layer-1+

## TR-MCP-MEMORY-003

**Memory service layer** — Add XML-documented `IMemoryService` and `MemoryService` contracts for add, list, update, and remove. The service validates IDs, categories, scopes, text, duplicate active IDs, and scope transitions; generates globally unique `MEMORY-{CATEGORY}-{NNN}` IDs per category; preserves raw text; increments `Version` on update; and soft-deletes on remove.
**Covered by:** FR: FR-MCP-MEMORY-003; TEST: TEST-MCP-MEMORY-003
**Status:** pending
Scope: layer-1+

## TR-MCP-MEMORY-004

**Memory REST and typed client contract** — Add `MemoryController`, `MemoryClient`, and client models under `/mcpserver/memory`. Create, list, update, and remove models include scope where applicable. `McpServerClient.Memory` exists and participates in `_allClients` propagation for workspace path, API key, bearer token, and port.
**Covered by:** FR: FR-MCP-MEMORY-004; TEST: TEST-MCP-MEMORY-004, TEST-MCP-MEMORY-006
**Status:** pending
Scope: layer-1+

## TR-MCP-MEMORY-005

**MCP stdio and REPL memory tools** — Add `memory_add`, `memory_list`, `memory_update`, and `memory_remove` to MCP stdio tools, and route `workflow.memory.add`, `workflow.memory.list`, `workflow.memory.update`, and `workflow.memory.remove` through typed REPL workflow code. Stdio tools require `workspacePath`, call `ApplyWorkspaceOverride`, and return compact JSON including scope for add, list, and update.
**Covered by:** FR: FR-MCP-MEMORY-005; TEST: TEST-MCP-MEMORY-005
**Status:** pending
Scope: layer-1+

## TR-MCP-MEMORY-006

**Memory schema and contract coverage** — Update canonical REPL YAML schema, plugin schema copies, and `docs/stdio-tool-contract.json` for all memory surfaces. Schemas validate required fields, `MEMORY-{CATEGORY}-{NNN}` IDs, `Global`/`Workspace`/`Effective` scope values where applicable, and invalid method/payload cases.
**Covered by:** FR: FR-MCP-MEMORY-006; TEST: TEST-MCP-MEMORY-008
**Status:** pending
Scope: layer-1+

## TR-MCP-MEMORY-007

**Scope-aware effective memory querying** — Add query/service helpers that resolve active memories for a workspace. Effective queries include active Global memories plus active memories for the current workspace, exclude deleted and other-workspace rows, and apply Global-first then Workspace ordering by ID. Controller, client, MCP stdio, REPL, YAML examples, marker injection, and plugin injection all use this ordering contract.
**Covered by:** FR: FR-MCP-MEMORY-006, FR-MCP-MEMORY-007; TEST: TEST-MCP-MEMORY-008, TEST-MCP-MEMORY-007, TEST-MCP-MEMORY-009
**Status:** pending
Scope: layer-1+

## TR-MCP-MEMORY-008

**Agent plugin memory integration** — Official McpServer plugins consume the shared memory contract and expose memory tools through their supported tool surfaces. Plugins with host request-boundary injection hooks render the exact `REQUIRED MEMORIES` block on supported user prompts. Plugins without such hooks document the limitation and expose explicit memory-list fallback behavior.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] In every official agent plugin repository whose memory skill wrapper (skills/memory/scripts/invoke.ps1) loads memory-context.ps1, that helper is present at a path the wrapper searches, so the wrapper dispatches memory_list to workflow.memory.list through the helper and exits 0 instead of failing with "memory-context.ps1 was not found" (proven through the helper's MCP_PLUGIN_REPL_LOG seam, without a server); a core sync (plugins/core/sync/sync-plugin-core.ps1) leaves that helper byte-identical (triage-report-834cec2e; PLAN-SESSIONTRIAGEFIX-001 F2; tests: MemoryDispatchAssertion_AcceptsHelperInSearchPathAndRejectsMissingMisplacedOrWrongDispatch and MemoryWrapper_ThisPlugin_DispatchesThroughItsHelper in each such repository's tests/MemoryWrapperHelper.Tests.ps1; HostHelperSyncAssertion_AcceptsKeptHelperAndRejectsDeletedRewrittenOrSkippedSync and CoreSync_KeepsPluginMemoryHelper in McpServer plugins/core/test-fixtures/pester/PluginCoreSyncHostFiles.Tests.ps1).
- [ ] Each official plugin's UserPromptSubmit hook entry (the hooks/scripts/user-prompt-submit.ps1 that plugins/core/hooks-templates/generate-wrappers.ps1 writes, lib/user-prompt-submit.ps1 for Codex) adds the plugin's required-memory block from its memory-context.ps1 (the descriptor's prefix and the effective memories, or the explicit empty fallback when there are none or the fetch fails) to the hook output's additionalContext whatever the turn-open status, and keeps the hook's other output fields and its exit code; a plugin without the helper gets the hook output unchanged; the memory fetch keeps to the hook's deadline (TR-MCP-REPL-012 AC-001): with less than one second left it is not started and the empty fallback is rendered, and otherwise its timeout is no longer than the whole seconds left (triage-report-f01b3544; PLAN-SESSIONTRIAGEFIX-001 F2; tests in plugins/core/test-fixtures/pester/UserPromptMemoryInjection.Tests.ps1: InjectedHookAssertion_AcceptsMergedBlockAndRejectsMissingReplacedOrLateFetch, Wrapper_UserPromptSubmit_MergesHelperBlockKeepingStatusAndExit, Wrapper_UserPromptSubmit_NoHelper_LeavesOutputUnchanged, Wrapper_UserPromptSubmit_PastDeadline_SkipsFetchWithEmptyFallback, Wrapper_CodexLibEntry_FindsHelperInHooksScripts; and each plugin's tests/MemoryInjection.Tests.ps1 user-prompt-submit cases).

## TR-MCP-MT-001

**WorkspaceContext Scoped Per-Request Service** — `WorkspaceContext` is a scoped service holding resolved workspace identity: `WorkspacePath`, `WorkspaceName`, `DataDirectory`, `TodoFilePath`, `SessionsPath`, `ExternalDocsPath`, `IsDefaultKey`, `IsResolved`. Populated by `WorkspaceResolutionMiddleware` before downstream services execute. Downstream services inject `WorkspaceContext` instead of reading `IConfiguration["Mcp:RepoRoot"]`.
**Covered by:** `WorkspaceContext`, `WorkspaceResolutionMiddleware`
**Status:** pending
Scope: layer-1+

## TR-MCP-MT-002

**WorkspaceResolutionMiddleware** — Runs before `WorkspaceAuthMiddleware` in the pipeline. Only activates for `/mcpserver/*` and `/mcp-transport` routes. Resolution chain: (1) `X-Workspace-Path` header validated against registered workspaces - returns 400 for unregistered paths; (2) API key reverse lookup via `WorkspaceTokenService.ResolveWorkspaceByToken()`; (3) `Mcp:RepoRoot` config fallback; (4) primary workspace from workspace list. Populates `WorkspaceContext` scoped service.
**Covered by:** `WorkspaceResolutionMiddleware`, `WorkspaceContext`, `WorkspaceTokenService`
**Status:** pending
Scope: layer-1+

## TR-MCP-MT-003

**EF Core Global Query Filter for WorkspaceId** — `McpDbContext` accepts optional `WorkspaceContext` to capture `_workspaceId` per-instance. `OnModelCreating` applies `.HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId)` on all 14 entity types. Empty `_workspaceId` disables filtering (backward compatible). `IgnoreQueryFilters()` escapes for cross-workspace admin queries. `WorkspaceId TEXT NOT NULL DEFAULT ''` column with indexes on all entity tables.
**Covered by:** `McpDbContext`, all entity types (`WorkspaceId` property)
**Status:** pending
Scope: layer-1+

## TR-MCP-MT-003A

`SessionLogService` injects an optional `WorkspaceContext` and stamps `WorkspaceId` on every entity it persists. When the context is null (ingestion / batch import path), the service skips stamping and relies on `McpDbContext.SaveChangesAsync` to auto-fill `WorkspaceId` for Added entities from the DbContext's resolved `_workspaceId`. This ensures POST/GET round-trips work under the same workspace context AND existing rows with empty WorkspaceId remain visible when no workspace header is set.
**Covered by:** FR: FR-SUPPORT-010A, FR-SUPPORT-010C, FR-SUPPORT-011; TEST: TEST-SUPPORT-010A-1, TEST-SUPPORT-010A-2, TEST-SUPPORT-010C-1, TEST-SUPPORT-010C-2, TEST-SUPPORT-010C-3
**Status:** pending
Scope: layer-1+

## TR-MCP-MT-004

**WorkspaceId stamping in SessionLogService** — SessionLogService injects an optional WorkspaceContext and stamps WorkspaceId on every entity it persists. When the context is null (ingestion/batch import path), the service skips stamping and relies on McpDbContext.SaveChangesAsync to auto-fill WorkspaceId for Added entities from the DbContext's resolved _workspaceId. This ensures POST/GET round-trips work under the same workspace context AND existing rows with empty WorkspaceId remain visible when no workspace header is set.
**Covered by:** FR: FR-SUPPORT-011; TEST: TEST-SUPPORT-010A-1, TEST-SUPPORT-010A-2
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-001

**Non-interactive PowerShell hosts for Nuke automation** — The root Nuke PowerShell bootstrap and any build-owned pwsh.exe or powershell.exe child process SHALL include -NoLogo, -NoProfile, and -NonInteractive unless an invocation is explicitly documented as interactive. Live deployment guidance SHALL use the same flags.
**Covered by:** FR: FR-MCP-110; TEST: TEST-MCP-148
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] build.ps1 re-launches itself through the PowerShell executable of the running installation, found in $PSHOME (pwsh, or pwsh.exe on Windows, for PowerShell 7; powershell.exe for Windows PowerShell), never through the current process's executable, so a remoting or embedded host such as wsmprovhost.exe under WinRM still runs the build with -NoLogo, -NoProfile and -NonInteractive; when that executable does not exist, build.ps1 stops with an error naming the path and a non-zero exit code instead of exiting 0 (operator 2026-10-15: "Don't triage, fix it"; Build.Tests BootstrapHostRule_ReferencePassesAndRejectsWrongRules, BootstrapHostRule_ShippedFunction_PinnedCases, BuildBootstrap_RelaunchesThroughResolvedHostNotProcessPath).

## TR-MCP-NUKE-002

**Nuke solution parameter and shared build project set** — The checked-in .nuke/parameters.json SHALL name a solution file that exists at the repository root, so Nuke can inject Build.Solution and every ./build.ps1 target that depends on Restore can run. The Nuke Compile target SHALL build, and the Restore target SHALL restore, one shared project set: every *.csproj under src/ and tests/ except tests/Build.Tests, as returned by Build.GetBuildProjectFiles. Restore SHALL restore the solution and then each project of that set that the solution does not list, so the --no-restore builds of Compile never fail with NETSDK1004.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] .nuke/parameters.json names a solution file that exists at the repository root. (evidence: tests/Build.Tests/NukeParametersTests.cs (NukeParameters_Solution_NamesExistingSolutionFile))
- [ ] Build.GetBuildProjectFiles returns every *.csproj under src/ and tests/ except tests/Build.Tests, ordered by path (OrdinalIgnoreCase), and Compile builds exactly that set. (evidence: tests/Build.Tests/BuildProjectSetTests.cs (GetBuildProjectFiles_CoversEverySourceAndTestProject_ExceptBuildTests, RestoreAndCompile_UseTheSharedProjectSet); tests/Build.Tests/BuildTargetTests.cs (BuildTargets_UseGitVersionForCompileAndReplTool))
- [ ] Restore restores the solution and then every project of the shared set that the solution does not list, so ./build.ps1 Compile succeeds with no NETSDK1004. (evidence: tests/Build.Tests/BuildProjectSetTests.cs (RestoreAndCompile_UseTheSharedProjectSet); ./build.ps1 Compile and ./build.ps1 Test receipts under docs/receipts/)

## TR-MCP-NUKE-005

**build.ps1 -WhatIf: full dry run with checks** — ./build.ps1 -WhatIf SHALL run the build tooling as a full dry run: it resolves every input, runs every check, reports what each side-effecting step would do without doing it, and exits non-zero on any violation. It is the acceptance test of FR-MCP-BUILD-001 and the place where operator rulings are enforced mechanically (the operator's rulings: Build.Tests runs are the Build.Tests infrastructure step, never G1, G2, or unit tests; Nuke behavior lives only in TR-MCP-NUKE-* TRs under FR-MCP-BUILD-001, never in an application FR or a TEST row; package-lock.json changes only through npm).
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] build.ps1 declares SupportsShouldProcess and passes -WhatIf through to the Nuke build; a -WhatIf run changes nothing: no file writes, restores, builds, tests, publishes, deploys, network writes, or git changes (git status and the working tree are identical afterwards).
- [ ] A -WhatIf run resolves the .NET SDK, the solution named by .nuke/parameters.json, the shared build project set (TR-MCP-NUKE-002), and the full target graph, and fails when any cannot be resolved (it would have caught BUG-BUILD-001 and BUG-BUILD-002).
- [ ] A -WhatIf run executes every check (ValidateConfig, ValidateTraceability, and the ruling checks) and fails on any finding, reporting each with its rule and evidence.
- [ ] Each side-effecting step reports the exact command or change it would make instead of performing it.
- [ ] Ruling checks are data, not one-off tests: each operator ruling is recorded once with its verbatim source and a checkable predicate (scope and condition), so a new ruling adds a check without new test code; the first rulings encoded are Build.Tests-as-infrastructure, Nuke-through-TR under FR-MCP-BUILD-001, and npm-owned lockfiles.

## TR-MCP-NUKE-006

**Sibling plugin parent resolution from the checkout or its worktrees** — Build.ResolveOfficialPluginSiblingParent, the default plugin parent for SyncAgentPlugins and the plugin-core checksum checks, SHALL return the directory that holds the mcpserver-*-plugin repositories when run from the main checkout or from a git worktree of it: a .worktrees/<name> worktree inside the checkout, or a worktree under a dot-folder of the checkout such as .claude/worktrees/<name> (Claude Code) or .mcpServer/worktrees/<name>.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] From <parent>/McpServer/.claude/worktrees/<name>, <parent>/McpServer/.mcpServer/worktrees/<name>, and <parent>/<repo>/.worktrees/<name>, ResolveOfficialPluginSiblingParent returns <parent>. (evidence: tests/Build.Tests/PluginParentResolutionTests.cs)
- [ ] The plugin-core checksum checks find the official plugin repositories when run from a .claude/worktrees worktree. (evidence: tests/Build.Tests/SyncAgentPluginsChecksumTests.cs; tests/Build.Tests/DocsSyncG8OverlayTests.cs)

## TR-MCP-NUKE-007

**InstallReplTool detects the installed REPL global tool by its package id** — The Nuke InstallReplTool target SHALL read the installed version of the QBrainAI.Repl global tool (command qbrain-ai-repl) from dotnet tool list --global by package id, case-insensitively, so it updates or skips correctly; the 1.x alias package SharpNinja.McpServer.Repl (command mcpserver-repl, TR-MCP-QBRAIN-003) is a different tool and never satisfies a QBrainAI.Repl lookup.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given dotnet tool list --global output with qbrainai.repl 6.1.1 (qbrain-ai-repl) and sharpninja.mcpserver.repl 1.4.41 (mcpserver-repl), GetInstalledGlobalToolVersion returns 6.1.1 for QBrainAI.Repl, 1.4.41 for SharpNinja.McpServer.Repl, and null for a missing tool. (evidence: tests/Build.Tests/BuildTargetTests.cs (InstallReplTool_ParsesInstalledGlobalToolVersion))

## TR-MCP-NUKE-008

**ValidateConfig reads the canonical QBrainAi root and the legacy Mcp root** — The Nuke ConfigValidator (./build.ps1 ValidateConfig, the G5 configuration check) SHALL parse instance configuration under the canonical QBrainAi: root of appsettings.yaml and under the legacy Mcp: root, as the server itself does through CanonicalSectionProjectionProvider, and SHALL report a missing root naming both.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ParseInstances returns the instances under a QBrainAi: root and under a legacy Mcp: root, and null when neither root is present. (evidence: tests/Build.Tests/ConfigValidatorTests.cs)
- [ ] The shipped validator parses and validates the live src/QBrainAi.Support.Mcp/appsettings.yaml without errors, and ./build.ps1 ValidateConfig succeeds. (evidence: tests/Build.Tests/DocsSyncG8OverlayTests.cs (G8_ValidateConfig_ShippedValidator_PassesOnLiveAppsettings); ./build.ps1 ValidateConfig)
- [ ] When both roots are present, the canonical QBrainAi: root wins per key whatever the document order, keys and instances only under the legacy Mcp: root are kept, and a sibling key after one root's Instances block does not stop parsing of the other root, matching CanonicalSectionProjectionProvider within one file layer. (evidence: tests/Build.Tests/ConfigValidatorTests.cs (ParseInstances_BothRoots_CanonicalWinsPerKeyInEitherOrder))

## TR-MCP-NUKE-009

**Build infrastructure tests bound every child process and its redirected output** — Every child process that the Build.Tests infrastructure suite starts with redirected standard output and standard error SHALL run through one shared helper that bounds the wait for the child to exit, bounds the wait for its redirected output to close after it exits, drains both streams concurrently, kills the child's process tree when the exit timeout expires or the caller cancels, and starts dotnet children with --disable-build-servers, so that no child, descendant, or persistent build server can stall the suite.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] When the child exits while a descendant still holds its redirected output open, the helper fails within the drain period, naming the command and stating that the captured output may be incomplete, instead of waiting for the descendant. (evidence: tests/Build.Tests/BoundedProcessTests.cs)
- [ ] When the child runs past its exit timeout, the helper kills the child's process tree and fails with a timeout naming the command. (evidence: tests/Build.Tests/BoundedProcessTests.cs)
- [ ] When the caller cancels, the helper kills the child's process tree and stops waiting. (evidence: tests/Build.Tests/BoundedProcessTests.cs)
- [ ] The helper drains standard output and standard error concurrently, so a child that fills one pipe's buffer before writing to the other completes. (evidence: tests/Build.Tests/BoundedProcessTests.cs)
- [ ] dotnet children started by Build.Tests pass --disable-build-servers. (evidence: tests/Build.Tests/BoundedProcessTests.cs)
- [ ] No Build.Tests source other than the helper reads a child's redirected output or waits for a child to exit. (evidence: tests/Build.Tests/BoundedProcessTests.cs)

## TR-MCP-NUKE-010

**Build infrastructure tests remove their temporary build artifacts on every OS** — A Build.Tests case that builds or publishes into a temporary artifacts root SHALL remove that root when it finishes, on Linux, macOS, and Windows. On Windows the removal SHALL NOT block the test host, because an antivirus scan of a trimmed self-contained publish tree can hold a recursive delete for tens of minutes.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] TryDeleteArtifacts removes a populated artifacts root on Linux, macOS, and Windows; on Windows it keeps the non-blocking detached delete. (evidence: tests/Build.Tests/TrimArtifactCleanupTests.cs)

## TR-MCP-NUKE-011

**Targeted test runs through Nuke: Test accepts --test-projects and --test-filter** — Targeted test runs through Nuke: Test accepts --test-projects and --test-filter, the G1 route and G3's route to the integration and validation projects. No Nuke target runs QBrainAi.Support.Mcp.IntegrationTests, QBrainAi.SessionLog.Validation, QBrainAi.Repl.IntegrationTests, or QBrainAi.Acid.IntegrationTests today, because Test leaves out every *.IntegrationTests and *.Validation project and filters out Category=Integration (build/Build.Test.cs:16-24,50; build/Build.TestDependencies.cs:60-61), and MigrationIntegrationTests runs only QBrainAi.Support.Mcp.Tests and Build.Tests (:70-74). Integration tests run only against temporary instances (operator, 2026-10-13: "Integration tests should always be using a temporary instance").
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] --test-projects runs only the named projects, and a name may select a test or validation project that Test leaves out by default (NukeTestTestProjectsRunsOnlyNamed, NukeTestTestProjectsSelectsDefaultExcludedIntegrationAndValidationProjects).
- [ ] A name that matches no test or validation project of the solution fails before any test runs, and so does a name that selects Build.Tests, which no Nuke target runs (NukeTestUnknownProjectFailsBeforeRun, NukeTestTestProjectsBuildTestsRefusedBeforeRun).
- [ ] --test-filter is appended as (<category filter>)&(<filter>), and for a project that Test leaves out by default the category filter drops its Category!=Integration term, the trait the integration projects' classes carry (NukeTestTestFilterCombinedWithCategory, NukeTestDefaultExcludedProjectCategoryFilterDropsIntegrationTerm).
- [ ] Without the parameters the target is unchanged (NukeTestNoParametersUnchanged, with the default-selection pins of TestTarget_ExcludesPluginIntegrationFromUnitInventory kept green).

## TR-MCP-NUKE-012

**Plugin core suite through Nuke (G4): TestPluginCore --pester-paths** — Plugin core suite through Nuke (G4): TestPluginCore --pester-paths <paths> runs the PowerShell plugin core's Pester suites, the only plugin test runner under the operator's PowerShell-only ruling of 2026-10-11, as a Nuke target natively on Windows and Linux, so no gate calls Invoke-Pester directly.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Runs Pester 5 with -CI on the named plugins/core/test-fixtures/pester files (TestPluginCoreRunsPesterCiOnNamedPaths).
- [ ] Fails on any failure (TestPluginCorePesterFailureFailsTarget).
- [ ] Fails on any skip (TestPluginCorePesterSkipFailsTarget).
- [ ] Fails on a missing path (TestPluginCoreMissingPathFailsTarget).
- [ ] Runs natively on Windows and on Linux (TestPluginCoreRunsNatively, run on both).
- [ ] In a fresh worktree on Linux and on Windows, ./build.ps1 TestPluginCore passes with no failed and no skipped test; no plugin-core test reads a marker at the repository root, reads a pre-built plugins/core/.staged-plugin, finds sibling plugin repositories other than by the TR-MCP-NUKE-006 walk-up, runs a Windows-only command outside an $IsWindows branch, joins PATH with a hard-coded separator, or uses a bash script as a fake (PLAN-SESSIONTRIAGEFIX-001 D7 to D9, triage-report-b9a8ab7e, -30a24728, -60ac3433; IsolationCheck_FlagsEachViolationAndPassesCleanText, IsolationCheck_PluginCoreSuiteIsClean, and the whole-suite TestPluginCore run in a fresh worktree).

## TR-MCP-NUKE-015

**UpdateService: cross-platform service update preserving live state** — The Nuke UpdateService target detects Windows or Linux and updates the selected service while preserving the live configuration and file data strategy; Linux updates an existing systemd service without rewriting its operator configuration. Preserve Windows behavior. Linux uses bounded shell-free systemctl/tar operations, installed-unit preflight, platform RID/apphost selection, shared preservation paths, 0700 backup directories/0600 archives, ownership/mode/ACL/xattr preservation, failure restoration and same health checks. No SQL database export or automatic binary rollback claim.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] UpdateService selects Windows or Linux at runtime; Windows defaults/launcher remain compatible; Linux x64/arm64 use extensionless apphost; unsupported hosts fail before changes.
- [ ] Both platforms preserve live appsettings.yaml and configured DataFolder with legacy fallback. Linux also preserves unit/drop-ins/environment files and Unix ownership/modes/ACLs/xattrs through a private retained archive.
- [ ] Linux validates installed service identity and stage before stop; backup precedes replacement; restore precedes start; service executable and server/workspace health must pass. Failures preserve recovery artifacts and never report success.
- [ ] Mocks-first tests and full applicable unit suites pass with zero failures/skips; independent gates pass; existing local Linux service update preserves configuration/data and trusted workspace behavior. Evidence: Build.Tests ServiceUpdateTests, run directly as infrastructure (no TEST record).
- [ ] Every Build.Tests ServiceUpdateTests case runs and passes on Linux and on Windows with no skip. A case whose Linux check needs a Linux-only facility (a FIFO, Unix file modes, GNU tar ACL and xattr preservation, unlinking a directory symbolic link with File.Delete) asserts on Windows the outcome the helper produces there for the same input (PLAN-SESSIONTRIAGEFIX-001 C2, triage-report-cdf116ad, as BUG-BASELINE-004 did for Windows-only tests; StatDouble_ReportsRegisteredSpecialFileAsFifo, and the Windows cases of LinuxPreflight_RejectsSpecialStageFiles, LinuxPreflight_RejectsSpecialApphostAndTextInputs, LinuxArchive_UsesPrivateMetadataPreservingCommands, LinuxReplacement_DoesNotFollowSymlinks and LinuxPreflight_RejectsSymlinkRootAndNonexecutableStage).

## TR-MCP-NUKE-016

**InstallOllama provisions Ollama on Windows and Linux** — The Nuke InstallOllama target provisions the local Ollama service the QuadBrain Ollama integration tests use (FR-MCP-QBOLLAMA-002), on Windows and on Linux, idempotently: when a server already answers on localhost:11434 with the required model it changes nothing; otherwise it uses an installed ollama executable (on PATH, or on Windows in Ollama's install folder LocalAppData/Programs/Ollama) or a previous portable install under the test-tools folder, else downloads that operating system's portable release into the test-tools folder (Windows: the windows-amd64 zip; Linux: the linux-amd64 archive), starts the server detached and waits until it answers, and pulls the required model (default gemma4:e4b; QBRAINAI_QUADBRAIN_OLLAMA_MODEL or MCP_QUADBRAIN_OLLAMA_MODEL overrides) when it is absent.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The target's plan is chosen per operating system: a server that already answers with the required model needs no step; otherwise an installed ollama (on PATH, or on Windows LocalAppData/Programs/Ollama/ollama.exe), else a previous portable install under the test-tools folder (ollama.exe on Windows, bin/ollama on Linux), else a download of that operating system's portable release; the plan then starts the server when it is not answering and pulls the model when it is absent (Build.Tests: OllamaInstallPlanAssertion_AcceptsPerOsPlanAndRejectsWindowsOnlyOrMissingStep, then InstallOllama_Plan_PerOperatingSystem_UsesTheShippedPlanner).
- [ ] Re-running the target when the server already answers with the required model changes nothing (Build.Tests: InstallOllama_Plan_ServerAnswersWithModel_ChangesNothing over the shipped planner).
- [ ] On LAB-OMARCHY the target leaves a portable Ollama that OllamaServerFixture's resolver finds (TR-MCP-QBOLLAMA-002) and the required model installed, and QuadBrainOllamaEndpointIntegrationTests run (TEST-MCP-QBOLLAMA-001).

## TR-MCP-NUKE-017

**ArchitectureDiagrams: one C4 definition for every documentation diagram** — A Nuke target validates docs/architecture/c4/workspace.dsl, renders every view to docs/architecture/c4/rendered/<viewKey>.svg through the Structurizr static export and headless Chromium, and fails when any documentation diagram does not come from that one workspace.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Validates docs/architecture/c4/workspace.dsl with structurizr/cli:2025.11.09 (pinned digest recorded in the target; never :latest) and fails on a non-zero exit or any inspection error.
- [ ] Renders every workspace view to docs/architecture/c4/rendered/<viewKey>.svg through the Structurizr static export and headless Chromium, light theme, help dialog dismissed, content fitted, no render-time stamp; two runs on the same workspace produce byte-identical files. In each rendered SVG the generated element ids (JointJS j_N, v-N and v_N) are renumbered from 1 per prefix in document order, every reference to a renumbered id (url(#id), href="#id") follows it, and no other byte changes, so the bytes of a render depend only on its own view.
- [ ] Fails when a tracked file outside docs/receipts contains a Mermaid fence or has the .mmd extension.
- [ ] Fails when a markdown image under docs points into docs/architecture/c4/rendered with a view key the workspace does not define, and when a rendered file has no view (orphan).
- [ ] Fails when no single scale puts every visible text size of a rendered view within 10.67 to 21.33 px (8 to 16 pt at 96 dpi, a spread of at most 2:1, so element names reach 16 pt while the element metadata line stays visible) and fits the render on one letter page (1056 x 816 landscape or 816 x 1056 portrait); a view that cannot fit is split per functional group.
- [ ] Honors the Nuke parameter WhatIf (--what-if, the name PLAN-BUILDWHATIF-001 gives it): reports what it would validate, render and check, runs the read-only checks, and writes nothing. The build.ps1 -WhatIf pass-through stays with TR-MCP-NUKE-005.

## TR-MCP-NUKE-018

**InstallTestDependencies provisions provider test servers on Linux** — On Linux, InstallTestDependencies supplies the PostgreSQL and SQL Server endpoints of the Category=Integration provider tests from the host test-database env file and the dedicated qbrain-test containers (operator decision 2026-10-11, BUG-BASELINE-005), and exports them to the test processes the build starts; Windows provisioning is unchanged.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Connection-string values never appear in logs, exception messages, or committed files; only variable names, file paths, container names and host:port endpoints are reported.

## TR-MCP-NUKE-019

**MigrationIntegrationTests selects host-specific integration cases by the Platform trait** — MigrationIntegrationTests runs the Category=Integration cases that belong to the host it runs on: tests tagged Platform=Linux run only on Linux hosts, tests tagged Platform=Windows only on Windows hosts, and untagged integration tests on both. Unit tests contain no platform-specific cases (operator 2026-10-19: "There should be ZERO platform specific UNIT tests.  If it depends on the environment, it's an integration test."). Provisioning stays as TR-MCP-NUKE-018 and the Windows LocalDB and PostgreSQL binaries define it.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] On a Windows host the target runs Category=Integration cases except those tagged Platform=Linux; on a Linux host it runs them except those tagged Platform=Windows; untagged integration cases run on both.
- [ ] MigrationIntegrationTests runs the Category=Integration cases of QBrainAi.Support.Mcp.Tests, QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests, QBrainAi.QBAgent.Tests, Build.Tests and QBrainAi.Repl.IntegrationTests and, from QBrainAi.Support.Mcp.IntegrationTests, only the Category=Integration cases of the classes RequirementScopeLayerReplIntegrationTests and QuadBrainOllamaEndpointIntegrationTests, selected by class name, each project with its own TRX under the run's --test-run-id. The other cases of QBrainAi.Support.Mcp.IntegrationTests, and QBrainAi.Acid.IntegrationTests and QBrainAi.TransactionSecurity.IntegrationTests, are not run by the lane until PLAN-SESSIONTRIAGEFIX-001 merges and their failing cases are resolved (TODO IMPL-TXNINTEGRATION-001); the .Validation projects, the AI review projects and QBrainAi.PluginIntegration.Tests, which has its own target, are not lane projects (operator 2026-10-29: "Run the 2 classes, AC first (Recommended)"; hostile validation finding R8B-F2). Every case tagged Platform=Linux or Platform=Windows runs and passes in the lane of a host of that platform, and a lane project whose filter selects no case fails the lane instead of passing empty (operator 2026-10-29: "Lane ACs (Recommended)"; holistic review findings PLAN-1 and BLD-1).
- [ ] The run log names the selection filter (filter text only). The host filter, each lane project's filter and each lane project's TRX name are outputs of one lane composition that the target uses and logs as given, proved by Build.Tests cases over that composition (operator 2026-10-29: "Lane ACs (Recommended)"; holistic review finding BLD-3; operator 2026-10-30: "F1: AC-3 wording (Recommended)").
- [ ] Every integration case MigrationIntegrationTests runs that needs a QBrain.AI instance uses a temporary instance it owns, whether a server process on a free port, an in-process host or a stdio host, with a test-owned workspace, marker and database, never the live server, its marker or its API key. A case that needs a server asserts on that instance's answers and fails when it cannot reach it; it never catches the failure and passes. A database selection outside the owned root is refused before the instance reads or writes that database. The rules for the instance's environment, database selection and consumers are FR-MCP-TESTRESOURCE-001 ac-4 (operator 2026-10-29: "Strengthen ACs, fix both (Recommended)", "Run the 2 classes, AC first (Recommended)" and "Lane ACs (Recommended)"; operator 2026-10-30: "FR-MCP-TESTRESOURCE-001 (Recommended)"; hostile validation findings R8B-F2 and R8B-F3; holistic review findings PLAN-6, PLAN-7, RINT-1, RINT-2 and RINT-3).

## TR-MCP-OPS-001

Operational scripts for startup, health checks, packaging, config validation, and migration.
**Covered by:** FR: FR-MCP-008
**Status:** pending
Scope: layer-1+

## TR-MCP-PATHNORM-001

**Forward-slash storage keys, rekey maintenance command and startup guard** — WorkspaceIdentityPath.NormalizeLexicalPath emits '/' for Windows paths (UNC keeps leading '//'; \\?\ device prefixes stripped to drive or // form); BuildStorageKey becomes windows:C:/WORKSPACES/EXAMPLE style. IsRemoteWindowsPath, IsNormalizedPathWithinRoot and NormalizeLocalAdministrativeShareAlias accept both separators. A ToNativePath helper produces backslash paths only for Win32 CreateFile/GetFinalPathNameByHandle, \\?\ long-path prefixes and cmd.exe arguments. Persisted data is rewritten by an explicit maintenance command --workspace-path-rekey (decision D5, 2026-10-07) modelled on McpDatabaseEncryptionTransitionCommand: required backup unless --dry-run, report with old->new mapping and before/after counts; one transaction per database with foreign keys deferred or suspended and fully re-validated (decision D6: SQLite defer_foreign_keys, SQL Server NOCHECK then WITH CHECK CHECK, PostgreSQL DEFERRABLE + SET CONSTRAINTS ALL DEFERRED); set-based UPDATE per workspace column from a mapping table limited to Windows-shaped values; C# recomputation of hashed values (federation GlobalWorkspaceId/CanonicalWorkspaceId and hub hash-keyed Workspaces rows, TriageGroups GroupKey/GroupId with repointed children, HandoffIngestionRuns ReplayIdentity); FileGitHubWorkspaceTokenStore keys rewritten. No legacy dual-key lookup (decision D7): a startup guard refuses to start while any Windows-shaped workspace id still contains a backslash. Enrolled proxies are re-enrolled once after deploy (the Windows proxy in the original deployment). Plan: docs/plans/2026-10-07-federation-fixes.md section 4c.
**Covered by:** FR: FR-MCP-PATHNORM-001; TEST: TEST-MCP-PATHNORM-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Reuse P1.4 Windows lexical normalization, preserve case, and reject distinct old keys converging before any database mutation.
- [ ] Apply requires successful verified backup; all database changes and FK validation occur in one transaction; repeat apply is a no-op.
- [ ] Dry-run inventory and planning run only on a database copy; report mappings, table and workspace counts, and retained manual triage hashes without secrets.
- [ ] HTTP and STDIO startup refuse legacy Windows workspace ids and point to --workspace-path-rekey.
- [ ] Recompute only derivable triage, handoff, and federation identities; repoint children; preserve manual triage hashes and rewrite token-store keys with file backup.

## TR-MCP-PERSIST-001

**Plugin appendDialog uses incremental dialog POST** — Invoke-WorkflowAppendDialog SHALL call SessionLogClient.AppendDialogAsync (or equivalent POST .../dialog) when a current turn exists. Failsafe for incremental dialog SHALL replay the dialog POST, not a full-session SubmitAsync upsert.
Acceptance Criteria:
- Pester proves appendDialog does not invoke client.SessionLog.SubmitAsync for an existing turn.
- Missing turn is not-found, retryable false.
**Covered by:** FR: FR-MCP-170; TEST: TEST-MCP-195, TEST-MCP-196
**Status:** pending
Scope: layer-1+

## TR-MCP-PERSIST-002

**PersistTurn maps 503 to timeout degrade-queue** — Invoke-ReplPersistTurn SHALL treat HTTP 503 and payload code backend_unavailable the same as timeout|timed out|command_timeout: return false, set LastReplPersistenceDetails degraded/queued, retain failsafe, do not throw.
Acceptance Criteria:
- Pester PersistTurn 503 does not throw and leaves failsafe on disk.
- current-turn.yaml remains in_progress.
**Covered by:** FR: FR-MCP-171; TEST: TEST-MCP-195
**Status:** pending
Scope: layer-1+

## TR-MCP-PERSIST-003

**Failsafe drain inspects errors without Write-Error Stop skip** — Invoke-ReplFailsafeDrain SHALL inspect Invoke-ReplRaw failures without Write-Error under ErrorActionPreference Stop skipping Test-ReplFailsafeBackendUnreachable. Drain of SessionLog.SubmitAsync SHALL abort on timeout/503 without incrementing drainAttempts and without latching ReplFailsafeDrainCompleted. Drain MUST NOT block the successful caller for the full 30s SubmitAsync timeout (bounded drain timeout or drain after return).
Acceptance Criteria:
- Pester: timeout/503 abort, no drain failed stderr, later replay works.
- getFr returns before 30s when a queued session_submit 503s.
**Covered by:** FR: FR-MCP-172; TEST: TEST-MCP-195
**Status:** pending
Scope: layer-1+

## TR-MCP-PERSIST-004

**SQLITE_BUSY under budget is persist contention not storage-down** — SessionLog SaveChanges SQLITE_BUSY or lock wait inside the storage command budget SHALL NOT be classified as backend_unavailable/storage-down when subsequent TODO/requirements reads succeed. Prefer SQLite busy-timeout/retry. GET /health remains process liveness plus nonce echo. Do not change /health liveness semantics.
Acceptance Criteria:
- Concurrent TODO query during SubmitAsync does not yield backend_unavailable when the DB file is valid.
- /health?nonce= still echoes the nonce.
**Covered by:** FR: FR-MCP-170; TEST: TEST-MCP-195, TEST-MCP-196
**Status:** pending
Scope: layer-1+

## TR-MCP-PLAN-001

**Safe session wrap-up and deploy sequencing** — Wrap-up plans must inventory dirty state across affected workspaces, preserve unrelated work, require Nuke or repo-supported deployment paths, and block publish or service updates until the intended slice is cleanly isolated and validated with zero failures and zero skips.
**Covered by:** FR: FR-MCP-107; TEST: TEST-MCP-143
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGIN-008

**Codex requirements update command fallback parity** — The Codex plugin requirements fallback must pass updateFr, updateTr, and updateTest payloads to the REPL/client without dropping fields or invoking unsupported command aliases.
**Covered by:** FR: FR-MCP-106; TEST: TEST-MCP-142
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGIN-009

**Session and compaction hook output contract** — Bash-family MCP plugins SHALL implement SessionStart, SessionEnd, PreCompact, and PostCompact scripts so that status-only execution paths return {}. Hook-specific output may be emitted only for event schemas that support it, and every hookSpecificOutput payload SHALL include the matching hookEventName. PostCompact history reload side effects SHALL NOT attempt context injection via additionalContext.
**Covered by:** FR: FR-MCP-115; TEST: TEST-MCP-155
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGIN-010

**PowerShell wrapper process timeout control** — Invoke-CodexMcpPlugin.ps1 SHALL expose a TimeoutSeconds parameter, wait only up to that bound for plugin helper processes, terminate timed-out processes, and avoid stdout/stderr read ordering that can deadlock the wrapper.
**Covered by:** FR: FR-MCP-117; TEST: TEST-MCP-157
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGIN-011

**Stop gate no-ops phantom turns and counts commits** — The Claude plugin Stop gate (Close-PluginTurnIfNeeded) SHALL NOT block session end for phantom/empty turns and SHALL count commits toward audit completeness. Acceptance Criteria: (AC1) an in_progress turn whose prompt is a system-event wrapper (first line matching <task-notification>, <user_query>, <command-name|message|args>, <local-command-*>) OR that has zero recorded work (auditActions+auditFiles+auditDialog+auditDecisions+auditCommits == 0 AND codeEdits == 0) is no-op'd (empty {} result), not blocked, and does not invoke completeTurn; (AC2) a completed code-edit turn whose only audit signal is auditCommits>0 is not blocked with 'audit is incomplete' (auditCommits is included in both hasAuditSchema and the audit-total sum). Origin: BUG-TRIAGE-071 (auditCommits omitted from the audit sum), BUG-TRIAGE-082/083 (Stop gate blocked on empty/task-notification turns). Applied to plugins/core/lib-ps/plugin-hook.ps1 (canonical) and mcpserver-claude-code-plugin/lib. Validated by TEST-MCP-PLUGIN-011.
**Covered by:** FR: FR-MCP-SESSIONLIFE-005; TEST: TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004, TEST-MCP-PLUGIN-011
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGIN-012

**Session rotation keeps the current turn fresh in its own session** — When the plugin session id rotates (Start-PluginSession mints a new sessionId) while a current-turn cache still carries the old sessionId, Assert-ReplCurrentTurnFresh SHALL treat the turn as fresh, rather than hard-rejecting every subsequent completeTurn/appendActions as stale, and SHALL NOT rewrite the turn's cached session id: the turn completes in the session it was opened in (FR-MCP-SESSIONLIFE-002: "Assert-ReplCurrentTurnFresh does not rewrite a cached session id"); only an empty turn session id takes the active one. Marker drift (wrong-workspace) SHALL still be rejected separately. Acceptance Criteria: (AC1) with session-state.yaml sessionId=B and current-turn.yaml sessionId=A (same/absent marker), Assert-ReplCurrentTurnFresh returns true and leaves current-turn.yaml sessionId at A, and with an empty current-turn.yaml sessionId it returns true and writes B (amended 2026-10-15 by operator decision "Fix the stale tests now", PLAN-SESSIONTRIAGEFIX-001, triage-report-d29a6e26: the earlier rewrite to B contradicted FR-MCP-SESSIONLIFE-002); (AC2) the failsafe persist path (ReplFailsafe) is unaffected. Origin: BUG-TRIAGE-071/075 (Start-PluginSession rotated the sessionId without superseding current-turn.yaml, so the sessionId-mismatch branch rejected completeTurn indefinitely). Applied to plugins/core/lib-ps/repl-invoke.ps1 (canonical) and mcpserver-claude-code-plugin/lib. Validated by TEST-MCP-PLUGIN-012.
**Covered by:** FR: FR-MCP-REPL-008; TEST: TEST-MCP-REPL-018, TEST-MCP-PLUGIN-012, TEST-MCP-PLUGIN-013
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A compaction leaves exactly one new session-log session: the PostCompact hook starts no session, leaves session-state.yaml unchanged and emits only {} (the status-only output contract), and the SessionStart hook that follows a compaction (source compact) starts the one session, so no empty twin session is minted (PLAN-SESSIONTRIAGEFIX-001 D2, triage-report-d9bd6fc3; PostCompactAssertion_AcceptsNoSessionAndRejectsMintOrOutput, PostCompact_StartsNoSessionAndKeepsSessionState).

## TR-MCP-PLUGIN-013

**UserPromptSubmit hook opens exactly one turn per prompt** — The UserPromptSubmit plugin hook SHALL open exactly one session-log turn per user prompt even when both the plugin hooks.json registration and the settings.json bridge deliver the prompt; a repeated identical prompt reuses the already-open turn. AC1: the second delivery of the same prompt returns turn-already-open and does not call beginTurn. AC2: a differing prompt opens a new turn. Guards BUG-TRIAGE-077 via TEST-MCP-PLUGIN-013 (HookTurnDedupe.Tests.ps1).
**Covered by:** FR: FR-MCP-REPL-008; TEST: TEST-MCP-REPL-018, TEST-MCP-PLUGIN-012, TEST-MCP-PLUGIN-013
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A UserPromptSubmit prompt made only of host events, Claude Code task notifications (<task-notification>...</task-notification>) and subagent hand-backs (<agent-message ...>...</agent-message>), is not an operator request: the hook opens no session-log turn, supersedes nothing, leaves current-turn.yaml unchanged and reports status system-event; a prompt with any other text, including an operator message after such a block, opens its turn as before (PLAN-SESSIONTRIAGEFIX-001 D4, triage-report-5c7278cd; HostEventAssertion_AcceptsEventsOnlyAndRejectsRequestText, HostEvent_Classifier_PinnedShapes, HookEventAssertion_AcceptsNoTurnAndRejectsOpenOrSupersede, UserPromptSubmit_HostEvent_OpensNoTurnAndKeepsCurrentTurn, UserPromptSubmit_RequestAfterEvent_OpensTurn).
- [ ] The Claude plugin's hook validator (skills/claude-hook-validation/scripts/validate-claude-mcp-hooks.ps1) and hook installer (skills/claude-hook-wiring/scripts/install-claude-mcp-hooks.ps1) recognize the plugin as enabled from the enabledPlugins object Claude Code writes to settings.json, a key "<plugin name>@<marketplace>" whose value is true, the plugin name being the one in .claude-plugin/plugin.json, whether or not CLAUDE_PLUGIN_ROOT is set; from enabledPlugins, a false value, another plugin's key (including one whose name only contains this plugin's name) or no entry is not recognized, the list form stays accepted, and the existing CLAUDE_PLUGIN_ROOT fallback is unchanged. With the plugin enabled and its hooks.json registering the UserPromptSubmit, Stop and PostToolUse hooks, the validator reports valid and the installer adds no settings-level bridge hook, so a repair cannot create a second delivery of the prompt (PLAN-SESSIONTRIAGEFIX-001 D5, triage-report-bd1f4386; EnabledPluginAssertion_AcceptsObjectFormAndRejectsCastOrBooleanBlindRules, EnabledPlugin_BothScripts_PinnedSettingsShapes, Validator_ObjectFormWithoutPluginRoot_ReportsValid, Validator_DisabledObjectForm_ReportsMissing, Installer_ObjectFormWithoutPluginRoot_AddsNoBridgeHooks).
- [ ] A UserPromptSubmit prompt the hook classifies as a background or hostile-agent brief (MCP_SUBAGENT_ID or GROK_SUBAGENT_ID set, a payload subagent field, or a brief marker), from a host whose identity (MCP_SUBAGENT_ID, else GROK_SUBAGENT_ID, else the hook payload's session_id) differs from the agentSessionId that session-state.yaml records for the root, opens and logs its own session-log turn in the root's session: its local turn state lives in a cache folder of its own keyed by that identity; its first write carries the root's session id and the id of the turn the root's current-turn.yaml names, open or completed, in contextList and the tag background-agent; the root's current-turn.yaml and the root's turn are unchanged, so the session holds both turns in progress. That host's Stop completes its own in-progress turn, with or without recorded work, and never the root's; with MCP_SUBAGENT_ID or GROK_SUBAGENT_ID set, the session-log verbs the agent calls itself reach its own turn. No prompt reuses another prompt's turn or is skipped; a brief whose identity equals the root's, or that has none, is the root host's own prompt and opens a root turn as an operator prompt does. Assumes the root's session-state.yaml is intact; a SessionStart that replaces it is triage-report-a4f43e4c, outside this criterion by operator decision 2026-10-15 (PLAN-SESSIONTRIAGEFIX-001 D3, triage-report-061f30e9; BackgroundTurnAssertion_AcceptsOwnTurnAndRejectsReuseSkipOrSupersede, ScopeIdentityRule_ReferencePassesAndRejectsWrongRules, StopOutcomeAssertion_AcceptsOwnCompletionAndRejectsRootCompletion, ScopeIdentityRule_ShippedFunction_PinnedIdentities, UserPromptSubmit_BackgroundBrief_OpensOwnTurnInRootSession, UserPromptSubmit_BackgroundBrief_CompletedRoot_OpensOwnTurn, UserPromptSubmit_BriefWithRootIdentity_OpensRootTurn, Stop_BackgroundHost_CompletesOwnTurnOnly, SessionLogVerb_SubagentEnvironment_ReachesOwnTurn).

## TR-MCP-PLUGINCORE-001

**sync-plugin-core + check-core-integrity (sh+ps1)** — Copy lib trees, emit CORE-MANIFEST.yaml with sha256; guard recomputes and fails on drift/missing.
**Covered by:** FR: FR-MCP-PLUGINCORE-001; TEST: TEST-MCP-PLUGINCORE-001
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGINCORE-002

**core-guard.yml no-duplication job** — CI enumerates lib files; any not in manifest nor PLUGIN-RESIDUAL.txt fails the build.
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGINCORE-003

**repl-daemon.js TCP broker + repl-persistent.sh wrapper** — Detached node broker keeps one repl child, NDJSON in/--- out, state-file readiness, idle shutdown, restart; shell wrapper builds envelopes and falls back to spawn-per-call.
**Covered by:** FR: FR-MCP-PLUGINCORE-003; TEST: TEST-MCP-PLUGINCORE-003
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGINCORE-004

**Dictionary-safe plugin dialog parsing** — The canonical PowerShell plugin core SHALL read dialogItems and dialog from IDictionary and property-backed parameter objects and SHALL fail closed when no dialog items are parsed.
**Covered by:** FR: FR-MCP-PLUGINCORE-004; TEST: TEST-MCP-PLUGINCORE-004, TEST-MCP-PLUGINCORE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGINCORE-005

**PowerShell.MCP cross-volume text-edit workaround documented** — Operator guidance SHALL document the PowerShell.MCP cross-volume text-edit workaround in the PowerShell.Mcp Command Routing block: the text-edit cmdlets (Add-LinesToFile/Update-LinesInFile/Update-MatchInFile/Remove-LinesFromFile) stage a replacement file and move it into place, and that move fails when TEMP/TMP are on a different volume than the target; the fix is to set TEMP/TMP to a directory on the same volume as the edit target and to verify the edit landed afterward, since a failed move can leave the edit unapplied while still previewing the diff. Acceptance Criteria: (AC1) templates/prompt-templates.yaml and its graphrag canonical mirror both contain the same-volume TEMP/TMP + verify-after-edit note and both parse as YAML; (AC2) the added guidance contains no em-dashes. Origin: BUG-TRIAGE-084 (third-party PowerShell.MCP module cross-volume replacement-move failure; prior art 054/056 resolved as an undocumented operational workaround). The module is unpinned in Confirm-PowerShellMcpRuntime; a version pin is a future option. Validated by TEST-MCP-PLUGINCORE-005.
**Covered by:** FR: FR-MCP-PLUGINCORE-004; TEST: TEST-MCP-PLUGINCORE-004, TEST-MCP-PLUGINCORE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGIN-HEADER-001

**Observed-only resolution of agent runtime header fields** — agent-runtime-header.ps1 MUST resolve agentSessionTranscriptFile through Get-McpPluginFirstExistingFile so only an existing file is reported and the previous unconditional fallback to (Join-Path CacheDir 'session.jsonl') is removed. Resolve-McpPluginAgentExecutableVersion MUST NOT fall back to MCP_PLUGIN_VERSION and MUST return 'unknown' when live discovery fails. agentSessionId MUST come only from a provider-native source (hook payload session_id or host env) and MUST NOT fall back to the MCP SessionId. Resolve-McpPluginAgentHeaderFields MUST accept ProviderSessionId and TranscriptPath, and plugin-hook.ps1 MUST supply the host hook payload session_id and transcript_path (Read-HookInput memoized with StrictMode-safe initialization). Invoke-ReplPersistTurn in repl-invoke.ps1 MUST apply the same existence rule at the submit layer and MUST clear agentSessionId when it equals the MCP session id, so a pre-fix session-state cache cannot re-submit a fabricated value. Validated by TEST-MCP-PLUGIN-HEADER-002, TEST-MCP-PLUGIN-HEADER-003, TEST-MCP-PLUGIN-HEADER-004, TEST-MCP-PLUGIN-HEADER-005. Covered by FR: FR-MCP-PLUGIN-HEADER-001. Status: implemented. Scope: layer-1+.
**Covered by:** FR: FR-MCP-PLUGIN-HEADER-001; TEST: TEST-MCP-PLUGIN-HEADER-002, TEST-MCP-PLUGIN-HEADER-003, TEST-MCP-PLUGIN-HEADER-004, TEST-MCP-PLUGIN-HEADER-005
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGININT-001

**Shared multi-plugin Session Log integration harness** — A central integration test harness must drive each plugin repository through its supported entrypoint against one disposable real MCP Server workspace, use a shared scenario catalog, and add aiUnit semantic validation without replacing deterministic assertions.
**Covered by:** FR: FR-MCP-PLUGININT-001; TEST: TEST-MCP-PLUGININT-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The Node plugin entries of mcpserver-cline-v2-plugin and mcpserver-opencode-plugin (src/index.ts, built by npm run build to dist/index.js, the file the harness imports) export the plugin factory as createQBrainAiPlugin, the name the integration harness loads (invoke-node-plugin-export.mjs), and keep createMcpServerPlugin exported for existing callers (triage-report-78b6efa1; PLAN-SESSIONTRIAGEFIX-001 E3, operator 2026-10-15 "Export in plugins"; tests: FactoryExportAssertion_AcceptsBothNamesAndRejectsMissingCommentedDroppedOrStaleBuild and NodePlugin_ThisRepository_ExportsCreateQBrainAiPlugin in tests/NodeFactoryExport.Tests.ps1 of both repositories, and the ClineV2 and OpenCode rows of the deterministic matrix).

## TR-MCP-PLUGIN-SKILLS-001

**Probe TR id pattern** — Probe only; should not be created if id validation fails or duplicate cleanup is needed.
**Covered by:** FR: FR-MCP-PLUGIN-SKILLS-001
**Status:** pending
Scope: layer-1+

## TR-MCP-PLUGIN-TRIAGE-001

**Triage plugin guidance** — Plugin skills and wrapper commands expose triage consistently.
**Status:** pending
Scope: layer-1+

## TR-MCP-POL-001

**Natural Language Policy Management** — `PolicyManagementTool` MCP STDIO tool + `POST /mcpserver/workspace/policy` REST endpoint. Accepts natural language directives, parses intent (action, category, value, scope) via LLM, applies workspace config mutations via `IWorkspaceService.UpdateAsync`, logs `policy_change` actions per affected workspace session log.

**Covered by:** `WorkspaceController` (`POST /mcpserver/workspace/policy`), `WorkspacePolicyService`, `WorkspacePolicyDirectiveParser`, `McpServerMcpTools.workspace_policy_apply`
**Status:** pending
Scope: layer-1+

## TR-MCP-PRODUCT-API-001

**Product CQRS and adapters** — Commands/queries live under McpServer.Support.Mcp/Products/. REST /mcpserver/products, MCP product_*, ProductClient, REPL client.Products, and plugin descriptors dispatch those handlers only. No public IProductService facade. GET /mcpserver/requirements/effective gains productScope=product|local (default product). AC: controller/MCP/REPL tests prove dispatch-only; invalid key 400; duplicate 409.
**Covered by:** FR: FR-MCP-PRODUCT-001, FR-MCP-PRODUCT-003; TEST: TEST-MCP-PRODUCT-001, TEST-MCP-PRODUCT-003, TEST-MCP-PRODUCT-004, TEST-MCP-PRODUCT-005, TEST-MCP-PRODUCT-002
**Status:** pending
Scope: layer-1+

## TR-MCP-PRODUCT-AUTH-001

**Product authorization and transaction gating** — Authorization lives in CQRS handlers (or a helper used only by those handlers). Owner-only mutate except self-leave. Non-member get is 404. Mutating product commands fail closed when turn transactions are required and degraded. AC: non-owner update 403; outsider get 404; degraded transactions reject create/add-member.
**Covered by:** FR: FR-MCP-PRODUCT-001, FR-MCP-PRODUCT-002, FR-MCP-PRODUCT-004; TEST: TEST-MCP-PRODUCT-001, TEST-MCP-PRODUCT-003, TEST-MCP-PRODUCT-004, TEST-MCP-PRODUCT-005, TEST-MCP-PRODUCT-002
**Status:** pending
Scope: layer-1+

## TR-MCP-PRODUCT-CTX-001

**Product requirement context source** — Context indexer/search path for source type product-requirements derives chunks from the CQRS share helper, not from sibling ContextDocument rows. Chunks are tagged with origin workspace. AC: member pack includes sibling FR text; sibling .cs files are absent; product-requirements filter returns only those chunks.
**Covered by:** FR: FR-MCP-PRODUCT-005; TEST: TEST-MCP-PRODUCT-006
**Status:** pending
Scope: layer-1+

## TR-MCP-PRODUCT-MODEL-001

**Product storage model** — Entities ProductEntity and ProductWorkspaceMembershipEntity are host-global (no workspace EF query filter). Soft-delete. Unique Key matching ^PROD-[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$ among non-deleted rows. FK to Workspaces. Audit rows on mutations (TR-MCP-DB-004). Migrations apply on SQLite, PostgreSQL, and SQL Server without re-adding unrelated columns. AC: unique filtered index on Key; membership composite PK ProductId+WorkspaceId; apply migrations on empty and production-shaped DBs.
**Covered by:** FR: FR-MCP-PRODUCT-001, FR-MCP-PRODUCT-002; TEST: TEST-MCP-PRODUCT-001, TEST-MCP-PRODUCT-003, TEST-MCP-PRODUCT-004, TEST-MCP-PRODUCT-005
**Status:** pending
Scope: layer-1+

## TR-MCP-PRODUCT-SHARE-001

**Product requirement share query** — CQRS query path (private helper only, called from handlers) ignores the workspace query filter only for member WorkspaceIds of the caller's products. Layer filter uses the origin workspace catalog. Results include originWorkspaceId. Never merge two rows that share an id across origins. AC: union includes sibling in-scope rows; missing origin layer key excludes sibling; productScope=local is local-only.
**Covered by:** FR: FR-MCP-PRODUCT-003, FR-MCP-PRODUCT-004; TEST: TEST-MCP-PRODUCT-002, TEST-MCP-PRODUCT-004, TEST-MCP-PRODUCT-003
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-001

**QA Entity Tenancy** — `QuestionEntity`, `AnswerEntity`, `CommentEntity` use composite PK `(WorkspaceId, Id)` plus global query filter (mirrors TR-MCP-MT-003).
**Covered by:** FR: FR-MCP-085, FR-MCP-086, FR-MCP-090, FR-MCP-093; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128, TEST-MCP-115, TEST-MCP-125
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-002

**QA Question Tags JSON** — Tags stored as `TagsJson` string column on `QuestionEntity` (no separate Tag table), serialized via the same pattern as `TodoItemEntity.DescriptionJson`.
**Covered by:** FR: FR-MCP-085, FR-MCP-088; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-113
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-003

**QA Provider Migrations** — Q&A storage works on all three providers: SQLite, PostgreSQL, SQL Server. One migration per provider project.
**Covered by:** FR: FR-MCP-085, FR-MCP-086, FR-MCP-093; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-004

**QA Denormalized Vote Counters** — `VoteCount` is an `int` column on Question and Answer; vote endpoints use atomic `UPDATE ... SET VoteCount = VoteCount + @delta` via raw SQL or EF interceptor pattern. Voter identity is NOT stored on the Question/Answer row itself - it is captured per vote in the audit history (TR-MCP-QA-019 records `Actor` on every `vote_up` / `vote_down` audit row), preserving full provenance without bloating the hot read path.
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-005

**QA Accepted Answer Storage** — `AcceptedAnswerId` is a nullable string FK on `QuestionEntity`; accepting writes `AcceptedAnswerId` + `AcceptedAt`; un-accepting clears both.
**Covered by:** FR: FR-MCP-087; TEST: TEST-MCP-112, TEST-MCP-116
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-006

**QA Service Shape** — `EfQaService` follows the `EfTodoService` shape: `IServiceScopeFactory` for scoped DbContext access, `IWriteAuditLog` for audit, optional `IChangeEventBus` for events, internal `SemaphoreSlim` for write serialization.
**Covered by:** FR: FR-MCP-085, FR-MCP-086, FR-MCP-087, FR-MCP-090; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128, TEST-MCP-112, TEST-MCP-116, TEST-MCP-115, TEST-MCP-125
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-007

**QA REST Surface** — `QaController` routes at `/mcpserver/qa`; `WorkspaceResolutionMiddleware.WorkspaceIndependentPrefixes` adds `"/mcpserver/qa"` (joining `/mcpserver/todo` and `/mcpserver/sessionlog`). Auth is enforced by existing `WorkspaceAuthMiddleware`.
**Covered by:** FR: FR-MCP-085, FR-MCP-086, FR-MCP-088, FR-MCP-094; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128, TEST-MCP-113, TEST-MCP-120, TEST-MCP-121
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-008

**QA Search Indexing** — Q&A search integration: `IQaSearchIndexer` writes a `ContextDocumentEntity` (`SourceType = "qa-question"` or `"qa-answer"`, `SourceKey = $"qa/{kind}/{id}"`) and a `ContextChunkEntity` (`Content = Title + "\n\n" + Body + "\n\nTags: " + tags`) on create/update, and removes both on delete. Existing FTS5 triggers and `EmbeddingService` handle the rest; no new FTS virtual table.
**Covered by:** FR: FR-MCP-085, FR-MCP-086; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-009

**QA Author Resolver** — `IQaAuthorResolver` reads `HttpContext.User.FindFirst("sub")`, the `X-Api-Key` header via `WorkspaceTokenService`, and a request-body `author` field, applying the precedence in FR-MCP-QA-008.
**Covered by:** FR: FR-MCP-092; TEST: TEST-MCP-118
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-010

**QA MCP STDIO Tools** — MCP STDIO tools live on the existing `FwhMcpTools` class (file `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`); each Q&A tool accepts optional `workspacePath` and calls the existing `ApplyWorkspaceOverride` helper.
**Covered by:** FR: FR-MCP-094; TEST: TEST-MCP-120, TEST-MCP-121
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-011

**QA FAQ Query Projection** — FAQ projection executes as a single EF query: `Questions.Where(q => q.AcceptedAnswerId != null).Include(q => q.AcceptedAnswer).OrderByDescending(q => q.AcceptedAnswer!.VoteCount).ThenByDescending(q => q.VoteCount).Take(limit)`.
**Covered by:** FR: FR-MCP-087, FR-MCP-091; TEST: TEST-MCP-112, TEST-MCP-116, TEST-MCP-128
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-012

**QA Typed Client** — `QaClient` ships in `McpServer.Client` (NuGet `SharpNinja.McpServer.Client`); wired into `McpServerClient.Qa` via `McpServerClientFactory`.
**Covered by:** FR: FR-MCP-094; TEST: TEST-MCP-120, TEST-MCP-121
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-013

**QA XML Documentation** — XML docs on every new public type and member (CS1591 enforced). Test classes cite TR-PLANNED-013 plus the FR/TR/TEST IDs they validate.
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-014

**QA REPL Workflow** — REPL exposure: `IQaWorkflow` in `McpServer.Repl.Core` wraps `McpServerClient.Qa`; `QaWorkflow` registered as singleton in `McpServer.Repl.Core/ServiceCollectionExtensions.cs`; `QaCommandShapes` defines `MethodNamespace = "workflow.qa"` and per-method constants; `ReplCommandDispatcher` constructor takes `IQaWorkflow` and switches on `workflow.qa.*` methods.
**Covered by:** FR: FR-MCP-095; TEST: TEST-MCP-122, TEST-MCP-123
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-015

**QA PowerShell Module** — PowerShell module `tools/powershell/McpQa.psm1` reads `AGENTS-README-FIRST.yaml` via existing `Find-McpMarkerFile`/`ConvertFrom-McpMarkerContent` helpers, exports `Get-McpQuestion`, `Search-McpQuestion`, `New-McpQuestion`, `Set-McpQuestion`, `Remove-McpQuestion`, `Add-McpAnswer`, `Approve-McpAnswer`, `Add-McpQaVote`, `Add-McpQaComment`, `Get-McpFaq`.
**Covered by:** FR: FR-MCP-095; TEST: TEST-MCP-122, TEST-MCP-123
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-016

**QA Plugin Skill** — Plugin skills: each sibling plugin repo adds `skills/qa/SKILL.md` with the standard YAML frontmatter (`name`, `description` with trigger phrases, `version`). Body documents the `workflow.qa.*` command namespace, request envelope shape, response shape, and explicitly positions Q&A as a workspace knowledge source (read FAQ first; if no accepted answer matches, ask; if a peer answers and accepts, future agents inherit the answer via FAQ + hybrid search).
**Covered by:** FR: FR-MCP-097; TEST: TEST-MCP-124, TEST-MCP-129
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-017

**QA Documentation Surface** — Documentation: a new `docs/context/qa-schema.md` defines the on-demand schema reference (entities, IDs, FAQ projection); `docs/USER-GUIDE.md`, `docs/CLIENT-INTEGRATION.md`, `docs/context/api-capabilities.md`, `docs/REPL-USER-GUIDE.md`, `docs/REPL-AGENT-GUIDE.md`, `docs/FAQ.md`, `AGENTS.md`, and the root `README.md` are updated to surface the subsystem; `CLAUDE.md` `Context Loading by Task Type` adds a Q&A row.
**Covered by:** FR: FR-MCP-096; TEST: TEST-MCP-132
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-018

**QA Audit Storage** — Audit storage: `QaAuditHistoryEntity` (composite PK `(WorkspaceId, Id)`, columns `EntityKind` enum `question`/`answer`/`comment`, `EntityId`, `Action` enum `create`/`update`/`delete`/`accept`/`unaccept`/`vote_up`/`vote_down`/`comment_add`/`comment_delete`, `Version` int, `Actor`, `SnapshotJson`, `CreatedAt`) lives in `src/McpServer.Storage/Entities/` with the same global query filter as Q&A entities. Composite index on `(EntityKind, EntityId, Version)`.
**Covered by:** FR: FR-MCP-089, FR-MCP-090, FR-MCP-098; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135, TEST-MCP-115, TEST-MCP-125, TEST-MCP-126
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-019

**QA Audit Emission** — Audit emission: `EfQaService` injects `IWriteAuditLog` and calls it before persisting each mutation, capturing the pre-mutation snapshot (post-mutation for create) and computing `Version = MAX(Version) + 1` for that `(EntityKind, EntityId)` pair (mirrors `EfTodoService` audit pattern at line ~374).
**Covered by:** FR: FR-MCP-089, FR-MCP-090, FR-MCP-098; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135, TEST-MCP-115, TEST-MCP-125, TEST-MCP-126
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-020

**QA Audit Query** — Audit query: `IQaService.GetAuditAsync` returns `QaAuditQueryResult` with `TotalCount` + paged `QaAuditEntryDto` rows; filterable by `entityKind`, `entityId`, `action`, `from`, `to`, `actor`. `EfQaService.GetAuditAsync` uses an EF query mirroring `EfTodoService.GetAuditAsync`.
**Covered by:** FR: FR-MCP-098; TEST: TEST-MCP-125, TEST-MCP-126, TEST-MCP-127
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-021

**QA Audit Surfaces** — Audit surfaces: `QaController` adds `[HttpGet("questions/{id}/audit")]`, `[HttpGet("answers/{id}/audit")]`, `[HttpGet("comments/{id}/audit")]`, and `[HttpGet("audit")]`; MCP tool `qa_get_audit`; client `QaClient.GetAuditAsync`; REPL `workflow.qa.audit`; PowerShell `Get-McpQaAudit`; plugin skill documents audit access as a knowledge-provenance tool.
**Covered by:** FR: FR-MCP-098; TEST: TEST-MCP-125, TEST-MCP-126, TEST-MCP-127
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-022

**QA Answer Sources JSON** — Answer payload supports a structured `sources[]` array (each entry: `{ url, title?, fetchedAt, contentHash? }`) so web-search-derived answers carry their citations. Stored on `AnswerEntity` as `SourcesJson` (nullable string column). DTO `AnswerDto.Sources`, request `CreateAnswerRequest.Sources`. FAQ projection surfaces sources alongside the accepted answer.
**Covered by:** FR: FR-MCP-086, FR-MCP-091, FR-MCP-099; TEST: TEST-MCP-111, TEST-MCP-117, TEST-MCP-128, TEST-MCP-116, TEST-MCP-129
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-023

**QA Mandatory Web Capture Skill Rule** — Plugin skill mandate: every `skills/qa/SKILL.md` opens with a mandatory rule block, formatted as a callout (`> [!IMPORTANT]`), stating: "Any time you invoke an internet search, web fetch, browser MCP, or any external HTTP retrieval to help answer a workspace question, you MUST create a Q&A entry capturing the question, the synthesized answer, and the source URLs in the `sources[]` array. If you verified the answer, accept it. This is required, not optional." Skill also documents the exact `workflow.qa.create` + `workflow.qa.createAnswer` + `workflow.qa.accept` sequence and the `sources[]` schema.
**Covered by:** FR: FR-MCP-097, FR-MCP-099; TEST: TEST-MCP-124, TEST-MCP-129
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-024

**QA Companion Web Skill Cross References** — Companion skills in each plugin that perform web research (`web-research`, `browser`, `search`, or equivalents already shipped in each plugin) get an inline "Required follow-up" section pointing back at the Q&A skill, so the mandate is visible from both directions. Implementation step: enumerate each sibling plugin's existing skills and add the cross-reference where applicable.
**Covered by:** FR: FR-MCP-097, FR-MCP-099; TEST: TEST-MCP-124, TEST-MCP-129
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-025

**QA Close and Duplicate Storage** — Close / duplicate storage: extend `QuestionEntity` with `IsClosed` (bool), `ClosedAt` (DateTimeOffset?), `ClosedByActor` (string?), `CloseReason` (enum string `closed`/`duplicate`/`off-topic`/`resolved`/`other`), `DuplicateOfQuestionId` (nullable, with a self-referencing FK + index). FAQ projection filters out closed questions unless `?includeClosed=true`. Search indexer emits a `closed` flag in the `ContextChunkEntity.Metadata` JSON so hybrid-search consumers can filter.
**Covered by:** FR: FR-MCP-100; TEST: TEST-MCP-130
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-026

**QA Close and Duplicate Surfaces** — Close / duplicate endpoints: `POST /mcpserver/qa/questions/{id}/close` (body `{ reason, duplicateOfQuestionId? }`), `POST /mcpserver/qa/questions/{id}/reopen`, both writing audit rows with action `close` / `reopen` / `mark_duplicate`. Surface in MCP tool (`qa_close_question`, `qa_reopen_question`), client (`CloseQuestionAsync`, `ReopenQuestionAsync`), REPL (`workflow.qa.close`, `workflow.qa.reopen`), PowerShell (`Close-McpQuestion`, `Open-McpQuestion`), and skill body.
**Covered by:** FR: FR-MCP-100; TEST: TEST-MCP-130
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-027

**QA Body Rendering** — Sanitization pipeline: add NuGet packages `Markdig` (markdown -> HTML) and `Ganss.Xss` to `Directory.Packages.props` (central package management). New service `IQaBodyRenderer` (impl `QaBodyRenderer`) renders + sanitizes using a strict allow-list: tags `p, h1, h2, h3, ul, ol, li, code, pre, strong, em, a, blockquote, table, thead, tbody, tr, th, td, hr, br, img`; attributes `href, src, alt, title, class` (with `class` restricted to a set of code-highlight classes); URLs limited to `http`, `https`, `mailto`; force `rel="nofollow noopener"` on all `<a>`; drop `script`, `iframe`, `object`, `embed`, `form`, all `on*` attributes, and `javascript:` URLs. Renderer is called by `EfQaService` on every Create / Update for Question, Answer, and Comment, populating sibling columns `TitleHtml?` (Question only), `BodyHtml`, plus answer/comment equivalents. Audit snapshots also store the rendered HTML so historical views are safe.
**Covered by:** FR: FR-MCP-101; TEST: TEST-MCP-131
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-028

**QA Sanitization Tests** — Sanitization tests: `tests/McpServer.Support.Mcp.Tests/Services/QaBodyRendererTests.cs` covers a canonical XSS-payload corpus (script tags, `<img onerror>`, `javascript:` href, data URLs, nested HTML in markdown, comment-out attacks, html entities). Same corpus runs against the live controller via `tests/McpServer.Qa.Validation/ErrorTests/SanitizationTests.cs`.
**Covered by:** FR: FR-MCP-101; TEST: TEST-MCP-131
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-029

**QA FAQ Wiki Generation Target** — FAQ wiki page generation: add a Nuke build target (e.g. `BuildFaqWikiPage`) in `build/Build.cs` (or whatever the existing target file is) that POSTs `GET /mcpserver/qa/faq?limit=500&includeSources=true` to a configured workspace (env-var-driven endpoint + API key, mirroring existing wiki publication patterns), formats the response into Markdown, writes `docs/Project/wiki/azure/FAQ.md` and `docs/Project/wiki/github/FAQ.md`, and updates `Home.md`, `_Sidebar.md`, and `.order` entries to list the FAQ page. Target is wired into the existing publication target so `./build.ps1` rebuilds the FAQ page alongside the requirements wiki.
**Covered by:** FR: FR-MCP-102; TEST: TEST-MCP-132
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-030

**QA FAQ Wiki Snapshot Tests** — FAQ wiki content tests: a `tests/Build.Tests/FaqWikiPageTests.cs` test invokes `BuildFaqWikiPage` against a fixture FAQ JSON payload, snapshot-compares the generated Markdown to a checked-in expected output to catch unintended formatting changes, and asserts the wiki index files reference the new page.
**Covered by:** FR: FR-MCP-102; TEST: TEST-MCP-132
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-031

**QA Voter History** — Voter-history endpoints (derived from audit): `GET /mcpserver/qa/questions/{id}/voters` and `GET /mcpserver/qa/answers/{id}/voters` return the audit rows for that entity filtered to `Action IN ('vote_up','vote_down','vote_change','vote_revoke')`, projected as `{ actor, action, createdAt }` with paging. Same surface exposed through MCP (`qa_get_voters`), client (`QaClient.GetVotersAsync`), REPL (`workflow.qa.voters`), and PowerShell (`Get-McpQaVoters`). The plugin skill documents this endpoint as the canonical way to answer "who voted on X". A companion `GET .../votes` endpoint returns the current per-voter state (one row per active voter) from `QaVoteEntity` for "what is each voter's current position" queries.
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-032

**QA Vote State Storage** — Per-voter state storage: new `QaVoteEntity` (composite PK `(WorkspaceId, Id)`; columns `EntityKind` (`question`/`answer`), `EntityId`, `VoterActor`, `VoteValue` (`1` or `-1`), `CreatedAt`, `UpdatedAt`). Unique index `(WorkspaceId, EntityKind, EntityId, VoterActor)` enforces one-vote-per-user-per-entity at the database layer (prevents race conditions even under concurrent vote calls). Global query filter on `WorkspaceId` matches other Q&A entities.
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-033

**QA Vote State Machine** — Vote state machine: `EfQaService.VoteAsync(entityKind, entityId, delta, actor)` runs in a single transaction that (a) looks up the existing `QaVoteEntity` row, (b) applies one of `no-op` (same vote already exists), `apply` (no existing vote -> insert + counter +/- 1), `change` (opposite vote exists -> update row + counter +/- 2), or `revoke` (delta is 0 and a vote exists -> delete row + counter -/+ 1), (c) writes the corresponding audit row with action `vote_up` / `vote_down` / `vote_change` / `vote_revoke` (no audit row on `no-op`), (d) updates the Question/Answer counter via atomic `UPDATE`. Returns the resulting state so callers know which branch ran.
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-034

**QA Vote Audit Actions** — Vote audit-action enum extends to: `vote_up`, `vote_down`, `vote_change`, `vote_revoke`. Migration adds the new values to the audit action enum check constraint (where one exists - SQLite stores as string, SqlServer / Postgres via check constraint).
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QB-001

**Remove the QuadBrain tool surface from plugins, MCP transports, passthrough, and the shared agent catalog** — Implements FR-MCP-142. Four independent exposures were identified and all four SHALL be removed; removing only the first leaves QuadBrain reachable. (1) Shared Node plugin core: delete plugins/core/lib-node/src/tools/brain-slots.ts and tests/brain-slots.test.ts, remove the import at runtime/host-context.ts:15, the brainSlotTools spread at :40, the dispatch branch at :290, the public re-export at index.ts:49, and the brain_slot_status assertion at tests/host-context.test.ts:127, then rebuild dist because dist is gitignored and stale compiled output keeps exporting the tools. (2) Server MCP transports: delete src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.BrainSlots.cs, which declares all eleven brain_slot tools and is registered by assembly scan at Program.cs:706 WithToolsFromAssembly and by McpStdioHost, with zero identity filtering anywhere in the project, so every MCP client sees them; prune the brain-slot entries from docs/stdio-tool-contract.json and retire tests/McpServer.Support.Mcp.Tests/McpStdio/BrainSlotContractArtifactTests.cs, keeping the artifact for MemoryContractArtifactTests. (3) Named client passthrough: remove the BRAINSLOTS mapping at src/McpServer.Repl.Core/GenericClientPassthrough.cs:168 and its BrainSlot result-unwrapping cases, closing the client-invoke back door that survives tool removal. (4) Shared hosted-agent catalog: remove mcp_quadbrain_coding_execute from McpHostedAgentToolAdapter, IMcpHostedAgent, QBAgentDefinition, and McpQuadBrainCodingAgentRouter; it is advertised to every McpServer.McpAgent host and is already dead for QBAgent because QuadBrainInternalToolExecutor has no case for it. Also delete the eleven mcps/mcpserver/tools/brain_slot_*.json descriptors, which have no in-repo consumer. BrainSlotClient itself remains, because the server-side orchestration path and its tests use it; only the agent-reachable routes to it are removed.
**Covered by:** FR: FR-MCP-142; TEST: TEST-MCP-193
**Status:** pending
Scope: layer-1+

## TR-MCP-QBAGENT-001

**QBAgent marker bootstrap and graceful no-marker exit** — QBAgent startup resolves baseUrl and apiKey from the AGENTS-README-FIRST.yaml marker in the working directory (not from defaulted McpAgentOptions); binds the QuadBrain coding route to that endpoint with X-Api-Key auth; rejects/omits all non-QuadBrain surfaces; and when no marker file is found performs a clean graceful shutdown (defined exit, informational log, no endpoint contact).
**Covered by:** FR: FR-MCP-QBAGENT-001; TEST: TEST-MCP-QBAGENT-001, TEST-MCP-QBAGENTINT-001
**Status:** pending
Scope: layer-1+

## TR-MCP-QBEXEC-001

**QuadBrain internal-tool interception** — QuadBrain internal-tool interception: classify mcp_ as internal, execute all catalog mcp_* tools server-side, strip them from tool_calls, return service results as a normal assistant message when no external tools remain, and emit only non-mcp_ tools to QBAgent.
**Covered by:** FR: FR-MCP-QBEXEC-001; TEST: TEST-MCP-QBEXEC-001
**Status:** pending
Scope: layer-1+

## TR-MCP-QBEXEC-002

**Internal-tool executor dispatch** — QuadBrainInternalToolExecutor dispatches on toolCall.Function.Name for every mcp_* name in McpHostedAgentToolAdapter and QBAgentDefinition. It deserializes Arguments and calls the matching in-process application/CQRS or transaction-gated service. It maps results to InternalToolExecutionOutcome Ok/Fail/Unhandled. It replaces NoopInternalToolExecutor in DI. It does not HTTP-call MCP endpoints. Unknown catalog-absent mcp_ names return Unhandled.
**Covered by:** FR: FR-MCP-QBEXEC-002; TEST: TEST-MCP-QBEXEC-002
**Status:** pending
Scope: layer-1+

## TR-MCP-QBEXEC-003

**Full-text inter-brain session-log capture** — Brain-slot invocations and AoT reconciliation write full prompt+output text to the session log via ISessionLogService correlated by TurnId, retaining the hashed BrainSlotInvocationEntity audit row; internal-tool executed/failed outcomes are logged; secrets are redacted.
**Covered by:** FR: FR-MCP-QBEXEC-003; TEST: TEST-MCP-QBEXEC-003
**Status:** pending
Scope: layer-1+

## TR-MCP-QBOLLAMA-002

**OllamaServerController with injectable probe, launcher, and ownership-scoped teardown** — Implements FR-MCP-QBOLLAMA-002. OllamaServerController lives in tests/TestSupport.Shared with no xunit dependency; its probe delegate, executable resolver, process launcher, poll interval, timeout, delay, and clock are injected so the policy is unit-testable without a real Ollama binary. It is linked into McpServer.Support.Mcp.Tests and McpServer.Support.Mcp.IntegrationTests through tests/Directory.Build.targets following the Validation.Shared linked-source idiom. EnsureRunningAsync probes first, launches only when the probe fails, then polls until the probe succeeds or the timeout elapses; on timeout it terminates any process it started before throwing. A resolver returning no executable throws an InvalidOperationException naming the InstallOllama Nuke target without attempting a launch. StopAsync terminates only a controller-started process and is idempotent. OllamaServerFixture implements IAsyncLifetime over the controller with a real HttpClient probe on /api/tags, a resolver covering PATH, LocalAppData/Programs/Ollama, and the Nuke test-tools ollama directory (ollama.exe on Windows; bin/ollama, the Linux portable layout InstallOllama provisions, on Linux; PLAN-SESSIONTRIAGEFIX-001 B13), and a launcher running 'ollama serve' that kills the whole process tree; QuadBrainOllamaEndpointIntegrationTests consumes it through IClassFixture. IMPLEMENTED 2026-07-20. Evidence: red gate was a compile failure (CS0234/CS0246, controller type absent); green gate OllamaServerControllerTests 6/0/0 and Support.Mcp.Tests 1692/0/0. Live proof on PAYTON-LEGION2 with no Ollama running and none on PATH: QuadBrainOllamaEndpointIntegrationTests went from 3 failed to 4 passed in 3m35s using the portable binary at LocalAppData/McpServer/test-tools/ollama, and after the run zero ollama processes remained with port 11434 not listening.
**Covered by:** FR: FR-MCP-QBOLLAMA-002; TEST: TEST-MCP-QBOLLAMA-002
**Status:** pending
Scope: layer-1+

## TR-MCP-QBOPENAI-001

**OpenAI chat-completions surface over QuadBrain orchestration** — Add OpenAI-compatible chat-completion request/response DTOs and a server endpoint that maps an inbound OpenAI ChatCompletion request onto QuadBrain orchestration (last user turn + system context as the prompt) and returns an OpenAI ChatCompletion response carrying the Arbiter output. Subsequent slices add tool/function-calling (tools in the request, assistant tool_calls in the response) and optional streaming. QBAgent points a standard OpenAI IChatClient at this endpoint (baseUrl/apiKey from marker), runs the Agent Framework tool loop, and executes action tools.
**Covered by:** FR: FR-MCP-QBOPENAI-001; TEST: TEST-MCP-QBINT-001, TEST-MCP-QBOPENAI-001
**Status:** pending
Scope: layer-1+

## TR-MCP-QBPROGRESS-001

**Role progress via orchestration IProgress and OpenAI SSE** — QuadBrainOrchestrationRequest carries a non-serialized IProgress of QuadBrainRoleProgress. InvokeRoleAsync reports phase started before the slot call and phase completed with output after. ExecuteAotReconciliationCoreAsync reports ArbiterOfTruth the same way. When POST /v1/chat/completions has stream=true, the controller writes SSE event quadbrain.role as those reports arrive, then the OpenAI chat.completion.chunk with the Arbiter assistant content, then data [DONE]. It does not buffer the entire orchestration before the first role event. QBAgent posts stream=true and prints each role event as a progress line while the turn is still running.
**Covered by:** FR: FR-MCP-QBPROGRESS-001; TEST: TEST-MCP-QBPROGRESS-001
**Status:** pending
Scope: layer-1+

## TR-MCP-QBRAIN-001

Use one token per surface class. Display text uses QBrain.AI. C# namespaces and assemblies use QBrainAi. NuGet package ids use QBrainAI.Component with no SharpNinja prefix. The lowercase single token that replaces mcpserver is qbrainai. Already-hyphenated slugs use qbrain-ai. npm scopes use @qbrainai. Do not replace the QBAgent segment or the qbagent command.
**Covered by:** FR: FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-003; TEST: TEST-MCP-QBRAIN-001
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A review of the rename diff finds no QBrainAI and no QBrain.AI inside a C# namespace or assembly name. QBrainAI is allowed only as the NuGet package-id prefix.
- [ ] QBAgent and qbagent are unchanged apart from a leading QBrainAi root becoming QBrainAi.

## TR-MCP-QBRAIN-002

Replace the root token QBrainAi with QBrainAi in namespaces, assembly names, project folders, InternalsVisibleTo, and the solution file. Keep every segment after the root, including .Mcp, .McpAgent, and .QBAgent. Storage, Services, and GraphRag stay under QBrainAi.Support.Mcp. tests/Build.Tests stays NukeBuild.Tests. _build stays _build.
**Covered by:** FR: FR-MCP-QBRAIN-003; TEST: TEST-MCP-QBRAIN-002
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The solution file and project paths use QBrainAi as the root token.
- [ ] No public namespace still starts with QBrainAi.
- [ ] QBrainAi.QBAgent exists and QBrainAi.QBrainAi does not.
- [ ] NukeBuild.Tests and _build are not renamed into QBrainAi.

## TR-MCP-QBRAIN-003

**Package and tool migration** — Move published package ids from SharpNinja.McpServer.Component to QBrainAI.Component for Client, Cqrs, Cqrs.Mvvm, McpAgent, Repl.Core, Repl, and QBAgent. Ship dependency facades under the old SharpNinja.McpServer.* ids through 1.x. Rename ToolCommandName mcpserver-repl to qbrain-ai-repl and keep the old command on the deprecated package. Rename the npm packages in this repo to the @qbrainai scope. Do not push the new ids to nuget.org in Phase 1.
**Covered by:** FR: FR-MCP-QBRAIN-004; TEST: TEST-MCP-QBRAIN-003
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each new package id is set on its project.
- [ ] Each old package id has a facade project that depends on the new package.
- [ ] qbrain-ai-repl is the new tool command and mcpserver-repl remains on the deprecated package.
- [ ] The renamed npm package @qbrainai/qbrain-ai-plugin-core keeps its 1.x public names createMcpServerPluginCore and McpServerPluginCoreConfig as deprecated aliases of createQBrainAiPluginCore and QBrainAiPluginCoreConfig through 1.x, so host plugins built against the 1.x names build and load unchanged (BUG-PLUGINCORE-001; lib-node tests aliases-1x.test.ts and aliases-1x-types.test.ts).

## TR-MCP-QBRAIN-004

Cloud pull requests land on the current GitHub names. Payton renames repositories only in Phase 3. Phase 1 does not rename a GitHub repository. RepositoryUrl keeps the live repo name until that rename.
**Covered by:** FR: FR-MCP-QBRAIN-005; TEST: TEST-MCP-QBRAIN-004
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] No cloud phase renames a GitHub repository.
- [ ] Package RepositoryUrl values still point at sharpninja/QBrainAi.

## TR-MCP-QBRAIN-005

**Route, config, and environment migration** — Add product HTTP prefix /qbrainai/* and keep /mcpserver/* through 1.x. Leave /mcp-transport unchanged. Configuration root becomes QBrainAi:. If only Mcp: is present, bind it. If both are present, QBrainAi: wins per key and startup logs one warning. Product MCP_* variables are read when the matching QBRAINAI_* variable is unset. When both are set, QBRAINAI_* wins. Do not rename the sentinel MCP_UNTRUSTED through 1.x.
**Covered by:** FR: FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-004; TEST: TEST-MCP-QBRAIN-005
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] One representative product route is declared on both /qbrainai/... and /mcpserver/....
- [ ] /mcp-transport is still mapped and is not an alias of the product prefix.
- [ ] Config and environment alias precedence matches the two rules above.
- [ ] MCP_UNTRUSTED remains the literal sentinel.

## TR-MCP-QBRAIN-006

Docker image, container, network, and volume use qbrain-ai. Windows service name becomes QBrainAi, DisplayName becomes QBrain.AI, and the default install path becomes C:\ProgramData\QBrainAi, with parameters that can still target the old service and path. Linux paths, the qbrainai account, and qbrainai.service stay Phase 4-L text. Do not rename database McpServer_Omarchy or host PAYTON-LEGION2.
**Covered by:** FR: FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-005; TEST: TEST-MCP-QBRAIN-006
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Compose and Dockerfile names in git match the qbrain-ai forms.
- [ ] Service script defaults in git match QBrainAi and C:\ProgramData\QBrainAi, and the old names remain available as parameters.
- [ ] McpServer_Omarchy and PAYTON-LEGION2 remain where they identify that database and that host.

## TR-MCP-QBRAIN-007

Before any replace, write a classification TSV for the patterns in the rebrand plan section 5. Do not rewrite existing files under docs/receipts/**. Do not hand-edit docs/Project/wiki/**. Do not rename mcp.db, .mcpServer, the class McpDbContext, or EF table names.
**Covered by:** FR: FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-005; TEST: TEST-MCP-QBRAIN-007
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The TSV exists before the first rename commit, and every hit has a class and a closed action.
- [ ] mcp.db, .mcpServer, and McpDbContext remain the persisted-state names.
- [ ] The slice does not add an EF migration whose only effect is a table rename.

## TR-MCP-QBRAIN-008

An implementation slice starts only after this plan is approved and only for the phase the operator has opened. The slice registers the FR and TR ids it cites before product code changes. Failing tests for that slice's acceptance criteria come next. Production edits come after those tests fail for the intended reason. The slice is not done until its unit scope reports zero failures and zero skips and until the hostile-validation checkpoint returns OverallVerdict AGREE from the hostile reviewer the operator names for the slice; no requirement names a specific model (operator rule 2026-10-15; PLAN-SESSIONTRIAGEFIX-001 B9). SessionLife tests are out of scope.
**Covered by:** FR: FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-003, FR-MCP-QBRAIN-004, FR-MCP-QBRAIN-005; TEST: TEST-MCP-QBRAIN-008
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Requirements ids are stored before product renames.
- [ ] The slice shows a red test run before the green run.
- [ ] Phase 1 is not marked done without the hostile-validation receipt.

## TR-MCP-QBSEED-002

**Gated idempotent Quad-Brain startup provisioning and /v1 workspace scoping** — BrainSlotOptions gains Slots (List<BrainSlotSeedDefinition>), each carrying a SlotId plus UpsertBrainSlotRequest fields with safe credential references. BrainSlotStartupSeeder provisions the GLOBAL quad on StartAsync with idempotency keyed by SlotId. WorkspaceResolutionMiddleware resolves /v1 requests from X-Workspace-Path header or Bearer/X-Api-Key token, scoping internal-tool mutations to that workspace while brains remain global.
**Covered by:** FR: FR-MCP-QBSEED-001; TEST: TEST-MCP-QBSEED-001
**Status:** pending
Scope: layer-1+

## TR-MCP-QBSKILLS-001

**SKILL.md manifest model and parser** — SkillManifest + SkillManifestParser parse agentskills.io frontmatter (name+description required; optional license/version/allowed-tools) using the YAML library already used by QBAgentBootstrapper; folder model supports optional scripts/references/assets.
**Covered by:** FR: FR-MCP-QBSKILLS-001; TEST: TEST-MCP-QBSKILLS-001, TEST-MCP-QBSKILLS-002
**Status:** pending
Scope: layer-1+

## TR-MCP-QBSKILLS-002

**Skill storage layout and dotnet/skills vendoring** — Skills live under skills/ at the repo root; dotnet/skills is vendored at skills/vendor/dotnet-skills as a git submodule (mirroring tools/McpServerTools) or synced via a build target; the registry scans both roots path-safely via IRepoFileService.
**Covered by:** FR: FR-MCP-QBSKILLS-003; TEST: TEST-MCP-QBSKILLS-002
**Status:** pending
Scope: layer-1+

## TR-MCP-QBSKILLS-003

**Discovery-list injection** — Only the size-bounded discovery list (name+description per skill) is injected into the QBAgent system prompt; full SKILL.md bodies are fetched on demand via load_skill.
**Covered by:** FR: FR-MCP-QBSKILLS-002; TEST: TEST-MCP-QBSKILLS-003
**Status:** pending
Scope: layer-1+

## TR-MCP-QBTOOLS-000

**Single core per capability (anti-duplication)** — Each tool capability (edit, bash, git) has exactly one core service carrying path-safety/transaction contracts. The internal plane calls the transaction-gated core directly; the external plane calls the same core via the MCP client. Tool classes contain only transport and JSON-shape adaptation, no business logic.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The repository file core (IRepoFileService) resolves every requested path the same way on every OS: an absolute path inside the workspace root is accepted as the matching repo-relative path; an absolute path outside the root, or any path with a .. segment, is rejected before any read, list, write, edit, or transactional capture; a leading separator always marks an absolute path and is never reinterpreted as repo-relative (operator decision 2026-10-11, "Reject outside absolutes (Recommended)", BUG-BASELINE-003; RepoFileServiceTests RootedExternalPath_AcrossRepoOperations_IsRejected, RootedPathInsideRoot_AcrossRepoOperations_ResolvesRepoRelative, LeadingSeparatorPath_IsNotReinterpretedAsRepoRelative, RootedPathWithParentSegment_IsRejected).

## TR-MCP-QBTOOLS-001

**External tool surface project and registration** — Agent-side external tools live in src/McpServer.QBAgent.Tools, are built with AIFunctionFactory.Create (non-mcp_ names), and are injected via baseOptions.ChatOptions.Tools into agent.CreateRunOptions; file tools delegate to the MCP client Repo surface.
**Covered by:** FR: FR-MCP-QBTOOLS-001; TEST: TEST-MCP-QBTOOLS-001, TEST-MCP-QBTOOLSINT-001
**Status:** pending
Scope: layer-1+

## TR-MCP-QBTOOLS-002

**run_powershell backed by HostedPowerShellSessionManager** — run_powershell reuses the in-process HostedPowerShellSessionManager runspace and returns captured streams.
**Covered by:** FR: FR-MCP-QBTOOLS-002; TEST: TEST-MCP-QBTOOLS-007
**Status:** pending
Scope: layer-1+

## TR-MCP-QBTOOLS-003

**run_bash via ProcessRunner with PATH resolution** — run_bash resolves bash.exe through IProcessEnvironmentService.ResolveExecutable and runs via ProcessRunner; absent bash yields a structured available=false result.
**Covered by:** FR: FR-MCP-QBTOOLS-003; TEST: TEST-MCP-QBTOOLS-003
**Status:** pending
Scope: layer-1+

## TR-MCP-QBTOOLS-004

**git tool via ProcessRunner with push guard** — git tool builds an argument list per the GitHubCliService pattern and runs via ProcessRunner; a subcommand allowlist gates status/diff/log/branch/add/commit/checkout/push/reset; push is constrained to the origin remote and current branch; an opt-in McpAgentOptions.AllowGitPush defaults off for first ship.
**Covered by:** FR: FR-MCP-QBTOOLS-004; TEST: TEST-MCP-QBTOOLS-002
**Status:** pending
Scope: layer-1+

## TR-MCP-QBTOOLS-005

**Server-side adapter tools mcp_repo_edit/mcp_bash/mcp_git** — McpHostedAgentToolAdapter.CreateFunctions adds mcp_repo_edit, mcp_bash, and mcp_git with mcp_ prefix; mutating variants execute through the transaction-gated core services.
**Covered by:** FR: FR-MCP-QBTOOLS-005; TEST: TEST-MCP-QBTOOLS-006
**Status:** pending
Scope: layer-1+

## TR-MCP-QBTOOLS-006

**RepoFileService.EditAsync semantics** — IRepoFileService.EditAsync(path, oldString, newString, expectedOccurrences?) reuses NormalizeRelative/TryResolveFullPath/IsAllowed/ComputeSha256/PublishChange; missing oldString fails; ambiguous match fails unless replaceAll/expectedOccurrences; returns RepoEditResult.
**Covered by:** FR: FR-MCP-QBTOOLS-006; TEST: TEST-MCP-QBTOOLS-004, TEST-MCP-QBTOOLS-005
**Status:** pending
Scope: layer-1+

## TR-MCP-QBTOOLS-007

**Transaction-gated EditAsync compensation** — TransactionGatedRepoFileService gates EditAsync (operation repo.edit) through ITurnTransactionCoordinator using the IRepoFileCompensation snapshot for rollback on reject; degraded coordinator fails.
**Covered by:** FR: FR-MCP-QBTOOLS-006; TEST: TEST-MCP-QBTOOLS-004, TEST-MCP-QBTOOLS-005
**Status:** pending
Scope: layer-1+

## TR-MCP-QBTOOLS-008

**QBAgent tool/skill DI wiring** — AddQBAgentTools and AddQBAgentSkills register the external tool and skill surfaces; Program.cs composes baseOptions.ChatOptions.Tools from both and injects the skill discovery list into the system prompt.
**Covered by:** FR: FR-MCP-QBTOOLS-007; TEST: TEST-MCP-QBTOOLSINT-001
**Status:** pending
Scope: layer-1+

## TR-MCP-QUAD-001

**Brain-slot storage, DTOs, CRUD, and validation** — Persist BrainSlotDefinition and BrainSlotInvocation rows per workspace; expose client DTOs, REST endpoints, and STDIO/MCP parity; validate known roles, credential-reference-only secrets, one enabled slot per workspace and role, replaceExisting replacement audit, soft delete, and readiness status.
**Covered by:** FR: FR-MCP-123, FR-MCP-129; TEST: TEST-MCP-163, TEST-MCP-170, TEST-MCP-171, TEST-MCP-174, TEST-MCP-175, TEST-MCP-176, TEST-MCP-177, TEST-MCP-178
**Status:** pending
Scope: layer-1+

## TR-MCP-QUAD-002

**External model provider adapter, credentials, endpoint allowlist, timeout, and redaction** — Resolve credentials from env:, config:, or file: references without persisting raw secrets; create OpenAI/OpenAI-compatible chat clients; enforce custom endpoint host allowlists, explicit loopback allowance, per-slot timeout and cancellation, and redacted audit/log output.
**Covered by:** FR: FR-MCP-129; TEST: TEST-MCP-174, TEST-MCP-175, TEST-MCP-176, TEST-MCP-177, TEST-MCP-178
**Status:** pending
Scope: layer-1+

## TR-MCP-QUAD-003

**Keyserver party mapping and transaction diffgram admission** — Require enabled trusted party/key mapping before invocation; invoke external models only when brain-slot execution and required turn transactions are enabled; commit brain-slot.invoke diffgrams before returning output.
**Covered by:** FR: FR-MCP-129, FR-MCP-130, FR-MCP-134; TEST: TEST-MCP-174, TEST-MCP-175, TEST-MCP-176, TEST-MCP-177, TEST-MCP-178, TEST-MCP-179, TEST-MCP-181, TEST-MCP-182, TEST-MCP-184, TEST-MCP-185
**Status:** pending
Scope: layer-1+

## TR-MCP-QUAD-004

**Quad branch containment and authorization** — Provide explicit runtime gates proving AoT reconciliation execution, weight update execution, and full automatic quad orchestration execute only through FR-MCP-134/FR-MCP-135 paths, while non-Curiosity GraphRAG mutation and implicit fallback model behavior remain fail-closed.
**Covered by:** FR: FR-MCP-131; TEST: TEST-MCP-174, TEST-MCP-180, TEST-MCP-181, TEST-MCP-183
**Status:** pending
Scope: layer-1+

## TR-MCP-QUAD-005

**Quad orchestration service and contracts** — Add service, DTO, REST, client, STDIO, and plugin contracts for full Quad-Brain orchestration and AoT reconciliation while reusing the existing transaction-gated brain-slot invocation path.
**Covered by:** FR: FR-MCP-131, FR-MCP-134, FR-MCP-135; TEST: TEST-MCP-174, TEST-MCP-180, TEST-MCP-181, TEST-MCP-183, TEST-MCP-182, TEST-MCP-184, TEST-MCP-185
**Status:** pending
Scope: layer-1+

## TR-MCP-QUAD-006

**AoT reconciliation decision loop** — Implement deterministic orchestration prompts, role-output aggregation, ArbiterOfTruth reconciliation execution, and final decision response shaping with transaction IDs and diffgram IDs preserved for every role.
**Covered by:** FR: FR-MCP-134; TEST: TEST-MCP-181, TEST-MCP-182, TEST-MCP-184, TEST-MCP-185
**Status:** pending
Scope: layer-1+

## TR-MCP-QUAD-007

**Durable weight versioning and safety gates** — Persist role weights and versions on brain-slot definitions, enforce dual-control and safety-gate validation, audit before/after snapshots, and expose explicit weight update APIs.
**Covered by:** FR: FR-MCP-131, FR-MCP-135; TEST: TEST-MCP-174, TEST-MCP-180, TEST-MCP-181, TEST-MCP-183, TEST-MCP-184, TEST-MCP-185
**Status:** pending
Scope: layer-1+

## TR-MCP-QUAD-SESSION-001

**Per-session QuadBrain instance attachment over global brains** — QuadBrainOpenAiController reads X-Session-Id (and optional X-Turn-Id) request headers and passes them to IQuadBrainOpenAiChatService.CompleteAsync. The service writes sessionId/turnId into QuadBrainOrchestrationRequest.Metadata for orchestration session attachment via IBrainInteractionSessionLogger. Because brain definitions are global and the orchestration holds no shared mutable per-instance state, concurrent /v1 requests with distinct X-Session-Id values run as independent instances over the same global quad.
**Covered by:** FR: FR-MCP-QUAD-SESSION-001; TEST: TEST-MCP-QUAD-SESSION-001
**Status:** pending
Scope: layer-1+

## TR-MCP-QUALITY-001

**Warning suppression decision register and aiUnit audit** — Warning remediation must distinguish approved suppressions from required fixes through structured acceptance criteria, durable TODO state, suppression inventory validation, and a dedicated aiUnit governance review.
**Covered by:** FR: FR-MCP-139; TEST: TEST-MCP-AIUNIT-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] CA1416 is approved only for Windows only code paths with explicit platform justification and a review condition that removes the suppression if the code becomes cross platform. (evidence: config/warning-suppression-approvals.json contains scoped CA1416 approvals for Windows-only service/event-log and platform-gated code paths.)
- [x] CA1819 is approved where returning arrays is intentional for DTO or API shape and the suppression includes justification. (evidence: config/warning-suppression-approvals.json and ContextChunkEntity.Embedding justify byte[] as EF Core BLOB storage contract.)
- [x] Current CA2227 suppressions are approved only for non observable JSON, YAML, options binding DTOs, and EF navigation collections. Observable collections must be repopulated in place and not suppressed. (evidence: config/warning-suppression-approvals.json and WarningSuppressionValidationTargetTests CA2227 allowed-scope check.)
- [x] CA1308 is not approved. Code must use explicit mapping or invariant case insensitive comparison rather than lower case normalization. (evidence: src/McpServer.Support.Mcp/Logging/ParseableEventFormatter.cs, src/McpServer.Services/Ingestion/MarkdownSessionLogParser.cs, src/McpServer.Storage/Indexing/EmbeddingService.cs, tests/McpServer.Support.Mcp.Tests/Indexing/EmbeddingServiceTests.cs)
- [x] CS8632 is not approved. Every project must enable nullable annotations and CS8632 NoWarn entries must be removed. (evidence: Directory.Build.props, build/_build.csproj, lib/NSubstitute/NSubstitute.csproj, tests/Build.Tests/Build.Tests.csproj, solution build on 2026-07-07)
- [x] TreatWarningsAsErrors false is not approved. The build project warning bypass must remain removed after warning clean validation. (evidence: Directory.Build.props and dotnet build McpServer.sln -c Debug -v minimal passed with zero warnings and zero errors on 2026-07-07)
- [x] Stale ASP0019 suppressions are not approved. ASP0019 NoWarn entries must remain removed when no IHeaderDictionary Add usage remains. (evidence: tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj, tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj, solution build on 2026-07-07)
- [x] Every warning suppression or warning bypass not explicitly approved by this TR must remain open remediation work until fixed and validated. (evidence: ValidateWarningSuppressions passes after CA1848, CS8602, and Storage migration obsolete pragma removals; approval register contains only scoped approved suppressions.)
- [x] The aiUnit warning suppression governance prompt audits the suppression decisions, TODO state, requirements traceability, generated exports, and source suppression inventory. (evidence: tests/McpServer.Review.Tests/AiReviewTests.cs and build/Build.AiWarningSuppressionReview.cs)
- [x] xUnit1051 is not approved. Test projects must pass TestContext cancellation tokens to cancellable async APIs instead of suppressing the analyzer. (evidence: test project NoWarn entries, cancellable async call updates, dotnet build McpServer.sln -c Debug -v minimal, WorkspacePolicyDirectiveParserTests focused run)
- [x] xUnit1041 is not approved. xUnit v3 tests must use supported fixture and output helper patterns instead of suppressing constructor injection diagnostics. (evidence: tests/McpServer.PlanReview.Tests/McpServer.PlanReview.Tests.csproj and tests/McpServer.PlanReview.Tests/PlanTransactionReviewTests.cs)
- [x] CA1812 is not approved. Middleware and DI activated types must be made visible to analyzers through real construction or removed. (evidence: src/McpServer.ServiceDefaults/Extensions.cs and src/McpServer.ServiceDefaults/GlobalExceptionHandlerMiddleware.cs)
- [x] CA1848 is not approved. No editorconfig, project, pragma, or attribute suppression may remain for LoggerMessage guidance. (evidence: .editorconfig CA1848 severity override removed; config/warning-suppression-approvals.json CA1848 approval removed; repository suppression scan for CA1848 returns zero matches.)
- [x] CA2000 is not approved. Disposal warnings must be fixed or proven stale by removing the suppression and building clean. (evidence: tests/McpServer.Support.Mcp.Tests/Middleware/FederationMiddlewareTests.cs and Support.Mcp.Tests project build)
- [x] CA1861 is not approved. Constant array arguments must be hoisted rather than suppressed. (evidence: src/McpServer.Storage/Migrations/20260212160034_AddSessionLogTables.cs and Storage project build)
- [x] CA1062 is not approved. Public migration methods must validate migrationBuilder arguments rather than suppressing the rule. (evidence: session log migration files 20260212160034, 20260212165804, 20260212170806, and 20260212172109 plus Storage project build)
- [x] CS0436 is not approved. Type conflict NoWarn entries must be removed once the conflict is no longer present. (evidence: src/McpServer.Support.Mcp/McpServer.Support.Mcp.csproj and Support.Mcp project build)
- [ ] CS0618 is not approved. Obsolete APIs must be replaced with current APIs and covered by focused regression tests. (evidence: REOPENED 2026-09-09T17:07:12Z: generated migration 612/618 pragmas recurred on candidate 808ec049 and current develop. Historical evidence retained. Storage migration designer and snapshot 612/618 pragmas removed; W18 warning-suppression test now scans src/McpServer.Storage/Migrations plus provider migration roots.)
- [x] CA1055 is not approved. String return APIs must not advertise URI semantics. (evidence: src/McpServer.ServiceDefaults/RailwayConnectionStringBuilder.cs, src/McpServer.ServiceDefaults/PostgresConnectionStringResolver.cs, ServiceDefaults build, and resolver tests)
- [x] NU5104 is not approved. Stable packages must not depend on prerelease packages or deprecated package metadata. (evidence: Directory.Packages.props stable Microsoft Agents versions, lib/NSubstitute.6.0.0/nsubstitute.nuspec, src/McpServer.McpAgent/McpServer.McpAgent.csproj, and dotnet pack McpServer.McpAgent)
- [ ] NU1901, NU1902, NU1903, and NU1904 are not approved. Vulnerable package advisories must be resolved by updating to the patched versions the advisories name, with a clean NuGet vulnerability audit, never by suppression, audit settings, or a build override. (evidence: REOPENED 2026-10-19 (operator: "Gotta use the recommended packages."): develop ace70daa pins System.Security.Cryptography.Xml 10.0.10 (Directory.Packages.props:91), which brings System.Security.Cryptography.Pkcs 10.0.10; on the Windows host the restore of build/_build.csproj fails NU1902 (GHSA-2fmv-rv2c-59x7) and NU1903 (GHSA-846f-f764-76mx, GHSA-7hqp-7c5m-4vq6, GHSA-c399-9853-6jmj, GHSA-r955-8q5j-3h83), run unit-green-win-baseline-20261019T1433. All five advisories name 10.0.13. Text strengthened from "NU1901 and NU1903 are not approved. Vulnerable package advisories must be resolved by dependency updates and a clean vulnerability scan." Historical evidence retained: Directory.Packages.props transitive pins, Directory.Build.props suppression removal, and dotnet list McpServer.sln package --vulnerable --include-transitive.)
- [x] ErrorOnDuplicatePublishOutputFiles=false is not approved. Duplicate publish output enforcement must remain enabled unless a scoped approval names the exact affected projects and review condition. (evidence: Directory.Build.props suppression removed; config/warning-suppression-approvals.json stale approval removed; default publish probes passed for McpServer.Support.Mcp, McpServer.Repl.Host, McpServer.QBAgent, McpServer.McpAgent.SampleHost, and McpServer.Launcher on 2026-07-13; ValidateWarningSuppressions passed; WarningSuppression Build.Tests passed 18/0/0; inventory contains zero ErrorOnDuplicatePublishOutputFiles occurrences.)
- [ ] Correction of generated migration CS0612/CS0618 pragma pairs uses an explicit opt-in Nuke NormalizeGeneratedMigrationObsoletePragmas target. Compile, Test, and ValidateWarningSuppressions never invoke normalization or mutate source. Validation remains read-only and fails when exact generated-migration pragma pairs are present.

## TR-MCP-REPL-001

**YAML Envelope Protocol** — The REPL host SHALL parse incoming STDIO lines as YAML-formatted command envelopes containing `type`, `payload` with method-specific parameters, and optional `correlationId`/`requestId`. Response envelopes SHALL contain `type` (`result`/`error`/`event`), `payload` with result data or error details, and echoed identifiers. Malformed YAML SHALL emit structured error responses rather than crashing the process.

**Covered by:** `McpServer.Repl.Core` (`IYamlEnvelope`, `IYamlSerializer`, `IReplProtocol`)
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The plugin REPL bridge (plugins/core/lib-ps/repl-invoke.ps1 run with -Method, the REPL surface every agent plugin calls) answers a call that fails before any response envelope arrives (marker refresh failure, REPL executable not found, a timeout, or the REPL exiting without output) with one YAML envelope on stdout of type error whose payload carries code, message (the failure text, or the REPL's exit code when it wrote nothing) and retryable, the code and retryable taken from the plugin's error classification (TR-MCP-TRIAGEERR-001), and the script exits 1; a call that succeeds without output writes nothing; the bridge never writes a bare True or False to stdout (triage-report-eb56d4eb; PLAN-SESSIONTRIAGEFIX-001 F1; tests in plugins/core/test-fixtures/pester/ReplBridgeErrorEnvelope.Tests.ps1: ReplBridgeErrorAssertion_AcceptsErrorEnvelopeAndRejectsBareBooleanOrUntypedOutput, ReplBridge_FailureWithoutOutput_WritesErrorEnvelope, ReplBridge_SuccessWithoutOutput_WritesNothing, ReplBridge_Script_MissingRepl_ExitsOneWithErrorEnvelope).

## TR-MCP-REPL-002

**DI-Integrated REPL Host** — The REPL host SHALL use DI composition for workflow and service registration. The command loop SHALL inject scoped service instances per command invocation and SHALL NOT instantiate services via `new` or `ActivatorUtilities.CreateInstance` outside DI registration paths. Workflows SHALL be registered as scoped services and resolved from the service provider.

**Covered by:** `McpServer.Repl.Host` (`ServiceCollectionExtensions`, `Program.cs`), `McpServer.Repl.Core` workflow interfaces
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-003

**Command Loop Lifecycle** — The REPL host SHALL support graceful startup with command loop initialization, interactive STDIO processing, structured error handling with typed error codes, and clean shutdown on EOF or explicit exit. The command loop SHALL read YAML envelopes from stdin, dispatch to workflow handlers, serialize responses as YAML to stdout, and maintain session context across commands. Unhandled exceptions SHALL emit structured error responses and continue the loop.

**Covered by:** `McpServer.Repl.Host` (`Program.cs`, `AgentStdioHandler`, `InteractiveHandler`), `McpServer.Repl.Core` (`SessionLogErrorEnvelope`)
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-004

**Command Registry and Dispatcher** — Workflow handlers SHALL implement typed interfaces (`ITodoWorkflow`, `ISessionLogWorkflow`, `IRequirementsWorkflow`, `IGenericClientPassthrough`) with async operation methods. Command dispatch SHALL resolve workflow instances from DI per invocation and SHALL pass deserialized parameters as strongly typed method arguments via YamlDotNet model binding. Command routing SHALL map YAML method names to workflow operations.

**Covered by:** `McpServer.Repl.Core` (`ITodoWorkflow`, `ISessionLogWorkflow`, `IRequirementsWorkflow`, `IGenericClientPassthrough`), `McpServer.Repl.Host` (`TodoWorkflow`, `SessionLogWorkflow`, `RequirementsWorkflow`, `GenericClientPassthrough`)
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-005

**Namespace Organization and Handler Parity** — Command names SHALL use dot-delimited namespaces: `workflow.todo.*`, `workflow.session.*`, `workflow.requirements.*`, `client.*`. Handler implementations SHALL delegate to existing client contracts (`TodoClient`, `SessionLogClient`, `RequirementsClient`, `ContextClient`, `RepoClient`, `DesktopClient`) without duplicating business logic. Workflows SHALL maintain stateful context (TODO selection, session state) within the REPL process.

**Covered by:** `McpServer.Repl.Core` (`TodoCommandShapes`, `SessionLogCommandShapes`, `RequirementsCommandShapes`, `ClientCommandShapes`), `McpServer.Repl.Host` (`TodoWorkflow`, `SessionLogWorkflow`, `RequirementsWorkflow`, `GenericClientPassthrough`)
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-006

**Trust Bootstrap and Token Validation** — The REPL host SHALL implement marker-file trust bootstrap with signature verification and health nonce challenge before accepting operational commands. API key authentication SHALL use per-workspace token semantics from marker files. The host SHALL detect API key rotation between commands via marker file watch and SHALL emit warnings when tokens become stale. Trust verification SHALL use the same contract as PowerShell modules.

**Covered by:** `McpServer.Repl.Core` (`ITrustBootstrapService`, `IMarkerFileReader`, `IAuthRotationHandler`), `McpServer.Repl.Host` (`AgentStdioHandler`)
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-007

**State Query Commands** — The REPL host SHALL expose commands for querying workspace state via generic client passthrough: context search, repository operations, desktop launch validation, and requirements operations. Handlers SHALL query current service state snapshots through typed client interfaces without blocking on long-running operations. All client operations SHALL support the generic passthrough pattern for extensibility.

**Covered by:** `McpServer.Repl.Core` (`IGenericClientPassthrough`, `ClientCommandShapes`), `McpServer.Repl.Host` (`GenericClientPassthrough`)
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-008

`MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(workspacePathOverride, markerPathOverride, out options, out error)` returns success/failure plus a human-readable diagnostic. The diagnostic enumerates every directory walked, names the marker file when found, and distinguishes "not found" from "malformed" and "signature mismatch". `FindMarkerFile(startPath, out searchedPaths)` exposes the same path list for callers that want raw enumeration. The legacy parameterless `Resolve()` remains for back-compat.
**Covered by:** FR: FR-MCP-REPL-007; TEST: TEST-MCP-REPL-007-1, TEST-MCP-REPL-007-2, TEST-MCP-REPL-007-3
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-009

**Explicit agent identity propagation** — REPL host, plugin launchers, and verified marker cache resolution must use a single agent identity precedence and canonical cache key so Codex, Claude, Grok, and other agents cannot share stale trust or cache state accidentally.
**Covered by:** FR: FR-MCP-REPL-008; TEST: TEST-MCP-REPL-018, TEST-MCP-PLUGIN-012, TEST-MCP-PLUGIN-013
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-010

**Independent REPL session-log persistence strategies** — McpServer.Repl.Core SHALL define separate primary MCP and filesystem failsafe session-log persistence strategies plus a failover coordinator. REPL session-log persistence calls SHALL route through the coordinator, suppress degraded notifications for non-terminal plugin operations after durable fallback, and return terminal persistence details. The failsafe strategy SHALL atomically write a replayable session-log envelope to the V4 workspace-and-agent-scoped pending path.
**Covered by:** FR: FR-MCP-REPL-009; TEST: TEST-MCP-REPL-025, TEST-MCP-REPL-026, TEST-MCP-REPL-027, TEST-MCP-REPL-028
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-011

**PascalCase session-id agent + openSession persistence** — Composed plugin session ids SHALL use a PascalCase source-type agent segment (never lowercase 'default' or a lowercase host key) so they satisfy the server sessionId regex ^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$; and workflow.sessionlog.openSession SHALL persist an explicit valid sessionId into session-state.yaml instead of being a success no-op. Acceptance Criteria: (AC1) Get-ReplCanonicalAgentName maps 'default' to 'Default' (regex-valid), 'claude-code'/'claudecode' to 'ClaudeCode', 'codex' to 'Codex', 'grok' to 'GrokCode', and its output always matches ^[A-Z][A-Za-z0-9]*$; New-ReplPluginSessionId composes the agent segment through it; (AC2) Invoke-WorkflowOpenSession given an explicit valid sessionId writes status=verified and that sessionId into the root session-state.yaml and returns true when the root records no session or the same id (SessionIdCanonicalAgent.Tests.ps1, TEST-MCP-REPL-026); when the root records a different session, or the call marks the session background, child or sessionKey, it writes them into sessions/<sessionId>/session-state.yaml under the cache folder, leaves the root file unchanged and returns true (TriagePluginIdentity.Tests.ps1, the child-session case) (amended 2026-10-15, PLAN-SESSIONTRIAGEFIX-001 D10, operator decision "Fix them in this plan": the earlier text named only the root path). Origin: BUG-TRIAGE-085 (AgentName fell back to lowercase 'default' producing server-rejected ids; openSession never persisted an explicit id). Applied to plugins/core/lib-ps/repl-invoke.ps1 (canonical) and mcpserver-claude-code-plugin/lib; do not relax server validation. Validated by TEST-MCP-REPL-011.
**Covered by:** FR: FR-MCP-REPL-009; TEST: TEST-MCP-REPL-025, TEST-MCP-REPL-026, TEST-MCP-REPL-027, TEST-MCP-REPL-028
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-012

**Per-method REPL invocation timeout** — REPL invocation timeouts SHALL be resolved per method: long-running requirement/agent methods (workflow.todo.analyzeRequirements, workflow.requirements.generateDocument/ingestDocument/analyze*, client.Requirements.Analyze*) SHALL use an extended, env-configurable budget (REPL_LONG_TIMEOUT, default 300s) while all other methods keep the short default (REPL_TIMEOUT, default 30s). Acceptance Criteria: (AC1) Get-ReplMethodTimeoutSeconds returns >30 for analyzeRequirements/generateDocument and 30 for sessionlog methods; (AC2) REPL_TIMEOUT overrides the short default and REPL_LONG_TIMEOUT overrides the long budget; Invoke-ReplRaw uses Get-ReplMethodTimeoutSeconds. Origin: BUG-TRIAGE-072 (workflow.todo.analyzeRequirements, which blocks on an external Copilot CLI, was killed at the flat 30s REPL_TIMEOUT). Applied to plugins/core/lib-ps/repl-invoke.ps1 (canonical) and mcpserver-claude-code-plugin/lib. Validated by TEST-MCP-REPL-027.
**Covered by:** FR: FR-MCP-REPL-009; TEST: TEST-MCP-REPL-025, TEST-MCP-REPL-026, TEST-MCP-REPL-027, TEST-MCP-REPL-028
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Inside a hook, every REPL call's timeout is the lesser of its per-method timeout and the whole seconds left before the hook's deadline, and a call is not started when less than one second is left (it fails as timed out, so a persist keeps its write-ahead failsafe record as the fallback); the deadline is the hook process's start time plus the timeout the plugin's generated hooks.json registers for that hook, less a three-second margin for writing the hook's result; a hook with no registered timeout, and every call outside a hook, keeps the per-method timeouts unchanged. The deadline belongs only to a hook running in a process started for that hook (the process's command line names the hook's entry script under the plugin root, or the running plugin-hook.ps1 itself): a hook script run inside a process started for something else publishes no deadline, and every generated hook entry leaves MCP_HOOK_DEADLINE_UTC_MS and MCP_MEMORY_REPL_TIMEOUT_SECONDS as it found them when it returns, so calls made later in the same process keep their per-method timeouts (PLAN-SESSIONTRIAGEFIX-001 D1, triage-report-124a374c; operator decision 2026-10-15 "Scope D1 to the hook process", after the plugin repositories' own ReplMethodTimeout and MemoryInjection tests failed on a deadline left in their Pester process; DeadlineAssertion_AcceptsMarginAndRejectsNoMargin, TimeoutCapAssertion_AcceptsCapAndRejectsUncapped, HookOwnershipAssertion_AcceptsOwnedEntriesAndRejectsForeignOrOtherHook, HookEnvironmentAssertion_AcceptsRestoredAndRejectsLeakedOrRemoved, HookDeadline_RegisteredTimeoutLessMargin_FromProcessStart and HookDeadline_NoRegisteredTimeout_NoDeadline (each run in a process started for the hook), HookOwnership_ShippedFunction_PinnedCommandLines, HookDeadline_ForeignProcess_PublishesNothing, HookEntry_InProcess_RestoresDeadlineAndMemoryTimeout, ReplTimeout_InsideHook_CappedByTimeLeft, ReplTimeout_OutsideHook_Unchanged).
- [ ] A UserPromptSubmit hook whose server calls do not answer attempts its server call and returns before its registered timeout with the turn opened degraded and the write-ahead failsafe record kept, also when it must first supersede an in-progress turn; proven at the production UserPromptSubmit timeout of 10 s (PLAN-SESSIONTRIAGEFIX-001 D1, triage-report-124a374c; BudgetAssertion_AcceptsDegradedWithinBudgetAndRejectsOverrun, UserPromptSubmit_UnansweredServer_OpensDegradedTurnWithinBudget).

## TR-MCP-REPL-013

**Marker-bearing current directory outranks inherited workspace env for repl resolution** — A marker-bearing current directory SHALL outrank inherited workspace environment variables when the repl bridge resolves the active workspace, so a hook launched from the correct repo binds to that repo's marker rather than a stale inherited MCP_WORKSPACE_PATH. AC1: with a marker at cwd and a conflicting MCP_WORKSPACE_PATH, resolution returns cwd. Guards BUG-TRIAGE-077 via TEST-MCP-REPL-028 (ReplWorkspaceResolution.Tests.ps1).
**Covered by:** FR: FR-MCP-REPL-009; TEST: TEST-MCP-REPL-025, TEST-MCP-REPL-026, TEST-MCP-REPL-027, TEST-MCP-REPL-028
**Status:** pending
Scope: layer-1+

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
**Covered by:** FR: FR-MCP-REPL-011; TEST: TEST-MCP-REPL-031, TEST-MCP-REPL-032, TEST-MCP-REPL-033, TEST-MCP-REPL-034, TEST-MCP-REPL-035, TEST-MCP-REPL-036, TEST-MCP-REPL-037, TEST-MCP-REPL-038, TEST-MCP-REPL-039
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] An invocation whose backend call succeeded drains the queue after the invoked method completes and its output is written, including an invocation that makes only one backend call; the drain never runs between two backend calls of one method, so beginTurn persists the superseded turn and the new turn before any queued record is replayed (PLAN-SESSIONTRIAGEFIX-001 D1, triage-report-124a374c; ReplayOrderAssertion_AcceptsDrainLastAndRejectsDrainBetweenCalls, Drain_SingleCallInvocation_RunsAfterMethodOutput, Drain_BeginTurn_ReplaysQueueOnlyAfterTurnPersists).
- [ ] Inside a hook the drain starts a record only while at least one second remains before the hook's deadline and caps each replay at the time left; records it does not reach stay queued with their attempt counts unchanged (PLAN-SESSIONTRIAGEFIX-001 D1, triage-report-124a374c; DrainDeadlineAssertion_AcceptsStopAndRejectsDrainPastDeadline, Drain_InsideHookPastDeadline_LeavesRecordsQueuedUnattempted).

## TR-MCP-REPL-017

**Failsafe quarantine and truthful pending queue depth in plugin status** — BUG-TRIAGE-097. A failsafe record that cannot be replayed MUST be quarantined rather than retried forever or deleted, because a captured record can be the only copy of a turn. plugins/core/lib-ps/repl-invoke.ps1 MUST move a record to a quarantine subdirectory of the failsafe directory, next to a sibling .reason.txt naming the quarantine timestamp, the original path, and the reason, when the record is not readable YAML, when its root is not a mapping, when it has no method or no params, or when its drainAttempts counter has reached the attempt budget (default 5). Quarantined records MUST NOT be counted as live queue depth. plugins/core/lib-ps/resolve-cache-dir.ps1 MUST own the single resolution of the queue location (Get-McpFailsafeDir and Get-McpFailsafeQuarantineDir, honouring MCPSERVER_FAILSAFE_DIR then MCP_FAILSAFE_DIR then the workspace cache) so the writer, the drain, and the status reporter cannot disagree. plugins/core/lib-ps/mcp-status.ps1 MUST count the failsafe directory: pendingCount previously counted only the cache pending directory and therefore reported 0 while 33 captured submits sat undrained, so pendingCount MUST be the sum of pending turn records and queued failsafe records, and the status document MUST additionally expose pendingTurnCount, failsafeDir, failsafeCount, and failsafeQuarantineCount. A read-only workflow.failsafe.status verb MUST report the same depth without replaying anything.
**Covered by:** FR: FR-MCP-REPL-011; TEST: TEST-MCP-REPL-031, TEST-MCP-REPL-032, TEST-MCP-REPL-033, TEST-MCP-REPL-034, TEST-MCP-REPL-035, TEST-MCP-REPL-036, TEST-MCP-REPL-037, TEST-MCP-REPL-038, TEST-MCP-REPL-039
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
**Acceptance Criteria:**
- [ ] The supersede path chooses the canceled turn's title from the local cache alone, with no server read: a refined local title is sent, and a raw or empty local title (the prompt's first line or 'User prompt') is omitted, so the server keeps its stored title under the additive turn merge (FR-SUPPORT-015); raw prompt text is never sent (PLAN-SESSIONTRIAGEFIX-001 D1, triage-report-124a374c; SupersedeTitleAssertion_AcceptsLocalOnlyAndRejectsServerRead, SupersedeTitle_LocalCacheOnly_NoServerRead, 'TEST-MCP-REPL-040 supersede keeps a locally refined title without a server round trip', 'TEST-MCP-REPL-040 supersede omits a raw prompt-derived local title without a server read', 'TEST-MCP-REPL-040 supersede omits the title when the local default is raw and the server has none').

## TR-MCP-REPL-020

**failTurn resolves the active turn from the plugin cache** — Implements FR-MCP-143 and closes BUG-TRIAGE-099. Plugin Status reports hasSession and hasCurrentTurn from the local cache (session-state.yaml carrying verified plus a non-empty sessionId, and the presence of current-turn.yaml), and the beginTurn path writes those files, but failTurn requires an empty in-process REPL state and otherwise returns "No active session exists". The result is that a turn the plugin reports as active cannot be closed as failed, so a wrap-up cannot truthfully record a validation failure and the turn is left in progress forever. failTurn SHALL resolve the active session and turn the same way the rest of the plugin does, falling back to the cache when the in-process state is empty, and SHALL close the turn as failed when the cache identifies one. Coverage SHALL prove the current refusal before the fix and a successful fail-close afterwards, using a temporary cache directory.
**Covered by:** FR: FR-MCP-143; TEST: TEST-MCP-194, TEST-MCP-REPL-040
**Status:** pending
Scope: layer-1+

## TR-MCP-REPL-TRIAGE-001

**Triage REPL surface** — REPL parity for triage through client passthrough and typed workflow wrappers.
**Status:** pending
Scope: layer-1+

## TR-MCP-REQ-001

**AI Requirements Analysis Service** — `RequirementsService` invokes `ICopilotClient` with a structured prompt containing the TODO item's title, description, technical details, implementation tasks, and pre-existing FR/TR assignments. The prompt instructs Copilot to identify existing FRs/TRs from `docs/Project/` and create new entries for unaddressed functionality, then emit a JSON block with assigned IDs. Response parsing first attempts structured JSON extraction; falls back to regex (`FR-[A-Z]+-\d{3}` / `TR-[A-Z]+-\d{3}`) for robustness. Discovered IDs are merged (deduplicated, order-preserved) back into the TODO via `ITodoService.UpdateAsync`.
**Covered by:** FR: FR-MCP-023
**Status:** pending
Scope: layer-1+

## TR-MCP-REQ-002

**Requirements Document Management Service** — `RequirementsDocumentService` parses the canonical requirements documents (`Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`) into a strongly typed in-memory model on startup and provides CRUD operations for FR/TR/TEST entries and mapping rows. It renders `Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`, and `Requirements-Matrix.md` for exports. Matrix rendering preserves existing matrix rows and appends missing FR/TR/TEST identifiers so generated exports satisfy traceability validation without discarding hand-maintained status/source metadata.
**Covered by:** `RequirementsDocumentService`, `RequirementsDocumentParser`, `RequirementsDocumentRenderer`, `RequirementsOptions`
**Status:** pending
Scope: layer-1+

## TR-MCP-REQ-003

**Requirements REST + STDIO Tool Integration** — The requirements management feature is exposed over REST via RequirementsController at /mcpserver/requirements/* and over STDIO via MCP tools (requirements_list, requirements_generate, requirements_create, requirements_update, requirements_delete). Document generation supports individual Markdown documents, including `doc=matrix` / `docType=matrix` for `Requirements-Matrix.md`, and `doc=all` workspace exports with canonical filenames including `Requirements-Matrix.md`.
**Covered by:** FR: FR-MCP-040, FR-MCP-041, FR-MCP-042
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] workflow.requirements.getTr and getTest return each record's stored createdAt and updatedAt: the server entry carries the row's CreatedAtUtc and UpdatedAtUtc, the client entry and the REPL item report them, and a record that stores none reports null at the server and empty text in the REPL, never the time of the read. Tests: RequirementsDatabaseDocumentServiceTests.GetById_ReturnsTheStoredTimestamps (QBrainAi.Support.Mcp.Tests); in RequirementsLegacyIdAndGenerateDocumentTests (QBrainAi.Repl.Core.Tests) the mock gate StoredTimestampAssertion_AcceptsStoredValuesAndRejectsReadTime and the binding GetItems_ReportTheStoredTimestamps (PLAN-SESSIONTRIAGEFIX-001 B2, triage-report-b72cfaf4).

## TR-MCP-REQ-004

**Dual Wiki Workspace Renderer** — Requirements document generation SHALL support format=wiki with doc=all, writing both azure/ and github/ folders under docs/Project/wiki and returning workspace export metadata. Each platform folder SHALL include canonical requirements markdown documents, `Requirements-Matrix.md`, and `.mcp-requirements-manifest.json` with generatedAtUtc. Azure Wiki output SHALL include `.order`; GitHub Wiki output SHALL include `_Sidebar.md` and `_Footer.md`. Status: Complete. Covered by `RequirementsWikiDocumentRenderer`, `RequirementsDocumentService`, `RequirementsDatabaseDocumentService`, `RequirementsController`, `RequirementsClient`, `RequirementsWorkflow`, `McpServerMcpTools`.
**Covered by:** FR: FR-MCP-084; TEST: TEST-MCP-106, TEST-MCP-107, TEST-MCP-108, TEST-MCP-109
**Status:** pending
Scope: layer-1+

## TR-MCP-REQ-005

**Wiki Import Selection and Authoritative Sync** — Requirements ingest SHALL accept sourceFormat=auto|canonical|wiki, preferredWikiFormat=azure|github, path-keyed documents, and optional per-document lastModifiedUtc. Wiki import SHALL compare both platform manifest generatedAtUtc values and latest file modified UTC values, fail on disagreement unless a preferred wiki format is supplied, and authoritatively create, update, delete, or ignore FR/TR/TEST/mapping records from the selected folder.
**Covered by:** FR: FR-MCP-084, FR-MCP-LIVE-CODEX-20260603T2014Z, FR-MCP-LIVE-CODEX-20260603T2015Z; TEST: TEST-MCP-106, TEST-MCP-107, TEST-MCP-108, TEST-MCP-109, TEST-MCP-REQACPLUGIN-LIVE
**Status:** pending
Scope: layer-1+

## TR-MCP-REQAC-001

**Acceptance criteria persistence** — Persist requirement acceptance criteria as a nullable AcceptanceCriteriaJson column on RequirementEntity using the existing JSON-column pattern and the shared AcceptanceCriterion type. Provider migrations for SQLite, SQL Server, and PostgreSQL include the column, and create/update/read plus document generation round-trip ordered checklist criteria with checked state and evidence.
**Covered by:** FR: FR-MCP-REQAC-001, FR-MCP-REQAC-002; TEST: TEST-MCP-REQAC-001, TEST-MCP-REQAC-002, TEST-MCP-REQAC-003, TEST-MCP-REQAC-004
**Status:** pending
Scope: layer-1+

## TR-MCP-REQAC-002

**Acceptance criteria markdown rendering** — Render acceptance criteria into Functional/Technical/Testing-Requirements.md; the parser remains tolerant of the block; the database remains authoritative.
**Covered by:** FR: FR-MCP-REQAC-001; TEST: TEST-MCP-REQAC-001, TEST-MCP-REQAC-002, TEST-MCP-REQAC-003
**Status:** pending
Scope: layer-1+

## TR-MCP-REQAC-PLUGIN-001

**Plugin typed request shaping preserves acceptanceCriteria** — Plugin typed-parameter builders and REPL passthrough binding must emit structured acceptanceCriteria without flattening or dropping nested boolean/list fields.
**Covered by:** FR: FR-MCP-REQAC-PLUGIN-001; TEST: TEST-MCP-REQAC-PLUGIN-BASH, TEST-MCP-REQAC-PLUGIN-TS
**Status:** pending
Scope: layer-1+

## TR-MCP-REQACPLUGIN-001

**Plugin-side schema + shaper changes for AcceptanceCriteria** — Bash plugins gain _repl_emit_acceptance_criteria_block helper and per-method emit/hydrate calls in _repl_requirements_typed_params for createFr/createTr/createTest/updateFr/updateTr/updateTest. TS plugins gain shared AcceptanceCriterion JSON schemas and typedParams pass-through for the same six methods plus per-kind/mixed batch records items.
**Covered by:** FR: FR-MCP-REQACPLUGIN-001; TEST: TEST-MCP-REQACPLUGIN-BASH, TEST-MCP-REQACPLUGIN-LIVE, TEST-MCP-REQACPLUGIN-TS
**Status:** pending
Scope: layer-1+

## TR-MCP-REQACPLUGIN-002

**Plugin-side AcceptanceCriteria capture verification** — Bash and TypeScript plugin requirement mutation dispatchers SHALL reject successful-looking create/update responses that explicitly show an empty acceptanceCriteria list when the caller supplied a non-null criteria array, while preserving backward compatibility for responses that omit the field.
**Covered by:** FR: FR-MCP-REQACPLUGIN-002; TEST: TEST-MCP-REQACPLUGIN-002
**Status:** pending
Scope: layer-1+

## TR-MCP-REQEXPORT-001

**Wiki requirement document renderer emits Markdown sections for TEST descriptions and AC** — The requirements wiki renderer shall preserve TEST grouping while emitting each TEST requirement as a heading with description text and a nested Acceptance Criteria checklist generated from the structured acceptanceCriteria field.
**Covered by:** FR: FR-MCP-112; TEST: TEST-MCP-152, TEST-MCP-REQEXPORT-002, TEST-MCP-REQEXPORT-003, TEST-MCP-REQWS-001
**Status:** pending
Scope: layer-1+

## TR-MCP-REQEXPORT-002

**Wiki generate surfaces real exception, never opaque 500** — The requirements generate endpoint (GET /mcpserver/requirements/generate?format=wiki) SHALL surface the real exception - message, exceptionType, stage, details - as a structured HTTP 500 for any failure outside the pre-existing catch list (RequirementsConflictException/ArgumentException/InvalidOperationException/UnauthorizedAccessException/IOException); no wiki generation failure may reach the global exception middleware and return an opaque internal_server_error. Acceptance Criteria: (AC1) an exception type outside the listed set (e.g. KeyNotFoundException) returns a structured 500 body naming exceptionType and the message; (AC2) successful wiki export still returns the ZIP; (AC3) the existing structured 400/409 paths (config load, transaction, zip assembly) are unchanged. Origin: BUG-TRIAGE-073 (wiki branch had no catch-all; yaml/all path succeeds while wiki/all 500s opaquely in some workspaces). Validated by TEST-MCP-REQEXPORT-002.
**Covered by:** FR: FR-MCP-112; TEST: TEST-MCP-152, TEST-MCP-REQEXPORT-002, TEST-MCP-REQEXPORT-003, TEST-MCP-REQWS-001
**Status:** pending
Scope: layer-1+

## TR-MCP-REQEXPORT-003

**Requirements generateDocument accepts markdown for all non-wiki docTypes** — generateDocument SHALL accept format=markdown for every non-wiki docType (functional, technical, testing, mapping, matrix) at the JSON schema, ReplYamlMessageValidator, and RequirementsWorkflow layers. AC1: a call {format:markdown,docType:matrix} returns markdown content, not a format rejection. AC2: no layer restricts format to {yaml,wiki}. Encodes BUG-TRIAGE-074 (non-reproducing) so a future enum narrowing fails TEST-MCP-REQEXPORT-003.
**Covered by:** FR: FR-MCP-112; TEST: TEST-MCP-152, TEST-MCP-REQEXPORT-002, TEST-MCP-REQEXPORT-003, TEST-MCP-REQWS-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ReplYamlMessageValidator accepts workflow.requirements.generateDocument with every documented format (markdown, yaml, wiki) combined with every documented docType (fr, tr, test, matrix, all, functional, technical, testing, mapping), and refuses an undocumented format or docType with one error that names its payload.params path and lists every documented value (PLAN-SESSIONTRIAGEFIX-001 B3, triage-report-c03d190f).

## TR-MCP-REQEXPORT-004

**Requirements generateDocument honors explicit workspacePath override end-to-end** — generateDocument SHALL honor an explicit workspacePath override end-to-end; a cross-workspace call without that workspace's API key fails 401 and never silently exports the session-bound workspace. AC1: an explicit workspacePath with a valid key returns that workspace's requirements. AC2: an unauthorized cross-workspace call returns 401, not a silent fallback to the session-bound workspace. Guards BUG-TRIAGE-078 via TEST-MCP-REQWS-001.
**Covered by:** FR: FR-MCP-112; TEST: TEST-MCP-152, TEST-MCP-REQEXPORT-002, TEST-MCP-REQEXPORT-003, TEST-MCP-REQWS-001
**Status:** pending
Scope: layer-1+

## TR-MCP-REQRECOVERY-001

**Transactional requirements recovery service and shared plugin contract** — A typed recovery service shall validate a complete mixed-operation plan before opening one provider transaction, enforce optimistic preconditions and persistent idempotency, apply dependency-safe record and mapping changes, and expose one canonical schema through the server, client, REPL, and supported plugins.
**Covered by:** FR: FR-MCP-REQRECOVERY-001; TEST: TEST-MCP-REQRECOVERY-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The service canonicalizes and validates all IDs, scopes, acceptance criteria, operation uniqueness, delete targets, and final referential integrity before the first mutation.
- [ ] One database transaction contains requirement rows, acceptance-criteria rows, mappings, deletions, idempotency state, and the recovery audit row; rollback restores the exact pre-state on every provider.
- [ ] The effective-state hash is compared immediately before mutation, and the idempotency key is reserved and committed in the same transaction. Competing plans cannot both commit against one pre-state.
- [ ] The operation planner orders mapping removals before target deletes and validates the final graph before commit while preserving canonical requirement and acceptance-criterion identifiers.
- [ ] Server, typed client, REPL schema, shared plugin core, and every generated or copied plugin surface derive from one canonical request and result contract using object-first serialization.
- [ ] The canonical precondition envelope includes workspace ID, target layer key, productScope, layer-catalog version or hash, scopeStartLayerKey, scopeEndLayerKey, sorted raw target-layer records including shadowed rows, and the sorted effective projection. Canonical UTF-8 JSON and SHA-256 produce deterministic plan and state hashes.
- [ ] Inside the serializable provider transaction, the service locks or concurrency-checks the target layer and catalog metadata, revalidates raw and effective hashes, and rejects mutation of records not owned by the declared target layer or outside the declared scope bounds.

## TR-MCP-REQSCOPE-001

**TR-MCP-REQSCOPE-001** — Placeholder requirement backfilled for TODO link TR-MCP-REQSCOPE-001.
**Covered by:** FR: FR-MCP-REQSCOPE-001
**Status:** pending
Scope: layer-1+

## TR-MCP-REQSCOPE-002

**TR-MCP-REQSCOPE-002** — Placeholder requirement backfilled for TODO link TR-MCP-REQSCOPE-002.
**Covered by:** FR: FR-MCP-REQSCOPE-002
**Status:** pending
Scope: layer-1+

## TR-MCP-REQSCOPE-003

**TR-MCP-REQSCOPE-003** — Placeholder requirement backfilled for TODO link TR-MCP-REQSCOPE-003.
**Covered by:** FR: FR-MCP-REQSCOPE-003
**Status:** pending
Scope: layer-1+

## TR-MCP-REQSCOPE-004

**TR-MCP-REQSCOPE-004** — Placeholder requirement backfilled for TODO link TR-MCP-REQSCOPE-004.
**Covered by:** FR: FR-MCP-REQSCOPE-004
**Status:** pending
Scope: layer-1+

## TR-MCP-RUNAS-001

**Proposed helper-only Unix worker, transient cgroup and journaled updater** — # Linux Grok RunAs worker

## Status and exact goal

Proposed architecture, October 6, 2026. No worker implementation or deployment is claimed.

The operator's goal is a completed Agent Helper answer using deployed defaults,
with the actual Grok process running as `sharpninja`. The MCP process remains
`mcpserver`, with `NoNewPrivileges=true`. Merely setting HOME, binding options,
launching Grok, or obtaining an echo fallback does not satisfy the goal.

Authoritative proposed traceability captured October 6, 2026 through the active
Cline PowerShell plugin: FR-MCP-RUNAS-001, TR-MCP-RUNAS-001 and
TEST-MCP-RUNAS-001. Supported Get/effective readbacks verify status proposed,
seven structured AC1-AC7 on each record, all isSatisfied=false, and the mapping.
This is requirements capture only, not implementation or deployment authorization.
Explicit operator approval of the architecture remains outstanding.

Durable proof (workspace-relative paths):
- docs/receipts/runas-f6/id-preflight.json - IDs absent before create; existing dependencies verified.
- docs/receipts/runas-f6/readback-Fr.yaml
- docs/receipts/runas-f6/readback-Tr.yaml
- docs/receipts/runas-f6/readback-Test.yaml
- docs/receipts/runas-f6/readback-mapping.yaml
- docs/receipts/runas-f6/effective-after.yaml
- docs/receipts/runas-f6/verification.json
- docs/receipts/runas-f6/session-proof.yaml

The RUNAS FR maps to RUNAS TR/TEST, TR-MCP-HELP-001/010,
TEST-MCP-HELP-004/005, TR-MCP-SERVICEUPDATE-001 and
TEST-MCP-SERVICEUPDATE-001. Existing FR-MCP-HELP-001/010/011 and
FR-MCP-SERVICEUPDATE-001 are linked in record notes; their mappings/statuses
are unchanged. The mapping schema supports TR/TEST IDs, not FR dependencies.
Canonical docs/Project export was attempted through the supported client but
returned access denied; no direct-store edit, privilege bypass or retry.
Failure reported as triage-report-6875b680969f4ebb9a777c7ddf422ad2 with
failsafe and accepted triage receipts under docs/receipts/runas-f6/.

## Observed failure and boundary

The present Grok strategy uses `IProcessSpawner` in the server process.
`GrokCliAgentExecutionStrategy.RunPromptAsync` creates a server-owned prompt
file before spawning. Configurable RunAs cannot itself transfer Unix identity.
The existing service's `NoNewPrivileges=true` must not be weakened to permit
setuid sudo delegation. Authentication files must not be copied to mcpserver.

Source anchors:

- `/home/sharpninja/github/McpServer/src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs`:
  `RunPromptAsync`, `BuildStartInfo`, `BuildGrokArgumentList`.
- `/home/sharpninja/github/McpServer/src/McpServer.Services/Services/AgentHelp/AgentHelpConversationService.cs`:
  `BuildAgentCliOptions` and `ExecuteHelperAsync`.
- `/home/sharpninja/github/McpServer/src/McpServer.Common.AgentCli/ProcessEnvironmentService.cs`:
  Linux environment behavior requires regression coverage, not identity claims.
- `/etc/systemd/system/mcpserver.service`: current service identity and hardening.

## Decision and authorization

Introduce a separate .NET worker running as sharpninja, reachable only by a
restricted Unix socket. This adds an administrator-installed service and is an
architectural change. Require explicit operator approval of this design before
product implementation. `fix it` authorizes remediation, but does not silently
approve every cross-account capability in a new architecture.

Rejected alternatives:

- HOME-only changes: do not change UID or securely grant credential access.
- Running the entire MCP server as sharpninja: unnecessarily broad identity change.
- Disabling NoNewPrivileges and permitting sudo: weakens existing server hardening.
- Copying API keys or login caches: duplicates private authentication material.

Security consequence: an authorized mcpserver process can ask an agent to act
with sharpninja's access to the allowed workspace. The worker is NOT a sandbox
for model tool calls. Socket authentication and workspace restrictions reduce
exposure but cannot eliminate this delegated trust. Operator approval includes
this consequence. Do not advertise arbitrary-code isolation.

## Scope and compatibility

- Add nullable `AgentHelp:RunAs` and `AgentHelp:WorkerSocketPath`, with matching
  `AgentCliClientOptions` routing information where necessary.
- Preserve existing `TodoPromptOptions.RunAs` fallback when AgentHelp.RunAs is
  unset. Explicit AgentHelp.RunAs takes precedence. No session-level identity
  override and no untrusted network selection of worker users.
- Linux nonempty RunAs must use the configured worker, fail closed on identity
  mismatch/unavailability, and never silently execute under the server UID.
- Empty Linux RunAs retains current local Grok execution. Windows retains its
  existing profile/process-launch behavior.
- Initially support Agent Helper one-shot Grok execution only. Voice, TODO,
  triage, persistent QuadBrain sessions, other strategies, and brain-slot
  migration are not silently included in this slice.
- No public HTTP response schema break. New deployment components and optional
  settings are additive. Existing Linux users relying on ineffective RunAs
  now receive a clear failure rather than false impersonation.

## Worker interface

Use built-in .NET Unix-domain sockets and System.Text.Json, not new libraries.
New project: `/home/sharpninja/github/McpServer/src/McpServer.GrokWorker/`.
Share versioned transport contracts in the existing Common.AgentCli project.
Add a dedicated `IAgentHelpExecutionRouter`, injected only into
AgentHelpConversationService. Do not select the worker inside the shared Grok
strategy, or infer helper purpose from generic AgentCliClientOptions.RunAs.
The helper router selects remote one-shot execution only for Linux, nonempty
effective helper RunAs, and the grok-cli strategy. It rejects another selected
strategy when RunAs is configured, including explicit session overrides.
Local helper execution still delegates to the existing strategy resolver.
Voice, TODO, triage and persistent sessions never receive this router; tests
exercise each excluded caller with generic RunAs set and prove no worker call.
This slice does not fix identity switching in those excluded callers, and must
not claim that it does. No shell, sudo, pkexec, or arbitrary command string is
accepted by the protocol.

One connection owns one invocation. Four-byte unsigned big-endian length
followed by UTF-8 JSON frames; protocol version 1; reject malformed lengths,
unknown versions, duplicate invocation IDs and unknown fields. Initial request:
invocation UUID, requested user, prompt text, workspace identifier, optional
model and requested timeout. Client cannot supply executable, prompt path,
HOME, environment map, arbitrary CLI flags, UID/GID, or filesystem outputs.

Reply events: started (actual UID, account, worker PID, child PID), stdout,
stderr, and exactly one terminal result (exit code, error category, canceled
flag, cleanup success). Correlate every event with invocation UUID. Preserve
stdout/stderr callbacks and AgentCliResult semantics. Logs include correlation
and identity, not prompts, tokens or full authentication-related stderr.

Enforce limits before allocation: initial request <= 1 MiB, output event <=
64 KiB, accumulated stdout and stderr <= 4 MiB each, one active invocation,
handshake <= 10 seconds, effective run timeout <= 120 seconds. UTF-8 byte
limits are tested with multibyte input. Excess output terminates the process
and returns an explicit resource-limit error, never a completed answer.

Client disconnect, cancellation, timeout, EndAsync and DisposeAsync all close
the invocation and require worker cancellation of the whole child process tree.
No detached background job survives an invocation. Use process-tree kill,
bounded drain/wait, and systemd KillMode=control-group on worker shutdown.
Lifecycle tests must prove cleanup; a timeout is not treated as completion.

## Revision 2: decisions addressing independent review F1-F5

This revision is a design proposal, not approval or implemented behavior.
The original failed review remains immutable. F6 authoritative requirement
persistence is now evidenced by the supported readbacks above; the canonical
markdown export is separately blocked by access denied. U1-U3 and approval,
implementation and deployed-helper acceptance remain unverified.

### F2: per-invocation OS lifetime, not ancestry-only kill

Replace the preceding process-tree-kill proposal with a dedicated transient
service in sharpninja's systemd user manager for each invocation. Use the
installed systemd-run executable via ArgumentList, with fixed --user, --pipe,
--wait, --quiet, --service-type=exec, --expand-environment=no, and generated UUID
unit name. Never combine --pipe with --remain-after-exit. The fixed command is
the root-owned published worker executable in invocation-runner mode, not Grok
directly. No client may choose unit names, commands, modes or unit properties.
The runner launches only the policy-pinned Grok with existing argument contract.
It is the unit main process and remains alive until explicitly stopped; Grok is
its child. Configure ExitType=main, RemainAfterExit=no, KillMode=control-group,
SendSIGKILL=yes, TimeoutStartSec=10s, TimeoutStopSec=5s, Restart=no,
RuntimeMaxSec=(requested timeout + 15 seconds), NoNewPrivileges=yes and
CollectMode=inactive. The runner never calls sd_notify to extend timeouts.

Handshake and capture protocol, including immediate Grok exits:
1. Before launch persist UUID/unit name, worker epoch, private prompt directory,
   launch monotonic timestamp and deadline. Refuse a pre-existing unit. The
   runner receives only generated identifiers and the root-owned policy path;
   prompt/config data travel over framed redirected stdin, not command arguments.
2. The runner validates its actual UID, UUID and /proc/self/cgroup, sends a ready
   frame containing PID, UID and canonical cgroup path, and blocks for start ACK.
   The worker queries manager MainPID, InvocationID and ControlGroup, checks the
   ready frame matches the actual active unit, persists the matching identity,
   then sends start plus validated prompt/model/deadline. NO Grok process may be
   launched before this handshake. Runner startup/no-ACK deadline is 10 seconds.
   Missing/pruned cgroup or unit before ACK is a failed launch, never success.
3. Runner creates private prompt files, launches Grok, drains stdout and stderr
   concurrently into bounded frames and waits for actual Grok exit. It reports
   the Grok exit code separately from the runner/systemd-run exit. Following
   child exit it stays alive, continuously awaiting stop or local deadline; this
   pins its cgroup and keeps RuntimeMaxSec effective in running state. Descendant
   output pipes never gate exit observation; bounded drains continue independently.
4. Worker persists the correlated child-exit frame then stops the exact unit
   through systemctl --user. The runner deliberately does not self-exit on normal
   child success before stop, and there is no active-exited retention dependency.
   SIGTERM/SIGKILL may terminate it during cleanup; manager or launcher exit codes
   caused by this intentional stop are not substituted for the Grok exit code.
5. Wait for stop, bounded launcher exit and stream drain; verify the previously
   observed cgroup has populated=0 recursively or its canonical directory no
   longer exists after completed stop. Directory absence is not success unless
   identity was captured before ACK and stop completed; PID/EOF/unit-not-found
   alone are not proof. Read errors fail cleanup. Worker removes its owned private
   prompt directory after verified stop, including crash paths. Persist receipts
   before accepting child status 0 plus FINAL ANSWER as completed.

Starting, queries and ambiguous launch are bounded: ready/ACK budget 10 seconds
from launch; every manager query <=2 seconds; local child deadline starts at ACK
and is <=120 seconds; stop/cgroup confirmation <=10 seconds; final drain <=5
seconds. On an ambiguous launcher/start/query timeout, cancel the launcher and
stop the persisted exact unit name (even if PID unknown), verify no pending start
job and absent/empty unit cgroup before admission resumes. If manager observation
fails, report failure and become unhealthy; killing systemd-run is not cleanup.
An unresolved manager job prevents successful cleanup. Recovery sweeps generated
units and pending jobs before listening. No completion without identity evidence.

Independent crash bound: Type=exec start budget 10s, running budget <=135s,
stop budget 5s: <=150 seconds from unit start-job execution, assuming a responsive
manager/kernel. Queued job scheduling is not a hard real-time bound; no ACK means
no Grok, and startup recovery handles the persisted pending job. The runner holds
the running state after Grok exits, so manager expiry still stops all cooperative
descendants if the worker dies after child exit. If runner dies, ExitType=main
starts cgroup shutdown immediately. User manager failure is outside independent
timer guarantees, fails health and requires operator recovery; never claim cleanup.
Transport EOF also triggers runner cancellation, but is not the sole guarantee.

Red/integration coverage must exercise hundreds of immediate child zero/nonzero
exits, pre-ACK failures, delayed status observation, leftover descendants and
inherited pipes, worker crash after child exit, runner crash, start/query hangs,
ambiguous jobs, manager loss, unreadable cgroups and output overflow. Record UID,
matching pre-ACK cgroup, child status and actual bounded cleanup independently.

Runner control transport uses the same bounded framing as the worker socket,
with internal ready/start/child-exit types and strict UUID/epoch correlation.
Only runner stdout carries framed events; Grok stdout/stderr are redirected by
the runner and encoded separately, never inherited directly into control stdout.
Drain systemd-run stderr independently as bounded launcher diagnostics; --quiet
does not make stderr a control channel. Runner verifies parent-issued start
frame exactly once, rejects unknown fields and applies independent startup,
child-run and frame deadlines. The worker must observe both actual Grok UID and
runner UID in started receipts; runner UID alone is not process-identity proof.
Output caps count actual UTF-8 payload bytes independently of frame overhead.
Launcher disconnect/EOF before start causes runner exit without Grok; after start
it requests cancellation and relies on unit shutdown as the containment boundary.

The worker continuously drains both pipe streams into bounded queues independent
of the client. On cancellation or transport loss it calls systemctl --user stop
for the exact generated unit and waits at most 10 seconds for stop plus empty
cgroup. At most 5 seconds may be spent on output drain. Cleanup failure returns
an error and makes the worker unhealthy; no next invocation is admitted.
Never interpret disappearance of the root PID, EOF or launcher exit alone as
proof of full cleanup. Terminal success requires the recorded Grok exit status,
both stream limits respected, the invocation cgroup empty and private files gone.

On worker crash the user manager independently enforces the runner RuntimeMaxSec plus the
stop timeout. Cleanup after crash is therefore bounded, not instantaneous. A
replacement worker sweeps only its generated-prefix units and own prompt dirs
before listening. First-install preflight must verify cgroup v2, user-manager
connectivity and permission to start/query/stop transient units without sudo.
Linger is currently disabled on this system. Nuke must snapshot and enable
sharpninja lingering, preserve user-manager ownership, and restore its prior
state on rollback; this is an explicit additional deployment side effect.

Containment is a lifecycle mechanism for cooperative Grok subprocesses, not a
sandbox preventing a malicious sharpninja process from requesting other units
or moving itself. Authentication and executable trust remain as disclosed above.
Integration gates include normal exit with leftover descendants, double fork,
reparenting, inherited pipes, disconnect, timeout, worker kill/restart, manager
unavailability and cleanup failure. Fail a test if a recorded child survives the
specified deadline, even when helper text is otherwise valid.

### F3: connection admission, replay and backpressure

Listener backlog is 8; application admits at most 8 authenticated connections,
including handshake/invocation/reply states, and exactly one active invocation.
There is no invocation queue. An idle capacity-excess or busy peer is closed
immediately; an optional bounded error reply gets at most 1 second. Authenticate
SO_PEERCRED before parsing any bytes. Handshake deadline is 10 seconds measured
from accept, not restarted for partial reads. A frame read or write has a
5-second absolute deadline. Only one outstanding 64-KiB event per connection
is buffered for outbound delivery; stop reading Grok output into unbounded memory.
Stdout/stderr are drained independently with the existing 4-MiB-per-stream cap.
If the outbound consumer stalls, cancel the invocation rather than buffer more.
Terminal reply/drain gets 5 seconds; its failure must not prevent cancellation.

Retain at most 4096 invocation UUIDs for 10 minutes, including failed launches.
Evict expired entries, but never evict a live entry. If capacity remains full,
reject new work. Restart loses replay history; document IDs as per-worker-epoch
at-most-once, not globally exactly-once. Started events include a worker epoch.
Flood, partial-frame, invalid-length, repeated UUID, multibyte-limit and
non-reading-client tests must prove bounded memory, admission and cleanup.

### F4: fail-closed compatibility and authentication preflight

Validate remote-helper inputs before sending any prompt. Reject configured
ModelApiKey, any nonempty session AgentParameters/environment map, and session
AgentPath overrides other than the exact pinned policy path. Do not silently
drop these fields or send secrets across accounts. Error messages name the
incompatible option without its value. An override equal to the pinned path is
accepted; omitted path uses the pinned path. Models are auto or a policy-allowed
ID; reject others. RunAs must resolve to the policy's exact sharpninja UID.
Other strategies with effective RunAs fail closed. Local/Windows options retain
their current semantics and regression tests.

Remote timeouts must be finite and positive, at most 120 seconds. Reject zero,
negative, infinite and over-limit values rather than silently changing semantics.
Environment is server-policy-only: HOME=/home/sharpninja,
XDG_RUNTIME_DIR=/run/user/1000, DBUS_SESSION_BUS_ADDRESS pointing to that user's
bus, and an administrator-fixed PATH containing the verified install and system
binaries. Set these fixed environment values explicitly on EVERY transient
invocation unit through administrator-controlled systemd-run ArgumentList; do
The helper-only slice does not accept optional environment files. Each invocation
sets explicit --setenv=NAME=VALUE arguments for HOME, USER=sharpninja,
LOGNAME=sharpninja, XDG_RUNTIME_DIR, DBUS_SESSION_BUS_ADDRESS and fixed PATH.
The worker's ProcessStartInfo environment only connects systemd-run/systemctl
to the user manager; it does not establish the transient service environment.
Use --property=WorkingDirectory=/home/sharpninja/github/McpServer and
--property=UMask=0077 on every unit. No shell, no argument-variable expansion,
no requester-selected environment names, files or unit properties. Validate
policy values at startup and preserve literal arguments containing dollar signs.
Manager-inherited environment is not claimed to be empty: inventory it during
installation, reject unexpected credential overrides that change authentication,
and unset conflicting XDG_CONFIG_HOME/XDG_DATA_HOME/XDG_CACHE_HOME and API-key
variables on invocation units using root-owned UnsetEnvironment policy. Never
read or copy their values into receipts. Required hook-specific non-secret
settings must be explicitly reviewed and added to policy before preflight.
Preserve normal
Grok authentication files under sharpninja. Preflight executes a read-only actual
Grok answer as that account, including required hooks and network connectivity;
--version alone is insufficient. Record only status, UID, invocation ID and
sanitized failure category. If preflight fails, do not alter MCP configuration.

### F5: journaled Nuke transaction for MCP plus worker

Extend Nuke, not ad hoc copying. Acquire an exclusive host deployment lock before
staging or inspecting mutable live state; refuse a second deployment. Save a
root-owned transaction manifest and flush it atomically before each side effect.
Back up both complete published trees, hashes and deployment manifests; the
MCP/worker service units and drop-ins; the worker socket unit; policy and live
YAML; active/enabled/masked states; socket metadata; and sharpninja's linger state.
Database/data directories are not replaced or rolled back by this transaction.
Quiesce work before the snapshot and leave unrelated data intact.

Persist these states: Prepared -> Quiesced -> WorkerInstalled -> WorkerVerified
-> McpInstalled -> HelperVerified -> Committed. Any exception or interrupted
noncommitted journal enters Recovering and must reach RolledBack or RecoveryFailed.
At each state retain the exact backups and installed hashes needed to reverse
all mutations. The existing updater's configuration-only recovery is explicitly
insufficient; implement and failure-injection-test binary recovery first.

Quiesce by stopping MCP admission/service, stopping worker socket admission, then
worker and outstanding generated invocation units. Confirm empty cgroups before
replacement. Preserve and reject preexisting masked units unless the operator
explicitly authorizes unmasking. Publish/install worker and policy first, reload
units, start socket then worker, verify peer restriction, UID, Grok authentication
and cleanup. Only then install MCP and object-mutate its preserved helper keys.
Start MCP and run a defaults-only helper answer before committing the journal.

Rollback stops new MCP, socket, worker and generated invocation units; confirms
cleanup; restores old trees/manifests/policy/config/units/drop-ins; removes only
transaction-created units/files when previously absent; reloads systemd; restores
masked/enabled/active state and linger state. Start restored worker socket/service
only when previously active, then restored MCP when previously active. Verify
prior binary hashes and health. Never report full rollback if only YAML or MCP
health was restored. If cleanup or restoration fails, leave admission stopped,
retain backups, report RecoveryFailed and require administrator intervention.

Inject failure and restart at every persisted transition, including after worker
success but MCP/helper failure, after first-install unit creation, after config
mutation and during recovery. An incomplete journal is recovered before another
update. An operator-visible helper acceptance failure always keeps the original
goal open, even when prior service health is successfully restored.

### Reference basis and remaining gates

Systemd upstream service documentation defines ExitType=main and cgroup, runtime
limits and stop behavior; its transient-settings catalogue supports these unit
settings. These are design mechanisms, not proof they work in this installation.
Sources: systemd upstream man/systemd.service.xml, man/systemd-run.xml,
man/systemd.kill.xml and docs/TRANSIENT-SETTINGS.md (consulted October 6, 2026).
Record the exact upstream source URLs in the session's web_reference actions.
Local evidence: user@1000.service is active; loginctl reports Linger=no and
RuntimePath=/run/user/1000. Installation preflight and real containment tests
remain mandatory and unexecuted.

F6 authoritative FR/TR/TEST/AC/mapping persistence is now supported by the
readbacks and verification under docs/receipts/runas-f6 listed above. The earlier
delegate's cache failure and failed reviews remain in the immutable evidence.
The canonical docs/Project export remains blocked by access denied, reported
through accepted triage; do not characterize that export as successful. A new
review must independently check these receipts and separate durable capture
from the remaining export failure. No acceptance criterion is satisfied yet.
U1 approval remains outstanding. U2 runtime verification and U3 source/build/live
equivalence remain future execution gates. This revision authorizes neither
implementation nor done-state changes.

## Worker validation and execution policy

- Root-owned policy permits only user sharpninja, peer UID for mcpserver, and
  `/home/sharpninja/github/McpServer` for this deployment.
- Root-owned configured executable is
  `/home/sharpninja/.local/share/mise/installs/node/26.8.2/bin/grok`.
  Validate executable existence and permissions at startup; user-owned Grok
  installation remains part of the explicitly trusted sharpninja boundary.
- Obtain peer UID with Linux SO_PEERCRED before reading request payload.
  Compare numeric UID from root-owned deployment policy; fail closed on
  platforms lacking verified peer-credential support. PID is audit information,
  not authorization. Test denial using another real UID at integration gate.
- Validate worker real/effective UID and account against policy and request.
  Match exact permitted canonical workspace, reject symlinked root and paths
  outside the allowlist. Agent workspace access remains trusted, not a sandbox.
- Models are `auto` or an administrator allowlist of named Grok models. `auto`
  omits --model. User prompts are not interpreted as transport options.
- Build ArgumentList from the existing one-shot Grok contract, retain plan
  permissions, and do not use persistent/always-approve flags in this slice.
- Worker inherits sharpninja's normal HOME and authentication; require successful
  authentication preflight without printing credential files or writing secrets.
- Create per-invocation private prompt directories mode 0700 and files 0600
  under the worker runtime directory; write the prompt as sharpninja, delete on
  every exit path. Reject symbolic links and use create-new semantics. Sweep
  worker-created abandoned invocation directories on startup, never arbitrary
  user files. Verify ownership in a real-account integration test.

## Deployment and rollback

Add worker publishing and service/socket registration to the existing Nuke
UpdateService Linux flow and its tests. Deploy through Nuke under pkexec only.
No binary hand-copy deployment. Stage and validate before stopping MCP.

Proposed units: mcpserver-grok-worker.socket and mcpserver-grok-worker.service.
systemd owns socket `/run/mcpserver-grok-worker/worker.sock`, mode 0660,
owner sharpninja/group mcpserver; parent root-owned, not caller-writable.
Worker accepts the systemd socket-activation descriptor (LISTEN_PID/LISTEN_FDS),
validates it is the expected listening Unix socket, and never replaces it.
Service: User=sharpninja, Group=sharpninja, HOME=/home/sharpninja,
NoNewPrivileges=true, UMask=0077, KillMode=control-group, private worker runtime
directory, bounded restart policy. Check sharpninja's Grok hook/socket/network
requirements before adding sandbox directives; do not break authentication.

Back up existing units, live config and application manifest. Install worker,
verify actual UID, socket ownership and authentication first. Object-mutate only
AgentHelp RunAs/socket/path/model/strategy in preserved live YAML. Keep other
settings and data unchanged. On worker or helper acceptance failure restore
previous units/config/build through Nuke recovery. Report the original helper
failure as still open after rollback; do not claim rollback fixes it.

## Byrd v4 slices and gates

1. Requirements and architectural design: capture FR/TR/TEST/AC and mapping;
   independently review security boundary, compatibility, full plan and approval.
2. Red tests: options forwarding, local/remote selection, identity mismatch,
   peer rejection, frame limits, cancellation/tree cleanup, prompt ownership,
   secret-safe diagnostics and updater preservation/rollback. Use mocks only
   for unit scope. Run the tests and retain expected failures before coding.
   Independent review of AC coverage before implementation.
3. Green implementation: configuration, framing/client, worker, remote session,
   Nuke deployment. Run full current and prior unit suite with zero failures
   and zero skips; remediate existing suite blockers rather than filtering them
   out for phase acceptance. Independent review before leaving implementation.
4. Integration: real Unix socket and separate UIDs, negative authorization,
   process identity, owned prompt files and termination on disconnect. Retain
   process/UID and exit receipts. No fake strategy can prove impersonation.
5. Deployment acceptance: Nuke via pkexec, verify service health and both units,
   call Agent Helper without RunAs/path/model/strategy session overrides, and
   require status completed plus expected FINAL ANSWER content from real Grok.
   Correlate helper turn with worker started/exit events proving sharpninja UID;
   verify MCP UID=mcpserver and NoNewPrivileges=true throughout. Independent
   review with accuracy and completeness >=98 before any done-state change.

Every review requires durable request and full response JSONL and the entire
verdict in the MCP session log with persistence proof. No summaries substituted.

## Acceptance criteria

- AC1: configuration forwards requested sharpninja identity and socket without
  HOME-only impersonation or session-supplied identity selection.
- AC2: actual Grok UID is sharpninja; MCP UID remains mcpserver with
  NoNewPrivileges=true. No secret-copy or expanded sudo delegation.
- AC3: restricted socket authenticates peer UID, enforces exact workspace and
  executable policy, rejects malformed/unbounded requests and unauthorized peers.
- AC4: completed helper answer from deployed defaults with matching invocation
  receipt; echo, progress-only and incomplete answers fail acceptance.
- AC5: cancellation, disconnect, timeout, output-limit, exception and shutdown
  remove private prompt files and terminate child process trees.
- AC6: Nuke deployment preserves unrelated config/data, and recovery is tested.
- AC7: full applicable suites have zero failures/skips, and all required phase
  reviews and durable receipts exist before completion. Existing broad-suite
  blockers are still blockers, not waived by the previous focused results.

## Outstanding decisions and blocked acceptance

Operator approval of the new cross-account worker architecture is outstanding.
Independent design review is not yet claimed approved. No worker exists yet.
The real helper answer, authentication as sharpninja, worker policy, unit/socket
installation, cleanup and full-suite gate all remain unverified until executed.
**Covered by:** FR: FR-MCP-RUNAS-001; TEST: TEST-MCP-HELP-004, TEST-MCP-HELP-005, TEST-MCP-RUNAS-001
**Status:** proposed
Scope: layer-1+
**Acceptance Criteria:**
- [ ] AC1: Helper-only Grok router forwards configured sharpninja identity/socket with AgentHelp.RunAs precedence and TodoPromptOptions.RunAs fallback. Reject session identity overrides. Empty Linux RunAs/local Windows retain semantics; voice/TODO/triage/persistent QuadBrain and other callers never reach worker; unsupported strategy plus effective RunAs fails closed. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
- [ ] AC2: Real Grok UID resolves to actual sharpninja policy UID (1000 here), MCP UID remains mcpserver and NoNewPrivileges=true throughout. No HOME-only impersonation, secret copy, sudo expansion or privilege weakening. Reject identity mismatch/unavailable worker without server-UID fallback. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
- [ ] AC3: Authenticate SO_PEERCRED before parse on restricted socket; exact workspace/executable/model policy. Reject ModelApiKey, nonempty AgentParameters/environment maps, identity override and nonpinned path; diagnostics exclude values. Bound protocol/admission/replay/backpressure; apply fixed HOME/PATH/XDG_RUNTIME_DIR/DBUS environment to each transient unit, not inherited manager state; read-only real authentication/answer preflight. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
- [ ] AC4: Agent Helper invoked using deployed defaults only, no session RunAs/path/model/strategy overrides, reaches completed with expected FINAL ANSWER from real Grok. Correlate helper turn and worker epoch/invocation started/exit/UID/cleanup receipts; echo, progress-only, fallback or incomplete answers fail. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
- [ ] AC5: Per-invocation systemd user transient cgroup lifetime terminates leftover/double-fork/reparented descendants and removes owned private prompt files on normal exit, cancellation, disconnect, timeout, output limit, exception, shutdown and worker crash/restart. Empty-cgroup and file-removal proof required; cleanup failure makes worker unhealthy and blocks admission. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
- [ ] AC6: Exclusive-lock journaled Nuke/pkexec deployment stages and verifies worker before MCP, preserves unrelated config/data, backs up/restores both binary trees/manifests/units/drop-ins/policy/YAML and active/enabled/masked plus linger states. Inject failures/restarts at every transition; incomplete journal recovered before update. RecoveryFailed retains backups and stopped admission; rollback never claims helper goal fixed. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
- [ ] AC7: TDD red evidence before code; full current/prior applicable unit suites and required integration/deployment scopes have zero failures and zero skips. Independent phase reviews with durable full request/response JSONL and logged verdict/readback, final accuracy/completeness >=98 before any done state. Explicit operator architecture approval required before implementation; no approval or implementation is claimed by this capture. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)

## TR-MCP-SCHEMA-109

**REPL request schema enforcement** — Every YAML or JSON request message exposed through the REPL shall have a published JSON Schema and shall be validated by the REPL before endpoint-backed workflow calls are invoked.
**Covered by:** FR: FR-MCP-109; TEST: TEST-MCP-145, TEST-MCP-146
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-001

**Per-Workspace Auth Tokens** — `WorkspaceResolutionMiddleware` resolves workspace identity per-request using a three-tier chain: (1) `X-Workspace-Path` header, (2) API key reverse lookup via `WorkspaceTokenService`, (3) default workspace from config. `WorkspaceAuthMiddleware` then validates the token against the resolved workspace. `WorkspaceTokenService` generates per-workspace cryptographic tokens (32-byte base64url) on startup and maintains reverse-lookup maps for API key → workspace resolution.
**Covered by:** FR: FR-MCP-013, FR-MCP-043
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-002

**Pairing Session Security** — `PairingSessionService` verifies passwords using SHA-256 with `CryptographicOperations.FixedTimeEquals` for constant-time comparison. Session state is stored in HttpOnly cookies with the Secure flag enabled on HTTPS. `PairingOptions` binds `Mcp:ApiKey` and `Mcp:PairingUsers` from configuration.
**Covered by:** FR: FR-MCP-014
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-003

**Signed Marker Bootstrap and Health Nonce Verification** — `MarkerFileService` SHALL render a top-level marker signature block and a top-level `trust_bootstrap` block into `AGENTS-README-FIRST.yaml` using a deterministic canonical payload. The rendered marker SHALL instruct agents to verify the signature first, generate a nonce for `/health`, require the response to echo the nonce exactly, and stop using MCP endpoints when verification fails. `McpSession`, `McpTodo`, and `McpContext` SHALL share the same trust-verification contract so bootstrap parity is preserved across the public PowerShell modules.

**Covered by:** `src/McpServer.Services/Services/MarkerFileService.cs`, `templates/prompt-templates.yaml`, `src/McpServer.ServiceDefaults/Extensions.cs`, `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, `tools/powershell/McpContext.psm1`
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-004

**Provider-Native At-Rest Encryption with No-Loss Transition Procedures** — The storage layer SHALL support optional at-rest encryption using only provider-native or provider-extension facilities: SQLite SEE, PostgreSQL `pg_tde` on Percona Server for PostgreSQL, and native SQL Server TDE. The implementation SHALL detect desired-versus-actual encryption state at startup, SHALL refuse to silently continue when the configured state and live state differ, and SHALL require explicit no-data-loss enable/disable/rotation procedures that preserve existing data when configuration changes. SQL Server LocalDB may be used for provider and migration coverage, but SQL Server TDE validation requires a non-LocalDB SQL Server target.

**Covered by:** `src/McpServer.Storage/Database/McpDatabaseProviderFactory.cs`, `src/McpServer.Storage/McpDbContextFactory.cs`, `src/McpServer.Storage/Database/SqliteMcpDatabaseProviderStrategy.cs`, `src/McpServer.Storage/Database/PostgreSqlMcpDatabaseProviderStrategy.cs`, `src/McpServer.Storage/Database/SqlServerMcpDatabaseProviderStrategy.cs`, `src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionCommand.cs`, `src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionRunner.cs`, `scripts/Invoke-McpDatabaseEncryptionTransition.ps1`, `src/McpServer.Storage.SqliteMigrations`, `src/McpServer.Storage.PostgreSqlMigrations`, `src/McpServer.Storage.SqlServerMigrations`
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-005

**Self-describing marker signature payload** — Implements FR-MCP-140. MarkerFileService SHALL declare an ordered SignaturePayloadFields array naming every canonical field used to build the marker-v1 HMAC-SHA256 payload, and BuildSignaturePayload SHALL derive its output order from that same array so the emitted list and the computed payload cannot diverge. The MarkerSignature model SHALL carry a fields array and a format string describing the key=value newline per-field encoding, the trailing LF on the final line, and UTF-8 encoding, and both SHALL be written into the signature block of AGENTS-README-FIRST.yaml. The conditional agentPlugins.policy and agentPlugins.contractDigest tail SHALL be represented faithfully: the fields array SHALL reflect the same conditional the payload builder applies for the marker being written. A test SHALL bind the documented field order in docs/REPL-AGENT-GUIDE.md to SignaturePayloadFields so the prose spec cannot drift from the code. Recovered from origin/claude/dreamy-brahmagupta; the CLAUDE.md session-start rewrite in that commit is deliberately NOT re-landed because it directs agents to PowerShell-module or hand-rolled verification, contradicting the current plugin-first trust contract.
**Covered by:** FR: FR-MCP-140; TEST: TEST-MCP-189
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-006

**Brain-slot signing keys follow a renamed party id** — CORRECTED 2026-07-20 after the original specification was proven wrong by test. Context unchanged: trusted parties and their keys live in the TransactionSecurity key store (KeyServerPartyEntity, KeyServerPartyKeyEntity), a separate SQLite database created with EnsureCreated, so the McpDbContext Creativity/Logic rename migration cannot reach them; BrainSlotRegistryService.RegisterPartyAsync registers the party only and never a key, while ValidateReadinessAsync requires an active signing key at "{partyId}:signing:1". On any migrated installation the renamed party therefore starts with no key and the quad reports NotReady. WHAT WAS WRONG: the first version of this requirement said to COPY the legacy party's signing key material forward. That is impossible and harmful. PartyKeyDescriptor and KeyServerPartyKeyEntity expose only PublicKeyPem, so only the public half can ever be read; InMemoryKeyServerService retains a private ECDsa handle only for a pair it generated itself, and CreateSigningKey given a public PEM with no private PEM stores an empty private-key set. The result passed every readiness gate and could never sign: turn transactions were rejected with TransactionFailureReason.UnknownKey after the model provider had already been invoked and billed, and persisting that row also suppressed TurnTransactionCoordinator.EnsureDefaultPartiesAsync, which mints a working pair only when no key row exists. CORRECT BEHAVIOR: when a brain slot's party has no active signing key and a legacy party id exists (brain-slot:left-hemisphere for Creativity, brain-slot:right-hemisphere for Logic), the runtime SHALL register the renamed party with NO key material so the keyserver mints a fresh pair and retains its private half, and SHALL leave the legacy party and key rows untouched. Historical verification is unaffected because a manifest carries its own PublisherPartyId and VerifyManifestAsync resolves the public key from that legacy party. Reconciliation remains idempotent, is a no-op when the new key already exists or no legacy party is found, and never rotates an existing key. Proven by TEST-MCP-192 and by BrainSlotAdoptedPartyKeySigningTests, which now assert a committed turn transaction under the renamed party and continued verification of a legacy-signed manifest.
**Status:** pending
Scope: layer-1+

## TR-MCP-SERVICEUPDATE-001

**Platform adapters and metadata-preserving Linux service lifecycle** — Preserve Windows behavior. Linux uses bounded shell-free systemctl/tar operations, installed-unit preflight, platform RID/apphost selection, shared preservation paths,0700 backup directories/0600 archives, ownership/mode/ACL/xattr preservation, failure restoration and same health checks. No SQL database export or automatic binary rollback claim.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] UpdateService selects Windows or Linux at runtime; Windows defaults/launcher remain compatible; Linux x64/arm64 use extensionless apphost; unsupported hosts fail before changes.
- [ ] Both platforms preserve live appsettings.yaml and configured DataFolder with legacy fallback. Linux also preserves unit/drop-ins/environment files and Unix ownership/modes/ACLs/xattrs through a private retained archive.
- [ ] Linux validates installed service identity and stage before stop; backup precedes replacement; restore precedes start; service executable and server/workspace health must pass. Failures preserve recovery artifacts and never report success.
- [ ] Mocks-first tests and full applicable unit suites pass with zero failures/skips; independent gates pass; existing local Linux service update preserves configuration/data and trusted workspace behavior.
- [ ] Every Build.Tests ServiceUpdateTests case runs and passes on Linux and on Windows with no skip. A case whose Linux check needs a Linux-only facility (a FIFO, Unix file modes, GNU tar ACL and xattr preservation, unlinking a directory symbolic link with File.Delete) asserts on Windows the outcome the helper produces there for the same input (PLAN-SESSIONTRIAGEFIX-001 C2, triage-report-cdf116ad, as BUG-BASELINE-004 did for Windows-only tests; StatDouble_ReportsRegisteredSpecialFileAsFifo, and the Windows cases of LinuxPreflight_RejectsSpecialStageFiles, LinuxPreflight_RejectsSpecialApphostAndTextInputs, LinuxArchive_UsesPrivateMetadataPreservingCommands, LinuxReplacement_DoesNotFollowSymlinks and LinuxPreflight_RejectsSymlinkRootAndNonexecutableStage).

## TR-MCP-SESSIONATTR-001

**Validate filesModified and commit paths against workspace root** — SessionLogService or turn append validates filesModified and commit paths against X-Workspace-Path. Outside-root paths require foreignRepo or equivalent or are rejected. Forward-only. Covered by FR-MCP-SESSIONATTR-001 TEST-MCP-SESSIONATTR-001.
**Covered by:** FR: FR-MCP-SESSIONATTR-001; TEST: TEST-MCP-SESSIONATTR-001
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONEND-001

**SessionEnd cache flush no-ops when cache cannot be resolved** — Invoke-CacheFlushHook and cache-manager do not throw on unresolved cache. CLAUDE_PROJECT_DIR is a Resolve-McpCacheDir fallback. Covered by FR-MCP-SESSIONEND-001 TEST-MCP-SESSIONEND-001.
**Covered by:** FR: FR-MCP-SESSIONEND-001; TEST: TEST-MCP-SESSIONEND-001
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLIFE-001

**Plugin keeps degraded turn cache and honest hook status** — Complete-ReplBeginTurnAfterPersist must not replace the turn file. Open-PluginTurn emits turn-opened-degraded while degraded. appendDialog 404 on a degraded local turn resubmits or queues session_dialog.
**Covered by:** FR: FR-MCP-SESSIONLIFE-001; TEST: TEST-MCP-SESSIONLIFE-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] One real begin-turn builder or shim preserves the complete cached turn on degraded persistence and exposes a shared, truthful degraded transition used by all hooks.
- [ ] Prompt, complete, update, and dialog hooks consume the shared transition without reduced-object replacement, empty-query overwrite, or false server-persisted status.
- [ ] Degraded missing-turn dialog recovery is bounded to one submit attempt and one retained dialog failsafe, while unrelated not-found remains nonretryable.

## TR-MCP-SESSIONLIFE-002

**Metadata durability and freshness rebind** — Snapshot cached metadata before replacing turn state. Same-request beginTurn is omit-unbound only when durable. Assert-ReplCurrentTurnFresh must not rewrite a cached session id. Three documentation files state ordinary rejection, supersede None, and durable reopen omission.
**Covered by:** FR: FR-MCP-SESSIONLIFE-002; TEST: TEST-MCP-SESSIONLIFE-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A single durability-first metadata resolver implements explicit, verified-cache, and None precedence for planFile and todoId.
- [ ] Workspace, agent, session, and request identity are immutable after binding; freshness checks diagnose drift and never rebind from markers or inherited variables.
- [ ] Ordinary first persistence, canceled/cancelled supersession, durable reopen, and all three contract documents use the same resolver semantics.

## TR-MCP-SESSIONLIFE-003

**Wrapper three-outcome exit contract** — Invoke-McpPlugin reports primary success, classified queued success, or nonzero failure. appendActions honors requestId. updateTurn dedupes. Child REPL has one deadline with process-tree cleanup.
**Covered by:** FR: FR-MCP-SESSIONLIFE-003; TEST: TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The wrapper exposes a typed primary, queued, or failed outcome contract and no session-log mutation collapses that result to an unclassified boolean.
- [ ] Result serialization includes identity, method, durability, queue, retryability, diagnostic, and failsafe fields without claiming primary success for queued work.
- [ ] Child REPL execution uses one operation deadline with deterministic process-tree cleanup, and dedupe preserves sequential method labels and queue counts.
- [ ] Existing-session graph materialization avoids multiplicative collection joins, remains update-tracked, and bounds transient SQL Server deadlock or timeout handling inside the storage command budget without converting retryable contention into primary success.

## TR-MCP-SESSIONLIFE-004

**Quarantine repair, import recovery, and contention drain** — Repair the three named quarantine files. Transcript ingestion scans importRecovery yaml and deletes only the envelope after durable success. McpErrorClassifier and both drain entry points keep contention retryable and unlatched.
**Covered by:** FR: FR-MCP-SESSIONLIFE-004; TEST: TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004, TEST-MCP-SESSIONLIFE-005
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A shared replay and quarantine repair core parses supported historical object shapes, validates canonical identity and paths, and preserves recovery artifacts on any failure.
- [ ] Both drain entry points use one contention classifier and identical attempt, latch, ordering, and delete-after-durable-success rules; true storage outage also drives health.storage=unreachable while contention remains retryable.
- [ ] Import recovery follows the canonical session persistence path, supports validated canonical-only or reconstructed bundles, and never exposes transcript ingestion through agent plugins.
- [ ] Replay enforces workspace containment, reparse containment, file and record bounds, cancellation, content identity, idempotency, and concurrency safety before destructive cleanup.

## TR-MCP-SESSIONLIFE-005

**Stop-hook stale pin and upsert uniqueness** — Close-PluginTurnIfNeeded runs build and audit gates first and allows a stale pin only with authoritative server completion. SessionLogSchemaGuard, SessionLogService.UpsertTurnAsync, and SessionLogController implement predecessor-schema detection, details.inner, and one uniqueness retry.
**Covered by:** FR: FR-MCP-SESSIONLIFE-005; TEST: TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004, TEST-MCP-PLUGIN-011
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Stop enforcement obtains exact-turn server proof through the supported typed client after build and audit gates and before any state mutation.
- [ ] A shared cache-state lock snapshots identity and content hashes, releases for bounded network I/O, then conditionally mutates only if the same state remains current.
- [ ] Schema predecessor detection and provider error classification occur before mutation and preserve useful credential-safe details.inner.
- [ ] Upsert retries once only for the known same-session/request unique race and merges the winning row without duplicate child collections.

## TR-MCP-SESSIONLOG-001

**Session-log lifecycle tools return structured errors** — The session-log lifecycle MCP tools (sessionlog_complete_turn, sessionlog_fail_turn) SHALL return a structured {error} result for every failure mode, including a malformed turnJson payload and a workspace-resolution failure; no failure may surface as the ModelContextProtocol SDK's opaque "An error occurred invoking sessionlog_complete_turn" message. Acceptance Criteria: (AC1) sessionlog_complete_turn/sessionlog_fail_turn with a malformed turnJson return {error} carrying the exception message and no success field; (AC2) a workspace-resolution failure during ApplyWorkspaceOverride returns {error} rather than escaping uncaught; (AC3) a valid or null turnJson still returns {success:true}. Origin: BUG-TRIAGE-070/075 (JsonSerializer.Deserialize and ApplyWorkspaceOverride were outside the try in FinalizeLifecycleTurnToolAsync/UpsertLifecycleTurnToolAsync, FwhMcpTools.SessionLog.cs). Validated by TEST-MCP-SESSIONLOG-001.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOG-002

**Session-log text search covers dialog and child collections** — Session-log query text search SHALL match content in processing-dialog items, actions (description/type/filePath), commits (message/sha/branch/files), string-list sections (design decisions / requirements discovered / files modified / blockers), tags, and context items - not only the four turn scalar fields (QueryText, QueryTitle, Response, Interpretation). Acceptance Criteria: (AC1) a query whose term exists only in a processing-dialog item returns the session; (AC2) a query whose term exists only in an action description returns the session; (AC3) existing scalar-field and boolean queries still return the correct sessions. Origin: BUG-TRIAGE-070 (SessionLogService.BuildSearchText joined only the four scalar fields). The full turn graph is Include-loaded in QueryAsync before the client-side matcher runs, so child navigations are populated for both SQLite and SQL Server. Validated by TEST-MCP-SESSIONLOG-002.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOG-003

**Terminal-turn compliance gate is QBAgent-only** — The terminal-turn decision/action/commit compliance gate (ValidateTerminalTurnCompliance) SHALL apply only to the QBAgent ACID source type; standard agents (ClaudeCode, Cursor, Copilot, ...) SHALL be able to complete or fail a turn with no decision/action/commit items. The sessionlog_complete_turn and sessionlog_fail_turn MCP tool descriptions SHALL state this QBAgent-only scope so operators/triage do not misattribute plugin-side completion failures to a server gate. Acceptance Criteria: (AC1) UpsertTurnAsync with a completed empty turn is accepted for a ClaudeCode session and rejected (ArgumentException) for a QBAgent session; (AC2) the complete/fail tool descriptions state the gate is QBAgent-only. Origin: BUG-TRIAGE-082/083 (reporter hypothesized the server gate blocked ClaudeCode completions; the gate is QBAgent-only, so the real cause is plugin-local). Validated by TEST-MCP-SESSIONLOG-003.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOG-005

**Dedicated session and turn title-update service, endpoints, and client** — ISessionLogService MUST expose SetSessionTitleAsync(sourceType, sessionId, title) and SetTurnTitleAsync(sourceType, sessionId, requestId, title) that unconditionally set the title on an existing session or turn (throwing when the session or turn does not exist), save, and publish a change event. SessionLogService, the TransactionGated decorator, and the Federated decorator MUST implement or delegate these. The capability MUST be reachable via REST on SessionLogController and via the typed SessionLogClient. Validated by TEST-MCP-SESSIONLOG-005. Covered by FR: FR-SUPPORT-012. Status: pending. Scope: layer-1+.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOG-006

**Required planFile and todoId scalars with None sentinel and backfill** — Session-log turn storage, DTOs, REST, MCP tools, and plugin wrappers SHALL expose required string fields planFile and todoId. New interactive creates/submits/beginTurn/replace-turn reject omitted, null, or empty values. Accepted planFile values are a workspace-relative path, an exact absolute path, a ~/ home-relative path, or the literal sentinel None. Accepted todoId values are a canonical MCP TODO id, ISSUE-N, or None. An EF migration adds both columns, defaults existing rows to None, then a one-time backfill scans turn contents and agent history under ~. Uncertain extractions remain None. Query/get results SHALL return both fields.
**Covered by:** FR: FR-MCP-SESSIONLOGCTX-001; TEST: TEST-MCP-SESSIONLOG-006
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The real file-system binding of AC-FR-MCP-SESSIONLOGCTX-001-008 is AC-TR-MCP-WS-005-002: the agent-history scan uses the same directory walk, so that integration case proves both (operator 2026-10-30: "New TR binding criterion (Recommended)").

## TR-MCP-SESSIONLOGSAN-001

**Bounded outbound session-log sanitizer** — A single sanitizer decorator must clone session-log DTO graphs, apply validated default and configured regex rules with deterministic replacement tokens and finite timeouts, and wrap the final local or federated ISessionLogService read result.
**Covered by:** FR: FR-MCP-SESSIONLOGSAN-001; TEST: TEST-MCP-SESSIONLOGSAN-001, TEST-MCP-SESSIONLOGSAN-002
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOGSAN-002

**Sanitizer timeout fail-open is deterministically testable** — The session-log sanitizer per-rule regex evaluation SHALL be exposed through an injectable seam (RegexReplaceInvoker, defaulting to Regex.Replace in production) so its timeout fail-open behavior can be verified deterministically - forcing exactly one field/rule to raise RegexMatchTimeoutException - independent of catastrophic-backtracking wall-clock jitter. Acceptance Criteria: (AC1) with an injected invoker that raises RegexMatchTimeoutException for the pathological field, SanitizeString returns [REDACTED:{ruleId}:timeout] for that field while other fields continue to be sanitized by their rules, across repeated isolated runs (no flake); (AC2) production behavior is unchanged (default invoker is Regex.Replace). Origin: BUG-TRIAGE-081 (SessionLogSanitizerTimeoutTests flaked under scheduler jitter; the deployed 1ms->100ms budget was a mitigation, not deterministic). Validated by TEST-MCP-SESSIONLOGSAN-002.
**Covered by:** FR: FR-MCP-SESSIONLOGSAN-001; TEST: TEST-MCP-SESSIONLOGSAN-001, TEST-MCP-SESSIONLOGSAN-002
**Status:** pending
Scope: layer-1+

## TR-MCP-SKILLS-001

**Use supported plugin MCP bridge paths** — Skill content uses each plugin's supported MCP bridge or wrapper path and forbids raw REST for normal MCP mutations.
**Covered by:** FR: FR-MCP-111; TEST: TEST-MCP-149, TEST-MCP-150, TEST-MCP-151
**Status:** pending
Scope: layer-1+

## TR-MCP-SKILLS-002

**Preserve commit-sync pause acknowledgement contract** — commit-sync skill content preserves the pause-and-acknowledge contract before staging, committing, or pushing.
**Covered by:** FR: FR-MCP-111; TEST: TEST-MCP-149, TEST-MCP-150, TEST-MCP-151
**Status:** pending
Scope: layer-1+

## TR-MCP-SKILLS-003

**Package skills through existing plugin distribution metadata** — Plugin manifests and package metadata expose or package the new skills according to each plugin's existing distribution model.
**Covered by:** FR: FR-MCP-111; TEST: TEST-MCP-149, TEST-MCP-150, TEST-MCP-151
**Status:** pending
Scope: layer-1+

## TR-MCP-STDIO-109

**Plugin stdio JSON request envelopes** — Codex, Claude, Copilot, and Cline plugins shall instruct direct stdio callers to send one single-line JSON request envelope per message, and plugin bridges that write stdio shall emit that shape.
**Covered by:** FR: FR-MCP-109; TEST: TEST-MCP-145, TEST-MCP-146
**Status:** pending
Scope: layer-1+

## TR-MCP-STRICTCOUNT-001

**Null-safe Count for updateTurn collection fields under StrictMode** — New-McpPluginTurnUpsertRequest and Invoke-WorkflowUpdateTurn enumerate tags and contextList with @() or equivalent under Set-StrictMode Latest. Covered by FR-MCP-STRICTCOUNT-001 TEST-MCP-STRICTCOUNT-001.
**Covered by:** FR: FR-MCP-STRICTCOUNT-001; TEST: TEST-MCP-STRICTCOUNT-001
**Status:** pending
Scope: layer-1+

## TR-MCP-SUBLOG-001

**Subscriber message-log sink** — Parseable sink test
**Covered by:** FR: FR-MCP-SUBLOG-001; TEST: TEST-MCP-SUBLOG-001
**Status:** pending
Scope: layer-1+

## TR-MCP-SUBSCRIBER-001

**Transaction Subscriber Service** — Provide shared subscriber commit services and a separate `McpServer.Subscriber` host with durable commit/status storage, keyserver-backed manifest verification, protected-envelope decrypt/hash validation, idempotent duplicate commit handling, conflict rejection, abort/status endpoints, subscriber encryption key-ring binding, XMLDocs, typed client contracts, and deterministic failure reasons.

**Covered by:** `McpServer.Subscriber`, `SubscriberController`, `SubscriberClient`, `TransactionSecurityServices`, `TransactionSecurityOptions`, `TransactionSecurityStateStores`, `TransactionSecurityModels`, `TransactionSecurityControllerTests`, `TransactionSecurityClientTests`, `DurableTransactionSecurityStorageTests`, `SeparateTransactionServiceIntegrationTests`
**Status:** pending
Scope: layer-1+

## TR-MCP-SVC-001

**Windows Service Configuration** — `UseWindowsService(options => { options.ServiceName = "McpServer"; })` in `Program.cs` enables Windows Service hosting. The service is published as a self-contained single-file executable to `C:\ProgramData\McpServer`. The `Manage-McpService.ps1` script handles Install, Uninstall, Start, Stop, Restart, Status, and Publish operations with gsudo elevation. Recovery policy restarts the service on failure with a 60 s delay.
**Covered by:** FR: FR-MCP-017
**Status:** pending
Scope: layer-1+

## TR-MCP-SVC-002

**Executable resolution skips WindowsApps App-Execution-Alias stubs** — Implements FR-MCP-141. IProcessEnvironmentService.ResolveExecutable in src/McpServer.Common.AgentCli/ProcessEnvironmentService.cs SHALL skip any PATH directory whose path contains Microsoft\WindowsApps when probing for an executable, because those entries are zero-byte App-Execution-Alias reparse points that a service account cannot launch, failing with Win32Exception 1920. The guard SHALL be an exact containment test on Microsoft\WindowsApps and SHALL NOT match the genuine C:\Program Files\WindowsApps MSIX install root, which holds real executables such as the packaged PowerShell. This affects every ResolveExecutable caller including the Agent CLI client, the Codex, Grok, and OneShot execution strategies, and ProcessRunner, not only tunnel providers. Recovered from origin/claude/busy-dubinsky, where the file lived at src/McpServer.Common.Copilot/ProcessEnvironmentService.cs before that project was renamed to McpServer.Common.AgentCli.
**Covered by:** FR: FR-MCP-141; TEST: TEST-MCP-190, TEST-MCP-191
**Status:** pending
Scope: layer-1+

## TR-MCP-SYNC-001

**Node core vendor package uses a version-less stable file name** — SyncAgentPlugins SHALL vendor the packed Node plugin core into consumer repositories under the version-less stable file name sharpninja-mcpserver-plugin-core.tgz, and SHALL NOT embed a package version in any vendored tarball file name or in the constant that names it. Rationale, from triage-report-52e8098cd299475d9922098f00d818b6: Build.SyncAgentPlugins.cs:465 hard-coded sharpninja-mcpserver-plugin-core-0.1.0.tgz while package.json had moved to 0.2.0 after a breaking-change bump, so the three Node plugin repositories held tarballs whose file name claimed the pre-breaking-change surface while their content was the new package; the versioned name is also why the constant existed at all, since consumer package.json files reference the tarball path and a version bump would break the reference. The vendor step SHALL discover consumer vendor directories by matching any sharpninja-mcpserver-plugin-core*.tgz variant so first-run migration finds the legacy versioned files, copy the packed artifact to the stable name, delete superseded variants from the vendor directory, and rewrite the consumer package.json dependency reference file:vendor/sharpninja-mcpserver-plugin-core*.tgz to the stable name via a targeted in-place replacement that preserves the file's formatting. The step SHALL also assert that npm pack produced a tarball whose embedded version matches plugins/core/lib-node/package.json, so a silent pack-versus-manifest drift fails the sync instead of shipping.
**Covered by:** FR: FR-MCP-143; TEST: TEST-MCP-194, TEST-MCP-REPL-040
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The vendor step expects the tarball file name npm pack derives from plugins/core/lib-node/package.json (the name with a scope's leading @ dropped and its / turned into -, then -<version>.tgz), refuses a manifest without a name, and fails the sync when npm pack reports a different file name, so renaming the package does not stop the sync (BUG-BUILD-010; Build.Tests SyncAgentPluginsPackNameTests).
- [ ] After the vendor step copies the packed plugin core into a consumer's vendor/sharpninja-mcpserver-plugin-core.tgz and rewrites its package.json reference, it refreshes that consumer's package-lock.json through npm. For a consumer whose package.json already referenced that stable tarball name before the sync (mcpserver-cline-plugin, mcpserver-cline-v2-plugin and mcpserver-opencode-plugin), the lockfile entry of the dependency its package.json names for the vendored tarball records the new tarball's SHA-512 integrity, package.json and every other lockfile line stay unchanged, and npm ci then succeeds without EINTEGRITY, also when the tarball's own package name differs from that dependency name (the core packs as @qbrainai/qbrain-ai-plugin-core while those consumers depend on @sharpninja/mcpserver-plugin-core). A plain npm install --package-lock-only leaves the stale integrity, and installing the bare file: spec adds a second dependency under the tarball's package name, so neither satisfies this; nothing but npm writes the lockfile (triage-report-de98c84b and -f2684f90, BUG-BASELINE-012; PLAN-SESSIONTRIAGEFIX-001 E1; Build.Tests NodeVendorLockfileTests: VendorLockRefreshAssertion_AcceptsKeyedSpecAndRejectsPlainBareHandOrMissingRefresh, VendorLockRefresh_PlannedCommand_InstallsVendoredSpecUnderConsumerKey, VendorLockRefresh_OfflineConsumer_RecordsNewTarballIntegrity).
- [ ] SyncAgentPlugins' version bump changes a Node plugin's package.json and package-lock.json only through npm, versioning each plugin root that has a package.json with npm version <next> --no-git-tag-version --allow-same-version --ignore-scripts: package.json changes in its version line only, package-lock.json in its two version lines only, no other character is re-escaped (an em dash, &&, + and >= stay as written), the vendored core's integrity is unchanged, and git gets no commit or tag; the bump writes plugin.json manifests and .version itself and never writes package.json or package-lock.json (triage-report-de98c84b and -f2684f90, BUG-BASELINE-012, operator ruling 2026-10-10 "Never edit an npm lockfile. It belongs to npm. If you want it changed, use npm."; PLAN-SESSIONTRIAGEFIX-001 E1; found in the C3 sync of 2026-10-15, where the bump re-serialized all three lockfiles; Build.Tests: NpmVersionAssertion_AcceptsVersionLinesOnlyAndRejectsEscapedReformattedOrUntouchedFiles and NpmVersion_NodePlugin_ChangesOnlyVersionLines and PlanPluginVersionUpdates_LeavesNpmFilesToNpm in NodeVendorLockfileTests, and PlanPluginVersionUpdates_BumpsMinorAndSkipsNodeModules in BuildTargetTests).

## TR-MCP-TEMPVOL-001

**Plugin entrypoints set TEMP TMP on the workspace volume** — session-start and wrapper entrypoints set TEMP and TMP to a workspace-drive directory when volumes differ. No PSGallery patch. Covered by FR-MCP-TEMPVOL-001 TEST-MCP-TEMPVOL-001.
**Covered by:** FR: FR-MCP-TEMPVOL-001; TEST: TEST-MCP-TEMPVOL-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Where every path shares one root (Linux), Set-McpPluginSameVolumeTemp leaves TEMP and TMP unchanged and reports Succeeded true and Changed false for any target; on Windows a target on another drive moves TEMP and TMP to a writable directory there, and a target whose temp directory cannot be created leaves them unchanged with a visible error (PLAN-SESSIONTRIAGEFIX-001 D8, triage-report-30a24728; TEMPVOL cross-volume target per platform, TEMPVOL uncreatable target per platform).

## TR-MCP-TESTRESOURCE-001

**Test resource loading and artifact isolation** — Package fixture inputs in the owning test project, resolve them by resource identity or copied test output, and isolate generated artifacts under test-owned output roots. Repository-root discovery is not used to find fixture inputs or artifact destinations. Live source-contract tests retain repository/sibling-plugin reads when validating the current source tree.
**Covered by:** FR: FR-MCP-TESTRESOURCE-001; TEST: TEST-MCP-164, TEST-MCP-TESTRESOURCE-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Fixture files are included as EmbeddedResource or copied to test output with explicit project metadata and stable logical names.
- [ ] Fixture loaders resolve from the test assembly or AppContext.BaseDirectory; they do not walk parent directories to find the checkout.
- [ ] Artifact writers accept an injected test output root or create a unique disposable directory and never write under docs/reviews or source fixture directories.
- [ ] Build/source contract tests may read repository and sibling plugin paths only when the current source tree is the intended subject.

## TR-MCP-TODO-002

**Cross-Workspace TODO Move** — `TodoController.MoveAsync` at `POST /mcpserver/todo/{id}/move` reads the item from the source workspace (resolved via header/API key), creates it in the target workspace (resolved via `IWorkspaceService.GetAsync` + `TodoServiceResolver.Resolve`), then deletes from the source. Request body: `TodoMoveRequest { TargetWorkspacePath }`. Error responses: 400 (null request or unknown target workspace), 404 (item not found), 409 (create failed in target), 500 (created in target but delete from source failed). MCP STDIO parity via `todo_move` tool in `FwhMcpTools`.
**Covered by:** `TodoController`, `FwhMcpTools`, `TodoMoveRequest`, `TodoServiceResolver`, `IWorkspaceService`
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-003

**GitHub-Backed TODO Creation Alias** — The server SHALL accept `ISSUE-NEW` only on TODO create requests, SHALL immediately create the corresponding GitHub issue, SHALL rewrite the persisted TODO identifier to the canonical `ISSUE-{number}` value returned by GitHub, and SHALL return that canonical identifier to the caller. Persisted TODO validation SHALL accept both `^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$` and `^ISSUE-\d+$`. Dependency validation SHALL use the same persisted-ID rule set.
The `ISSUE-NEW` flow SHALL be implemented through a shared creation path so HTTP, MCP/STDIO, and voice-driven TODO creation all apply the same rewrite and persistence behavior.


**Covered by:** `TodoCreationService`, `TodoValidator`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`, `TodoService`, `EfTodoService`
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-004

**Shared ISSUE-* TODO Update Orchestration** — All server-side TODO update entry points that can mutate existing TODO items SHALL route `ISSUE-{number}` updates through shared orchestration instead of writing directly to the TODO store. The shared path SHALL suppress description changes after first sync, SHALL reuse the existing TODO store for the local mutation, and SHALL trigger the GitHub sync/comment flow after a successful local update.

**Covered by:** `TodoUpdateService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-005

**Provider-Agnostic Database-Authoritative TODO Storage with Deterministic YAML Projection** — The TODO subsystem SHALL use the configured `Mcp:Database:Provider` (SQLite, SQL Server, or PostgreSQL) via `McpDatabaseProviderFactory` (TR-MCP-CFG-007) as the authoritative current-state store for workspace TODO items. Service initialization SHALL perform EF Core schema migration, one-time bootstrap import from an existing `TODO.yaml` when the authoritative database is empty, and deterministic projection back to the configured TODO YAML path after successful mutations. The authoritative store SHALL preserve projection metadata needed to rehydrate ordered sections, `code-review-remediation` phases, `notes`, `completed`, and the code-review reference without treating YAML as runtime source of truth.
Projection failures after a committed authoritative mutation SHALL surface an explicit failure result instead of silent success. The TODO storage provider setting SHALL accept `yaml` or `database`; the legacy `sqlite` value SHALL be accepted and aliased to `database` with a one-time warning log. No sqlite-specific settings SHALL be consulted when provider is `database`.


**Covered by:** `EfTodoService`, `TodoItemEntity`, `TodoAuditHistoryEntity`, `TodoDocumentMetadataEntity`, `McpDbContext` (Todo DbSets), `McpDatabaseProviderFactory`, `TodoYamlFileSerializer`, `TodoServiceFactory`, `TodoStorageOptions`, `McpInstanceResolver`, `appsettings.yaml`, `appsettings.Staging.yaml`, `src/McpServer.Support.Mcp/appsettings.yaml`, `src/McpServer.Support.Mcp/appsettings.Staging.yaml`
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-006

**Append-Only TODO Audit History, Projection Failure Classification, and Repair Contract** — TODO create, update, delete, and bootstrap-import operations SHALL append reconstructable audit snapshots with monotonic per-item versions. The server SHALL expose `GET /mcpserver/todo/{id}/audit` together with typed client parity and MCP STDIO tool parity so callers can retrieve ordered tracked states for a TODO item even when the current row has been deleted but audit history still exists.
Mutation results SHALL include a machine-readable failure classification so callers can distinguish validation, not-found, projection-failure, conflict, and external-sync error shapes when TODO operations fail or only partially succeed. For database-backed TODO storage (the authoritative mode per TR-MCP-TODO-005), a projection failure SHALL preserve committed authoritative database state, record operator-visible projection failure metadata, and leave `TODO.yaml` repairable without replaying the mutation. The server SHALL expose `GET /mcpserver/todo/projection/status` and `POST /mcpserver/todo/projection/repair` together with typed client parity and MCP STDIO tool parity so operators can verify whether `TODO.yaml` matches authoritative database state and rebuild it on demand.


**Covered by:** `ITodoService`, `ITodoStore`, `EfTodoService`, `TodoAuditHistoryEntity`, `TodoYamlFileSerializer`, `TodoController`, `McpServerMcpTools`, `TodoClient`, `TodoModels`, `TodoCreationService`, `TodoUpdateService`
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-007

**Legacy SQLite TODO Storage One-Shot Migration** — When TR-MCP-TODO-005 provider-agnostic storage is enabled and a pre-existing legacy `mcp.db` SQLite TODO store is present at the deprecated `Mcp:TodoStorage:SqliteDataSource` path, the server SHALL copy rows from `todo_items`, `todo_item_history`, and `todo_document_metadata` into the configured authoritative database on first boot, preserving primary keys, audit identifiers, and monotonic per-item versions. The migrator SHALL be idempotent: subsequent starts SHALL be no-ops when the target TODO tables are non-empty or the completion marker file exists in the effective data folder. The migrator SHALL honor the `Mcp:TodoStorage:MigrateFromLegacySqlite` feature flag and SHALL run as a background hosted service so it never blocks the SCM 30-second service-start window. Failures SHALL log per-row context and continue with the next row rather than aborting the whole migration.

**Note:** In practice the provider-agnostic rewrite (TR-MCP-TODO-005) was shipped alongside the workspace-scoped schema (TR-MCP-TODO-008), whose per-workspace YAML bootstrap (`TodoBootstrapImporter`) replaces the need for a legacy `mcp.db` row copy. Live Phase 5 deploys on LEGION2 and PAYTON-DESKTOP (2026-04-21) reconstructed authoritative state from workspace YAML directly; legacy SQLite TODO rows are out-of-band archived to CSV when present (see `C:\Users\Public\mcpserver-postgres-archive-20260421` on PAYTON for the postgres/SQLite snapshot archived during deploy).

**Covered by:** `TodoBootstrapImporter` (replaces legacy migrator), per-workspace YAML bootstrap path
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-008

**Workspace-Scoped Database-Backed TODO Storage with Per-Workspace YAML Bootstrap** — Database-backed TODO storage (TR-MCP-TODO-005) SHALL scope every TODO row, audit-history row, and document-metadata row to the active workspace via a `WorkspaceId` column populated from the resolved `WorkspaceContext.WorkspacePath`, matching the TR-MCP-MT-003 multi-tenant pattern used by context, session-log, agent, tool, and graph entities. `McpDbContext` SHALL install a global query filter on all three Todo entities so reads, updates, and deletes never cross workspace boundaries. `TodoItemEntity` SHALL use composite primary key `(WorkspaceId, Id)` so the same canonical TODO id MAY exist in multiple workspaces without collision. `TodoDocumentMetadataEntity` SHALL use composite primary key `(WorkspaceId, SingletonId = 1)` so each workspace owns exactly one document-metadata singleton. `TodoAuditHistoryEntity` SHALL carry `WorkspaceId` as a filter column and index; the audit primary key remains `(TodoId, Version)` scoped implicitly by the query filter.
Bootstrap SHALL import from the per-workspace `TodoFilePath` YAML into the authoritative database when that workspace's TODO rows are empty, running exactly once per workspace per marker-file lifetime. The bootstrap path SHALL preserve ordered sections, completed items, notes, code-review reference, and projection metadata identically to the single-workspace bootstrap shape used by `TodoService`. After bootstrap, YAML projection SHALL write to the workspace-specific `TodoFilePath`; no other workspace's YAML SHALL be touched.

The `LegacyTodoSqliteMigrator` (TR-MCP-TODO-007) SHALL stamp imported rows with the active workspace's `WorkspacePath`. REST routes `/mcpserver/todo/*` and MCP STDIO `todo_*` tools SHALL honor the workspace resolved by the existing `WorkspaceAuthMiddleware` / `X-Workspace` header path without additional caller changes beyond what TR-MCP-MT-003 already mandates.


**Note:** Phase 5 live deploy verified on 2026-04-21: LEGION2 bootstrapped 130 items across 7 workspaces; PAYTON-DESKTOP bootstrapped 96 items across 5 workspaces (workspace scoping confirmed via `TodoBootstrapImporter summary` log lines with per-workspace `Imported:N` outcomes).

**Covered by:** `TodoItemEntity`, `TodoAuditHistoryEntity`, `TodoDocumentMetadataEntity`, `McpDbContext` (query filters + composite keys), `EfTodoService`, `TodoBootstrapImporter`, `TodoServiceFactory.CreateForWorkspace`, per-provider migration assemblies
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-009

**Preserve TODO description Markdown** — TODO persistence, plugin/client update paths, database storage, audit rows, and informational projections must treat description as Markdown, preserving blank lines, indentation, code fences, list spacing, and trailing content without trimming meaningful formatting.
**Covered by:** FR: FR-MCP-108; TEST: TEST-MCP-144
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-010

**Root-scoped TODO done serialization** — TODO update serializers in MCP plugin wrappers SHALL read the parent done field only from the request root and SHALL NOT derive it from nested implementationTasks[].done values. Structured root-level parsing is required for boolean root fields when building HTTP or workflow update bodies.
**Covered by:** FR: FR-MCP-114; TEST: TEST-MCP-154
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-CLOSE-001

**TODO close operation surfaces** — Add a dedicated close-by-id operation on the TODO controller and typed client that delegates through the existing TODO mutation path with done true and a UTC completion timestamp.
**Covered by:** FR: FR-MCP-TODO-CLOSE-001; TEST: TEST-MCP-TODO-CLOSE-001
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-001

**Prompt Template YAML Storage** — `PromptTemplateService` persists templates in a single YAML file (default `templates/prompt-templates.yaml`) using YamlDotNet with `HyphenatedNamingConvention`. Root structure: `templates:` → map of template-id → entry object (title, category, tags, description, engine, variables, content). Read/write serialization uses `SemaphoreSlim(1,1)` for write safety. Templates are loaded on-demand and not cached (file is source of truth).
**Covered by:** `PromptTemplateService`, `TemplateStorageOptions`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-002

**Prompt Template Rendering** — `PromptTemplateRenderer` compiles Handlebars templates via `HandlebarsDotNet` with content-hash-based caching in a `ConcurrentDictionary`. Variable validation checks required variables against supplied data and reports missing values. `RenderAsync` returns `PromptTemplateTestResult` with `RenderedContent` on success or `MissingVariables`/`Error` on failure. Thread-safe for concurrent rendering.
**Covered by:** `PromptTemplateRenderer`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-003

**Prompt Template REST + MCP Endpoints** — `PromptTemplateController` exposes 7 REST endpoints at `/mcpserver/templates` (list/filter with query params, CRUD by ID, test stored template, test inline template). `FwhMcpTools` exposes 6 MCP tools (`prompt_template_list`, `prompt_template_get`, `prompt_template_create`, `prompt_template_update`, `prompt_template_delete`, `prompt_template_test`). Both delegate to `IPromptTemplateService`.
**Covered by:** `PromptTemplateController`, `FwhMcpTools`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-004

**Prompt Template CQRS + Director UI** — Full 4-layer CQRS stack: `TemplateMessages.cs` defines queries/commands/results, 6 handlers (`ListTemplatesQueryHandler`, `GetTemplateQueryHandler`, `TestTemplateQueryHandler`, `CreateTemplateCommandHandler`, `UpdateTemplateCommandHandler`, `DeleteTemplateCommandHandler`) delegate to `ITemplateApiClient`. `TemplateApiClientAdapter` bridges to `McpServerClient.Template`. `TemplateListViewModel` and `TemplateDetailViewModel` drive `TemplatesScreen` in Director TUI. Authorization: `McpArea.Templates` with Viewer (read) and Admin (write) roles.
**Covered by:** `TemplateMessages`, `\*TemplateQueryHandler`, `\*TemplateCommandHandler`, `ITemplateApiClient`, `TemplateApiClientAdapter`, `TemplateListViewModel`, `TemplateDetailViewModel`, `TemplatesScreen`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-005

**System Template Externalization** — Three provider interfaces decouple system prompt templates from inline C# constants: (1) `IMarkerPromptProvider` / `FileMarkerPromptProvider` reads `templates/prompt-templates.yaml` via `IPromptTemplateService` (id: `default-marker-prompt`), throwing a critical exception on file-missing. Fallback to `MarkerFileService.DefaultPromptTemplate` is REMOVED. Injected into `WorkspaceProcessManager` with precedence: config override (`Mcp:MarkerPromptTemplate`) > file template. (2) `ITodoPromptProvider` / `TodoPromptProvider` looks up templates from `IPromptTemplateService` by well-known IDs (`todo-status-prompt`, `todo-implement-prompt`, `todo-plan-prompt`), falling back to `TodoPromptDefaults` constants. Injected into `TodoPromptService` with precedence: `IOptionsMonitor<TodoPromptOptions>` > file template > built-in default. (3) `PairingHtmlRenderer` replaces static `PairingHtml` calls with DI-injected instance class, loading templates from `IPromptTemplateService` by well-known IDs (`pairing-login-page`, `pairing-key-page`, `pairing-not-configured-page`) using `string.Replace` token substitution (`{errorBanner}`, `{apiKey}`, `{serverUrl}`), falling back to `PairingHtml` static methods. Template YAML files ship via `.csproj` Content items and are preserved across deployments.
**Covered by:** `IMarkerPromptProvider`, `FileMarkerPromptProvider`, `ITodoPromptProvider`, `TodoPromptProvider`, `PairingHtmlRenderer`, `templates/prompt-templates.yaml`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-006

**Template Resolution for One-Shot Requests** — Template rendering SHALL support:
- Explicit template mode: `promptTemplateId` + optional values dictionary.
- Context resolution mode: context-based template selection when template ID is omitted.
- Value precedence: caller-provided values override workspace-context-derived values on key collision.
- Placeholder binding: request `id` injected into render variables for `{id}` substitution.

For `AdHoc` context with no template ID, explicit ad-hoc prompt text is required.

The server SHALL provide a prompt resolution endpoint returning the populated prompt for a given template ID and values dictionary.


**Covered by:** `PromptTemplateController` *(planned extension)*, `PromptTemplateRenderer`, `AgentPoolController` *(planned)*
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-007

**Marker template requires actionable requirements-backed plans** — The default marker prompt must instruct every agent in every workspace to make plans decision-complete, capture FR/TR/TEST requirements, include explicit TDD unit-test expectations, and preserve Byrd gates so implementation agents can execute the plan directly.
**Covered by:** FR: FR-MCP-104; TEST: TEST-MCP-137, TEST-MCP-141
**Status:** pending
Scope: layer-1+

## TR-MCP-TR-001

**Tool Registry Service** — Keyword search across tool tags (bidirectional singular/plural contains matching), name, and description. Results combine global tools (`WorkspacePath == null`) with workspace-scoped tools. Full CRUD for `ToolDefinitionEntity` and `ToolDefinitionTagEntity`.
**Covered by:** FR: FR-MCP-012
**Status:** pending
Scope: layer-1+

## TR-MCP-TR-002

**Tool Bucket Service** — GitHub repository browsing via `gh api /repos/{owner}/{repo}/contents{path}?ref={branch}`. Reads and parses `stdio-tool-contract.json` manifests for install and sync operations. Persists bucket state to `ToolBucketEntity`.
**Covered by:** FR: FR-MCP-012
**Status:** pending
Scope: layer-1+

## TR-MCP-TR-003

**Tool Registry Default Bucket Seeding** — On startup, `Program.cs` reads `Mcp:ToolRegistry:DefaultBuckets` and calls `IToolBucketService.EnsureDefaultBucketsAsync` to register any configured buckets not already in the database. Idempotent: existing buckets are not modified.
**Covered by:** FR: FR-MCP-012, FR-MCP-022
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-001

**Bounded transcript reader and parser registry** — Implement streaming transcript readers with size, line, record, timeout, and cancellation bounds plus deterministic source adapter selection.
**Covered by:** FR: FR-MCP-TRANSCRIPT-001, FR-MCP-TRANSCRIPT-002; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-003, TEST-MCP-TRANSCRIPT-007, TEST-MCP-TRANSCRIPT-002, TEST-MCP-TRANSCRIPT-009
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-002

**Source adapters and bundle detection** — Implement adapters for Claude, Codex, Grok, Cline, Copilot, and OpenCode plus bundle detection for native files, folders, ZIPs, and SQLite snapshots.
**Covered by:** FR: FR-MCP-TRANSCRIPT-001, FR-MCP-TRANSCRIPT-002, FR-MCP-TRANSCRIPT-008; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-003, TEST-MCP-TRANSCRIPT-007, TEST-MCP-TRANSCRIPT-002, TEST-MCP-TRANSCRIPT-009, TEST-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-011, TEST-MCP-TRANSCRIPT-012
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-003

**Neutral event model and projectors** — Define loss-aware neutral transcript events and Claude, Codex, and Grok compatibility projectors while preserving provenance and derived-value markers.
**Covered by:** FR: FR-MCP-TRANSCRIPT-002, FR-MCP-TRANSCRIPT-003, FR-MCP-TRANSCRIPT-008; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-002, TEST-MCP-TRANSCRIPT-003, TEST-MCP-TRANSCRIPT-009, TEST-MCP-TRANSCRIPT-004, TEST-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-011, TEST-MCP-TRANSCRIPT-012
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-004

**Canonical YAML and write-ahead persistence** — Use existing SessionLogIngestor, UnifiedSessionLog DTOs, and IYamlSerializer to write deterministic importRecovery YAML and persist through the current session-log path.
**Covered by:** FR: FR-MCP-TRANSCRIPT-003, FR-MCP-TRANSCRIPT-004; TEST: TEST-MCP-TRANSCRIPT-002, TEST-MCP-TRANSCRIPT-004, TEST-MCP-TRANSCRIPT-005
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-005

**REPL import and model-owned session logging** — Expose shared transcript conversion/import through non-plugin typed server, client, MCP, and REPL surfaces while keeping plugin packages free of transcript ingestion endpoints, helpers, skills, handwritten YAML, and parser forks.
**Covered by:** FR: FR-MCP-TRANSCRIPT-005; TEST: TEST-MCP-TRANSCRIPT-006, TEST-MCP-TRANSCRIPT-010
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-006

**Recursive discovery upload extraction and path security** — Implement recursive bundle discovery, upload extraction, ZIP defenses, allowlisted roots, traversal rejection, and symlink/reparse escape prevention.
**Covered by:** FR: FR-MCP-TRANSCRIPT-001, FR-MCP-TRANSCRIPT-006; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-003, TEST-MCP-TRANSCRIPT-007, TEST-MCP-TRANSCRIPT-008
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-007

**HTTP client MCP and REPL contracts** — Add typed REST request/result models, SessionLogClient ingestion methods, native MCP tools, and REPL commands for path ingestion and normalization.
**Covered by:** FR: FR-MCP-TRANSCRIPT-006, FR-MCP-TRANSCRIPT-007; TEST: TEST-MCP-TRANSCRIPT-007, TEST-MCP-TRANSCRIPT-008
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-008

**Provider normalization and native stores** — Support secondary provider normalization for Cline paired JSON, Copilot event streams, OpenCode JSONL exports, and read-only OpenCode SQLite snapshots.
**Covered by:** FR: FR-MCP-TRANSCRIPT-008; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-009, TEST-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-011, TEST-MCP-TRANSCRIPT-012
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-009

**Codex transcript ingestion normalizes tool-call, reasoning, and turn_context records** — Codex transcript ingestion SHALL normalize tool-call, reasoning, and turn_context rollout records so known record classes do not emit codex_missing_role or codex_unknown_record warnings. AC1: ingesting a Codex rollout fixture produces 0 warnings for these known classes and the full event count (vs the old ~2432 warnings). Guards BUG-TRIAGE-080 via TEST-MCP-TRANSCRIPT-011 and TEST-MCP-TRANSCRIPT-012.
**Covered by:** FR: FR-MCP-TRANSCRIPT-008; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-009, TEST-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-011, TEST-MCP-TRANSCRIPT-012
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-010

**Int32.MaxValue transcript ceilings with streaming JSONL reader** — Implements FR-MCP-TRANSCRIPT-009. (1) TranscriptUtilities.ReadJsonLinesAsync raises maxSourceFileBytes from 256 MiB, maxLineBytes from 8 MiB, and maxRecords from 2,000,000 to int.MaxValue. (2) SessionLogTranscriptIngestionController raises MaxUploadRequestBytes from 512 MiB, MaxSourceFileBytes from 256 MiB, and MaxExpandedUploadBytes from 2 GiB to int.MaxValue; MaxArchiveEntries (10,000) and MaxCompressionRatio (20.0) are unchanged. RequestSizeLimit remains a compile-time constant attribute and continues to work because int.MaxValue is a constant. (3) ReadJsonLinesAsync streams lines through a StreamReader instead of calling File.ReadAllLinesAsync, so a ceiling of int.MaxValue does not convert a rejection into an out-of-memory kill. IMPLEMENTED 2026-07-20. Evidence: red gate IngestionService_AcceptsJsonlLineAboveFormerCeiling failed with "Transcript JSONL line exceeds the 8 MiB limit" at TranscriptUtilities.cs:104 (1 failed, 1 passed); green gate Support.Mcp.Tests 1692/0/0, Repl.Core.Tests 810/0/0, Client.Tests 259/0/0, transcript integration subset 10/0/0. Deployed to the McpServer Windows service (1.4.20, pid 39404) and verified live: sessionlog_ingest_path accepted a 9,437,285-byte single-line JSONL returning totalSessions 1 with zero diagnostics.
**Covered by:** FR: FR-MCP-TRANSCRIPT-009; TEST: TEST-MCP-TRANSCRIPT-013
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-SEARCH-001

**CodexTranscriptAdapter cases for tool_search and inter_agent** — CodexTranscriptAdapter.NormalizeAsync handles inter_agent_communication_metadata, tool_search_call, and tool_search_output. Coverage tests use inline JSONL. Covered by FR-MCP-TRANSCRIPT-SEARCH-001 TEST-MCP-TRANSCRIPT-SEARCH-001.
**Covered by:** FR: FR-MCP-TRANSCRIPT-SEARCH-001; TEST: TEST-MCP-TRANSCRIPT-SEARCH-001
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGE-001

**Durable triage storage** — Durable EF entities store reports, groups, research runs, statuses, idempotency keys, and workspace filters.
**Covered by:** FR: FR-MCP-TRIAGE-001; TEST: TEST-MCP-TRIAGE-001
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGE-002

**Deterministic triage grouping** — The grouping service uses workspace, dedupeKey, component, path, symbol, error signature, normalized title tokens, and McpServer workspace routing for MCP Server core and plugin bugs.
**Covered by:** FR: FR-MCP-TRIAGE-002; TEST: TEST-MCP-TRIAGE-002
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGE-003

**Async triage worker** — A background worker handles quiet-period expiry, configured agent execution, prompt rendering, and timeouts.
**Covered by:** FR: FR-MCP-TRIAGE-003; TEST: TEST-MCP-TRIAGE-003
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGE-004

**Triage schema and TODO creation** — Triage research output is schema-validated and converted idempotently into BUG-TRIAGE TODOs.
**Covered by:** FR: FR-MCP-TRIAGE-004; TEST: TEST-MCP-BUGTRIAGE-043, TEST-MCP-TRIAGE-004, TEST-MCP-TRIAGE-005, TEST-MCP-TRIAGE-006
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGE-005

**Triage Grok runner omits CLI-rejected flags** — The triage Grok CLI runner SHALL NOT pass --model auto or --effort max, both of which the Grok CLI rejects at startup. AC1: BuildGrokArgumentList omits --model when the model is the 'auto' sentinel. AC2: effort is pinned to 'high' (HighestEffort), never 'max'. Guards BUG-TRIAGE-076 via TEST-MCP-BUGTRIAGE-043.
**Covered by:** FR: FR-MCP-TRIAGE-004; TEST: TEST-MCP-BUGTRIAGE-043, TEST-MCP-TRIAGE-004, TEST-MCP-TRIAGE-005, TEST-MCP-TRIAGE-006
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGEERR-001

**Shared error classifier and envelope** — One shared classifier produces code, message, retryable, and details. McpToolErrors, the /mcpserver exception filter, the REPL error envelope, and the plugin shim consume it. backend_unavailable is retryable true. persistence_error, validation_error, not_found, and conflict are retryable false unless SQLITE_BUSY or deadlock.
**Covered by:** FR: FR-MCP-TRIAGEERR-001; TEST: TEST-MCP-TRIAGEERR-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A persistence failure keeps its own classification when EF Core's execution strategy reports it inside its transient-failure InvalidOperationException, as the SQL Server provider's default strategy does for a deadlock: a SQL Server deadlock (error 1205, recognized by its error number whatever the language of the server's message) raised while saving is persistence_error, retryable true, with the provider text in details.inner, as the same DbUpdateException is when unwrapped; a wrapped unique violation stays conflict, retryable false; an InvalidOperationException that carries no database error stays internal_server_error, retryable false (PLAN-SESSIONTRIAGEFIX-001 A7 follow-up, triage-report-905c3ac1b6af4d57b9c24c2383642379, operator decision 2026-10-16 "Fix it in this plan").

## TR-MCP-TRIAGEHELP-001

**Agent Help no completed echo and long timeout** — AgentHelpConversationService and GrokCliAgentExecutionStrategy do not mark echo-fallback or progress-only output as completed. UseEchoHelperFallback default false or never sets completed. Plugin long timeout covers workflow.agenthelp.submitTurn.
**Covered by:** FR: FR-MCP-TRIAGEHELP-001; TEST: TEST-MCP-TRIAGEHELP-001
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGEPLUGIN-001

**Sticky root session cache rebind and degraded persist** — plugins/core cache-scope keeps root session-state for UserPromptSubmit. ReplacePluginCache retains or rebinds. Resolve-McpCacheDir uses hook workspace path when env and profile cwd would fail. Get-ReplMethodTimeoutSeconds long list includes workflow.agenthelp.submitTurn. beginTurn degraded path and completeTurn persist identity are implemented in repl-invoke.ps1.
**Covered by:** FR: FR-MCP-TRIAGEPLUGIN-001; TEST: TEST-MCP-TRIAGEPLUGIN-001, TEST-MCP-TRIAGEPLUGIN-002, TEST-MCP-TRIAGEPLUGIN-003, TEST-MCP-TRIAGEPLUGIN-004, TEST-MCP-TRIAGEPLUGIN-005
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGEREQ-001

**ValidateTrId create only** — RequirementsWorkflow applies TrIdPattern only on create and batch create. getTr updateTr deleteTr accept any non-empty id and resolve the store, returning 404 if missing.
**Covered by:** FR: FR-MCP-TRIAGEREQ-001; TEST: TEST-MCP-TRIAGEREQ-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] RequirementsWorkflow applies the FR and TEST id patterns only on createFr, createTest and the create batches; getFr, updateFr, deleteFr, getTest, updateTest, deleteTest and the FR, TEST and mixed update batches check only that the id is present, as the TR operations already do, and resolve the store (PLAN-SESSIONTRIAGEFIX-001 B7, triage-report-3bd514bb).

## TR-MCP-TRIAGESCHEMA-001

**Startup probe for AgentSession header columns** — Startup schema probe requires SessionLogs AgentSessionId, AgentSessionTranscriptFile, AgentExecutablePath, and AgentExecutableVersion. Apply Sqlite 20260818205751_AddSessionLogTagsAndAgentSessionHeaders via MigrateAsync, SqlServer 20260818205807_AddSessionLogTagsAndAgentSessionHeaders via captured Up() SQL on disposable LocalDB, and Postgres 20260818205822_AddSessionLogTagsAndAgentSessionHeaders via captured Up() SQL on disposable local PostgreSQL. Missing columns fail closed with pending-migration, not raw SQL Invalid column name on query.
**Covered by:** FR: FR-MCP-TRIAGESCHEMA-001; TEST: TEST-MCP-TRIAGESCHEMA-001
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGESTORE-001

**Session-log merge tags replace supersede** — SessionLogService SameAction identity is order plus type plus filePath plus description. SessionLogEntity stores session tags. ReplaceTurn missing requestId is 404. Plugin superseded persist stamps planFile and todoId None and uses UpsertTurn. Canceled status persists and is documented.
**Covered by:** FR: FR-MCP-TRIAGESTORE-001; TEST: TEST-MCP-TRIAGESTORE-001, TEST-MCP-TRIAGESTORE-002, TEST-MCP-TRIAGESTORE-003, TEST-MCP-TRIAGESTORE-004, TEST-MCP-TRIAGESTORE-005, TEST-MCP-TRIAGESTORE-006, TEST-MCP-TRIAGESTORE-007
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGESTORE-002

**Five second intake and submit storage budget** — TriageService.SubmitReportAsync and session-log SaveChanges used by beginTurn persist use a 5 second connect and command budget. Timeouts map to backend_unavailable. No partial triage rows.
**Covered by:** FR: FR-MCP-TRIAGESTORE-002; TEST: TEST-MCP-TRIAGESTORE-007
**Status:** pending
Scope: layer-1+

## TR-MCP-TRIAGETODO-001

**Durable EXEC fallback and soft-delete id allocate** — TodoExecutionService FindTodo falls back to ITodoService.GetByIdAsync with path normalization and rehydrates EXEC records. EfTodoService CreateAsync and GenerateNextTodoIdAsync use IgnoreQueryFilters for soft-deleted collisions. Batch create is transactional or compensating.
**Covered by:** FR: FR-MCP-TRIAGETODO-001; TEST: TEST-MCP-TRIAGETODO-001, TEST-MCP-TRIAGETODO-002
**Status:** pending
Scope: layer-1+

## TR-MCP-TUN-001

**Tunnel Strategy Pattern** — DI registration in `Program.cs` reads `Mcp:Tunnel:Provider`, normalizes to uppercase, and uses `ActivatorUtilities.CreateInstance<T>` to instantiate the matching provider (`NgrokTunnelProvider`, `CloudflareTunnelProvider`, or `FrpTunnelProvider`). The provider is registered as both a singleton and an `IHostedService`, conditionally on the provider name being non-empty.
**Covered by:** FR: FR-MCP-015, FR-MCP-059
**Status:** pending
Scope: layer-1+

## TR-MCP-TUN-002

**Tunnel Process Lifecycle** — `Process.Kill()` is wrapped in a try-catch for `InvalidOperationException` to handle races. `WaitForExit(5000)` enforces a 5 s shutdown timeout. FRP config files written to temp storage are deleted on stop. All three providers log start, stop, and error events.
**Covered by:** FR: FR-MCP-015
**Status:** pending
Scope: layer-1+

## TR-MCP-TUN-003

**Ngrok Auth Token Security** — The ngrok auth token is passed via the `NGROK_AUTHTOKEN` environment variable on the child process, rather than as a CLI argument, to prevent exposure in process listings and shell history.
**Covered by:** FR: FR-MCP-015
**Status:** pending
Scope: layer-1+

## TR-MCP-TUN-004

**Tunnel provider inherits the interactive user environment** — Implements FR-MCP-141 and extends TR-MCP-TUN-003, which already establishes that NgrokTunnelProvider manipulates child-process environment variables. NgrokTunnelProvider SHALL take IProcessEnvironmentService and apply the interactive user's USERPROFILE, HOME, APPDATA, and PATH to the ProcessStartInfo of the ngrok child process, so ngrok.yml resolves when the server runs under LocalSystem instead of the provider silently starting with the service account profile. The provider SHALL resolve the ngrok binary through ResolveExecutable against that enriched PATH rather than relying on the literal string ngrok. When the binary cannot be found the provider SHALL log at Warning rather than Error, and the message SHALL name the Windows-service and Store-alias causes and the available remedies. Recovered from origin/claude/busy-dubinsky. Note that develop ships Tunnel Provider empty and Ngrok.Enabled false, so this path is dormant under stock configuration.
**Covered by:** FR: FR-MCP-141; TEST: TEST-MCP-190, TEST-MCP-191
**Status:** pending
Scope: layer-1+

## TR-MCP-TXN-001

**Turn Transaction Coordinator** — Add `Mcp:TurnTransactions`, `ITurnTransactionCoordinator`, transaction request/result models, keyserver/subscriber client handoff, direct/HTTP/external broker pub-sub adapters, durable local pub-sub outbox/replay, degraded status, pending-commit cancellation, and first-party mutation gates. Mutation paths SHALL either use compensation-capable coordinator execution or fail closed before uncompensated side effects while required turn transactions are active. Operations that TR-MCP-TXNKEY-001 routes around the coordinator (every non-QuadBrain operation) invoke the inner mutation directly: TransactionGatedSessionLogService keeps no snapshot, restore, or revive path because every session-log operation is non-QuadBrain (FR-MCP-173), and it fails closed if the keyserver scope would ever route a session-log operation to the coordinator (operator decision 2026-10-13, PLAN-SESSIONTRIAGEFIX-001 A1, A2, A4).

**Covered by:** `TurnTransactionCoordinator`, `TransactionPubSubServices`, `TransactionPubSubReplayWorker`, `TurnTransactionFederationOperationApplyService`, `TransactionGatedMemoryService`, `TransactionGatedTodoMutationService`, `TransactionGatedRepoFileService`, `TransactionGatedPromptTemplateService`, `TransactionGatedRequirementsDocumentService`, `TransactionGatedSessionLogService`, `TransactionGatedToolRegistryService`, `TransactionGatedToolBucketService`, `TransactionGatedGraphRagService`, `TransactionGatedGitHubCliService`, `TransactionGatedIssueTodoSyncService`, `TransactionGatedVoiceConversationService`, `TransactionGatedAgentPoolService`, `ClientMutationPolicy`, `FederationController`, `MemoryController`, `TodoController`, `McpServerMcpTools`, `TransactionalTodoWorkflow`, `TurnTransactionCoordinatorTests`, `TransactionPubSubTests`, `TransactionGatedMemoryServiceTests`, `TransactionGatedTodoMutationServiceTests`, `TransactionGatedSessionLogServiceTests`, `ClientMutationPolicyTests`, `TransactionalTodoWorkflowTests`
**Status:** pending
Scope: layer-1+

## TR-MCP-TXNAIUNIT-001

**aiUnit Plan Review Gate** — Add a test-only aiUnit plan-review evidence gate for PLAN-TURNTRANSACTIONS-001. The gate SHALL validate committed aiUnit run-log evidence, require the reviewed scope to include FR-MCP-118 through FR-MCP-128 and TEST-MCP-158 through TEST-MCP-173, and fail when critical/high findings are present.

**Covered by:** `PlanTransactionReviewTests`, `artifacts/aiunit-plan-review/aiunit-review-plan-20260612T060729.901Z.json`
**Status:** pending
Scope: layer-1+

## TR-MCP-TXNARCH-001

**Transaction Architecture Rounds** — Preserve a first architecture round that defines component ownership, trust boundaries, storage boundaries, threat model, rollback/audit rules, and gap analysis before implementation closeout.

**Covered by:** `TurnTransactions-Architecture-Round1.md`, `TurnTransactionPlanArtifactTests`
**Status:** pending
Scope: layer-1+

## TR-MCP-TXNAUDIT-001

**Transaction Audit Actions** — Transaction code SHALL record structured audit/session-log evidence for manifest sign/verify, commit/reject, abort, degraded, rollback, replay, retention, and aiUnit review events without deleting durable audit rows during rollback.

**Covered by:** `TransactionSecurityStateStores`, `TransactionPubSubServices`, `TurnTransactionCoordinator`, `TransactionGatedSessionLogService`, `TurnTransactionsControllerTests`, `TransactionPubSubTests`, `DurableTransactionSecurityStorageTests`, `PlanTransactionReviewTests`
**Status:** pending
Scope: layer-1+

## TR-MCP-TXNBYRD-001

**Byrd v4 Transaction Gates** — Transaction implementation work SHALL be split into requirements-first, test-first, mock-first, implementation, refactor, and validation gates. Executed validation scopes SHALL exit with zero failures and zero skips; deferred work belongs in TODO/requirements state rather than skipped test placeholders.

**Covered by:** `Functional-Requirements.md`, `Testing-Requirements.md`, `Requirements-Matrix.md`, `TurnTransactionPlanArtifactTests`, `ValidateTraceability`
**Status:** pending
Scope: layer-1+

## TR-MCP-TXNCOMPAT-001

**Federation Compatibility** — Existing `Mcp:Federation` HMAC envelopes SHALL remain backward compatible. Transaction crypto is additive and separate from federation envelope signing. Federation apply paths route through the coordinator, and federation control-plane mutations fail closed while required transaction gating is active until full compensation is designed.

**Covered by:** `TurnTransactionFederationOperationApplyService`, `FederationController`, `FederationOperationApplyServiceTests`, `FederationControllerTests`, `FederationControllerPushTests`, `ClientMutationPolicyTests`, `TurnTransactions-Mutation-Endpoint-Audit.md`
**Status:** pending
Scope: layer-1+

## TR-MCP-TXNDESIGN-001

**Implementable Transaction Design Contracts** — Preserve a second design round that defines public DTOs, durable entities, options, interfaces, endpoint contracts, reason codes, audit payloads, XMLDoc obligations, canonicalization, test mappings, and explicit deferred scope before closeout.

**Covered by:** `TurnTransactions-Design-Round2.md`, `TransactionSecurityModels`, `TransactionSecurityOptions`, `TransactionSecurityServices`, `TurnTransactions-Mutation-Endpoint-Audit.md`, `TurnTransactionPlanArtifactTests`
**Status:** pending
Scope: layer-1+

## TR-MCP-TXNDIAGRAMS-001

**Imported Diagram Traceability** — Imported diagrams SHALL be preserved as views of docs/architecture/c4/workspace.dsl whose keys are the imported diagram IDs, with source-section references, branch IDs (element and relationship properties), scope annotations, and test mappings. In-scope branches SHALL have tests; future quad-model, Curiosity, AoT, and weight-update branches SHALL remain explicitly deferred.

**Covered by:** `Quad-Model-Transactional-Diffgram-Plan.md`, `docs/architecture/c4/workspace.dsl`, `TurnTransactions-Architecture-Round1.md`, `TurnTransactions-Design-Round2.md`, `Testing-Requirements.md`, `TurnTransactionPlanArtifactTests`
**Status:** pending
Scope: layer-1+

## TR-MCP-TXNKEY-001

**Keyserver gate is QuadBrain/brain-slot only** — TurnTransactionKeyserverScope SHALL treat only publisher party ids with prefix brain-slot: and operation names with prefix brain-slot. or quadbrain. as requiring keyserver signing. TransactionGated adapters, TransactionalTodoWorkflow, TurnTransactionFederationOperationApplyService, RequirementsController ingest, ContextController mutations, FederationController control-plane mutations, and STDIO context mutations SHALL call ShouldBypassCoordinator and invoke the inner mutation directly for every non-QuadBrain operation even when Mcp:TurnTransactions:Enabled=true and RequiredForMutations=true. TurnTransactionCoordinator.ExecuteAsync SHALL also bypass SignManifestAsync unless RequiresKeyserver is true. RepairWorkspaceStampsAsync remains a workspace-wide fail-closed path.
Acceptance Criteria:
- ac-1: ShouldBypassCoordinator returns true for todo.update, requirements.fr.update, sessionlog.submit, memory.add, repo.write, github.cli, workflow.todo.update, federation.control, context.mutate, and requirements.ingest when a coordinator is registered.
- ac-2: ShouldBypassCoordinator returns false for brain-slot.invoke and brain-slot.weight-update when a coordinator is registered.
- ac-3: ExecuteAsync does not call SignManifestAsync for general-agent operation names even when Enabled and RequiredForMutations are true.
**Covered by:** FR: FR-MCP-173; TEST: TEST-MCP-221
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-001

**Use case EF storage multi-provider soft-delete** — Implement Use Case 4NF entities on McpDbContext with WorkspaceId max 1024, FK to Workspaces, soft-delete columns, Restrict/NoAction relationships, string FrId to RequirementEntity Kind=fr, global workspace query filters, and migrations for SQLite, PostgreSQL, and SQL Server named AddUseCaseSupport (timestamped). Audit emission for mutable entities per TR-MCP-DB-004.
**Covered by:** FR: FR-MCP-USECASE-001, FR-MCP-USECASE-002, FR-MCP-USECASE-003; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-002, TEST-MCP-USECASE-004, TEST-MCP-USECASE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-002

**Use case CQRS commands and queries** — All use case domain mutations and reads go through McpServer.Cqrs ICommand/IQuery handlers registered with Dispatcher. Controllers and MCP tools only dispatch; they do not open DbContext for domain logic.
**Covered by:** FR: FR-MCP-USECASE-001, FR-MCP-USECASE-002, FR-MCP-USECASE-003, FR-MCP-USECASE-004; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-002, TEST-MCP-USECASE-004, TEST-MCP-USECASE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-003

**Use Case REST controller** — Thin /mcpserver/usecases dispatching CQRS. Covered by FR-MCP-USECASE-001; TEST-MCP-USECASE-002. Controller unit tests map Result failures to HTTP status codes.
**Covered by:** FR: FR-MCP-USECASE-001; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-002, TEST-MCP-USECASE-004
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-004

**Use Case diagram service** — DI-owned pure generator; mermaid + one extra format. Covered by FR-MCP-USECASE-005; TEST-MCP-USECASE-003.
**Covered by:** FR: FR-MCP-USECASE-005; TEST: TEST-MCP-USECASE-003
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-005

**Use Case MCP tools, client, plugins** — usecase_* tools; UseCaseClient live JSON parity; plugin-core + skills; REPL client.UseCases. Covered by FR-MCP-USECASE-001, FR-MCP-USECASE-007; TEST-MCP-USECASE-004, TEST-MCP-USECASE-010.
**Covered by:** FR: FR-MCP-USECASE-001, FR-MCP-USECASE-007; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-002, TEST-MCP-USECASE-004, TEST-MCP-USECASE-008, TEST-MCP-USECASE-010, TEST-MCP-USECASE-011
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-006

**FR projection, coverage, audit** — linkedUseCases on FR get/list; coverage API; TR-MCP-DB-004 audit on mutable UC ops. Covered by FR-MCP-USECASE-003, FR-MCP-USECASE-006; TEST-MCP-USECASE-005, TEST-MCP-USECASE-007.
**Covered by:** FR: FR-MCP-USECASE-003, FR-MCP-USECASE-006; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-007

**Use Case UI hosting** — Static /usecases/ assets REST-only. Covered by FR-MCP-USECASE-007; TEST-MCP-USECASE-008.
**Covered by:** FR: FR-MCP-USECASE-007; TEST: TEST-MCP-USECASE-008, TEST-MCP-USECASE-010, TEST-MCP-USECASE-011
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The web root never hands out the content of a Markdown file. Folder README.md files under wwwroot are not static web assets and are not copied to build or publish output, and a request for any .md file under wwwroot (any case of the extension) does not return that file content, including when the server runs from the source tree and reads wwwroot from disk. UI assets (.html, .js, .css) are still served. Holds on any host and for any content root.

## TR-MCP-USECASE-008

**Approval and product API** — CQRS + REST for approval and ProductKey. Covered by FR-MCP-USECASE-008, FR-MCP-USECASE-009; TEST-MCP-USECASE-009.
**Covered by:** FR: FR-MCP-USECASE-008, FR-MCP-USECASE-009; TEST: TEST-MCP-USECASE-009
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-009

**Validation seams** — Docs matrix includes USECASE IDs; shared Realizes algorithm for DB findings. Covered by FR-MCP-USECASE-010; TEST-MCP-USECASE-005.
**Covered by:** FR: FR-MCP-USECASE-010; TEST: TEST-MCP-USECASE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-010

**Use Case service deploy** — Nuke UpdateService only; config backup/restore; live health and routes. Covered by FR-MCP-USECASE-007; TEST-MCP-USECASE-011.
**Covered by:** FR: FR-MCP-USECASE-007; TEST: TEST-MCP-USECASE-008, TEST-MCP-USECASE-010, TEST-MCP-USECASE-011
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-011

**Graph storage** — JSON column graph storage; multi-provider migrations; soft-delete and workspace filters. ACs AC-T11-1..3.
**Covered by:** FR: FR-MCP-USECASE-012; TEST: TEST-MCP-USECASE-013, TEST-MCP-USECASE-014, TEST-MCP-USECASE-016
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-012

**Diagram graph CQRS** — GetGraph/PutGraph/Export handlers; thin controller. ACs AC-T12-1..2.
**Covered by:** FR: FR-MCP-USECASE-012; TEST: TEST-MCP-USECASE-013, TEST-MCP-USECASE-014, TEST-MCP-USECASE-016
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-013

**Diagram graph REST** — GET/PUT diagram-graph; GET diagram kind=usecase format mermaid|plantuml; sequence remains. ACs AC-T13-1..3.
**Covered by:** FR: FR-MCP-USECASE-012, FR-MCP-USECASE-013, FR-MCP-USECASE-014; TEST: TEST-MCP-USECASE-013, TEST-MCP-USECASE-014, TEST-MCP-USECASE-016, TEST-MCP-USECASE-012
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-014

**UML serialization service** — Pure DI service graph to Mermaid and PlantUML. ACs AC-T14-1..2.
**Covered by:** FR: FR-MCP-USECASE-013, FR-MCP-USECASE-014; TEST: TEST-MCP-USECASE-012, TEST-MCP-USECASE-014
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-015

**Canvas UI hosting** — Static /usecases/ canvas assets; palette; umlCanvas; REST-only. ACs AC-T15-1..2.
**Covered by:** FR: FR-MCP-USECASE-011; TEST: TEST-MCP-USECASE-015, TEST-MCP-USECASE-017
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-016

**Graph put audit** — Put graph emits DataAuditLog. AC-T16-1.
**Covered by:** FR: FR-MCP-USECASE-012; TEST: TEST-MCP-USECASE-013, TEST-MCP-USECASE-014, TEST-MCP-USECASE-016
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-017

**Handle-relative containment, bounded providers, canonical identity, and hermetic evidence** — Normalize durable identifier strings before computing derived hashes. Use pinned-directory-relative Windows NT and POSIX operations for contained creation, open, replacement, rollback deletion, and empty-directory removal. Resolve physical paths through finite cancellable native operations used asynchronously by persistence and federation. Snapshot only regular files with no-follow nonblocking stat enforcement. Scope SQLite open and query work with exact restoration of managed, EF, connection-string, native timeout, and handler state; preserve real provider exceptions. Discover repository roots from stable assembly or source metadata and bound process plus pipe-drain lifetime for full process trees.
**Covered by:** FR: FR-MCP-USECASE-015; TEST: TEST-MCP-USECASE-018
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] On Linux with Landlock, a missing export root (with its missing ancestors) and missing nested export folders are created on a thread Landlock-confined to the pinned root or, for a missing root, its deepest existing ancestor (pinned from its approved mount without following links or crossing mounts); a parent moved outside before mkdirat is refused by the kernel and nothing appears outside (BUG-BASELINE-001; BugTriage139EighteenthLinuxTests EnsureDirectory_MissingNestedDirectory_IsCreatedUnderLandlock, EnsureDirectory_ParentMovedOutsideBeforeCreate_IsDeniedByLandlock, OpenExportRoot_MissingRootAndAncestors_IsCreatedUnderLandlock, LandlockConfinement_DeniesDirectoryCreationOutsideItsHierarchy).
- [ ] On Linux with Landlock, nested stale files and empty directories are deleted on a thread Landlock-confined to the root through the pinned parent after the target kind and identity match; a missing parent or target returns false; a parent moved outside before unlinkat is refused by the kernel and the entry survives; export cleanup deletes nested stale files (BUG-BASELINE-001; DeleteFileIfExists_NestedStaleFile_DeletesUnderLandlock, DeleteFileIfExists_NestedParentMovedOutsideBeforeUnlink_IsDeniedByLandlock, RequirementsExport_NestedStaleCleanup_DeletesStaleUnderLandlock).
- [ ] On Linux without Landlock the export fails closed as before: missing roots and nested folders throw DirectoryNotFoundException before any mutation and nested stale cleanup is refused before any generated write (BUG-BASELINE-001; EnsureDirectory_MissingNestedDirectory_FailsClosedBeforeMutation, OpenExportRoot_MissingRoot_FailsClosedWithoutLandlock, RequirementsExport_NestedStaleCleanup_IsRejectedBeforeGeneratedWrite).
- [ ] If the Landlock confinement cannot be established (ruleset creation, the path-beneath rule, no_new_privs, or restrict_self fails), the confined operation never runs and the call fails with an IOException, so no export mutation happens without kernel enforcement (BUG-BASELINE-001 what-if; BugTriage139EighteenthLinuxTests.LandlockConfinement_RuleSetupFailure_NeverRunsTheOperation, where the kernel rejects directory rights on a regular-file descriptor).
- [ ] A symbolic link planted at a missing export folder name before mkdirat is never followed: mkdirat reports EEXIST, the re-open beneath the anchor refuses the link, the call fails, and nothing is created through the link outside the root (BUG-BASELINE-001 what-if; BugTriage139EighteenthLinuxTests.EnsureDirectory_SymlinkPlantedAtMissingSegment_IsNeverFollowed).

## TR-MCP-USECASE-018

**Cross-platform bounded native I/O and deterministic evidence** — Use searchable Linux directory modes and descriptor-relative restoration. Isolate potentially blocking physical identity and SQLite pre-handle opens behind finitely owned platform cancellation mechanisms. Classify cancellation from direct operation evidence, and resolve repository fixtures from deterministic assembly metadata rather than mapped caller paths.
Converted from FR-MCP-USECASE-016 on 2026-10-19 (operator: "If it is in a FR, then that FR needs to be converted to a TR"; answers "Merge into its TR (Recommended)" and "FR-001, delete FRs (Recommended)"). FR-MCP-USECASE-016 stated: Use-case persistence and its supporting contained rollback, physical workspace identity, SQLite opening, cancellation classification, and repository evidence paths shall remain safe and finitely completing on every supported platform, including deterministic CI builds, while preserving exact provider failures and truthful supersession evidence.
**Covered by:** FR: FR-MCP-USECASE-001; TEST: TEST-MCP-USECASE-019
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Linux rollback directories retain owner search and execute permission, nested restoration reproduces file contents and mode, and all mutations remain contained.
- [ ] Physical workspace identity resolution completes within a finite bound on Windows and non-Windows without caller-thread blocking, cancellation gaps, abandoned workers, or silent platform returns.
- [ ] SQLite connection opening is finitely bounded before native-handle publication on every supported platform and leaves no worker, handle, callback, interrupt, pool, or lifecycle leak.
- [ ] Only outcomes precisely attributable to requested cancellation become OperationCanceledException; independent I/O, disposal, and provider failures retain their original type and details.
- [ ] Repository and Agent Help fixtures discover their root from true external deterministic builds with mapped CallerFilePath and no MCP_REPOSITORY_ROOT override.
- [ ] Every supported platform executes an applicable native contract assertion: a platform-specific test is a Category=Integration test tagged Platform=Linux or Platform=Windows for the host it applies to, which TR-MCP-NUKE-019 AC-1 and AC-2 route to that host's MigrationIntegrationTests and require to run and pass there, and it passes only as TR-MCP-USECASE-019 AC-008 states (operator 2026-10-30: "D3: dedupe lane rules (Recommended)"); no unit test depends on the platform or on the process environment, directly, through a test helper or through the production code it calls: a test whose outcome can change with the value of an environment variable it reads or writes, directly, through a test helper or through the production code it calls (for example a temporary-folder lookup that takes its folder from a variable and creates it), a test that imports the process environment into configuration (AddEnvironmentVariables or a default host builder), and a test that asserts on an environment value a child process inherits are Category=Integration tests that set and restore every variable their assertions or the code they call depend on, every name of a fallback chain the code reads, both forms where a variable has a canonical (QBrainAi, QBRAINAI_) and a legacy (Mcp, MCP_) form, and the tests that write the process environment run one at a time (operator 2026-10-21: "Fix both, strengthen ACs", "Strengthening Acceptance Criteria should always be the recommended option" and "Approve, strengthen AC-006/008 (Recommended)"; operator 2026-10-26: "Strengthen AC, move 14 (Recommended)", "Dependence + proof AC (Recommended)" and "Fix source, strengthen AC (Recommended)"; hostile validation findings R5-F2, R6-F1 and R7-F1). The hostile-environment runs are out of scope: they are not part of this criterion or its evidence (operator 2026-10-27: "Get ride of the hostile runs enforcing integration test separation.  I have already stated this once." and "Which I sated last night were out of scope."). An integration case may read the lane's own inputs without setting them: MCP_REPOSITORY_ROOT, the MCP_TEST_ connection variables the lane provisions, and, for an import that scans agent folders, the user's real profile folder, read only and never asserted on; every other variable its assertions or the code it calls depend on it sets and restores. A unit test asserts no wall-clock bound (operator 2026-10-29: "Environment ACs (Recommended)"; holistic review findings MCPT-11, SVC-6 and MCPT-12). These rules are stated here once for TR-MCP-USECASE-019 AC-008 as well; the environment-delta rule moved to FR-MCP-047 ac-1 (operator 2026-10-29: "Move criteria that are misplaced.  Unit tests are for FR, Integration tests are for TR.").
- [ ] The rejected fourteenth receipt is preserved with an explicit supersession notice, and the fifteenth evidence records every failed, inconclusive, and passing run with independently verified hashes and command mappings.

## TR-MCP-USECASE-019

**Race-safe physical I/O, causal cancellation, and immutable evidence** — Resolve and validate physical filesystem identity through interruptible native handles or isolated workers whose public deadlines and accounting remain truthful; bind SQLite opening to the validated object without lexical races; attribute PostgreSQL cancellation to the actual caller cancel request; compose platform-native tests and process containment without pass-through fallbacks; and generate exact-final-HEAD evidence through a mechanically verified immutable workflow.
Converted from FR-MCP-USECASE-017 on 2026-10-19 (operator: "If it is in a FR, then that FR needs to be converted to a TR"; answers "Merge into its TR (Recommended)" and "FR-001, delete FRs (Recommended)"). FR-MCP-USECASE-017 stated: Use-case persistence and its filesystem, provider, process, platform-test, and evidence boundaries shall close physical alias and replacement races, complete publicly within finite contracts, preserve truthful worker and provider causality, execute applicable native contracts, and report only exact immutable evidence while retaining every previously approved control.
**Covered by:** FR: FR-MCP-USECASE-001; TEST: TEST-MCP-USECASE-020
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Linux preflight classifies each path prefix with statx(AT_SYMLINK_NOFOLLOW) and reads link targets with readlink, so a symbolic link to any path inside a userspace or network mount (not only its root) is rejected from the mount table without sending that mount a request (BUG-BASELINE-009 C1; BugTriage139SixteenthLinuxTests.SqliteOpen_SafeSymlinkBelowFuseRoot_IsRejectedWithoutEnteringTheMount).
- [ ] The SQLite caller-side data-source preflight runs off the caller's thread under the open's single finite deadline and the caller's cancellation: OpenAsync returns its task at once, a preflight that does not return fails the open with TimeoutException at the bound, no open worker starts, and the preflight stays counted (ActivePreflightCount) until it actually returns (BUG-BASELINE-009 C2; SqliteBoundedOpenFourteenthTests.OpenAsync_PreflightHeldAtBoundary_TimesOutWithoutBlockingTheCaller).
- [ ] Linux workspace identity resolution resolves symbolic-link prefixes through the mount table before its anchored RESOLVE_NO_XDEV lookup, so an absolute alias within a nested approved mount (for example /tmp) resolves like the data-file and SQLite opens instead of failing with a mount transition (BUG-BASELINE-009 C3; WorkspaceIdentityFourteenthNativeTests.NativeResolver_RepeatedPlatformCallsLeaveNoActiveWorkers).
- [ ] A bounded traversal or file worker that uses the export root descriptor holds a reference on it from before it starts until it actually exits, so disposing the root at the public deadline defers the close: on Linux the root descriptor stays open while the worker runs and closes after it exits; on Windows the open root still blocks rename (BUG-BASELINE-009, operator decision 2026-10-11 "Fix it now"; BugTriage139EighteenthTraversalTests.EnumerateFilesBoundedAsync_BlockingMetadata_TimesOutWithTruthfulWorkerAccounting).
- [ ] Physical workspace identity rejects safe lexical paths whose physical target is on an unsafe mount, remains safe across symlink or mount replacement timing, completes publicly within its deadline, and reports any unstoppable worker as active until it actually exits.
- [ ] SQLite data-source validation and pre-handle opening resist safe-link and check-to-open replacement, use no ineffective Thread.Interrupt dependency for native Linux I/O, finish cleanup within a finite deadline, and preserve exact lifecycle, handle, worker, and exception state.
- [ ] PostgreSQL SQLSTATE 57014 is translated to caller cancellation only with causal evidence produced by the caller-triggered cancellation mechanism; statement timeout and administrative cancellation remain PostgresException even during concurrent token cancellation.
- [ ] A platform-specific test (TR-MCP-USECASE-018 AC-006) proves real execution and inventory counts and never passes through source inspection, skip, silent return, abort, inconclusive outcome, or a below-minimum count (operator 2026-10-30: "D3: dedupe lane rules (Recommended)"). A test fixture that needs a directory junction or link creates it with an operating-system call made in the test process, so no test outcome depends on how long a helper process takes to start (operator 2026-10-26: "Fix flake, strengthen AC (Recommended)"; at f5cda3c1 two Windows lane cases failed when a 5-second budget for starting pwsh.exe ran out under load; triage-report-8c2c30ef63d442bdb29fd785ec91e7d7). The rules this criterion shares with TR-MCP-USECASE-018 AC-006 (unit tests independent of the platform and the process environment, the lane inputs, the hostile-environment scope, no wall-clock bound in a unit test) are stated there once, and the environment-delta rule is FR-MCP-047 ac-1 (operator 2026-10-29: "Move criteria that are misplaced.  Unit tests are for FR, Integration tests are for TR.").
- [ ] Containment race coverage requires at least one successful contained read and write while proving no escape or corruption, and deterministic total-rejection tests prove that rejecting every attempt cannot satisfy the success gate.
- [ ] Windows descendant cleanup establishes containment at process creation or proves an equivalent race-free boundary before descendants can escape, and timeout or cancellation terminates immediate descendants finitely without regressing non-Windows process-group cleanup.
- [ ] Historical rejected receipts remain intact but are explicitly superseded; final evidence identifies the exact immutable tested SHA, contains executable commands or a mechanically complete ledger, independently reconciles hashes, mappings and totals, and limits conclusions to the proven local and retained-evidence boundary.

## TR-MCP-USECASE-020

**Identity-preserving use-case component update pipeline** — Implement workspace- and parent-scoped CQRS mutations for existing actor, flow, and step components, exposed consistently through REST, typed client, source-generated JSON, and MCP tool surfaces.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CQRS update commands scope every actor, flow, and step lookup by workspace and complete parent identity, use tracked in-place mutations, and return not-found for mismatches.
- [ ] Actor updates validate name and actor type, flow updates validate flow type, name, and positive sequence, and step updates validate positive order, nonempty action, and an actor associated with the same use case when ActorId is supplied.
- [ ] REST routes, the typed client, generated JSON metadata, and MCP tools expose update-actor, update-flow, and update-step with cancellation and classified failures.
- [ ] Each successful component update advances UseCaseEntity.UpdatedAtUtc, emits the established use-case mutation audit signal, and leaves approval/version and unrelated aggregate data unchanged.

## TR-MCP-VERIFYPOLICY-001

**Challenge-bound signed verification policy contract** — Read QBrainAi:VerificationPolicies with legacy Mcp fallback only when canonical section is absent. Select exactly one entry matching resolved workspace. Validate schema version, continuation bound 1-3, unique check ids, literal ordered argv, contained relative cwd/pathGlobs, code/doc triggers, explicit required flag, suite kind, turn/phaseGate scope and unique TODO phase selectors. Canonicalize set ordering while preserving argv order. Return SHA256 payload digest with HMAC-SHA256 binding format, fresh request challenge and digest to current marker key. Typed client verifies signature, challenge, canonical payload and workspace before exposing data. Keep generated JSON metadata and typed REPL task handling compatible with trimmed distribution.
**Covered by:** FR: FR-MCP-VERIFYPOLICY-001; TEST: TEST-MCP-VERIFYPOLICY-001
**Status:** pending
Scope: layer-1+

## TR-MCP-VERIFYWRAP-001

**code-verify catch disk-full and enforce wrapper timeout** — code-verify.ps1 catches IOException for disk full as a typed status and applies a hard timeout around the wrapper so the console is released. Covered by FR-MCP-VERIFYWRAP-001 TEST-MCP-VERIFYWRAP-001.
**Covered by:** FR: FR-MCP-VERIFYWRAP-001; TEST: TEST-MCP-VERIFYWRAP-001
**Status:** pending
Scope: layer-1+

## TR-MCP-VOICE-001

**Voice Conversation Service** — `VoiceConversationService` manages the full voice session lifecycle: session creation with `CopilotInteractiveSession` spawned via `DesktopProcessLauncher` (or standard `Process.Start`), turn processing with tool-call loop (max `MaxToolSteps` iterations), in-memory transcript storage, tool-call record tracking, and session cleanup. Configurable via `VoiceConversationOptions` bound from `Mcp:Voice` configuration section (model, timeouts, rate limits for writes/deletes per turn, transcript context limit).
**Covered by:** `VoiceConversationService`, `VoiceConversationOptions`, `CopilotInteractiveSession`
**Status:** pending
Scope: layer-1+

## TR-MCP-VOICE-002

**Voice Controller REST API** — `VoiceController` at `/mcpserver/voice/session/*` exposes 8 endpoints: `POST /` (create session with `DeviceId`/`Language`/`ClientName`), `GET /?deviceId=` (find by device), `POST /{id}/turn` (synchronous turn), `POST /{id}/turn/stream` (SSE streaming turn), `POST /{id}/interrupt` (cancel active turn), `POST /{id}/escape` (send ESC chars to Copilot stdin), `GET /{id}` (session status), `GET /{id}/transcript` (transcript entries), `DELETE /{id}` (destroy session). DTOs: `VoiceSessionCreateRequest/Response`, `VoiceTurnRequest/Response`, `VoiceInterruptResponse`, `VoiceSessionStatusDto`, `VoiceTranscriptEntryDto/Response`, `VoiceToolCallRecordDto`, `VoiceTurnStreamEvent`.
**Covered by:** `VoiceController`, `VoiceConversationContracts`
**Status:** pending
Scope: layer-1+

## TR-MCP-VOICE-003

**Voice Session Lifecycle Management** — One active session per device enforced via `DeviceId` lookup; creating a new session for a device with an active session returns the existing session. Idle timeout (`SessionIdleTimeoutMinutes`, default 15) triggers the configured idle-shutdown prompt sent to Copilot, waits for the configured sentinel response, then terminates the session. `UseDesktopLaunch` option (default true) selects `CreateProcessAsUser` for Windows service context.
**Covered by:** `VoiceConversationService.OnIdleCleanupTick` / `CleanupIdleSessionsAsync` (60s timer-driven idle-shutdown orchestrator), `VoiceConversationOptions.SessionIdleTimeoutMinutes`, `VoiceConversationOptions.IdleShutdownCommand` (config string sent to Copilot), `VoiceConversationOptions.IdleShutdownSentinel` (config string awaited as the shutdown confirmation). Note: `IdleShutdownCommand` and `IdleShutdownSentinel` are configured strings on `VoiceConversationOptions`, not CLR message types.
**Status:** pending
Scope: layer-1+

## TR-MCP-VOICE-004

**Interactive Presence Signals on Stream State Changes** — On interactive stream disconnect, the runtime SHALL send `User is AFK.` to the associated interactive agent session.
On interactive stream reconnect, after response stream establishment, the runtime SHALL send `User is here.` to the associated interactive agent session.

Presence signaling SHALL be excluded from one-shot sessions.


**Covered by:** `VoiceConversationService` *(planned extension)*, `AgentPoolStreamService` *(planned)*
**Status:** pending
Scope: layer-1+

## TR-MCP-WEB-001

**Web UI Ownership Boundary** — Web UI implementation work for the former McpServer.UI.Core and McpServer.Director surfaces SHALL be owned by the McpServerManager repository. This repository SHALL keep only server-side contracts, API behavior, and compatibility documentation required by those external UI clients.
**Status:** pending
Scope: layer-1+

## TR-MCP-WEB-002

**Web UI API Compatibility Contract** — Server APIs consumed by external web-management clients SHALL remain documented and version-compatible across McpServer and McpServerManager. Breaking API changes require explicit requirements updates, migration notes, and tests in the server repository before deployment.
**Status:** pending
Scope: layer-1+

## TR-MCP-WEB-003

**Web UI Authentication And Workspace Boundary** — External web-management clients SHALL authenticate through the existing MCP workspace auth/token model and SHALL preserve workspace isolation. This repository SHALL provide the server-side policy and tests; client UX and screen implementation remain in McpServerManager.
**Status:** pending
Scope: layer-1+

## TR-MCP-WEB-004

**Web UI Deployment And Handoff Documentation** — Deployment guidance for web-management surfaces SHALL distinguish server deployment in this repository from UI/client deployment in McpServerManager. This repository SHALL document only the server prerequisites, endpoint contracts, and compatibility expectations needed for the external UI.
**Status:** pending
Scope: layer-1+

## TR-MCP-WIKIEXPORT-001

**docs/wiki.yaml export configuration loader and renderer integration** — Implement a typed YamlDotNet-backed docs/wiki.yaml loader, validation layer, and wiki renderer integration shared by database-backed and document-backed requirements services.
**Covered by:** FR: FR-MCP-WIKIEXPORT-001; TEST: TEST-MCP-WIKIEXPORT-001
**Status:** pending
Scope: layer-1+

## TR-MCP-WIKIEXPORT-002

**Marker writer default wiki.yaml serializer** — Extend MarkerFileService.WriteMarkerAsync with an idempotent default wiki.yaml writer that builds typed objects and serializes them with YamlDotNet before writing docs/wiki.yaml.
**Covered by:** FR: FR-MCP-WIKIEXPORT-002; TEST: TEST-MCP-WIKIEXPORT-002
**Status:** pending
Scope: layer-1+

## TR-MCP-WIKIEXPORT-003

**Dump schema and export flag** — schemaVersion mcp-wiki-dump/v1. SHA-256 per table plus root hash. Flag default off. tables[] uses McpDbContext DbSet names.
**Covered by:** FR: FR-MCP-WIKIEXPORT-003; TEST: TEST-MCP-WIKIEXPORT-003
**Status:** pending
Scope: layer-1+

## TR-MCP-WIKIEXPORT-004

**Import remap and dump parameter binding** — Bind --dump to WorkspaceController.CreateAsync. Remap workspace id, paths, FKs. Idempotent re-import. Reject malformed dump, version mismatch, missing tables, unsafe path.
**Covered by:** FR: FR-MCP-WIKIEXPORT-004; TEST: TEST-MCP-WIKIEXPORT-004
**Status:** pending
Scope: layer-1+

## TR-MCP-WIKIEXPORT-005

**todo.yaml archive and conflict diagnostics** — Discovery classifies generated/shadow/deprecated. Archive is copy then optional remove with report. Conflict diagnostic includes both paths.
**Covered by:** FR: FR-MCP-WIKIEXPORT-005; TEST: TEST-MCP-WIKIEXPORT-005
**Status:** pending
Scope: layer-1+

## TR-MCP-WS-002

**Workspace Service** — CRUD operations for workspace entities persisted in EF Core SQLite. Auto-port assignment starts at base 7147 and increments from the current maximum registered port. Init scaffolding creates the workspace directory, `docs/Project/TODO.yaml`, `docs/sessions/`, `docs/external/`, and `mcp.db`.
**Covered by:** FR: FR-MCP-009
**Status:** pending
Scope: layer-1+

## TR-MCP-WS-003

**Workspace Process Manager** — Manages workspace marker file lifecycle. On startup, generates tokens and writes `AGENTS-README-FIRST.yaml` marker files for all registered workspaces - all pointing to the single shared host port. On stop, removes marker files. No longer spawns child `WebApplication` instances (replaced by single-app multi-tenant model, see TR-MCP-MT-001 through TR-MCP-MT-003).
**Covered by:** FR: FR-MCP-011
**Status:** pending
Scope: layer-1+

## TR-MCP-WS-004

**Workspace Controller** — REST API at `/mcpserver/workspace` with Base64URL-encoded path keys. Provides create, read, update, delete, init, start, stop, status, and prompt (GET/PUT) endpoints. All `/mcpserver/*` routes protected by `WorkspaceAuthMiddleware` (per-workspace token).
**Covered by:** FR: FR-MCP-009
**Status:** pending
Scope: layer-1+

## TR-MCP-WS-005

**Marker File Service** — `MarkerFileService.WriteMarkerAsync` writes `AGENTS-README-FIRST.yaml` to the workspace root. All markers point to the same shared host port. Uses Handlebars.Net templating with full workspace context. The marker template MUST be loaded from `templates/prompt-templates.yaml` via `PromptTemplateService` (id: `default-marker-prompt`). **CRITICAL**: If the template cannot be loaded (missing file, invalid YAML, or missing ID), the service must log a critical error and shut down the server immediately. Fallback to hardcoded templates is PROHIBITED. The YAML file contains port, `baseUrl`, all endpoint paths, process PID, `startedAt` timestamp, workspace name, per-workspace auth token (`apiKey`), and a machine-readable `prompt` block. Agents should send `X-Workspace-Path` header for workspace targeting.
**Covered by:** FR: FR-MCP-018
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] On the host's real file system, the directory walk that FR-MCP-018 ac-1 relies on reaches a real folder below the scan boundary, does not reach a scan root that is a directory link or one reached through a linked ancestor whose target lies outside the boundary, still reaches a root below a boundary that is itself a link, and flags directory and file links without descending a directory link; the integration case creates its links in its own temporary folder and removes them (operator 2026-10-30: "New TR binding criterion (Recommended)"; the real file-system binding of FR-MCP-018 ac-1 and of AC-FR-MCP-SESSIONLOGCTX-001-008).

## TR-MCP-WS-006

**Workspace Host Controller Isolation** — *Obsolete.* Replaced by single-app multi-tenant model (TR-MCP-MT-002). `ExcludeControllerFeatureProvider` can be removed.
**Covered by:** FR: FR-MCP-019
**Status:** pending
Scope: layer-1+

## TR-MCP-WS-007

**Workspace Auto-Start on Service Startup** — `WorkspaceProcessManager`, as an `IHostedService`, queries all registered workspaces on `StartAsync` and writes marker files for each. Failures on individual workspace marker writes are logged and skipped rather than aborting global startup.
**Covered by:** FR: FR-MCP-011, FR-MCP-020
**Status:** pending
Scope: layer-1+

## TR-MCP-WS-008

**Workspace Auto-Init and Auto-Start on Creation** — `WorkspaceController` POST calls `WorkspaceService.InitAsync` to scaffold the directory structure, then calls `WorkspaceProcessManager.StartAsync` to bring the host online, all within a single request, before returning 201 Created.
**Covered by:** FR: FR-MCP-021
**Status:** pending
Scope: layer-1+

## TR-MCP-WS-009

**Primary Workspace Detection and IsEnabled Gating** — `WorkspaceProcessManager.IHostedService.StartAsync` resolves the primary workspace: first by `IsPrimary = true` + lowest port among enabled workspaces; then by lowest-port enabled workspace if none is marked primary. For the primary workspace, only a marker file is written - no child `WebApplication` is created. Workspaces with `IsEnabled = false` are skipped during auto-start but can be started manually.
**Covered by:** FR: FR-MCP-025
**Status:** pending
Scope: layer-1+

## TR-MCP-WS-UI-001

**McpServer Management Web UI** — Reserved/planned: web-based management UI for workspace and server administration. Tracks FR-MCP-031.
**Covered by:** FR: FR-MCP-031
**Status:** pending
Scope: layer-1+

## TR-MCP-XAGENT-001

**Assert-ReplCurrentTurnFresh refuses different sourceType prefixes** — Assert-ReplCurrentTurnFresh compares sessionId sourceType prefix. Different prefix refuses CompleteTurn persist. Same prefix still rebinds. persist identity keeps planFile/todoId. Covered by FR-MCP-XAGENT-001 TEST-MCP-XAGENT-001.
**Covered by:** FR: FR-MCP-XAGENT-001; TEST: TEST-MCP-XAGENT-001
**Status:** pending
Scope: layer-1+

## TR-MOBILE-001

**TR-MOBILE-001** — Placeholder requirement backfilled for TODO link TR-MOBILE-001.
**Status:** pending
Scope: layer-1+

## TR-PLANNED-013A

`AddControllers().ConfigureApiBehaviorOptions` installs an `InvalidModelStateResponseFactory` that produces `application/problem+json` responses for body-binding failures on `/mcpserver/*` endpoints. The factory strips the action parameter name (`dto`, `body`, `turn`) from the `errors` keys, replacing them with `$` so callers see the canonical JSON root marker instead of a misleading wrapper field name. `SessionLogController.SubmitAsync` and `GetByIdAsync` use `ValidationProblem` for domain validation to keep the response shape uniform.
**Status:** pending
Scope: layer-1+

## TR-PLANNED-CORE-014

**Problem+JSON response factory for model binding failures** — AddControllers().ConfigureApiBehaviorOptions installs an InvalidModelStateResponseFactory that produces application/problem+json responses for body-binding failures on /mcpserver/* endpoints. The factory strips the action parameter name (dto, body, turn) from the errors keys, replacing them with $ so callers see the canonical JSON root marker instead of a misleading wrapper field name. SessionLogController.SubmitAsync and GetByIdAsync use ValidationProblem for domain validation to keep the response shape uniform.
**Status:** pending
Scope: layer-1+

## TR-SUPPORT-010E

**Stateless lifecycle controller + client + tool adapters** — SessionLogController exposes open/begin/complete/fail keyed by ids; SessionLogClient and MCP tools delegate; UpsertTurnAsync underpins all.
**Covered by:** FR: FR-SUPPORT-010E; TEST: TEST-SUPPORT-010E
**Status:** pending
Scope: layer-1+

## TR-SUPPORT-010F

**Merge-on-null mapping for partial submits** — MapDtoToEntity merges non-null scalars; UpsertTurns passes mergeOmittedFields:true; collections append-only.
**Covered by:** FR: FR-SUPPORT-010F, FR-SUPPORT-015; TEST: TEST-SUPPORT-010F, TEST-SUPPORT-015
**Status:** pending
Scope: layer-1+

## TR-SUPPORT-CORE-014

**Stateless lifecycle controller with client and tool adapters** — SessionLogController exposes open/begin/complete/fail endpoints keyed by ids; SessionLogClient and MCP tools delegate to these endpoints; UpsertTurnAsync underpins all lifecycle operations to provide a single point of truth for turn state transitions.
**Covered by:** FR: FR-SUPPORT-014; TEST: TEST-SUPPORT-014
**Status:** pending
Scope: layer-1+

## TR-SUPPORT-CORE-015

**Merge-on-null mapping for partial submits** — MapDtoToEntity merges non-null scalars when mapping DTOs to entities; UpsertTurns passes mergeOmittedFields:true to enable partial updates; collections use append-only semantics to preserve existing items when partial submits occur.
**Covered by:** FR: FR-SUPPORT-015; TEST: TEST-SUPPORT-015
**Status:** pending
Scope: layer-1+

## TR-SUPPORT-CORE-016

**Turn replace and section service** — ReplaceTurnAsync, ReplaceTurnSectionAsync, ClearTurnSectionAsync and DeleteTurnItemAsync in SessionLogService, with TurnSection and ParseSection, are the single implementation behind the REST, MCP and client surfaces of FR-SUPPORT-016.
**Covered by:** FR: FR-SUPPORT-016; TEST: TEST-SUPPORT-024
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ReplaceTurnAsync, ReplaceTurnSectionAsync, ClearTurnSectionAsync and DeleteTurnItemAsync in SessionLogService, with TurnSection and ParseSection, are the single implementation behind the REST, MCP and client surfaces (existing behavior; SessionLogServiceReplaceDeleteTests).
- [ ] AppendProcessingDialogAsync, UpsertTurnAsync, ReplaceTurnAsync and ReplaceTurnSectionAsync (with ClearTurnSectionAsync) read the turn and write its items inside one serializable transaction on a relational provider when the caller has none open, as SubmitAsync does, so writes to one turn committed at the same moment serialize or the losing one is refused with its provider's contention error (SQLite busy or locked, SQL Server deadlock 1205, PostgreSQL 40001 or 40P01), which McpErrorClassifier reports as retryable; UpsertTurnAsync's retry after a lost first insert of the same request id runs in a fresh transaction (PLAN-SESSIONTRIAGEFIX-001 A7 follow-up, triage-report-905c3ac1b6af4d57b9c24c2383642379, operator decision 2026-10-16 "Fix it in this plan").

## TR-SUPPORT-LOG-010

**Session-log ProblemDetails contract** — Session-log REST endpoints SHALL return application/problem+json for malformed JSON binding and domain validation failures. Error keys SHALL identify the JSON root or offending domain field rather than leaking action parameter names such as dto.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-TEST-001

**TR-TEST-001** — Placeholder requirement backfilled for TODO link TR-TEST-001.
**Covered by:** FR: FR-TEST-002
**Status:** pending
Scope: layer-1+

## TR-TIMER-LIFECYCLE-001

**Serialize timer lifecycle admission separately from callback invocation** — Protect shutdown admission and token replacement with a short-lived state lock. Cancel outside locks before draining the callback invocation gate. Reject queued Restart if shutdown admission changed while it waited. Do not dispose cancellation sources still visible to callbacks. Preserve reentrant Stop/Dispose/Restart behavior.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Both Manager timer implementations enforce the same shutdown admission and fresh-token rules without invoking user callbacks under the state lock.
- [ ] Restart cannot replace the token after a shutdown captured it; multiple overlapping shutdowns cannot reopen admission early.
- [ ] Cancellation registration and callback reentrancy cannot deadlock the lifecycle state lock.

## TR-TRIAGE-CLIENT-001

**Typed triage dashboard client endpoints** — SharpNinja.McpServer.Client exposes typed triage dashboard and run-history methods backed by REST endpoints for queue contents, groupings, AI triage runs, results, and current status.
**Covered by:** FR: FR-TRIAGE-001; TEST: TEST-TRIAGE-001
**Status:** pending
Scope: layer-1+

## TR-TRIAGE-CLIENT-002

**Typed triage TODO client endpoint** — REST, service, and SharpNinja.McpServer.Client typed triage APIs expose a triage-created TODO index with TODO IDs, created-at datetimes, workspace filters, group IDs, run IDs, and current triage status context.
**Covered by:** FR: FR-TRIAGE-002; TEST: TEST-TRIAGE-002
**Status:** pending
Scope: layer-1+

## TR-WEB-API-001

**New REST only when existing APIs cannot meet AC** — Prefer Todo, AgentPool, SessionLog, Events, Workspace, Templates, and Memory APIs. Any new endpoint MUST be documented with FR/TR ids and covered by TEST-WEB tests.
**Covered by:** FR: FR-WEB-013, FR-WEB-014; TEST: TEST-WEB-013, TEST-WEB-014
**Status:** pending
Scope: layer-1+

## TR-WEB-BUDGET-001

**Budget enforcement at enqueue** — Hourly, daily, and monthly USD caps SHALL be enforced server-side when dispatching. mcp-web only edits the caps. Client-side only checks are not sufficient.
**Covered by:** FR: FR-WEB-020; TEST: TEST-WEB-020
**Status:** pending
Scope: layer-1+

## TR-WEB-CTX-001

**Hierarchical context via templates and memories** — Workspace/agent/TODO context SHALL compose from existing template and memory APIs where they fit. New fields are allowed only when those APIs cannot represent persona text.
**Covered by:** FR: FR-WEB-007, FR-WEB-008, FR-WEB-019; TEST: TEST-WEB-007, TEST-WEB-008, TEST-WEB-019
**Status:** pending
Scope: layer-1+

## TR-WEB-GIT-001

**Commit linkage from session-log commit actions** — TODO-to-commit mapping SHALL read session-log actions with type commit. mcp-web SHALL NOT parse git logs as the primary source.
**Covered by:** FR: FR-WEB-010, FR-WEB-015; TEST: TEST-WEB-010, TEST-WEB-015
**Status:** pending
Scope: layer-1+

## TR-WEB-OBS-001

**Burn and progress from session logs** — Token burn and daily progress SHALL be aggregated from session-log tokenCount, commit actions, and agent-pool working timestamps. Do not scrape git independently unless session-log data is missing, and then record the fallback in the UI.
**Covered by:** FR: FR-WEB-009, FR-WEB-010; TEST: TEST-WEB-009, TEST-WEB-010
**Status:** pending
Scope: layer-1+

## TR-WEB-ORCH-001

**Fleet uses AgentPoolClient** — Start, stop, recycle, status, start-all, stop-all, and queue dispatch SHALL call /mcpserver/agent-pool via AgentPoolClient. mcp-web SHALL NOT spawn agent processes itself.
**Covered by:** FR: FR-WEB-001, FR-WEB-002, FR-WEB-003, FR-WEB-005, FR-WEB-007, FR-WEB-012, FR-WEB-014, FR-WEB-019, FR-WEB-020; TEST: TEST-WEB-001, TEST-WEB-002, TEST-WEB-003, TEST-WEB-005, TEST-WEB-007, TEST-WEB-012, TEST-WEB-014, TEST-WEB-019, TEST-WEB-020
**Status:** pending
Scope: layer-1+

## TR-WEB-PRIV-001

**No prompt or file telemetry from mcp-web** — The mcp-web client bundle SHALL NOT send prompt text, file bodies, or diffs to hosts other than the configured MCP Server and the operator-configured model providers used by the pool.
**Covered by:** FR: FR-WEB-016; TEST: TEST-WEB-016
**Status:** pending
Scope: layer-1+

## TR-WEB-SCHED-001

**Recurring TODOs have a durable scheduler** — If MCP Server has no recurring TODO scheduler, add a server-side scheduler that creates/reopens TODOs. mcp-web only authors the schedule fields. Browser timers SHALL NOT be the source of truth.
**Covered by:** FR: FR-WEB-006; TEST: TEST-WEB-006
**Status:** pending
Scope: layer-1+

## TR-WEB-SEC-001

**Reuse pairing and OIDC** — mcp-web SHALL authenticate with existing pairing (FR-MCP-014) and OIDC (FR-MCP-026). Do not add Prompter Hawk magic-link email auth.
**Covered by:** FR: FR-WEB-016; TEST: TEST-WEB-016
**Status:** pending
Scope: layer-1+

## TR-WEB-SEC-002

**Allow/deny stored as workspace policy** — Tool and path rules SHALL persist through workspace policy APIs (FR-MCP-033 / WorkspacePolicyService), fail closed, and be editable from mcp-web.
**Covered by:** FR: FR-WEB-013; TEST: TEST-WEB-013
**Status:** pending
Scope: layer-1+

## TR-WEB-SESS-001

**Live peek uses session log and events** — Live peek and fire-and-forget refresh SHALL use SessionLogClient and EventStreamClient (/mcpserver/events). Terminal scraping is forbidden.
**Covered by:** FR: FR-WEB-005, FR-WEB-008, FR-WEB-009, FR-WEB-011, FR-WEB-015; TEST: TEST-WEB-005, TEST-WEB-008, TEST-WEB-009, TEST-WEB-011, TEST-WEB-015
**Status:** pending
Scope: layer-1+

## TR-WEB-TODO-001

**Task bank uses TodoClient only** — Task bank CRUD, lanes, assignment, and dependencies SHALL use MCP TODO APIs. Direct reads or writes of TODO.yaml / docs/todo.yaml are forbidden.
**Covered by:** FR: FR-WEB-002, FR-WEB-004, FR-WEB-005, FR-WEB-006, FR-WEB-008, FR-WEB-011, FR-WEB-012, FR-WEB-018; TEST: TEST-WEB-002, TEST-WEB-004, TEST-WEB-005, TEST-WEB-006, TEST-WEB-008, TEST-WEB-011, TEST-WEB-012, TEST-WEB-018
**Status:** pending
Scope: layer-1+

## TR-WEB-UI-001

**Host orchestration UI in McpServer.Web** — The orchestration dashboard SHALL ship in src/McpServer.Web (mcp-web), extending FR-MCP-031. If the project is absent from the solution, restore or create it. Do not implement this UI inside Director.
**Covered by:** FR: FR-WEB-001, FR-WEB-002; TEST: TEST-WEB-001, TEST-WEB-002
**Status:** pending
Scope: layer-1+

## TR-WEB-UI-002

**Primer layout with desktop and mobile breakpoints** — mcp-web SHALL use the existing Primer CSS / Octicons visual language and MUST satisfy FR-WEB-017 breakpoints in automated UI tests.
**Covered by:** FR: FR-WEB-001, FR-WEB-003, FR-WEB-004, FR-WEB-017; TEST: TEST-WEB-001, TEST-WEB-003, TEST-WEB-004, TEST-WEB-017
**Status:** pending
Scope: layer-1+

