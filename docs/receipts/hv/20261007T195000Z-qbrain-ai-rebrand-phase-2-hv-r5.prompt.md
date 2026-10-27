# Hostile Verification: QBrain.AI rebrand Phase 2 (r5 after HV-R4-01 TimerService Stop race fix)

You are Astra conducting BDPv4 Hostile Verification at effort xhigh on PAYTON-LEGION2.

## Workspace lock (MEMORY-PROCEDURE-003 / operator mandatory)
- Review workspace / -C directory / $hvDir MUST be: F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007
- Do NOT use F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
- Write the receipt markdown ONLY under that worktree's docs/receipts/hv/, then copy to F:\GitHub\McpServer\docs\receipts\hv\

## Mission
Unbounded re-verification of every Phase 2 draft PR at the tips below: all repositories, full suites, no sampling. Operator lock: BDPv4 full-suite Failed 0 / Skipped 0. No quarantine, no waivers, no baseline exemptions, no selecting a passing rerun over a failing one. Do not merge. Target OverallVerdict AGREE with Accuracy >=98 and Completeness >=98.

## Prior findings closed
- HV-GREEN-01 CLOSED (r2): Cline tip c7f24495 commits memory-context.ps1
- HV-GREEN-02 CLOSED (r2): Grok Bot tip 6e1edf99 CORE-MANIFEST LF hashes match
- Manager UI.Core CLOSED (r2): tip 519a758
- Manager Desktop/Director/Web restore-build CLOSED (r3): tip 43d3158
- HV-R3-01 CLOSED (r4 receipt 20261007T185000Z): Director repo-root discovery anchored on McpServerManager.sln + src/McpServerManager.Director; Directory.Packages.props ManagePackageVersionsCentrally=false

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
- McpServerManager #10: b9793096df77a94228487da78f3914c80657d2ab (Markdown.Avalonia gitlink d71a34d8de3c675cd027920d8d2fe936e8a90455)
- McpServerTools #1: 11ee91241943c3032ce5ec6ea66fcb374f6dc518

Green proof: F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\20261007T194500Z-phase2-green-proof.md (evidence dir 20261007T194500Z-manager-green-proof)
Prior receipts (read-only): 20261007T185000Z-...-hv-r4.md (DISAGREE Acc97 Comp97; HV-R4-01), 20261007T184500Z-phase2-green-proof.md, 20261007T183000Z-...-hv-r3.md, 20261007T181500Z-...-hv-r2.md
Plan: docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md (do not edit)

## r5 delta to verify (Manager only; 8e1550d -> b9793096df77a94228487da78f3914c80657d2ab)
1. 6c97627 fix(phase2) HV-R4-01: Core and UI.Core TimerService TimerHandle gate each callback start behind a re-entrant lock plus running flag; Stop/Dispose close the gate and wait for any invocation that is starting, so no callback runs its synchronous portion after they return; Restart reopens; ITimerHandle.Stop doc states the contract. Tests first: Stop/Dispose-while-callback-in-flight tests (10 iterations each) in Core.Tests and new UI.Core.Tests TimerServiceTests were red on the old implementation (4 FAIL; logs in 20261007T192500Z-hv-r4-01-timer-fix\red-*.log) and green after.
2. b979309 test(phase2): the first full proof of 6c97627 exposed two more load-dependent Web failures (VoiceClientStreamingTests 1 s/500 ms waits; MemoryPageTests bUnit 1 s WaitFor ceiling). Evidence: 20261007T192500Z-hv-r4-01-timer-fix\checkout1-Web.trx. Fixed without waivers: VoiceClient streaming proven by ordering (server holds stream until released) with 30 s signal ceilings and no masked dispose; Web.Tests module initializer sets BunitContext.DefaultWaitTimeout to 30 s (assertions unchanged); timer tests wait on completion signals instead of fixed sleeps; AgentEventListenerCoordinatorTests ceiling 2 s -> 30 s. Scrutinize whether any of these weakens an assertion; report if so.
3. Manager suite counts at b9793096df77a94228487da78f3914c80657d2ab: Core 143, UI.Core 406, Director 140, Web 122, Desktop 6.

## Required checks
1. Fresh git-archive snapshots of ALL tips above, expanded inside the locked worktree (keep Manager expansion paths short enough for the HarfBuzz native loader; r4 documented the 259-char limit). Re-run full unit suites: Pester tests/ for shell plugins; npm ci + build + full test for Node plugins; dotnet test for all five Manager suites; Manager DeployAllTargets Pester and extension compile/Node tests; Tools static manifest/schema checks. Record Total/Passed/Failed/Skipped. Failed 0 and Skipped 0 required for AGREE.
2. Run the complete Manager five-suite set at least twice on fresh expansions; every run counts. Any failure in any run is a blocker.
3. CORE-MANIFEST raw git cat-file blob integrity for shell plugins (Grok Bot especially). Expect 0 mismatches.
4. Confirm R1-R3 still hold (ReplBridge 45/45, CORE sync, Cline package identity @qbrainai/qbrain-ai-cline-plugin).
5. Draft PRs remain OPEN/draft/unmerged. No Phase 3/4-L. No merges.

## Operator notes
- Remote GitHub Actions jobs do not start because the GitHub account is billing-locked. Operator directive: do not depend on GHA. Record it as infrastructure in the receipt, not as a content blocker; local full-suite execution on these exact tips is the gate.

## Output
Write receipt markdown ONLY to:
F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\20261007T195000Z-qbrain-ai-rebrand-phase-2-hv-r5.md
Also copy to F:\GitHub\McpServer\docs\receipts\hv\20261007T195000Z-qbrain-ai-rebrand-phase-2-hv-r5.md

Must include: OverallVerdict AGREE|DISAGREE, Accuracy, Completeness, ModelLock proof (gpt-6-astra / xhigh from THIS rollout), suite table covering all plugins + all five Manager suites (every run) + Manager additional suites + Tools static, tip SHAs, remaining blockers if any.

Be hostile: green claims are false until re-executed on these exact tips in the mandated worktree.