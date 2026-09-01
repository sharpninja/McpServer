# Hostile Validator Receipt

TimestampUtc: 2026-08-18T00:04:00Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: user-directed general action (class 2). Windows service restart of McpServer. Not product implementation. Implementer claimed no plan-step done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md).
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.93.0)
Marker signature: Test-MarkerSignature True (F:\GitHub\McpServer\AGENTS-README-FIRST.yaml)
Health nonce: 36d0cbc1c48647afa537ca0a4e50d71d echoed exactly. HealthStatus=200. storage=reachable. FULL_BOOTSTRAP=True
SessionId: GrokCode-20260818T000358Z-hostile-restart
RequestId: req-20260818T000358Z-001-hostile-validate-restart
ServerTurnId: 41570
planFile: None
todoId: None
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass re-queried Win32_Service and Get-Service, compared process PID to the workspace marker, independently called /health with a fresh nonce and /ready, re-read live AgentHelp as an object, hashed the live YAML and the deployed exe, scanned C:\ProgramData\McpServer\logs\mcp-20260817.log, and proved the review session with sessionlog_query. The implementer receipt was not trusted.

This review did not restart the service. This review did not edit product code.

## Classification

Class 2. Operator-directed Windows service restart. Surface C is N/A. Surface D is N/A. Byrd v4 is not applied to the ops action.

## Session-log persistence proof (required reviewer process)

Native MCP Streamable HTTP at http://PAYTON-LEGION2:7147/mcp-transport (not raw /mcpserver/sessionlog REST):

- initialize HTTP 200. Mcp-Session-Id 8WXAGYZoGjCVuE1iWvBmIQ
- sessionlog_open: success=true, created=true, sessionId=GrokCode-20260818T000358Z-hostile-restart
- sessionlog_begin_turn with planFile=None and todoId=None: success=true, turnId=41570, status=in_progress
- sessionlog_dialog: success=true, totalDialogItems=5 (four observation, one decision)
- sessionlog_replace_section actions: success=true, 6 actions including two design_decision
- sessionlog_complete_turn: success=true, turnId=41570, status=completed
- sessionlog_query text equal to the exact sessionId: totalCount=0 (text filter does not match the id string)
- sessionlog_query text=hostile-restart: totalCount=1 but the hit is GrokCode-20260817T235647Z-plugin-session because that session's queryText contains the spawn prompt token hostile-restart. Not this review session.
- sessionlog_query agent=GrokCode, from=2026-08-18T00:00:00Z: totalCount=1, sessionId=GrokCode-20260818T000358Z-hostile-restart, title=Hostile validate McpServer Windows service restart, turn requestId=req-20260818T000358Z-001-hostile-validate-restart, turn status=completed, queryTitle=Hostile validate McpServer service restart, actionCount=6, dialogCount=5, planFile=None, todoId=None

Persistence is proved by the from-date sessionlog_query result.

## Explicit FAIL list

- A3. First post-restart /health was 200 with nonce echo and storage unreachable. The first logged GET /health after Application started is HTTP 200 with nonce echoed and storage=reachable, not unreachable. Zero "unreachable" hits in the 18:38-18:42 local window of mcp-20260817.log. Later /health and /ready being 200 with storage reachable is true and does not rescue the false first-body claim.
- B5. Honesty / no fabricated results. Implementer receipt stated first post-start health storage: unreachable. Server-log Output of that first completed GET /health contradicts it. The same receipt also stated MCP Streamable HTTP 503 backend_unavailable in that window; this review found zero 503 and zero backend_unavailable hits in 18:38-18:42 (GET /mcp-transport after restart was 404 on a stale session id).

## Explicit UNKNOWN list

None on applicable surfaces. Notes that are not FAILs:

- Get-WinEvent System/Service Control Manager returned no McpServer events in the restart window. Restart is proved by the application log and live PID, not by SCM event 7036.
- Get-Process StartTimeUtc was unavailable without elevation. Win32_Process CreationDateUtc was available and matched the claimed new start time.
- sessionlog_query text filter does not match sessionId strings.
- Unrelated dirty handoff/product tree exists. It is outside this ops window.

## Claims reviewed

### A Requested

#### A1. Restarted McpServer via one elevated Restart-Service. Old PID 5572, new PID 57744, Status Running.

Verdict: PASS

Evidence:

