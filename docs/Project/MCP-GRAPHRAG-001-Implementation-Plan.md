# MCP-GRAPHRAG-001 Implementation Plan

## Objective

Implement GraphRAG as an optional, workspace-isolated retrieval enhancement for MCP Server, including:

- Configuration and bootstrap for per-workspace GraphRAG state.
- Server-side GraphRAG service abstraction with graceful fallback.
- New REST endpoints for GraphRAG query/index/status.
- MCP STDIO tools for GraphRAG query/index/status.
- Integration with existing context search pipeline.
- Client SDK support for GraphRAG endpoints.
- Tests for endpoint behavior and client behavior.

This plan is intentionally exhaustive to support phased delivery, operational safety, and future replacement of the initial backend adapter with a full GraphRAG engine.

## Scope

### In Scope (this implementation)

1. Add GraphRAG options and defaults.
2. Add GraphRAG models and service interfaces.
3. Implement a `GraphRagService` with:
   - Workspace-local directory initialization.
   - Status persistence.
   - Index trigger path.
   - Query path with fallback to existing context search.
4. Expose `/mcpserver/graphrag/status`, `/mcpserver/graphrag/index`, `/mcpserver/graphrag/query`.
5. Add MCP tools:
   - `graphrag_status`
   - `graphrag_index`
   - `graphrag_query`
6. Optionally enhance `/mcpserver/context/search` via GraphRAG when enabled.
7. Add client SDK methods for GraphRAG.
8. Add tests for controller and client behavior.

### Out of Scope (deferred)

1. A hard dependency on external GraphRAG runtime binaries.
2. Production LLM key orchestration and managed secret stores.
3. Advanced graph analytics (community detection tuning, drift/global mode quality benchmarks).
4. Distributed indexing workers.

## Non-Functional Requirements

1. **Backwards Compatibility**
   - Existing `context/search`, `context/pack`, and `context/sources` remain available.
   - If GraphRAG is disabled or not ready, no existing search path regresses.
2. **Workspace Isolation**
   - GraphRAG files are not shared across workspaces.
3. **Safety**
   - No direct edits of `docs/Project/TODO.yaml`.
4. **Observability**
   - Log mode selection and fallback reasons.
5. **Performance**
   - Avoid expensive operations on request path unless explicitly requested.

## Architecture

### New Components

1. `GraphRagOptions` (configuration)
2. `IGraphRagService` (contract)
3. `GraphRagService` (implementation)
4. `GraphRagController` (REST surface)
5. GraphRAG request/response models
6. MCP tool wrappers in `FwhMcpTools`
7. Client SDK methods in `ContextClient`

### Data Flow

1. Client calls GraphRAG endpoint or tool.
2. Workspace resolution middleware sets `WorkspaceContext`.
3. GraphRAG service resolves workspace-local GraphRAG root and initializes if needed.
4. Query/index/status action executes:
   - Primary backend adapter (initial internal adapter).
   - Fallback to existing context search when required.
5. Response includes explicit metadata: backend mode, fallback usage, and readiness.

### Workspace Isolation Strategy

1. Resolve GraphRAG root with this precedence:
   - Absolute `GraphRagOptions.RootPath` if provided.
   - Relative `GraphRagOptions.RootPath` under resolved workspace path.
   - Default under workspace: `mcp-data/graphrag`.
2. Ensure standard subfolders:
   - `input`
   - `output`
   - `cache`
3. Persist status to workspace-local `graphrag-status.json`.

## Detailed Implementation Tasks

### Phase 1 - Config and Contracts

1. Add `GraphRagOptions` under `Options` with fields:
   - `Enabled` (bool, default false)
   - `EnhanceContextSearch` (bool, default true)
   - `RootPath` (string, default `mcp-data/graphrag`)
   - `DefaultQueryMode` (string, default `local`)
   - `IndexTimeoutSeconds` (int, default 600)
   - `QueryTimeoutSeconds` (int, default 120)
   - `BackendCommand` (optional string)
   - `BackendArgs` (optional string)
2. Register options in `Program.cs` and `McpStdioHost.cs`.
3. Add post-configuration path normalization if needed.

### Phase 2 - Models and Service

