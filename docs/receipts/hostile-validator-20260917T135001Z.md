# Hostile validation: session-keyserver-scope

UTC: 2026-09-17T13:50:01Z
Workspace: F:\GitHub\McpServer
Reviewer: GrokSubagentHostile
Class: 2 (user-directed ops / diagnosis). Parent did not implement product code and did not claim a plan step done.
Byrd v4: N/A for this class-2 slice.
add-profile: executed first. Read 19 non-skill profile markdown files under C:\Users\kingd\.claude\profile (excluded add-profile.grok.md). Profile file count recorded below.

## OverallVerdict

AGREE

Accuracy: 99
Completeness: 99
PASS: 9
FAIL: 0
UNKNOWN: 0
Surface B: PASS
Surface C: N/A
Surface D: N/A

OverallVerdict=AGREE only because both scores are at least 98 and every applicable claim PASS.

## Surface A: requested claims

### Claim 1 PASS
New MCP session GrokCode-20260917T132412Z-new-mcp-session exists server-side and did not reuse GrokCode-20260703T131501Z-mousekeyproxy-sync.

Receipt:
- mcpserver__sessionlog_query text=GrokCode-20260917T132412Z-new-mcp-session returned totalCount=1, sourceType=GrokCode, sessionId exact match, started 2026-09-17T13:24:14Z, turnCount=2.
- mcpserver__sessionlog_query text=GrokCode-20260703T131501Z-mousekeyproxy-sync returned totalCount=0 items=[].
- Live log C:\ProgramData\McpServer\logs\mcp-20260917.log has no mousekeyproxy string.
- sessionlog_open for the new id at 2026-09-17 08:25:38 -05:00 returned created=false on a concurrent retry; the session id is still the new id, not mousekeyproxy-sync.

### Claim 2 PASS
First sessionlog_begin_turn failed with Keyserver manifest signing failed (txn-082d9e0fd0a94d7f8fe615dfc820c65d); retry succeeded; later turn req-20260917T132923Z-prompt-b144 persisted as turnId 45193 and completed.

Receipt:
- Log line 25867 ERR: Turn transaction coordinator did not commit sessionlog.upsert_turn 'txn-082d9e0fd0a94d7f8fe615dfc820c65d': Keyserver manifest signing failed. Stack: TransactionGatedSessionLogService.ExecuteMutationAsync then FwhMcpTools.UpsertLifecycleTurnToolAsync.
- Same request logged tools/call sessionlog_begin_turn agent=GrokCode sessionId=GrokCode-20260917T132412Z-new-mcp-session requestId=req-20260917T132530Z-001-add-profile-new-mcp-session, jsonrpc error internal_server_error with that txn id.
- Retry at 2026-09-17 08:25:59 -05:00 same begin_turn args returned success turnId=45191 status=in_progress.
- Later begin_turn at 2026-09-17 08:34:37 -05:00 requestId=req-20260917T132923Z-prompt-b144 returned success turnId=45193 status=in_progress.
- Independent sessionlog_query shows that requestId status=completed with queryTitle "Keyserver must stay QuadBrain-only".
Note: triage summary says retry committed turnId 45191 (the first turn). Claim 2 names 45193 for the later turn. Both ids are in the live log. HV begin_turn for this review succeeded as turnId 45196, so keyserver signing is not a 100 percent failure.

### Claim 3 PASS
TransactionGatedSessionLogService always routes mutating session-log ops through ITurnTransactionCoordinator; coordinator SignManifestAsync is the keyserver path when Mcp:TurnTransactions Enabled and RequiredForMutations are true.

Receipt:
- src/McpServer.Support.Mcp/Services/TransactionGatedSessionLogService.cs ExecuteMutationAsync: if coordinator is null, inner mutation; else always _coordinator.ExecuteAsync. Submit, dialog, UpsertTurn, open, and other mutating methods call ExecuteMutationAsync.
- Program.cs lines 612-624 always wrap ISessionLogService with TransactionGatedSessionLogService(..., sp.GetService<ITurnTransactionCoordinator>(), ...).
- TransactionSecurityServiceCollectionExtensions.cs registers ITurnTransactionCoordinator as TurnTransactionCoordinator singleton.
- TurnTransactionCoordinator.ExecuteAsync: if !Mutating or !Enabled or !RequiredForMutations, bypass without SignManifestAsync. Otherwise SignManifestAsync calls _keyServer.SignManifestAsync; failure message is "Keyserver manifest signing failed."
Caveat (not a FAIL of the live path): RepairWorkspaceStampsAsync fail-closes instead of ExecuteMutationAsync when required transactions are active. Coordinator-null is a test/DI hole, not the live decorator.

### Claim 4 PASS
Live C:\ProgramData\McpServer\appsettings.yaml has TurnTransactions.Enabled=true and RequiredForMutations=true. Repo src/McpServer.Support.Mcp/appsettings.yaml default Enabled=false.

