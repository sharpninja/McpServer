# Hostile validator receipt

TimestampUtc: 2026-08-21T15:34:16Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: class-2-user-directed-general-action
WorkClassNote: Operator-directed post of queued failsafe data and triage of failures. Not project implementation. Surface C N/A. Surface D N/A.

add-profile: executed yes. Profile markdown files read in full: 18. Skill ports excluded: add-profile.grok.md. Total *.md in C:\Users\kingd\.claude\profile: 19.

SessionId: GrokCode-20260821T153034Z-hostile-failsafe-post
RequestId: req-20260821T153034Z-001-hostile-failsafe-post-review
TurnId: 42631

Trust: marker signature verified true (docs/receipts/_hv-failsafe-post-review/marker-sig.json). Health nonce nonce-0db6aca0562548a49cdec0ff7a4ba64a echoed exactly; status Healthy (docs/receipts/_hv-failsafe-post-review/health.json). Plugin Status available, failsafeCount 0, failsafeQuarantineCount 0, plugin .version 1.97.0.

OverallVerdict: DISAGREE

PASS: 10
FAIL: 3
UNKNOWN: 0
N/A: 2 (C, D)

Accuracy rating: 72/100. Remaining-queue counts, triage report IDs, no-TODO-done, and no-timeout-patch re-verified. The headline 167 / grok-live 54 / grok-quarantine 48 posted totals are not supported by itemized replay receipts (124) and are 2 off even if the 503 triage's unitemized 41 grok-live posts are granted (165).
Completeness rating: 86/100. Live, quarantine, and pending queues were independently re-counted. Four triage reports were queried live. Replay JSON was re-tallied. Drain stdout was not retained; the 41-file grok-live burst has no itemized receipt.

## FAIL list (do not bury)

1. A2: Posted 167 SubmitAsync records (54/48/12/12/41) is false as a machine-verifiable total. Itemized success across the three replay receipts is 124. Gap 43.
2. B1 honesty: the summary and later triage reports repeat 167/54/48 while the only itemized SubmitAsync receipts sum to 124 (12+47+65). A reconstructed 41 (from triage-report-97262343...) + 124 = 165, still not 167, and grok live would be 53 not 54.
3. B2 receipts: no itemized file list for the alleged 41 grok-live posts before the 14:59 remaining-12 replay. Drain stdout was not saved; only the implementer summary plus a triage report paraphrase the abort.

## Surface A: requested validation

### A1. workflow.failsafe.drain aborted: scanned 1 failed 1 aborted true, abortReason mcpserver-repl timed out after 2s because Get-ReplMethodTimeoutSeconds returns 2 during drain for client.SessionLog.SubmitAsync
Verdict: PASS
Evidence:
- Live triage-report-bb6214a43c40430da3285a530deec758 (createdUtc 2026-08-21T14:41:20.9793617+00:00, status grouped) summary: scanned 1, failed 1, aborted=true, abortReason 'backend unreachable: mcpserver-repl timed out after 2s'. File: docs/receipts/_hv-failsafe-post-review/triage-bb6214a43c40430da3285a530deec758.json
- plugins/core/lib-ps/repl-invoke.ps1 Get-ReplMethodTimeoutSeconds returns 2 when ReplFailsafeDraining and Method is client.SessionLog.SubmitAsync. LastWriteUtc 2026-08-21T00:46:48Z (before this ops turn). timeout-source.json drainSubmitReturns2 true.
- Invoke-ReplRaw error text is 'mcpserver-repl timed out after ${timeout}s'. Parent brief omitted the code's 'backend unreachable:' prefix; the live triage report has the precise string.
- Drain stdout was not found outside the implementer summary (drain-search.json hits the summary, the replay script comment, and a false-positive todo dump). Not scored FAIL because the live triage report plus source are enough for this mechanism+event claim.

### A2. Posted 167 SubmitAsync records total: grok live 54, grok quarantine 48, Codex live 12, Codex quarantine 12, Claude quarantine 41
Verdict: FAIL
Evidence:
- failsafe-replay-20260821T144010Z.json posted 12 successTrue 12 (grok live remaining after the 503 burst).
- failsafe-quarantine-replay-20260821T150140Z.json posted 47 successTrue 47 (relative names; grok quarantine).
- failsafe-other-replay-20260821T150540Z.json posted 65 successTrue 65 failed 3: Codex live 12 ok, Codex quarantine 12 ok / 3 fail, Claude quarantine 41 ok.
- replay-arithmetic.json: itemizedSuccess 124, claimedTotal 167, gap 43.
- Independent prior count at 11:45: grok live 53, grok quarantine 48 (hostile-validator-20260821T114530Z.json A10).
- triage-report-972623438ab84b489e8195f977698a86 (createdUtc 2026-08-21T14:59:46.9331451+00:00) claims 41 grok-live posts then 11 consecutive 503s, 12 yaml remaining. 41 is not itemized. 41+12=53 grok live, not 54. 41+12+47+12+12+41=165, not 167.
- 41 files at claimed 73-88s each cannot fit in the 14:41-14:56 window (that duration implies about 11 sequential posts). The 41 figure is internally inconsistent with the timestamps in the same report.

### A3. Grok live, grok quarantine, grok pending, Codex live, Claude quarantine are now 0 yaml files
Verdict: PASS
Evidence: Independent pwsh Get-ChildItem -File -Filter *.yaml (non-recursive) at 2026-08-21T15:23:51Z and again in yaml-counts.json: grok failsafe 0, grok quarantine 0, grok pending 0, Codex failsafe 0, Claude quarantine 0. Plugin Status failsafeCount 0 failsafeQuarantineCount 0. workflow.failsafe.status pendingCount 0 quarantineCount 0.

