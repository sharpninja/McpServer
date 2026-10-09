# Handoff: Session-turn logging, request class, and turn reviews plan (NOT APPROVED)

**Active workstream handoff.** Prepared by ClaudeCode (Opus 5.5, effort xhigh) on 2026-10-09 UTC for Payton Byrd, after the operator declined plan approval and directed: "Write plan to high todo and create handoff (remove existing handoffs in worktree)".

Copy everything below the line into the next agent.

---

## Status (read first)

- The plan is **NOT APPROVED**. Do not implement any step, including P0 (requirements, use cases, TODO creation), until the operator explicitly approves it. The operator may still be typing corrections.
- Authoritative copy: MCP TODO `PLAN-SESSIONTURNREVIEW-001` (priority high, section SessionLog, done=false). Retrieve with `workflow.todo.get` `id: PLAN-SESSIONTURNREVIEW-001`.
  - `description`: the full plan text. Verified on 2026-10-09 after the round-12 sync (2026-10-09T21:44Z): all 529 non-empty lines of the source file are stored exactly (PowerShell `Get-Content` count, non-empty = non-whitespace); the server drops blank lines.
  - `implementationTasks` (30): operator approval, P0.1-P0.9, A1-A9, S1-S11.
  - `note`: the not-approved status. `technicalDetails`: session, worktree, and baseline facts.
  - FR/TR arrays are empty on purpose. The new FR/TR/TEST ids are created in P0 and do not exist in the store yet.
- Local source of the plan, on PAYTON-OMARCHY only: `/home/sharpninja/.claude/plans/create-a-new-plan-giggly-music.md` (649 lines, 529 non-empty, PowerShell `Get-Content` count after the round-12 sync (2026-10-09T21:44Z)). If the operator corrects the plan, update this file and the TODO description together, and verify the TODO round trip.

## What the plan does (one paragraph)

It makes MCP session-log turns trustworthy (Phase A, ships first), then adds structured review data (Phase B):
- **Phase A.** One turn per user request, including messages sent mid-turn, with a server-issued request token minted at ingress through a new mint route (D1), queued messages minted at enqueue by an arrival bridge (D5). Writes bind to their request automatically through host context, and are refused rather than guessed when the host session is ambiguous (D2). System events never open turns. A new prompt never cancels an open turn. Turn edits are independent per turn. Write receipts are truthful (`skipped_terminal`). The stop gate never auto-closes turns.
- **Phase B.** A `requestClass` turn field (Code, Docs, Chore) and two review records per turn (`agentReview`, `hostileReview`). Each record carries:
  - a status lifecycle: Unnecessary, Unstarted, Running, Pass, Fail, Other;
  - `otherNotes`, writable only when status is Other and required (non-empty) for Other (D4);
  - lossless decimal correctness and completeness (0..1);
  - server-derived Pass/Fail from per-workspace thresholds (default 0.98, inclusive, both scores must meet theirs), with the thresholds stamped on the record;
  - `reasons[]` and receipt paths.
- **Completion gate (server-enforced).** A turn completes only with a request class and a terminal agent review; for Code and Docs it also needs a terminal hostile review.

Scope covers the server, client, REPL, MCP tools, the PowerShell and Node plugin cores, and all nine agent plugins. Those are claude-code, claude-cowork, codex, copilot, grok, grok-bot, cline, cline-v2, and opencode.

## Locked operator decisions (do not re-litigate)

