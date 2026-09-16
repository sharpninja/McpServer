# Hostile Validator Receipt

TimestampUtc: 2026-08-22T06:41:47Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-green-P15 ONLY, PLUGININT failsafe pending isolation green gate). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not require P16-P20. Do not require D4/D5. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 must remain done:false. Combined PLAN task "C P14 red + hostile then green; P15 red + hostile then green." stays open.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.100.0; .version 1.100.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin. Plugin Test-MarkerSignature true via plugins/core/lib-ps/marker-resolver.ps1. Health nonce nonce-hv-cgreen-p15-20260822062813-61400 echoed exactly.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P15: C-red-P15 AGREE, then P15 green, then C-green-P15 AGREE. Named theories Theory_{Agent}_Success_NoPendingFailsafe; Theory_{Agent}_FailedSubmit_RetainsRootIdPending; Theory_{Agent}_RetrySuccess_DeletesOnlyMatchingPending. Maps remaining TEST-MCP-PLUGININT-001 AC2 isolation plus failsafe.
Requirement IDs: FR-MCP-PLUGININT-001; TR-MCP-PLUGININT-001 AC failsafe cleanup; TEST-MCP-PLUGININT-001 AC2 failsafe half.
Prior C-red-P15 AGREE (parent named): docs/receipts/hostile-validator-20260822T053539Z.md (OverallVerdict AGREE, independent filter Failed 24 Passed 0 Skipped 0). Duplicate C-red receipt docs/receipts/hostile-validator-20260822T060633Z.md is DISAGREE because a later filter Passed 24; that DISAGREE is not this C-green-P15 AGREE.
Collector: docs/receipts/_hv-c-green-p15/run2/
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass independently: executed add-profile (18 files); verified marker signature and health nonce; opened dedicated session GrokSubagentHostile-20260822T062812Z-pluginhandoff-p15green; live-got PLAN-PLUGINHANDOFF-001, MCP-PLUGININT-001, FR/TR/TEST/listMappings; listed PluginIntegration tests (P15 Success 8, FailedSubmit 8, Retry 8, AiTheory 0, P16 0); grepped Skip/AiTheory_/P16 in *.cs (Skip attributes 0, AiTheory_ 0, P16 0); re-read adapter/tests/result/fixture/plan; independently re-ran the three-name P15 filter in unique ResultsDirectory docs/receipts/_hv-c-green-p15/run2/results-20260822T063532Z.

Independent filter evidence (this validator, not leftover or implementer TRX):

- Command: `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter "FullyQualifiedName~Theory_Agent_Success_NoPendingFailsafe|FullyQualifiedName~Theory_Agent_FailedSubmit_RetainsRootIdPending|FullyQualifiedName~Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending" --logger trx --logger console --results-directory F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2\results-20260822T063532Z`
- WMI PID 52344. ExitCode 0. DurationMs 302914. Console: Passed! Failed: 0, Passed: 24, Skipped: 0, Total: 24, Duration: 4 m 45 s.
- TRX: docs/receipts/_hv-c-green-p15/run2/results-20260822T063532Z/kingd_PAYTON-LEGION2_2026-08-22_01_36_06_net10.0.trx LastWriteTimeUtc 2026-08-22T06:40:34.8335533Z. outcome Completed, executed 24, passed 24, failed 0, notExecuted 0, unitCount 24, p16Count 0, otherCount 0. Host matrix 8/8/8. skippedNames empty. Console Skipped: 0.
- Independent --list-tests ExitCode 0 DurationMs 17093. listP15Success 8 listP15FailedSubmit 8 listP15Retry 8 listAiTheory 0 listP16 0.

Leftover 062054Z collector TRX under docs/receipts/_hv-c-green-p15/trx-p15-hv/p15-filter.trx also Passed 24 Failed 0 (LastWrite 2026-08-22T06:27:30Z) then started an out-of-scope full PluginIntegration suite. After inspect, leftover tree PIDs 86108,82396,70176,88088,82400,19328 were killed so this unique ResultsDirectory run could proceed. Leftover TRX is corroboration only, not this review's independent proof.

This review wrote no todo_update and did not implement product features.

## Clock and live MCP

Health GET /health?nonce=nonce-hv-cgreen-p15-20260822062813-61400 returned status Healthy, storage reachable, version 1.4.30+ee89cd63f6d16aa43d8e8dfac2388246c6ba39f8, nonce echoed exactly. Core Test-MarkerSignature true. Invoke-FullBootstrap true. Tool registry exact name mcpserver-grok-plugin present.

## Session log proof

