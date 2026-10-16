<!-- SPDX-License-Identifier: Apache-2.0 -->
# Handoff: federation fixes (branch claude/federation-fixes-20261007), 2026-10-16T12:14:00 local

Prepared by ClaudeCode (model claude-opus-5-5) for Payton Byrd at the operator's request "write a new, definitive handoff for the current federation state." All times are local US/Central (-05:00) unless marked Z. Facts marked "measured" were read from the live system between 12:05 and 12:14 today; facts marked "earlier" come from this session's work on 2026-10-06 to 2026-10-08 and were not re-measured for this file.

This is the authoritative state of the federation workstream. The plan `docs/plans/2026-10-07-federation-fixes.md` remains the design authority; this file records where execution stands against it. Receipts are in `docs/receipts/federation-fixes-20261007/` (00 to 07).

Copy everything below the line into the next agent.

---

## 0. Status in brief (read first)

- Phase P1 (path handling and forward-slash workspace keys) is partly delivered. P1.1 to P1.3, the P1.R test retrofit, and P1.4a to P1.4d are committed. P1.4 has never passed its three-suite gate. P1.5 (the rekey command and startup guard) has not started. Phases P2, P3 and P4 have not started.
- The branch was rebased onto `origin/develop` today (12:06). Because the standing rule is "Never force-push", the operator chose (12:16) to publish the rebased history on a new branch, `claude/federation-fixes-20261016-rebased`, which is now the working branch. `origin/claude/federation-fixes-20261007` is left at the pre-rebase `372c991a` as history (section 2).
- Do not deploy this branch to any server whose database still holds backslash workspace ids. Without P1.5, rows keyed `C:\...` stop matching the canonical `C:/...` keys this build reads and writes (decision D7: no dual-key fallback).
- The local McpServer on PAYTON-LEGION2 runs Standalone ("Federation disabled", measured 12:12 via `GET /health`). It was reverted from LocalProxy on 2026-10-06 (earlier) because federation does not backfill or address proxy data correctly.
- MCP session turns 006, 007 and 008 of session `ClaudeCode-20261007T135500Z-federation-fixes` are open (in_progress).

## 1. Starting a session on this workstream

1. Run `/add-profile` and read the operator profile in full first.
2. Read `F:\GitHub\McpServer\AGENTS-README-FIRST.yaml` in full (marker at baseUrl `http://PAYTON-LEGION2:7147`, workspace `F:\GitHub\McpServer`), verify `/health` with a nonce, review session history, open a turn.
3. Work in the worktree `F:\GitHub\McpServer\.worktrees\federation-fixes-20261007`. Use pwsh. Stage by explicit path only (section 2 lists what must stay out of commits). Never force-push. Never move files (copy, verify, keep the original). Never read or write TODO or session-log storage files directly; use MCP.
4. Byrd Development Process v4 (MEMORY-PROCEDURE-002): tests from TEST ids, green against MockData fixtures before production code, then Real fixtures, refactor, baseline-relative full gate, receipts. Red-first is not acceptable.
5. Hostile validation: the reviewer must not be the requester's agent family. MEMORY-PREFERENCE-005 (2026-10-08) says to use Codex `gpt-6.1-sol` at effort high while Grok is unavailable; the operator profile (2026-10-13) says to ask which agent and model to use when none is named in the session. Ask.

## 2. Git state (measured 12:14)

- Worktree `F:\GitHub\McpServer\.worktrees\federation-fixes-20261007` (the folder name is historical), working branch `claude/federation-fixes-20261016-rebased`, upstream `origin/claude/federation-fixes-20261016-rebased`. The local branch `claude/federation-fixes-20261007` also points at the rebased commits and has diverged from its upstream; do not push it.
- Local branch after rebase (base `99a2a840`, "Merge main into develop (sync)", origin/develop at 2026-10-16T10:19:29):
  - `13da798d` fix(federation): interpret request workspace paths by their own syntax (P1.1 to P1.3; was `8ca38d5d`)
  - `4f72c0f6` test(federation): prove P1.1-P1.3 path tests against mocked data (P1.R; was `982201f9`)
  - `c0e4146f` feat(federation): forward-slash workspace keys at the storage boundary (P1.4a-d checkpoint; was `372c991a`)
  - plus the commit that adds this handoff.
