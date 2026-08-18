# MCP-HANDOFFPLAN-001 receipt for Codex review

UTC written: 2026-08-16T23:32:00Z
Agent: GrokCode
Session: GrokCode-20260816T175800Z-mcp-handoff-001
Turn: req-20260816T224629Z-007-handoff-audit-defects
TODOs: MCP-HANDOFF-001 and MCP-HANDOFFPLAN-001 remain open (done=false). No commit, merge, or push.

This is not an implementation-complete claim. Unfiltered Support.Mcp.Tests is still red on the four SQL Server LocalDB migration tests.

## Marker / plugin

- Marker signature verified True.
- Health nonce 55e5c67e81f24d3184d661a87b3e92b3 echoed Healthy.
- Plugin version from F:\GitHub\mcpserver-grok-plugin\.version: 1.89.0 (marker field 1.86.0 is stale).

## Required command receipts

- Client.Tests: Failed 0, Passed 281, Skipped 0, EXIT=0.
- Repl.Core.Tests: Failed 0, Passed 824, Skipped 0, EXIT=0.
- Unfiltered Support.Mcp.Tests (2026-08-16T23:07Z): Failed 4, Passed 1904, Skipped 0, Total 1908, EXIT=1, wall 13m46s. The four failures are the SQL Server LocalDB migration tests.
- Support.Mcp.IntegrationTests: Failed 0, Passed 264, Skipped 0, EXIT=0, duration 5m5s.
- Repl.IntegrationTests: Failed 0, Passed 181, Skipped 0, EXIT=0, duration 8m16s.
- ./build.ps1 Compile: EXIT=0.
- ./build.ps1 Test (Nuke, Category!=Integration): EXIT=0. This excludes the four Category=Integration LocalDB tests.
- ./build.ps1 ValidateTraceability: EXIT=0.
- ./build.ps1 SyncAgentPlugins: EXIT=0.
- Focused handoff + SessionLogSanitizerTests + SessionLogSanitizerTimeoutTests: Failed 0, Passed 60, Skipped 0, EXIT=0.

## LocalDB diagnosis (exact)

- CREATE DATABASE is fast.
- Last captured SQL on the 1904/4 unfiltered run was:
  EXEC @result = sp_releaseapplock @Resource = '__EFMigrationsLock', @LockOwner = 'Session';
  SqlException: Execution Timeout Expired after ~180s.
- SQL error log also showed login 18456 state 38 (connect to Database=name before the catalog existed), then READ_COMMITTED_SNAPSHOT, then ~3 minute drops.
- L48Peak remained running (about 8.8 GiB earlier). This agent did not terminate it. Free RAM during hangs was a few hundred MB.
- Attempted remediations: reuse MSSQLLocalDB (no extra sqlservr), EF auto-create, orphan cleanup (0 leftover mcp_*.mdf), last-SQL interceptor, Connect Retry Count=0, Pooling=false, disable EF __EFMigrationsLock via test history repository, leftover mcp_backfill_/mcp_quadbrain_ drop. Command Timeout left at 180s.
- Best focused SQL result after lock disable: Failed 1, Passed 3, Skipped 0. The remaining failure last SQL was the 4NF backfill INSERT INTO RequirementAcceptanceCriteria ... SELECT ... OPENJSON. Later reruns under lower free RAM went back to 3/1 and 4/0. A 384 MB max-server-memory experiment made results worse and was reverted; instance memory was reset to 2147483647.

## Handoff audit defects

Implemented in the dirty worktree with behavior tests in HandoffAuditDefectTests, validator/parser/controller/MCP/skill tests:

1. Unique ReplayIdentity reservation before extraction; unique-violation race returns the existing run.
2. TODO create then run update; SaveChanges failure after create heals from existing TODO id.
3. Removed static ApprovalGates; ExecuteUpdate PendingReview to Approving; Created is not regressed.
4. Succeeded/Error/ErrorCode persisted; MapEntity no longer forces Success=true.
5. OperationCanceledException propagates; no persist with a canceled token.
6. Invalid/malformed drafts are Failed and not approvable.
7. MCP/Director reject invalid mode; MCP accepts promptTemplateId; Director passes agent/template/notes and CancellationToken.
8. WorkspaceServiceAccessor AsyncLocal override used from ApplyWorkspaceOverride.
9. Extractor copies enqueue/job Model; missing model adds provenance_model_missing.
10. Parser rejects unknown fields; Success requires zero parse errors.
11. Validator uses live FR/TR/TEST multi-segment regex; service diagnoses missing requirement/dependency ids.
12. Handle-safe async reader (GetFinalPathNameByHandle on Windows) with 8 MiB bound.
13. SessionLogSanitizer applied to draft/diagnostic/review/TODO text before persist.
14. Director approve Notes + command CancellationToken.
15. Controller 404 uses ErrorCode run_not_found.
16. HandoffSkillDelegationTests dispatches skill-documented workflow.handoff.* through HandoffWorkflow to /mcpserver/handoff/*.

## Not done

- Unfiltered Support.Mcp.Tests is not Failed 0 / Skipped 0.
- Hostile OverallVerdict AGREE was not obtained this turn.
- MCP-HANDOFF-001 and MCP-HANDOFFPLAN-001 were not marked done.
