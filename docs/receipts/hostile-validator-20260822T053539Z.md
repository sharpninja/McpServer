# Hostile Validator Receipt

TimestampUtc: 2026-08-22T05:35:39Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-red-P15 ONLY, failsafe pending isolation red tests). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not require P15 green. Do not write P16. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.100.0; .version 1.100.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin. Plugin Test-MarkerSignature true. Health nonce nonce-hv-20260822052804-85089 echoed exactly.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P15: P15 red Theory_{Agent}_Success_NoPendingFailsafe; Theory_{Agent}_FailedSubmit_RetainsRootIdPending; Theory_{Agent}_RetrySuccess_DeletesOnlyMatchingPending. Requires Phase B drain timeout and V4 failsafe path. Maps remaining TEST-MCP-PLUGININT-001 AC2 isolation plus failsafe. Hostile C-red-P15. Then P15 green.
Requirement IDs: FR-MCP-PLUGININT-001; TR-MCP-PLUGININT-001 AC failsafe cleanup; TEST-MCP-PLUGININT-001 AC2 failsafe half.
Prior receipt: docs/receipts/hostile-validator-20260822T050748Z.md (OverallVerdict AGREE, FailCount 0, C-green-P14; full Failed 0 Passed 62 Skipped 0 Total 62; P15NamedTestsPresent false).
Collector: docs/receipts/_hv-c-red-p15/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-read PluginSessionLogWorkflowAdapterTests.cs, PluginSessionLogWorkflowAdapter.cs, PluginSessionLogWorkflowResult.cs, and plugins/core/lib-ps/resolve-cache-dir.ps1; grepped P16 AiTheory_ (absent in PluginIntegration.Tests); grepped Skip attributes (absent); live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 plus FR/TR/TEST/listMappings through the Grok plugin; listed PluginIntegration tests (P15 Success 8, FailedSubmit 8, Retry 8, P16 0); and independently re-ran the required filter via a WMI-created pwsh that outlived the tool job object:

- `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter "FullyQualifiedName~Theory_Agent_Success_NoPendingFailsafe|FullyQualifiedName~Theory_Agent_FailedSubmit_RetainsRootIdPending|FullyQualifiedName~Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending"` ExitCode 1. Console: Failed! Failed: 24, Passed: 0, Skipped: 0, Total: 24, Duration: 4 m 20 s. TRX executed 24 passed 0 failed 24 notExecuted 0 unitCount 24. Success rows 8 fail with "{hostKind} did not verify the V4 failsafe pending path." FailedSubmit rows 8 throw ExecuteFailedSubmitAsync is not implemented. Retry rows 8 throw RetryFailedSubmitAsync is not implemented. Log: docs/receipts/_hv-c-red-p15/hv-dotnet-p15-filter.log TRX: docs/receipts/_hv-c-red-p15/trx-p15-hv/p15-filter.trx.

Implementer chat and implementer logs were not trusted as proof. This review did not implement failsafe verification or write P16 tests.

## Clock and live MCP

Health GET /health?nonce=nonce-hv-20260822052804-85089 returned status Healthy, storage reachable, nonce echoed exactly. Plugin Test-MarkerSignature true. Tool registry exact name mcpserver-grok-plugin present.

## Session log proof

Dedicated persistence is through plugin `client.SessionLog.*` as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T052803Z-c-red-p15. Turn requestId req-20260822T052803Z-001-hostile-c-red-p15. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42894. CompleteTurnAsync same requestId after this receipt. Query proof files under docs/receipts/_hv-c-red-p15/ (sl-open.txt, sl-begin.txt, sl-dialog-start.txt, sl-complete.txt, sl-query-sid-after.txt, sl-query-history-after.txt).

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Explicit UNKNOWN list

None.

## Residual (not extra FAILs)

