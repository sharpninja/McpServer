# BUG-TRIAGE-139 fourteenth remediation receipt

## Outcome

All ten findings in the thirteenth independent review were remediated and validated on branch `codex/bug-triage-139-remediation`. This is a remediation candidate, not approval: BUG-TRIAGE-139 remains open pending a fresh independent review. No merge or push was performed.

- Approved comparison base: `210cb223d81dac6b4045b868e4d7b2712d08b752`.
- Rejected candidate: `4e0751530fdb6035a8195bfd411260260002ef48`.
- Rejected verdict: **NOT APPROVED**.
- Authoritative review copy: `docs/receipts/artifacts/BUG-TRIAGE-139/reviews/thirteenth-independent-review.txt`.
- Source and repository-copy SHA-256: `343F2690A92970E2EFFC882E7F33A9DA27A20E28968D09849EBF5FD3D8909ECE`.
- Fourteenth evidence: `docs/receipts/artifacts/BUG-TRIAGE-139/fourteenth-green/final`.

## Validated remediation commits

- `4017d65f272bafe9bd7af5876f6708c1e6952b41` — handle-bound contained mutations and special-file rejection.
- `b93c97b702f01ab5dc6e823a4bc5784a3351132a` — canonical persisted identifiers before hashing.
- `0baae58ba694f69e03b1a0c8ca628f4ad5b5941d` — bounded, cancellable, joined physical-path resolution.
- `d521de2e3c29f7ba0eb4a51349b625c0c777daac` — exact SQLite state protection, bounded opening, and cancellation classification.
- `9b915f92cef7456533c3b7f51007ffb8378d4076` — retained prompt-lock cancellation without broad exception masking.
- `c3775a55b9c36db5245079cb892e24a696e9aebc` — hermetic repository discovery and bounded process-tree draining.
- `9613fe5d115824f50b16cd711d9d19d3c5565c09` — explicit supersession of rejected twelfth claims.
- `c69634f3c5be7d1f44794697d108842042883bd0` — global empty-identity, test-state isolation, Agent Help, and policy-log regressions.
- `2ac2d4581fa2d7d073afc9d21874d13f0f6edea4` — short-lived Windows child/job-assignment race.
- `27dcf49d8cbed3584a9cccb5c1de9e31a4de3af3` — bounded test-only allowance for the immutable SQL Server migration chain.
- `f56961d5532c1c85e48610d76ee531191fc6ecb2` — first committed fourteenth evidence tree and independently verified inventory.

Each commit has one adjacent parseable `AI-Signature` / `AI-Confidence` trailer pair.

## Finding-by-finding resolution

1. **Rollback and Windows creation races.** `WorkspaceContainedFileSystem` now performs deletion, directory creation, and replacement relative to pinned contained directory handles: Windows uses handle-relative native opens/mutations and Linux uses `openat` / `unlinkat` with no-follow validation. `TransactionGatedRequirementsDocumentService` delegates rollback deletion and directory creation to this boundary. `RequirementsFourteenthAdversarialReviewTests` exercises actual rollback-directory and creation swap windows plus containment contracts. RED was 0/4; affected GREEN was 28/28.

2. **Unicode-dash identity.** `WorkspaceIdentity.NormalizePersistedIdentifier` canonicalizes persisted identifier values before storage-key and hash construction. `McpDbContext`, use-case creation, replay helpers, workspace IDs, proxy IDs, paths, and request IDs hash the exact final persisted ASCII values. Consumer RED was 0/2; cross-provider GREEN was 4/4, and the no-root external suite passed 20/20 across SQLite, LocalDB, and PostgreSQL.

3. **Physical identity cancellation and boundedness.** Async SaveChanges and federation upsert now await identity resolution. Native Windows resolution uses one named worker, finite linked cancellation, `CancelSynchronousIo`, cancellation checks between ancestor opens, and a mandatory join before completion; there is no abandoned `Task.Run` work. Native invocation/active-worker probes cover repeated calls and pre-cancellation. RED was 0/3; affected GREEN was 31/31. A discovered global empty-identity regression was RED 0/2 and GREEN 2/2.

4. **Exact SQLite state.** Caller-owned connections retain their exact managed timeout, EF command timeout, connection string, native busy timeout, and custom busy handler. Short read state is restored in `finally`; owned lookup connections are non-pooled and disposed; operation cancellation pins the real native handle only for the operation lifetime. Tests intentionally set managed, EF, native timeout, and handler state to different values and cover success, failure, cancellation, disposal, pooling/non-pooling, and repeated calls. RED state scopes were 1/4 and 0/2; the final affected-provider gate passed 42/42.