### A4. Remaining: 3 Codex quarantine yaml (corrupt shape / omitted planFile) and 2 Codex pending importRecovery yaml
Verdict: PASS
Evidence: yaml-counts.json and remaining-shape.json.
- Codex quarantine (3): 20260722T232406Z-session_submit-3358.invalid-requestid-corrupted-shape.yaml turns is mapping key "0" (corrupt shape); 20260817T123057Z-session_submit-d5f9.yaml and 20260817T123219Z-session_submit-9ad7.yaml have no planFile key. Other-replay failures match JSON deserialize and HTTP 400 planFile omitted.
- Codex pending (2): both importRecovery: persisted false. Not SessionLog.SubmitAsync records.

### A5. Triage accepted: bb6214... (2s drain), 972623... (503 burst), f52c6e... (3 unpostable), ab5a9d... (importRecovery)
Verdict: PASS
Evidence: Native triage_status HTTP 200 for all four. Each returns reportId, groupId, status grouped.
- triage-report-bb6214a43c40430da3285a530deec758 group triage-group-1cf130328c87c15c createdUtc 2026-08-21T14:41:20.9793617+00:00
- triage-report-972623438ab84b489e8195f977698a86 group triage-group-d7d0df4bfbee15f1 createdUtc 2026-08-21T14:59:46.9331451+00:00
- triage-report-f52c6edada3f437bbe7aadcc5611c684 group triage-group-6538ff0abc1a3aff createdUtc 2026-08-21T15:22:18.5664863+00:00
- triage-report-ab5a9da38021414b96dcb651bc6697e0 group triage-group-c1f7b55905a5f6c3 createdUtc 2026-08-21T15:22:21.9841525+00:00

### A6. No TODO marked done. No product timeout patch shipped
Verdict: PASS
Evidence:
- todo_list done=false totalCount 40. Prior 11:45 open count 36. Diff: added BUG-TRIAGE-168, BUG-TRIAGE-169, BUG-TRIAGE-170, BUG-TRIAGE-171. Removed from open: none. newlyDoneFromPriorOpen empty. git-todo-porcelain.txt empty.
- git diff --stat on plugins/core/lib-ps/repl-invoke.ps1 and Invoke-McpPlugin.ps1 is empty. repl-invoke lastWriteUtc 2026-08-21T00:46:48Z. Latest commit touching it is 0e0c5763 2026-08-20 (before this ops turn). The 2s drain cap is pre-existing, not a patch shipped in this work.

## Surface B: workspace rules

### B1. Honesty / no fabricated results
Verdict: FAIL
Rule: accuracy-first-verify-sources.md; bring-the-receipts.md
Evidence: Summary grokLivePosted 54, grokQuarantinePosted 48, submitAsyncPostedTotal 167 does not match itemized replay receipts (12+47+65=124). The 503 triage's 41 is not itemized and arithmetically yields 53/165, not 54/167. Remaining-queue zeros and the four triage IDs are honest.

### B2. Always bring the receipts
Verdict: FAIL
Rule: bring-the-receipts.md
Evidence: Three replay JSON files itemize 124 successes and 3 failures. Missing: drain stdout; itemized list of the 41 grok-live files allegedly posted before 14:59. Validator re-counted queues and re-read receipts; could not re-prove 167.

### B3. MCP-only TODO / session / requirements storage
Verdict: PASS
Rule: AGENTS.md MCP-only storage
Evidence: Native sessionlog_* and todo_list / triage_status used. git status porcelain for docs/todo.yaml and docs/Project/TODO.yaml empty. No TODO.yaml edit. Four new BUG-TRIAGE TODOs appeared via triage intake, not by marking done.

### B4. PowerShell-only / no Python
Verdict: PASS
Rule: no-python-lab.md
Evidence: Replay scripts are pwsh. Validator used pwsh.exe -NoProfile -NonInteractive only. No python/python3/py invoked.

### B5. Look-before-delete / failsafe delete-after-success
Verdict: PASS
Rule: lab-authorization.md look-before-delete
Evidence: Replay scripts Remove-Item only after LASTEXITCODE 0 and type: result. Remaining 5 yaml files still on disk. No extra yaml deletion of the 3+2 remainder.

## Surface C: requirements
Verdict: N/A
Class 2 user-directed ops. Not project implementation. No FR/TR/TEST gap is a FAIL.

## Surface D: current plan holistically
Verdict: N/A
Implementer did not claim a plan-step complete. No product plan DoD applies.

## Session-log persistence proof
Native sessionlog_open created true. sessionlog_begin_turn success turnId 42631 status in_progress. sessionlog_query agent GrokCode from 2026-08-21T15:30:00Z returns session GrokCode-20260821T153034Z-hostile-failsafe-post turnCount 1. Proof files: self-open.json, self-begin.json, q-self-from.json. Complete-turn proof is written after this receipt by persist-complete.ps1.

## Supporting artifacts
- docs/receipts/_hv-failsafe-post-review/
- docs/receipts/failsafe-post-all-summary-20260821T152147Z.json
- docs/receipts/failsafe-replay-20260821T144010Z.json
- docs/receipts/failsafe-quarantine-replay-20260821T150140Z.json
- docs/receipts/failsafe-other-replay-20260821T150540Z.json
