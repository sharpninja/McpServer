# BUG-TRIAGE-139 fifteenth remediation receipt

## Outcome

All seven findings in the fifteenth independent review were remediated and validated on `codex/bug-triage-139-remediation`. This is a new remediation candidate, not approval. BUG-TRIAGE-139 remains open pending another independent adversarial review. No merge or push was performed.

- Approved comparison base: `210cb223d81dac6b4045b868e4d7b2712d08b752`.
- Rejected candidate: `f7d8a07f9a754321a5523d749bf5e713a74ab9f6`.
- Fifteenth verdict: **NOT APPROVED**.
- Authoritative review copy: `docs/receipts/artifacts/BUG-TRIAGE-139/reviews/fifteenth-independent-review.txt`.
- Review SHA-256: `D2227DB3D7CA7B003965FA84EC6D9F1F07EF0F87940CFA0EF5849C085A504AB1`.
- Evidence: `docs/receipts/artifacts/BUG-TRIAGE-139/fifteenth-remediation`.
- Validated implementation tree before evidence-only commits: `db0e6950de907cc9008b24c689210a864d90295d`.
- Initial evidence assembly commit: `f81ac5b0587e6f48774757dadcb0298f7b945370`.
- Final integrity update: the commit containing this receipt revision; its SHA is reported in the handoff to avoid a self-referential hash.

The fourteenth receipt is preserved as history with a prominent supersession notice. The fifteenth reviewer’s deterministic failures remain recorded as failed baselines: external **13/20** and Agent Help **8/18**, both with zero skips. They are not overwritten or restated as passes.

## Validated BDPv4 increments

- `65fcf257d6e02e24dea681228ea5131fd50aa367` — preserved consumer, real-service, platform-native, deterministic-harness, and exact-exception RED evidence before production edits.
- `10f45b46902985566bdc81a4f353d9bfcd2d97d8` — restored Linux directories with owner read/write/search mode.
- `9dc85cbeec1963cf6d7a05ef91e04188f0f768b9` — bounded cross-platform physical workspace resolution and closed the Windows pre-open cancellation gap.
- `37c36f6087373bdfb4960c2a05fe08136ea155dc` — bounded cross-platform SQLite pre-handle opening and exact cancellation attribution.
- `fea1ef72c236a7d7f8b9b62344fc1ab35e61b17b` — deterministic repository anchors and external Agent Help fixtures.
- `caf5a61f797766175b931dde5952615160c35516` — strengthened platform/native race probes without silent platform passes.
- `db0e6950de907cc9008b24c689210a864d90295d` — normalized retained RED text evidence to repository EOL requirements.

Each increment has one adjacent parseable `AI-Signature` / `AI-Confidence` trailer pair. The historical rewrite map proves that the earlier local, unpushed versions and these formal-trailer versions have identical tree objects.

## Fifteenth finding resolution

1. **Linux rollback directory usability.** `WorkspaceContainedFileSystem` now passes `UnixOwnerReadWriteExecute = 0x1C0` (octal 0700) to each Linux `mkdirat` restoration step. `ContainedRestoration_NestedDirectoriesRestoreContentsModeAndContainment` creates nested directories and a file through the real service boundary, reads back exact content, asserts owner rwx bits, then proves a symbolic-link escape cannot create the external leaf. It no longer catches every I/O/authorization failure. The focused Windows scope passed 3/3, the Ubuntu native scope passed 19/19, and the non-root Ubuntu service test passed 1/1.

2. **Bounded physical workspace identity.** `BoundedFileSystemPolicy` classifies Linux mounts from `/proc/self/mountinfo` without probing the candidate mount and rejects FUSE, network, automount, and special filesystems. Supported Unix resolution runs `realpath` in a killable child process; cancellation kills and drains it before return, so no blocked in-process worker is abandoned. Windows resolution repeatedly issues `CancelSynchronousIo` until its named native worker completes, including cancellation after the last check but before `CreateFile`, and joins before returning. The real Windows cancellation-gap hook and real Linux hanging-FUSE branch both ran in the 19/19 platform gates; the current-plus-prior affected scope passed 37/37 with zero active workers.

