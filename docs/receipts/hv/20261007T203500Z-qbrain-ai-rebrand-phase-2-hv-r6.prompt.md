# Hostile Verification: QBrain.AI rebrand Phase 2 (r6 after HV-R5-01 and HV-R5-02 TimerService fixes)

You are Astra conducting BDPv4 Hostile Verification at effort xhigh on PAYTON-LEGION2.

## Workspace lock (MEMORY-PROCEDURE-003 / operator mandatory)
- Review workspace / -C directory / $hvDir MUST be: F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007
- Do NOT use F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
- Write the receipt markdown ONLY under that worktree's docs/receipts/hv/, then copy to F:\GitHub\McpServer\docs\receipts\hv\

## Mission
Unbounded re-verification of every Phase 2 draft PR at the tips below: all repositories, full suites, no sampling. Operator lock: BDPv4 full-suite Failed 0 / Skipped 0. No quarantine, no waivers, no baseline exemptions, no selecting a passing rerun over a failing one. Do not merge. Target OverallVerdict AGREE with Accuracy >=98 and Completeness >=98.

## Prior findings closed (re-confirm, do not assume)
- HV-GREEN-01 CLOSED (r2): Cline tip c7f24495 commits memory-context.ps1
- HV-GREEN-02 CLOSED (r2): Grok Bot tip 6e1edf99 CORE-MANIFEST LF hashes match
- Manager UI.Core CLOSED (r2); Manager Desktop/Director/Web restore-build CLOSED (r3)
- HV-R3-01 CLOSED (r4): Director repo-root discovery anchored on McpServerManager.sln + src/McpServerManager.Director
- HV-R4-01 CLOSED per r5 receipt 20261007T195000Z (gate serializes callback start against Stop/Dispose); keep verifying its tests still run and pass

## Tips to review (OPEN DRAFT; immutable heads)
- mcpserver-grok-plugin #6: b2a0ec7e03869f0f2863728ccdc7da75ba1b0bda
- mcpserver-grok-bot-plugin #1: 6e1edf99f6b5c40fd3576e364b7cd725261b7df1
- mcpserver-claude-code-plugin #6: 2510cc1be501b028e59736fb638cf38df3d29ccb
- mcpserver-claude-cowork-plugin #5: 95bce132f1c71e4c4d10bf3943e0c700ed5968f8
- mcpserver-codex-plugin #5: ae35f225bd896fdc2bea2fdd6b923703beaaec39
- mcpserver-copilot-plugin #5: a0792f4fcb1cca8e7ac8d5f20d6917218a30dbd0
- mcpserver-cline-plugin #5: c7f2449550aadf66f24ce8153138c0411614efc9
- mcpserver-cline-v2-plugin #5: 1e13d8e12f74d92b5bdc6f367e86cbd2201f066e
- mcpserver-opencode-plugin #4: fc20aafda2da612d72ad0171792e369e4438a121
- McpServerManager #10: d0082d0e02a8e3f10bab5dfd5e5920784ecf8233 (Markdown.Avalonia gitlink d71a34d8de3c675cd027920d8d2fe936e8a90455)
- McpServerTools #1: 11ee91241943c3032ce5ec6ea66fcb374f6dc518

Green proof: F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\20261007T202100Z-phase2-green-proof.md (evidence dir 20261007T202100Z-manager-green-proof)
Fix evidence: docs\receipts\hv\20261007T201500Z-hv-r5-fix\ (red-*.log, red2-*.log, mutant-interval-*.log, mutant-interval.diff, green*.log)
Prior receipts (read-only): 20261007T195000Z-...-hv-r5.md (DISAGREE Acc96 Comp97; HV-R5-01, HV-R5-02) and its -evidence dir (reuse its DisposeCancellationProbe and timer-mutation harness against the new tip), 20261007T185000Z-...-hv-r4.md, 20261007T183000Z-...-hv-r3.md
Plan: docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md (do not edit)

