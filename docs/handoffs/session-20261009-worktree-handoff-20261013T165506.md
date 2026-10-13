<!-- SPDX-License-Identifier: Apache-2.0 -->
# Handoff: worktree session-20261009 (branch worktree-session-20261009), 2026-10-13T16:55:06 local

Prepared by ClaudeCode (model claude-opus-5-5) for Payton Byrd at the operator's request "write a COMPLETE handoff for this worktree." All times are local US/Central (-05:00) in .NET "s" form unless marked Z. Facts marked "measured" were read from the live system between 16:40 and 17:08 today; facts marked "earlier" come from this session's earlier work and were not re-measured for this file.

Scope: this project workstream only. Operator profile maintenance is a separate effort and is not part of this handoff (operator, 2026-10-13: "The project handoff and profile maintenance are completely different efforts.").

This file supersedes `docs/handoffs/sessionturn-review-plan-20261009T022141Z.md`, which remains the detailed history of the pre-merge plan (plan-readiness rounds 1 to 43, 2026-10-09 to 2026-10-10) but is stale on status: it says the plan is approved, and the merged plan is not.

Copy everything below the line into the next agent.

---

## 0. Status in brief (read first)

- The merged plan `PLAN-SESSIONTURNREVIEW-001` (Phases A, B and C) is NOT APPROVED. A code freeze holds: no new code under this plan until the operator approves the merged plan (operator, 2026-10-11: "No new code until I approve merged plan."). The one exception is the operator-directed analyzer branch `chore/netanalyzers-all`.
- The architecture is the authority and the plan derives from it (operator, 2026-10-12: "The plan should depend on the architecture, not the other way around.").
- Architecture hostile validation round 2 finished: DISAGREE, accuracy 95, completeness 94, confidence 98; 12 PASS, 6 FAIL (AR1, A2, M1, M3, M4, M5), 0 UNKNOWN; six implementer fixes AR2-F1 to AR2-F6, no operator-decision failures (section 3.3). Its receipts are committed in `645b94ed`.
- Next workstream step, by operator ruling: present the round 2 findings together with the projected problems that were never presented (the round-5 reconciliation, preserved in this worktree, section 4) and get the operator's confirmation before any fix or round 3. Operator: "Let round two finish, and we will deal with its findings when they come in."
- The `/goal` ("HV Agree on Updated Architecture") was paused by the operator ("/goal pause").
- Session logging: the plugin's UserPromptSubmit hook has been cancelled on every prompt since 18:28:51Z (13:28:51 local); turns are being opened by hand (section 5).

## 1. Starting a session in this worktree

1. Run `/add-profile` and read the operator profile in full, including after every compaction or resume, as the first action. A tool result saying "already loaded, instructions unchanged" is never a reason to skip the read.
2. Follow `CLAUDE.md` session start in full: read the whole marker `AGENTS-README-FIRST.yaml` (never partially), bootstrap the plugin `mcpserver-local` 1.118.0 (`~/.claude/plugins/cache/mcpserver-local/mcpserver/1.118.0`), verify signature, `/health` and nonce, review history and TODOs, open a turn.
3. Standing rules that bite here (all in the profile): PowerShell only; Bash only launches `pwsh -NoProfile -NonInteractive -File <script.ps1>` with literal arguments; no Python and no Node anywhere; every script through the PowerShell parser and Invoke-ScriptAnalyzer (Warning, Error) before it runs against data, and `-WhatIf` first for state changes; no tables and no em or en dashes in output; sortable local timestamps, measured, on every reply; never print or commit marker keys or credentials; never force-push; never move a file (copy, verify, keep the original until the operator clears deletion).
4. HV reviewer: HV must not be run by the requester's agent family; if the operator has not named one in the session, ask which agent and model to use. Rounds since 2026-10-12 used Codex `gpt-6-astra` at effort `max` (operator-specified then; superseded by the 2026-10-13 rule to ask). Review time is never bounded.

## 2. Workspace and git state (measured 16:51:22, commits measured 17:07:44)

