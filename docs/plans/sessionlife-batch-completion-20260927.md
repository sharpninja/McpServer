# Complete the 35-Item Session Lifecycle Batch

Status: revision 4 approved for execution. P0 is in progress; no implementation gate or bug closure is accepted.

Workspace: `F:\GitHub\McpServer`.
Implementation worktree: `.worktrees/session-lifecycle`, branch `grok/session-lifecycle`.
Integration branch: `develop`; do not substitute `main`.
Planning TODO: `PLAN-SESSIONLIFE-001`.
Prepared: 2026-09-27 UTC.
Revision: 4. Section 7 contains the approved execution amendments and supersedes conflicting earlier process text. The technical scope remains binding. Approval is not implementation acceptance.

## 1. Objective and Execution Rules

Finish the existing Grok implementation, correct remaining contract and coverage gaps, prove every scoped bug against the actual code and persisted behavior, and integrate only the accepted changes. This is continuation, not a rewrite or a claim that Grok's historical green runs completed the batch.

- Use BDPv4 in `docs/Development-Process-draft-v4.md`: requirements and structured AC, mock-backed consumer contracts, a failing real-code test for each unsatisfied behavior, minimal implementation, refactor, full current-plus-prior unit regression, then integration validation.
- Use `gpt-5.6-sol` with `high` effort for coding. Use independent `gpt-6-astra` with `xhigh` effort for hostile review of code and documentation. Do not silently substitute a model. A model availability failure blocks that assignment, not unrelated planning.
- Reviews apply to code and documentation changes and their implementation evidence. They do not review the operator's requests or standalone operational actions.
- Use PowerShell.Mcp and pwsh for commands. Use the active Codex plugin for MCP operations. No raw MCP REST, handwritten YAML, direct database mutation, generated-file editing, Python, or alternative TODO store.
- Keep the user's paused unrelated work paused. Do not reopen the keyserver redesign, transcript-plugin endpoints, Handoff implementation, package publishing, SSH installation, or other batches.
- Preserve all existing worktree changes. No blanket reset, cleanup, staging, or deletion. Record provenance and dependencies before selecting a merge scope.
- Log decisions and actions immediately. Bind audit calls to an explicit agent, session ID, and request ID. A local cache or exit code alone is not proof of server persistence.
- Existing fulfilled behavior gets proof tests, not manufactured red failures. Historical TDD cannot be reconstructed by backdating records or deliberately breaking correct code. Missing historical gate evidence remains disclosed; new remediation gets genuine Red -> Green -> Refactor evidence.

## 2. Verified Starting Point

The 2026-09-27 investigation found all 35 scoped TODOs open. Both `develop` and the worktree were based on `6a565d8762072ed040469047791a57c80dbf1e88`. The worktree has 37 modified tracked files, 11,097 additions and 10,491 deletions, plus untracked production code, tests, fixtures, and receipts. Most line churn is in four memory benchmark artifacts, not session lifecycle code.

Grok development session: `01a0ceda-cfc0-7ba1-9649-0b785c9314fc`. Its goal stopped on usage exhaustion; a later continuation produced validation and review-remediation receipts. Do not resume the native session belonging to PluginIntegration test output as though it were the development session.

Historical evidence, not a fresh validation of the eventual merge:

- `.worktrees/session-lifecycle/docs/receipts/sessionlife-resume-20260924T015616Z/pester-full-summary.txt`: 180 passed, zero failures/skips, zero failed blocks/containers.
- The same directory's `build-test.txt` and `build-test-summary.txt`: Nuke Test exited 0; eight project summaries total 4,268 passed, zero failures/skips. `build/Build.Test.cs` excludes Integration, AiReview, Build.Tests, review projects, and dedicated integration projects. This is not an integration/provider gate.
- `.worktrees/session-lifecycle/docs/receipts/hv/20260924T031450Z-sessionlife-resume-hv.response.jsonl`: DISAGREE, accuracy 88, completeness 62, seven PASS, two FAIL, three UNKNOWN. Missing durable full-review logging and unproven requirements/TODO state prevented acceptance.
- `.worktrees/session-lifecycle/docs/receipts/hv-remediate-20260924T142043Z/SESSIONLOG-HV-turn.txt`: last recorded remediation at 2026-09-24T14:30:56Z, `Durable=False`, storage outage, and a stale-session binding. No final qualifying AGREE was located.
- The live effective-requirements query returns five SESSIONLIFE FRs, five TRs, five TESTs, all with empty structured `acceptanceCriteria`. Exact `getFr` reads confirm prose requirements exist but structured AC are absent. Five mappings exist; TEST-MCP-SESSIONLIFE-003 is not linked in the returned mapping set. FR-MCP-173 currently maps TR-MCP-TXNKEY-001 and TEST-MCP-221.
- The 35 TODOs have no implementation-task entries. All still carry generic triage links; BUG-TRIAGE-170 additionally has older persistence links. Requirement membership and actionable remaining work need reconciliation, not just a batch done flag.

### Concrete Gaps to Resolve

1. Restore the requirements gate: structured AC, code/test mappings, explicit supersession notes, and one governing behavioral FR per bug while preserving historical triage links.
2. Stop hook: `Close-PluginTurnIfNeeded` uses local `status` and age; it has no authoritative completion lookup in the inspected function. It also evaluates stale status before the later failed-build/audit gates and refreshes `lastUpdated` before those later gates. The original plan requires authoritative stale-pin proof and safety gates first.
3. Import recovery: `ReplayPendingImportRecoveryAsync` runs before `ValidateReadablePath`; replay normalizes each `sourceFile` as a separate bundle, bypassing the normal path-security entry point. It cannot recover from canonical YAML alone when source files are absent. Add focused tests before changing these paths.
4. Uniqueness: `IsSessionLogTurnUniqueRace` matches message substrings `UNIQUE` and `SessionLogTurns`, with SQLite-specific documentation. Demonstrate the intended behavior on each supported provider; do not assume this is a portable classifier.
5. Coverage: the new metadata/Stop suite has only six named cases. Inventory the complete existing test set against all original AC, including blocked stdin/stdout/stderr, unrelated 404, replay deduplication, request/session isolation, and both drain paths. Test presence or a total pass count is not coverage proof.
6. Documentation: the three BUG-TRIAGE-246 contract documents are not in the worktree diff. They may already conform; inspect and prove each, changing only contradictions.
7. Scope: worktree memory deletion, migration Down changes, Handoff test edits, benchmark rewrites, and shared test-harness changes need a dependency disposition. Do not merge them silently as session-log fixes.
8. Evidence: historical full review was not durably recorded under the intended identity. Recover evidence through supported operations, preserve attribution, and obtain a new review of the corrected patch.

## 3. Exact Batch and Governing Requirements

Each ID below is `BUG-TRIAGE-<number>`. Keep all 35 open until their individual acceptance evidence and applicable review gates are met.

### Cache and Recovery: FR-MCP-SESSIONLIFE-001 / TR-MCP-SESSIONLIFE-001