## r6 delta to verify (Manager only; b9793096df77a94228487da78f3914c80657d2ab -> d0082d0e02a8e3f10bab5dfd5e5920784ecf8233, single commit)
1. HV-R5-02 (P1): Core and UI.Core TimerHandle Stop() and Dispose() now cancel the current run's token BEFORE waiting on the gate, so a synchronous cancellation-aware callback can exit instead of deadlocking; the gate still guarantees no callback starts its synchronous portion after Stop/Dispose return. Each run owns a CancellationTokenSource; Restart after Stop installs a fresh, uncancelled source (a cancelled source of a stopped run is replaced, not disposed). Dispose cancels but no longer disposes the source, so an in-flight async continuation can still observe its cancelled token after Dispose returns. ITimerService/ITimerHandle docs updated (Stop now cancels the in-flight token; Restart after Stop supplies a new token). Production callers use Dispose; no production caller uses Stop.
   Tests first, Core and UI.Core parity: Dispose_WhileCancellationAwareCallbackBlocks_CancelsTokenAndReturns, Stop_WhileCancellationAwareCallbackBlocks_CancelsTokenAndReturns (red on b979309: 2 FAIL Core, 2 FAIL UI.Core; red-core.log, red-uicore.log), Restart_AfterStop_SuppliesUncancelledToken, Dispose_WhileAsyncCallbackPending_TokenStaysObservableAfterDisposeReturns (red with ObjectDisposedException before the no-dispose change; red2-*.log).
2. HV-R5-01 (P2): Restart_WithNewInterval_UsesNewInterval (both suites) starts with a 1 h interval, asserts no fire after 100 ms, restarts with 50 ms, and requires two fires within 30 s. Executor mutation proof: replacing `_interval = newInterval.Value;` with an empty block in both TimerService copies makes the test FAIL in Core and UI.Core (mutant-interval-core.log, mutant-interval-uicore.log, mutant-interval.diff); source restored afterwards. Re-run the r5 interval-ignoring mutant harness yourself against the new test file; it must be rejected.
3. r5 caveat: VoiceClientStreamingTests finally block now releases the stream and awaits any outstanding MoveNextAsync (30 s ceiling) before await-using disposal.
4. Manager suite counts at d0082d0: Core 147, UI.Core 411, Director 140, Web 122, Desktop 6 (826 per five-suite run).
Scrutinize whether the new cancel-before-wait ordering reopens HV-R4-01 (any callback synchronous start after Stop/Dispose return), whether Stop/Restart/Dispose races can leak a cancelled token into a new run or an uncancelled token into a stopped run, and whether any test assertion was weakened.

## Required checks
1. Fresh git-archive snapshots of ALL tips above, expanded inside the locked worktree (keep Manager expansion paths short for the HarfBuzz native loader; r4 documented the 259-char limit). Re-run full unit suites: Pester tests/ for shell plugins; npm ci + build + full test for Node plugins; dotnet test for all five Manager suites; Manager DeployAllTargets Pester and extension compile/Node tests; Tools static manifest/schema checks. Record Total/Passed/Failed/Skipped. Failed 0 and Skipped 0 required for AGREE.
2. Run the complete Manager five-suite set at least twice on fresh expansions; every run counts. Any failure in any run is a blocker.
3. Re-run the r5 DisposeCancellationProbe against d0082d0 Core and UI.Core sources: Dispose must return and cancel without rescue.
4. CORE-MANIFEST raw git cat-file blob integrity for shell plugins (Grok Bot especially). Expect 0 mismatches.
5. Confirm R1-R3 still hold (ReplBridge 45/45, CORE sync, Cline package identity @qbrainai/qbrain-ai-cline-plugin).
6. Draft PRs remain OPEN/draft/unmerged. No Phase 3/4-L. No merges.

## Operator notes
- Remote GitHub Actions jobs do not start because the GitHub account is billing-locked. Operator directive: do not depend on GHA. Record it as infrastructure in the receipt, not as a content blocker; local full-suite execution on these exact tips is the gate.

## Output
Write receipt markdown ONLY to:
F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\20261007T203500Z-qbrain-ai-rebrand-phase-2-hv-r6.md
Also copy to F:\GitHub\McpServer\docs\receipts\hv\20261007T203500Z-qbrain-ai-rebrand-phase-2-hv-r6.md

Must include: OverallVerdict AGREE|DISAGREE, Accuracy, Completeness, ModelLock proof (gpt-6-astra / xhigh from THIS rollout), suite table covering all plugins + all five Manager suites (every run) + Manager additional suites + Tools static, tip SHAs, remaining blockers if any.

Be hostile: green claims are false until re-executed on these exact tips in the mandated worktree.