The full list is in the plan, section "Locked operator decisions (2026-10-09)". Key points:
- Every user request is exactly one turn, including messages queued mid-turn. Actions and decisions map to the turn of the request that caused them.
- Operator decisions on 2026-10-09 after Codex plan-readiness round 1: D1 the server issues the request token when a message arrives; D2 writes are tied to their request automatically through the agent app's hooks; D3 the use-case coverage gate checks only this plan's requirements; D4 a review with status Other requires the Other reason (`otherNotes`). After round 2 the operator chose D5: queued mid-turn messages are minted at enqueue by an arrival bridge ("Arrival bridge"). After round 3 the operator chose D6: hosts without an arrival mechanism are unsupported for turn logging, with no first-sight waivers ("Adapters or unsupported"). The operator then confirmed D7 (plugin equivalence across all QBrain.AI plugins, limited by each host's plugin infrastructure, with limitations documented in `docs/AGENT-PLUGIN-FEATURE-MATRIX.md`; D6 unsupported hosts are such documented limitations) and D8 (add `mcpserver-grok-bot-plugin`, making nine plugins). After round 4 the operator chose D9 ("Refuse when ambiguous"): while two or more delivered requests are open in one host session, a write without `requestId` is refused with `request_ambiguous`. After round 5 the operator chose D10 ("Only HV when all green at end of slice"): every slice ends with its own Codex gpt-6-sol xhigh HV once all its gates are green, A9 and S11 also carrying the phase-wide HV-A and HV-B; the next phase starts only after all phase PRs are merged (`MEMORY-PROCESS-006`).
- Strict BDPv4 per slice:
  - contract and stubs first;
  - RED;
  - mocks-green (tests validated against mocks before production code), with a negative check;
  - agent self-review;
  - real green, then refactor;
  - gates G1-G6 with zero failures and zero skips.
- Hostile validation happens at the end of every slice once its gates are green (operator D10, 2026-10-09: "Only HV when all green at end of slice") and at P0's end; HV-A9 and HV-S11 also cover all of Phase A and Phase B (HV-A, HV-B). It runs on Codex CLI with model `gpt-6-sol` at reasoning effort `xhigh`, launched by the implementing agent. This replaced the PR-comment protocol on 2026-10-09 (operator: "Yes, replace the PR-comment HV with Codex gpt-6-sol xhigh").
  - The runner writes the brief and the reviewer's `--json` stream to `docs/receipts/hv/<utc>-<gate>.request.jsonl` and `.response.jsonl`; the reviewer writes `hostile-validator-<gate>-<utc>.md` and `.json`. Verify `model=gpt-6-sol`, `effort=xhigh` in the Codex rollout `turn_context`. Worked example: `docs/receipts/hv/20261009T034810Z-session-20261009-ops-hv-r2.*`.
  - The full verdict goes in the session log.
  - Pass requires AGREE with accuracy >= 98 and completeness >= 98.
- Missing historical requirements were imported by "Upsert develop docs". This is done; see below.

## Workspace facts (verified 2026-10-09T02:2xZ)

- Worktree `/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009`, branch `worktree-session-20261009`, based on `f56dcf70` (equal to `develop` and `origin/develop` at 02:2xZ; QBrainAi rename, routes `qbrainai/*` with 1.x `mcpserver/*` aliases).
  - This file and the 12 handoff removals were committed in `3ed6c639` and pushed to `origin/worktree-session-20261009` on 2026-10-09 (see Handoff hygiene). Use `git log` for the current head.
- Marker: `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml`. At 2026-10-09T19:39Z it read baseUrl `http://LAB-OMARCHY:7147`, startedAt `2026-10-09T17:45:57.2150865+00:00` (the server restarted at about 17:46Z when Codex registered the QBrain.AI workspaces; an earlier restart that day was at 13:58:49Z; the server binds `0.0.0.0:7147`; the host was renamed from PAYTON-OMARCHY, which still resolves to it). The startedAt changes on every restart, so read it fresh. The running server reported 1.4.41-recovery+b53ce6b8 with the SQL Server provider earlier in the session. Re-verify with the marker signature and the `/health` nonce.
- Claude plugin repo: `/home/sharpninja/github/mcpserver-claude-code-plugin`, `main` at `d3a07e0` (1.118.0).
- Preserved stashes. They are not applied. Never drop them without operator direction.
  - Main checkout `/home/sharpninja/github/McpServer`: `3ba6fc5413d5bb9fa68d7c312229512da0d86832` ("claude-session-20261009-predevelop-sync-dirt-20261009T005407Z"). The stash stack is shared with all worktrees.
  - Plugin repo: `9544db6940b30d6c695aace71eb081cd4638ddb0` ("claude-presync-1.108.0-core-c195837a-20261009T0036Z").

## Done in session `ClaudeCode-20261009T002615Z-plugin-session`

- Plan written: request turn `req-20261009T004308Z-prompt-e18e`, catalog amendment `req-20261009T015405Z-prompt-18cf`.
- Requirements import (turn `req-20261009T020056Z-prompt-dad0`; evaluation on `req-20261009T020009Z-prompt-fd7e`).
  - Non-destructive canonical upsert of develop `docs/Project/*.md`: +13 FR, +18 TR, +19 TEST, +13 mappings, 0 deletions.
  - Store is now FR 352, TR 480, TEST 512, with 0 develop-doc ids missing. There are 0 use cases.
  - The hosted GitHub wiki was not imported. It is stale, and wiki mode is authoritative: it deletes records that are missing from the wiki.
  - Store-only records left untouched: FR-MCP-ACKRECOVERY-001, FR-MCP-RUNAS-001, FR-MCP-SERVICEUPDATE-001, TR-MCP-AGENT-PARITY-020..027.
  - FR-SUPPORT-016 has no record anywhere. Plan P0 creates it.
- Turn backfill and remap. Every operator request in the session has its own turn. Twin and phantom turns are canceled, with "Turn-mapping correction" decisions pointing to the canonical turn.
- Triage filed:
  - `triage-group-56053b8fdbc844b6` (twin turns: reports `d3bcb648...`, `1a283182...`);
  - `triage-report-629497bab74c4676823aebbccfb58cc0` (missing `lib/session-start.ps1`);
  - `triage-report-a03b85ac70594a298fa9d56eca566492` (storage outage);
  - `triage-report-5e2c5c5f86d043a388788cf2f98ae6e6` (plugin wrapper argv limit);
  - `triage-report-ae1ea81e32b640cbabcfd41c3905377e` (non-atomic requirements ingest with opaque errors).

## Known defects that will affect you until Phase A ships

- **Two turns per prompt (historical).** Early on 2026-10-09 MCP hooks were registered twice, by the `~/.claude/settings.json` bridge and by the enabled plugin `hooks/hooks.json`. The bridge hooks were removed later that day (MEMORY-FACT-003); at 17:54Z `~/.claude/settings.json` had no `hooks` key and the plugin registered one hook per event. Current defect instead: the prompt hook opens phantom turns for background task notifications, and each such open supersede-cancels the real turn in progress (for example `req-20261009T175347Z-prompt-dc63`).
- **New prompts cancel open turns.** `Invoke-ReplSupersedeCurrentTurnIfInProgress` cancels the in-progress turn when the next prompt arrives.
- **Phantom turns.** Background `<task-notification>` events open turns. Cancel them with a mapping-correction decision; no work belongs there.
- **Unreliable turn pointer.** `.mcpServer/claude/current-turn.yaml` can point at a phantom or canceled turn. Target turns by explicit requestId with `client.SessionLog.BeginTurnAsync`, `PatchTurnAsync`, and `CompleteTurnAsync`, invoked through the plugin `lib/repl-invoke.ps1`. Verify every write with `client.SessionLog.QueryAsync`. A write to a terminal turn can report `persisted` while storing nothing.
- **Cache location.** The plugin cache dir follows `CLAUDE_PROJECT_DIR`, otherwise the marker walk-up. Set it to the worktree when you run the wrapper manually.
- **Large payloads fail.** `Invoke-McpPlugin.ps1` passes params as a command-line argument, so payloads above roughly 128 KB fail on Linux. Invoke `lib/repl-invoke.ps1` in-process with `&` instead.
- **Partial requirements ingest.** It is non-atomic per kind and can apply part of a payload before failing with "The change could not be saved." Ingest per kind and only missing entries.

## Next steps (when the operator resumes)

1. Run session start per `CLAUDE.md`: `/add-profile`, marker signature, `/health` nonce, plugin bootstrap, history and TODOs, then an initial turn.
2. Read TODO `PLAN-SESSIONTURNREVIEW-001` and present the plan to the operator for corrections or approval. Plan-readiness hostile validation on Codex gpt-6-sol xhigh is iterated to AGREE >= 98/98 before approval is requested (operator, 2026-10-09). Round 1: `docs/receipts/hv/hostile-validator-plan-readiness-20261009T145521Z.md` (DISAGREE 84/68, 16 findings; four operator decisions D1-D4 answered and applied with the twelve implementer fixes). Round 2: `docs/receipts/hv/hostile-validator-plan-readiness-r2-20261009T160546Z.md` (DISAGREE 90/78, findings R2-01..R2-10; operator decision D5 answered R2-08; the operator then directed a full self-analysis of the plan before the next round). Round 3: `docs/receipts/hv/hostile-validator-plan-readiness-r3-20261009T164531Z.md` (DISAGREE 90/82, 1 PASS, 7 FAIL, 2 UNKNOWN, findings R3-01..R3-08; operator decision D6 "Adapters or unsupported" answered R3-03). Round 4: `docs/receipts/hv/hostile-validator-plan-readiness-r4-20261009T174055Z.md` (DISAGREE 88/76, 2 PASS, 8 FAIL; findings F-R4-01..F-R4-07; operator decision D9 answered F-R4-04). Round 5: `docs/receipts/hv/hostile-validator-plan-readiness-r5-20261009T181414Z.md` (DISAGREE 87/80, 2 PASS, 8 FAIL; findings F-R5-01..F-R5-09; operator decision D10 answered F-R5-05; the requirement ids are now FR-SUPPORT-016, TR-SUPPORT-CORE-016, TEST-SUPPORT-023 because `010G` fails the id grammar). Round 6: `docs/receipts/hv/hostile-validator-plan-readiness-r6-20261009T185109Z.md` (DISAGREE 92/88, 3 PASS, 7 FAIL; findings F-R6-01..F-R6-08, all implementer edits; the operator confirmed the D10 interpretation with "agree"). Round 7: `docs/receipts/hv/hostile-validator-plan-readiness-r7-20261009T191539Z.md` (DISAGREE 96/94; findings F-R7-01..F-R7-05; operator decisions D11 "Supported only if single hook" for Copilot and D12 "Keep Node 18/20" with a raw-token score parser answered F-R7-03 and F-R7-04). Round 8 was launched at 2026-10-09T19:42:42Z and stopped by the implementer before any verdict, because the operator asked for another holistic self-review first ("Before next HV, do another holistic self-review. Have next HV do holistic review."); its partial receipts `docs/receipts/hv/20261009T194242Z-plan-readiness-r8.request.jsonl` and `.response.jsonl` are kept as an aborted run, not a verdict. The self-review fixed: D2 and FR-MCP-SESSIONTURN-007-AC002 now state that the explicit `requestId` is required under D9 (`request_ambiguous`) and name `request_unknown`; C9 lists every typed code; C1 uses full ids; the coordination section records the partial clearance and the 19:51:42Z NOTICE; the HV protocol, UC-7, and UC-1 follow D10, D6, D9, and D11; FR-MCP-SESSIONTURN-001 covers every arrival mechanism; AC records are in numeric order. Round 8 (holistic, launched 2026-10-09T20:05:25Z after the server restart rotated the leaked keys): `docs/receipts/hv/hostile-validator-plan-readiness-r8-20261009T200525Z.md` (DISAGREE 94/86; findings F-R8-01..F-R8-07). F-R8-06 (the public meaning of `skipped_duplicate`) is an operator decision, asked by phone (ntfy) at 20:35:12Z and posted to `MEMORY-PROCESS-007` at 20:35:25Z with options A, B (recommended), and C. The round-9 edits fix F-R8-01 (arrival race protocol), F-R8-02 (all P0.2 host probes), F-R8-03 (mapping edges), F-R8-04 (S4 section tests, contended-lock no-mint test), F-R8-05 (live smoke split by capability), and F-R8-07 (this paragraph), and fold in the C1-C12 contracts agreed with Codex in amended form (Codex ACK 20:25:34Z and 20:33:51Z; A8 owns the shared Node lifecycle; the S4/S5 READINESS prerequisite was removed to break a dependency cycle). Round 9 (holistic): `docs/receipts/hv/hostile-validator-plan-readiness-r9-20261009T203954Z.md` (DISAGREE 90/82; F-R9-01 is the still-open `skipped_duplicate` operator decision; implementer findings F-R9-02..F-R9-09: nine-repo sync blocked by the tarball-name check and missing grok-bot host mapping, queued-arrival probes for codex, grok, copilot, opencode, the C4 binding field set, the C5 no-key boundary, UC-1 against D9, a late-enqueue-after-close double mint, these handoff counts, and the C2 alias journal ownership and tests), addressed in the round-10 edits. Round 10 (holistic): `docs/receipts/hv/hostile-validator-plan-readiness-r10-20261009T210052Z.md` (DISAGREE 93/86; F-R10-01..06) reopened the C2 alias delivery (Codex counter 21:01:48Z, accepted by ClaudeCode at 21:12:10Z) and found the backfill pair on `recordReview`, two TEST-MCP-SESSIONTURN-008 mapping edges, the C3 branch tests, and this handoff's HV cadence line; the operator answered `skipped_duplicate` with "Remove" (2026-10-09T21:15Z; C3 change awaits Codex re-ACK) and set two working rules: "98/98 is not perfection, it is functional." and "If a requirement is deficient, fix it. Create a new req if it's legit, but try reconciliation first. Strengthening AC is usually better than a new requirement." The round-11 edits fix all six as strengthened ACs and named tests, without new mechanism. Round 11 (holistic): `docs/receipts/hv/hostile-validator-plan-readiness-r11-20261009T211948Z.md` (DISAGREE 96/93; F-R11-01 illegal Complete -> Blocked rollback, F-R11-02 no branch for a Claude Code payload without `prompt_id`, F-R11-03 C2 all-consumer acknowledgement). Before round 12 a structured what-if pass (three read-only analysts: Phase A, Phase B, cross-cutting, each also sweeping for siblings of past defect classes) produced about 70 candidates and 21 cross-section contradictions; the vetted ones are applied with the three round-11 fixes as strengthened ACs, named tests, and explicit dispositions. Operator rules added that day: run what-if scenarios before each HV ("Adding AC that is valid is always a win"), ask "how did it deviate from a known-good anchor", and a design principle: "Always have a traceable relationship when coordinating two disparate entities" (new FR-MCP-SESSIONTURN-007-AC009: every plugin record carries its root ids). Codex's approval check declined to apply the operator's `skipped_duplicate` "Remove" relayed through ClaudeCode; Codex needs it from the operator directly, and A1/A8 wait for Codex's C3 re-ACK. Operator licensing decision 2026-10-09: "preserve license on existing files, Apache 2.0 for new files." (recorded in the plan and in `MEMORY-PROCESS-007` 20:58:32Z; it settles triage `triage-report-8509a6345e6c408a981ade489513fa2b`). Coordination with Codex's QB-AGENT-001 (same `plugins/core/lib-node` and server session-log files) runs through Global memory `MEMORY-PROCESS-007`; the plan's section "Coordination with QB-AGENT-001" holds contracts C1-C12. Later rounds add receipts with the same gate prefix.
3. Apply operator corrections to the plan file and the TODO description, then re-verify the round trip.
4. Only after explicit approval, execute P0 in order (P0.1-P0.9). Stop at HV-P0, which runs on Codex gpt-6-sol xhigh.
5. Triage is filed (nothing left to file; one group relocation pending, below). The plan's "Risks and incidental bugs" "To file" list was verified against source and filed on 2026-10-09: `triage-report-41d7d27c53f2464f8339fe963079a55e` (gated SessionQuery omits commit files), gated restore and clone do not carry PlanFile/TodoId (the original `-53dc4c8b359d496ea5327841deb1ea45` overstated the effect and was withdrawn by soft-deleting its one-report group; the corrected replacement's group `triage-group-804b21086f3019ab` was filed with `workspacePath` set to the worktree `/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009` by mistake; a read-only `workflow.triage.getGroup` under the worktree path at 2026-10-09T18:09Z returned it (title "Gated restore and clone do not carry PlanFile and TodoId", 1 report with status `grouped`, group status `failed`), after the round-3 reviewer had seen not found under both paths; relocation to the main workspace is still pending, and the `failed` group status is not processed triage), `-953f44d7574942419e6d5d3123acb971` (revive resurrects replaced children), `-d8c40d80a4ba454e8b5b96b9b5762105` (restamp skips commit files), `-ab06ac3befed4ff99725ef6047715158` (federation snapshot drops turn fields), `-04659db176a8473bbf2aa9b6caf1f98a` (`filesModified` vs `FileModified`). Also filed: `-36d2da231add47f18af5abd35a340e5f` (dialog ordinals repeat; delete-by-ordinal removes several items), `-ed3b0381bd2948e58d6315f6e1601b02` (delete-item fails for values containing `/`), `-bd1f438638f64f808e53a407aeb58c98` (hook validator depends on `CLAUDE_PLUGIN_ROOT`), `-e1f01430477f441fb86538469d15fc15` (plugin memory verbs log an undocumented `memory` action type and reuse action orders), and `-02e7a0198d064403a85b13efa6273637` (dangling `CODEX-HANDOFF.md` citations). The plan's "Risks and incidental bugs" section lists the plan-scope reports above (the "To file" list was replaced in the round-2 edit, 2026-10-09); the five "Also filed" reports are handoff-only context and are not listed in the plan's Risks section. Moving group `triage-group-804b21086f3019ab` to the main workspace (operator: "2 move") is still pending.