- 175: degraded begin preserves the complete cached turn, not a three-field replacement.
- 180: prompt hook reports durable opened only after primary persistence; otherwise opened-degraded.
- 181: complete recovers required query text without replacing a valid title.
- 184: update omits an empty query text and preserves the stored query/title.
- 205: degraded missing-turn dialog gets one recovery attempt then a retained dialog failsafe; unrelated 404 remains an error.
- 208: a locally active turn is not represented as server-persisted.
- 214: empty update query is accepted by the shim and omitted at the persistence boundary.
- 245: degraded begin preserves prompt/context and supplies creation metadata before any replay.

### Identity and Metadata: FR-MCP-SESSIONLIFE-002 / TR-MCP-SESSIONLIFE-002

- 182: native begin/dialog followed by plugin action append preserves the same session/request and required metadata.
- 185: update/append/complete distinguish first persistence from durable reopen.
- 198: freshness validation never rewrites an already-bound session ID.
- 239: first-persist metadata survives the recovery path.
- 246: explicit/cache/None precedence and raw API validation agree with all three contract documents.

### Persistence Outcomes: FR-MCP-SESSIONLIFE-003 / TR-MCP-SESSIONLIFE-003

- 186: appendActions honors the caller's matching request ID, rejects mismatch, and distinguishes retained failsafe from total failure.
- 202: primary success is supported by a server read of response, interpretation, tags, and contextList.
- 204: no generic failure exit after a confirmed queued result; no false primary-success claim.
- 213: unchanged repeated update does not create duplicate recovery work.
- 240: appendDialog emits a classified result instead of exit 1 with an empty diagnostic body.
- 241: sequential append/complete operations preserve method labels, queue counts, identity, and useful diagnostics.
- 243: title persistence timeout is queued only after confirmed failsafe write; total loss returns failure with diagnostics.
- 244: empty stderr is not the only record of failed persistence; the structured result explains the outcome.

### Replay and Deadline: FR-MCP-SESSIONLIFE-004 / TR-MCP-SESSIONLIFE-004

- 171: busy/locked/budget/wrapped contention remains retryable, not a generic 500; both drains retain retryable work without consuming attempts or latching completion.
- 172: repair and replay the three named malformed/exhausted quarantine records; retain valid values and delete only after durable success.
- 173: recover importRecovery envelopes through the canonical session persistence path, never submit the envelope as a session or expose ingestion endpoints to plugins.
- 221: one bounded child deadline covers pipe writes/reads and process-tree cleanup; inherited agent variables cannot redirect Codex's cache or audit identity.

### Stop and Storage: FR-MCP-SESSIONLIFE-005 / TR-MCP-SESSIONLIFE-005

- 229: predecessor-schema failure is diagnosed before save; provider failures are classified; an actual same-turn insert race gets one bounded retry.
- 234: stale-pin recovery requires authoritative completion and cannot bypass build/audit enforcement or clear genuinely active work.

### Existing Governing Requirements, Proof-First

- 170: FR-MCP-REPL-011 / TR-MCP-REPL-012. Prove the existing 120-second drain default and larger REPL_TIMEOUT precedence; do not rewrite already-correct timeout code.
- 165: FR-MCP-170, with scoped error/audit AC captured before further work. Reuse its existing technical mappings and add only missing links to TEST-MCP-SESSIONLIFE-002 and 004. Prove classified dialog storage errors, no false increment, and successful reconciliation to the exact persisted turn. Do not mislabel this as TEST-MCP-SESSIONLIFE-005.
- 199, 200, 210, 226, 232, 236: FR-MCP-173 / TR-MCP-TXNKEY-001. Preserve the existing coordinator bypass; prove actions/dialog/additive completion through durable reads and duplicate-free replay. Add TEST-MCP-SESSIONLIFE-003 to the mapping without dropping TEST-MCP-221. An old rollback requirement is superseded only where FR-MCP-173 changes it; all user-visible persistence AC still apply.

## 4. Structured Acceptance Criteria to Capture

Use existing IDs. Do not recreate, rename, or delete requirements. Add stable AC IDs through supported requirements operations, with `isSatisfied=false` until evidence is accepted. Preserve existing AC and mappings. The criteria below are a proposed representation of the original approved behavior and the bounded security/coverage corrections found during completion planning; approval of this plan precedes production changes.

### FR Criteria

FR-MCP-SESSIONLIFE-001:

- AC001: degraded begin retains queryText, queryTitle, planFile, todoId, turnRequestId, sessionId, openedAt, build state, and all audit counters.
- AC002: hook status differentiates primary-persisted from retained-local/degraded state.
- AC003: complete fallback order is cached queryText, cached queryTitle, then exact `Recovered session-log turn`; it does not force a replacement title. Update/append/fail omit empty queryText.
- AC004: degraded missing-turn appendDialog performs at most one resubmit; failed primary persistence retains a replayable dialog envelope. A never-degraded missing turn is not silently created.
- AC005: same-request degraded retry recovers creation metadata; supersession stores canceled, with cancelled accepted equivalently.

FR-MCP-SESSIONLIFE-002:

- AC001: never-persisted turns use explicit metadata, then cached metadata, then exact `None` for both fields on begin/update/append/complete.
- AC002: raw ordinary first persistence rejects missing/null/empty/whitespace fields; canceled/cancelled supersession may omit either field and stores exact None. Durable reopen preserves omitted values.
- AC003: explicit metadata updates both cache and outgoing payload. Matching durable identity is proven before selecting omission behavior.
- AC004: freshness checks reject wrong workspace/marker drift/source mismatch without changing a bound session; native and plugin calls keep exact session A/request R.
- AC005: module-bootstrap, REPL user guide, and REPL message schema document all creation/supersession/reopen cases consistently.

FR-MCP-SESSIONLIFE-003:

- AC001: every verb has three tested results: primary success; confirmed retained-failsafe queued success with persisted=false/degraded=true; failure with nonzero exit when neither destination received the write.
- AC002: results carry code, retryable, persisted, degraded, queued, method, requestId, failsafePath, message, and child stderr; fields accurately describe the attempted verb and disposition.
- AC003: caller request mismatch refuses persistence without mutating another turn or queue record.
- AC004: primary success is verified by exact-session/request query of changed scalars and additive children; local cache success is insufficient.
- AC005: repeated unchanged updates and sequential queued operations replay without duplicate turns/actions/dialog or a false title update.
- AC006: write-ahead failsafes are retained on degradation/ambiguous response and removed only after confirmed primary durability; queued success never authorizes TODO completion.

FR-MCP-SESSIONLIFE-004:

