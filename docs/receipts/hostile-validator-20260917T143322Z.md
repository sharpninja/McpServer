# Hostile validation receipt: txnkey FR-MCP-120 rereview

TimestampUtc: 2026-09-17T14:33:22Z
ValidatorIdentity: GrokSubagentHostile
WorkspacePath: F:\GitHub\McpServer
Gate: txnkey-fr120-rereview
AddProfileExecuted: yes
AddProfileFileCount: 19
AddProfileExcluded: add-profile.grok.md (skill port)
WorkClass: 1
WorkClassDetail: class 1 project implementation rereview of prior FAIL C-FR120-conflict after parent amended FR-MCP-120 via requirements store. Not a user-directed ops action.
RequestJsonl: F:\GitHub\McpServer\docs\receipts\hv\20260917T143322Z-txnkey-fr120-rereview.request.jsonl
ResponseJsonl: F:\GitHub\McpServer\docs\receipts\hv\20260917T143322Z-txnkey-fr120-rereview.response.jsonl
HvSessionId: GrokSubagentHostile-20260917T142902Z-txnkey-fr120-rereview
HvRequestId: req-20260917T142902Z-001-txnkey-fr120-rereview
HvTurnId: 45203
Agent: GrokSubagentHostile
ActivePlan: operator-approved QuadBrain-only keyserver for session-log (PLAN-TXNKEYSERVER-001)
TodoId: PLAN-TXNKEYSERVER-001
PriorReceipt: F:\GitHub\McpServer\docs\receipts\hostile-validator-20260917T141843Z.md

## Classification

Class 1 project implementation. Surfaces A, B, C, and D all apply. This rereview attacks the FR-MCP-120 store amendment against the prior FAIL C-FR120-conflict. Implementer did not mark PLAN-TXNKEYSERVER-001 done.

## Trust bootstrap

- Marker HMAC-SHA256 recomputed from workspace API key: SIG_OK=True (value 5A0E2E806E4607AFF4D9D318CAEC38CB1E58BF13522364296470880078FA8CD3).
- GET /health nonce 741d7a74127844d3b54571d20684b7f1 echoed exactly. NONCE_OK=True. version 1.4.38+6a72d445dda56f1d288660de044a2b020291630d.

## OverallVerdict

AGREE

Accuracy: 99
Completeness: 99
PASS: 16
FAIL: 0
UNKNOWN: 0

Prior FAIL C-FR120-conflict is remediated in the MCP requirements store. GetFr and requirements_list both show ac-fr120-001 gating QBAgent session-log writes and carving non-QBAgent session-log writes to FR-MCP-173. Product ExecuteMutationAsync still bypasses the coordinator for non-QBAgent. PLAN-TXNKEYSERVER-001 remains Done=false.

## Explicit FAIL list

None.

## Explicit UNKNOWN list

None.

## Residuals (do not block this AGREE)

- Markdown projection `docs/Project/Functional-Requirements.md` FR-MCP-120 still has unqualified "session-log writes" at the first AC bullet and unqualified "Session-log rollback" at the fourth AC bullet. Store is the source of truth. Parent must regenerate via MCP `requirements_generate` before treating markdown as current. This is projection lag, not an unamended store AC.
- TR-MCP-TXN-001 body still says generic mutation paths use coordinator or fail closed and still names `TransactionGatedSessionLogService` without a QBAgent qualifier. Structured AC count is 0. The more specific TR-MCP-TXNKEY-001 plus amended FR-MCP-120 govern the session-log source-type split.
- FR-MCP-173 / TR-MCP-TXNKEY-001 structured AcceptanceCriteria arrays are empty; TEST-MCP-221 uses Condition rather than structured AC. AC text is in Body/Condition. Residual hygiene, same as prior HV.
- Live service binary was not redeployed in this slice. Out of this rereview claim set.

## Claims reviewed

### Surface A: requested validation

