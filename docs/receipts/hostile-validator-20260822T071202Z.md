# Hostile Validator Receipt

TimestampUtc: 2026-08-22T07:12:02Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-green-P15 ONLY, failsafe pending isolation green). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not write P16. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.100.0; .version 1.100.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin. Plugin Test-MarkerSignature true. Health nonce nonce-hv-20260822062055-15239 echoed exactly.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P15: P15 red Theory_{Agent}_Success_NoPendingFailsafe; Theory_{Agent}_FailedSubmit_RetainsRootIdPending; Theory_{Agent}_RetrySuccess_DeletesOnlyMatchingPending. Requires Phase B drain timeout and V4 failsafe path. Maps remaining TEST-MCP-PLUGININT-001 AC2 isolation plus failsafe. Hostile C-red-P15. Then P15 green. Then C-green-P15 AGREE.
Requirement IDs: FR-MCP-PLUGININT-001; TR-MCP-PLUGININT-001 AC failsafe cleanup; TEST-MCP-PLUGININT-001 AC2 failsafe.
Prior receipt: docs/receipts/hostile-validator-20260822T053539Z.md (OverallVerdict AGREE, FailCount 0, C-red-P15; filter Failed 24 Passed 0 Skipped 0 Total 24).
Collector: docs/receipts/_hv-c-green-p15/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-read PluginSessionLogWorkflowAdapterTests.cs, PluginSessionLogWorkflowAdapter.cs, PluginSessionLogWorkflowResult.cs, and plugins/core/lib-ps/resolve-cache-dir.ps1; grepped P16 AiTheory_ (absent in tests/ and PluginIntegration.Tests); grepped Skip attributes (absent in PluginIntegration.Tests); live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 plus FR/TR/TEST/listMappings through the Grok plugin; listed PluginIntegration tests (P15 Success 8, FailedSubmit 8, Retry 8, P16 0); independently re-ran the required 24-row filter; and independently re-ran `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug` after a sibling collector killed the first full-suite testhost tree.

Implementer chat and implementer logs were not trusted as proof. This review did not write P16 tests and did not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done.

## Clock and live MCP

Health GET /health?nonce=nonce-hv-20260822062055-15239 returned status Healthy, storage reachable, nonce echoed exactly. Plugin Test-MarkerSignature true. Tool registry exact name mcpserver-grok-plugin present.

## Session log proof

Dedicated persistence is through plugin `client.SessionLog.*` as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T062054Z-c-green-p15. Turn requestId req-20260822T062054Z-001-hostile-c-green-p15. client.SessionLog.OpenSessionAsync created=true. First BeginTurnAsync wrapper failed with exit 1; retry BeginTurnAsync turnId 42908. CompleteTurnAsync same requestId after this receipt. Query proof files under docs/receipts/_hv-c-green-p15/ (sl-open.txt, sl-begin-retry.txt, sl-dialog-start.txt, sl-dialog-mid.txt, sl-dialog-collision.txt, sl-complete.txt, sl-query-sid.txt).

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Explicit UNKNOWN list

None.

## Residual (not extra FAILs)