- AC001: the hook child has one 30-second default budget across asynchronous stdin/stdout/stderr and cleanup; timeout/cancellation cannot strand children or remove pending payloads.
- AC002: plugin identity and cache namespace remain Codex despite inherited Grok host variables; workspace ownership is never inferred from the plugin repository root.
- AC003: numeric-keyed and list-shaped historical envelopes are repaired object-first, preserving valid fields and filling only missing historical metadata with None; exhausted attempt counters do not suppress the explicit repair path.
- AC004: contention and outage are separately classified. Both automatic and explicit drains retain transient work, leave attempt counts unchanged for transient contention, do not latch completion, and can later replay successfully.
- AC005: importRecovery supports a validated canonical artifact or a reconstructed complete source bundle; the envelope itself is never submitted as session data. Preserve canonical artifacts and source files.
- AC006: validate workspace/agent identity, contained paths, reparse targets, size bounds, and record identity before reading or persisting recovery references. A request rejected by input validation causes no replay side effects.
- AC007: partial failure/cancellation retains the envelope; replay is idempotent across source bundles and concurrent retry. Delete only the matching unchanged envelope after all intended data is durably stored with no degraded result.

FR-MCP-SESSIONLIFE-005:

- AC001: failed-build and incomplete-audit checks execute before any stale-pin allow, refresh, or auto-close that could bypass them.
- AC002: stale local in_progress state may be reconciled only using authoritative completion for that exact workspace/agent/session/request. Different genuinely active work is not cleared. Unavailable/ambiguous query fails closed and retains state.
- AC003: exercise old/fresh/missing/invalid timestamp and lastUpdated combinations, completed/missing local turn, and mismatched request IDs; refreshing recency alone never proves stale-pin completion.
- AC004: schema-predecessor errors are detected before mutation and provider error responses retain useful classified details.inner without leaking credentials.
- AC005: one retry handles only the known same-session/request unique constraint; re-read the winning row, merge without duplicating children, preserve unrelated tracked work, and propagate unrelated failures/cancellation without an unbounded retry.

Existing requirements: preserve FR-MCP-REPL-009's distinction between successful queued workflow and primary persistence; record narrow supersession notes. Amend FR-MCP-SESSIONLOGCTX-001's ordinary-first-persist wording with the already-approved canceled/cancelled exception rather than leaving contradictory AC. Add explicit bounded AC for FR-MCP-170 and FR-MCP-REPL-011 where their current structured arrays are empty. Do not mark the entire broader FR-MCP-173 satisfied solely because its session-log subset passes.

### TR and TEST Criteria

- TR-001: real builder/shim preserve fields; one shared degraded-state transition; hooks consume that state. TEST-001 covers each FR-001 criterion with mock-backed boundary tests and real builder invocation with MCP_PLUGIN_PERSIST_LOG unset.
- TR-002: one durability-first metadata resolver and immutable bound identity; no alternate plugin implementation. TEST-002 covers metadata/identity, wrappers, deadlines, Stop state transitions, quarantine, and dialog audit, with an explicit named case for each applicable AC.
- TR-003: one typed three-outcome contract through script and wrapper; no success inferred from a bare false/true return. TEST-002 exercises actual process exit and serialized receipts, not only mocked internal booleans.
- TR-004: shared replay/repair uses canonical security and bundle/persistence components; failure preserves source and envelope. TEST-004 covers import recovery, schema/provider diagnostics, contention, and storage races using real services after mocked contract checks; TEST-005 locks correct 120-second default and session_dialog dispatch without new production behavior.
- TR-005: authoritative exact-turn Stop reconciliation, ordered enforcement, and a provider-aware bounded same-turn retry. TEST-002 covers Stop decisions; TEST-004 covers schema/race/error behavior. TEST-003 covers durable action/dialog query and repeated additive complete under existing FR-MCP-173.

Mapping gate: FR-001 -> TR-001 -> TEST-001; FR-002 -> TR-002 -> TEST-002; FR-003 -> TR-003 -> TEST-002; FR-004 -> TR-004 -> TEST-002/004/005; FR-005 -> TR-005 -> TEST-002/004. Preserve FR-MCP-173's existing mapping and add TEST-003. Link FR-MCP-170 to TEST-002/004 and FR-MCP-REPL-011 to TEST-005 through their existing TRs. Verify no scoped TEST or TR is orphaned. The shorthand in this paragraph means the SESSIONLIFE IDs above.

## 5. Completion Slices

### Mandatory Per-Increment Gate

Every new code/documentation increment in P1-P5 follows these gates:

1. Query accepted FR/TR/TEST/AC and record the next bounded test plan.
2. Make consumer-contract tests pass against mocks; show the real implementation failing the intended assertion. Fulfilled behavior stays green proof-only.
3. Astra xhigh reviews changed tests, AC coverage, and real red evidence before production edits. Require AGREE, both scores >=98, and the full durable review receipt.
4. Sol high implements the minimum fix, refactors, then runs the entire cumulative unit inventory defined below, including Build.Tests. Zero failures, skips, missing reports, or zero-discovery projects.
5. Astra xhigh reviews code/docs and Green evidence before advancing. The same receipt/score gate applies. A review request or retained failsafe alone does not pass it.

For documentation-only work, test executable contract/inventory assertions before the document change, then review the document. Do not invent a failing product test for prose-only corrections.

### Locked Stop Reconciliation Design

Add compatible typed SessionLogClient.GetAsync(agent, sessionId, cancellationToken) for the existing GET /mcpserver/sessionlog/{agent}/{sessionId} route, already backed by ISessionLogService.GetAsync. Return existing UnifiedSessionLogDto; no new server endpoint/store. Register the method in supported client invocation and test delegation/serialization. Agents call the plugin, not raw REST.

Shared helper Get-ReplTurnPersistenceProof(workspacePath, agent, sessionId, requestId) calls that operation once within the remaining hook deadline. Require exact workspace, sourceType, sessionId, and exactly one matching requestId. Exact-session retrieval avoids pagination/text-search ambiguity. Return identity, observedAtUtc, serverStatus, and outcome Completed, Active, Missing, Ambiguous, or Unavailable. Only exact completed authorizes stale in_progress completion repair; failed/canceled/cancelled are not relabeled completed. Missing fields, duplicate IDs, truncated data, 404, timeout, or contradictory identity cannot authorize clearing.

Transition order:

- Evaluate failed-build/incomplete-audit checks and existing explicit operator-override semantics first. No timestamp update or stale-pin allow precedes these checks.
- Snapshot current-turn/session-state identity and content hashes under a shared cache-state lock. Release it for bounded server lookup, reacquire, and require unchanged identity/hashes before mutation/output. All writers of these two files, including begin/prompt/complete/Stop, use the same lock. Never hold it across network I/O.
- If active B replaced snapshot A, block with identity-changed and do not modify B. If A remains current and exact proof is Completed, reconcile A and refresh lastUpdated atomically using the object-first helper. Never copy marker session ID over A.
- Active/Missing/Ambiguous/Unavailable retain A and block stale recovery with diagnostics. Fresh lastUpdated plus old timestamp is not proof.
- Local completed may pass an age-only case after enforcement, without claiming new primary persistence. Missing local turn may pass only after checking available session-level enforcement; unknown failed-build/audit state is not fabricated as clean.
- Test old/fresh/missing/invalid timestamps. Unknown age cannot prove completion. Lock acquisition consumes the hook deadline; cancellation leaves state unchanged.

