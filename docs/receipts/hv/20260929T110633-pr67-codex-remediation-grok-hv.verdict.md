# Grok HV — PR #67 Codex tip 3175ad98 remediations

**Reviewer:** Grok Bot (HV path only; no Codex/Astra for HV)
**Subject:** titleless TEST recovery, Id≤128, priority/status lowercase, exception ctor binary compat
**Base tip:** `3175ad98`
**Timestamp (CT):** 2026-09-29 11:06:33 CT

## Scores

| Dimension | Score |
| --- | ---: |
| Accuracy | 99 |
| Completeness | 99 |
| Consistency | 99 |
| **Overall AGREE** | **99** |

**Verdict:** AGREE

## Finding coverage

1. **P1 titleless TEST** — PASS. `NormalizeItems` requires body always; title only for `fr`/`tr`. REPL `ValidateRecoveryPayload` uses `OptionalText` for `kind=test`. Covered by `ApplyAsync_TitlelessTest_IsAcceptedAndPersisted` and `PlanRecovery_TitlelessTest_IsValid` / `PlanRecovery_TitlelessFr_IsInvalid`.
2. **P2 Id max 128** — PASS. `IdMaxLength` matches `RequirementEntity.Id`; rejected before transaction. Covered by `ApplyAsync_IdExceeds128_IsArgumentExceptionWithoutWrite`.
3. **P2 priority/status lowercase** — PASS. Same `Trim().ToLowerInvariant()` defaults as `RequirementsDatabaseDocumentService`. Covered by `ApplyAsync_PriorityAndStatus_AreNormalizedToLowercase`.
4. **P2 exception ctor binary compat** — PASS. Restored distinct CLR overloads for legacy one-/two-arg ctors; metadata-bearing overloads kept. Covered by `McpClientExceptionCtorCompatTests`.

## Residuals (non-blocking)

- REPL YAML schema does not duplicate Id≤128 (service enforces before SaveChanges; Codex asked for pre-transaction validation).
- `new McpClientException(msg, status, null)` remains theoretically ambiguous between `string?` and `Exception`; no current call site uses that form.

## Evidence

- `dotnet test` RequirementsRecoveryTests: 12 passed
- `dotnet test` McpClientExceptionCtorCompatTests + ErrorHandlingTests: 8 passed
- `dotnet test` RequirementsRecovery* (REPL): 5 passed
- `dotnet test` ReplMcpErrorClassifierTests: 15 passed
- Builds: Support.Mcp, Client, Repl.Core — 0 warnings / 0 errors