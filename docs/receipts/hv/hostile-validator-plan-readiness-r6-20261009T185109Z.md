# Hostile validator: plan readiness, round 6

TimestampUtc: 2026-10-09T19:08:25Z  
HEAD: cf871394e200b06a6a1f29bbe50547f2b2a0097b  
Reviewer session/request: Codex-20261009T032846Z-plugin-session / req-20261009T185235Z-plan-readiness-r6

## Identity and mandatory reads

- Live Codex CLI rollout `/home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T13-51-10-01a12201-3f5e-7010-a888-759fc086ca8a.jsonl`: `session_meta` ordinal 0 records `codex_exec`, CLI 0.160.1, and this worktree; `turn_context` ordinal 7 records `gpt-6-sol`, effort `xhigh`.
- Read `/home/sharpninja/.claude/skills/add-profile/SKILL.md` and all 19 non-skill Markdown files in `/home/sharpninja/.claude/profile/` in full. Read the main `AGENTS-README-FIRST.yaml` in full, verified main and worktree marker trust, and read all 12 worktree global and 14 main effective MCP memories, including the 18:08:08Z partial-clearance entry in MEMORY-PROCESS-007.
- Review only. The main TODO was read through the read-only MCP REPL after the Codex wrapper main-workspace call failed. Infrastructure repair is out of scope.

## Artifact comparison and round-5 reattack

- The plan source has 625 lines and 508 nonempty lines. The main-workspace TODO is **NOT APPROVED**; its description is 508/508 byte-equal to the plan's nonempty lines, and its implementationTasks count is 30. The handoff's status, counts, locations, and nine-plugin list match. The TODO technicalDetails is stale (F-R6-08).
- Read the r1-r5 receipts and re-attacked F-R5-01 through F-R5-09. The reported textual repairs are present: valid SUPPORT IDs, correct document types, nine-plugin inventory, host-only mint, D10 slice HV, conditional Cowork arrival, nine-plugin A9/handoff, D9 live refusal, and merged-PR phase gate. The defects below remain.
- Live requirements store: 352 FR, 480 TR, 512 TEST. The proposed 21 FR, 23 TR, and 23 TEST IDs are free. Legacy `FR-SUPPORT-010G` occurs 36 times in 9 source/test files, as the revised plan says. Nine local plugin heads, versions and hook hashes, and the eight-entry build list match the revised inventory.

## P1-P10

- **P1 FAIL:** Plan line 13 says duplicate Claude hooks are currently registered. Active `~/.claude/settings.json` has `hooks=null`, zero bridge references, and the plugin enabled. Historical twin turns do not make that present-tense claim current.
- **P2 FAIL:** Line 495 cites nonexistent `TR-SESSIONCLASS-004`; P0 defines `TR-MCP-SESSIONCLASS-004`. P0.4 at line 167 and mappings at lines 387-410 map only new FRs, leaving changed existing ACs without refreshed mappings.
- **P3 PASS:** D1-D9 appear in the model, hooks, class gate, Other reason, refusal rule, nine-plugin matrix, and live checks. D10 is implemented as each slice's Codex HV after G1-G6 green, with pending `Other` reason and phase-wide HV-A/HV-B at A9/S11. This matches the operator's words and disclosed interim-turn interpretation.
- **P4 PASS:** Tasks cover server, client, REPL, MCP tools, PowerShell and Node cores, all nine agent plugins including grok-bot, and SQLite, PostgreSQL, and SQL Server migrations and compatibility.
- **P5 FAIL:** Coordination lines 110/170 allow shared-file work after posting C1-C12 without a C1-C5/C9 ACK or resolved REQUEST gate, despite MEMORY-PROCESS-007 partial clearance holding QB P1 shared lifecycle files and receipt shapes.
- **P6 FAIL:** P0.1 line 164 tells this Linux host to run Microsoft's `dotnet-install.ps1`; the distribution-package alternative gives no concrete version-pinned command. The current SDK 10.0.111 does not satisfy `global.json` 10.0.201. Microsoft documents `.ps1` for Windows and `.sh` for Linux/macOS.
- **P7 FAIL:** The new `FR-SUPPORT-013 ac-5` at line 443 is S10-assigned, but its document/live mapping points only to prior `TR-MCP-HTTP-002` and `TEST-SUPPORT-010C-1/2/3`, not the new completion-gate test `TEST-MCP-SESSIONCLASS-002`.
- **P8 FAIL:** Lines 146/156 complete each slice after its own HV, whereas A9/S11 at lines 567/591 defer all phase slice completions to HV-A/HV-B. Line 149 permits a landed slice verdict only as Pass/Fail, while lines 308/585 require Other for DISAGREE with scores deriving Pass.
- **P9 FAIL:** Description parity, 30 tasks, and handoff details check out, but TODO technicalDetails still names invalid `FR/TR/TEST-SUPPORT-010G` rather than valid IDs in source lines 166/180/335/385.
- **P10 PASS:** Risks at lines 601-605 match inspected source and plugin inventory. Named triage reports and the worktree group resolve in the read-only live store; in-scope plugin defects are scheduled in A9.

## Fail findings and PASS fixes

### F-R6-01 — implementer plan edit

- Plan location: Context, line 13. Claim P1.
- Defect: Present-tense duplicate-hook assertion is stale.
- Evidence: Active Claude settings have null hooks and no bridge entries; MEMORY-FACT-003 records removal at 06:11Z.
- PASS fix: Describe the historical reproduction and current hook inventory; keep A2 as a regression check contingent on a fresh live check. No operator decision needed.