A1. MCP store FR-MCP-120 structured acceptanceCriteria no longer contains unqualified "session-log writes" as a coordinator-required path. GetFr shows QBAgent session-log writes plus FR-MCP-173 carve-out.
Verdict: PASS
Evidence: REST GET `/mcpserver/requirements/fr/FR-MCP-120` and MCP `requirements_list` type=fr both return Id=FR-MCP-120 Title="MCP Server transaction gating" Status=pending Notes="Amended 2026-09-17 for FR-MCP-173: session-log coordinator/keyserver applies to QBAgent only." Body states FR-MCP-173 carves out non-QBAgent session-log writes and QBAgent/QuadBrain session-log writes remain in this gate. ac-fr120-001 isSatisfied=True text contains "QBAgent session-log writes" in the coordinator/fail-closed list, then "Non-QBAgent session-log writes are governed by FR-MCP-173 and SHALL persist without coordinator/keyserver even when Mcp:TurnTransactions:Enabled=true." Phrase scan of Body+Notes+AC texts: 4 matches of "session-log writes", all qualified (non-QBAgent persist x2, QBAgent remain in gate, QBAgent in coordinator list). ac-fr120-004 is "QBAgent session-log rollback", not unqualified session-log writes.

A2. FR-MCP-173 / TR-MCP-TXNKEY-001 / TEST-MCP-221 mapping still intact.
Verdict: PASS
Evidence: REST GET mapping FR-MCP-173: frId=FR-MCP-173 trIds=["TR-MCP-TXNKEY-001"] testIds=["TEST-MCP-221"]. MCP requirements_list mapping row matches. GetFr FR-MCP-173 Title="Keyserver signs QuadBrain transactions only" Body still requires ordinary agent session-log mutations persist without keyserver when Enabled=true, QBAgent remains gated. GetTr TR-MCP-TXNKEY-001 Title="Session-log keyserver gate is QBAgent-only" Body still requires ExecuteMutationAsync inner mutation when sourceType is not QBAgent. GetTest TEST-MCP-221 Condition length 644 names TransactionGatedSessionLogServiceTests, GrokCode/Codex persist with Request null, QBAgent still hits coordinator, degraded GrokCode succeeds, red-before-green, zero fail zero skip.

A3. Product still bypasses coordinator for non-QBAgent (ExecuteMutationAsync).
Verdict: PASS
Evidence: `src/McpServer.Support.Mcp/Services/TransactionGatedSessionLogService.cs` lines 283-284: `if (_coordinator is null || !IsQuadBrainSessionLogSource(sourceType)) return await mutation(cancellationToken)`. Lines 336-337: `IsQuadBrainSessionLogSource` is `string.Equals(sourceType, "QBAgent", StringComparison.OrdinalIgnoreCase)`. Submit/Upsert/Replace/Open/Dialog/section/delete paths all funnel through ExecuteMutationAsync with sourceType. RepairWorkspaceStampsAsync remains inner/fail-closed and is out of this slice per TR-MCP-TXNKEY-001.

A4. PLAN-TXNKEYSERVER-001 still Done=false.
Verdict: PASS
Evidence: MCP `todo_get` PLAN-TXNKEYSERVER-001 Done=false CompletedDate=null DoneSummary=null. Parent has not marked done on this rereview.

A5. Prior FAIL C-FR120-conflict is remediated.
Verdict: PASS
Evidence: Prior receipt `docs/receipts/hostile-validator-20260917T141843Z.md` FAIL C-FR120-conflict was unqualified "session-log writes" in FR-MCP-120 AC while product bypassed non-QBAgent. Store AC is now QBAgent-qualified plus FR-MCP-173 carve-out. Product bypass remains. Conflict against the store is gone.

### Surface B: workspace rules

B1. Byrd v4 for this rereview (requirements amendment after prior red/green slice).
Verdict: PASS
Evidence: This turn is a store amendment rereview, not a new implementation slice. Did not FAIL B2 from FR createdAt vs file mtime. Prior slice already showed tests then product. Requirement change was HV-required remediation of C-FR120-conflict, not a new product behavior.

