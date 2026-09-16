# BUG-TRIAGE-139 sixteenth remediation receipt

## Disposition

This is a remediation candidate, not approval. The authoritative sixteenth independent review rejected `f410963f94d75064197ff576de3ee163a5f10f90` with seven findings. Core implementation is frozen in content commit `031af32d9a1043311b8ccb96c49e3909d53daf21`, and the initial evidence commit is `25b7a6925bf8354982e55ec5a4e7d88710051a47`. Its exact-HEAD audit exposed byte-level EOL drift, corrected without semantic changes in `987bfb99f1c721fba9bcd4ba3370d97dd8c99b92`; this receipt is amended once before the immutable final rerun.

BUG-TRIAGE-139 remains open with `done=false` until another independent review approves. The linked FR/TR/TEST records remain `in_progress`. No merge, push, deployment, or durable-store migration was performed.

- Approved comparison base: `210cb223d81dac6b4045b868e4d7b2712d08b752`.
- Rejected sixteenth candidate: `f410963f94d75064197ff576de3ee163a5f10f90`.
- Remediation content commit: `031af32d9a1043311b8ccb96c49e3909d53daf21`.
- Initial evidence commit: `25b7a6925bf8354982e55ec5a4e7d88710051a47`.
- EOL-only normalization commit: `987bfb99f1c721fba9bcd4ba3370d97dd8c99b92`.
- Branch: `codex/bug-triage-139-remediation`.
- Authoritative review: [sixteenth-independent-review.txt](artifacts/BUG-TRIAGE-139/reviews/sixteenth-independent-review.txt), SHA-256 `47CAD88F29DE6A4131D7B50F8CF0CD337B362316DAD41001BB49FB7F556C60D7`.
- Exact content file inventory: [content-changed-files.tsv](artifacts/BUG-TRIAGE-139/sixteenth-remediation/content-changed-files.tsv).
- Machine-readable declaration: [evidence-declaration.yaml](artifacts/BUG-TRIAGE-139/sixteenth-remediation/evidence-declaration.yaml).

## Immutable final-evidence design

A commit cannot truthfully contain its own SHA. After this receipt amendment is committed, no repository content may change. Every required gate is then rerun against that exact commit, and the exact final SHA, command vectors, parsed counters, hashes, cleanup state, and branch status are serialized to:

`C:\Users\kingd\AppData\Local\Temp\BUG-TRIAGE-139-sixteenth-review\BUG-TRIAGE-139-sixteenth-remediation-20260904T181236Z\final-verification\exact-final-head.yaml`

This external C: artifact is the authoritative exact-final-HEAD ledger. The repository declaration identifies its location and design without pretending to know a self-referential commit SHA.

## Requirement and mapping receipts

The supported MCP wrapper created and then queried:

- `FR-MCP-USECASE-017` — 10 acceptance criteria.
- `TR-MCP-USECASE-019` — the same 10 acceptance criteria.
- `TEST-MCP-USECASE-020` — the same 10 acceptance criteria.
- FR→TR and FR→TEST mappings — exactly two normalized mapping rows.

The canonical acceptance-criteria SHA-256 is `DC5A89C426EDB5002767C570F98BB05FF9C717096CD63C0E880CF877E1A138AF`. The live wrapper receipt is under the external evidence root at `mcp-receipts\requirements-and-mappings-live.yaml`, SHA-256 `451EFE946B0EF2712401A6A01B7FD991A9D2353B8035807C7E627C6239A24E03`.

The wrapper was degraded during receipt aggregation and session persistence. Attempts were bounded, failsafe records were retained, and no replay loop was performed.

## BDPv4 RED → GREEN record

All test results below are real executed tests with zero skips, aborts, or inconclusive results. The external precommit inventory contains 99 parsed TRX files, including 22 preserved RED TRX files, at `diagnostics\trx-inventory-precommit.yaml`, SHA-256 `86926913265303AFCCDDA0628198D5E21FDB9DD7598D16E7D23695A95587F842`.

1. Physical identity:
   - RED: synchronous-surface 0/2, Windows cancellation boundary 0/2, worker-tail accounting 0/1, and async-use-case consumer 0/1.
   - GREEN: slice Windows 2/2; worker-tail 1/1; sync-surface 5/5; async-use-case 1/1; cumulative identity scope 16/16.
