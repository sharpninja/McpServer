# Hostile validation receipt: txnkey session-log green

TimestampUtc: 2026-09-17T14:18:43Z
ValidatorIdentity: GrokSubagentHostile
WorkspacePath: F:\GitHub\McpServer
Gate: txnkey-sessionlog-green
AddProfileExecuted: yes
AddProfileFileCount: 19
AddProfileExcluded: add-profile.grok.md (skill port)
WorkClass: 1
WorkClassDetail: class 1 project implementation (session-log keyserver scope). Operator-approved QuadBrain-only keyserver for session-log. Not a user-directed ops action.
RequestJsonl: F:\GitHub\McpServer\docs\receipts\hv\20260917T141843Z-txnkey-sessionlog-green.request.jsonl
ResponseJsonl: F:\GitHub\McpServer\docs\receipts\hv\20260917T141843Z-txnkey-sessionlog-green.response.jsonl
HvSessionId: GrokSubagentHostile-20260917T141036Z-txnkey-sessionlog-green
HvRequestId: req-20260917T141036Z-001-txnkey-sessionlog-green
HvTurnId: 45201
Agent: GrokSubagentHostile
ActivePlan: operator-approved QuadBrain-only keyserver for session-log (PLAN-TXNKEYSERVER-001)
TodoId: PLAN-TXNKEYSERVER-001

## Classification

Class 1 project implementation. Surfaces A, B, C, and D all apply. Implementer did not claim PLAN-TXNKEYSERVER-001 done and did not claim live redeploy.

## OverallVerdict

DISAGREE

Accuracy: 99
Completeness: 99
PASS: 16
FAIL: 1
UNKNOWN: 0

AGREE is blocked because surface C has a FAIL: FR-MCP-120 still requires session-log writes to route through the coordinator or fail closed while required transactions are active, and the product now bypasses the coordinator for non-QBAgent source types.

## Explicit FAIL list

- C-FR120-conflict: FR-MCP-120 AC text still includes "session-log writes" among paths that "either route through compensation-capable coordinator gates or fail closed while required transactions are active" (isSatisfied=true). Working-tree ExecuteMutationAsync now returns the inner mutation when sourceType is not QBAgent, even with TurnTransactions.Enabled=true and RequiredForMutations=true. FR-MCP-173 is the operator-approved carve-out, but FR-MCP-120 was not amended. Residual before any done-state change.

## Explicit UNKNOWN list

None.

## Claims reviewed

### Surface A: requested validation

A1. FR-MCP-173, TR-MCP-TXNKEY-001, TEST-MCP-221 exist in MCP requirements store and are mapped (FR-MCP-173 -> TR-MCP-TXNKEY-001 + TEST-MCP-221). PLAN-TXNKEYSERVER-001 exists Done=false.
Verdict: PASS
Evidence: MCP requirements_list parsed from store dumps. FR-MCP-173 title "Keyserver signs QuadBrain transactions only". TR-MCP-TXNKEY-001 title "Session-log keyserver gate is QBAgent-only". TEST-MCP-221 condition names TransactionGatedSessionLogServiceTests and red-before-green. Mapping row FrId=FR-MCP-173 TrIds=[TR-MCP-TXNKEY-001] TestIds=[TEST-MCP-221]. todo_get PLAN-TXNKEYSERVER-001 Done=false CompletedDate=null. Structured AcceptanceCriteria arrays on FR-173/TR-TXNKEY-001/TEST-221 are empty; AC text is in Body. That is residual hygiene, not a contradiction of "exist and are mapped".

A2. Red-before-green: four new tests failed before the product change (Submit/Upsert/Open GrokCode reject path and GrokCode degraded path) then passed after. Existing TEST-MCP-161 tests were retargeted to sourceType QBAgent and still pass.
Verdict: PASS
Evidence: HEAD test constant was Agent="Codex". Working tree Agent="QBAgent" plus NonQuadBrainAgent="GrokCode". Implementer session 01a0af86 recap: tests edited 2026-09-17T14:03:17Z; red run 2026-09-17T14:05:59Z Failed 4 Passed 9 Skipped 0 Total 13; named failures SubmitAsync_WhenNonQuadBrain..., UpsertTurnAsync_WhenNonQuadBrain..., OpenSessionAsync_WhenNonQuadBrain..., SubmitAsync_WhenNonQuadBrainAndCoordinatorDegraded...; product ExecuteMutationAsync edit started 2026-09-17T14:06:10Z; green focused 2026-09-17T14:08:07Z Passed 13 Skipped 0. HV re-run after that: focused Passed 13 Failed 0 Skipped 0.