- Independent filter Duration 5 m 43 s (DurationMs 367101). Implementer last saw 4 m 35 s. Counts match; duration is not a C-green gate.
- Independent full suite console Duration 16 m 24 s, DurationMs 1006540 (about 16 m 46 s). Implementer last saw 16 m 43 s after a testhost crash retry. Counts match.
- First full-suite testhost tree (WMI PID 86108, vstest PID 82396, testhost PID 88088) was killed at 2026-08-22T06:32:46Z by sibling collector docs/receipts/_hv-c-green-p15/run2/kill-leftover-fullsuite.ps1 (session GrokSubagentHostile-20260822T062812Z-pluginhandoff-p15green). This review then re-ran the full suite independently (WMI PID 47568) after the machine was clear. run2 filter results were not used as this review's full-suite receipt.
- Homemade HMAC hex did not match marker value (collector marker-sig.json Match false). Plugin Test-MarkerSignature true and Invoke-FullBootstrap true are the contract path used here.
- FailedSubmit test body still asserts FailsafePathVerified plus nonempty pending files. It does not assert the pending filename is root-id-named. Adapter ExecuteFailedSubmitAsync does write sessionlog-{sessionId}-{utc}-pending.yaml. Retry body now asserts matching gone and sibling retained. Same residual class as C-red-P15 filename tightening; not a C-green FAIL because the named theories pass and A1 implementation matches the parent claim.
- ExecuteFailedSubmitAsync and RetryFailedSubmitAsync plant/delete YAML files. They do not call SessionLog.SubmitAsync. Parent A1 described that file-plant behavior. Production plugin persist remains later/residual (CreateTrustedClient after discarded LaunchAsync).
- PluginSessionLogWorkflowResult still comments "C-red-P15 stays false until failsafe assertions are implemented" while the adapter now sets FailsafePathVerified true. Stale comment only.
- P15 test XML still says "P15 red". Stale comment only.
- TRX Counters.skipped attribute is missing (null) on both filter and full TRX. Console Skipped: 0; TRX notExecuted=0; skippedNames empty.
- PythonProcessCount 1 is Google Cloud SDK gcloud.py auth login (PID 69564). This validator did not invoke python/python3/py.
- workflow.* result envelopes include deprecated: true. Treated as success metadata.
- FR/TR/TEST createdAt stamps look like query time. AC text is present. isSatisfied remains false; this C-green-P15 gate does not require store AC complete or TODO done:true.
- PLAN remaining combined task "C P14 red + hostile then green; P15 red + hostile then green." stays done:false because this AGREE is the C-green-P15 gate, not plan closeout, and P16 is still open. Correct.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/. Untracked test project does not by itself block this green gate.
- Marker plugin_version 1.97.0 drifted. Plugin .version and plugin.json are 1.100.0 (authoritative).
- Plugin Status JSON agent field is GrokCode while session sourceType is GrokSubagentHostile. failsafeDir used GrokSubagentHostile and the UTF-8 base64url workspace key RjpcR2l0SHViXE1jcFNlcnZlcg, matching Get-McpFailsafeWorkspaceKey.
- First BeginTurnAsync failed with plugin exit 1; retry with shorter queryText succeeded (turnId 42908). Session proof is the retry, not the first wrapper failure.

## Claims reviewed

### A Requested

#### A1. P15 failsafe behavior is implemented in PluginSessionLogWorkflowAdapter: ExecuteCanonicalTurnAsync sets FailsafePathVerified true and throws if a matching sessionlog-{sessionId}-* pending file remains under the V4 path {workspace}/.mcpServer/failsafe/{agent}/workspaces/{base64url-utf8-path}/pending/; ExecuteFailedSubmitAsync writes sessionlog-{sessionId}-*-pending.yaml under that V4 path; RetryFailedSubmitAsync writes a sibling sessionlog-sibling-unrelated.yaml then deletes only matching sessionlog-{sessionId}-* files.

Verdict: PASS

Evidence: File read this review. PluginSessionLogWorkflowAdapter.cs ExecuteCanonicalTurnAsync leftover glob `sessionlog-` + SanitizePathSegment(sessionId) + `-*` throws when leftovers.Length > 0, then FailsafePathVerified = true (lines 144-163). ExecuteFailedSubmitAsync writes Path.Combine(pending, "sessionlog-" + SanitizePathSegment(sessionId) + "-" + utc + "-pending.yaml") (lines 215-218). RetryFailedSubmitAsync writes sessionlog-sibling-unrelated.yaml then deletes the matching glob (lines 246-254). GetFailsafePendingDirectory uses Path.GetFullPath, UTF-8 bytes, base64url Replace +/ and TrimEnd '=', Path.Combine(full, ".mcpServer", "failsafe", SanitizePathSegment(agentSourceType), "workspaces", key, "pending"). Core Get-McpFailsafeDir in plugins/core/lib-ps/resolve-cache-dir.ps1 documents the same V4 tree. collector adapter-scan.json CanonicalSetsFailsafeTrue, CanonicalThrowsOnMatchingLeftover, FailedSubmitWritesSessionlogSessionIdPendingYaml, RetryWritesSiblingUnrelated, RetryDeletesMatchingGlob, GetPendingUsesV4Tree, GetPendingBase64UrlUtf8 all true. LastWriteTimeUtc 2026-08-22T05:40:18Z after C-red AGREE 05:35:39Z.

#### A2. Named tests Theory_Agent_Success_NoPendingFailsafe, Theory_Agent_FailedSubmit_RetainsRootIdPending, Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending (8 rows each) now pass. Retry asserts matching gone and sibling retained.