2. SQLite pre-handle opening:
   - RED: consumer state 0/1, worker-tail 0/1, and pool identity 0/1.
   - GREEN: consumer 1/1, worker-tail 1/1, pool identity 1/1, and Windows cumulative SQLite scope 7/7.
3. PostgreSQL causality:
   - RED: 2 passed and 3 failed of 5, with the server-timeout/concurrent-token cases failing.
   - GREEN: mocked contract 5/5 and cumulative 7/7; fresh PostgreSQL race 2/2.
4. Platform execution inventory:
   - RED: compiled-inventory contract 0/1.
   - GREEN: inventory contract 1/1, source-fallback removal 3/3, and Windows platform scope 7/7.
5. Containment success:
   - RED: total-rejection acceptance guard 0/1.
   - GREEN: success-required race and deterministic rejection scope 3/3.
6. Windows descendant containment:
   - RED: process-tree scope 1 passed/1 failed and suspended-creation boundary 0/1.
   - GREEN: immediate-descendant process scope 2/2, suspended boundary 1/1, and cumulative process scope 7/7.
7. Receipt/evidence:
   - RED: supersession and executable-ledger consumers 0/2, TRX SHA-256 `79FA82E6C39809BA869ABDC4CE4C4A64E161540B7D1C4D2F9FFF88C4A14861A2`.
   - GREEN: both consumers 2/2 while executing 21 safe Git/ripgrep boundary commands and comparing all seven old/new trees, TRX SHA-256 `A6BDFDF7B8BC85B9D8E1FDF63B68E58ABB6DAF565B2D02C8907460889707D057`.

The complete current Windows seven-finding scope passed 39/39 before the evidence slice. The exact-final rerun must include the two evidence tests.

## Seven current finding map

1. Physical workspace identity: `LinuxPhysicalPathResolver` uses kernel-enforced `openat2(RESOLVE_NO_XDEV|RESOLVE_NO_MAGICLINKS)`; Windows physical traversal runs on an interruptible, finitely pumped native-I/O thread; an unkillable helper remains in active-worker accounting until it actually exits; opaque identifiers remain lexical; use-case CQRS handlers resolve identity asynchronously with caller cancellation. Consumer, Windows-native, and Linux-native test classes cover bounded completion, replacement timing, and worker observability.
2. SQLite pre-handle opening: validation repeats at the worker boundary; Linux atomically opens and pins the exact database descriptor without crossing a mount; Windows cancellation cleanup has a finite deadline; completion and worker counts are published only after real thread exit; connection state, connection string, pooling identity, and original exception causality are retained.
3. PostgreSQL classification: only an `OperationCanceledException` carrying the exact caller token is translated for Npgsql. SQLSTATE `57014` alone is never causal evidence. Mocked tests and the fresh-provider two-case race preserve server statement-timeout `PostgresException` while retaining genuine caller cancellation.
4. Platform tests: shared, Windows, and Linux native tests are composed by MSBuild host applicability. Source-string fallback branches are removed. The compiled inventory test reflects actual discovered `Fact` methods and exact host counts.
5. Containment races: read/write loops require at least one successful contained operation, narrowly classify transient failures, and include deterministic all-rejected controls that must fail the assertion helper.
6. Windows descendants: processes are created suspended with inherited redirected handles, assigned to a kill-on-close Job Object before their primary thread resumes, and cleaned within one deadline. A fast managed helper creates the descendant immediately; timeout and cancellation tests prove termination. Unix process-group cleanup remains finite.
7. Evidence integrity: the rejected fifteenth receipt now begins with a supersession notice. F17, G37, and G38 map rows point to a 23-row exact executable/argument-vector ledger. The evidence tests hash the historical F17 artifact and execute every safe G37/G38 command.

## Ten prior finding map