## Do not

- Implement anything before approval.
- Edit `docs/Project/TODO.yaml`, session-log storage, or requirements storage directly. Use the plugin wrappers.
- Run a wiki-mode requirements import (it deletes records).
- Deploy without `./build.ps1 UpdateService` and operator approval.
- Drop or apply the preserved stashes without operator direction.
- Move any TODO to a done state without HV AGREE >= 98/98.

## Handoff hygiene

The operator directed removal of every existing handoff in the worktree. The first attempt was blocked by the Claude Code auto-mode classifier. On 2026-10-09T03:12Z, at the operator's explicit "Remove them", ClaudeCode removed all 12 with `git rm`; the deletions are committed in `3ed6c639` and pushed. All 12 files are tracked at `f56dcf70` and remain readable with `git show f56dcf70:<path>`. The dangling `CODEX-HANDOFF.md` citations in `docs/Byrd-Dev-Process-v4-Project-Management-Guide.md` (lines 6 and 926) are filed as `triage-report-02e7a0198d064403a85b13efa6273637`.

- `HANDOFF.md` (2026-06-23 Grok)
- `CODEX-HANDOFF.md` (2026-06-25 Codex)
- `docs/handoffs/grok-completion-program-20260909.md`
- `docs/handoffs/handback-overlay-from-quadbrain-qbagent-20260910.md`
- `docs/handoffs/handoff-qbexec-mcp-tools-compaction-20260910.md`
- `docs/handoffs/integrate-session-start-opensession-20260916.md`
- `docs/handoffs/sessionlife-p2-contracts-pr72-20261001T113858Z.md` (stale: PR #72 merged 2026-10-03T09:39:23Z)
- `docs/handoffs/txnkeyserver-all-adapters-20260919.md` (already marked superseded)
- `docs/plans/handoff-agent-parity-plan-2026-05-28.yaml`
- `docs/plans/handoff-mcpserver-503-auth-readiness-2026-06-15.md`
- `docs/plans/handoff-parity-v4-phase1-complete-2026-05-29.md`
- `docs/Project/Plugin-Simplification-Handoff.md`

Kept on purpose:
- `docs/handoffs/example.md`: the handoff-ingestion worked sample, referenced by `docs/Handoff-Ingestion.md`, `docs/CLIENT-INTEGRATION.md`, the REPL guides, `plugins/core/skills/handoff/SKILL.md`, and `HandoffSkillDelegationTests.cs`.
- `docs/Handoff-Ingestion.md` and its wiki copies: feature documentation, not handoffs.

This file is the only active handoff for this workstream.
