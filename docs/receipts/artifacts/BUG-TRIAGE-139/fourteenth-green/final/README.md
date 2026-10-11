# BUG-TRIAGE-139 fourteenth validation evidence

Captured on 2026-09-04 from `codex/bug-triage-139-remediation`.

## Status and provenance

- Approved comparison base: `210cb223d81dac6b4045b868e4d7b2712d08b752`.
- Rejected candidate: `4e0751530fdb6035a8195bfd411260260002ef48`.
- Thirteenth verdict: **NOT APPROVED**.
- Authoritative thirteenth review: `../../reviews/thirteenth-independent-review.txt`.
- Review SHA-256: `343F2690A92970E2EFFC882E7F33A9DA27A20E28968D09849EBF5FD3D8909ECE`.
- BUG-TRIAGE-139 remains open pending a fourteenth independent review.
- No merge or push is represented by this evidence.

Commands, environment declarations, consoles, result summaries, and TRX files are grouped by run below `red/` and `green/`. A result file is authoritative only together with the command, environment file when present, console, and TRX sharing its directory. Every copied output has a command file in its directory or its immediate run parent.

## Classification rules

- **RED / failed** means an assertion, compile, command, or minimum-count gate failed.
- **Aborted / inconclusive** means a test host or harness ended before the required scope completed. Partial passes are not accepted.
- **GREEN / passed** requires exit zero, the required count, zero failed, zero skipped/not-executed, zero aborted, and zero inconclusive.
- Counts from different rows overlap and must not be summed.
- Directory names inherited from exploratory runs are not classifications. In particular, `sqlite-save-cancellation-green-tests` is retained under `red/` because it failed 0/2.

## RED evidence retained

- `red/slice-1-intended`: 0 passed, 4 failed, 0 skipped. Rollback deletion, directory-creation swap, containment delegation, and Linux FIFO contracts failed before the handle-relative implementation.
- `red/slice-2-identifiers`: 0 passed, 2 failed, 0 skipped. Raw U+2013/U+2014 values persisted and did not replay through canonical ASCII identity.
- `red/slice-3-physical-identity-intended`: 0 passed, 3 failed, 0 skipped. Async consumers and native-worker lifecycle contracts were absent.
- `red/slice-4-6-sqlite-state`: 1 passed, 3 failed, 0 skipped. Independent managed/EF/native state, custom busy-handler preservation, and real SQLite error classification failed.
- `red/slice-4-sqlite-aggregate-state`: 0 passed, 2 failed, 0 skipped. Aggregate SaveChanges state and disposal restoration failed.
- `red/slice-5-sqlite-bounded-open`: compile RED, exit 1, 0 tests executed, no TRX. The bounded opener did not exist. Its result also points to the later real-lock 0/1 failing TRX.
- `red/slice-8-root-discovery`: 0 passed, 2 failed, 0 skipped. Evidence consumers duplicated current-directory discovery and the external-root contract failed.
- `red/slice-9-descendant-pipe-confirmed`: 0 passed, 1 failed, 0 skipped after 30 seconds. A real descendant retained redirected pipes and the runner did not kill the tree within the bound.
- `red/global-empty-workspace-red`: 0 passed, 2 failed, 0 skipped. The first async identity implementation broke the global empty-workspace save contract.
- `red/sqlite-save-cancellation-green-tests`: despite its historical name, 0 passed, 2 failed, 0 skipped. Prompt SQLite lock cancellation regressed and was not accepted.
- `red/usecase-authoritative`: 250 passed, 2 failed, 0 skipped. This was a failed 252-test authoritative gate, not a near-pass.
- `red/nuke-test-final4`: only the first assembly completed: 2,038 passed and 2 failed of 2,040 in that assembly. The remaining six assemblies did not run, so this is an incomplete failed cumulative gate.
- `red/hermetic-external-no-env-final`: 18 tests completed before the blame-hang collector aborted the test host. Console text says “Test Run Aborted”; this is inconclusive/failing even though the TRX counters recorded no failed assertions and the generic result parser reported `aborted=0`.
- `red/hermetic-external-no-env-final2`: 19 passed, 1 failed, 0 skipped. LocalDB timed out while applying the immutable historical migration chain before the durable-identifier assertion.
- `red/hermetic-external-sqlserver-recovery`: 0 passed, 1 failed, 0 skipped at the same 120-second test-fixture migration bound.
- `red/repository-audits-committed-evidence`: 13 passed, 1 failed, 0 skipped. The byte-level EOL audit rejected newly committed evidence text without terminal newlines.
- `red/repository-audits-committed-evidence-newline-scan`: 13 passed, 1 failed, 0 skipped. After the inventory-only correction, the audit enumerated the remaining command/result terminal-newline defects; this run is retained as failed.

