# Hostile validator receipt

TimestampUtc: 2026-08-19T20:03:34Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
Worktree: F:\GitHub\McpServer\.worktrees\triage-plugin-core
WorkClass: 1 (project implementation H-green of S2 plugin-core leftovers)
ActivePlan: docs/plans/triage-cluster-002.md (S2 / G4-G8 plus 120 FAIL from S1)
GitBranch: triage/plugin-core
GitSha: 0620078259d0be441d953fbaf457b0fdb670dbbc (same as develop; 8 unstaged files)
add-profile: executed yes; profile file count read: 18 (excluded add-profile.grok.md)

SessionLog:
- sessionId: GrokCode-20260819T195224Z-hostile-s2-hgreen
- requestId: req-20260819T195224Z-001-hostile-s2-hgreen
- persistence: sessionlog_query todoId=PLAN-TRIAGELEFTOVER-001 from=2026-08-19T19:50:00Z returned this session with turn in_progress, 2 dialog items, planFile=docs/plans/triage-cluster-002.md (queried 2026-08-19T20:01:55Z). Complete-turn proof appended after this receipt write.

Plugin identity:
- sourceType: GrokCode
- plugin: F:\GitHub\mcpserver-grok-plugin .grok-plugin/plugin.json and .version = 1.95.0
- marker signature: True (docs/receipts/_hv-s2-hgreen/01-trust.json)
- health nonce: hv-s2-20260819T195224Z-65480 echoed; storage=reachable; version 1.4.28+f4060f037e62e64974026aff9d24e11b2f481952
- tools/search keyword=mcpserver-grok-plugin exact name count=1

OverallVerdict: DISAGREE

Counts: PASS 14, FAIL 7, UNKNOWN 0, N/A 0

## Claims reviewed

### A Requested validation

A1. Worktree files changed: plugins/core/lib-ps/McpPluginShim.psm1, repl-invoke.ps1, plugin-hook.ps1, Invoke-McpPlugin.ps1, resolve-cache-dir.ps1, hooks-templates/wrapper.ps1.template, Pester TriagePluginIdentity.Tests.ps1 and PluginPowerShellRuntime.Tests.ps1.
Verdict: PASS
Evidence: git status dirty 8 files, exact claimed list, unstaged. Diff vs origin/develop on each file. docs/receipts/_hv-s2-hgreen/01-trust.json and 02-files.json. Plan also named code-verify.ps1 and cache-manager.ps1; code-verify.ps1 is not under plugins/core/lib-ps (disk_full lives in plugin-hook.ps1 Invoke-CodeVerify WriteAllText catch). cache-manager.ps1 LastWriteTimeUtc 2026-08-19T19:16:33Z with empty vs-develop stat in this collector.

A2. 158: updateTurn omitted/empty/scalar tags/contextList under StrictMode; exit 0; no Count cannot be found; success stdout silent.
Verdict: PASS
Evidence: Pester S2-only Passed the STRICTCOUNT Its. Child-process It 'workflow.sessionlog.updateTurn omitted empty and scalar tags exit 0 with silent stdout' ExitCode 0, Stdout empty, no Count cannot be found. Production ConvertTo-McpPluginStringList in McpPluginShim.psm1. docs/receipts/_hv-s2-hgreen/03-pester-s2-only.json.

A3. 159: Test-ReplFailsafeBackendUnreachable treats backend_unavailable and HTTP 503 as unreachable. Drain aborts without incrementing drainAttempts or quarantining. Later in-process drain can replay.
Verdict: PASS
Evidence: repl-invoke.ps1 Test-ReplFailsafeBackendUnreachable markers include backend_unavailable, HTTP 503, http 503. Invoke-ReplFailsafeDrainOnFirstSuccess does not latch completed when summary.aborted. Pester TEST-MCP-FAILSAFE-001 both Its passed.

A4. 140: SessionEnd unresolved cache exit 0 and {}. Identifiable workspace still flushes.
Verdict: FAIL
Evidence: Unresolved path PASS: It 'session-end without a resolvable workspace exits 0 and writes {}' passed. Flush path is not proven. It 'session-end flushes the cache identified by CLAUDE_PROJECT_DIR' only asserts ExitCode 0, stdout {}, and Test-Path cacheDir. It does not assert pending YAML removed or flushed= count. Invoke-CacheFlushHook swallows all session-end errors and still writes {}, so a failed flush is indistinguishable from a successful flush in that test.

A5. 142/106: cross-sourceType CompleteTurn refused; same-agent rotation rebinds; different requestId refused; empty title omitted not fail.
Verdict: PASS
Evidence: Pester TEST-MCP-XAGENT-001 prefix incompatibility, CompleteTurn refuse GrokCode-on-Codex, same-agent rebind submit count 1, refuse other requestId with zero submits, empty title omits queryTitle. All passed in S2-only run. Production Assert-ReplCurrentTurnFresh sourceType prefix refuse at repl-invoke.ps1:1477-1481.

