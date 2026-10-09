# Handoff: Session-turn logging, request class, and turn reviews plan (NOT APPROVED)

**Active workstream handoff.** Prepared by ClaudeCode (Opus 5.5, effort xhigh) on 2026-10-09 UTC for Payton Byrd, after the operator declined plan approval and directed: "Write plan to high todo and create handoff (remove existing handoffs in worktree)".

Copy everything below the line into the next agent.

---

## Status (read first)

- The plan is **NOT APPROVED**. Do not implement any step, including P0 (requirements, use cases, TODO creation), until the operator explicitly approves it. The operator may still be typing corrections.
- Authoritative copy: MCP TODO `PLAN-SESSIONTURNREVIEW-001` (priority high, section SessionLog, done=false). Retrieve with `workflow.todo.get` `id: PLAN-SESSIONTURNREVIEW-001`.
  - `description`: the full plan text. Verified on 2026-10-09 after the round-4 and coordination edits: all 508 non-empty lines of the source file are stored exactly (PowerShell `Get-Content` count, non-empty = non-whitespace); the server drops blank lines.
  - `implementationTasks` (30): operator approval, P0.1-P0.9, A1-A9, S1-S11.
  - `note`: the not-approved status. `technicalDetails`: session, worktree, and baseline facts.
  - FR/TR arrays are empty on purpose. The new FR/TR/TEST ids are created in P0 and do not exist in the store yet.
- Local source of the plan, on PAYTON-OMARCHY only: `/home/sharpninja/.claude/plans/create-a-new-plan-giggly-music.md` (625 lines, 508 non-empty, PowerShell `Get-Content` count after the round-4 and coordination edits). If the operator corrects the plan, update this file and the TODO description together, and verify the TODO round trip.

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

Scope covers the server, client, REPL, MCP tools, the PowerShell and Node plugin cores, and all eight agent plugins. Those are claude-code, claude-cowork, codex, copilot, grok, cline, cline-v2, and opencode.

## Locked operator decisions (do not re-litigate)