- Prior independent hostile receipt docs/receipts/hostile-validator-20260817T233618Z.md recorded Win32_Service ProcessId=5572 while State=Running.
- This review: Get-Service Status=Running StartType=Automatic. Win32_Service State=Running ProcessId=57744 StartMode=Auto StartName=LocalSystem PathName=`C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147` ExitCode=0.
- Win32_Process Id=57744 CreationDateUtc=2026-08-17T23:38:29.5863800Z. Matches implementer AFTER StartTimeUtc=2026-08-17T23:38:29.5863805Z.
- Server log: 2026-08-17 18:38:27.324 -05:00 Graceful shutdown initiated: PID=5572. 18:38:27.861 Graceful shutdown completed: PID=5572. 18:38:31.405 Server startup event: PID=57744. 18:38:56.167 Now listening on http://[::]:7147. 18:38:56.168 Application started.
- Leftover script docs/receipts/_restart-mcpserver-20260817T233717Z.ps1 contains exactly one Restart-Service, zero Stop-Service, zero Start-Service, zero Copy-Item. Elevation is implied because Restart-Service on a LocalSystem service succeeded (PID changed).
- This review did not restart the service again.

#### A2. Marker AGENTS-README-FIRST.yaml now has pid 57744 and a rotated apiKey.

Verdict: PASS

Evidence:

- Workspace marker pid=57744. PidMatchService=True. Test-MarkerSignature=True.
- MarkerLastWriteTimeUtc=2026-08-17T23:38:48.7976227Z. startedAt=2026-08-17T23:38:48.7047470+00:00. serverStartedAtUtc=2026-08-17T23:38:29.7115442+00:00.
- Server log: Deleted MCP marker file at F:\GitHub\McpServer\AGENTS-README-FIRST.yaml at 18:38:27.414. Wrote MCP marker file: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml at 18:38:48.798.
- Pre-restart live RequestHeaders against X-Workspace-Path=F:\GitHub\McpServer used apiKey prefix IHOW suffix idDI (example 18:30:02 GET /mcpserver/sessionlog). Current workspace marker apiKey prefix N3fW suffix RMao, SHA256=E7B163CCB214AB8176C372809B7A8CBE4B87B9C74FBBFB2C407994B566849664. Different key. Rotation proved.
- ProgramData marker is a different workspace key (suffix ygKw) with the same pid 57744. That is expected and is not the workspace marker under review.

#### A3. First post-restart /health was 200 with nonce echo and storage unreachable; later /health and /ready are 200 with storage reachable.

Verdict: FAIL

Evidence:

- Application started at 18:38:56.168 local. First GET /health ENTRY 18:38:56.415. First completed GET /health 18:38:56.584 HTTP 200. Output: status=Healthy, nonce=ffbf87a5a57c46cdada44497d922e256, storage=reachable.
- Second GET /health 18:38:57.750 HTTP 200, nonce=nonce-20260817183857-56476, storage=reachable.
- Zero lines containing unreachable in 18:38-18:42 of mcp-20260817.log.
- Later implementer nonce postrestart3 at 18:56:24.098 HTTP 200, storage=reachable. Independent review /health 200 nonce 36d0cbc1c48647afa537ca0a4e50d71d echoed, storage=reachable. Independent /ready 200, checks workspace-ready Healthy and storage Healthy, storage=reachable.
- The later-half of the claim is true. The first-body storage=unreachable assertion is false. Compound claim FAILs.

#### A4. AgentHelp live config survived: grok-cli / grok-4.5 / Enabled true.

Verdict: PASS

Evidence:

- Read-McpYamlObject on C:\ProgramData\McpServer\appsettings.yaml.
- LastWriteTimeUtc=2026-08-17T23:30:09.0404870Z Length=58975 SHA256=B42E2462D67EADE136EC3BF64A1224BF1253ADB73EA6596CFED1BC7C7A4E3D46. Identical to the prior 23:36:18Z independent hostile receipt. File was not rewritten by the restart.
- AgentHelpKeys=DefaultExecutionStrategy,HelperModel,Enabled.
- DefaultExecutionStrategy=grok-cli TYPE=String.
- HelperModel=grok-4.5 TYPE=String.
- Enabled=True TYPE=bool.

#### A5. No binary deploy. No SCM account/start-type change.

Verdict: PASS

Evidence:

