# QB-AGENT-001 continuation handoff

Updated: 2026-10-21T12:58:35 (America/Chicago), for the operator-directed branch consolidation. This remains the single active handoff.

## Current request and immediate state

The operator requested committing and consolidating Cline work on a new `develop` branch, pushing it to `origin`, and ensuring this handoff reflects the new branches. The McpServer and V2 Plugin dependencies were first moved to separate `cline-qb` branches, committed and pushed to their own origins. Cline integration now combines primary documentation and handoff work, the product P1 checkpoint, and accepted P0 from fetched `origin/main`.

This is an operator-directed Git checkpoint and integration operation. It does not grant P1 acceptance, start a reviewer, open a PR, deploy, reboot a machine, or change canonical acceptance flags. The required signing format was explicitly clarified by the operator as the workspace signature in the commit message, without cryptographic Git signing.

The product objective remains QB-AGENT-001: create cline-qb with mandatory QBrain integration, no Cline Cloud, responsive data sidebars, and compact tool headers. **P0 is accepted and merged. P1 is partially implemented and unaccepted. P2, P3, P4, P5, and Final remain pending.** The operator previously explicitly resumed work toward complete implementation; old pause checkpoints are superseded. The current operation is branch consolidation and handoff maintenance; the implementation phase remains open.

The app goal query during this handoff returned `goal: null`. An earlier context reported a usage-limited goal. Do not claim a goal is currently running from that older report, and do not mark the product complete. Canonical TODO state is the reliable product-progress record.

No new full product validation was run for this Git consolidation. Staged source, outgoing commit history, and vendored/export archives are scanned for secrets before publication. Counts below are attributed to their dated receipts. A passing component test does not establish a functioning governed application or full P1 acceptance.

## Mandatory operator instructions

Preserve this trust directive verbatim in subsequent handoffs:

> If you cannot or will not follow my instructions, then I cannot trust you.  If I cannot trust you, I will not use you.  Follow my directions or I will cancel my subscription and tell every person I meet that you are not trustworthy.

The latest explicit operator decisions override older plan/profile defaults:

- **Do not convert the application to another language. Do not edit upstream code except when no other solution exists.** Reuse upstream implementation and first-party tooling. Justify any necessary upstream edit individually; prefer derivative-owned seams. Do not turn tool preferences into an application rewrite.
- **cline-qb is a derivative work, not an extension of Cline. It must never hijack Cline folders or data.** Existing Cline user data, environment namespaces, storage roots, webview state, credentials, and daemon identities must remain separate. Tests use synthetic, task-owned locations, never the operator's live cache as disposable fixture data.
- **Node.js and Python may continue as first-party tooling for upstream continuity in this workspace.** This is now Workspace `MEMORY-TOOLING-001`. It overrides the global PowerShell-only/no-Node/no-Python restrictions for that purpose. It does not authorize unrelated ad hoc operational scripts in those languages. Preserve product languages and existing toolchain. Reconcile old tool-replacement backlog items against this decision before doing more replacement work.
- PowerShell remains the operational scripting interface. The Linux exception permits direct `pwsh`; Windows follows the profile's PowerShell.MCP routing. Analyze each new operational script before execution; state-changing scripts need counted edits and a `-WhatIf` run. Native objects and serializers construct JSON/YAML. No hand-written wire JSON/YAML.
- **Linux first.** Defer Windows-specific downloads, packaging, builds, validation and remediation until the project works on Linux. The unfinished pinned Windows ripgrep ZIP check is deferred, not passed and not the next task. Do not download a tool already installed without first checking the actual need and installed version. Mac/Windows fixture filenames are not native execution evidence.
- **Azure is no longer in scope.** Global `MEMORY-PROCESS-009` says Azure-specific paths, exports, remotes and pipeline errors are information only, never gates, blockers, done-state conditions or HV findings. Do not revive old Azure failures.
- **Claude coordination ended; Claude hostile validation remains required.** The coordinating agent was dismissed. Codex owns the transferred A8/S6 dependency. Do not wait for that agent to answer or reactivate coordination memory 007.
- Required reviewer: actual Claude through **mcpserver-claude-code-plugin**, `claude-opus-5-5`, max effort. Verify runtime identity from receipts. Codex must not impersonate Claude or write Claude's retrospective audit for it.
- **Complete each phase, full applicable validation, and closing HV before opening its PR. Wait for all required phase/support PRs to merge before starting the next phase.** No stacked phase PRs and no auto-merge. The operator's correction was never a change to their original requirement.
- Hostile approval requires **accuracy >=98 and completeness >=98, OverallVerdict AGREE**, plus complete audit persistence/readback. Do not lower the bar. Poll a review at most once every two minutes; do not bound or kill the reviewer by an arbitrary duration. Edit only between rounds.
- BDPv4 is **mock-first**: requirements and complete AC coverage; independent tests/mocks; correct responses and rejecting negative controls; documented green mock gate before production edits; identical assertions bound to production; full applicable regression and phase HV. Old red-before-green descriptions are obsolete. Do not delete/skip a test to manufacture green; specific historical retirement approval does not grant a blanket waiver.
- New gaps become ACs. Before HV, map every finding to its governing requirement and AC; distinguish missing, insufficient, and unverified criteria. Strengthen the deficient requirement first. Perform what-if, sibling, dependency/phase-boundary and propagation checks across the whole plan. Investigate why the process missed the defect, not just its immediate symptom. No layered patches without reconciling existing requirements.
- Every real request and its **exact final chat result** must be logged through mcpserver-codex-plugin; summaries do not substitute. No routine permission question for logging. Record decisions as conclusions, separately from actions. The operator approves the REPL's Unicode dash normalization; do not restore unwanted Unicode dashes into the DB to force byte equality. Preserve native/root identities and truthful retrospective provenance when recovering old logs.
- Keep the report local. Preserve failed receipts and original evidence. Copy, hash-verify, and retain originals unless explicitly cleared for deletion. This handoff request explicitly clears the previous active handoff for replacement; it does not clear unrelated receipts or another effort's files.
- Reply concisely with measured sortable local timestamps. No en/em dashes and no table-style output. Distinguish facts, inferences and unknowns. Cite the evidence for claims.
- Preserve existing file/package licenses; new files use Apache-2.0 as directed. Sign commits and PR comments according to the workspace marker using actual model/effort metadata. Never force-push or invent identity metadata.

