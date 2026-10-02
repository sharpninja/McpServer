# MemoryVersions soft-delete columns + API-key redaction

Class: Legion stop-gate follow-up. Cursor Cloud agent on `cursor/memory-versions-softdelete-1f0a` from `develop`.

Agent: CursorGrok
Date: 2026-09-19T19:00:00Z
Marker: absent in this environment (`MCP_UNTRUSTED`); no live key was read or written.

## Bug A — MemoryVersions schema mismatch

Verified against the model, not the operator hypothesis alone:

- `McpDbContext.ApplySoftDeleteMetadata` maps shadow properties `IsDeleted`, `DeletedAtUtc`, `DeletedBy`, and `DeleteReason` onto every durable `*Entity` except `DataAuditLogEntity`.
- `MemoryVersionEntity` has no CLR soft-delete properties. EF still inserts the four shadow columns.
- `20260918223000_AddMemoryVersionAndEdgeStorage` created `MemoryVersions` and `MemoryEdges` without those columns.
- `20260919060000_AddMemoryIndexStorage` created `MemoryIndexes` without those columns.
- `Memories` already has the four columns from `AddMemoryStorage`.
- `MemoryService.AddAsync` / `memory_remember` calls `AppendVersionAsync`, which `MemoryVersions.Add`s and `SaveChanges`. That is the HTTP 500 on PAYTON-LEGION2.

Develop also lacked `[DbContext(typeof(McpDbContext))]` and `[Migration("...")]` on the two September memory migrations (commit `22e4865a` is not on `origin/develop`). Those attributes are added in place. Designers were not regenerated.

New forward-only migration `20260919193000_AddMemoryVersionSoftDeleteColumns` on Sqlite, PostgreSQL, and SQL Server adds the four columns to:

- `MemoryVersions` (required to unblock remember)
- `MemoryEdges` and `MemoryIndexes` (same model convention; recall reads `MemoryIndexes`)

Historical September migrations were not rewritten.

### SQL Server (Legion Nuke UpdateService)

After pull, `Nuke UpdateService` applies the new migration. Equivalent explicit SQL if operators need a receipt of the DDL:

```sql
ALTER TABLE [MemoryVersions] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_MemoryVersions_IsDeleted] DEFAULT CAST(0 AS bit);
ALTER TABLE [MemoryVersions] ADD [DeletedAtUtc] datetimeoffset NULL;
ALTER TABLE [MemoryVersions] ADD [DeletedBy] nvarchar(256) NULL;
ALTER TABLE [MemoryVersions] ADD [DeleteReason] nvarchar(1024) NULL;

ALTER TABLE [MemoryEdges] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_MemoryEdges_IsDeleted] DEFAULT CAST(0 AS bit);
ALTER TABLE [MemoryEdges] ADD [DeletedAtUtc] datetimeoffset NULL;
ALTER TABLE [MemoryEdges] ADD [DeletedBy] nvarchar(256) NULL;
ALTER TABLE [MemoryEdges] ADD [DeleteReason] nvarchar(1024) NULL;

ALTER TABLE [MemoryIndexes] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_MemoryIndexes_IsDeleted] DEFAULT CAST(0 AS bit);
ALTER TABLE [MemoryIndexes] ADD [DeletedAtUtc] datetimeoffset NULL;
ALTER TABLE [MemoryIndexes] ADD [DeletedBy] nvarchar(256) NULL;
ALTER TABLE [MemoryIndexes] ADD [DeleteReason] nvarchar(1024) NULL;
```

Prefer the EF migration over hand-running this SQL so `__EFMigrationsHistory` stays aligned.

## Bug B — API key leakage

`InteractionLoggingMiddleware.FormatHeaders` wrote every request header, including `X-Api-Key`, into service logs (see `docs/receipts/_hv-agenthelp-logscan-20260817T230630Z.txt`). Diagnostic/log dumps of the same header or `api_key=` / `"apiKey"` JSON now go through `ApiKeyRedaction` (`McpServer.Common.AgentCli`). Values become `[REDACTED]`. No live key is printed or committed.

Operator rotates the live Legion key separately.

## Proof

- `MemoryMigrationDiscoveryTests` requires the three migration ids on all three assemblies.
- `MemoryMigratedSchemaOperationsTests` applies real Sqlite migrations (not `EnsureCreated`): remember fails before the new migration, then remember / list / recall succeed after it.
- `ApiKeyRedactionTests` and `InteractionLoggingMiddlewareTests.InvokeAsync_RedactsApiKeyHeaders_FromLogMessages` use synthetic fixture keys only.