A3. TransactionGatedSessionLogService.ExecuteMutationAsync now bypasses ITurnTransactionCoordinator when sourceType is not QBAgent (case-insensitive). QBAgent still uses the coordinator.
Verdict: PASS
Evidence: git diff HEAD: `if (_coordinator is null)` became `if (_coordinator is null || !IsQuadBrainSessionLogSource(sourceType))`. IsQuadBrainSessionLogSource uses string.Equals(sourceType, "QBAgent", OrdinalIgnoreCase). All mutating ISessionLogService methods except dry-run RepairWorkspaceStampsAsync funnel through ExecuteMutationAsync. TEST-MCP-161 reject-before-mutation still uses default Agent QBAgent and asserts coordinator.Request is set.

A4. Focused filter FullyQualifiedName~TransactionGatedSessionLogServiceTests: Failed 0 Passed 13 Skipped 0.
Verdict: PASS
Evidence: HV `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~TransactionGatedSessionLogServiceTests` EXIT=0. Console: Passed! Failed: 0, Passed: 13, Skipped: 0, Total: 13. TRX outcome Passed=13. Results File: F:\GitHub\McpServer\docs\receipts\_hv-txnkey-sessionlog-green\focused-TransactionGatedSessionLogServiceTests.trx

A5. Broader filter FullyQualifiedName~TransactionGated: Failed 0 Passed 139 Skipped 0.
Verdict: PASS
Evidence: HV `dotnet test` same project `--filter FullyQualifiedName~TransactionGated` EXIT=0. Console: Passed! Failed: 0, Passed: 139, Skipped: 0, Total: 139. TRX outcome Passed=139. Results File: F:\GitHub\McpServer\docs\receipts\_hv-txnkey-sessionlog-green\broader-TransactionGated.trx

A6. Live C:\ProgramData\McpServer\appsettings.yaml TurnTransactions.Enabled was not changed (still true). Repo default still Enabled=false.
Verdict: PASS
Evidence: Live yaml lines: TurnTransactions / Enabled: true / RequiredForMutations: true. LIVE_YAML_LASTWRITE_UTC=2026-09-16T15:21:04.1655807Z (before this slice). Repo src/McpServer.Support.Mcp/appsettings.yaml TurnTransactions.Enabled: false. Git status does not list the repo appsettings.yaml as modified.

A7. No product done-state change yet (PLAN-TXNKEYSERVER-001 remains Done=false). Live service was not redeployed.
Verdict: PASS
Evidence: todo_get Done=false. Live exe LastWriteUtc 2026-09-16T22:28:06.4408896Z ProductVersion 1.4.38+6a72d445dda56f1d288660de044a2b020291630d SHA256 BCEA9C20062651A42788FFCEC5AA0F963AD5E03B80DBC9D5860FC341DBFBFB61. Win32_Process 138544 CreationDate 2026-09-16 5:28:11 PM local (22:28:11Z). LIVE_EXE_HAS_IsQuadBrainSessionLogSource=False. Marker pid still 138544 from 2026-09-16T22:28:32Z.

### Surface B: workspace rules

B1. Byrd v4 tests first shown red, then implement, 0 fail 0 skip for this slice.
Verdict: PASS
Evidence: Recap sequence tests (14:03:17Z) -> red 4/9/0 (14:05:59Z) -> product bypass (14:06:10Z) -> green 13/0/0 then 139/0/0. HV independently re-ran both filters 0 fail 0 skip. Did not FAIL B2 from FR createdAt vs file mtime.

B2. Always bring the receipts / honesty.
Verdict: PASS
Evidence: Implementer A-claims matched artifacts HV re-read or re-ran. HEAD Agent was Codex (retarget to QBAgent is accurate). No fabricated 13/139 counts.