1. Pinned containment mutations remain covered, and the strengthened read/write races require visible success.
2. Unicode-dash normalization before persistence and hashing remains unchanged and passes the provider aggregate.
3. Async consumers now use cancellable physical identity end to end; sync public physical traversal is absent.
4. SQLite managed, EF, connection-string, busy-timeout, transaction, reopen, and lifecycle behavior remains covered.
5. Unsafe pre-handle aliases and replacement windows are closed by atomic Linux lookup/open and finite Windows work.
6. Unrelated SQLite failures remain original, while PostgreSQL caller cancellation now requires exact-token causality.
7. FIFO/special-file tests are Linux-only compiled execution tests rather than non-Linux source-pass substitutes.
8. Deterministic external seven-class and Agent Help gates remain independent of `MCP_REPOSITORY_ROOT`.
9. Pipe drains share the parent deadline, Unix groups remain finite, and Windows containment begins before user code.
10. Historical receipts remain preserved with visible supersession; commands, hashes, counters, and final-SHA evidence use mechanical ledgers.

## Exact-HEAD audit correction

The first exact-HEAD repository/history/harness/EOL audit ran against `25b7a6925bf8354982e55ec5a4e7d88710051a47` and failed 17/18 because ten async use-case files mixed CRLF with their baseline LF style. That RED TRX is preserved at `final-verification\11-repository-history-harness-eol-audit-18`, SHA-256 `5D460E0DC347F5C03BA83A5AE5C32046DDEAC17B7D8AE5DEC9172A055738BFA8`.

The ten files were Git-renormalized to LF with no semantic diff. The same real audit then passed 18/18 with zero skips, SHA-256 `B62BFD451D11FD4AF306396AF45F6B060B471D57F3DAAB3BCF9DE328D56B8F52`, before commit `987bfb99f1c721fba9bcd4ba3370d97dd8c99b92`.

This gate-discovered correction invalidates every earlier exact-HEAD label. Only the external ledger produced after this receipt amendment is authoritative.

## Precommit gates

- Current Windows seven-finding scope: 39/39.
- SQLite, LocalDB, and fresh PostgreSQL provider aggregate: 42/42.
- Fresh PostgreSQL causal race: 2/2.
- LocalDB historical migration allowance: 3/3 and test-only.
- Deterministic external seven-class gate: 20/20 with `ContinuousIntegrationBuild=true` and no root override.
- Agent Help: 18/18 under the same external build contract.
- Full BDPv4 unit target: 3,349/3,349 across seven assemblies.
- `.\build.ps1 Compile`: exit 0, zero warnings/errors.
- `.\build.ps1 ValidateTraceability`: exit 0, zero findings.
- Release warnings-as-errors matrix: 12/12 projects.
- Repository/history/harness/EOL Windows audit: 16/16 before adding the two evidence tests; exact-final expected count is 18.

These are precommit proofs only. The external exact-final ledger supersedes their SHA association after all final gates rerun.

## Native Linux execution boundary

Linux-only sources include seven real FUSE/alias/worker/transaction tests plus the retained native tests and non-root/FIFO tests. They are not compiled into the Windows inventory and were not reported as passed, skipped, or source-substituted.

The host's WSL control plane remained unresponsive under bounded probes. Docker's WSL backend also stalled, and the independent Hyper-V backend reached its pipe but returned `Docker Desktop is unable to start`. Each attempt was bounded and fully cleaned; no replay loop was used. Consequently no current native-Linux/FUSE/non-root GREEN claim is made. Another independent review must require that gate on a functioning Linux host before approval.

## Command ledger correction

The formerly indirect F17, G37, and G38 cells are expanded in [superseded-command-ledger.tsv](artifacts/BUG-TRIAGE-139/sixteenth-remediation/superseded-command-ledger.tsv):

- F17 contains the exact failed and corrected WSL argument vectors and hashes the historical output.
- G37 contains seven independently executable repository/ref/deployment-audit commands with expected exits.
- G38 contains all fourteen `git rev-parse <sha>^{tree}` invocations for seven old/new pairs.

## Deployment and migration boundary

The evidence proves only this local branch, Git refs visible to this checkout, isolated provider fixtures, and the recorded command history. It does not assert facts about unrecorded external systems. No push, merge, deployment, production migration, or durable-database repair occurred. The SQL Server historical allowance exists only in finite test interception; production migration sources are unchanged.