Receipt:
- Live file lines 1217-1219: TurnTransactions Enabled: true, RequiredForMutations: true.
- Repo file lines 130-132: TurnTransactions Enabled: false, RequiredForMutations: true.

### Claim 5 PASS
FR-MCP-120 currently requires session-log writes through coordinator/keyserver when turn transactions are active.

Receipt:
- docs/Project/Functional-Requirements.md FR-MCP-120 body: gate first-party mutating user-turn paths behind transaction manifest signing and subscriber commit confirmation.
- Same FR AC: session-log writes either route through compensation-capable coordinator gates or fail closed while required transactions are active.
- MCP requirements_list type=fr Id=FR-MCP-120 body and AC match that text. Store Status=pending (docs matrix says Complete). Parent did not claim the FR is complete. Manifest signing plus coordinator gates is the live keyserver path when Enabled and RequiredForMutations are true.

### Claim 6 PASS
Failsafe YAML exists at the claimed path.

Receipt:
- Get-Item FullName F:\GitHub\McpServer\.mcpServer\failsafe\GrokCode\workspaces\RjpcR2l0SHViXE1jcFNlcnZlcg\pending\20260917T133442Z-keyserver-sessionlog-scope.yaml Length=1668 LastWriteTimeUtc=2026-09-17T13:34:42Z.
- File kind=triage-failsafe reporterAgent=GrokCode sessionId=GrokCode-20260917T132412Z-new-mcp-session turnId=req-20260917T132923Z-prompt-b144 includes txn-082d9e0fd0a94d7f8fe615dfc820c65d.

### Claim 7 PASS
Triage reportId triage-report-25aebcc191a8436f9d0034d6599ee07a groupId triage-group-81f17141af2ec492 was submitted.

Receipt:
- mcpserver__triage_status reportId returned reportId exact, groupId=triage-group-81f17141af2ec492, status=grouped, createdUtc=2026-09-17T13:35:06.4913182+00:00.
- mcpserver__triage_status groupId returned status=collecting reportCount=1 same reportId.

### Claim 8 PASS
Parent did not implement a product fix and is waiting for BDPv4 plan approval because this is a requirement-scope change.

Receipt:
- git status --porcelain -- src docs/Project empty.
- git diff --stat -- src empty.
- Get-ChildItem src LastWriteTimeUtc >= 2026-09-17T12:00:00Z: NONE.
- Get-ChildItem docs *plan* LastWriteTimeUtc today: NONE.
- Parent completed turn response: "No product implementation until the plan is approved."
Note: no BDPv4 plan file exists yet. The claim is that they are waiting, not that a plan is already in review. That matches requirement-change-plan-first: do not implement an unapproved requirement change.

### Claim 9 PASS
Operator profile was loaded (19 non-skill profile markdown files).

Receipt:
- Directory C:\Users\kingd\.claude\profile: 20 *.md, 1 skill port add-profile.grok.md, 19 non-skill.
- HV add-profile read those 19 files in full before claim checks.
- Parent chat_history.jsonl at C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a0af86-0463-75d2-91cd-cdd02d5453e0\chat_history.jsonl: 19 unique read_file calls of those same 19 files (readCalls=19 uniqueFiles=19). add-profile.grok.md was not in the unique set.
Note: parent reasoning summaries at one point said 8 or 9 files. Tool_calls prove 19. Claim 9 is about the load, which happened.

## Surface B: workspace rules PASS

Honesty: parent diagnosed and triaged; did not claim a product FR or plan step complete; distinguished requirement-scope change from a bug-to-fix-now.
Receipts: failsafe YAML, triage ids, session-log turn, live log txn id.
MCP-only storage: no TODO.yaml or session-log file edits. Failsafe YAML is the allowed failsafe path.
Lab PowerShell / no-Python: parent and HV used pwsh native objects. HV did not invoke python.
Byrd v4: not applied to this class-2 diagnosis.

## Surface C: requirement violations N/A

Class-2 diagnosis. Do not FAIL for missing new FR/TR. Parent did not claim a product FR complete.

## Surface D: current plan holistically N/A

No plan path claimed complete.

## Findings that did not flip a claim

- F1: Parent reasoning summaries under-counted profile files as 8/9; unique read_file set is 19.
- F2: RepairWorkspaceStampsAsync fail-closes rather than coordinator.ExecuteAsync.
- F3: MCP store FR-MCP-120 Status=pending versus docs matrix Complete.
- F4: Keyserver signing is not universally failing (HV turnId 45196 succeeded).
- F5: No BDPv4 plan artifact on disk yet.

## Scores

Accuracy 99: every named id, path, flag, and turn was re-checked against live MCP, live logs, or on-disk files. Residual caveats do not falsify the claims.
Completeness 99: all 9 claims, surfaces B/C/D, class recorded, add-profile first, jsonl paths, session-log persist required.

=== VERDICT JSON ===
