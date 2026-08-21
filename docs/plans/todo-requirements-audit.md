# Plan: Audit project state; align open TODOs to logged requirements and HEAD code

**Scope (live MCP `todo_list` `done: false`, 2026-08-20):** 38 open TODOs. Align their contents, requirement links, and priorities to currently logged MCP FR/TR/TEST records and the actual code on `develop` HEAD. This plan does not implement QuadBrain, FILETOOLS, Handoff product slices, or BUG-TRIAGE-160..163 remediations.

**Master tracking TODO (create after approval):** `PLAN-TODOAUDIT-001`

**Durable plan path after approval:** `docs/plans/todo-requirements-audit.md` (copy of this document)

**Process:** Byrd Development Process v4 (`docs/Development-Process-draft-v4.md`)

**Predecessor:** leftover-27 (`PLAN-TRIAGELEFTOVER-001`, `docs/plans/triage-cluster-002.md`) is `Done: true` with H-done AGREE `docs/receipts/hostile-validator-20260820T092641Z.md`. Do not reopen leftover-27. Do not treat 160-163 as leftover-27.

**Breaking change:** No. Store content and requirement-status evidence only. No product behavior change. Not `/health`. Not PSGallery. Not wrap-up/commit-sync unless the operator explicitly authorizes those skills.

**Hostile gates:** H0 after S0 inventory freeze. H-classify after S3 matrix. H-apply after S7 store writes and export regenerate. H-done before `PLAN-TODOAUDIT-001` `done: true`. OverallVerdict AGREE required. Cite receipt path in `doneSummary`.

## Problem

Open TODOs and logged requirements have drifted from HEAD.

Observed from live MCP on 2026-08-20 (workspace `F:\GitHub\McpServer`):

- 38 TODOs `done: false`.
- 293 FR: 27 completed, 7 in_progress (all `FR-HANDOFF-001..007`), 3 planned, 2 deferred, 254 pending.
- 422 TR: 30 completed, 8 in_progress (all `TR-HANDOFF-*`), 5 planned, 13 deferred, 366 pending. About 28 placeholder/stub TRs (`[]`, `TR-01`..numeric stubs, "Placeholder requirement backfilled").
- 448 TEST: 50 completed, 7 in_progress (all `TEST-HANDOFF-*`), 8 planned, 1 satisfied, 382 pending.

Drift modes already visible without a full re-audit:

- Many Remaining fields still say `Actual-code completion audit 2026-07-11`. That keyword-grep is not HEAD evidence.
- QuadBrain E1 Remaining claims 0 percent rename work. HEAD has `RenameQuadBrainRolesToCreativityLogic` migrations and `BrainSlotRoles.Creativity` tests.
- `MCP-SESSIONLOG-001` Remaining says S13/S14 (federation + outermost DI) not started. HEAD already constructs `SessionLogSanitizingService` in `Program.cs` and `McpStdioHost.cs`, and `FederatedSessionLogServiceTests` wrap the decorator.
- `MCP-HANDOFF-001` Remaining says Phase 0 plus focused tests green; every `ImplementationTasks` row is still `Done: false`; FR/TR/TEST stay `in_progress` with `isSatisfied: false`.
- `PLAN-FILETOOLS-001` Remaining looks for `IRepoDiscoveryService`. HEAD has `RepoFileService` / `IRepoFileService` plus tests, but MCP tools/list still does not expose `read_file` / `list_dir` / `grep_files` names from `FR-MCP-FILETOOLS-001`.
- 14 QuadBrain/QBCODE TODOs have empty `functionalRequirements` / `technicalRequirements` even though related FR/TR families exist in the store and markdown.
- `PLAN-DELETECOMPLIANCE-003` links FR `[]`, which created a TR id `[]` placeholder.
- Native MCP `todo_update` only patches title/priority/done/note. Remaining, description, implementationTasks, and FR/TR links require plugin `workflow.todo.update`.

Agents currently pick "high" Backlog items whose Remaining is a month stale, while live write 503s (160-162) sit at medium.