- `origin/claude/federation-fixes-20261007` is at `372c991a` (pre-rebase). Local backup branch `backup/federation-fixes-20261007-pre-rebase-20261016` also points at `372c991a`. Keep it until the operator clears it.
- The rebase applied cleanly. The two files changed on both sides (`src/QBrainAi.Services/Services/RepoFileService.cs`, `tests/QBrainAi.Support.Mcp.Tests/Services/TriageServiceTests.cs`) merged without conflicts.
- Stash: `stash@{0}` "autostash" (created by the rebase) holds only the four test-rewritten benchmark files under `docs/benchmarks/results/`. Its reapply conflicted with develop's versions, so those files were restored to HEAD; the stash was kept. It is safe to drop only with operator direction. `stash@{1}` and `stash@{2}` belong to other work; never touch them.
- Never commit: `docs/benchmarks/results/memory-bench-*` (rewritten by every test run), `artifacts-local/` (local logs and TRX files). Other agents' `docs/receipts/hv/*` files have appeared in this tree before; keep them out.
- Probe worktree `F:\GitHub\McpServer\.worktrees\fed-p14a-probe` (detached at `982201f9`, with uncommitted copies of the P1.4 changes) is a throwaway used to find affected tests. Remove it once P1.4 passes its gate (`git worktree remove`), after confirming with the operator. Its `artifacts-local/trx/probe*.trx` files are the only record of the probe runs.

## 3. What is delivered

### 3.1 P1.1 to P1.3 and P1.R (earlier, receipts 01 to 06)

- `IWorkspacePathNormalizer` and `IWorkspaceHostEnvironment` (QBrainAi.Client) interpret a request path by its own syntax rather than by the host OS, so the Linux hub no longer turns `C:\Users\kingd` into `/opt/mcpserver/app/C:\Users\kingd` (root cause was `TriageService.cs` calling `Path.GetFullPath`).
- About 20 request-path resolvers route through the normalizer (TriageService, WorkspaceService, HandoffWorkspacePaths, WorkspaceTokenService, TodoServiceResolver, TodoServiceFactory, EfTodoService, TodoExecutionService, RequirementsDatabaseDocumentService, HostileReviewService, AgentPoolService, FileGitHubWorkspaceTokenStore, SessionLogWorkspaceAttributionValidator, WorkspaceContext derived paths, RepoFileService foreign-platform guard).
- `WorkspacePathGetFullPathRatchetContractTests`: identity files have zero `Path.GetFullPath(` calls; all other files have reviewed allowlist counts that may only go down.
- P1.R retrofitted these tests to MockData-then-Real fixtures (shared scripted fake `ScriptedWorkspacePathNormalizer`, simulated Linux and Windows hosts). P1.R gate passed baseline-relative on 2026-10-07 (receipt 06).

### 3.2 P1.4a to P1.4d (committed as a checkpoint; receipt 07-p1.4-checkpoint.txt)

- P1.4a lexical form: Windows paths are stored and returned with `/` (`C:/x`, `//srv/share/x`, storage key `windows:C:/USERS/KINGD`); Win32 device prefixes are stripped; unrooted values such as opaque ids (`ws:\test`) and relative values keep their spelling.
- P1.4a database boundary: `WorkspaceIdentity.CanonicalizeWorkspaceId` defines the persisted key. `McpDbContext` canonicalizes its discriminator. `src/QBrainAi.Storage/WorkspaceKeyConversion.cs` applies an EF value converter and a canonicalizing value comparer to every `WorkspaceId`, `OwnerWorkspaceId` and `WorkspacePath` property, so a predicate built from a raw caller path matches the canonical row, and rewrites tracked values in place before save. In-memory C# comparisons that EF cannot reach are canonicalized at their entry points (memory promote and consolidate, federation memory ownership, `EnsureWorkspaceRows`, `SessionLogService.ResolveWorkspaceId`, product caller and member ids).
- P1.4b: remote UNC detection, containment and the `//localhost/C$` admin-share alias accept the `/` form.
- P1.4c: public `WorkspaceIdentityPath.ToNativePath(path, platform)` for Win32 boundaries (explicit platform because `//srv/share` is also a valid POSIX path).
- P1.4d: ToolRegistryService, AgentService, the WorkspaceService RepoRoot workspace and the WorkspaceServiceAccessor default route through the normalizer; the ratchet moved three files to the zero-call list and WorkspaceService from 4 to 2.
- Test support: `tests/QBrainAi.Support.Mcp.Tests/ExpectedWorkspaceKey.cs` is the test-side oracle for the canonical form (backslash to `/` for drive and UNC paths only; it does not call production code). About 70 existing assertions use it.
- Evidence before the rebase (earlier, 2026-10-08): MockData 131 passed, 1 skipped, of 132; Real 129 passed, 1 skipped, of 130 (the skip is a by-design non-Windows-host case). Probe full run: 3251 total, 17 failed (13 old baseline, 2 SQL Server timeout flake that passed in isolation, 2 stale AgentServiceRuntimeTests expectations fixed afterwards). Client.Tests 304/304 in the probe. Build.Tests not run for P1.4.