The full list is in the plan, section "Locked operator decisions (2026-10-09)". Key points:
- Every user request is exactly one turn, including messages queued mid-turn. Actions and decisions map to the turn of the request that caused them.
- Operator decisions on 2026-10-09 after Codex plan-readiness round 1: D1 the server issues the request token when a message arrives; D2 writes are tied to their request automatically through the agent app's hooks; D3 the use-case coverage gate checks only this plan's requirements; D4 a review with status Other requires the Other reason (`otherNotes`). After round 2 the operator chose D5: queued mid-turn messages are minted at enqueue by an arrival bridge ("Arrival bridge"). After round 3 the operator chose D6: hosts without an arrival mechanism are unsupported for turn logging, with no first-sight waivers ("Adapters or unsupported"). The operator then confirmed D7 (plugin equivalence across all QBrain.AI plugins, limited by each host's plugin infrastructure, with limitations documented in `docs/AGENT-PLUGIN-FEATURE-MATRIX.md`; D6 unsupported hosts are such documented limitations) and D8 (add `mcpserver-grok-bot-plugin`, making nine plugins). After round 4 the operator chose D9 ("Refuse when ambiguous"): while two or more delivered requests are open in one host session, a write without `requestId` is refused with `request_ambiguous`.
- Strict BDPv4 per slice:
  - contract and stubs first;
  - RED;
  - mocks-green (tests validated against mocks before production code), with a negative check;
  - agent self-review;
  - real green, then refactor;
  - gates G1-G6 with zero failures and zero skips.
- Hostile validation happens at the completion of major phases (HV-P0, HV-A, HV-B) on Codex CLI with model `gpt-6-sol` at reasoning effort `xhigh`, launched by the implementing agent. This replaced the PR-comment protocol on 2026-10-09 (operator: "Yes, replace the PR-comment HV with Codex gpt-6-sol xhigh").
  - The runner writes the brief and the reviewer's `--json` stream to `docs/receipts/hv/<utc>-<gate>.request.jsonl` and `.response.jsonl`; the reviewer writes `hostile-validator-<gate>-<utc>.md` and `.json`. Verify `model=gpt-6-sol`, `effort=xhigh` in the Codex rollout `turn_context`. Worked example: `docs/receipts/hv/20261009T034810Z-session-20261009-ops-hv-r2.*`.
  - The full verdict goes in the session log.
  - Pass requires AGREE with accuracy >= 98 and completeness >= 98.
- Missing historical requirements were imported by "Upsert develop docs". This is done; see below.

## Workspace facts (verified 2026-10-09T02:2xZ)

- Worktree `/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009`, branch `worktree-session-20261009`, based on `f56dcf70` (equal to `develop` and `origin/develop` at 02:2xZ; QBrainAi rename, routes `qbrainai/*` with 1.x `mcpserver/*` aliases).
  - This file and the 12 handoff removals were committed in `3ed6c639` and pushed to `origin/worktree-session-20261009` on 2026-10-09 (see Handoff hygiene). Use `git log` for the current head.
- Marker: `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml`. After the 2026-10-09 restart it reads baseUrl `http://LAB-OMARCHY:7147`, startedAt `2026-10-09T13:58:49Z` (the server now binds `0.0.0.0:7147`; the host was renamed from PAYTON-OMARCHY, which still resolves to it). The running server reported 1.4.41-recovery+b53ce6b8 with the SQL Server provider earlier in the session. Re-verify with the marker signature and the `/health` nonce.
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
  - FR-SUPPORT-010G has no record anywhere. Plan P0 creates it.
- Turn backfill and remap. Every operator request in the session has its own turn. Twin and phantom turns are canceled, with "Turn-mapping correction" decisions pointing to the canonical turn.
- Triage filed:
  - `triage-group-56053b8fdbc844b6` (twin turns: reports `d3bcb648...`, `1a283182...`);
  - `triage-report-629497bab74c4676823aebbccfb58cc0` (missing `lib/session-start.ps1`);
  - `triage-report-a03b85ac70594a298fa9d56eca566492` (storage outage);
  - `triage-report-5e2c5c5f86d043a388788cf2f98ae6e6` (plugin wrapper argv limit);
  - `triage-report-ae1ea81e32b640cbabcfd41c3905377e` (non-atomic requirements ingest with opaque errors).

## Known defects that will affect you until Phase A ships

- **Two turns per prompt.** MCP hooks are registered twice: by the `~/.claude/settings.json` bridge and by the enabled plugin `hooks/hooks.json`.
- **New prompts cancel open turns.** `Invoke-ReplSupersedeCurrentTurnIfInProgress` cancels the in-progress turn when the next prompt arrives.
- **Phantom turns.** Background `<task-notification>` events open turns. Cancel them with a mapping-correction decision; no work belongs there.
- **Unreliable turn pointer.** `.mcpServer/claude/current-turn.yaml` can point at a phantom or canceled turn. Target turns by explicit requestId with `client.SessionLog.BeginTurnAsync`, `PatchTurnAsync`, and `CompleteTurnAsync`, invoked through the plugin `lib/repl-invoke.ps1`. Verify every write with `client.SessionLog.QueryAsync`. A write to a terminal turn can report `persisted` while storing nothing.
- **Cache location.** The plugin cache dir follows `CLAUDE_PROJECT_DIR`, otherwise the marker walk-up. Set it to the worktree when you run the wrapper manually.
- **Large payloads fail.** `Invoke-McpPlugin.ps1` passes params as a command-line argument, so payloads above roughly 128 KB fail on Linux. Invoke `lib/repl-invoke.ps1` in-process with `&` instead.
- **Partial requirements ingest.** It is non-atomic per kind and can apply part of a payload before failing with "The change could not be saved." Ingest per kind and only missing entries.

## Next steps (when the operator resumes)

1. Run session start per `CLAUDE.md`: `/add-profile`, marker signature, `/health` nonce, plugin bootstrap, history and TODOs, then an initial turn.
2. Read TODO `PLAN-SESSIONTURNREVIEW-001` and present the plan to the operator for corrections or approval. Plan-readiness hostile validation on Codex gpt-6-sol xhigh is iterated to AGREE >= 98/98 before approval is requested (operator, 2026-10-09). Round 1: `docs/receipts/hv/hostile-validator-plan-readiness-20261009T145521Z.md` (DISAGREE 84/68, 16 findings; four operator decisions D1-D4 answered and applied with the twelve implementer fixes). Round 2: `docs/receipts/hv/hostile-validator-plan-readiness-r2-20261009T160546Z.md` (DISAGREE 90/78, findings R2-01..R2-10; operator decision D5 answered R2-08; the operator then directed a full self-analysis of the plan before the next round). Round 3: `docs/receipts/hv/hostile-validator-plan-readiness-r3-20261009T164531Z.md` (DISAGREE 90/82, 1 PASS, 7 FAIL, 2 UNKNOWN, findings R3-01..R3-08; operator decision D6 "Adapters or unsupported" answered R3-03). Round 4: `docs/receipts/hv/hostile-validator-plan-readiness-r4-20261009T174055Z.md` (DISAGREE 88/76, 2 PASS, 8 FAIL; findings F-R4-01..F-R4-07; operator decision D9 answered F-R4-04). Coordination with Codex's QB-AGENT-001 (same `plugins/core/lib-node` and server session-log files) runs through Global memory `MEMORY-PROCESS-007`; the plan's section "Coordination with QB-AGENT-001" holds contracts C1-C12. Later rounds add receipts with the same gate prefix.
3. Apply operator corrections to the plan file and the TODO description, then re-verify the round trip.
4. Only after explicit approval, execute P0 in order (P0.1-P0.9). Stop at HV-P0, which runs on Codex gpt-6-sol xhigh.
5. Triage is filed (nothing left to file; one group relocation pending, below). The plan's "Risks and incidental bugs" "To file" list was verified against source and filed on 2026-10-09: `triage-report-41d7d27c53f2464f8339fe963079a55e` (gated SessionQuery omits commit files), gated restore and clone do not carry PlanFile/TodoId (the original `-53dc4c8b359d496ea5327841deb1ea45` overstated the effect and was withdrawn by soft-deleting its one-report group; the corrected replacement's group `triage-group-804b21086f3019ab` was filed with `workspacePath` set to the worktree `/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009` by mistake; a read-only `workflow.triage.getGroup` under the worktree path at 2026-10-09T18:09Z returned it (title "Gated restore and clone do not carry PlanFile and TodoId", 1 report with status `grouped`, group status `failed`), after the round-3 reviewer had seen not found under both paths; relocation to the main workspace is still pending, and the `failed` group status is not processed triage), `-953f44d7574942419e6d5d3123acb971` (revive resurrects replaced children), `-d8c40d80a4ba454e8b5b96b9b5762105` (restamp skips commit files), `-ab06ac3befed4ff99725ef6047715158` (federation snapshot drops turn fields), `-04659db176a8473bbf2aa9b6caf1f98a` (`filesModified` vs `FileModified`). Also filed: `-36d2da231add47f18af5abd35a340e5f` (dialog ordinals repeat; delete-by-ordinal removes several items), `-ed3b0381bd2948e58d6315f6e1601b02` (delete-item fails for values containing `/`), `-bd1f438638f64f808e53a407aeb58c98` (hook validator depends on `CLAUDE_PLUGIN_ROOT`), `-e1f01430477f441fb86538469d15fc15` (plugin memory verbs log an undocumented `memory` action type and reuse action orders), and `-02e7a0198d064403a85b13efa6273637` (dangling `CODEX-HANDOFF.md` citations). The plan's risks section now lists these filed ids instead of "To file" (round-2 edit, 2026-10-09). Moving group `triage-group-804b21086f3019ab` to the main workspace (operator: "2 move") is still pending.

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
