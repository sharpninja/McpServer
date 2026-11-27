# TR-MCP-AGENT

[Back to Technical Requirements](Technical-Requirements)

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

**Status:** 🔴 Planned

**Covered by:** `AgentPoolOptions` *(planned)*, `AgentPoolDefinitionOptions` *(planned)*, `Program.cs` *(planned extension)*
Scope: layer-1+

## TR-MCP-AGENT-005

**Pooled Runtime and Queue Dispatcher** — All agent execution SHALL flow through a singleton pool runtime service that maintains lifecycle state per configured pooled agent and dispatches queued one-shot jobs to eligible idle agents.
Pool runtime SHALL support start/stop/recycle operations, busy/idle transitions, one-shot queue states (`queued`, `processing`, `completed`, `failed`, `canceled`), and concurrent interactive attachment to agents currently processing one-shot requests.

No alternate direct-launch path is permitted for pooled workloads; pooled agents launch through the voice interactive session mechanism.

**Status:** 🔴 Planned

**Covered by:** `IAgentPoolService` *(planned)*, `AgentPoolService` *(planned)*, `AgentPoolQueueService` *(planned)*
Scope: layer-1+

## TR-MCP-AGENT-006

**Hosted .NET 9 Microsoft Agent Framework Library** — The solution SHALL provide a dedicated .NET 9 class library for hosting an MCP-aware agent inside external .NET applications built on Microsoft Agent Framework. The library SHALL expose DI-friendly registration and configuration APIs for MCP Server connectivity, agent construction, and host lifecycle integration so host applications do not need to assemble low-level MCP session-log or TODO plumbing themselves.
**Status:** ✅ Complete

**Covered by:** `ServiceCollectionExtensions`, `McpAgentOptions`, `McpAgentOptionsValidator`, `IMcpHostedAgent`, `IMcpHostedAgentFactory`, `McpHostedAgent`, `McpHostedAgentFactory`, `McpHostedAgentRegistration`
Scope: layer-1+

## TR-MCP-AGENT-007

**Built-In MCP Session Log, TODO, Repository, Desktop-Launch, and PowerShell Workflow for Hosted Agents** — The hosted agent library SHALL implement built-in workflow operations for session bootstrap, turn creation/update, TODO retrieval/update, TODO plan/status/implementation flows, repository read/list/write operations, local desktop process launch using the existing MCP Server contracts, and persistent in-process PowerShell sessions hosted directly inside the current .NET agent process. The workflow SHALL preserve canonical ID conventions for session IDs, request IDs, and TODO IDs, SHALL keep repository access scoped to repo-relative paths, SHALL expose desktop launch through the authenticated workspace context only when the server-side desktop-launch feature gate, executable allowlist, and privileged desktop-launch token requirements are satisfied, SHALL keep PowerShell session state local to the hosted agent instance, SHALL expose the same local PowerShell session manager to host applications through `IMcpHostedAgent.PowerShellSessions`, and SHALL prefer reuse of existing client abstractions where server contracts already exist instead of duplicating transport logic.
**Status:** ✅ Complete

**Covered by:** `ISessionLogWorkflow`, `SessionLogWorkflow`, `SessionLogWorkflowContext`, `SessionLogTurnContext`, `ITodoWorkflow`, `TodoWorkflow`, `IMcpHostedAgent.PowerShellSessions`, `IHostedPowerShellSessionManager`, `McpHostedAgentToolAdapter`, `HostedPowerShellSessionManager`, `HostedPowerShellSessionHost`, `PowerShellSessionCreateResult`, `PowerShellSessionCommandResult`, `PowerShellSessionCloseResult`, `McpServerClient`, `RepoClient`, `DesktopClient`, `IMcpSessionIdentifierFactory`, `McpSessionIdentifierFactory`
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

**Status:** ✅ Complete

**Covered by:** `tools/powershell/McpSession.psm1`
Scope: layer-1+

## TR-MCP-AGENT-014

**PowerShell McpSession Trust Bootstrap Parity** — `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, and `tools/powershell/McpContext.psm1` SHALL use the same marker-signature verification, `/health` nonce echo verification, and `MCP_UNTRUSTED` fallback semantics before any follow-on MCP calls are allowed. The trust flow SHALL be explicit enough that session bootstrap, TODO bootstrap, and context bootstrap behave identically when trust succeeds or fails, and the failure path SHALL stop additional endpoint probing.
**Status:** ✅ Complete

**Covered by:** `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, `tools/powershell/McpContext.psm1`, `docs/context/module-bootstrap.md`, `docs/USER-GUIDE.md`
Scope: layer-1+

## TR-MCP-AGENT-015

**ACID hosted-agent profile and sealed run contract** — The McpServer.McpAgent package SHALL define an ACID tightly coupled profile that applies strict McpAgentOptions defaults, filters the model-visible tool surface to approved read/audit tools, seals ChatClientAgent run options with serialized function invocation, and documents the profile as fail-closed for unproven mutation paths.
**Covered by:** FR: FR-MCP-136; TEST: TEST-MCP-186
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] McpAgentOptions exposes a UseAcidTightlyCoupledProfile helper that sets strict profile defaults without changing default registration behavior.
- [x] ACID run options preserve AllowMultipleToolCalls=false and FunctionInvokingChatClient.AllowConcurrentInvocation=false.
- [x] ACID tool exposure is generated from an allowlist and excludes unsafe generic, shell, desktop, and mutation tools by default.
- [x] All new public APIs have XMLDocs and are covered by focused tests.

## TR-MCP-AGENT-016

**Hosted-agent Quad Brain coding adapter** — RETIRED 2026-07-20 by FR-MCP-142 and TR-MCP-QB-001. This requirement specified the mcp_quadbrain_coding_execute hosted-agent tool routing to POST mcpserver/brain-slots/orchestrate through McpQuadBrainCodingAgentRouter. Both the tool and the router are removed: the tool was registered into the shared McpServer.McpAgent catalog for every host, which violates the rule that no QuadBrain capability is exposed outside QBAgent, and it was already non-functional for QBAgent because QuadBrainInternalToolExecutor never had a case for it, so the interceptor classified it internal and returned Fail. Removed artifacts: McpHostedAgentToolAdapter tool registration and ExecuteQuadBrainCodingTaskAsync, IMcpHostedAgent.ExecuteQuadBrainCodingTaskAsync, McpHostedAgent.ExecuteQuadBrainCodingTaskAsync, QBAgentRuntime.ExecuteCodingTaskAsync and its codingTaskExecutor parameter, QBAgentDefinition allow-list entry, McpQuadBrainCodingAgentRouter.cs, and the McpQuadBrainCodingAgentRequest DTO. Server-side QuadBrain orchestration is unchanged and remains reachable only through POST /v1/chat/completions. Note that this is a public API break in the shared McpServer.McpAgent assembly, accepted deliberately as the intended consequence of the ruling.
**Covered by:** FR: FR-MCP-137; TEST: TEST-MCP-187
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Coding-agent DTOs are public, XML-documented, and use System.Text.Json property names compatible with Microsoft Agent Framework function invocation.
- [x] The adapter preserves caller metadata and appends coding-agent fields including taskKind, executionProfile, and sourceType.
- [x] The adapter fails through the typed QuadBrainOrchestrationResponse status/reason contract returned by MCP Server and does not synthesize implicit fallback model output.
- [x] Existing non-ACID hosted-agent registration remains backward compatible aside from the additional Quad Brain coding tool.
- [x] All new public APIs have XMLDocs and are covered by focused tests.

