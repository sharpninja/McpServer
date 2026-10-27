# Hostile Verification: QBrain.AI rebrand Phase 2 (post green remediation)

You are Astra conducting BDPv4 Hostile Verification at effort xhigh.

## Mission
Verify Phase 2 draft PR contents on branch `cursor/qbrain-ai-rebrand-phase2-91dd` (and Manager #10 if still open) after full-suite green remediation. Prior HV `20261007T162924Z-...-r123b` was DISAGREE Acc96 Comp94 solely due to 63 failing tests + 4 Grok Bot CORE-MANIFEST mismatches. Operator directed: fix root causes, do NOT quarantine inherited fails, prove Failed 0, then re-HV.

## Scope
- Shell plugins: mcpserver-grok-plugin (GrokCode), grok-bot, claude-code, claude-cowork, codex, copilot
- Node: mcpserver-cline-plugin (and cline-v2 / opencode if still in Phase 2 PR set)
- Manager draft PR #10 if open
- Worktrees under F:\GitHub\* on PAYTON-LEGION2
- MCP_WORKSPACE_PATH=F:\GitHub\mcpserver when logging

## Tips to review (draft PRs; do not merge)
- mcpserver-grok-plugin: b2a0ec7e03869f0f2863728ccdc7da75ba1b0bda
- mcpserver-grok-bot-plugin: abb746cfdefba7cbcf06724a011b1568487ad155
- mcpserver-claude-code-plugin: 2510cc1be501b028e59736fb638cf38df3d29ccb
- mcpserver-claude-cowork-plugin: 95bce132f1c71e4c4d10bf3943e0c700ed5968f8
- mcpserver-codex-plugin: ae35f225bd896fdc2bea2fdd6b923703beaaec39
- mcpserver-copilot-plugin: a0792f4fcb1cca8e7ac8d5f20d6917218a30dbd0
- mcpserver-cline-plugin: 7a28d0294797bf643bdd23e3ecb7d03c3ac89473

Green proof: F:\GitHub\McpServer\docs\receipts\hv\20261007T174000Z-phase2-green-proof.md
Prior receipt: F:\GitHub\McpServer\docs\receipts\hv\20261007T162924Z-qbrain-ai-rebrand-phase-2-hv-r123b.md

## Required checks
1. Re-run applicable full unit suites (Pester tests/ for shell; npm test for Node). Record Total/Passed/Failed/Skipped. BDPv4: Failed must be 0 and Skipped 0 for exit-gate claims.
2. CORE-MANIFEST integrity for shell plugins (especially Grok Bot vs Grok) — no unexplained mismatches on committed blobs.
3. Confirm R1–R3 still hold (ReplBridge harness, CORE sync, Cline package identity).
4. Confirm rebrand identity strings/paths/lockfiles remain correct; no Phase 3/4-L scope creep.
5. Draft PRs remain open (do not merge).

## Output
Write receipt markdown to:
`F:\GitHub\McpServer\.worktrees\federation-fixes-20261007\docs\receipts\hv\20261007T173917Z-qbrain-ai-rebrand-phase-2-hv-green.md`
and copy to `F:\GitHub\McpServer\docs\receipts\hv\20261007T173917Z-qbrain-ai-rebrand-phase-2-hv-green.md`.

Include OverallVerdict AGREE|DISAGREE, Accuracy (0-100), Completeness (0-100), evidence table of suite counts, remaining fails if any, and tip SHAs reviewed.

Be hostile: treat green claims as false until re-executed on these tips.

Also still open (unchanged tips; re-verify suites if in Phase 2 scope):
- cline-v2 #5: 36c95e48c9452907463b44b5edf5ac6df4c4dbea
- opencode #4: fc20aafda2da612d72ad0171792e369e4438a121
- Tools #1: 11ee91241943c3032ce5ec6ea66fcb374f6dc518
- Manager: locate draft PR #10 (tip was 242a8968) if still open under McpServer / QBrain Manager repo