5. **Bounded SQLite opening before a handle.** `SqliteBoundedConnectionOpener` executes inherited synchronous open on a joined worker. Caller cancellation and finite timeout interrupt Windows synchronous I/O before a handle and use `sqlite3_interrupt` after handle publication, then close/restore state and join. Tests cover cancel-before-handle, timeout, close/dispose races, pooling, repeated opens, zero active workers, and interrupt lifetime. Compile RED executed 0 tests because the opener was absent; final provider GREEN is included in 42/42.

6. **Provider exception preservation.** Cancellation conversion now requires provider-specific proof: SQLite `SQLITE_INTERRUPT` plus an issued operation interrupt, SQL Server’s cancellation error signature, or PostgreSQL SQLSTATE `57014`. Unrelated SQLite, SQL Server, and PostgreSQL exceptions survive even when the token is concurrently cancelled. The real SQLite classifier failed in RED; final affected-provider coverage passed 42/42.

7. **Linux special files.** Linux contained opens use no-follow, nonblocking descriptors and `fstat` type enforcement; only regular files or expected directories are accepted. FIFO, socket, and device entries cannot enter the blocking snapshot path. The no-writer FIFO contract failed in the 0/4 RED slice and passes in the 28/28 affected scope.

8. **True external root discovery.** Evidence tests share `RepositoryEvidenceTestSupport.ResolveRepositoryRoot`, which validates configured roots and otherwise searches immutable `CallerFilePath` source provenance before assembly/base/current-directory candidates. The genuine C:-artifact run with `MCP_REPOSITORY_ROOT` absent passed 20/20; no environment injection is part of that proof.

9. **Descendant-held pipes and tree cleanup.** The process runner applies the same finite token to direct-parent wait, stdin, stdout, and stderr drains. Timeout kills the Windows job or POSIX process group and bounds termination/drain cleanup. The real descendant-held-pipe RED failed after 30 seconds; the harness passed 4/4, including 128 quick Git children and the direct-parent exit race.

10. **Evidence accuracy.** The twelfth receipt and README now explicitly identify candidate `4e075153...` as rejected, record the real 13/7 no-root result, name all three missing command mappings, and classify aborts as inconclusive. Historical artifacts were retained. This new evidence tree preserves every selected failure and replacement, maps every output to a command, and is independently hashed.

## RED-to-GREEN notes

The evidence README is the run ledger. Important non-passes remain visible:

- The first current no-root external run completed 18 tests before a testhost abort. It is inconclusive/failing, not 18/18.
- The second completed 20 with 19 passing and one LocalDB migration timeout before product assertions. It is failed, not a near-pass.
- The focused LocalDB retry reproduced 0/1 at the 120-second test-fixture migration bound. A bounded 300-second test-only migration limit was then applied; it changes no production timeout or assertion. The rebuilt focused run passed 1/1 and the complete external run passed 20/20.
- The 2,038/2,040 NUKE artifact is only the first assembly of an interrupted cumulative gate. The accepted cumulative replacement is 3,323/3,323 across seven assemblies.
- The 250/252 authoritative run and the misleadingly named 0/2 SQLite cancellation directory are both classified as RED.
- Two post-commit repository audits passed 13/14 and failed the byte-level EOL assertion: first on the committed evidence set, then after an inventory-only correction exposed the remaining command/result files. Both are retained under `red/`; the corrected replacement passed 14/14.

## Final validation ledger

Counts overlap; they are not summed.

- Focused containment GREEN: 28/28.
- Identifier providers: 4/4.
- Bounded physical identity: 31/31.
- Final affected providers: 42/42 across SQLite, LocalDB, and healthy ephemeral PostgreSQL.
- Hermetic true-external no-root Twelfth scope: 20/20.
- Authoritative UseCase ledger: 252/252.
- Global empty identity: 2/2.
- Identity/Agent Help regression: 23/23.
- Static identity/policy regression: 44/44.
- Harness: 4/4.
- Marker/client/REST-MCP/requirements/wiki/workspace preflights: 24/24, 30/30, 17/17, 74/74, and 16/16.
- Repository/harness audits: 14/14 before final evidence assembly, 14/14 against the live assembled tree, and 14/14 against committed evidence after two retained 13/14 terminal-newline failures.
- Full cumulative unit gate: 3,323/3,323 across seven assemblies.
- Release matrix: restore plus 12/12 projects, warnings as errors, zero warnings/errors.
- `build.ps1 Compile`: passed.
- `build.ps1 Test`: passed.
- `build.ps1 ValidateTraceability`: passed.
- Plugin synchronization: not run because no plugin implementation or generated plugin artifact changed.

Every accepted test gate recorded zero failures, zero skips/not-executed, zero aborts, and zero inconclusive outcomes. All test/build artifacts were physically routed to C: or another non-F: target.

## Remaining work

- Independent fourteenth adversarial review.
- BUG-TRIAGE-139 remains open until that review approves the candidate.