Dedicated persistence is through plugin `client.SessionLog.*` as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T062812Z-pluginhandoff-p15green. Turn requestId req-20260822T062812Z-001-hostile-c-green-p15. queryTitle Hostile C-green-P15 failsafe pending isolation green gate. OpenSession created=true. BeginTurnAsync succeeded. QueryAsync before complete: totalCount includes this sessionId, turnCount 1, queryTitle as above. Evidence: docs/receipts/_hv-c-green-p15/run2/sl-open.txt, sl-begin.txt, sl-query-sid.txt, sl-query-history.txt, sl-dialog-mid.txt.

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

- A5: FailedSubmit still only Assert.NotEmpty pending files; it does not assert a root-id-named pending filename. MCP-PLUGININT-001 P15 task and the theory name RetainsRootIdPending require that. C-red-P15 AGREE 053539Z deferred this tightening to green. Retry now asserts matching file deleted and sibling retained.
- C3: suite-green 24/24 is not AC coverage for root-ID-named pending. TEST-MCP-PLUGININT-001 AC2 isolation plus failsafe remainder is encoded by the named P15 theories; FailedSubmit does not prove the root-id filename.

## Explicit UNKNOWN list

None.

## Residual (not extra FAILs)

- ExecuteCanonicalTurnAsync still persists via fixture.CreateTrustedClient after discarding LaunchAsync (P12 residual). P15 DoD is the named failsafe theories, not production plugin persist. Not a P15 FAIL.
- PluginSessionLogWorkflowResult.cs comment still says C-red-P15 stays false until failsafe assertions are implemented. FailsafePathVerified is assigned true. Stale comment only.
- P15 test XML still says "P15 red". ImplementationTask still labeled [Red]. Parent allowed that; this review did not flip done:true.
- TRX Counters.skipped attribute is missing (null). Console Skipped: 0; TRX notExecuted=0; skippedNames empty.
- Independent list-tests unique FQN line count 88 includes project-to-dll banner noise. Cited P15 evidence is 8+8+8 plus TRX unitCount 24.
- PythonProcessCount 1 is Google Cloud SDK gcloud.py (PID 69564). This validator did not invoke python/python3/py.
- workflow.* result envelopes include deprecated: true. Treated as success metadata.
- FR/TR/TEST createdAt stamps look like query time. AC text is present. isSatisfied remains false.
- Marker plugin_version 1.97.0 drifted. Plugin .version and plugin.json are 1.100.0 (authoritative).
- Plugin Status JSON agent field is GrokCode while session sourceType is GrokSubagentHostile. failsafeDir used GrokSubagentHostile and UTF-8 base64url workspace key RjpcR2l0SHViXE1jcFNlcnZlcg.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/ and many untracked receipts. docs/Project/TODO.yaml and docs/todo.yaml were not in porcelain. This validator did not edit TODO storage.
- Parallel leftover session GrokSubagentHostile-20260822T062054Z-c-green-p15 remains in_progress from a crashed/parallel spawn. This review used a new required session suffix pluginhandoff-p15green.
- AppendActionsAsync is not a SessionLogClient method. Actions are recorded via PatchTurnAsync.

## Claims reviewed

### A Requested

#### A1. C-red-P15 AGREE exists at docs/receipts/hostile-validator-20260822T053539Z.md with OverallVerdict AGREE, independent filter Failed 24 Passed 0 Skipped 0, P15 names present, P16 absent.

Verdict: PASS

Evidence: md OverallVerdict AGREE. JSON OverallVerdict AGREE, FailCount 0, FilterConsoleFailed 24, FilterConsolePassed 0, FilterConsoleSkipped 0, FilterConsoleTotal 24, P15NamedTestsPresent true, P16NamedTestsPresent false, FailsafePathVerifiedImplemented false. Receipt LastWriteTimeUtc 2026-08-22T05:36:55.8921213Z.

#### A2. Adapter/tests LastWriteTimeUtc 2026-08-22T05:40:18Z is after that C-red AGREE. ExecuteFailedSubmitAsync and RetryFailedSubmitAsync no longer throw not implemented. ExecuteCanonicalTurnAsync sets FailsafePathVerified true. GetFailsafePendingDirectory uses V4 layout {workspace}/.mcpServer/failsafe/{agent}/workspaces/{b64}/pending.

Verdict: PASS

Evidence: Adapter and tests LastWriteTimeUtc 2026-08-22T05:40:18.7559535Z. C-red AGREE timestamp 2026-08-22T05:35:39Z / file 05:36:55Z. File read this review: ExecuteFailedSubmitAsync writes sessionlog-{sanitizedSessionId}-{utc}-pending.yaml and returns FailsafePathVerified true (adapter lines 205-228). RetryFailedSubmitAsync seeds sibling then deletes matching files (lines 238-265). ExecuteCanonicalTurnAsync assigns FailsafePathVerified = true (line 163). GetFailsafePendingDirectory combines .mcpServer/failsafe/{agent}/workspaces/{key}/pending (lines 274-281). Core Get-McpFailsafeDir documents the same V4 tree.