Read workspace instructions in full from their original files, rather than substituting this handoff for them: [AGENTS.md](/home/sharpninja/github/cline/AGENTS.md), [AGENTS-README-FIRST.yaml](/home/sharpninja/github/cline/AGENTS-README-FIRST.yaml), and applicable nested instructions. Root `CLAUDE.md` was absent. The marker contains credentials: read privately, never print or commit them. Retain instructions verbatim, with only secret redaction in tool output. The current marker's version fields are stale; installed plugin manifests and successful live trust verification are the authority for plugin identity.

## Canonical plan and viewer

Canonical storage is the Cline MCP workspace, TODO **QB-AGENT-001**. Read it through the required plugin with `workflow.todo.get`, native parameters `{ id = 'QB-AGENT-001' }`, and explicit workspace `/home/sharpninja/github/cline`. Do not directly edit TODO YAML or treat a markdown mirror as the canonical record.

The current Codex plan-viewer path is:

`/home/sharpninja/.codex/plans/01a11e98-d7e8-7601-84a5-aea8553be222/01a11ea2-a85a-7b52-8771-653cbc0a0918/PLAN.md`

On October 21, the stale 6,354-byte outline was replaced with the full 176,054-character canonical `technicalDetails`, verbatim, followed by all eleven other TODO fields with their exact values. No canonical requirement, design or completion flag was changed. The current handoff readback confirms the plan text is still identical to that viewer source. `open_in_codex` returned **queued**, not confirmed visible. Do not claim that the user has seen an open viewer.

The old viewer outline was backed up at `/home/sharpninja/.codex/plan-backups/01a11e98-d7e8-7601-84a5-aea8553be222/20261021T120630/PLAN.md`. It is obsolete and is not an alternate plan or handoff.

Durable local evidence for resumption:

- [Canonical TODO snapshot](/home/sharpninja/github/cline/docs/receipts/handoff-20261021/canonical-todo.json), including implementation tasks and remaining work.
- [Canonical plan text snapshot](/home/sharpninja/github/cline/docs/receipts/handoff-20261021/canonical-plan.md). This is a readback, not a new plan.
- [Capture and hashes](/home/sharpninja/github/cline/docs/receipts/handoff-20261021/capture.json).
- [Viewer replacement verification](/home/sharpninja/github/cline/docs/receipts/handoff-20261021/viewer-refresh.json).