## 4. State after today's rebase (measured 12:08 to 12:13)

- Restore of the rebased tree fails with NU1902/NU1903 (NuGet vulnerability audit treated as errors) for `System.Security.Cryptography.Pkcs` 9.0.10 and 10.0.10 (GHSA-2fmv-rv2c-59x7 moderate, GHSA-846f-f764-76mx high). The branch changes no project or package files, so this comes from develop's dependency graph and new advisories; it will affect any build of develop. Local verification used the command-line override `-p:WarningsNotAsErrors=NU1902%3BNU1903` (no repository change). A real fix (patched Pkcs version or audit policy) is an operator decision outside this workstream.
- With that override the tree builds (0 errors, 72 warnings).
- The ratchet test failed after the rebase because develop added one `Path.GetFullPath` each to `src/QBrainAi.Client/LinuxPhysicalPathResolver.cs` (line 307, BUG-BASELINE-009 Linux directory pinning) and `src/QBrainAi.Services/Services/RepoFileService.cs` (line 279, BUG-BASELINE-003 repo-relative mapping). Both are host-native file operations, so the allowlist was raised (8 to 9, 2 to 3). That edit is in the handoff commit.
- Full `QBrainAi.Support.Mcp.Tests` on the rebased tree, run after the allowlist edit (the run rebuilt the test project; the ratchet passed): total 3270, passed 3264, failed 4, skipped 2. TRX: `artifacts-local/trx/rebase-20261016-QBrainAi.Support.Mcp.Tests.trx`.
  - `Services.UseCaseGetWorkspaceResolutionContractTests.ProductionGate_EquivalentRawPath_UsesCanonicalWorkspace`: new on develop; expects `F:\GitHub\TruckMate`, gets `F:/GitHub/TruckMate`. A stale expectation under FR-MCP-PATHNORM-001; update it with `ExpectedWorkspaceKey.Of` (test first, per BDP).
  - `Storage.Decompose4nfBackfillMigrationTests.Migrate_Decompose4nf_BackfillsJsonListColumnsIntoChildTables`, `Storage.RemoveTodoRecordsMigrationTests.Migrate_RemoveTodoRecords_BackfillsAnchorOnlyTriageTodos`, `Storage.RemoveTodoRecordsMigrationTests.Migrate_RepairTriageCreatedTodoWorkspace_CopiesWrongWorkspaceTodoToTriageWorkspace`: all three were in the old f56dcf70 baseline. Not yet attributed: run the same tests on a clean checkout of `99a2a840` to learn whether develop still fails them or whether the workspace-key converter now affects these migration tests (they seed rows with raw SQL, which bypasses the converter).
- Client.Tests and Build.Tests have not been run on the rebased tree.
- The old baseline list in receipt 06 is obsolete for this base; establish a new baseline on `99a2a840` before the P1.4 gate.

## 5. Remaining work, in order

1. (Done 12:16) Rebase published on `claude/federation-fixes-20261016-rebased` (section 6).
2. Establish the develop baseline at `99a2a840` (Support.Mcp, Client, Build), with the NU1902/NU1903 override or a real fix.
3. Update the stale `UseCaseGetWorkspaceResolutionContractTests` expectation; attribute the three migration-test failures.
4. P1.4 gate: three suites, compare with the new baseline, write `08-gate-p1.4.txt`, remove the probe worktree.
5. P1.5 (plan section 4c.3 and 4c.4): pure mapping `f(x)` and hash recomputation (P1.5a), rekey planner (P1.5b), provider executors for SQLite, SQL Server and PostgreSQL with deferred or suspended FKs re-validated in one transaction (P1.5c, D6), the `--workspace-path-rekey` command with `--dry-run`, `--backup-path`, `--report-path`, the startup guard and the token-store rewrite (P1.5d, D5, D7), and a rehearsal on a restored copy of `McpServer_PAYTON_LEGION2` (P1.5e). Inventory (earlier): about 1.86M backslash rows across 50 columns, about 68 NO ACTION foreign keys, 11 composite. Recompute triage `GroupKey`/`GroupId`, `HandoffIngestionRuns.ReplayIdentity` (its hash includes the workspace path, so pre-upgrade replay identities stop de-duplicating otherwise) and federation `GlobalWorkspaceId`/`CanonicalWorkspaceId`.
6. P1 gate, then P2 (`IMPL-FEDROUTE-001`: hub resolves forwarded proxy workspaces and fails closed; LocalProxy sends `X-Mcp-Global-Workspace-Id`), P3 (`IMPL-FEDWSSYNC-001`: typed workspace configuration sync, CurrentRequirementLayerKey hub-owned per D3), P4 (`IMPL-FEDBACKFILL-001`: backfill through signed envelopes with `applyMode: backfill` and no fanout per D2; proxy serves reads locally until a workspace backfill completes per D4).
7. Deployment (plan 4c.5): back up both databases, rekey locally, deploy, verify, repeat on LAB-OMARCHY, re-enroll PAYTON-LEGION2 (D1).