- Independent filter Duration 4 m 20 s (wall DurationMs 279170, TRX LastWriteTimeUtc 2026-08-22T05:33:04Z). Implementer last saw 5 m 19 s. Counts and failure messages match; duration is not a C-red gate.
- Homemade HMAC hex did not match marker value (collector marker-sig.json Match false). Plugin Test-MarkerSignature true and Invoke-FullBootstrap true are the contract path used here.
- FailedSubmit body asserts FailsafePathVerified plus nonempty pending files. It does not assert the pending filename is root-id-named. Retry body asserts FailsafePathVerified plus empty pending if the directory exists. It does not seed a sibling pending file and assert only the matching file is deleted. Same residual class as C-red-P14 sibling-cache: named theories exist and currently fail closed; tightening assertions is a later/green concern, not a C-red FAIL.
- PluginSessionLogWorkflowAdapterTests class XML still says C-red-P11 / TEST-MCP-PLUGININT-001 AC1. P15 methods document P15 red without repeating the TEST id. Stale class comment only.
- ExecuteCanonicalTurnAsync still persists via fixture.CreateTrustedClient and discards LaunchAsync. P15 DoD is the named failsafe theories, not production plugin persist. Persist remains later/residual work.
- Adapter ExecuteFailedSubmitAsync and RetryFailedSubmitAsync exist only as not-implemented throws so the red tests compile. FailsafePathVerified is not assigned in ExecuteCanonicalTurnAsync (defaults false). That is the intended red state.
- TRX Counters.skipped attribute is missing (null). Console Skipped: 0; TRX notExecuted=0; skippedNames empty.
- list-tests unique-line count 88 includes the project-to-dll banner line. Independent P15 counts are Success 8 + FailedSubmit 8 + Retry 8. P16 0. Cited evidence is hv-list-fqn-count.json plus TRX unitCount 24, not the 88 banner-inclusive total.
- PythonProcessCount 1 is Google Cloud SDK gcloud.py auth login (PID 69564). This validator did not invoke python/python3/py.
- workflow.* result envelopes include deprecated: true. Treated as success metadata.
- FR/TR/TEST createdAt stamps look like query time. AC text is present. isSatisfied remains false; this C-red-P15 gate does not require store AC complete or TODO done:true.
- PLAN remaining combined task "C P14 red + hostile then green; P15 red + hostile then green." stays done:false because P15 green has not started. Correct.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/. Untracked test project does not by itself block this red gate.
- Marker plugin_version 1.97.0 drifted. Plugin .version and plugin.json are 1.100.0 (authoritative).
- Plugin Status JSON agent field is GrokCode while session sourceType is GrokSubagentHostile. failsafeDir used GrokSubagentHostile and the UTF-8 base64url workspace key RjpcR2l0SHViXE1jcFNlcnZlcg, matching Get-McpFailsafeWorkspaceKey.

## Claims reviewed

### A Requested

#### A1. Three named P15 theories exist, each with eight PluginHostKind InlineData rows, no Skip. V4 path {workspace}/.mcpServer/failsafe/{agent}/workspaces/{workspace-key}/pending/ (base64url of UTF-8 full workspace path). Maps remaining TEST-MCP-PLUGININT-001 AC2 isolation plus failsafe.

Verdict: PASS

