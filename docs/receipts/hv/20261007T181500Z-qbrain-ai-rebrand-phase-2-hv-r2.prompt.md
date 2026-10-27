# Hostile Verification: QBrain.AI rebrand Phase 2 (r2 after tip fixes)

You are Astra conducting BDPv4 Hostile Verification at effort xhigh on PAYTON-LEGION2.

## Mission
Re-verify Phase 2 draft PR contents after resume fixes that closed HV-GREEN-01/02 from receipt 20261007T174500Z (DISAGREE Acc95 Comp92). Operator lock: BDPv4 full-suite Failed 0 / Skipped 0. No quarantine. Do not merge.

## Prior findings closed by new tips
- HV-GREEN-01: hooks/scripts/memory-context.ps1 is now COMMITTED on Cline tip c7f24495 (was untracked at 7a28d02).
- HV-GREEN-02: Grok Bot CORE-MANIFEST LF hashes aligned at tip 6e1edf99 (8 CRLF expected digests fixed).
- Manager UI.Core: WorkspaceContextViewModel wired in dispatch tests at tip 519a758 (403/0 locally).

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
- McpServerManager #10: 519a7588f8e8195fa81c0e9978a02b9f9ce63d14
- McpServerTools #1: 11ee91241943c3032ce5ec6ea66fcb374f6dc518

Green proof: F:\GitHub\McpServer\docs\receipts\hv\20261007T180500Z-phase2-green-proof.md
Prior receipts (read-only): 20261007T174500Z-...-hv-green.md, 20261007T162924Z-...-r123b.md
Plan: docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md (do not edit)

## Required checks
1. Fresh git-archive snapshots of the tips above. Re-run full unit suites (Pester tests/ for shell; npm ci+build+test for Node). Record Total/Passed/Failed/Skipped. Failed 0 and Skipped 0 required for AGREE on those suites.
2. CORE-MANIFEST raw git cat-file blob integrity for shell plugins (especially Grok Bot). Expect 0 mismatches vs committed LF blobs.
3. Confirm R1-R3 still hold (ReplBridge, CORE sync, Cline package identity @qbrainai/qbrain-ai-cline-plugin).
4. Manager: re-run UI.Core and Core; attempt Desktop/Director/Web. If Desktop/Director/Web are blocked by NuGet audit NU1902/NU1903 or Markdown.Avalonia nullable warnings-as-errors, document as environment/dependency restore-build blocks with evidence (not test skips). Do not invent a quarantine waiver.
5. Draft PRs remain OPEN/draft/unmerged. No Phase 3/4-L.

## Output
Write receipt markdown ONLY to:
F:\GitHub\McpServer\.worktrees\federation-fixes-20261007\docs\receipts\hv\20261007T181500Z-qbrain-ai-rebrand-phase-2-hv-r2.md
Also copy to F:\GitHub\McpServer\docs\receipts\hv\20261007T181500Z-qbrain-ai-rebrand-phase-2-hv-r2.md

Must include: OverallVerdict AGREE|DISAGREE, Accuracy, Completeness, ModelLock proof (gpt-6-astra / xhigh from THIS rollout), suite table, tip SHAs, remaining blockers.

Be hostile: green claims are false until re-executed on these exact tips.