## Value

The next implementation playlist matches what the code and the requirements store actually are. Operators stop re-doing work that already shipped. Incomplete slices keep honest Remaining and task checkboxes. Live defects rank above speculative Backlog. Requirement status stops claiming pending for bodies that already say Complete, except where evidence is missing.

## Locked decisions

1. MCP database is source of truth. Never write `docs/Project/TODO.yaml`, session-log files, or requirements markdown as the write path. After store updates, regenerate export docs through `requirements_generate` so markdown matches the store.
2. 2026-07-11 Remaining is historical only. Every updated Remaining must cite HEAD evidence (type names, test class names, `todo_get` after patch, UTC date).
3. Full-field TODO patches use plugin `workflow.todo.update` (or equivalent typed client). Do not use native `todo_update` for Remaining, tasks, or requirement arrays.
4. Never set `done: true` without hostile OverallVerdict AGREE. SHIPPED-UNCLOSED items stay open until that gate, even if all implementationTasks are true.
5. This plan does not implement product slices (QuadBrain, QBCODE, FILETOOLS, Handoff, wiki dump, hostile-review queue, LLM strategy, Octopus, 160-163). It only classifies them and rewrites store contents.
6. Requirement *status* `completed` plus AC `isSatisfied: true` is a goal-state change. Same hostile-on-goal-state rule. Empty-AC FRs do not auto-complete.
7. New FR/TR/TEST discovered during the audit are captured in MCP, then this plan stops on that item for operator approval before any product implementation (`requirement-change-plan-first`). Linking an existing FR/TR onto a TODO is not a new requirement.
8. `PLAN-TODOAUDIT-001` is the tracker for this audit. `TR-AUDIT-001` stays open for TR-internal consistency and TR-to-code compliance. This plan feeds TR-AUDIT Remaining; it does not close TR-AUDIT unless H-done proves its remaining tasks are also done.
9. Leftover-27 and `PLAN-TRIAGELEFTOVER-001` stay closed. BUG-TRIAGE-160, 161, 162, 163 stay open.
10. Do not delete TODOs unless a duplicate is proven and the operator confirms. Prefer Remaining rewrite, priority change, and task-flag correction.
11. Invalid links (`[]`, numeric TR stubs) are unlinked or pointed at real IDs. Placeholder TR records are `deferred` with notes, not deleted, unless the operator confirms delete.
12. Worktrees are not used. No merge. No Nuke UpdateService. No SyncAgentPlugins unless a later approved product plan needs them.
13. pwsh.exe only. No Python. JSON/YAML from native objects.
14. Wrap-up / commit-sync / wiki push only if the operator explicitly asks after this audit.

## Classification enum (per open TODO)

Each of the 38 items gets exactly one primary class plus optional tags.

- **ALIGNED:** Remaining, tasks, FR/TR, and priority already match HEAD and logged requirements.
- **STALE-CONTENT:** Still open work, but Remaining, task Done flags, or description disagree with HEAD.
- **PARTIAL-SHIPPED:** Some AC/tasks exist in code; remaining work is real.
- **SHIPPED-UNCLOSED:** In-repo AC plus tests exist; Remaining should say so; `done` stays false until hostile AGREE and operator store-close.
- **NOT-STARTED:** No material HEAD evidence for the stated AC.
- **ORPHAN-TODO:** No FR/TR arrays. Either link existing IDs or record `OrphanReason` in Remaining and a note. Do not invent FR IDs without capturing them through MCP first.
- **SUPERSEDED:** Work absorbed by a closed TODO/plan. Remaining must say which ID. Still no `done: true` without AGREE.
- **WRONG-REPO:** Defect not in this workspace (candidate: BUG-TRIAGE-163 avalonia-remote). Remaining must name the actual repo or N/A.
- **BLOCKED:** `dependsOn` not met. Priority may drop; Remaining must name the blocker ID.
- **DUPLICATE:** Same AC as another open TODO. Keep one canonical ID.

