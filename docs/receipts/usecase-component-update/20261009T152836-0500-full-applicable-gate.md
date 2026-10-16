# Use-case component update full applicable gate

Timestamp: 2026-10-09T15:28:36-05:00
Workspace: F:/GitHub/McpServer/.worktrees/usecase-edit-api-20261009
Branch: codex/usecase-edit-api-20261009
Pre-commit HEAD: f56dcf70e7e84f2d286e1ca9e0e6680133f09a82

## Build and traceability

Nuke Compile completed successfully against QBrainAi.sln after all 53 projects in the Nuke compile inventory were explicitly restored.
Nuke ValidateTraceability completed successfully and printed "Traceability validation passed."
git diff --check completed successfully with no output.

## Full applicable suite evidence

The first full Nuke Test run found 7 failures among 4,338 tests. All were outside the new use-case update behavior:

- three hosted-agent fake routes still expected the pre-rebrand /mcpserver prefix
- two historical migration fixtures seeded IsQBrainAiRelated although the historical schema column is IsMcpServerRelated
- two local evidence fixtures required committed test artifacts absent from this worktree

The stale route and historical-schema fixtures were corrected. The missing committed test artifacts were copied from the authoritative main checkout and hash-verified for local validation. The design document copied solely as a test fixture remains untracked and is excluded from the commit.

The subsequent batch had one unrelated intermittent failure:
HandoffDurabilityTests.ProcessingLease_RenewsAndFencesTerminalUpdates.
That test passed when run individually, and the remaining Support tests passed as a single batch.

Latest current-project results:

- QBrainAi.Client.Tests: 306 passed, 0 failed, 0 skipped
- QBrainAi.Cqrs.Tests: 33 passed, 0 failed, 0 skipped
- QBrainAi.Launcher.Tests: 20 passed, 0 failed, 0 skipped
- QBrainAi.McpAgent.Tests: 65 passed, 0 failed, 0 skipped
- QBrainAi.QBAgent.Tests: 96 passed, 0 failed, 0 skipped
- QBrainAi.Repl.Core.Tests: 868 passed, 0 failed, 0 skipped
- QBrainAi.Support.Mcp.Tests: 2,952 passed, 0 failed, 0 skipped, using the 2,951-test batch excluding the isolated intermittent test plus its passing isolated rerun

Aggregate latest applicable executions: 4,340 passed, 0 failed, 0 skipped.

## Nuke runner defect retained for triage

The Nuke Test target cannot currently provide one clean command receipt from this worktree for two pre-existing orchestration reasons:

- .nuke/parameters.json defaults to the removed McpServer.sln instead of QBrainAi.sln
- Restore restores only the selected solution while Compile enumerates every project, so compatibility projects outside the solution can lack assets

These orchestration defects do not change the individual current project results above. They must be triaged separately and must not be represented as a clean Nuke Test execution.