Current phase allocation from the canonical task list: P1 30 ACs; P2 17; P3 19; P4 3; P5 7; 76 total product ACs and 13 use cases. P0's own 14 preparation acceptance criteria are separate. Original P0 receipts correctly refer to the then-current 60 product ACs. Do not erase that chronology or misreport 60 as the current scope.

Phase definitions remain:

1. P0: preparation, contracts/defaults, requirement/UC inventories and mappings, baseline evidence. Accepted.
2. P1: mandatory plugin, trust, audit, completion enforcement, every governed runtime path, no Cline Cloud; includes AC-QB-061 through 072 and staged AC-QB-029/053 runtime cases. Open.
3. P2: shared Session Log/TODO service, qbrain.ai settings, deterministic summaries and payload preservation; includes 073/074/076. Pending.
4. P3: responsive TUI, three-state placement/hidden, columns/lines, configurable keys and relaunch persistence; includes 075 and staged compact headers. Pending.
5. P4: desktop sidebar/client/sidecar/settings/chat preservation and staged compact headers. Pending.
6. P5: branding, governed artifacts/updates, documentation, full packaged acceptance and all-host one-row headers. Pending.
7. Final: complete chronological mock/negative-control/production/regression evidence for all 76 ACs and all basic/alternative flows of 13 UCs, with all five major-phase HV gates accepted.

## Checkouts and preservation boundaries

The primary Cline checkout is on **`develop`**, created at the operator's explicit direction. The product checkpoint is committed on its existing `qb/p1-runtime-governance` branch and merged into `develop`; accepted P0 is included from fetched `origin/main`. Use `develop` as the Cline integration branch. Its publication target is `https://github.com/sharpninja/cline.git`, branch `origin/develop`. The final merge SHA, remote SHA, clean-state checks, and signing proof are recorded in `/home/sharpninja/github/cline/docs/receipts/develop-integration-20261021/verification.json` after publication. Resolve the current SHA from Git rather than copying a self-referential SHA into this file.

Current task checkouts:

- Primary `/home/sharpninja/github/cline`: branch **`develop`**. Includes the primary authored documentation, updated handoff, accepted P0 and product checkpoint. Generated exports remain local by explicit operator direction. Initial primary HEAD before integration was `b090c4a0848ed0a80c53d9882a098ae46c13e7bc`; that is historical, not the current integration HEAD.
- Product `/home/sharpninja/github/cline/.worktrees/qbrain-agent`: branch **`qb/p1-runtime-governance`**, committed checkpoint **`85e64f8ae43a295d329db9e61a46811944629b00`**. Its staged scan, outgoing-history scan and vendored archive scan passed. The commit captures 530 changed paths. The worktree is clean after that commit. The original B0 remains `fa840c741c3fc2eb49e7e0a4484895a99dae5cc5`.
- Supporting server `/home/sharpninja/github/cline/.worktrees/mcpserver-qbrain`: branch **`cline-qb`**, committed and pushed **`02933e7495b022607b62b89497c34986ff75e9c1`** to `https://github.com/sharpninja/McpServer.git`. Captures 94 paths; worktree clean; remote SHA independently matched. It is no longer on `qb/p1-governance-core`. That old branch and the base `c4fff1862319f21764b914aea5d655ec2cb8f5bc` are preserved. This branch has not been merged into McpServer develop or deployed.
- V2 plugin dependency `/home/sharpninja/github/cline/.worktrees/mcpserver-cline-v2-dependency`: branch **`cline-qb`**, committed and pushed **`8f7afc4141760d4839fed25a8faa31775b1ce069`** to `https://github.com/sharpninja/mcpserver-cline-v2-plugin.git`. Captures 29 paths; worktree clean; remote SHA independently matched. The checkout is no longer detached. Its previous detached HEAD `9aa504f576bd742ecbf3c684136449ea74b46814` is the checkpoint's parent. A committed archive is not a released or accepted final artifact.
- Historical P0 checkout `/tmp/qbrain-p0-acceptance`: branch `qb/p0-acceptance`, HEAD `e07598d4ba592197f873299186127ff4c9f0385f`. Its accepted commit is already merged in `origin/main` at `be3ce90fa2d7013c0a383e44e6275f5ab492c53d`. The temporary checkout has 134 unstaged deletions, all paths involving hidden files/directories, and no modified/staged files. Preserve this historical checkout as found. Those temporary deletions are not product work and are not included in the Cline-folder consolidation. No data was discarded to clean it.