Requirement records get a parallel class: **STATUS-STALE**, **AC-MISSING**, **PLACEHOLDER**, **MAPPED**, **UNMAPPED**, **OBSOLETE** (example: FR-MCP-019 marked obsolete in body).

## Priority rubric (apply in S5)

P0 `critical`: live agent-blocking defects with recent evidence. Seed: BUG-TRIAGE-160, 161, 162.

P1 `high`: in-progress logged requirements or incomplete slices whose Remaining is current. Seed: MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001, MCP-SESSIONLOG-001 (if S13-S19 still open after re-audit), MCP-PLUGININT-001, MCP-PLUGINCORE-004.

P2 `high` sequenced (keep high, do not start until predecessors): PLAN-QUADBRAIN-* (I1 first), PLAN-QBCODE-* (after QuadBrain IOC), PLAN-FILETOOLS-001..004, PLAN-LLMSTRATEGY-001, MCP-WIKIEXPORT-001, MCP-HOSTILEREVIEW-001, MCP-WORKSPACEHYGIENE-002.

P3 `medium`: PLAN-BYRDPROCESS-001, PLAN-DELETECOMPLIANCE-003, TR-AUDIT-001 (after this plan writes its findings into Remaining), PLAN-OCTOPUS-001, PLAN-SHARPMIND-001 (blocked on LLMSTRATEGY).

P4 `low`: BUG-TRIAGE-163 unless S1 proves it is this repo.

Section cleanup: stop parking product work in generic `Backlog` when a real section exists (`QuadBrain`, `Session Logging`, `MCP Server`, `Iteration Global File Tools`, `Plugin Core`, `Testing`, `Review Automation`, `Workspace Validation`).

`PLAN-TODOAUDIT-001` itself: section `Backlog` or `Process`, priority `high` until H-done.

## Evidence rules (tests-first analog)

This is store hygiene, not a product feature. The "red" artifacts are mechanical classifiers that must fail on current drift before any MCP write.

Named verification (must exist as receipts before S5 writes):

1. Open-TODO snapshot JSON from `todo_list` `done: false` (38 or the live count at S0).
2. FR/TR/TEST snapshot counts by status from `requirements_list`.
3. Per-TODO classification row: id, class, HEAD evidence (file or "absent"), linked FR/TR or OrphanReason, current priority, proposed priority, proposed Remaining summary.
4. Negative checks that prove drift: at least one Remaining still containing `2026-07-11`; QuadBrain E1 Remaining vs rename migration files; SESSIONLOG-001 Remaining vs `Program.cs` decorator registration; FILETOOLS Remaining vs missing `read_file` MCP tool name; DELETECOMPLIANCE FR `[]`.

After S5/S6, the same queries must show:

- No open TODO Remaining whose only completion claim is the 2026-07-11 audit sentence (historical citation allowed if paired with a 2026-08-20+ HEAD sentence).
- Every open TODO has FR/TR arrays or an explicit OrphanReason in Remaining.
- Priorities match the rubric unless the operator amended it at approval.
- `todo_get` after each patch equals the intended object (not the request we meant to send).
- `ValidateTraceability` findings = 0 if mappings changed; if it already fails, record the failure and do not claim S8 done.

No skipped tests. If a C# or Pester harness is added, that is a new product requirement: capture FR/TR/TEST, stop, wait (S9). Do not sneak a Nuke target into this plan.

## Grouping for S1 (read-only parallel)

### G-A QuadBrain revision playlist (7)

IDs: PLAN-QUADBRAIN-001, I1, E1, C1, C2, C3, T1.

HEAD already has Creativity/Logic migrations and role tests. Re-audit C1 readiness split, C2 ballot protocol, C3 research loop against source, not 07-11 keywords. Link existing FR-MCP-129 / FR-MCP-134 / related TRs if those IDs are in the store. Do not mark the playlist done.

### G-B QBCODE playlist (7)

IDs: PLAN-QBCODE-001, I1, E1, C1, C2, C3, T1.

Blocked on QuadBrain IOC per their own description. Confirm blocker still true. Link or OrphanReason. Priority stays sequenced P2.