#### A3. The three named P15 theories still exist with eight InlineData PluginHostKind rows each. No [Skip]. P16/AiTheory_ names absent.

Verdict: PASS

Evidence: PluginSessionLogWorkflowAdapterTests.cs methods at lines 203, 234, 263. Each has `[Theory(Timeout = 120000)]` plus eight `[InlineData(PluginHostKind.*)]` rows (Codex, ClaudeCode, ClaudeCowork, Copilot, Grok, Cline, ClineV2, OpenCode). Grep of tests/McpServer.PluginIntegration.Tests *.cs: [Skip] 0, Fact(Skip) 0, Theory(Skip) 0, Assert.Skip 0, AiTheory_ 0, P16 0. Independent --list-tests P15 8+8+8, AiTheory 0, P16 0.

#### A4. Independent P15 filter is currently GREEN: Failed 0 Passed 24 Skipped 0. FAIL if any P15 row fails or is skipped. FAIL if P16 rows are mixed in.

Verdict: PASS

Evidence: Independent unique ResultsDirectory run2/results-20260822T063532Z. Console Passed! Failed 0 Passed 24 Skipped 0 Total 24 Duration 4 m 45 s ExitCode 0. TRX executed 24 passed 24 failed 0 notExecuted 0 p16Count 0 otherCount 0. Host matrix every host 1/1/1. leftover 062054Z TRX is corroboration only.

#### A5. After success, no matching pending file remains. Failed submit retains a pending file under the V4 path. Retry deletes the matching pending file. Attack FailedSubmit root-id filename assert and Retry only-matching delete (C-red residual). Score FAIL if P15 DoD/TEST AC2 requires them.

Verdict: FAIL

Evidence: Success test asserts FailsafePathVerified and empty pending if the directory exists (tests 213-218). Adapter success path throws if sessionlog-{sessionId}-* leftovers exist. FailedSubmit XML and TODO P15 require a root-ID-named pending file, but the body only Assert.True(Directory.Exists) plus Assert.NotEmpty files (tests 244-247). Adapter does write a sessionlog-{sanitizedSessionId}-*-pending.yaml, but a wrongly named pending file would still pass. Retry now asserts matching SessionId filename gone and a sibling retained (tests 273-278); that C-red residual is closed. C-red AGREE 053539Z recorded the missing root-id filename assert as a later/green concern. MCP-PLUGININT-001 P15 task text: "failed submit retains a root-ID-named pending file". That is this slice DoD. Missing assert is FAIL for C-green-P15.

#### A6. Fixture/harness does not use the developer 7147 database.

Verdict: PASS

Evidence: PluginIntegrationServerFixture.cs summary: Never the developer 7147 database. ReservedServicePort = 7147. AllocateFreePort skips 7147. DatabasePath = Path.Combine(temp mcp-pluginint-{guid}/data, mcp.db). Workspace under Path.GetTempPath(). P11 theory Assert.NotEqual(7147, fixture.Port).

#### A7. PLAN-PLUGINHANDOFF-001 done=false. MCP-PLUGININT-001 done=false. Combined C P14/P15 PLAN task done=false. P15 implementationTask may still be labeled [Red]. This review writes no done:true.

Verdict: PASS

Evidence: Live workflow.todo.get / client.Todo.GetAsync at 2026-08-22T06:28:31Z to 06:28:36Z. PLAN top done: false. Combined task `C P14 red + hostile then green; P15 red + hostile then green.` done: false. MCP-PLUGININT-001 top done: false. P15 [Red] Add failsafe assertions done: false. This review did not call todo_update. git porcelain had no docs/Project/TODO.yaml or docs/todo.yaml.

#### A8. P15 maps remaining TEST-MCP-PLUGININT-001 AC2 isolation plus failsafe. isSatisfied remains false. Do not require AC3/AC5 suite-green.

Verdict: PASS

Evidence: Plan section 7 P15 maps remaining AC2 isolation plus failsafe. Live getTest AC2 isSatisfied false; AC1-AC5 all false; status pending. TR ac-4 failsafe cleanup isSatisfied false. AC3/AC5 suite-green was not required. Coverage hole for root-id filename is scored on A5/C3, not as a demand for AC3/AC5.

#### A9. This AGREE, if earned, authorizes C-red-P16 to start. It does not close PLAN or MCP-PLUGININT-001. It does not authorize P16 green. Duplicate 060633Z DISAGREE is not this gate.

Verdict: PASS

Evidence: 060633Z JSON OverallVerdict DISAGREE, Phase C-red-P15, Run3ConsolePassed 24. This receipt OverallVerdict DISAGREE, so C-red-P16 is not authorized. PLAN and MCP-PLUGININT-001 remain done false. This review is C-green-P15, not plan closeout.

