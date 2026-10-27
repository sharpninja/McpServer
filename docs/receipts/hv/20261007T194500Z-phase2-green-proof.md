# Phase 2 green proof r5 (HV-R4-01 TimerService Stop race closed; Manager timing tests deterministic)

- ReceiptId: 20261007T194500Z-phase2-green-proof
- Host: PAYTON-LEGION2
- Gate: BDPv4 Failed 0 / Skipped 0; no quarantine; no waivers; draft tips only; no merges
- Workspace lock: F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007 (MEMORY-PROCEDURE-003)
- Evidence: docs\receipts\hv\20261007T194500Z-manager-green-proof (proof.ps1, progress.log, summary.json, stress.json, *.trx, *.test.log, nested-ancestors.txt); red evidence docs\receipts\hv\20261007T192500Z-hv-r4-01-timer-fix

## Change since HV r4 (DISAGREE Acc97 Comp97, HV-R4-01)

McpServerManager #10 draft tip 8e1550d -> b9793096df77a94228487da78f3914c80657d2ab (pushed by sharpninja; PR OPEN, draft, unmerged)

1. 6c97627 fix(phase2) HV-R4-01. Root cause: TimerHandle.Stop only called Timer.Change(Infinite, Infinite). System.Threading.Timer.Change does not wait for queued or in-flight callbacks, and OnTick checked only the CTS, so a tick that had already been dispatched could invoke the callback after Stop() (or Dispose()) returned. Fix (Core and the duplicate UI.Core TimerService): a re-entrant gate lock plus a volatile running flag; OnTick starts the callback only inside the gate while running; Stop/Dispose clear the flag, then take the gate, so they return only after any starting invocation has finished its synchronous portion; Restart reopens the gate; Dispose detaches timer and CTS under the gate. ITimerHandle.Stop XML doc states the contract.
   Tests first: Stop_WhileCallbackInFlight_NoInvocationCompletesAfterStopReturns and Dispose_WhileCallbackInFlight_NoInvocationCompletesAfterDisposeReturns (10 iterations each; callback signals entry then sleeps 40 ms so it always overlaps Stop/Dispose) in Core.Tests and a new UI.Core.Tests TimerServiceTests. Red on the old implementation: Core 2 FAIL of 8, UI.Core 2 FAIL of 3 (red-core.log, red-uicore.log). Green after the fix.
2. b979309 test(phase2). The first full proof of 6c97627 (checkout1-Web.trx in the 192500Z dir) failed 2 Web tests under parallel load: VoiceClientStreamingTests (1 s and 500 ms wall-clock waits; failure then masked by NotSupportedException from disposing an async iterator mid-MoveNext) and MemoryPageTests.MemoryDetailPage_ShowsDisplayOnlyVersion_AndEditor (bUnit 1 s default WaitFor ceiling elapsed before the post-load render). Fixed without waivers or assertion changes: VoiceClient streaming is proven by ordering (server holds the stream until the test releases it; first chunk asserted while the stream is still open), 30 s ceilings on cross-process signals, server released before enumerator disposal; Web.Tests module initializer sets BunitContext.DefaultWaitTimeout to 30 s; Core and UI.Core timer tests wait on completion signals instead of fixed sleeps (Restart_WithNewInterval initial interval 10 s); AgentEventListenerCoordinatorTests signal ceiling 2 s -> 30 s.

## Results at b9793096 (every run counted; no reruns selected)

Timer stress: 10 x Core TimerServiceTests (8 tests) and 10 x UI.Core TimerServiceTests (3 tests), all exit 0 (stress.json).

| Run | Core | UI.Core | Director | Web | Desktop |
|---|---:|---:|---:|---:|---:|
| checkout1 (F:\GitHub\McpServerManager) | 143/143 | 406/406 | 140/140 | 122/122 | 6/6 |
| checkout2 | 143/143 | 406/406 | 140/140 | 122/122 | 6/6 |
| Web extra x3 (checkout, --no-build) | | | | 122/122 x3 | |
| nested1 (fresh git archive under McpServer worktree) | 143/143 | 406/406 | 140/140 | 122/122 | 6/6 |
| nested2 | 143/143 | 406/406 | 140/140 | 122/122 | 6/6 |

All rows Failed 0, Skipped 0. Nested expansion: docs\receipts\hv\p6m-194500\McpServerManager from git archive b9793096 plus Markdown.Avalonia d71a34d; ancestors include the worktree Directory.Packages.props/Directory.Build.props and F:\GitHub\McpServer\McpServer.sln, Directory.Packages.props, Directory.Build.props (nested-ancestors.txt). No isolation files, no filters.

Not re-run here (unchanged since r4 PASS): Manager DeployAllTargets Pester 3/3, Manager extension compile/Node tests 11/11, plugins 319/319, Tools static 18/18.

## Unchanged tips (HV r4 verified)

grok b2a0ec7e, grok-bot 6e1edf99, claude-code 2510cc1b, cowork 95bce132, codex ae35f225, copilot a0792f4f, cline c7f24495, cline-v2 1e13d8e1, opencode fc20aafd, Tools 11ee9124.

## Infrastructure note

Remote GitHub Actions checks do not start because the GitHub account is billing-locked. Operator directive: do not depend on GHA. Infrastructure, not a content blocker.