Implement the bounded per-workspace/agent lock in the existing shared cache layer using one normalized key. Test real two-process begin-versus-Stop races. This is internal synchronization, not a second cache or public breaking change.

### Locked Import Recovery Design

Do not assume a canonical reader already exists: the inspected code has CanonicalSessionLogYamlWriter and a private DTO mapper, not a callable inverse. Add internal ICanonicalTranscriptRecoveryReader.ReadAsync(path, envelope, cancellationToken) in the transcript library. Return a validated TranscriptSession, diagnostics, and artifact SHA-256. Use YamlDotNet object deserialization with an explicit schema for the existing writer's sourceType/sessionId/nativeSessionId/model/workspace/provenance/turns fields. Preserve supported turn IDs, roles, native types, timestamps, text, and extension fields; report absent/unknown semantics rather than inventing them. Unsupported or lossy ambiguous input stays retained for review, not replayed.

Bounds: 8 MiB envelope, 256 MiB canonical artifact or individual source file, 2 GiB aggregate source bundle, 10,000 referenced files, two million records, 8 MiB source JSONL line, filesystem recursion depth 32, and YAML nesting depth 64. Reject duplicate YAML keys, aliases, custom tags, oversized inputs, and unsupported schema versions before persistence. Stream bounded reads; do not load an arbitrary file fully before checking its limit.

Artifacts must resolve under the owning workspace's .mcpServer/<agent>/transcripts/runs; sources must resolve under that workspace or explicitly configured provider transcript roots. Reuse and strengthen TranscriptPathSecurity to resolve every ancestor reparse/symlink component, apply directory-boundary and platform case semantics, and revalidate the opened target. An envelope cannot expand the allowlist. Reject artifact/envelope/source aliases to the same file or another agent/workspace. Validate the outer ingestion request before any pending replay.

Selection is deterministic: prefer a present valid canonical artifact; a missing artifact may fall back to the complete source bundle; an existing invalid/mismatched artifact is quarantined without silent fallback. Validate sessionId/sourceType against the envelope, workspace ownership, source set, and root ID. Existing TranscriptRunArtifactWriter.CreateArtifactIdentity hashes only source paths; preserve that legacy sourceHash field for compatibility but never treat it as content identity or durable-replay proof. New envelopes carry schemaVersion=2, canonicalArtifactSha256, bundleContentSha256, and normalizedPayloadSha256. Legacy envelopes missing these fields retain their original content and require successful identity checks plus reconstructed content hashes from verified sources or a supported explicit operator-approved legacy-recovery action. They are not silently trusted/discarded. Unverifiable legacy records remain open and retained.

Fallback sends all related files through ITranscriptBundleDetector and the existing source adapter as one bundle. Cline pairs, Copilot metadata/events, and subagents remain together. Preserve source session identity; never substitute the string recovery for missing IDs/hashes.

Keep public ITranscriptSessionPersister.PersistAsync returning Task<string> compatible. Add an internal recovery adapter implementing ITranscriptRecoveryPersister, returning TranscriptRecoveryPersistenceResult(Persisted, Degraded, ReceiptId, Identity, ContentVerified, Diagnostics). It calls the existing persister or, for canonical DTO mapping, the same ISessionLogService.SubmitAsync path, then reads the exact session via ISessionLogService.GetAsync. Persisted=true requires a positive sessionLogId receipt and verified expected turn/child content under the correct identity; degraded=false is explicit. Empty/malformed/unrelated/unverified string receipts cannot authorize deletion. No second store, public breaking API, transcript plugin endpoint, or compatibility reparse.

Use a per-workspace/agent/root-session recovery lock and atomic same-directory move from pending envelope to a uniquely named owned processing claim. Scan pending and abandoned processing claims. A crash releases the OS lock so the next owner can resume; never remove a live owner's claim. Stable replay identity is workspace + agent + sourceKind + native/root session + bundleContentSha256 + normalizedPayloadSha256 + ordered native request IDs, hashed with full SHA-256. Build bundleContentSha256 from a deterministic length-prefixed manifest of sorted allowed-root-relative canonical paths, byte lengths, and full SHA-256 of each file's actual bytes. Normalize separators and apply platform path identity consistently. Hash and parse the same bounded read-only snapshot; source mutation during snapshot produces a retained retryable diagnostic. normalizedPayloadSha256 hashes deterministic serialized normalized content excluding attempt timestamps and run IDs. New recovery filenames use rootId plus the content-based fingerprint, preventing changed bytes at unchanged paths from overwriting pending work. Legacy filenames are discovered unchanged. Reuse canonical upsert/additive deduplication and verify expected content so a crash after commit before cleanup is duplicate-free. Add named tests: unchanged content yields the same replay key; changed bytes at identical paths/record IDs yield a different key and cannot reuse an old receipt; partial retry does not duplicate children; simultaneous distinct content retains both envelopes.

Hold ownership while processing; compare claim identity/content hash before deleting only that owned claim after verified persistence. Newly written pending envelopes are never deleted with the old claim. Failure/cancellation retains a replayable claim with diagnostics. Continue independent valid envelopes after malformed ones, but propagate caller cancellation. Preserve canonical/source artifacts and other owners' lock/claim files.

### Locked Unique-Race Recovery Design

Target unique index IX_SessionLogTurns_SessionLogId_RequestId on SessionLogId/RequestId, declared by McpDbContext/provider migrations. No index-renaming migration.

Add a scoped classifier beside SessionLogService, using HandoffDbExceptions provider-inspection patterns without changing Handoff behavior. Walk inner exceptions with cycle protection. PostgreSQL requires SQLSTATE 23505 plus exact ConstraintName. SQL Server requires Number 2601/2627 plus the exact quoted known index/constraint token, not a generic UNIQUE substring. SQLite requires error code 19, extended code 2067, and the exact SessionLogTurns.SessionLogId/RequestId failed-column set. Also require DbUpdateException.Entries to identify the attempted added turn graph and an exact winning-row lookup. Unrecognized/localized text that cannot prove the index propagates as classified failure, not speculative retry.

Before mutation, snapshot preexisting tracked entry state/current/original values and identify entries changed by this upsert. Retain existing shared SaveChanges semantics. BEFORE the first SaveChangesAsync, if an ambient transaction supports savepoints, create a named savepoint and record that creation succeeded. Do not attempt savepoint creation after a failed statement. On a recognized race, roll back to that already-created savepoint before querying the winner or detaching/restoring entries. Without an ambient transaction, EF's failed implicit transaction must have rolled back before retry. If an ambient configuration lacks safe savepoints, mark in-context retry unavailable before the first save; on failure propagate a classified retryable error for caller-owned transaction rollback/full-command replay and retain the failsafe. Never query/retry inside a poisoned transaction. On success release the owned savepoint when supported, without committing or disposing the caller's transaction. Add an ordered-interceptor test asserting create-savepoint -> first-save-failure -> rollback-savepoint -> winner-read -> single-retry, plus no-savepoint and rollback-failure propagation tests. Test this explicitly, including SQL Server savepoint limitations.