### G-C FILETOOLS + LLM + SharpMind (6)

IDs: PLAN-FILETOOLS-001..004, PLAN-LLMSTRATEGY-001, PLAN-SHARPMIND-001.

FILETOOLS: distinguish existing `RepoFileService` from FR-MCP-FILETOOLS-001 MCP tool names. LLMSTRATEGY: still needs FR/TR capture (orphan until linked). SHARPMIND: BLOCKED.

### G-D Handoff (3)

IDs: MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001.

Only in_progress FR/TR/TEST family. Flip implementationTasks to match files (`HandoffClient`, `HandoffIngestionStorageMigrationTests`, REPL/Director commands). Keep `done: false`. Keep priority P1.

### G-E Session log, plugin, hostile, wiki, hygiene (8)

IDs: MCP-SESSIONLOG-001, MCP-SESSIONLOG-002, MCP-PLUGINCORE-004, MCP-PLUGININT-001, MCP-HOSTILEREVIEW-001, MCP-WIKIEXPORT-001, MCP-WORKSPACEHYGIENE-002, MCP-PLUGININT already listed.

SESSIONLOG-002 is the strongest SHIPPED-UNCLOSED candidate (tasks all true, note cites hostile 185650Z, Remaining says in-repo complete). Do not store-close in this plan unless H-done of *this* audit plus operator yes.

SESSIONLOG-001: re-walk S13-S19 against HEAD; rewrite Remaining and task flags.

HOSTILEREVIEW / WIKIEXPORT / WORKSPACEHYGIENE: likely NOT-STARTED plus ORPHAN-TODO; Remaining already says needs BDPv4 requirements first.

### G-F Bugs 160-163 (4)

Keep open. Write Remaining from current diagnosis (160-162: full SubmitAsync upsert 503 vs incremental dialog POST). Priority 160-162 to critical. 163 WRONG-REPO or low until proven.

### G-G Process / compliance (4)

IDs: TR-AUDIT-001, PLAN-BYRDPROCESS-001, PLAN-DELETECOMPLIANCE-003, PLAN-OCTOPUS-001.

BYRDPROCESS: v4 draft exists; Remaining correctly notes missing RUP LCO/LCA/IOC/PR terms in the process doc. Unlink FR `[]` on DELETECOMPLIANCE. OCTOPUS stays NOT-STARTED.

## Slices

**S0 Inventory freeze**

Create `PLAN-TODOAUDIT-001` after approval. Snapshot via MCP:

- `todo_list` done false and done true counts
- `todo_get` for all 38 open IDs
- `requirements_list` type fr, tr, test, mapping
- git rev-parse HEAD, `git status --short` (do not commit)

Write receipts under `docs/receipts/todo-audit-<utc>/` as JSON produced by pwsh `ConvertTo-Json` from native objects. H0 hostile on: snapshot completeness, leftover-27 still done, 38-count (or recorded delta).

**S1 Classify open TODOs vs HEAD**

Read-only subagents per group G-A..G-G. For each ID: grep/types/tests named in the TODO; `requirements_list` for linked IDs; fill classification row. No MCP writes. H-classify waits for S2/S3.

**S2 Classify logged requirements vs code**

Do not walk 293 FR bodies by hand without a pwsh pass. Mechanical pass:

- Status vs body tokens (`Complete`, `Covered by`, `Obsolete`, `Planned`)
- Empty vs non-empty `acceptanceCriteria`
- Placeholder IDs (`[]`, `TR-01` style)
- in_progress set (expect Handoff only unless live store moved)
- FR/TR IDs referenced by TODOs that do not exist
- FR/TR that exist with no TODO (orphan-req list). Do not auto-create TODOs for all 254 pending FRs. S7 only creates TODOs for in_progress or operator-named gaps.

**S3 Gap matrix and proposed patch list**

One receipt JSON array: for each of 38 TODOs, the exact `workflow.todo.update` field set (priority, section, remaining, implementationTasks, functionalRequirements, technicalRequirements, note). For each requirement status/AC change, the exact update. No writes yet.