The two dependency commits use the required `gpt-6.1-sol-high@Codex` confidence trailer, with identity read from runtime metadata. The operator explicitly selected commit-message workspace signing. Dependency proof is in `/home/sharpninja/github/cline/docs/receipts/dependency-sync-20261021/verification.json`. Both dependencies track their own `origin/cline-qb`; they are separate repositories, not Cline branches.

The earlier [worktree inventory](/home/sharpninja/github/cline/docs/receipts/handoff-20261021/worktree-inventory.json) and [V2 inventory](/home/sharpninja/github/cline/docs/receipts/handoff-20261021/v2-worktree.json) are dated snapshots from before the branch moves and commits. Their dirty counts and old branch names are historical evidence, not current checkout state. The accepted P0 files, complete product checkpoint and primary work must be ancestors/content of `develop`; verify this with Git and the integration receipt.

The operator explicitly directed keeping generated exports local. `docs/Project/wiki/` and `docs/requirements/` are excluded through local Git `info/exclude`; project source, authored documentation and this handoff are committed. The generated files remain intact, including MCP-service-owned read-only manifests/documents. No ownership or permission change was made.

Other McpServer worktrees belong to other efforts and remain outside this task. Preserve the retained McpServer rebase stash `fb941200e66392b9926bf267d4791777b3831b29`. Never reset or clean unrelated work. Git commits and this consolidation preserve source histories; they do not close implementation acceptance gaps or authorize deployment.
## What has actually been achieved

### P0 and requirement reconciliation

P0's final acceptance is recorded in [task-progress.json](/home/sharpninja/github/cline/docs/receipts/p0/remediation-r2/task-progress.json): HV10 technical AGREE at 99 accuracy/99 completeness, full audit recovery/readback and canonical completion. Review directory: `/home/sharpninja/github/cline/docs/receipts/hv/20261010T225840Z-p0-r10`; reviewer session `9a3822e9-5d7c-49d8-924d-603f8621dd05`. P0 PR [#1](https://github.com/sharpninja/cline/pull/1) was merged October 11 at 00:52:00Z, merge commit `be3ce90fa2d7013c0a383e44e6275f5ab492c53d`. This handoff relies on the saved acceptance/merge evidence and current canonical P0 flag, not a new remote PR query.

The remaining-phase AC gap analysis was completed before subsequent coding. Later holistic reconciliation expanded the inventory to 20 FR, 20 TR, 76 TEST/AC records, 96 mappings, 228 criterion copies and 13 UCs. [Resolution verification](/home/sharpninja/github/cline/docs/receipts/holistic-review-20261013/resolution/verification.json) records 1,911 checks, phase allocation, and preservation of P0 acceptance. New product criteria stayed pending. These are requirement-alignment results, not proof their product behavior is implemented.

### P1 A8 binding and durable store

In the support worktree, `plugins/core/lib-ps/governed-turn-binding.ps1` and `governed-turn-store.ps1` exist. The store handles multiple delivered open turns, captured bindings, pending-to-minted aliases, durable consumer acknowledgments and restart lookup with a context-scoped interprocess lock.

The [latest A8 store proof](/home/sharpninja/github/cline/docs/receipts/p1/a8-lifecycle-20261014T195738/verified-20261014T201951/proof.json) records matching mock/production test and worker hashes: 72 cases, 920 checks, 848 expected negative-control rejections, 0 failed and 0 skipped. This includes 63 lifecycle and 9 storage cases. Real cases cover process termination around alias publication, concurrent distinct/duplicate ingress, corruption, wrong context, lock contention and existing/dangling symlinks. Earlier 71-case mock evidence preceded implementation; the first production failure and fix remain retained.

Binding regression records 41 cases, 351 checks, 310 negative controls, 0 failed and 0 skipped. These prove bounded local primitives. The proof explicitly has `liveIntegrationVerified:false` and `phaseAccepted:false`. S6, full migration, transport integration and a real governed application task are not established by these counts.

### Support rebase and Linux regression