A6. 125/130: typed disk_full; wrapper timeout honored.
Verdict: PASS
Evidence: Invoke-CodeVerify WriteAllText catch maps disk-full IOException to status failed code disk_full. TEST-MCP-VERIFYWRAP-001 types disk_full and Invoke-PluginBoundedProcess kills a 20s sleep in under 8s. wrapper.ps1.template WaitForExit plus MCP_CODE_VERIFY_TIMEOUT_SECONDS. Those unit tests passed. Current-turn lastBuildStatus preservation on disk_full is not asserted (scored under C4).

A7. 120: persist timeout returns degraded/queued inside REPL_TIMEOUT; Invoke-McpPlugin emits classified command_timeout retryable: true instead of unclassified throw. Attack whether a real SubmitAsync timeout is tested, not only injected Persisted=false.
Verdict: FAIL
Evidence:
- Wrapper classification PASS on worktree-core copy: TimeoutSeconds 1 beginTurn exit 0 stdout code command_timeout retryable true details.degraded true queued true. docs/receipts/_hv-s2-hgreen/17-live-120-force-timeout.json. Elapsed 1.918s. That is a wrapper kill, not client.SessionLog.SubmitAsync returning timeout.
- Happy-path worktree-core beginTurn with TimeoutSeconds 12 completed exit 0 in 3.097s. docs/receipts/_hv-s2-hgreen/16-live-120-core.json. No persist timeout occurred.
- Pester BeginTurn.SubmitAsyncTimeout_ReturnsDegradedQueued still stubs Invoke-ReplRaw to Start-Sleep 20 a pwsh child. It does not call SubmitAsync. BeginTurn.SubmitTimeoutAfterFailsafe still injects Persisted=false. Invoke-McpPlugin timeout It is a source-string match, not a live SubmitAsync hang.
- Installed grok plugin at F:\GitHub\mcpserver-grok-plugin is 1.95.0 and is not this worktree copy. Plan says SyncAgentPlugins after merge. S1 live unclassified 40s path was not re-run against the unsynced plugin in this review.

A8. Parent independently re-ran named Pester: Passed 44 Failed 0 Skipped 0. YOU re-run in the worktree. Failed 0 Skipped 0 required.
Verdict: FAIL
Evidence:
- Two claimed files, no filter: Passed 121 Failed 8 Skipped 0 Total 129. docs/receipts/_hv-s2-hgreen/03-pester-full.json. Failures are missing plugins/core/.staged-plugin (gitignored; main repo has 43 staged files, worktree has 0).
- S2-name filter: Passed 15 Failed 0 Skipped 0, NotRun 114. Not 44.
- Broader CompleteTurn/FAILSAFE filter: Passed 30 Failed 1 Skipped 0. Failure: TEST-MCP-BUGTRIAGE-029 completeTurn refreshes marker-only drift because $script:LastReplPersistenceDetails is unset under StrictMode (repl-invoke.ps1:1929). That failure did not reproduce in the full 129 run (order/isolation).
- 44/0/0 was not reproduced.

A9. BUG-TRIAGE-158,159,140,142,106,125,130,120 and PLAN-TRIAGELEFTOVER-001 remain Done=false.
Verdict: PASS
Evidence: MCP todo_get each id Done=false. PLAN-TRIAGELEFTOVER-001 Done=false.

### B Workspace rules

B1. Byrd v4 phase-order scored at this H-green gate, not FR-vs-file timestamps.
Verdict: PASS
Evidence: No createdAt vs LastWriteTime FAIL. This is the claimed implementation H-green.

B2. Inter-phase hostile AGREE required before claiming S2 implementation complete.
Verdict: FAIL
Evidence: S0 leftover AGREE exists (hostile-validator-20260819T183208Z.md). S1 closeout is DISAGREE (hostile-validator-20260819T193921Z.md and 20260819T185900Z.md). No S2 red-test hostile AGREE receipt was found before this H-green. Claiming S2 implementation complete without that gate is a process FAIL.

B3. MCP-only TODO/session/requirements storage.
Verdict: PASS
Evidence: todo_get, sessionlog_*, requirements_list only. No direct edit of todo.yaml or session-log files.

B4. PowerShell only; no Python.
Verdict: PASS
Evidence: pwsh.exe collectors under docs/receipts/_hv-s2-hgreen.

B5. Honesty: 44 Pester claim matches artifacts.
Verdict: FAIL
Evidence: Independent re-run of the two named files is not 44/0/0. Full files 121/8/0. S2-only 15/0/0.

