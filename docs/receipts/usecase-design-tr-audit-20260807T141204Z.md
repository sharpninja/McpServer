# Audit: McpServer-UseCase-Extension-Design-v1.0.md vs MCP requirements

- Design: docs/McpServer-UseCase-Extension-Design-v1.0.md (v1.0, 2026-07-10)
- Requirements source: mcpserver__requirements_list type=all for F:\GitHub\McpServer
- RequirementEntity.Id type verified in source: string (RequirementEntity.cs)
- Verdict: NOT implementation-ready against current FR/TR without revision

## Hard conflicts
1. FrId BIGINT + FunctionalRequirement nav vs string RequirementEntity.Id Kind=fr (TR-MCP-REQ-*, FR-MCP-040 family, RequirementEntity)
2. SQL Server IDENTITY-only schema vs multi-provider SQLite/Postgres/SQL Server (TR-MCP-CFG-007)
3. No soft-delete metadata vs TR-MCP-DB-003
4. No Workspace FK to Workspaces vs TR-MCP-DB-002; WorkspaceId length 50 too short for path discriminator (FR-MCP-043/044, TR-MCP-MT-*)
5. UseCaseFrLinks missing WorkspaceId vs TR-MCP-MT-003 query filters
6. Section "API & CQRS" names CQRS but defines no ICommand/IQuery/handlers/Dispatcher usage (FR-MCP-029, TR-MCP-CQRS-001..005)
7. Physical cascade delete implied / no audit ledger vs TR-MCP-DB-003, TR-MCP-DB-004
8. ValidateTraceability extension for UseCaseFrLinks mismatches docs-only Nuke validator model (build TraceabilityValidator; TR-MCP-REQ-002/003 export matrix)

## Gaps
- Dual transport STDIO MCP tools (FR-MCP-007, TR-MCP-REQ-003 pattern)
- Typed client + JsonContext (TR-MCP-CLIENT-001)
- DI single-owner/pull rules (TR-MCP-ARCH-002)
- Auth/workspace middleware assumptions not stated (FR-MCP-013, TR-MCP-AUTH-010, TR-MCP-MT-001/002)
- Three-provider migrations (TR-MCP-MEMORY-002 pattern)
- FR/TR/TEST/TEST acceptance for the feature itself (COMP-003, REQ create flow)
- Federation, transaction gating, audit, soft delete
- GraphRAG phase only a one-liner (TR-GRAPHRAG-ADHOC-001)

## Compatible intents
- REST under /mcpserver/usecases (TR-MCP-API-001 style)
- No Blazor / pure API (TR-MCP-WEB-001 UI external)
- Workspace-scoped column present (partial FR-MCP-044)
- Bidirectional FR linkage intent (aligns with TR-MCP-DB-005 spirit)
- Zero breaking changes to FR surface (stated goal)