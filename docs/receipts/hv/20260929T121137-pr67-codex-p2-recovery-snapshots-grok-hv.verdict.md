# Grok HV — PR #67 Codex P2 RequirementsRecoveryRuns EF snapshots

**Reviewer:** Grok Bot (HV path only; no Codex/Astra for HV)
**Subject:** Update SQLite / SQL Server / PostgreSQL `McpDbContextModelSnapshot.cs` to include `RequirementsRecoveryRunEntity`
**Base tip:** `dd9a1ed7`
**Timestamp (CT):** 2026-09-29 12:11:37 CT
**add-profile:** profile dir absent on this machine (`~/.claude/profile` empty/missing); proceeded with hostile-validator skill directive + prior PR67 HV receipt pattern.

## Scores

| Dimension | Score |
| --- | ---: |
| Accuracy | 99 |
| Completeness | 99 |
| Consistency | 98 |
| **Overall AGREE** | **99** |

**Verdict:** AGREE

## Finding coverage

1. **P2 Update migration snapshots for recovery table** — PASS. Hand-synced `RequirementsRecoveryRunEntity` into all three provider snapshots (entity definition + Workspace FK relationship). Table `RequirementsRecoveryRuns`, composite PK `(WorkspaceId, IdempotencyKey)`, `IX_RequirementsRecoveryRuns_WorkspaceId`, soft-delete shadows, `Status` default `applied`, and provider column types match the existing `20260929020000_AddRequirementsRecoveryRuns` migrations. No second CreateTable migration.

## Residuals (non-blocking)

- `DeletedAtUtc` CLR type in the new snapshot blocks is `DateTimeOffset?` (matches migration + `ApplySoftDeleteMetadata`); older entities in the same snapshots still use `DateTime?` — pre-existing drift, out of scope for this P2.
- Legacy `src/McpServer.Storage/Migrations/McpDbContextModelSnapshot.cs` not updated; Codex ask was the three provider assemblies, which are the ones paired with the recovery migrations.

## Evidence

- Builds: SqliteMigrations / SqlServerMigrations / PostgreSqlMigrations — green (0 warn / 0 err)
- Diff scoped to three `McpDbContextModelSnapshot.cs` files only (+186 lines)
- Column parity vs migrations: WorkspaceId, IdempotencyKey, PayloadHash, Status, ResultJson, CreatedAtUtc, DeleteReason, DeletedAtUtc, DeletedBy, IsDeleted — all present; PK/index/ToTable/FK Restrict verified
- PR left unmerged by design