H-classify: attack the matrix for invented FR IDs, premature `done: true`, leftover-27 reopening, 07-11-as-evidence, FILETOOLS/`RepoFileService` conflation, QuadBrain rename denial.

If H-classify DISAGREE, fix the matrix; do not write.

**S4 Apply TODO patches (plugin only)**

For each matrix row, `workflow.todo.update`, then `todo_get` and diff. Persist session-log actions per ID. Stop the batch if a get does not match.

**S5 Apply requirement patches**

Only STATUS-STALE rows with mechanical evidence *and* AC text that can be marked satisfied with evidence paths. Placeholder TRs: `deferred` plus notes. Unlink `[]`. Do not complete Handoff FRs here (product still open).

If a status `completed` is proposed, hostile that single ID before the write (goal-state).

**S6 Create missing TODOs (bounded)**

Create TODOs only when S3 lists an in_progress requirement family with no TODO (should not happen for Handoff) or an operator-approved orphan-req that is actually active work. Canonical IDs. Link FR/TR. Do not explode 254 pending FRs into 254 TODOs.

**S7 Regenerate exports and close the tracker**

`requirements_generate` format markdown doc all (and wiki only if the operator asks). `ValidateTraceability`. Re-query open TODOs. Rewrite `TR-AUDIT-001` Remaining with the TR findings from S2 (do not mark TR-AUDIT done unless its code-compliance task is truly finished). H-apply on store vs matrix. H-done on PLAN-TODOAUDIT-001.

**S8 Out of scope unless operator adds it**

Product implementation of any classified TODO. Durable CI `ValidateTodoAlignment` Nuke target (that would be a new FR; capture and stop). Commit-sync, wrap-up, wiki push, UpdateService.

## Named tests / verification (minimum)

- S0 JSON: open count, FR/TR/TEST status histograms.
- S3 matrix: 38 rows, each with class and evidence path.
- Post-S4: `todo_get` for every patched ID; Remaining date >= audit UTC; no leftover `FR=[]` except documented OrphanReason.
- Post-S5: `requirements_list` no TR id `[]` linked from an open TODO; Handoff still in_progress.
- `PLAN-TRIAGELEFTOVER-001` still `done: true`.
- Hostile receipts H0, H-classify, H-apply, H-done.

## Merge and TODO closeout

No git merge. After H-done AGREE: `workflow.todo.update` PLAN-TODOAUDIT-001 `done: true` with `doneSummary` citing `docs/receipts/hostile-validator-<utc>.md`. Copy this plan to `docs/plans/todo-requirements-audit.md` after approval, before S0, so receipts can point at a durable path.

## Out of scope

- Reopening leftover-27 or PLAN-TRIAGECLUSTER-001.
- Implementing or closing 160-163, QuadBrain, QBCODE, FILETOOLS, Handoff, Octopus, wiki dump, hostile-review queue.
- Mass-completing 254 pending FRs.
- `/health` liveness change.
- PSGallery vendor patch.
- Azure wiki / merge to main / commit-sync unless the operator asks.
- Deleting placeholder TRs without a confirm.

## Risks

- Native `todo_update` silently drops Remaining if someone uses it. Mitigation: plugin `workflow.todo.update` only; prove with `todo_get`.
- Hostile may refuse SHIPPED-UNCLOSED close of SESSIONLOG-002 because live UpdateService was a non-goal. Mitigation: this plan does not close it.
- Requirement generate overwrite of markdown that agents still treat as SoT. Mitigation: store-first, then generate; do not hand-edit markdown.
- Parallel subagents writing TODOs. Mitigation: S1-S3 read-only; S4-S5 single writer.
- Disk/context: 293+422+448 requirement payloads are large. Mitigation: pwsh parse of saved MCP JSON files; no Python.

## Approval

Stop here. Implement S0 only after explicit plan approval. Do not create PLAN-TODOAUDIT-001, do not patch TODOs or requirements, and do not copy the durable plan file until approved.