## 6. Publishing the rebase (decided 12:16)

The rebased history replaces three commits that were already on origin, so a plain push to the old branch is rejected as non-fast-forward, and the standing rule is "Never force-push". Of the three options offered (new branch, one-time force-with-lease, or undo the rebase and merge develop), the operator chose the new branch: `claude/federation-fixes-20261016-rebased`. Continue all work there. Any PR for this workstream should come from that branch.

## 7. Decisions on record (plan section 4 and 4c.6)

- D1: forward-slash storage folded into P1; re-enroll PAYTON-LEGION2 once after P1 and P2 deploy.
- D2: backfill through batched signed envelopes, `applyMode: backfill`, no fanout.
- D3: CurrentRequirementLayerKey is hub-owned; drift is reported, never overwritten by heartbeats.
- D4: the proxy serves reads locally per workspace until that workspace's backfill completes; writes forward to the hub.
- D5: explicit `--workspace-path-rekey` maintenance command plus startup guard; no automatic startup migration.
- D6: foreign keys deferred or suspended in one transaction per database and fully re-validated.
- D7: no legacy dual-key lookup; the startup guard refuses an unconverted store.

## 8. Requirements, TODOs, triage, memory

- Requirements (earlier, MCP store, workspace `F:\GitHub\McpServer`): FR/TR/TEST-MCP-FED-PATH-001, -FED-ROUTE-001, -FED-WSSYNC-001, -FED-BACKFILL-001, and FR/TR/TEST-MCP-PATHNORM-001 (updated for D5 to D7).
- TODOs (measured 12:13 through `todo_get`): `IMPL-FEDPATH-001`, `IMPL-FEDROUTE-001`, `IMPL-FEDWSSYNC-001`, `IMPL-FEDBACKFILL-001` are all Done false, each with only "Requirements registered" done. The `IMPL-FEDPATH-001` note is stale: it still says "Next: P1.4a" and should be updated through MCP to point at this handoff. `PLAN-PATHNORM-001` is not in the local store (it was created on the LAB-OMARCHY hub; not re-measured); session turns 007 and 008 reference it.
- Triage (earlier): groups `triage-group-18acf2d8a0adfecd` (no backfill on enrollment), `triage-group-d708a0e30a2b7195` (workspace field drift), `triage-group-bfc69ceba9e9f1fb` (Windows path mangling on the hub), whose background research failed; reports `triage-report-430f89a36197402ea514069a09740afd` (requirements_create returns an empty WorkspaceId), `triage-report-4635ba853ac74942996858c1981d3a81` (stale docs/wiki.yaml source after the rebrand), `triage-report-a5fb0faedc244cc5b3c480bfdb884cf4` (SQL Server migration tests time out under parallel load).
- Memory: MEMORY-PROCEDURE-002 (BDP v4, no red tests), MEMORY-PROCEDURE-003 (rebrand HV runs use their own worktree), MEMORY-FACT-006 (never move files), MEMORY-PREFERENCE-005 (HV model while Grok is unavailable).

## 9. Federation runtime facts (earlier unless marked)

- Hub: LAB-OMARCHY (10.42.0.214), McpServer on port 7147 configured as federation hub, database `McpServer_Omarchy`. GlobalWorkspaceId on the hub is SHA256(lower(proxyId:storageKey)). There is no backfill on enrollment; on 2026-10-06 the proxy held about 1.885M rows and the hub 1,650. Proxy workspaces were unaddressable on the hub (400 for the proxy path and the hash id; no header fell back to the hub's primary workspace).
- Proxy: PAYTON-LEGION2 McpServer on port 7147, database `McpServer_PAYTON_LEGION2` on local MSSQLSERVER (cloned from LAB-OMARCHY on 2026-10-06). Currently Standalone (measured 12:12).
- Lab network, SSH and Tailscale details are in the operator rules (`~/.claude/rules/dev-lab.md`).