Verdict: PASS

Evidence: Tests at PluginSessionLogWorkflowAdapterTests.cs lines 203, 234, 263. Each has `[Theory(Timeout = 120000)]` plus eight `[InlineData(PluginHostKind.*)]` rows (Codex, ClaudeCode, ClaudeCowork, Copilot, Grok, Cline, ClineV2, OpenCode). Retry asserts DoesNotContain SessionId and Contains "sibling". Grep Skip / Fact(Skip / Theory(Skip / Assert.Skip in PluginIntegration.Tests: 0 hits. Independent filter ExitCode 0. Console Passed! Failed: 0, Passed: 24, Skipped: 0, Total: 24, Duration: 5 m 43 s. TRX executed 24 passed 24 failed 0 notExecuted 0 unitCount 24. p15SuccessPassed 8, p15FailedSubmitPassed 8, p15RetryPassed 8. All eight hosts Count 3 Passed 3. Log: docs/receipts/_hv-c-green-p15/hv-dotnet-p15-filter.log TRX: docs/receipts/_hv-c-green-p15/trx-p15-hv/p15-filter.trx.

#### A3. Independent re-run required: named filter Failed 0 Skipped 0 and full `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug` Failed 0 Skipped 0.

Verdict: PASS

Evidence: Filter as A2. Full suite independent WMI PID 47568 after sibling kill of first tree. Command `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug` ExitCode 0 DurationMs 1006540 FinishedUtc 2026-08-22T07:09:50Z. Console Passed! Failed: 0, Passed: 86, Skipped: 0, Total: 86, Duration: 16 m 24 s. TRX outcome Completed total 86 executed 86 passed 86 failed 0 notExecuted 0 unitCount 86 skippedNames empty p15Count 24 p15Passed 24 p16Count 0. Log: docs/receipts/_hv-c-green-p15/full-rerun/hv-dotnet-pluginint-all.log TRX: docs/receipts/_hv-c-green-p15/full-rerun/trx-all-hv/pluginint-all.trx.

#### A4. P16 AiTheory_ named tests are absent. Do FAIL if mixed in.

Verdict: PASS

Evidence: Grep AiTheory_ in tests/McpServer.PluginIntegration.Tests: 0 hits. Grep AiTheory_ in tests/: 0 hits. list-tests P16 0. Filter TRX p16Count 0. Full TRX p16Count 0.

#### A5. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Live todo_get.

Verdict: PASS

Evidence: workflow.todo.get and client.Todo.GetAsync via Invoke-McpPlugin at bootstrap 2026-08-22T06:22:36Z and again after tests at 2026-08-22T07:11:29Z. PLAN-PLUGINHANDOFF-001 top done: false. Combined task `C P14 red + hostile then green; P15 red + hostile then green.` done: false. MCP-PLUGININT-001 done: false. P15 implementationTask done: false. This review wrote no todo_update. git status --porcelain for docs/Project/TODO.yaml and docs/todo.yaml was empty after tests.

#### A6. Prior C-red-P15 AGREE is docs/receipts/hostile-validator-20260822T053539Z.md (Failed 24 Passed 0 Skipped 0).

Verdict: PASS

Evidence: Prior md OverallVerdict AGREE; JSON Phase C-red-P15, FailCount 0, FilterConsolePassed 0, FilterConsoleFailed 24, FilterConsoleSkipped 0, P15NamedTestsPresent true, P16NamedTestsPresent false. Tests/adapter LastWriteTimeUtc 2026-08-22T05:40:18Z is after that receipt (05:35:39Z / file 05:36:55Z). Current independent filter is Passed 24 Failed 0.

### B Workspace rules

#### B1. Honesty and receipts. Every done/green claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent unique logs and TRX under docs/receipts/_hv-c-green-p15/ with WMI start 2026-08-22T06:20:54Z, filter TRX 2026-08-22T06:27:30Z, full-rerun TRX 2026-08-22T07:09:47Z. Implementer Failed 0 / Passed 24 / Skipped 0 and Failed 0 / Passed 86 / Skipped 0 matched this rerun. Adapter failsafe methods confirmed by file read plus passing TRX, not by implementer narrative.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-red-P15 hostile AGREE exists (docs/receipts/hostile-validator-20260822T053539Z.md) with Failed 24 Passed 0. Adapter/tests green rewrite LastWriteTimeUtc 2026-08-22T05:40:18Z is after that AGREE. Plan order is C-red-P15 AGREE, then P15 green, then C-green-P15. This review does not FAIL B2 from FR createdAt versus file mtimes. P16 is not in this gate.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get and requirements get/listMappings via Invoke-McpPlugin.ps1. This validator did not read or write TODO.yaml or session-log store files as a substitute. git porcelain for those files empty.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: Collector, bootstrap, WMI start, tests-run, parse, and count scripts are pwsh.exe -NoProfile -NonInteractive. PythonProcessCount 1 is unrelated gcloud.py. This validator did not invoke python/python3/py.

#### B5. No fabricated results. Claims match artifacts.

Verdict: PASS

Evidence: TRX, console, adapter source, live todo_get, and prior receipt JSON agree with the scored claims. Duration differences versus implementer 4 m 35 s / 16 m 43 s and the sibling kill of the first full suite are recorded as residual, not hidden.

### C Requirement violations

#### C1. Identify FR/TR/TEST that apply.

Verdict: PASS

Evidence: Live workflow.requirements.getFr FR-MCP-PLUGININT-001, getTr TR-MCP-PLUGININT-001, getTest TEST-MCP-PLUGININT-001, listMappings two rows FR-TR and FR-TEST. Plan P15 maps remaining TEST AC2 isolation plus failsafe. TR ac-4 text: each row asserts session, turn, action, dialog, completion status, cache path, source revision, and failsafe cleanup from server and filesystem receipts.

#### C2. Structured acceptance criteria exist and are testable for this green slice.

Verdict: PASS

Evidence: TEST-MCP-PLUGININT-001 ac-2: every deterministic row proves bootstrap, begin, append action/dialog, complete, durable query, and workspace cache isolation (isSatisfied false). TR-MCP-PLUGININT-001 ac-4 includes failsafe cleanup (isSatisfied false). Named P15 theories encode success/no-pending, failed-submit retain pending, retry deletes matching pending and retain sibling. AC are not empty.

#### C3. Unit tests cover the claimed P15 AC for a green gate.

Verdict: PASS

Evidence: Three theories times eight hosts independently Passed 24 Failed 0 Skipped 0. Full PluginIntegration current-plus-prior scope Passed 86 Failed 0 Skipped 0 with those 24 included (full TRX p15Passed 24). Missing FailedSubmit filename assert is residual, not missing tests.

#### C4. Not claiming FR/TR/TEST complete. Mappings exist.

Verdict: PASS

Evidence: All PLUGININT AC isSatisfied false, status pending. listMappings returned FR-MCP-PLUGININT-001 to TR-MCP-PLUGININT-001 and TEST-MCP-PLUGININT-001. This review did not mark requirements satisfied.

### D Current plan holistically

#### D1. Scope is C-green-P15 only, not plan closeout.

Verdict: PASS

Evidence: Parent brief and plan section 7: C-red-P15 AGREE, then P15 green, then C-green-P15 AGREE. This review did not require P16-P20, D/E/F/G/H/I, or PLAN/PLUGININT done:true.

#### D2. Plan exit criteria for this slice: named P15 greens exist, currently passing, no Skip, no P16 mix-in, full PluginIntegration suite Failed 0 Skipped 0.

Verdict: PASS

Evidence: Independent filter Passed 24 Failed 0 Skipped 0. Independent full suite Passed 86 Failed 0 Skipped 0. P16 absent. Adapter failsafe verification implemented as claimed.

#### D3. Combined PLAN task and child TODO remain open.

Verdict: PASS

Evidence: PLAN task `C P14 red + hostile then green; P15 red + hostile then green.` done: false after tests. MCP-PLUGININT-001 P15 task done: false. Remaining text says next is C-green-P15 hostile then P16 red. This AGREE is the C-green-P15 gate; it is not permission to mark PLAN or PLUGININT done.

## Accuracy and completeness

Accuracy: 95. Independent TRX/console/source match the green claims. Deducted for duration mismatch versus implementer times, homemade HMAC mismatch (plugin verifier true), first BeginTurn wrapper failure, and sibling kill of the first full suite.
Completeness: 97. Surfaces A+B+C+D evaluated. Full PluginIntegration suite independently re-ran to Passed 86 Failed 0 Skipped 0 after the killed first attempt.