B2. Always bring the receipts / honesty.
Verdict: PASS
Evidence: HV re-queried GetFr/GetTr/GetTest/mapping, todo_get, and re-read ExecuteMutationAsync. Did not trust prior HV prose for the current FR-MCP-120 AC text.

B3. MCP-only TODO/session/requirements storage.
Verdict: PASS
Evidence: HV used MCP sessionlog/todo/requirements_list plus REST GetFr as the GetFr surface the brief named (hosted MCP has list, not get-by-id). Did not edit TODO.yaml or requirements markdown. git status porcelain for this slice is still only the two cs product/test files.

B4. PowerShell-only / no Python.
Verdict: PASS
Evidence: HV used pwsh.exe (PowerShell.MCP) and MCP tools. No python/python3/py.

B5. Honesty of Done=false.
Verdict: PASS
Evidence: Matches A4.

### Surface C: requirements

C1. FR-MCP-120 store AC no longer conflicts with FR-MCP-173 / product bypass.
Verdict: PASS
Evidence: same as A1 and A5. Mapping FR-MCP-120 remains TR-MCP-TXN-001 / TEST-MCP-161 / TEST-MCP-168, which still covers QBAgent session-log gating. Non-QBAgent persist is mapped on FR-MCP-173 -> TEST-MCP-221.

C2. FR-MCP-173 mapping intact.
Verdict: PASS
Evidence: same as A2.

C3. AC coverage for the carve-out still exists in TransactionGatedSessionLogServiceTests (methods named for TEST-MCP-221 ac-1 and ac-3).
Verdict: PASS
Evidence: tests file still contains SubmitAsync_WhenNonQuadBrainAndCoordinatorWouldReject_PersistsWithoutCallingCoordinator, UpsertTurnAsync_WhenNonQuadBrain..., OpenSessionAsync_WhenNonQuadBrain..., SubmitAsync_WhenNonQuadBrainAndCoordinatorDegraded_PersistsWithoutCallingCoordinator. This rereview did not re-run dotnet test; it does not claim a new green count.

C4. No remaining store AC that lists unqualified session-log writes as coordinator-required.
Verdict: PASS
Evidence: same phrase scan as A1. Markdown export is residual projection lag, not the store.

### Surface D: current plan

D1. PLAN-TXNKEYSERVER-001 is not marked done.
Verdict: PASS
Evidence: todo_get Done=false. This HV may AGREE; parent had not marked done at review time.

D2. Working-tree implementation still matches the operator-approved QuadBrain-only session-log keyserver design.
Verdict: PASS
Evidence: ExecuteMutationAsync bypass is non-QBAgent only. Live yaml TurnTransactions.Enabled=true RequiredForMutations=true LastWrite=2026-09-16T15:21:04.1655807Z unchanged. Live binary deploy remains out of this claim set.

## Design decisions (HV)

- Classified the request as class 1 project implementation before scoring C.
- Treated MCP store GetFr / requirements_list as authoritative for C-FR120-conflict remediation, not the stale markdown projection.
- Did not FAIL TR-MCP-TXN-001 generic mutation-path prose because FR-MCP-120 and TR-MCP-TXNKEY-001 are the specific session-log source-type requirements and FR-MCP-120 was the prior FAIL.
- Did not re-run unit tests on this rereview; product-bypass claim is source-level.
- AGREE here authorizes the parent to mark PLAN-TXNKEYSERVER-001 done only after citing this receipt. Regenerate Functional-Requirements.md via MCP generate before treating markdown as current.

## Blockers for AGREE / done

None for this rereview. Parent may mark PLAN-TXNKEYSERVER-001 done citing this AGREE. Residual markdown regenerate is recommended, not a remaining FAIL.

## Session-log persistence proof (pre-complete)

sessionlog_open created=true sessionId=GrokSubagentHostile-20260917T142902Z-txnkey-fr120-rereview. sessionlog_begin_turn success=true turnId=45203 requestId=req-20260917T142902Z-001-txnkey-fr120-rereview status=in_progress.