The [rebase proof](/home/sharpninja/github/cline/docs/receipts/p1/linux-runtime-20261014T184544/rebase-verified-20261014T195312/proof.json) records the support branch's October 14 rebase to c4fff186 and retention of its original changes/stash. An automatic merge duplicated a traversal SafeHandle acquisition; restoring the exact original source and forcing a fresh build resolved two descriptor-lifetime failures. The affected Linux scope passed 117/117: 78 build tests and 39 server tests, 0 failures/skips. This is a focused scope, not a fresh full McpServer or P1 gate.

### Build/CI callers and storage isolation

The [CI wrapper proof](/home/sharpninja/github/cline/docs/receipts/p1/ci-wrappers-20261014T144220/increment-proof.json) records 20 reconciled CI command bodies, 649 contract checks, 434 rejecting controls and 0 contract failures/skips. Full VS Code Vitest reported 1,440/1,440 tests across 109 files. Bun unit reported 1,210 passed/0 failed across 90 files, but its aggregator did not report skips: **do not infer zero skips**. Three compiler checks passed without graph-observed source changes. The upstream package-script manifest was unchanged at that checkpoint.

The [native webview verification](/home/sharpninja/github/cline/docs/receipts/p1/ci-reconciliation-20261014T131045/webview-verification.json) records Linux VS Code 1.139.1 seed/verify/restart isolation: derivative and synthetic Cline data used separate origins/state, persisted across restart, and the synthetic upstream state stayed unchanged. Five input artifacts were unchanged. This is not all-host storage acceptance.

Earlier P1 work includes SDK tool/final-completion gates, private governance integration, signed workspace verification policy, server tool-execution validation, event/receipt identity checks, plugin artifacts and bundle groundwork. Their dated receipts remain under `docs/receipts/p1`. They must be checked against current sources and accumulated changes before reuse as phase evidence. In particular, no signed/pinned final bundle, full host integration or complete no-egress proof is inferred from a successful lower-level test.

## Exact unfinished increment and next implementation sequence

The interrupted increment is the PowerShell governed audit outbox:

- Contract: `/home/sharpninja/github/cline/.worktrees/mcpserver-qbrain/plugins/core/contracts/governed-audit-outbox.md`.
- Fixture draft: `/home/sharpninja/github/cline/.worktrees/mcpserver-qbrain/plugins/core/test-fixtures/GovernedAuditOutbox.Contract.ps1`.
- Saved context: `/home/sharpninja/github/cline/docs/receipts/p1/a8-outbox-20261014T202425`.
- Related audit identity: session `Codex-20261009T200426Z-plugin-session`, request `req-20261015T012425Z-a8-outbox`. Its old pointer is not proof of a live process.

The contract maps FR-QB-003/005, TR-QB-AUDIT-001, AC/TEST-QB-008/009/069. It specifies captured context, durable independent event identity/order, exact captured payload preservation, journal-before-alias application, context/receipt matching, bounded retry classification, retained terminal rejection/quarantine evidence, and no authority for the outbox to complete a turn. Read the full contract, verify consistency with current canonical requirements, and preserve its testable limits: process termination/restart evidence is not machine power-loss durability.

**No passing mock gate, production PowerShell outbox implementation, or Nuke outbox target has been established.** Only the contract and fixture draft were found; the source directory contains binding/store scripts, not an outbox implementation. An October 21 static check of the draft confirms parser errors 0 and analyzer warnings 4:

1. Automatic variable `$Event` is assigned at lines 72 and 88. Rename the fixture variable without changing semantics.
2. A non-ASCII fixture character lacks a BOM. Preserve the intended Unicode case with a justified representation compatible with the actual target runtime.
3. `Get-FixtureFiles` at line 113 violates the singular-noun rule.

Fix these fixture issues and independently verify the assertions before executing them. Current fixture parameters are `-Mode Mock|Production -ReceiptPath <task-owned-path>`, and it supports `-WhatIf`. Do not execute Production mode before a documented green mock gate and implementation. Temporary design scratch text previously said `create.lock`; the current contract and existing store use `.lock`. Resolve against source, not scratch notes.

When implementation resumes, the order is:

