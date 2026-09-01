# MCP-HANDOFFPLAN-001 Codex NOT APPROVED remediations

Written: 2026-08-17T03:54:35Z
Session: GrokCode-20260816T175800Z-mcp-handoff-001
Turn: req-20260817T025955Z-010-codex-not-approved-fixes
Agent: GrokCode
TODOs: MCP-HANDOFF-001 Done=false; MCP-HANDOFFPLAN-001 Done=false
Commit: none

## Scope

Independent Codex review VERDICT: NOT APPROVED. This turn remediates P1-1 through P3 against already-specified handoff behavior. Planning was not restarted. Unrelated SessionLogSanitizer timeout work and compressed historical acceptance-criteria edits were not used as handoff evidence and were not modified in this turn.

## Decisions

- Treat the Codex report as a pre-approved bug report.
- Reject custom PromptTemplateId. Replay identity uses only the immutable canonical prompt version.
- Heartbeat renews ProcessingLeaseExpiresAtUtc on a fresh DbContext and does not increment StateVersion. Takeover and terminal completion remain owner plus StateVersion fenced.
- Approval completion is fenced on ApprovalOwner plus StateVersion after detach/reload.
- One shared TodoPayloadFingerprint: same idempotency key plus exact normalized payload heals; any mismatch is Conflict.
- SaveRunAfterTodo reports Created only after a fresh-context TODO lookup and a durable ExecuteUpdate receipt. Otherwise CompensationFailed. Compensation tokens are bounded and disposed.
- AgentPool executor receives raw handoff prompt. Queue, DTOs, notifications, and LastRequestPrompt publish only [REDACTED:handoff-source:{sha256}].
- Contained file open uses FileShare.Read, dynamic GetFinalPathNameByHandle buffers, correct \\?\UNC\ normalization, and fail-closed resolution.
- Handoff enums are string-only. Enum.IsDefined is enforced at the service and MCP/REPL boundaries. MVC JSON uses allowIntegerValues=false.
- One HandoffWorkspacePaths.Canonicalize value is pushed into WorkspaceContext, McpDbContext, and WorkspaceServiceAccessor.
- ReplayOfRunId is removed from the entity and all three AddHandoffIngestionStorage migrations, designers, and snapshots. HandoffReviewState.Approved stays removed.
- HTTP ErrorCode mapping: 404/409/413/5xx/400. 5xx responses replace provider messages with "Handoff processing failed."

## File counts

- Handoff-related dirty status rows in this worktree: 79 (10 tracked modifications plus 69 untracked handoff product/test/doc files, including prior receipts).
- Unrelated dirty files left untouched: docs/Project/Functional-Requirements.md, docs/Project/Technical-Requirements.md, tests/.../SessionLogSanitizerTests.cs (and any existing SessionLogSanitizer timeout edits).
- New focused test files this remediation: TodoPayloadFingerprintTests.cs, HandoffContainedFileReaderTests.cs, HandoffBoundedSourceTests.cs, HandoffStrictEnumAndHttpTests.cs, OneShotSensitivePromptPolicyTests.cs, HandoffWorkspacePathsTests.cs, HandoffHttpEnumIntegrationTests.cs.

git diff --check on the handoff path set: CHECK_EXIT=0.

## Validation receipts

- Focused handoff/fingerprint/AgentPool redaction filter: Failed 0, Passed 124, Skipped 0. EXIT=0.
- Client: Failed 0, Passed 281, Skipped 0. EXIT=0.
- Repl.Core: Failed 0, Passed 826, Skipped 0. EXIT=0.
- Support.Mcp unit Category!=Integration: Failed 0, Passed 1952, Skipped 0. EXIT=0.
- Repl.IntegrationTests: Failed 0, Passed 181, Skipped 0. EXIT=0.
- AddHandoffIngestionStorage serial provider cycle (SQLite, SQL Server LocalDB sandbox, PostgreSQL): Failed 0, Passed 3, Skipped 0. EXIT=0. Each test applies head, round-trips HandoffIngestionRuns/HandoffDiagnostics, downgrades to the preceding migration, and re-upgrades. No competing testhost LocalDB suite was observed; SQL Server ran in this serial pass.
- ./build.ps1 Compile from F:\GitHub\McpServer: Succeeded, COMPILE_EXIT=0, 0 Warning(s), 0 Error(s).
- ./build.ps1 Test from F:\GitHub\McpServer: Succeeded, TEST_EXIT=0 (Support 1952, Client 281, Cqrs 33, Launcher 20, McpAgent 63, Repl.Core 826, QBAgent 50; all Failed 0 Skipped 0).
- ./build.ps1 ValidateTraceability: TRACE_EXIT=0. "Traceability validation passed."
- ./build.ps1 SyncAgentPlugins: SYNC_EXIT=0. Plugin skill hash test PluginSync_HandoffSkill_MatchesCoreArtifact: Passed 1 / Failed 0.

## Combined Support integration (honest)

- Full project run 1: Failed 1, Passed 268, Skipped 0. Failure: MarkerRegenerationIntegrationTests.WorkspacePromptUpdate_RegeneratesMarkerFile (10s marker write timeout). Isolated rerun of that test: Passed 1.
- Full project run 2: Failed 2, Passed 267, Skipped 0. Failures: MarkerRegenerationIntegrationTests.GlobalPromptUpdate_RegeneratesMarkerFile (same timeout class) and QuadBrainOllamaEndpointIntegrationTests.QBAgentRunLoop_LocalOllamaDefaultModel_DisplaysQuadBrainResponse (missing ArbiterOfTruth output). Isolated rerun of Marker + Ollama + HandoffHttpEnum: Failed 0, Passed 6, Skipped 0.
- These two failures are not handoff-path tests. I do not claim the combined Support.Mcp.IntegrationTests project is Failed 0 under suite load.

## Remaining blockers

- Combined Support.Mcp.IntegrationTests is not a stable Failed 0 / Skipped 0 run because of unrelated Marker regeneration timeouts and local Ollama QuadBrain output. Isolated those tests pass.
- MCP-HANDOFF-001 and MCP-HANDOFFPLAN-001 stay open. Independent Codex re-review is still required. Hostile validator was not spawned.
- No commit, merge, or push.

## Not claimed

- I do not claim MCP-HANDOFF-001 or MCP-HANDOFFPLAN-001 complete.
- I do not claim sanitizer or compressed-requirements dirty files as handoff evidence.
- I do not claim the combined Support integration project is green under suite load.
