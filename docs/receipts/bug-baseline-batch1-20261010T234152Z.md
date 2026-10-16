<!-- SPDX-License-Identifier: Apache-2.0 -->
# BUG-BASELINE batch 1 receipts (20261010T234152Z)

PLAN-SESSIONTURNREVIEW-001 P0.1 on LAB-OMARCHY (Linux, .NET SDK 10.0.201 from ~/.dotnet), worktree .claude/worktrees/session-20261009. RED for every item is the P0.1 baseline (docs/receipts/p01-baselines-20261010T215349Z.md). Raw green logs (worktree path made relative, API keys redacted) are in docs/receipts/_bug-baseline-batch1-20261010T234152Z/.

- BUG-BASELINE-011 (75798edd, test-only): the hosted-workflow recording handler's segment matches use qbrainai. QBrainAi.McpAgent.Tests: Failed!  - Failed:     7, Passed:    58, Skipped:     0, Total:    65 (all 7 were the BUG-BASELINE-002 WorkspacePath cases, fixed later in ba58603b).
- BUG-BASELINE-006 (e4eb24da, test-only): historical seed SQL uses IsMcpServerRelated. Unit and SQLite cases: Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5. ./build.ps1 MigrationIntegrationTests --skip InstallTestDependencies against the PostgreSQL and SQL Server containers: target Succeeded; QBrainAi.Support.Mcp.Tests Passed 49, Failed 0, Skipped 0; Build.Tests Passed 16, Failed 0, Skipped 0 (log not committed: the run loaded test-database credentials).
- BUG-BASELINE-004 (9538e11f, test-only): per-OS assertions for the handoff open-share test and the Codex reasoning-effort argument. Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15.
- BUG-BASELINE-002, -010, and the -003 session-log part (ba58603b, test-only): UnitTestPaths and the McpAgent TestWorkspacePath idiom. QBrainAi.McpAgent.Tests: Passed!  - Failed:     0, Passed:    65, Skipped:     0, Total:    65. Support.Mcp classes: Failed!  - Failed:     3, Passed:    79, Skipped:     0, Total:    82; the 3 failures are the BUG-BASELINE-001 GenerateWikiAsync cases.
- BUG-BASELINE-008 (33e6773f): RequirementsFourteenthLinuxNativeTests. Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3.

## Logs

- bug-baseline-011-green.log.txt: BUG-BASELINE-011 green: QBrainAi.McpAgent.Tests after 75798edd (7 remaining failures are BUG-BASELINE-002)
- bug-baseline-006-sqlite.log.txt: BUG-BASELINE-006 green: the 2 unit and 3 SQLite migration cases after e4eb24da
- bug-baseline-004-green.log.txt: BUG-BASELINE-004 green: HandoffContainedFileReaderTests and CodexCliAgentExecutionStrategyTests after 9538e11f
- bug-baseline-002-green3-mcpagent.log.txt: BUG-BASELINE-002 green: QBrainAi.McpAgent.Tests after ba58603b
- bug-baseline-002-green3-supportmcp.log.txt: BUG-BASELINE-002, -010, -003 (session-log part) green: the five Support.Mcp classes after ba58603b (3 remaining failures are BUG-BASELINE-001)
- bug-baseline-008-green.log.txt: BUG-BASELINE-008 green: the new Linux FIFO test and both harness inventory tests after 33e6773f