3. **Cross-platform bounded SQLite opening.** Before starting an opener worker, `SqliteBoundedConnectionOpener` rejects data sources on Linux filesystems whose native I/O cannot be bounded. Safe local opens retain the finite caller timeout, repeatedly interrupt native work, close an interrupted handle, and join the single worker before completion. The Linux FUSE test proves rejection occurs before worker/handle creation; the Windows test exercises the real pre-handle kernel wait. The prerequisite record proves `/dev/fuse`, GCC, FUSE headers, and `fusermount3` were present, so the Linux native branch was applicable rather than silently bypassed. Windows and Linux native gates passed 19/19.

4. **Precise cancellation classification.** An issued cancellation request alone no longer changes an independently thrown `IOException`, `ObjectDisposedException`, or provider failure. Translation requires a precisely attributable outcome: SQLite interrupt plus an issued handle interrupt, the matching caller token, a Windows operation-aborted outcome after synchronous-I/O cancellation, or the managed wait interruption actually issued by this opener. The real named-pipe kernel-wait coincidence test now requires the exact `ObjectDisposedException` and message; close/dispose races require exact outcomes, closed state, null handle, and zero workers. Focused SQLite passed 8/8; the SQLite/LocalDB/PostgreSQL affected-provider scope passed 42/42.

5. **Deterministic external root and fixtures.** The test project computes an MSBuild repository root before path mapping, emits it as `AssemblyMetadata("McpRepositoryRoot", ...)`, and copies Agent Help fixtures beside the assembly. Root and fixture consumers prefer those deterministic artifacts instead of compiler `CallerFilePath`. A warnings-as-errors Release build with `ContinuousIntegrationBuild=true`, no `MCP_REPOSITORY_ROOT`, and C:-only artifacts succeeded. The resulting executable passed the exact external seven-class scope 20/20 and Agent Help 18/18.

6. **Substantive platform coverage.** `WorkspaceIdentityFourteenthNativeTests` no longer returns silently on non-Windows. Windows and Linux execute applicable native assertions; unsupported platforms execute an explicit bounded-strategy contract assertion. The fifteenth harness enforces that structure. The Ubuntu native 19/19, non-root 1/1, Windows timing 19/19, and harness 4/4 gates all passed with zero skips.

7. **Receipt and evidence accuracy.** The fourteenth receipt now says exactly which rejected claims are superseded and records the reviewer’s 13/20 and 8/18 deterministic results. The new evidence retains every material RED, failed, setup, timeout/inconclusive, invalidated, and replacement result; `command-output-map.tsv` maps 82/82 raw outputs exactly once; all 27 TRX files are parsed; and the final SHA-256 inventory is independently rehashed.

## Unicode-dash migration consequence

The raw-dash candidate `4e0751530fdb6035a8195bfd411260260002ef48` was checked against all local heads, remote refs, and tags. It is reachable only from the local remediation branch, which has no upstream; it is not an ancestor of `origin/develop` or `origin/main`. Retained command evidence contains no push, deployment, service update, or installer command for that candidate, and provider tests used isolated ephemeral stores with teardown. Within the audited repository/ref/deployment evidence, no durable store was exposed to the rejected raw-dash implementation, so no repair migration is required. The audit explicitly does not claim knowledge of unrecorded external systems; if such a deployment is later identified, repair must be collision-detecting and must not silently merge identities.

The canonical persisted-value implementation remains covered across SQLite, LocalDB, and PostgreSQL. Hashes are computed from the exact final ASCII-dash persisted workspace/request values and canonical replay identity remains passing.

## Ten prior findings re-review