1. Reload profile and live memories, excluding 007; read the complete current plan and effective requirements through the plugin. Reconcile old proposed Node/Python tooling replacements against the October 21 exception. This exception is not permission to silently revise plan acceptance criteria or broaden product scope.
2. Inspect current branch revisions and any new dirty changes before continuing implementation. Keep unrelated work intact. Use current source and contracts to decide whether old outbox assumptions remain valid after the first-party-tooling clarification.
3. Finish the outbox test contract and green mock/negative-control gate; then bind the same assertions to the approved implementation. Preserve original failed evidence and chronological hashes. Cover alias recovery, retained blockers, receipt identities and retries rather than just event counts.
4. Complete A3/A5/A8 legacy migration, transport integration, alias acknowledgments, durable event replay and explicit decision triples. Do not infer steering relationships from similar text; captured native/root IDs must trace each request to its session and actual host lifecycle.
5. Deliver S6 `mintTurn`, `recordReview` and lossless normalization support via the supported plugin/REPL boundary. The coordinating Claude agent will not deliver it. Keep Codex's ownership and Claude's independent review role distinct.
6. Produce and verify the required Linux runtime/module/REPL/plugin artifacts and signed policies. Wire actual shipped host startup before any storage-bearing import/daemon startup. Rebuild SDK `dist` when changing SDK sources, then restart consuming processes.
7. Demonstrate an actual governed Linux startup, task and exact audited request/result with provider credentials handled privately. Verify storage isolation, trust failures, policy/tool/completion enforcement and no Cline Cloud behavior. A mocked task or `--version` is not this milestone.
8. Finish the full P1 requirement/host/runner matrix and cumulative validation, with explicit failure/skip accounting and upstream-edit necessity review. Respect the operator's Linux-first deferment; do not silently mark deferred platforms passed. Resolve how later platform evidence satisfies the final gate before claiming full acceptance.
9. Perform the complete introspective/holistic self-review, freeze inputs, then request actual Claude phase HV through its required plugin. Persist request JSONL, full response stream, full verdict and server readback. Source-verify findings and sweep for sibling defects before the next round.
10. Only after all phase gates and canonical readback agree, create phase/support PRs. Attach them to the chat. Wait for their merges before P2. No P1 PR or phase acceptance is claimed here.

## Open risks and unresolved evidence

- Full governed positive startup/task evidence is still missing in the latest canonical checkpoint. Historical compiled-startup tests failed with "Hub governance identity is not initialized by trusted host bootstrap." Do not weaken trusted identity requirements to make them pass. Later component successes do not close this gap.
- Historical full-suite failures and platform gaps need current disposition, not deletion from status. Some old failing tests were subsequently repaired; use the current evidence chain before calling them still failing or resolved.
- A prior operator-cache fixture incident had `originalRestored:false` and original data unknown. This handoff found no new restoration/disposition evidence. Retain it as unresolved for phase exit unless a fresh authoritative receipt proves otherwise.
- Three superseded BUG-TRIAGE-139 evidence tests had explicit operator-approved retirement. That approval was exact and bounded; it does not allow retiring arbitrary upstream tests. Existing immutable-receipt checks and current-index checks remain.
- PowerShell named-pipe errors were observed inside the sandbox and absent outside it. That comparison establishes a sandbox effect, not which restriction caused it. The requested network-only controlled comparison is not declared resolved here. Neither old probe established compiler/NuGet behavior. Keep the diagnostic report local and distinguish it from product bootstrap failures.
- The October 15 LEGION2 reboot request has failure receipts and an interrupted later attempt, but no verified reboot completion in the inspected directories. Do not claim the machine rebooted, and do not trigger another reboot solely from this handoff. The relevant folders are `docs/receipts/operations/legion2-reboot-20261015T124143` and `docs/receipts/p1/legion2-reboot-20261015T124304`.
- Host process inventory was limited to this tool's process namespace. It cannot prove that no reviewer or build runs elsewhere. Inspect actual handles/receipts before dispatching a duplicate review. No reviewer was launched during handoff preparation.
- Historical exact-text recovery covered 128 operator messages and 268 final responses (396 records) with explicit retrospective/native identities. Do not describe recovery as proof the original turns were logged on time, and do not revert approved dash normalization.

## Profile, memories, tools and operational receipts

Use [add-profile](/home/sharpninja/.codex/skills/add-profile/SKILL.md) on resume/compaction. The October 21 update requires full profile reads plus live Global and active Workspace memory reads every invocation, with pagination/full text checks and 007 filtered before display. A cached merged profile is not a substitute for live memories. The skill reports counts and explains how they affect the current task.