### F-R6-02 — implementer plan edit

- Plan location: Slice coverage, line 495. Claim P2.
- Defect: `TR-SESSIONCLASS-004` is neither a canonical ID nor a defined shorthand.
- Evidence: P0 defines `TR-MCP-SESSIONCLASS-004`; the live store has no shortened ID. Line 176 defines shorthand only for AC identifiers.
- PASS fix: Use `TR-MCP-SESSIONCLASS-004`. No operator decision needed.

### F-R6-03 — implementer plan edit

- Plan location: P0.4 line 167; Mappings lines 387-410; Updated existing requirements lines 412-451, especially line 443. Claims P2 and P7.
- Defect: Changed existing FR acceptance criteria do not receive mapping updates.
- Evidence: New `FR-SUPPORT-013 ac-5` remains mapped in `docs/Project/TR-per-FR-Mapping.md:324` and the live store only to `TR-MCP-HTTP-002` and `TEST-SUPPORT-010C-1/2/3`; `TEST-MCP-SESSIONCLASS-002` is not linked.
- PASS fix: Map each updated FR/AC to covering TR/TEST, preserve old edges, and make each TEST statement name the updated ACs it verifies. No operator decision needed.

### F-R6-04 — implementer plan edit

- Plan location: Coordination line 110; P0.7 line 170. Claim P5.
- Defect: The partial-clearance coordination procedure lacks a contract-disposition gate.
- Evidence: MEMORY-PROCESS-007 at 18:08:08Z holds QB P1 shared lifecycle files and receipt shapes until Codex ACK or REQUEST; the plan requires only posting C1-C12 and recording state.
- PASS fix: Require ACK for C1-C5/C9 or an operator-resolved REQUEST before dependent shared-file integration/merge; require C6-C12 readiness or resolution before their dependent slices. No new operator decision now.

### F-R6-05 — implementer plan edit

- Plan location: P0.1, line 164. Claim P6.
- Defect: The Linux SDK installation path uses the Windows `.ps1` route; its package alternative is unspecified.
- Evidence: Linux host has SDK 10.0.111 while `global.json` requires 10.0.201. [Microsoft's dotnet-install documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script) assigns `.ps1` to Windows and `.sh` to Linux/macOS.
- PASS fix: Specify a Linux-valid PowerShell-only package or download/extract sequence pinned to 10.0.201, followed by `dotnet --list-sdks` validation. No operator decision needed.

### F-R6-06 — implementer plan edit

- Plan location: Generic transitions lines 146/156; A9 line 567; S11 line 591. Claim P8.
- Defect: A1-A8 and S1-S10 TODO completion timing contradicts itself.
- Evidence: Generic rule completes a slice on its own HV AGREE; phase tasks defer every slice until HV-A/HV-B AGREE.
- PASS fix: A1-A8 and S1-S10 complete on own slice HV; A9 and S11 complete on phase-wide HV. A phase-wide DISAGREE blocks named affected slices. No operator decision needed.

### F-R6-07 — implementer plan edit

- Plan location: Slice HV recording line 149; review mapping lines 308/585. Claim P8.
- Defect: Generic verdict recording omits valid `Other` outcome.
- Evidence: DISAGREE with scores deriving Pass maps to Other plus otherNotes under lines 308/585.
- PASS fix: Use Pass/Fail/Other mapping at line 149 and require otherNotes for Other. No operator decision needed.

### F-R6-08 — implementer TODO edit

- Plan location: P0 IDs lines 166-180, 335, 385; live TODO technicalDetails. Claim P9.
- Defect: TODO technicalDetails retains obsolete `FR/TR/TEST-SUPPORT-010G` IDs.
- Evidence: Live TODO uses 010G; source and byte-equal description use `FR-SUPPORT-016`, `TR-SUPPORT-CORE-016`, `TEST-SUPPORT-023`.
- PASS fix: Resync technicalDetails to valid IDs, retaining 010G only as a historical source citation. No operator decision needed.

## Verdict and limits

**DISAGREE.** Accuracy 92/100, completeness 88/100, confidence 97/100; PASS 3, FAIL 7, UNKNOWN 0. The 98/98 threshold is unmet and seven applicable claims fail. All eight repairs are implementer edits. A later Codex REQUEST under MEMORY-PROCESS-007 would require resolution before dependent shared-file work.

This was a read-only plan/source/MCP review. No build or test was run; the host's SDK is below `global.json`. The reviewer turn contains the full result and persistence proof.

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":92,"completeness":88,"confidence":97,"passCount":3,"failCount":7,"unknownCount":0,"failList":["P1","P2","P5","P6","P7","P8","P9"],"unknownList":[],"implementerFixes":["F-R6-01","F-R6-02","F-R6-03","F-R6-04","F-R6-05","F-R6-06","F-R6-07","F-R6-08"],"operatorDecisionFails":[],"headSha":"cf871394e200b06a6a1f29bbe50547f2b2a0097b","receiptPaths":["/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r6-20261009T185109Z.md","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r6-20261009T185109Z.json"],"reviewerSessionId":"Codex-20261009T032846Z-plugin-session","reviewerRequestId":"req-20261009T185235Z-plan-readiness-r6"}