- Worktree `/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009`, branch `worktree-session-20261009`, upstream `origin/worktree-session-20261009` (open PR #78).
- Committed on 2026-10-13 at the operator's "commit everything": `645b94ed` (this handoff, the round-5 reconciliation copies, the superseded note on the 2026-10-09 handoff, the architecture round 2 receipts) and `7872ef16` (the two pre-existing benchmark result files, whose only change is `bench_workspace_id`, and `.claude/launch.json`, a preview config that points at a session scratchpad script and works only in that session). Before them, HEAD was `c3e64d9a` (2026-10-13T12:44:47, "docs: fixes for architecture HV round 1 findings AR1-F1..AR1-F9 and A4").
- Against upstream: 0 behind, 15 ahead (`bae3062c`, 2026-10-11T19:18:55, "docs: merged plan, PowerShell-only plugin core and four-phase execution", through `7872ef16`); all are docs, receipts and the two files above. Against `origin/develop`: 12 behind. Against local `develop`: 0 behind. Against `origin/main`: 1 behind.
- Push disclosure: the unpushed commits include large receipts, about 150 MB (earlier), of which about 50.5 MB are rendered diagram pages; today's two commits add about 24 MB (measured, 41 files). Re-measure and tell the operator before any push. Ask before pushing to `origin/develop`.
- Other worktrees of this repository (measured): main checkout `/home/sharpninja/github/McpServer` at `f56dcf70` [develop]; `.claude/worktrees/agentcontext-doc-20261011` at `a8d3934b` [docs/agentcontext-plan-20261011]; `.claude/worktrees/namespace-match-folder` at `0a6453d7`; `.claude/worktrees/netanalyzers-all` at `fe5879ba` [chore/netanalyzers-all] (the analyzer branch; the plan records 515 uncommitted paths there on 2026-10-13); `/home/sharpninja/github/McpServer-mcp-session-recovery` at `b53ce6b8`; `/home/sharpninja/github/cline/.worktrees/mcpserver-qbrain` at `f56dcf70`; a prunable `/tmp/mcpserver-serviceupdate-validation/McpServer`.
- Preserved stashes (from the 2026-10-09 handoff, not re-verified): main checkout `3ba6fc5413d5bb9fa68d7c312229512da0d86832`, plugin repo `9544db6940b30d6c695aace71eb081cd4638ddb0`. Never apply or drop them without operator direction. The stash stack is shared with every worktree.

## 3. The workstream: merged plan and architecture

### 3.1 Documents (measured)

- Architecture (authority): `docs/architecture/SessionTurn-Review-AgentContext-Architecture.md`, 877,709 bytes, written 2026-10-13T12:43:47. Sections: goals, system context, cross-cutting conventions and contracts (3; shared components in 3.5), Phase A turn logging (4), Phase B request class and reviews (5), Phase C stateless agent context (6), use cases (7), delivery in four execution phases and parallel domain coding (8), diagram index (9), Addendum A entity diagrams.
- Plan (derived): `docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md`, 927,837 bytes, written 2026-10-13T12:26:30. Its first status line reads "Status of the merged plan: NOT APPROVED, awaiting operator approval". Locked decisions are recorded in dated sections (2026-10-09, 2026-10-11, 2026-10-12, 2026-10-13). The 2026-10-13 ruling: requirements are catalogued and worked by problem domain ("Catalog and work by domain (Recommended)").
- Stateless agent context specification: `docs/Stateless-Agent-Context-Management.md`, 439,345 bytes (its shared sections must stay byte-equal to the architecture; round 2 confirmed they are).
- Diagram render archive for round 2: `docs/receipts/diagrams/architecture-merged-r2-20261013T174051Z/` (213 Mermaid blocks, 322 letter pages; render standard: letter pages, sequences landscape, straight lines, 8 to 12 point text, tile when needed).

### 3.2 TODO state (measured 16:53 through `workflow.todo.get`, main workspace)

- `PLAN-SESSIONTURNREVIEW-001` (high, done false): 30 implementation tasks, 29 open (P0.1 to P0.9, A1 to A9, S1 to S11 labels). Discrepancy to fix with the operator's direction: its `note` still says "APPROVED by the operator on 2026-10-10" and its `technicalDetails` cite the pre-merge plan source `/home/sharpninja/.claude/plans/create-a-new-plan-giggly-music.md` and the old handoff; the merged plan is not approved. Edit TODOs only through the plugin, never `TODO.yaml`.
- `BUG-BUILD-005` (ConfigValidator parses the instances of the live appsettings.yaml) and `BUG-BUILD-008` (sync the official plugins' lib-ps to the canonical core): each has one open task, the HV pass before done. Their HV-time snapshots are in the round 2 receipts (`20261013T174659Z-todo-BUG-BUILD-005.json`, `-008.json`).
- `PLAN-BUILDWHATIF-001` (`./build.ps1 -WhatIf` as a full dry run with checks): 4 of 4 tasks open; design awaits the operator's decisions D1 to D4.
- `MAINT-CIREMOVAL-001`: 3 of 6 tasks open, including operator decisions on the GitHub Actions run API feature, `azure-pipelines.yml`, and the CLAUDE.md CI/CD section.
- `QB-AGENT-001`, `PLAN-LINUXEXPORTCREATE-001` and `MAINT-LIBSHRETIRE-001` returned not found in the main workspace store (QB-AGENT-001 is Codex's workstream; MAINT-LIBSHRETIRE-001 is proposed in the plan and not yet created).

### 3.3 Hostile validation history and the open round 2 verdict

- Merged-plan readiness rounds (commits): round 2 DISAGREE 90/87 (`1f77feb7`), round 3 DISAGREE 91/89 (`17ef6bde`), round 4 DISAGREE 92/90 (`c1f2d527`, fixes `89859efc`).
- Architecture round 1: DISAGREE 91/89 (`6bbd7534`); fixes for AR1-F1 to AR1-F9 and A4 in `c3e64d9a`.
- Architecture round 2 (reviewed HEAD `c3e64d9a`; Codex gpt-6-astra, effort max; reviewer session `Codex-20261013T175033Z-architecture-merged-r2`, request `req-20261013T174659Z-architecture-merged-r2`; result written 2026-10-13T18:34:25Z): DISAGREE 95/94/98. Receipts: `docs/receipts/hv/hostile-validator-architecture-merged-r2-20261013T174659Z.md` and `.json`, request `20261013T174659Z-architecture-merged-r2.request.jsonl`, response `.response.jsonl`, evidence ledger `.evidence.jsonl`, final verdict `20261013T174659Z-final-verdict.json`, persistence proof `20261013T174659Z-persistence-proof.json`.
- Round 1 findings re-attacked in round 2: resolved AR1-F1, AR1-F2, AR1-F3, AR1-F4, AR1-F5, AR1-F7, AR1-F8, A4; not resolved AR1-F6, AR1-F9, and the inherited R4-F8 and R3-F6 (all through AR2-F1 and AR2-F2).
- The six findings (all implementer edits; each receipt section names exact architecture lines):
  - AR2-F1, first-ingress durability: the first-ingress path writes the failsafe mint record before it durably records a new `mcpSessionId`, so a crash can lose the session binding. Required: persist the session-ID allocation as a completed atomic state write before the write-ahead record and before any open or mint call; reconcile the one-replace and lock wording and both diagrams; add crash cases after ID persistence, after the write-ahead record, and after a lost open or mint reply.
  - AR2-F2, turn-write isolation: the shared `TurnWritePipeline` activity still ends with "summary refreshed" inside `SerializableWrite`, contradicting the single-turn transaction that reads the parent row only. Required: remove the summary refresh from the single-turn pipeline; draw it as a separate post-commit shared call whose failure is logged and swallowed; keep Submit mode's refresh in its session transaction; align the sequences and the three-provider isolation and refresh-failure cases.
  - AR2-F3, GraphRAG projection repair: `ReconcileProjectedDocuments` returns a row with a missing document to Pending without clearing `GraphRagDocumentId` or `ProjectedContentSha256`, and `ProjectOneResponse` skips on hash equality, so the row is never re-ingested. Required: clear the obsolete ID and hash on reset (or require a confirmed Projected document for the skip), apply the invariant to every document-losing transition, and add a two-pass re-ingest case.
  - AR2-F4, supersede retry: a repeat of an applied supersede returns 409 once the replacement's status changes, contradicting "a repeat naming the same replacement returns 200 unchanged". Required: inside the serialized operation, recognize the already-applied original and replacement pair first and return the stored result; check replacement eligibility only for new transitions; add retries after the replacement is superseded or rejected.
  - AR2-F5, token budget: rendered finding lines are not charged to the bounded package budget, so a package can exceed its bound. Required: estimate over all rendered content (headers, separators, finding lines), reserve mandatory overhead before the overflow decision, account for optional-item-induced findings during the fill, and add exact-boundary cases with a contradiction and a blocker.
  - AR2-F6, DRY component: the post-commit response-effects orchestration (dependent selection, Mode check, staleness before projection, failure handling) is repeated across Grade, Reject, Supersede and EvaluateStaleness without a section 3.5 component. Required: one named post-commit response-effects component in 3.5 with owner, namespace and folder, input, output and error contract, and one test class; each committing unit calls it once.
- Fix discipline (operator profile): fix the deficient requirement or AC first, add no design mechanism to plan text, make each fix minimal, attack it before the next round, run the propagation audit across architecture, stateless document and plan, re-render changed diagrams to the standard, and do a holistic end-to-end read before launching round 3.

## 4. Projected problems not yet presented to the operator

- On 2026-10-12 a five-lane read-only reconciliation (requirements A and B, requirements C, server code, plugin code, risk) produced `RECONCILIATION.md` and its lane reports. They were never presented to the operator before HV, against the operator's ruling that every projected problem is presented and confirmed before a review is launched.
- They lived only in a session scratchpad under `/tmp`. They are now in this worktree, each with an SPDX header line added and otherwise byte-identical: `docs/handoffs/session-20261009-recon-20261012/` (`BRIEF.md`, `RECONCILIATION.md`, `req-ab.md`, `req-c.md`, `code-server.md`, `code-plugin.md`, `risk.md`; credential scan 0 hits).
- They describe round 5 of the architecture (2026-10-12), before the domain reorganization and both architecture HV rounds. Many items may since be addressed (round 2 passed A3 namespaces and file rules, A5 reconciliation and test churn, M6 code freeze and analyzer exception, M9 requirement ids). Re-check each against the current architecture before presenting, and present what remains.
- Their headline items: WorkspaceResolutionBehavior through the singleton dispatcher (blocker); analyzer-branch namespace, file-per-type and underscore test names (blocker for compile if merged first); plan not re-derived; thirteen cross-part inconsistencies; whole-session Serializable scope against independent turn edits; nested dispatch joining the caller's transaction; SQLite write transaction across retrieval; the completion gate breaking about 120 server-path tests; serial HV gates on the critical path; client package public API growth and migration risks. Four operator decisions were listed (analyzer branch and naming; workspace-resolution scope; Serializable scope; code-freeze exception).

## 5. Session log state (measured 16:53 through `client.SessionLog.QueryAsync`)

- Current session: `ClaudeCode-20261013T174639Z-plugin-session`. The session rotates on compaction; re-read the active session after any compaction and never hardcode the session id.
- Other ClaudeCode sessions today: `ClaudeCode-20261013T022610Z-plugin-session` (45 turns), `ClaudeCode-20261013T142651Z-plugin-session` (22 completed), `ClaudeCode-20261013T171452Z-plugin-session` (1 failed; an extra session from the twin-session defect).
- Project turns still in progress, to close with their true outcomes:
  - `req-20261013T024550Z-problem-domain-split` (session 022610Z): the problem-domain split, delivered in `bff6a7aa` and the 2026-10-13 locked decision.
  - `req-20261013T192821Z-bg-never-skip-logging`: operator rule "BG agents should NEVER skip logging. NEVER." The backfill below is not done.
- The same session also holds turns of the separate profile-maintenance effort; that effort tracks them.
- Defects in force (triage filed):
  - The UserPromptSubmit hook is cancelled at its 10-second timeout on every prompt since 18:28:51Z, so no turn opens automatically (`triage-report-124a374ca6c0440e945a3e0275b90204`). Open turns by hand: `client.SessionLog.BeginTurnAsync`, then `client.SessionLog.PatchTurnAsync` (additive; collections append), through the plugin's `lib/repl-invoke.ps1` invoked in-process with `&`, targeting by explicit requestId, and verify with `client.SessionLog.QueryAsync`.
  - Each SessionStart creates two sessions, one empty (`triage-report-d9bd6fc3276f4facb1d3ced6ba0682a1`).
  - The FR-MCP-TRIAGEPLUGIN-001 background-agent skip in `plugin-hook.ps1` (about lines 725 to 750) contradicts the operator rule that background agents always log (`triage-report-061f30e90031406d8e0c56bc35ee07c7`).
  - `PatchTurnAsync` intermittently fails with "The change could not be saved" (2026-10-13T16:36 to 16:40; later writes succeeded) (`triage-report-17b0cad8ec844a29b6e5f12c58be0b50`, group `triage-group-00445a5daec612ee`).
- Practical notes (earlier): the server refuses `filesModified` paths outside the workspace root unless prefixed `foreign:`; a `-WhatIf` dry run that reaches `repl-invoke.ps1` leaks `$WhatIfPreference` into the plugin's temp cleanup and leaves `.mcpServer/claude/envelope-req-*.tmp` files, so skip the plugin call under `-WhatIf`.
- Earlier this session: all 555 turns since 2026-10-09 were filled (interpretation, response, actions, decisions, files, context, dialog) and audited; 21 compactions occurred, none followed by add-profile as the first action.
- Not started: count every background agent run since 2026-10-09 that did not log its own turn, and backfill those turns (operator rule above; ClaudeCode's own subagent briefs had told agents to stay out of the session log).

## 6. Requirements and identifiers pending under the freeze

- "Local everywhere" (operator, 2026-10-13): identifiers and file names that carry a time use sortable local time. The workspace contract still mints UTC ids (`<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`, `req-<yyyyMMddTHHmmssZ>-...`) and HV receipts use UTC stamps. Changing them is a requirement change: it needs its own BDPv4 plan and the operator's approval, and the code freeze applies.

## 7. Coordination and other agents

- Coordination with Codex (QB-AGENT-001, which shares `plugins/core/lib-node` and server session-log files) runs only through global memory `MEMORY-PROCESS-007` on the LAB-OMARCHY server, append-only with read-back. Read it when the operator asks.
- The analyzer branch (`chore/netanalyzers-all`, worktree `.claude/worktrees/netanalyzers-all`, head `fe5879ba`) is the operator-directed exception to the code freeze; the architecture (8.1) has it merge first, after which P0.1 re-verifies every source anchor at the approval HEAD.
- Windows evidence for product work runs on PAYTON-LEGION2 over the operator's WSMan route in a worktree under `F:\GitHub\McpServer\.worktrees\`, with `git -c safe.directory=...` per command.

## 8. Next steps, in order

1. Session start (section 1), then close or complete the open project turns in section 5 with their true outcomes, and open a turn for every request.
2. Present to the operator, in chat: the six round 2 findings with their required changes (section 3.3), the PLAN-SESSIONTURNREVIEW-001 note discrepancy (section 3.2), and every round-5 projected problem that the current architecture still has (section 4). Get confirmation before changing anything.
3. Apply the confirmed fixes as minimal architecture, stateless-document and plan edits, with the propagation audit, the structured what-if pass, diagram re-render to the standard, and a holistic read of all three documents.
4. Ask the operator which agent and model run round 3, then launch it with request and response jsonl receipts, the reviewer's own session-log turn, the full verdict in the session log, and no time bound. Iterate to AGREE with accuracy and completeness of at least 98 and every claim PASS.
5. Present the merged plan for approval; after approval, sync the PLAN-SESSIONTURNREVIEW-001 TODO (description, note, technicalDetails) through the plugin and begin P0.1 under the plan's gates.
6. Backfill the session-log turns of background agents that did not log (section 5).
7. Plan the "Local everywhere" identifier change separately (section 6).
8. Before any push, disclose the receipt sizes; never force-push.

## 9. Do not

- Write product code under this plan before the operator approves the merged plan.
- Launch HV round 3 before presenting the findings and projections and getting confirmation.
- Edit `docs/Project/TODO.yaml`, session-log storage or requirement storage directly; use the plugin wrappers, never raw REST.
- Run a wiki-mode requirements import (it deletes records).
- Commit marker files, credentials, or unrelated workload names.
- Move files, delete originals without clearance, apply or drop the preserved stashes, or force-push.
- Mark any TODO, plan step or requirement done without hostile AGREE at 98/98 with every claim PASS.

## Appendix A. Triage report ids seen in this session's transcript (65)

The four filed on 2026-10-13 are described in section 5. The 2026-10-09 handoff describes many of the earlier ones; titles of the rest are in the triage store.

`triage-report-02e7a0198d064403a85b13efa6273637`, `-04659db176a8473bbf2aa9b6caf1f98a`, `-061f30e90031406d8e0c56bc35ee07c7`, `-1102310fe4b84f5c8084bfbbde99b92c`, `-124a374ca6c0440e945a3e0275b90204`, `-13f095f4df4e485db5f8f634ea52ce60`, `-17b0cad8ec844a29b6e5f12c58be0b50`, `-1a283182658245f3a28a7bf36a4d0fe2`, `-1a6f4a03884c4f71a8d152d753e1b223`, `-20d8e79fabd54f7b9ae5c9a12a42bd48`, `-2727498f929a4dad92fd33ae85553c2e`, `-3081edb1d0414e8fb737738438cd3ccd`, `-3546932eb5e94bbc8602fcb586cf5d5b`, `-36d2da231add47f18af5abd35a340e5f`, `-410efece19aa4877931d88b4aa9f0928`, `-41d7d27c53f2464f8339fe963079a55e`, `-43f0a95a04cf4374ac97e88146e2b1f7`, `-51008bde605f47cc8e5a11010b1bca00`, `-52e8098cd299475d9922098f00d818b6`, `-53dc4c8b359d496ea5327841deb1ea45`, `-556685b012d64f08b379f4b24950ebda`, `-5b07fc9222b543b0810889bc9fcc31e2`, `-5c7278cd1a9543f49bd92da2b9700f67`, `-5d4da242d23d40a4acab78727b222bf4`, `-5e2c5c5f86d043a388788cf2f98ae6e6`, `-611ca1b413ea44ff946672f79bd7e850`, `-629497bab74c4676823aebbccfb58cc0`, `-73f6a051c3464b409a7c791b1a0f71fc`, `-75290be6d6b749119a7819fba3cca386`, `-78b6efa1fdea433482f56df6710307a4`, `-7c84e6437f7b42d0a67fbe32679a686a`, `-80d6f90c612e48d2ac2430143e4f1e5e`, `-834cec2ebfaa4ab3935120bea88f700e`, `-8509a6345e6c408a981ade489513fa2b`, `-914a281216354c559a46f6304cea2f2f`, `-93757591e2eb44aebed232bd1689940d`, `-953f44d7574942419e6d5d3123acb971`, `-96ea2f714e1b45cdba27bf6f92dcb9b9`, `-9ede09f8a35e4d17bb75ba725c4e216a`, `-a03b85ac70594a298fa9d56eca566492`, `-ab06ac3befed4ff99725ef6047715158`, `-ae1ea81e32b640cbabcfd41c3905377e`, `-b1b83f89743446aa8553f8a5a2b81c21`, `-b72cfaf4265f4fef84f731e974802737`, `-b9f345e7ace7420a8aa2761f1fcac3e9`, `-bd1f438638f64f808e53a407aeb58c98`, `-c03d190f194b4a76a4923ca626adbb41`, `-c381e4c99d284e0dbc1ec443c221ca59`, `-cdf116adf956449fb0a8e576bd572f26`, `-d0221e7955d045ada07181e6f9b1cb31`, `-d3bcb648a72642f0a52c68c801316a0f`, `-d8c40d80a4ba454e8b5b96b9b5762105`, `-d9bd6fc3276f4facb1d3ced6ba0682a1`, `-dbbbec24a03a4d70856cfe9c5fd7ab80`, `-de98c84b64ce497f976ff498fdb35182`, `-e1f01430477f441fb86538469d15fc15`, `-eb56d4ebc48c49c8a0ec6015346a9ed7`, `-ed3b0381bd2948e58d6315f6e1601b02`, `-f19cdc9bf84d4e949ad7a56f0736a31a`, `-f2684f9067d7465797a44b92c41e01d6`, `-f4d47e1f85b746de8ce3f5926103711f`, `-f4e08770ed6549669d2421415c06d41b`, `-f68f13b8864b4905adbdf008eb139991`, `-fbac91dc9ac24cd2bc444294150b7210`, `-fed753f7ec23496893a8bae0de0fefcc`.

## Appendix B. Ephemeral working files

The session scratchpad (`/tmp/claude-1000/-home-sharpninja-github-McpServer--claude-worktrees-session-20261009/49428485-c8ea-46d7-86c6-58add8faed97/scratchpad/`) can be wiped at any time. Nothing in this handoff depends on it: the recon reports are in this worktree. Useful patterns there, if it survives: `open-missed-turns.ps1` (hand-open and fill turns from a `.psd1`) and `audit-session-log.ps1` (paged session-log audit).