1. **Resolved.** Handle-relative/pinned creation, write, replacement, and rollback deletion remain intact on Windows and Linux. The new Linux 0700 restoration fix closes the fifteenth defect; nested content/mode and containment tests pass.
2. **Resolved.** Unicode dashes normalize before persistence and hashing across providers; the deployment audit above explains why no historical repair is in scope.
3. **Resolved.** Physical identity now uses a killable Unix helper after mount-policy classification and repeated Windows native cancellation across the pre-open gap; Windows/Linux native and 37-test affected gates pass.
4. **Resolved and retained.** Managed, EF, connection-string, native busy-timeout, and custom busy-handler state remain exact when intentionally different; affected providers pass 42/42.
5. **Resolved.** SQLite opening rejects unbounded Linux filesystems before worker/handle creation and preserves bounded Windows kernel-wait cancellation; native gates pass on both platforms.
6. **Resolved.** SQLite, SQL Server, and PostgreSQL classification tests preserve unrelated provider failures; the new real kernel-wait coincidence test preserves the exact lifecycle exception.
7. **Resolved and retained.** Linux snapshot capture still uses no-follow/nonblocking/type enforcement; the no-writer FIFO contract remains in the 19-test native gate.
8. **Resolved.** The deterministic no-environment external replacement passes 20/20 and Agent Help passes 18/18 under `ContinuousIntegrationBuild=true`.
9. **Resolved and retained.** The full process tree is killed and stdout/stderr drains share the parent deadline; the real descendant-held-pipe/quick-process harness remains green in the 4/4 harness and 18/18 repository/harness scope.
10. **Resolved.** Historical overclaims are visibly superseded; exact commands, classifications, mappings, TRX counters, hashes, and independent verification are retained in the fifteenth evidence set.

## RED to GREEN record

The committed pre-production RED slice includes two zero-test setup failures and behavioral Windows 12/7, refined 9/10, final 13/6, Linux 10/9, and non-root Linux 0/1 runs. Later implementation/refactor iterations remain under `final/failed`: identity 5/6, 5/6, minimum 26/31, minimum 37/38; SQLite 4/8, 7/8, 3/8; native Linux 18/19; native Windows 14/19; non-root setup 0 tests; one invalidated deterministic build; the 1,200-second NUKE telemetry timeout; four incomplete/failed NUKE Test attempts; and one malformed cleanup probe. None is counted as passed.

The accepted replacements are exact in `final/command-output-map.tsv`. Every accepted test has zero failures, skips/not-executed, aborts, and inconclusive outcomes.

## Final validation ledger

Counts overlap and are not summed.

- Native: Windows 19/19; Ubuntu 19/19; Ubuntu non-root restoration 1/1.
- Focused containment/identity/SQLite: 3/3, 6/6, 8/8.
- Deterministic external: 20/20; Agent Help: 18/18; fifteenth harness: 4/4.
- Current-plus-prior identity: 37/37.
- Affected SQLite, LocalDB, and healthy ephemeral PostgreSQL providers: 42/42.
- Marker/client/REST-MCP/requirements-wiki/workspace-provider: 24/24, 30/30, 17/17, 74/74, 16/16.
- Repository/harness/EOL: 18/18 before evidence assembly and 18/18 at `f81ac5b0587e6f48774757dadcb0298f7b945370`; formal history/EOL: 5/5 after trailer normalization.
- Full BDPv4 unit target: 3,331/3,331 across seven assemblies.
- `build.ps1 Compile`, `build.ps1 Test`, and `build.ps1 ValidateTraceability`: passed.
- Release matrix: restore plus 12/12 current projects, `ContinuousIntegrationBuild=true`, warnings as errors, zero warnings/errors.
- Repository/harness/EOL audits and evidence mapping/inventory checks: passed.
- Plugin synchronization: not run because the changed scope has no plugin implementation or generated plugin output.

All generated build/test/database/harness runtime artifacts were physically on C: (including the Ubuntu VHD). The hermetic gates did not use `MCP_REPOSITORY_ROOT`. The SQL Server historical migration allowance remains test-only, finite at 300 seconds, and unchanged. No production timeout or assertion was weakened.

## Remaining work

- A fresh independent review of the new candidate.
- BUG-TRIAGE-139 remains open with `done=false`.
