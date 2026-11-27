# TR-MCP-MEMORY

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-MEMORY-001

**EF memory storage model** — Add `MemoryEntity` and `DbSet<MemoryEntity>` to the shared EF model. `Id` is unique across the memory store. `Scope` is required and constrained to `Global` or `Workspace`. `WorkspaceId` is null for Global memories and required for Workspace memories. Soft-delete metadata hides removed memories by default. Indexes exist for `Scope`, `WorkspaceId`, `Category`, and `UpdatedAtUtc`.
**Covered by:** FR: FR-MCP-MEMORY-001; TEST: TEST-MCP-MEMORY-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] The shared EF model exposes `MemoryEntity` and `DbSet<MemoryEntity>`.
- [x] Memory IDs are unique across the memory store.
- [x] Scope is required and constrained to `Global` or `Workspace`.
- [x] Global memories have null `WorkspaceId`; Workspace memories require `WorkspaceId`.
- [x] Soft-delete metadata hides removed memories by default.
- [x] Indexes exist for `Scope`, `WorkspaceId`, `Category`, and `UpdatedAtUtc`.

## TR-MCP-MEMORY-002

**Provider memory migrations** — Add provider migrations for SQLite, SQL Server, and PostgreSQL. Each migration creates the memory table, unique ID constraint, scope and workspace indexes, category/update-time indexes, soft-delete metadata, and provider-appropriate constraints or service-level validation for Global rows with null `WorkspaceId` and Workspace rows with required `WorkspaceId`.
**Covered by:** FR: FR-MCP-MEMORY-002; TEST: TEST-MCP-MEMORY-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] SQLite, SQL Server, and PostgreSQL migration projects include memory migrations.
- [x] Provider snapshots include `MemoryEntity`.
- [x] Migrations create the memory table, unique ID constraint, scope/workspace indexes, category/update-time indexes, and soft-delete metadata.
- [x] Provider projects compile.
- [x] Scope and `WorkspaceId` consistency is enforced by provider-appropriate constraints or service-level validation.

## TR-MCP-MEMORY-003

**Memory service layer** — Add XML-documented `IMemoryService` and `MemoryService` contracts for add, list, update, and remove. The service validates IDs, categories, scopes, text, duplicate active IDs, and scope transitions; generates globally unique `MEMORY-{CATEGORY}-{NNN}` IDs per category; preserves raw text; increments `Version` on update; and soft-deletes on remove.
**Covered by:** FR: FR-MCP-MEMORY-003; TEST: TEST-MCP-MEMORY-003
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] `IMemoryService` and `MemoryService` expose add, list, update, and remove operations with XMLDocs.
- [x] The service validates IDs, categories, scopes, text, duplicate active IDs, and scope transitions.
- [x] ID generation is globally unique per category across Global and Workspace scopes.
- [x] List returns Global memories first and Workspace memories second, sorted by ID within each group.
- [x] Update increments `Version` and can change scope under validation.
- [x] Remove soft-deletes without physically deleting the row.

## TR-MCP-MEMORY-004

**Memory REST and typed client contract** — Add `MemoryController`, `MemoryClient`, and client models under `/mcpserver/memory`. Create, list, update, and remove models include scope where applicable. `McpServerClient.Memory` exists and participates in `_allClients` propagation for workspace path, API key, bearer token, and port.
**Covered by:** FR: FR-MCP-MEMORY-004; TEST: TEST-MCP-MEMORY-004, TEST-MCP-MEMORY-006
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] REST endpoints are available under `/mcpserver/memory`.
- [x] `MemoryController`, `MemoryClient`, and client models include scope where applicable.
- [x] `McpServerClient.Memory` exists.
- [x] Workspace path, API key, bearer token, and port propagate through `_allClients`.
- [x] Client models serialize and deserialize Global and Workspace scope values.

## TR-MCP-MEMORY-005

