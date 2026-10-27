# Phase 2 green proof r4 (HV-R3-01 Director root discovery closed)

- ReceiptId: 20261007T184500Z-phase2-green-proof
- Host: PAYTON-LEGION2
- Gate: BDPv4 Failed 0 / Skipped 0; no quarantine; draft tips only; no merges
- Workspace lock: F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007 (MEMORY-PROCEDURE-003)

## Change since HV r3 (DISAGREE Acc97 Comp97, HV-R3-01)

McpServerManager #10 draft tip 43d3158 -> 8e1550d508eee88635b47ce13ff4c9384849120a
- tests/McpServerManager.Director.Tests/DirectorRunner.cs: FindRepoRoot(startDirectory) requires McpServerManager.sln AND src/McpServerManager.Director; no longer matches an enclosing McpServer.sln.
- tests/McpServerManager.Director.Tests/DirectorRunnerRepoRootTests.cs: regression tests (nested under McpServer.sln returns Manager root; RepoRoot contains Director csproj). Red on old logic (FAIL observed), green after fix.
- Directory.Packages.props: ManagePackageVersionsCentrally=false so an enclosing repo's CPM props are not imported (closes the NU1008 restore failure r3 had to isolate around).

## Nested-environment proof (no isolation)

Fresh git archive of 8e1550d plus Markdown.Avalonia d71a34d, expanded at
F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\20261007T184500Z-manager-nested-proof\McpServerManager
(ancestors include F:\GitHub\McpServer\McpServer.sln and the worktree Directory.Packages.props). No empty parent props and no filters.

| Suite | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| Core | 141 | 141 | 0 | 0 |
| UI.Core | 403 | 403 | 0 | 0 |
| Director | 140 | 140 | 0 | 0 |
| Web | 122 | 122 | 0 | 0 |
| Desktop | 6 | 6 | 0 | 0 |

Logs and TRX: that directory (*.test.log, *.trx, summary.json).

## Unchanged tips (HV r3 verified: plugins 319/319, manifests 144/144, R1-R3 PASS)

grok b2a0ec7e, grok-bot 6e1edf99, claude-code 2510cc1b, cowork 95bce132, codex ae35f225, copilot a0792f4f, cline c7f24495, cline-v2 1e13d8e1, opencode fc20aafd, Tools 11ee9124.