### C Requirements

C1. Applicable leftover FR/TR/TEST identified from MCP.
Verdict: PASS
Evidence: requirements_list extracted FR-MCP-STRICTCOUNT-001, FAILSAFE-001, SESSIONEND-001, XAGENT-001, VERIFYWRAP-001, TRIAGEPLUGIN-001. TESTs matching those IDs exist. TRs exist except TR-MCP-TRIAGEPLUGIN-004 (missing in TR list; 120 maps through FR-MCP-TRIAGEPLUGIN-001 to TEST-MCP-TRIAGEPLUGIN-004).

C2. Structured AC exist.
Verdict: PASS
Evidence: Each leftover FR has 1-3 AC texts. Mapping rows FR->TR->TEST exist for STRICTCOUNT, FAILSAFE, SESSIONEND, XAGENT, VERIFYWRAP, TRIAGEPLUGIN. docs/receipts/_hv-s2-hgreen/10-extract-reqs.json, 11-extract-tests.json, 12-extract-tr.json, 13-extract-map.json.

C3. AC are testable.
Verdict: PASS
Evidence: Count, 503 drain, SessionEnd {}, cross-agent refuse, disk_full, wrapper timeout, persist timeout are named and testable.

C4. Tests cover each AC (suite green is not coverage).
Verdict: FAIL
Evidence:
- TEST-MCP-TRIAGEPLUGIN-004 / FR-MCP-TRIAGEPLUGIN-001 persist-timeout AC still lacks a real SubmitAsync timeout. Sleeping pwsh stub plus wrapper kill is not SubmitAsync.
- FR-MCP-SESSIONEND-001 AC "identifiable workspace still flushes" is not asserted (pending file / flushed count).
- FR-MCP-VERIFYWRAP-001 AC "current-turn remains valid" on disk_full is not asserted.
- All leftover FR AC isSatisfied=false (expected until closeout; not used as the FAIL by itself).

C5. Leftover FR/TR/TEST IDs were created (S0) and mapped.
Verdict: PASS
Evidence: MCP mappings present for the S2 leftover FRs. TR-MCP-TRIAGEPLUGIN-004 missing is covered by TEST-MCP-TRIAGEPLUGIN-004 under FR-MCP-TRIAGEPLUGIN-001.

### D Plan holistically

D1. S2 plugin-core leftovers G4-G8 plus 120 original AC on this worktree, ready to merge after AGREE.
Verdict: FAIL
Evidence: Plan S2 named tests Failed 0 Skipped 0. S2-only 15 tests are green. The two claimed Pester files are not green in this worktree (8 fails, missing .staged-plugin). 44/0/0 not reproduced. 120 original AC (beginTurn Submit over ~30s must not hang; classified retryable/degraded/queued) is only proven as a 1s wrapper kill plus a stubbed child sleep, not SubmitAsync. SessionEnd flush AC is not proven. Unstaged worktree on develop SHA. Do not merge. Do not mark BUG-TRIAGE-* or PLAN-TRIAGELEFTOVER-001 done.

D2. TODOs remain Done=false as claimed.
Verdict: PASS
Evidence: A9.

## Explicit FAIL list

1. A4: SessionEnd flush AC is not proven. The CLAUDE_PROJECT_DIR test never checks that pending records flushed. Hook catch still emits {} on flush failure.
2. A7: No real client.SessionLog.SubmitAsync timeout test. Persisted=false injection remains. New It stubs Invoke-ReplRaw with Start-Sleep. Live worktree-core beginTurn completed in 3.097s. Forced TimeoutSeconds 1 classified command_timeout on the wrapper, which is not SubmitAsync.
3. A8: Named Pester 44 Failed 0 Skipped 0 not reproduced. Two-file run Passed 121 Failed 8 Skipped 0. S2-only Passed 15 Failed 0 Skipped 0.
4. B2: No S2 red-phase hostile AGREE before this claimed implementation H-green.
5. B5: The 44/0/0 count does not match independent artifacts.
6. C4: Leftover AC coverage still red for 120 SubmitAsync persist-timeout, 140 flush, and 125 current-turn-on-disk-full.
7. D1: S2 is not merge-ready. Do not mark TODOs done. Do not merge.

## Mandatory surfaces not evaluated

None. UNKNOWN count is 0.

## Trust bootstrap

- Marker signature Test-MarkerSignature True (docs/receipts/_hv-s2-hgreen/01-trust.json).
- Health nonce hv-s2-20260819T195224Z-65480 echoed. storage=reachable. version 1.4.28+f4060f037e62e64974026aff9d24e11b2f481952.

Do not mark any MCP TODO done. Do not merge.