1. Add `GraphRagQueryRequest`:
   - `query`, `mode`, `maxChunks`, `includeContextChunks`.
2. Add `GraphRagIndexRequest`:
   - `force`.
3. Add `GraphRagStatusResponse`:
   - `enabled`, `workspacePath`, `graphRoot`, `isInitialized`, `isIndexed`, `lastIndexedAtUtc`, `lastError`, `backend`.
4. Add `GraphRagQueryResponse`:
   - `query`, `mode`, `answer`, `citations`, `chunks`, `sourceKeys`, `fallbackUsed`, `backend`.
5. Implement `IGraphRagService` methods:
   - `GetStatusAsync`
   - `InitializeAsync`
   - `IndexAsync`
   - `QueryAsync`
6. Implement `GraphRagService`:
   - Read `WorkspaceContext`.
   - Ensure root/subfolders/status file.
   - Maintain status JSON atomically.
   - Query fallback to existing context search.
   - Structured logging for degrade reasons.

### Phase 3 - HTTP Endpoints

1. Add `GraphRagController` with route `/mcpserver/graphrag`.
2. Implement:
   - `GET /status`
   - `POST /index`
   - `POST /query`
3. Return `400` for invalid requests and `200` for graceful fallback responses.

### Phase 4 - Context Search Integration

1. Inject `IGraphRagService` + `GraphRagOptions` into `ContextController`.
2. On `POST /mcpserver/context/search`:
   - If GraphRAG enhancement enabled, attempt GraphRAG query first.
   - Map GraphRAG chunks into existing response shape.
   - Include metadata object describing GraphRAG/fallback mode.
3. If GraphRAG unavailable/not indexed/error:
   - Use existing `_searchService` result path unchanged.

### Phase 5 - MCP Tooling (STDIO)

1. Inject `IGraphRagService` into `FwhMcpTools`.
2. Add tools:
   - `graphrag_status(workspacePath)`
   - `graphrag_index(workspacePath, force=false)`
   - `graphrag_query(query, workspacePath, mode=null, maxChunks=20, includeContextChunks=true)`
3. Ensure all tools call `ApplyWorkspaceOverride` before service usage.

### Phase 6 - Client SDK

1. Extend context models with GraphRAG request/response types.
2. Add methods to `ContextClient`:
   - `GraphRagStatusAsync`
   - `GraphRagIndexAsync`
   - `GraphRagQueryAsync`
3. Add unit tests verifying route/method/body shape.

### Phase 7 - Tests

1. Add controller tests:
   - GraphRAG status returns shape.
   - GraphRAG query returns fallback-safe payload.
   - GraphRAG index updates status.
2. Update/extend context controller tests for metadata when GraphRAG enhancement is active.
3. Add service unit tests if service-specific test harness exists.
4. Add client tests for new methods.

### Phase 8 - Validation and Hardening

1. Run build:
   - `dotnet build`
2. Run targeted tests:
   - `dotnet test` for `McpServer.Support.Mcp.Tests`
   - `dotnet test` for `McpServer.Client.Tests`
3. Resolve lint/diagnostic issues in touched files.

## Rollout and Compatibility

1. Default deployment: GraphRAG disabled.
2. Opt-in enablement with configuration toggle.
3. Fallback behavior guarantees existing search remains functional.
4. Incrementally replace internal adapter with real external GraphRAG runtime in future.

## Acceptance Criteria

1. New GraphRAG endpoints exist and return valid payloads.
2. MCP tools for GraphRAG are callable and workspace-aware.
3. Context search can be GraphRAG-enhanced without breaking existing clients.
4. When GraphRAG cannot run, fallback search still returns results.
5. All touched projects build and tests pass.

## Risk Register

1. **External runtime not available**
   Mitigation: internal fallback adapter and non-fatal status reporting.
2. **Per-workspace path confusion**
   Mitigation: central path resolver and status payload containing effective paths.
3. **Context search regressions**
   Mitigation: preserve existing response shape and fallback path.
4. **Tool/API drift**
   Mitigation: add both controller and client tests for endpoint contracts.

## Done Definition

1. Plan document committed in repo.
2. TODO item updated through API with detailed implementation checklist.
3. Code for all phases above merged into working tree.
4. Build/test/lint checks completed for touched scope.
