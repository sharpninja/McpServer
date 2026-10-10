# Grok HV — PR #67 Codex P2 soft-delete / priority-status length / unique-race retry

**Reviewer:** Grok Bot (HV path only; no Codex/Astra for HV)
**Subject:** revive soft-deleted requirements; enforce priority≤32 / status≤64; retry requirement-key unique races
**Base tip:** `33e8ee26`
**Timestamp (CT):** 2026-09-29 11:39:09 CT
**add-profile:** profile dir absent on this machine (`~/.claude/profile` empty/missing); proceeded with hostile-validator skill directive + prior PR67 HV receipt pattern.

## Scores

| Dimension | Score |
| --- | ---: |
| Accuracy | 99 |
| Completeness | 98 |
| Consistency | 99 |
| **Overall AGREE** | **99** |

**Verdict:** AGREE

## Finding coverage

1. **P2 Revive soft-deleted requirements** — PASS. `LoadExistingAsync` uses `IgnoreQueryFilters(SoftDeleteQueryFilter)`. `ApplyItems` clears `IsDeleted` / `DeletedAtUtc` / `DeletedBy` / `DeleteReason` (and resets `CreatedAtUtc` when previously deleted), matching `RequirementsDatabaseDocumentService.AddRequirementAsync`. Covered by `ApplyAsync_SoftDeletedRequirement_IsRevived`.
2. **P2 Validate priority/status lengths** — PASS. After lowercase normalization, `PriorityMaxLength=32` and `StatusMaxLength=64` match `RequirementEntity` column attributes; oversize → `ArgumentException` (400) before the transaction. Covered by `ApplyAsync_PriorityOrStatusExceedsColumn_IsArgumentExceptionWithoutWrite`.
3. **P2 Retry requirement-key races** — PASS. Unique violations no longer fabricate an idempotency-key payload conflict when no recovery-run row exists. Loop retries up to `UniqueRaceMaxAttempts`; exhausted attempts rethrow `DbUpdateException`. Idempotency-key races still resolve via `FindRunAsync` → Replay/Conflict. Covered by `ApplyAsync_UniqueWithoutRun_RetriesThenSucceeds` and `ApplyAsync_UniqueWithoutRun_ExhaustedRetries_PreserveDbUpdateException`. Soft-delete revive covers the reload-as-upsert path for hidden PK rows.

## Residuals (non-blocking)

- Concurrent PK race is proven via synthetic UNIQUE interceptor (SQLite Serializable + shared-cache inject deadlocks on table lock); production path still reloads via `LoadExistingAsync` on retry after a real unique failure once the winner commits.
- `ClearSoftDelete` on every update is slightly broader than "only when deleted" but matches revive intent and avoids stale tracked shadow values.

## Evidence

- `dotnet test` filter `FullyQualifiedName~RequirementsRecoveryTests`: **16 passed**, 0 failed
- Builds: Support.Mcp + Support.Mcp.Tests — green
- PR left unmerged by design