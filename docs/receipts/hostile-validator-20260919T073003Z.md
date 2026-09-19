# Hostile validator receipt — H4-green (MCP-MEMORY-002 / S4)

| Field | Value |
|---|---|
| Phase | H4-green |
| Branch | memory-s4-red |
| Tip SHA | `a71baeb3930147185acb9d746e82045d44b6c6ec` |
| Filter | `FullyQualifiedName~McpServer.Support.Mcp.Tests.Memory` |
| Started (UTC) | 2026-09-19T07:29:16Z |
| Ended (UTC) | 2026-09-19T07:29:48Z |
| TRX SHA-256 | `5803d422336bbe0a4390ec0bdea302bf0300d937dd30124927358677f04de7ee` |

## Independent Memory filter counts

| Outcome | Count |
|---|---|
| Failed | **0** |
| Passed | **200** |
| Skipped | **0** |

## Catalog coverage (same filter)

| Slice | Pass / Total | Fail | Miss | Skip |
|---|---|---|---|---|
| S1 | 73/73 | 0 | 0 | 0 |
| S2 | 40/40 | 0 | 0 | 0 |
| S3 | 20/20 | 0 | 0 | 0 |
| S4 | **42/42** | 0 | 0 | 0 |

Compat Memory filter subset: Passed 37 / Failed 0 / Skipped 0 (embedded in 200).

## Focus checks (plan H4-green)

| Area | Evidence method(s) | Outcome |
|---|---|---|
| Per-workspace lock + TTL | `MemoryConsolidateTests.LockContention_BusyError`, `MemoryConsolidateJobTests.PerWorkspaceLock`, `LockExpiry_Recovers` | Passed |
| SSE workspace + run id | `MemoryConsolidateJobTests.EventIncludesWorkspaceAndRunId` | Passed |
| AllowHardDelete default false | `HardDelete_OnlyWhenConfigured`, `Decay_FlagsWithoutHardDelete` | Passed |
| Promote SourceKind / SourceRef | `MemoryPromoteTests.UnsupportedSourceKind_Returns400` (+ promote ops provenance ACs in S4 catalog) | Passed |
| Read-only cannot write-consolidate/promote | `MemoryAuthTests.ReadOnlyKey_CannotConsolidateWrite`, `ReadOnlyKey_CannotPromote` | Passed |
| Job disabled by default | `MemoryConsolidateJobTests.Disabled_NoEffectOnCrud` | Passed |
| No auto-promote | `MemoryPromoteTests.NoAutoPromote_OnTurnComplete` | Passed |
| No cross-workspace merge | `NeverMergesAcrossWorkspaces`, `CrossWorkspace_Forbidden` | Passed |
| Dry-run default | `DryRunDefaultTrue` (CRUD + job) | Passed |

## Scoring

| Metric | Score | Floor |
|---|---|---|
| Accuracy | **99** | ≥98 |
| Completeness | **99** | ≥98 |
| **OverallVerdict** | **AGREE** | AGREE required |

## Notes

- Tip implements S4 Green handlers: `ConsolidateMemoryCommandHandler`, `PromoteMemoryCommandHandler`, `RunConsolidateJobCommandHandler` plus `MemoryPromoteOperations` / `MemoryConsolidateOperations` (+ Locks partial).
- CS1591 treated as errors; tip includes XML docs on public consolidate ops members.
- No PR opened. Branch remains `memory-s4-red` only. MCP-MEMORY-002 not marked Done. S5 not started.

## Verdict

**AGREE** — S4 catalog 42/42 Pass; Memory filter Failed 0 / Passed 200 / Skipped 0; focus locks, SSE, hard-delete default, promote provenance, read-only gates, and no-auto-promote all green on tip `a71baeb3930147185acb9d746e82045d44b6c6ec`.