Detach only the losing newly added turn/children; restore affected preexisting session summary fields to their pre-attempt snapshot without dropping unrelated pending edits. Never ChangeTracker.Clear. Reload the exact winner in the same workspace/session, apply existing omission-preserving scalar and stable child merge once, revalidate compliance/workspace stamps, then SaveChanges once more. A second failure propagates. Publish changes only after success. Cancellation/unrelated constraints do not enter retry. Unit tests prove tracker preservation; disposable-provider tests prove transaction recovery and duplicate-free content.

### Machine-Readable Unit Gate

Implement report plumbing as the first bounded P1 infrastructure-code increment, with red Build.Tests fixtures and the mandatory inter-phase review before changes. Add a --test-run-id Nuke parameter and deterministic result directories to Test and MigrationIntegrationTests. Test emits its selected-project inventory and per-project TRX using SetLoggers("trx;LogFileName=<project>.trx"); the provider target emits the same for its two selected projects. Do not silently alter exclusions.

Add tools/validation/Invoke-SessionLifeUnitGate.ps1 -RunId <id> as orchestration only: configure Pester NUnitXml at TestResults/<id>/pester/results.xml, invoke the whole Pester directory, Nuke Test with that run ID, and Build.Tests with Category!=Integration and a unique TRX. Consume Nuke's inventory rather than maintain a competing project list. Persist Pester native counters as structured data too. Every coding-slice exit uses this complete gate, not only P6.

A tested validator requires every expected fresh report under TestResults/<id>, matches run start and a source-file/tool-version manifest, rejects duplicate/missing projects, and fails on total=0, executed<total, failed>0, skipped>0, notExecuted>0, or Pester failed blocks/containers. Per-command exits remain mandatory. Stale reports cannot satisfy a new run. Provider paths: TestResults/<id>/provider/<project>/<project>.trx. Unit paths: TestResults/<id>/unit/<project>/<project>.trx. Pass one unique run ID explicitly per gate. Baseline console receipts remain historical until this plumbing proves the new gate.

### P0. Approve, Freeze, and Repair Traceability

1. Record approval on this plan TODO. Re-read active marker, skills, BDPv4, current branches, worktree status, and supported plugin operations. Check the live service; do not assume the September 24 SQL outage still describes today's host.
2. Snapshot HEAD/base SHA, tracked and untracked file list, file hashes, binary/tool versions, all 35 full TODOs, all governing FR/TR/TEST/AC and mapping responses. Store sanitized receipts under `docs/receipts/sessionlife-completion/<utc>/`.
3. Classify every changed file as direct scope, required regression dependency, or unrelated. Preserve unrelated changes in the original worktree and keep them out of the session-lifecycle integration commit. Four benchmark outputs are excluded unless separately justified; do not rewrite historical benchmark results to make tests green.
4. For memory-service/migration changes, identify their owning requirement and reproduce the dependency. Do not change previously applied migration history merely because a test fails. If needed, use a separately tracked prerequisite with fresh/predecessor database tests; a no-op Down is not evidence of a successful rollback. If the batch passes without a collateral change, leave it preserved but unmerged. Do not waive current failures as pre-existing debt.
5. Capture Section 4 AC and mappings through MCP, reconcile scoped TODO links/tasks, and preserve original descriptions and historical triage references. Store supersession rationale and query back exact records.
6. Create a per-ID acceptance manifest: governing FR, AC IDs, code paths, unit test fully-qualified names, integration scenarios, evidence paths, reviewer verdict, and unresolved work. All 35 start not accepted.
7. Create an isolated clean acceptance candidate from the recorded develop SHA plus only approved hunks/files, including needed untracked code/tests. Preserve the original dirty Grok worktree unchanged. Exclude unselected collateral changes and record an exact candidate file/hash manifest. All final tests/reviews use this candidate, not a superset with excluded fixes. Any later candidate change invalidates affected evidence even when develop did not advance.
8. Review the requirements/plan document changes with Astra xhigh. Exit only with complete scope/coverage and no contradictory contract. No production code before this gate.

### P1. Restore Baseline and Record Genuine Coverage Gaps

1. Preflight rg, pwsh, dotnet SDK, Pester, mcpserver-repl, plugin source roots, test-specific database prerequisites, and child PATH. Record resolved executable versions. Use existing build dependency targets, not improvised database repair.
2. Establish report plumbing through the mandatory per-increment gate, then run the complete unit inventory on the selected clean candidate. Preserve full logs and machine reports. Keep baseline failures distinct from new red assertions; all required failures must be resolved before leaving a coding slice.
3. Inventory existing lifecycle, metadata, quarantine, audit, transaction-bypass, service, controller, import, and plugin tests against every manifest row. Examine assertions and actual process/service paths. Reject tests that only echo fixtures or bypass the implementation under test.
4. Add only missing mock-backed consumer tests for the next small increment; prove boundary contracts with mocks, then show the new real-code assertion failing for the intended reason. Record names, expected/actual failure, and the production diff before Green.
5. Record missing historical inter-phase receipts honestly. Do not claim these reruns establish past chronology. Current acceptance requires new evidence, not altered old receipts.

### P2. Complete Cache, Identity, Metadata, and Outcome Contracts

Primary files: `plugins/core/lib-ps/repl-invoke.ps1`, `McpPluginShim.psm1`, `plugin-hook.ps1`, `resolve-cache-dir.ps1`, and the existing wrapper only if an output/exit contract test proves a defect. Tests: the four SessionLog*.Tests.ps1 files and existing turn-context/runtime suites.

1. Cover absent cache, degraded begin, same-request degraded retry, durable reopen, explicit metadata, canceled/cancelled supersede, and empty query handling through the real builder and shim.
2. Cover wrong-workspace marker, marker drift, inherited agent variables, session A/request R preservation, request mismatch, and native-to-plugin transitions.
3. Cover all session verbs across primary success, confirmed queued success, and neither-destination failure. Assert process exit, all receipt fields, retained file contents, and unchanged server state on failed writes.
4. Cover duplicate updates and additive replay. Use durable query as a later integration proof, not a mock claim of actual server persistence.
5. Include BUG-TRIAGE-246 proof tests for module-bootstrap, REPL user guide, and REPL message schema in this metadata slice. Correct contradictions before exit; if conforming, retain proof without edits. Do not defer these documents to P5.
6. Obtain red-test review, then fix failing behaviors with shared helpers; do not reimplement the correct complete-turn resolver. Refactor only when green.
7. Run the complete machine-readable cumulative unit gate above, including Build.Tests, with zero failures/skips, then independent code/doc review before the next slice.

### P3. Complete Stop Enforcement and Bounded Child Execution

