# Hostile Verification: QBrain.AI rebrand Phase 2 (r4 after HV-R3-01 Director root discovery fix)

You are Astra conducting BDPv4 Hostile Verification at effort xhigh on PAYTON-LEGION2.

## Workspace lock (MEMORY-PROCEDURE-003 / operator mandatory)
- Review workspace / -C directory / $hvDir MUST be: F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007
- Do NOT use F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
- Write the receipt markdown ONLY under that worktree's docs/receipts/hv/, then copy to F:\GitHub\McpServer\docs\receipts\hv\

## Mission
Re-verify Phase 2 draft PR contents after Manager tip 43d3158 closed the HV-r2 Desktop/Director/Web restore-build blocks. Operator lock: BDPv4 full-suite Failed 0 / Skipped 0. No quarantine. Do not merge. Target OverallVerdict AGREE with Accuracy >=98 and Completeness >=98.

## Prior findings closed
- HV-GREEN-01 CLOSED (r2): Cline tip c7f24495 commits memory-context.ps1
- HV-GREEN-02 CLOSED (r2): Grok Bot tip 6e1edf99 CORE-MANIFEST LF hashes match
- Manager UI.Core CLOSED (r2): tip 519a758 WorkspaceContextViewModel wiring; 403/0
- Manager Desktop/Director/Web CLOSED (this tip 43d3158): Markdown.Avalonia master d71a34d Avalonia 12; Desktop Avalonia 12.0.3 + Tmds.DBus.Protocol 0.95.1; NU190* package bumps; Directory.Build.targets Avalonia nullable not errors. Local suites: Core 141, UI.Core 403, Director 138, Web 122, Desktop 6; all Failed 0 Skipped 0.

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
- McpServerManager #10: 8e1550d508eee88635b47ce13ff4c9384849120a
- McpServerTools #1: 11ee91241943c3032ce5ec6ea66fcb374f6dc518

Green proof: F:\GitHub\McpServer\docs\receipts\hv\20261007T184500Z-phase2-green-proof.md (also under this worktree)
Prior receipts (read-only): 20261007T183000Z-...-hv-r3.md (DISAGREE Acc97 Comp97; HV-R3-01), 20261007T182800Z-phase2-green-proof.md, 20261007T181500Z-...-hv-r2.md, 20261007T180500Z-phase2-green-proof.md, 20261007T174500Z-...-hv-green.md, 20261007T162924Z-...-r123b.md
Plan: docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md (do not edit)

## r4 delta to verify
- HV-R3-01 (r3 P1): Manager tip 8e1550d changes DirectorRunner.FindRepoRoot to require McpServerManager.sln plus src/McpServerManager.Director, adds DirectorRunnerRepoRootTests (Director suite is now 140 tests), and adds Directory.Packages.props (ManagePackageVersionsCentrally=false) so the enclosing McpServer CPM props are no longer imported. Run Manager suites from a fresh archive expanded inside the locked worktree WITHOUT the r3 empty-parent isolation; restore must succeed on its own.
- Plugins and Tools tips are unchanged from r3; still re-run them in full.

## Required checks
1. Fresh git-archive snapshots of the tips above. Re-run full unit suites (Pester tests/ for shell; npm ci+build+test for Node; dotnet test for all five Manager suites including Desktop/Director/Web). Record Total/Passed/Failed/Skipped. Failed 0 and Skipped 0 required for AGREE.
2. CORE-MANIFEST raw git cat-file blob integrity for shell plugins (especially Grok Bot). Expect 0 mismatches vs committed LF blobs.
3. Confirm R1-R3 still hold (ReplBridge, CORE sync, Cline package identity @qbrainai/qbrain-ai-cline-plugin).
4. Manager Desktop/Director/Web MUST execute (not N/E). Treat any new restore/build/test fail as a real blocker; no quarantine waiver.
5. Draft PRs remain OPEN/draft/unmerged. No Phase 3/4-L. No merges.

## Output
Write receipt markdown ONLY to:
F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\20261007T185000Z-qbrain-ai-rebrand-phase-2-hv-r4.md
Also copy to F:\GitHub\McpServer\docs\receipts\hv\20261007T185000Z-qbrain-ai-rebrand-phase-2-hv-r4.md

Must include: OverallVerdict AGREE|DISAGREE, Accuracy, Completeness, ModelLock proof (gpt-6-astra / xhigh from THIS rollout), suite table covering all plugins + all five Manager suites + Tools static, tip SHAs, remaining blockers if any.

Be hostile: green claims are false until re-executed on these exact tips in the mandated worktree.