B3. MCP-only TODO/session/requirements storage.
Verdict: PASS
Evidence: git status for this slice is only the two cs files. docs/Project/TODO.yaml and requirements markdown were not edited. FR/TR/TEST/mapping/TODO were created through MCP in implementer session 01a0af86 (todo_create PLAN-TXNKEYSERVER-001). HV used MCP sessionlog/todo/requirements tools, not file edits of those stores.

B4. PowerShell-only / no Python.
Verdict: PASS
Evidence: HV used pwsh.exe and MCP tools. Implementer used pwsh execute_command for tests. No python/python3/py invocation found as a lab command in the implementer session (skill-list text mentions python as a word only).

B5. Honesty of Done=false and no live redeploy.
Verdict: PASS
Evidence: Matches A7.

### Surface C: requirements

C1. FR-MCP-173, TR-MCP-TXNKEY-001, TEST-MCP-221 exist and map.
Verdict: PASS
Evidence: same as A1.

C2. AC coverage in TransactionGatedSessionLogServiceTests for FR-MCP-173 body ACs 1,2,3,5 and TR-MCP-TXNKEY-001 body ACs 1-3.
Verdict: PASS
Evidence: GrokCode Submit/Upsert/Open bypass tests cover ac-1. QBAgent TEST-MCP-161 reject-before-mutation covers ac-2 / TR ac-2 / TR ac-3. Degraded GrokCode test covers ac-3. Focused class 13/13 0 skip covers ac-5. Live Enabled=true is ops AC-4, verified on disk, not a unit test. Codex is named as "or" with GrokCode; GrokCode is the exercised non-QBAgent type.

C3. Existing FR-MCP-120 session-log AC vs new bypass.
Verdict: FAIL
Evidence: MCP FR-MCP-120 body still gates first-party mutating paths; AC includes "session-log writes" through coordinator or fail closed while required transactions are active, isSatisfied=true. Mapping still FR-MCP-120 -> TR-MCP-TXN-001 / TEST-MCP-161. New FR-MCP-173 does not update that row. Product bypass is the approved carve-out, but FR-MCP-120 remains unamended. This blocks AGREE for class-1 requirement hygiene even though PLAN Done=false.

### Surface D: current plan

D1. Implementer did not claim PLAN-TXNKEYSERVER-001 complete.
Verdict: PASS
Evidence: todo_get Done=false. Claims explicitly say no product done-state change.

D2. Working-tree implementation matches the operator-approved design: keep live TurnTransactions.Enabled=true; stop non-QBAgent session-log from calling keyserver; QBAgent still gated.
Verdict: PASS
Evidence: Live yaml Enabled=true unchanged. Source bypass is non-QBAgent only. QBAgent tests still call coordinator. Live binary does not yet contain the bypass, consistent with not redeployed and not done.

## Design decisions (HV)

- Classified the request as class 1 project implementation before scoring C.
- Treated FR-MCP-173 as the approved carve-out for A/D, and still scored FR-MCP-120 as an active conflicting AC on surface C.
- Treated empty structured AcceptanceCriteria arrays as residual hygiene because Body contains AC text and the implementer did not claim done.
- Used implementer recap timestamps as red-before-green evidence only after extracting Failed 4 / named tests / then product edit; did not trust chat.

## Blockers for AGREE / done

- Amend FR-MCP-120 (and mapping if needed) so session-log coordinator/keyserver obligation is QBAgent-only, consistent with FR-MCP-173.
- Do not mark PLAN-TXNKEYSERVER-001 done until that conflict is closed and a later HV AGREE is recorded.
- Live service still lacks IsQuadBrainSessionLogSource; deploy remains out of this claim set.

## Session-log persistence proof (pre-complete)

sessionlog_query agent=GrokSubagentHostile from=2026-09-17T14:00:00Z todoId=PLAN-TXNKEYSERVER-001 totalCount=1 sessionId=GrokSubagentHostile-20260917T141036Z-txnkey-sessionlog-green turnCount=1 requestId=req-20260917T141036Z-001-txnkey-sessionlog-green status=in_progress turnId=45201.