1. Add failing tests for authoritative completed A versus active B, stale local A with server A completed, missing server turn, query failure, workspace mismatch, and both timestamp fields. Include local completed/missing-file cases without allowing them to erase available failed-build/audit evidence.
2. Move/check failed-build and incomplete-audit enforcement before an allow or recency mutation. Use the existing typed session client through the plugin bridge to query the exact identity; do not infer remote completion from local status or a different request ID.
3. Implement the exact-session typed GetAsync and shared proof/conditional-state transition defined above after red-test review. No alternate text search, pagination guess, second store, or raw REST agent call.
4. Test blocked stdin, nonclosing stdout, saturated stderr, cancellation, and child descendants. One remaining deadline covers all phases; kill/await the child tree on expiry and retain write-ahead data. Avoid increasing timeouts to hide hangs.
5. Keep 170's 120-second drain proof separate from the 30-second hook child contract. Test environment isolation so one setting does not contaminate another case.
6. Refactor and run the full current-plus-prior unit gate, then independent code review. No weakening of the original authoritative-proof AC.

### P4. Complete Replay Security, Atomicity, and Server Persistence

Primary files: `ImportRecoveryReplayer.cs`, `TranscriptIngestionService.cs`, `TranscriptRunArtifactWriter.cs`, canonical transcript path/bundle/persister components, `SessionLogService.cs`, `SessionLogController.cs`, and existing schema/error classifiers only where tests are red.

1. Quarantine: use sanitized fixtures corresponding to `20260722T232406Z-session_submit-3358.invalid-requestid-corrupted-shape.yaml`, `20260817T123057Z-session_submit-d5f9.yaml`, and `20260817T123219Z-session_submit-9ad7.yaml`. Convert numeric turns to an ordered list; preserve valid values; retain failures; delete only after primary success. Inspect real records before any approved replay and reconcile server duplicates first.
2. Import: validate the caller request before replay side effects; resolve workspace/agent recovery roots with the same security rules as normal ingestion. Validate each artifact/source path, symlink/reparse target, size, root/session identity, and provenance. Do not read arbitrary paths because an envelope names them.
3. Prefer valid canonical YAML using the supported canonical loader/DTO mapper. If unavailable, rediscover the entire source bundle using existing adapters, preserving paired metadata/messages and subagents. Do not normalize related files independently or invent recovery IDs/hashes.
4. Implement the locked canonical reader, recovery adapter, ownership protocol, and internal receipt above. Preserve the public string-returning persister contract. Test ambiguous/degraded/unverified receipts and explicit legacy-envelope retention.
5. Replay mixed valid/malformed bundles deterministically. Retain failed/canceled envelopes, isolate an individual corrupt envelope, avoid duplicate additive records on partial retry, and prevent deleting a concurrently changed recovery file. Preserve canonical/source artifacts.
6. Contention: test busy, locked, budget expiry, nested DbUpdateException, and connection outage through both drain entrypoints and the server classifier. Never set drain-completed after a transient abort or consume transient retry attempts as corruption attempts.
7. Server: test predecessor schema before save, classified dialog/submit errors, and exact same-turn uniqueness races on SQLite, SQL Server, and PostgreSQL using the existing provider abstractions. Retry once only for the intended constraint; unrelated uniqueness failures propagate. Verify merged children and other tracked changes are not lost by ChangeTracker.Clear.
8. Audit: failed dialog append leaves counters unchanged; successful exact-turn reconciliation uses persisted counts without removing decisions/actions from other turns.
9. Coordinator bypass: reuse existing tests and add only missing durable-query/repeated-complete assertions. Do not modify TransactionGatedSessionLogService unless a failing test proves session logging is actually gated.
10. Run full current-plus-prior unit scope and code review. Run provider/integration tests only after unit green; infrastructure failures leave the gate open and receive precise diagnostics, not skipped tests or migration workarounds.

### P5. Documentation and Shared Plugin Synchronization

1. Confirm the three metadata contract documents passed P2. Update remaining cross-surface documentation here without deferring those earlier corrections.
2. Document three outcomes, exact identity, deadlines, security/retention, retry classification, and manual recovery receipts. Do not document queued writes as primary-persisted.
3. Update prompt templates with the object-first helper. Regenerate marker/requirement projections through supported generation. Never clear read-only attributes to edit generated requirements or TODO.yaml.
4. After the clean candidate's cumulative unit and pre-activation code/doc review gates pass, perform one controlled development plugin activation: .\build.ps1 SyncAgentPlugins --agent-plugin-parent F:\GitHub. The target increments the common minor version AND refreshes installed caches. Plan approval includes this single in-scope development mutation, not repeated bumps, plugin-repo pushes, or production promotion. Preflight the eight repositories in Build.SyncAgentPlugins.cs, manifests, dirty scopes, and promotion guard. Zero/missing repositories is blocked, not staged-only success. Preserve unrelated plugin changes and stop on unsafe overlap.
5. Record the computed next version and before/after source/cache hashes. Freeze generated packages after this one invocation. Then run .\build.ps1 ValidatePluginPowerShellOnly --agent-plugin-parent F:\GitHub; assert all eight plugins plus the staged package were validated and run native suites. Do not invoke SyncAgentPlugins again in P6/P7. A source correction requiring another sync invalidates downstream receipts and starts an explicitly recorded new version/activation iteration.
6. Review generated code/docs/package diffs, test installed paths, and verify effective hooks separately. Cache refresh does not necessarily rebind running automatic hooks; schedule needed restart before claiming their validation. Record recoverable prior package/version paths. Failed activation remains failed and recovery uses supported plugin mechanisms, not arbitrary cache copying.

### P6. Full Validation and Independent Acceptance

Run from the selected clean acceptance candidate with the frozen generated plugin packages. Record exact command, cwd, source hash, tools, duration, exit, totals, failures, skips, and artifacts. Scope is cumulative; focused filters are not final gates. Commands below assume P1's reviewed report plumbing is implemented and each run ID is unique.

```powershell
.\build.ps1 Compile
.\tools\validation\Invoke-SessionLifeUnitGate.ps1 -RunId <unique-unit-run-id>
.\build.ps1 ValidateTraceability
.\build.ps1 ValidatePluginPowerShellOnly --agent-plugin-parent F:\GitHub
.\build.ps1 MigrationIntegrationTests --test-run-id <unique-provider-run-id>
dotnet test tests\McpServer.Support.Mcp.IntegrationTests\McpServer.Support.Mcp.IntegrationTests.csproj --logger 'trx;LogFileName=sessionlife-support-integration.trx'
dotnet test tests\McpServer.Repl.IntegrationTests\McpServer.Repl.IntegrationTests.csproj --logger 'trx;LogFileName=sessionlife-repl-integration.trx'
.\build.ps1 PluginSessionLogIntegration
```