**MCP stdio and REPL memory tools** — Add `memory_add`, `memory_list`, `memory_update`, and `memory_remove` to MCP stdio tools, and route `workflow.memory.add`, `workflow.memory.list`, `workflow.memory.update`, and `workflow.memory.remove` through typed REPL workflow code. Stdio tools require `workspacePath`, call `ApplyWorkspaceOverride`, and return compact JSON including scope for add, list, and update.
**Covered by:** FR: FR-MCP-MEMORY-005; TEST: TEST-MCP-MEMORY-005
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] MCP stdio exposes `memory_add`, `memory_list`, `memory_update`, and `memory_remove`.
- [x] Stdio tools require `workspacePath` and call `ApplyWorkspaceOverride`.
- [x] `memory_add` and `memory_update` accept scope values.
- [x] `memory_list` returns scope and Global-first ordering.
- [x] The REPL dispatcher routes `workflow.memory.*` methods through typed workflow code.
- [x] The TypeScript REPL client exposes memory helpers.

## TR-MCP-MEMORY-006

**Memory schema and contract coverage** — Update canonical REPL YAML schema, plugin schema copies, and `docs/stdio-tool-contract.json` for all memory surfaces. Schemas validate required fields, `MEMORY-{CATEGORY}-{NNN}` IDs, `Global`/`Workspace`/`Effective` scope values where applicable, and invalid method/payload cases.
**Covered by:** FR: FR-MCP-MEMORY-006; TEST: TEST-MCP-MEMORY-008
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] `docs/context/repl-yaml-message.schema.json` includes `workflow.memory.*` methods.
- [x] Plugin schema copies include the memory surfaces.
- [x] `docs/stdio-tool-contract.json` includes all memory tools.
- [x] Schemas validate required fields, memory ID format, and allowed scope values.
- [x] Valid schema examples pass and invalid examples fail.

## TR-MCP-MEMORY-007

**Scope-aware effective memory querying** — Add query/service helpers that resolve active memories for a workspace. Effective queries include active Global memories plus active memories for the current workspace, exclude deleted and other-workspace rows, and apply Global-first then Workspace ordering by ID. Controller, client, MCP stdio, REPL, YAML examples, marker injection, and plugin injection all use this ordering contract.
**Covered by:** FR: FR-MCP-MEMORY-006, FR-MCP-MEMORY-007; TEST: TEST-MCP-MEMORY-008, TEST-MCP-MEMORY-007, TEST-MCP-MEMORY-009
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Effective queries include active Global memories plus active current-workspace memories.
- [x] Effective queries exclude deleted rows and rows from other workspaces.
- [x] Effective queries apply Global-first then Workspace ordering by ID.
- [x] Controller, client, MCP stdio, REPL, YAML examples, marker injection, and plugin injection use the same ordering contract.

## TR-MCP-MEMORY-008

**Agent plugin memory integration** — Official McpServer plugins consume the shared memory contract and expose memory tools through their supported tool surfaces. Plugins with host request-boundary injection hooks render the exact `REQUIRED MEMORIES` block on supported user prompts. Plugins without such hooks document the limitation and expose explicit memory-list fallback behavior.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Official plugin lanes consume the shared memory API, REPL, and stdio contract. (evidence: Plugin memory tools route through workflow.memory.*.)
- [x] Plugins expose memory tools through supported tool surfaces. (evidence: Shell and TypeScript memory tool tests.)
- [x] Plugins with host request-boundary injection hooks render the exact REQUIRED MEMORIES block on supported user prompts. (evidence: Plugin validation coverage.)
- [x] Plugins without usable request-boundary injection hooks document the limitation without claiming automatic injection. (evidence: Plugin host limitation docs.)
- [x] Plugin memory mutations append session-log actions and clear local failsafe entries after server acknowledgement. (evidence: Updated shell wrappers, TypeScript handlers, Bats tests, and Jest tests.)
- [x] Plugins without automatic injection expose explicit memory-list fallback behavior. (evidence: memory_list plugin tests and fallback behavior.)