#### A10. Persist path still uses fixture.CreateTrustedClient after LaunchAsync (residual vs P12). Do not FAIL P15 solely for that unless P15 DoD requires production plugin persist.

Verdict: PASS

Evidence: Adapter LaunchAsync result discarded (lines 71-77) then fixture.CreateTrustedClient() (line 85). P15 DoD is the named failsafe theories. Residual vs P12, not a P15 FAIL.

### B Workspace rules

#### B1. Honesty and receipts. Every done/green claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent unique ResultsDirectory TRX/console under docs/receipts/_hv-c-green-p15/run2/. Leftover 062054Z TRX was not treated as proof. Adapter/source/live todo_get re-read this review.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-red-P15 hostile AGREE exists (docs/receipts/hostile-validator-20260822T053539Z.md) with Failed 24 Passed 0. Adapter/tests writes at 05:40:18Z are after that AGREE. Plan order is C-red-P15 AGREE, then P15 green, then C-green-P15. This review does not FAIL B2 from FR createdAt versus file mtimes. P16 is not in this gate.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get and requirements get/listMappings via Invoke-McpPlugin.ps1. This validator did not read or write TODO.yaml or session-log store files as a substitute.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: Collector, bootstrap, WMI start, tests-run, parse, and count scripts are pwsh.exe -NoProfile -NonInteractive. PythonProcessCount 1 is unrelated gcloud.py PID 69564. This validator did not invoke python/python3/py.

#### B5. No fabricated results. Claims match artifacts.

Verdict: PASS

Evidence: Independent TRX/console, adapter source, live todo_get, and prior receipt JSON agree with the scored claims. Leftover kill and hung waiter are disclosed. Missing root-id filename assert is not hidden.

### C Requirement violations

#### C1. Identify FR/TR/TEST that apply.

Verdict: PASS

Evidence: Live workflow.requirements.getFr FR-MCP-PLUGININT-001, getTr TR-MCP-PLUGININT-001, getTest TEST-MCP-PLUGININT-001, listMappings two rows FR-TR and FR-TEST. Plan P15 maps remaining TEST AC2 isolation plus failsafe. TR ac-4 text includes failsafe cleanup from server and filesystem receipts.

#### C2. Structured acceptance criteria exist and are testable for this green slice.

Verdict: PASS

Evidence: TEST-MCP-PLUGININT-001 ac-2: every deterministic row proves bootstrap, begin, append action/dialog, complete, durable query, and workspace cache isolation (isSatisfied false). TR-MCP-PLUGININT-001 ac-4 includes failsafe cleanup (isSatisfied false). Named P15 theories encode success/no-pending, failed-submit retain pending, retry deletes matching pending. AC are not empty.

#### C3. Unit tests cover the claimed P15 AC for a green gate.

Verdict: FAIL

Evidence: Independent filter Passed 24 is not AC coverage. FailedSubmit does not assert root-id-named pending filename required by MCP-PLUGININT-001 P15 task and theory name RetainsRootIdPending. Retry sibling-retain assert exists. Success empty-pending assert exists. The remaining failsafe AC2 isolation hole is the root-id filename.

#### C4. Not claiming FR/TR/TEST complete. Mappings exist.

Verdict: PASS

Evidence: All PLUGININT AC isSatisfied false, status pending. listMappings returned FR-MCP-PLUGININT-001 to TR-MCP-PLUGININT-001 and TEST-MCP-PLUGININT-001. This review did not mark requirements satisfied.

### D Current plan holistically

#### D1. Scope is C-green-P15 only, not plan closeout.

Verdict: PASS

Evidence: Parent brief and plan section 7: C-red-P15 AGREE, then P15 green, then C-green-P15 AGREE. This review did not require P16-P20, D/E/F/G/H/I, or PLAN/PLUGININT done:true.

#### D2. Plan exit criteria for this slice: named P15 greens currently passing, no Skip, no P16 mix-in.

Verdict: PASS

Evidence: Independent filter Failed 0 Passed 24 Skipped 0. P16 absent. Weak FailedSubmit root-id filename assert is scored on A5/C3, not as plan closeout. This DISAGREE blocks treating the slice as done.

#### D3. Combined PLAN task and child TODO remain open.

Verdict: PASS

Evidence: PLAN task `C P14 red + hostile then green; P15 red + hostile then green.` done: false. MCP-PLUGININT-001 P15 task done: false. This review wrote no done:true.

## Accuracy and completeness

Accuracy: 95. Independent TRX/console/source match the green counts. Deducted for leftover concurrent collector, hung waiter kill, and listFqn 88 versus prior 86 unique FQN noise.
Completeness: 97. Surfaces A+B+C+D evaluated. Full PluginIntegration suite was not re-run; C-green-P15 requires the named 24-row filter. Root-id filename assert gap is explicit FAIL, not buried.