- Deployed exe LastWriteTimeUtc=2026-08-12T21:55:30.4271605Z FileVersion=1.4.26.0 ProductVersion=1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e SHA256=A95B178712D30BE73CB55AEC8DF98127F44DDDEE4A62C932E52C1D3B09AF5529.
- C:\ProgramData\McpServer\.mcpservice-deployment.json LastWriteTimeUtc=2026-08-12T21:55:34.9448414Z generatedBy=build/Build.UpdateService.cs. Support.Mcp.exe hash in that file matches the live exe hash.
- All ProgramData binaries except appsettings.yaml (23:30:09, config) and AGENTS-README-FIRST.yaml (23:38:50, marker rewrite) have LastWriteTimeUtc on 2026-08-12 or earlier.
- Win32_Service StartMode=Auto StartName=LocalSystem PathName unchanged from the 23:36:18Z independent receipt.

### B Workspace rules

#### B1. Byrd Development Process v4

Verdict: PASS (N/A to class 2)

Evidence: Operator-directed service restart, not project implementation. Byrd phase-order was not applied and is not required.

#### B2. Always bring the receipts

Verdict: PASS

Evidence: Implementer receipt exists at docs/receipts/restart-mcpserver-20260817T233829Z.md. This review re-ran service CIM, Get-Service, marker signature, health nonce, ready, object-first live YAML, exe hash, and server-log scans. Helper scripts: docs/receipts/_hv-restart-verify-20260817T234200Z.ps1, _hv-restart-logs-20260817T235959Z.ps1, _hv-restart-logs2-20260818T000100Z.ps1, _hv-restart-healthbody-20260818T000200Z.ps1, _hv-restart-unreach-20260818T000300Z.ps1, _hv-restart-postrestart3-20260818T000400Z.ps1, _hv-restart-session-20260818T000500Z.ps1.

#### B3. MCP-only storage

Verdict: PASS

Evidence: No direct edit of todo.yaml, session-log store files, or requirements store. Session logging used native sessionlog_* tools. docs/Project/TODO.yaml LastWriteTimeUtc remains 2026-07-10T00:56:30.7156679Z.

#### B4. PowerShell-only / no Python

Verdict: PASS

Evidence: This review used pwsh.exe -NoProfile -NonInteractive only. Python, python3, and py are present on the machine and were not invoked. Implementer leftover scripts are pwsh.

#### B5. Honesty / no fabricated results

Verdict: FAIL

Evidence: Implementer claimed first post-restart /health storage=unreachable and Streamable HTTP 503 backend_unavailable. First logged GET /health Output is storage=reachable. UnreachableHits=0 and BackendOr503Hits=0 in 18:38-18:42. Other A claims matched artifacts. The false first-health storage claim is enough to fail honesty on this receipt.

### C Requirements

Verdict: N/A

Class 2 operator-directed ops. No product feature shipped. No FR/TR completion claimed. Missing FR/TR is not a fail.

### D Current plan holistically

Verdict: N/A

Implementer did not claim a plan-step done. planFile=None. todoId=None.

## Observations that are not FAILs

- Repo and live ProgramData AgentHelp remain as previously reviewed. Restart did not mutate the live YAML.
- Unrelated dirty handoff/product tree exists. Targeted AgentHelp/restart product files were not part of this ops claim.
- GET /ready was not present in the 18:38-18:42 log window. Later independent /ready 200 is the proof for current readiness.

## Ratings

Accuracy: 96. Live service, marker, health, ready, YAML, exe, and first-health log body were re-read. Residual 4 points: SCM event log is empty so the Restart-Service cmdlet itself is inferred from the leftover script plus one graceful SCM-style shutdown, not from Event Viewer.

Completeness: 95. Required re-checks (Get-Service, PID vs marker, /health nonce, live AgentHelp YAML) plus binary/SCM/log attacks and sessionlog_query were done. First /ready in the implementer's later window was not found in the early log slice; current /ready was used instead.

## Files written by this review

- docs/receipts/hostile-validator-20260818T000400Z.md
- docs/receipts/hostile-validator-20260818T000400Z.json
- docs/receipts/_hv-restart-verify-20260817T234200Z.ps1
- docs/receipts/_hv-restart-logs-20260817T235959Z.ps1
- docs/receipts/_hv-restart-logs2-20260818T000100Z.ps1
- docs/receipts/_hv-restart-healthbody-20260818T000200Z.ps1
- docs/receipts/_hv-restart-unreach-20260818T000300Z.ps1
- docs/receipts/_hv-restart-postrestart3-20260818T000400Z.ps1
- docs/receipts/_hv-restart-session-20260818T000500Z.ps1