Execute commands individually and stop progression on failure; a final command's exit 0 cannot hide an earlier failure. Parse machine reports for skipped/notExecuted/zero-discovery as well as failure counts. Build.Tests is explicit because the Nuke unit target excludes it. Provider integration is mandatory when retaining any storage/migration change; the session race/provider cases are mandatory regardless. Use disposable test databases, never a destructive reset of the shared service database.

The integration matrix must cover:

- Each registered plugin's applicable native workflow, with aiUnit Theory inventory coverage and actual supported wrappers/hooks. Missing agents, auth, balance, or executables are blockers, not passes. Preflight Grok balance before expensive runs.
- Begin -> title/update -> dialog/actions -> complete -> exact persisted read, including all four runtime session-header fields and planFile/todoId.
- Degraded begin/write, primary unavailable plus failsafe available, both unavailable, restart/rebind, duplicate drain, and complete-after-server-already-completed.
- Two concurrent agent sessions in the same workspace, different workspaces, and wrong inherited agent variables. No cross-turn/cache contamination.
- Malformed/exhausted quarantine, canonical-only import recovery, paired source bundles, missing source, traversal/reparse escape, cancellation, concurrent replay, and no unintended artifact deletion.
- SQLite/SQL Server/PostgreSQL same-turn race, predecessor schema, retry budget, error classification, and existing coordinator bypass. Do not require reapproval of already-approved migrations; investigate a current failing gate and fix only a demonstrated defect in approved scope.

After unit green, implement any still-missing integration scenarios through BDPv4 and rerun affected cumulative gates. Obtain human-guided sampling of at least Codex, Claude, and Grok user-facing session completion/recovery. Record any unavailable sampling as unfinished.

Independent Astra xhigh reviews the complete selected patch, requirements/AC manifest, docs, and exact receipts. Preflight its PowerShell.Mcp and plugin access before launch. Record request/response JSONL under `docs/receipts/hv/<utc>-sessionlife-*`. Give it read-only code access, not permission to close tasks or change requirements.

Store the full review body, findings, scores, verdict object, artifact paths, and source identity in a dedicated MCP review turn. Query back the exact session/request and verify the entire body, not a one-line summary. A retained failsafe is not durable primary review evidence. Reconcile the historical wrong-session receipt without rewriting agent identity or pretending its old review passed.

Acceptance requires AGREE, accuracy >=98, completeness >=98, zero applicable FAIL/UNKNOWN, and durable full-review proof. These are thresholds, not targets for inflated scores. New code/doc fixes after review invalidate the affected approval and require tests plus re-review. A service/logging outage prevents closure, not preservation of work or reporting status.

### P7. Integrate, Deploy Development, and Close Individually

1. Present the exact reviewed file/commit scope and preserved unrelated changes. Follow the commit-sync approval contract before committing/pushing. Do not sweep all dirty files or MCP failure diagnostics into a commit. Preserve useful sanitized validation evidence.
2. Integrate the exact approved candidate into current develop using the normal non-destructive Git workflow. Verify integrated source/generated-plugin hashes against the accepted manifest regardless of whether develop advanced. Run full cumulative gates on the final integration checkout before deployment. Different files/base, conflicts, or omitted dependencies invalidate affected receipts and require re-review. Do not merge to main or push unrelated repos under this plan alone.
3. Deploy development from that verified checkout using Nuke UpdateService --skip-version-bump unless a release bump is separately requested. Nuke owns installation/restart; no manual binary copy. Record publish input/source manifest, deployed hash/version, and live marker identity. Do not re-run plugin sync in this deployment; P5 already performed that separate mutating target.
4. Verify workspace enumeration, service health, session-log operations, and installed plugin hashes/hook binding against the deployed build. Execute the accepted degraded/replay/Stop scenarios in a scoped test workspace. A healthy /health response alone is not product validation.
5. Stage/production promotion requires explicit approval and existing pipelines. This batch's proposed release scope is develop plus development validation; any broader requirement remains open until its additional environment obligations are met.
6. For each of the 35 IDs, populate implementation tasks, remaining work, requirement links, and a doneSummary citing the exact AC, tests, reviewed source/commit, integration/deployment receipt, and durable review turn. Change done only after that item's entire acceptance is proven; do not bulk-close by group membership or pass totals.
7. Query all 35 again. Compare expected/actual done states, task states, requirement links, and summaries. Mark scoped AC satisfied only with their evidence; do not blanket-complete broader FRs or unrelated plans.
8. Complete the parent plan only after all 35 are individually accepted and integrated, required development validation is recorded, applicable failsafe/review evidence is reconciled, and there are no unreported gaps. Otherwise leave it open with an exact remaining list.

## 6. Deliverables and Stop Conditions

- One versioned acceptance manifest covering all 35 IDs with no missing or duplicate scope entries.
- Live structured AC and mapping receipts, including explicit treatment of existing FRs and supersessions.
- Selected patch manifest separating direct fixes, approved prerequisites, and preserved unrelated work.
- Real Red/Green/Refactor receipts for new remediation; historical evidence clearly labeled as historical.
- Full unit, provider, plugin, and integration results with zero failures/skips and nonzero discovered tests.
- Independent code/doc review request/response streams and full durable review turn.
- Source-to-installed-plugin/deployed-service evidence, exact-turn readbacks, and per-TODO closure receipts.

Stop the affected phase on unapproved behavior changes, ambiguous ownership, unavailable required model/provider, lost audit identity, failing/skipped/unrun required tests, incomplete review persistence, or unreviewed patch drift. Preserve work and report the concrete blocker. Do not equate infrastructure failure with a product defect, but do not call the blocked validation complete.

Current action: execute revision 4 through bounded BDPv4 gates. No bug or implementation gate is yet accepted.

## 7. Approved Revision 4 Execution Amendments

Approval: operator instruction "Implement the proposed plan." received 2026-09-28 UTC. Revision 3 baseline SHA-256: CFDDF01564C13F2322D42B174FFB996A3CCFBFEDBDA804FE236A2DC3E40F0CB7. These amendments take precedence over conflicting earlier text.

### Authority and Scope

Approval of this plan is the only approval required for its listed actions: requirements capture, implementation, one plugin synchronization, scoped commit/integration into develop, development deployment, and evidence-backed TODO closure. No repeated approval prompts. Main, staging, production, unrelated changes, and plugin-repository pushes remain outside scope.

Use gpt-5.6-sol high for coding and independent gpt-6-astra xhigh for code/document reviews. Do not review the operator's requests or standalone operational actions. Do not substitute unavailable requested models silently.

### Baseline and Traceability