This handoff reload read the single `/home/sharpninja/.claude/profile/PROFILE-MERGED.md`, three Global memories and one Workspace memory. Included IDs are Global `MEMORY-INFRA-001`, `MEMORY-PROCEDURE-001`, `MEMORY-PROCESS-009`; Workspace `MEMORY-TOOLING-001`. 007 was excluded. The full filtered readbacks are preserved locally beside this handoff's receipts. A helper's single-item `.Count` reporting overstated the Workspace excluded count; the loaded Workspace IDs contain exactly the one tooling memory, and 007 content was not loaded. Do not reuse that incidental excluded-count display as a server record count.

The Global lab memories document working SSH and source sharing. LAB-OMARCHY source is under `/home/sharpninja/github`; LEGION2 source is under `F:\github`, reachable from Linux at `/mnt/legion2-github`. Use the documented SSH alias `PAYTON-LEGION2`/`LEGION2` when the task needs it. Credentials/key material remain private. Do not print or copy credential contents into this handoff, receipts or chat.

Active Codex plugin manifest: **1.121.0**, root `/home/sharpninja/.codex/plugins/cache/mcpserver-codex-plugin/mcpserver/1.121.0`. Trust verification on October 21 at 12:13:57 local succeeded for marker signature and nonce after the restricted sandbox denied network access. Do not treat `-Command Status` alone as fresh health verification; it primarily reports cached state in this installed version. Use the plugin bootstrap for trust and its wrapper for all canonical mutations/reads, not raw REST.

Reusable temporary helpers, inspect before using:

- `/tmp/qb-profile-memory-session.ps1 -Mode Library` defines `Invoke-ProfileWorkflow` using the installed plugin and explicit Cline workspace. Dot-sourcing its parameters can shadow caller variables; save caller parameters under distinct names first.
- `/tmp/qb-check-audit-script.ps1 -Path <script.ps1>` runs PowerShell parser and PSScriptAnalyzer Warning/Error checks. A returned exception is a failure, even when parser count alone is zero.
- `/tmp/qb-read-local.ps1` and `/tmp/qb-search-code.ps1` support bounded file reads/searches. Prefer `rg` for discovery. Avoid output truncation when claiming a complete read.
- `/tmp/qb-handoff-audit.ps1` owns this handoff's audit cache and exact-result readback. Its durable pointer is [turn.json](/home/sharpninja/github/cline/docs/receipts/handoff-20261021/turn.json).

The main conversation session is `Codex-20261009T200426Z-plugin-session`. The original handoff replacement request was `req-20261021T171556Z-replace-handoff`; current develop integration has its own request pointer in `docs/receipts/develop-integration-20261021/turn.json`. The server requires `req-yyyyMMddTHHmmssZ-slug` even though the operator prefers local display/file timestamps. A local-format handoff request was rejected before persistence; its failsafe remains. The successful retry uses the server's required UTC ID while this document and chat use measured local time. Do not relabel a local time with Z.

Audit caveats: the earlier plan-viewer turn `req-20261021T170412Z-open-current-plan` was interrupted while closing; a helper parameter-shadowing defect prevented completion before the later request. Its actions and prepared result must not be represented as proof that an exact final was delivered in chat. This handoff carries the actual plan result. Do not claim all historical session logging is clean merely because this handoff's request/result readback passes. The old runtime cache under `.mcpServer/qb-task-runtime-20261013/mcp-cache` retains earlier interrupted state and should not be reset blindly.

The tooling-memory creation was independently read back with exact text/scope/ID and exact request/result audit: [verification](/home/sharpninja/github/cline/docs/receipts/handoff-20261021/tooling-memory-verification.json). The add-profile skill edit has an original retained at `/home/sharpninja/.codex/skills-backups/add-profile/20261021T113436/SKILL.md`. No memory mutation occurs merely from a reload.

## What to verify before any next completion claim

Read the canonical TODO again, current effective requirement records and the relevant complete receipt bodies. Confirm scope, source hashes, native runtime identity, failure/skip counts, and reviewer/audit provenance. Confirm every governing requirement has adequate ACs and every claimed result has its named evidence. Keep original operator decisions, upstream working behavior and established idempotency/outbox patterns as comparison anchors. No new mechanism should be introduced just to silence one review finding.

This handoff has one current state and one ordered continuation path. Historical receipts remain evidence only. The previous active handoff text has been replaced, not appended or left as a competing active handoff. Product acceptance and all pending phase gates remain unchanged.