## Focused GREEN evidence

- `green/slice-1-affected-diagnostic`: 28/28.
- `green/slice-2-identifiers-providers`: 4/4, including canonical replay providers.
- `green/slice-3-physical-identity-affected-green2`: 31/31.
- `green/slice-4-5-6-affected-providers-final2`: 39/39 across SQLite, LocalDB, and healthy ephemeral PostgreSQL.
- `green/slice-8-9-harness-affected`: 3/3.
- `green/global-empty-workspace-green-rerun`: 2/2.
- `green/global-identity-agenthelp-green`: 23/23.
- `green/identity-static-policy-green`: 44/44.
- `green/fourteenth-harness-final2`: 4/4, including the real descendant-held-pipe and repeated quick-process cases.
- `green/hermetic-external-build-after-timeout-adjustment`: Release test artifact rebuilt on C:, zero warnings and zero errors.
- `green/hermetic-external-sqlserver-green`: 1/1 focused LocalDB durable-identifier scenario.
- `green/hermetic-external-no-env-final3`: 20/20 from the true external C: runner with `MCP_REPOSITORY_ROOT` absent.
- `green/affected-providers-final7`: 42/42 across SQLite, LocalDB, and four healthy ephemeral PostgreSQL lifecycles.

## Complete GREEN gates

- `green/usecase-authoritative-final2`: 252/252.
- `green/marker-trust-final2`: 24/24.
- `green/client-usecase-final3`: 30/30.
- `green/rest-mcp-usecase-final3`: 17/17.
- `green/requirements-wiki-final3`: 74/74.
- `green/workspace-identity-provider-final3`: 16/16 with its PostgreSQL data directory under the C: runtime root.
- `green/repository-audits-final4`: 14/14 before final evidence assembly.
- `green/repository-audits-post-evidence`: 14/14 against the live assembled evidence tree.
- `green/repository-audits-committed-evidence`: 14/14 against committed evidence after the two retained terminal-newline failures were corrected.
- `green/nuke-test-final6`: 3,323/3,323 across seven assemblies. Zero failures, skips, aborts, or inconclusive outcomes.
- `green/nuke-compile-final3`: `build.ps1 Compile` succeeded.
- `green/nuke-traceability-final2`: `build.ps1 ValidateTraceability` succeeded.
- `green/release-build-matrix-final3`: restore succeeded and 12/12 Release projects built with warnings as errors, zero warnings, and zero errors.

## Artifact placement

Test and build execution used C:-backed physical destinations:

- Direct external artifacts: `C:\Users\kingd\AppData\Local\Temp\BUG-TRIAGE-139-fourteenth-20260904T050327Z`.
- NUKE logical artifact junction target: `...\final-gates\nested-logical-artifacts-target` on C:.
- Runtime `TEMP` / `TMP`: `...\final-gates\runtime-temp` on C:.
- `TestResults` junction target: `...\final-gates\nuke-test-results` on C:.

The genuine hermetic proof did not inject `MCP_REPOSITORY_ROOT`.

## Integrity

Raw console/stdout/stderr and TRX files are marked `-diff` by this directory's `.gitattributes` so their captured bytes—including trailing spaces emitted by NUKE—remain unchanged. Commands, environment records, result summaries, README, and receipt remain ordinary text. The initial 550-diagnostic cached check and both 13/14 committed-evidence EOL audits are retained as failed evidence; the editable evidence text was then normalized to the repository terminal-newline contract.

`SHA256SUMS.txt` hashes every other file in this directory. `inventory-verification-result.txt` records an independent parse and rehash with counts for matches, missing paths, unlisted paths, malformed entries, and duplicates.