- Live verification at 2026-09-28T00:05:41Z found all 35 bugs open. The subsequent effective-requirements query found five SESSIONLIFE FRs, five TRs, and five TEST records, all pending with zero structured AC. Both branches remained based on 6a565d8762072ed040469047791a57c80dbf1e88.
- The previously cited planning-snapshot path is absent. Treat unavailable receipts as missing evidence, not verified artifacts. Historical test runs remain historical.
- Preserve the exact 35-item batch in Section 3. Capture Section 4 structured AC through supported MCP operations without replacing existing requirements or historical links.
- Assign each TODO exactly one governing FR in the acceptance manifest. Preserve other links as provenance or dependencies. Do not invent a governing-link field in existing flat DTO requirement arrays.
- Reconcile partial supersession at AC level: record old criterion, replacement criterion, affected behavior, rationale, and retained obligations. Preserve many-to-many TR/TEST mappings. Do not complete broad legacy FRs from only the session-lifecycle subset.
- Before production edits, populate every AC-to-test entry with TODO IDs, production surface, existing or planned concrete test names, mock boundary, real-code assertion, integration scenario, and acceptance state. Unknown coverage must be explicit and resolved before that slice starts.
- Maintain a decision register with decision ID, chosen behavior, rationale, owner, evidence, and revisit trigger. Provider coverage, canonical compatibility, deadlines, synchronization scope, and migration dependencies are explicit decisions.

### Evidence and Gate Rules

- Create one versioned acceptance index under docs/receipts/sessionlife-completion/<runId>/. Include sanitized baseline queries, commands, source references, changed-file classifications, AC/test mappings, report locations, review linkage, and blockers.
- Every run records UTC start/end, cwd, base/head SHA, candidate content-manifest hash, tool versions, command/filter, expected test inventory, report hashes, exit codes, and parsed counts. Validate source identity before and after execution; changed inputs invalidate the run.
- Commit sanitized plans, manifests, machine reports, and review receipts with scoped changes. Sensitive originals stay outside Git in workspace-owned MCP storage with access restrictions and indexed hashes. Retain acceptance evidence until explicitly authorized cleanup. Hashes without accessible evidence are insufficient.
- Use repository-relative evidence paths, not temporary worktree paths. Verify references after integration and before cleanup. Preserve full review findings/verdicts in MCP; redact credentials without removing substantive findings.
- States: not-started, red-proven, implemented, unit-verified, integration-verified, reviewed, accepted. Blocked records identify interrupted state and missing prerequisite. Proof-only cases may bypass red-proven with explicit rationale. Only accepted permits TODO closure.
- Reproducible defects require mock-backed consumer validation, real-code Red, minimum Green, and Refactor. Existing correct behavior receives green proof. Prose-only corrections use executable consistency checks where practical and exact document review otherwise. Security reproductions use isolated fixtures.
- Product failures require remediation. Infrastructure failures block dependent validation without establishing product defects. Unrelated failures must be reproduced against baseline and resolved through demonstrated prerequisites or remain blocking. No debt waivers.
- Every coding-slice exit requires the full cumulative unit inventory, including Build.Tests: zero failures, skips, missing reports, or zero-discovery projects. Measure duration; expense does not reduce scope.
- Independent reviews occur after requirements, new Red tests, Green changes, and before acceptance. Require AGREE, both scores at least 98, no applicable FAIL/UNKNOWN, complete JSONL receipts, and full server-persisted review evidence.

### Ordered Implementation Clarifications

P0: Preserve the original dirty Grok worktree. Classify each change as direct, proven prerequisite, or unrelated. Create an isolated candidate containing selected changes only. Capture requirements, mappings, complete acceptance index, and baseline evidence. Existing migration changes require demonstrated dependency; do not reopen approved migrations merely because infrastructure failed.

P1: Add tested Nuke run-ID, selected-project inventory, and per-project TRX output. Add cumulative Pester/Nuke/Build.Tests orchestration and validation rejecting stale, missing, duplicate, skipped, or empty reports. Record mocks and real Red before implementation.

P2: Validate metadata precedence, immutable session/request binding, degraded preservation, omission behavior, and primary/queued/lost outcomes through real builders/wrappers. Include the three BUG-TRIAGE-246 documents here. Preserve runtime header fields and existing public compatibility.

P3: Add proposed SessionLogClient.GetAsync for the existing endpoint and the internal exact-turn proof helper. Enforce build/audit checks first; reconcile only one exact remotely completed turn using lock/snapshot/compare. Duplicate IDs remain ambiguous because the model declares uniqueness. Keep the 30-second default and existing positive timeout overrides. Include serialization, startup, pipe I/O, waits, and cleanup in one monotonic budget, with cleanup time reserved inside it. Drain stdout/stderr concurrently, inject short deadlines in tests, and report unconfirmed termination as failure. Keep the separate 120-second drain contract.

P4: Implement proposed canonical reader and internal verified-persistence adapter while preserving public Task<string> persister. Retain Section 5 bounds, ancestor-link containment, content fingerprints, whole-bundle reconstruction, owned claims, crash recovery, and verification-before-deletion. Unsupported/lossy canonical data remains retained. Use provider-specific uniqueness classification and pre-save savepoints; preserve unrelated tracked state and retry at most once. No transcript ingestion in plugins.

P5: Complete docs and supported generated projections. After pre-activation gates, run SyncAgentPlugins --agent-plugin-parent F:\GitHub once across all eight repositories. Record every generated/package/cache mutation. Create a new post-sync candidate manifest with generated changes and external package hashes; rerun validation/review against that state. A corrective resync starts a new activation iteration and invalidates dependent evidence.

P6: Run Compile, cumulative units, ValidateTraceability, ValidatePluginPowerShellOnly, MigrationIntegrationTests, Support.Mcp integration, REPL integration, and PluginSessionLogIntegration. SQLite, SQL Server, and PostgreSQL are mandatory for provider-sensitive behavior. Existing GitHub CI runs the unit target, not provider gates. Obtain provider receipts using disposable databases and supported dependency provisioning.

P7: Commit only reviewed scoped changes and sanitized evidence; integrate into develop without sweeping unrelated files. Rerun full gates on the integrated checkout regardless of base drift. Deploy with UpdateService --skip-version-bump and no second sync. Verify deployed identity, workspace enumeration, persistence, recovery, and effective hooks. Close each bug against its own evidence; parent completes only when all 35 qualify.

### Preflight, Sampling, and Completion

- Missing coding/review models block their assignments. Untrusted/unavailable MCP blocks authoritative writes and acceptance. Preserve work in supported failsafes without claiming queued evidence durable.
- Missing provider prerequisites block provider validation and affected acceptance. Missing plugin repositories/unsafe overlap block synchronization. Missing executables, authentication, or balance block the affected agent's claims and therefore batch completion. No fabricated passes or skips.
- Independent diagnostic commands may run after failure, labeled diagnostic; dependent phases do not advance until their gates pass.
- Human-guided Codex/Claude/Grok sampling uses isolated registered workspaces and frozen plugins: begin, update, dialog/actions, complete, exact readback, controlled outage/recovery, stale-pin reconciliation. Verify all headers, identity isolation, failsafe retention/deletion, and automatic hook binding. Manual wrappers do not prove refreshed automatic hooks.
- Concrete helper names, serializer, and lock choices are intended design. Security, compatibility, ownership, and durability invariants are mandatory. Equivalent internal design requires recorded design/code review; changed behavior or scope requires a plan amendment.