Evidence: File read this review. Methods at tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs lines 203, 234, 263. Each has `[Theory(Timeout = 120000)]` plus eight `[InlineData(PluginHostKind.*)]` rows (Codex, ClaudeCode, ClaudeCowork, Copilot, Grok, Cline, ClineV2, OpenCode). Grep Skip / Fact(Skip / Theory(Skip / Assert.Skip in PluginIntegration.Tests: 0 hits. ResolveFailsafePendingDirectory uses Path.GetFullPath, UTF-8 bytes, base64url Replace +/ and TrimEnd '=', Path.Combine(full, ".mcpServer", "failsafe", agentSourceType, "workspaces", b64, "pending"). Core Get-McpFailsafeDir in plugins/core/lib-ps/resolve-cache-dir.ps1 documents the same V4 tree. Plugin Status failsafeDir key matches UTF-8 base64url of F:\GitHub\McpServer.

#### A2. Currently red. Independent re-run of the three-theory filter Failed 24 Passed 0 Skipped 0. Success rows fail because FailsafePathVerified is false. FailedSubmit rows fail because ExecuteFailedSubmitAsync throws not implemented. Retry rows fail because RetryFailedSubmitAsync throws not implemented.

Verdict: PASS

Evidence: WMI Win32_Process Create PID 71784, tests-run.ps1, filter ExitCode 1 DurationMs 279170 FinishedUtc 2026-08-22T05:33:05Z. Console Failed! Failed: 24, Passed: 0, Skipped: 0, Total: 24, Duration: 4 m 20 s. TRX outcome Failed executed 24 passed 0 failed 24 notExecuted 0 unitCount 24. failsafeVerifiedFalseMessageCount 8. executeFailedNotImplementedCount 8. retryNotImplementedCount 8. Adapter ExecuteCanonicalTurnAsync does not assign FailsafePathVerified. ExecuteFailedSubmitAsync and RetryFailedSubmitAsync throw InvalidOperationException with those exact messages.

#### A3. P16 AiTheory_ named tests are absent. Do FAIL if mixed in, or if any of the 24 P15 rows pass or skip.

Verdict: PASS

Evidence: Grep AiTheory_ in tests/McpServer.PluginIntegration.Tests: 0 hits. list-tests P16 0. Filter TRX p16Count 0, p15Passed 0, p15Skipped 0, passedNames empty, skippedNames empty. All 24 failedNames are the three P15 theories times eight hosts.

#### A4. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Live todo_get.

Verdict: PASS

Evidence: workflow.todo.get and client.Todo.GetAsync via Invoke-McpPlugin at 2026-08-22T05:28:29Z to 05:28:40Z. PLAN-PLUGINHANDOFF-001 top done: false. Combined task `C P14 red + hostile then green; P15 red + hostile then green.` done: false. MCP-PLUGININT-001 done: false. P15 implementationTask done: false. This review wrote no todo_update. git status --porcelain for docs/Project/TODO.yaml and docs/todo.yaml was empty.

#### A5. Prior C-green-P14 AGREE is docs/receipts/hostile-validator-20260822T050748Z.md (Passed 62 Failed 0 Skipped 0; P15 absent).

Verdict: PASS

Evidence: Prior md OverallVerdict AGREE; JSON Phase C-green-P14, FailCount 0, FullConsolePassed 62, FullConsoleFailed 0, FullConsoleSkipped 0, P15NamedTestsPresent false, P16NamedTestsPresent false. Tests/adapter/result LastWriteTimeUtc 2026-08-22T05:15:12Z is after that receipt (05:07:48Z / file 05:08:53Z). Current independent filter is Failed 24 Passed 0.

### B Workspace rules

#### B1. Honesty and receipts. Every done/red claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent unique logs and TRX under docs/receipts/_hv-c-red-p15/ with WMI start 2026-08-22T05:28:03Z, filter TRX 2026-08-22T05:33:04Z. Implementer Failed 24 / Passed 0 / Skipped 0 and the three failure-message classes matched this rerun. Adapter stubs confirmed by file read plus TRX messages, not by implementer narrative.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-green-P14 hostile AGREE exists (docs/receipts/hostile-validator-20260822T050748Z.md) with Passed 62 and P15 absent. P15 test/adapter writes at 05:15:12Z are after that AGREE. Plan order is C-green-P14 AGREE, then P15 red, then C-red-P15. This review does not FAIL B2 from FR createdAt versus file mtimes. P15 green is not in this gate.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get and requirements get/listMappings via Invoke-McpPlugin.ps1. This validator did not read or write TODO.yaml or session-log store files as a substitute.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: Collector, bootstrap, WMI start, tests-run, parse, and count scripts are pwsh.exe -NoProfile -NonInteractive. PythonProcessCount 1 is unrelated gcloud.py. This validator did not invoke python/python3/py.

#### B5. No fabricated results. Claims match artifacts.

Verdict: PASS

Evidence: TRX, console, adapter source, live todo_get, and prior receipt JSON agree with the scored claims. Duration difference versus implementer 5 m 19 s is recorded as residual, not hidden.

### C Requirement violations

#### C1. Identify FR/TR/TEST that apply.

Verdict: PASS

Evidence: Live workflow.requirements.getFr FR-MCP-PLUGININT-001, getTr TR-MCP-PLUGININT-001, getTest TEST-MCP-PLUGININT-001, listMappings two rows FR-TR and FR-TEST. Plan P15 maps remaining TEST AC2 isolation plus failsafe. TR ac-4 text: each row asserts session, turn, action, dialog, completion status, cache path, source revision, and failsafe cleanup from server and filesystem receipts.

#### C2. Structured acceptance criteria exist and are testable for this red slice.

Verdict: PASS

Evidence: TEST-MCP-PLUGININT-001 ac-2: every deterministic row proves bootstrap, begin, append action/dialog, complete, durable query, and workspace cache isolation (isSatisfied false). TR-MCP-PLUGININT-001 ac-4 includes failsafe cleanup (isSatisfied false). Named P15 theories encode success/no-pending, failed-submit retain pending, retry deletes matching pending. AC are not empty.

#### C3. Unit tests cover the claimed P15 AC for a red gate.

Verdict: PASS

Evidence: Three theories times eight hosts currently fail on FailsafePathVerified false or not-implemented submit/retry. That is the red encoding of remaining failsafe isolation. Missing root-id filename and sibling-retain asserts are residual, not missing tests.

#### C4. Not claiming FR/TR/TEST complete. Mappings exist.

Verdict: PASS

Evidence: All PLUGININT AC isSatisfied false, status pending. listMappings returned FR-MCP-PLUGININT-001 to TR-MCP-PLUGININT-001 and TEST-MCP-PLUGININT-001. This review did not mark requirements satisfied.

### D Current plan holistically

#### D1. Scope is C-red-P15 only, not plan closeout.

Verdict: PASS

Evidence: Parent brief and plan section 7: C-red-P15 AGREE, then P15 green, then C-green-P15 AGREE. This review did not require P16-P20, D/E/F/G/H/I, or PLAN/PLUGININT done:true.

#### D2. Plan exit criteria for this slice: named P15 reds exist, currently failing, no Skip, no P16 mix-in.

Verdict: PASS

Evidence: Independent filter Failed 24 Passed 0 Skipped 0. P16 absent. Adapter failsafe verification not implemented.

#### D3. Combined PLAN task and child TODO remain open.

Verdict: PASS

Evidence: PLAN task 16 `C P14 red + hostile then green; P15 red + hostile then green.` done: false. MCP-PLUGININT-001 P15 task done: false. Remaining text says next is C-red-P15 hostile then P15 green.

## Accuracy and completeness

Accuracy: 96. Independent TRX/console/source match the red claims. Deducted for duration mismatch versus implementer 5 m 19 s and homemade HMAC mismatch (plugin verifier true).
Completeness: 96. Surfaces A+B+C+D evaluated. Full PluginIntegration suite was not re-run; C-red-P15 requires the named 24-row filter, not a mixed green-plus-red full suite.
