# Hostile validator receipt

TimestampUtc: 2026-08-21T01:04:31.910Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
add-profile: executed yes
ProfileFileCount: 18 (all non-skill *.md under C:\Users\kingd\.claude\profile; excluded skill port add-profile.grok.md)
WorkClass: class 2 user-directed ops. Operator told implementer to validate the marker via the plugin, never roll HMAC. Not project-requirement implementation. Surface C N/A. Surface D N/A (implementer did not claim PLAN done).
ActivePlan: not claimed complete
TodoId: None
SessionId: GrokCode-20260821T005742Z-hostile-hmac-plugin
RequestId: req-20260821T005742Z-001-hostile-plugin-hmac
PluginVersion: 1.97.0 from F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json and .version (not the marker 1.95.0)
ValidatorPluginPath: F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1 and lib\Invoke-McpPlugin.ps1
ValidatorDidNotRollHmac: true (this review called plugin Test-MarkerSignature / Invoke-FullBootstrap / Status only)

## This validator's plugin re-run (A2)

Command: pwsh.exe -NoProfile -NonInteractive -File docs/receipts/_hv-hmac-plugin-validate.ps1
UTC: 2026-08-21T00:57:42.790Z
Invoke-McpPlugin Status: status=available agent=GrokCode cacheDir=F:\GitHub\McpServer\.mcpServer\grok
Test-MarkerSignature=True (dot-sourced F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1; MarkerFile F:\GitHub\McpServer\AGENTS-README-FIRST.yaml)
Invoke-FullBootstrap=True (StartDir F:\GitHub\McpServer; plugin health nonce check inside that function)
No HMACSHA256 constructed in this validator's scripts.

## Implementer identity

Parent GrokCode session 01a01290-749a-7271-8c76-d04be7e683d7
Plugin session cache: GrokCode-20260820T234412Z-plugin-session
Operator prompt (chat_history L489): Use the plugin to validate, never roll your own HMAC
Implementer turn: req-20260821T005459Z-018-plugin-hmac-validate

## Surface A Requested validation

### A1 Implementer used plugin Test-MarkerSignature and Invoke-FullBootstrap (and/or Invoke-McpPlugin Status), not a hand-rolled HMACSHA256 canonical builder
Verdict: PASS
Evidence:
- After the operator instruction, implementer pipeline call-05acd9b2-1313-44e9-ab39-6923fbc3a916-323 (pwsh invoke_expression, 1.76s) printed ===STATUS=== Invoke-McpPlugin.ps1 -Command Status status=available, then ===PLUGIN_TRUST=== testMarkerSignature=true invokeFullBootstrap=true rolledOwnHmac=false pluginVersion=1.97.0 utc=2026-08-21T00:54:59.0247327Z.
- Artifact copy: docs/receipts/_hv-hmac-plugin-only/plugin-this-turn-tool-result.jsonl
- After the operator instruction, HMACSHA256 hits in chat_history are L510 (read of plugin marker-resolver.ps1 Test-MarkerSignature body) and L545 (concession dialog text saying never HMACSHA256 in agent scripts). Neither is a new homemade canonical builder.

### A2 Plugin reports signature valid and bootstrap true this turn (this validator re-ran plugin functions)
Verdict: PASS
Evidence: this review's 2026-08-21T00:57:42.790Z plugin run: Test-MarkerSignature=True; Invoke-FullBootstrap=True; Status available. Matches implementer 00:54:59 PLUGIN_TRUST.

### A3 Implementer conceded the prior homemade HMAC false-negative
Verdict: PASS
Evidence:
- Prior homemade HMAC (call-3b3c6885-6b2f-4617-bb79-93518c8f852a-52 at 2026-08-20T23:46:01.3485936Z): signatureOk=false computed=09A02381566BF7AEEF61607C947698916DCCEB27C63B336C835D09A2B1ADC0C8 expected=64512C73B38D93CA862A1633182941E39A7AF414A9FE6605598E89D5B22915DA nonceOk=true. Artifact: docs/receipts/_hv-hmac-plugin-only/homemade-hmac-tool-result.jsonl
- Immediate concession (chat_history L82): Nonce matched; my HMAC parser is wrong (H0 already had signatureOk true on the same marker).
- This-turn concession (chat_history L545 sessionlog_dialog itemsJson): I rolled my own HMAC last turn with a hand-built canonical string. Test-MarkerSignature later proved the marker valid; my parser was wrong. Standing rule: plugin Test-MarkerSignature / Invoke-FullBootstrap / Invoke-McpPlugin Status only. Never HMACSHA256 in agent scripts.
- Plugin Test-MarkerSignature later the same hour (2026-08-20T23:46:28.3338818Z) returned signatureOk true on the same marker, proving the homemade result was a false-negative.

## Surface B Workspace rules

### B1 Byrd v4 phase-order
Verdict: N/A
Class 2 ops. No product implementation claimed this slice.

### B2 Always bring the receipts
Verdict: PASS
Homemade false-negative, plugin-this-turn trust object, and this validator's re-run are command outputs, not narrative.

### B3 MCP-only storage
Verdict: PASS
No direct edit of TODO.yaml / session-log files / requirements store. Session work used plugin/native sessionlog tools (failsafe SubmitAsync still present; that is the persist bug, not a storage-file edit).

### B4 PowerShell-only / no Python
Verdict: PASS
Implementer used pwsh invoke_expression. Python hits after the operator instruction are profile reads (no-python-lab.md, PROFILE.md), not python.exe. This validator used pwsh.exe -NoProfile -NonInteractive only.

### B5 Honesty / no fabricated results
Verdict: PASS
Implementer reported homemade signatureOk false, then plugin true, then conceded the parser was wrong. Does not claim PLAN done. Does not claim the homemade HMAC was valid.

## Surface C Requirements
Verdict: N/A
User-directed ops. Do not FAIL for missing FR/TR.

## Surface D Current plan holistically
Verdict: N/A
Implementer did not claim PLAN-SESSIONLOGREMEDIATE-001 or any plan step done.

## Counts

PASS: 7 (A1 A2 A3 B2 B3 B4 B5)
FAIL: 0
UNKNOWN: 0
N/A: 3 (B1 Byrd, C, D)

FAIL list: (empty)

UNKNOWN list: (empty)

OverallVerdict: AGREE

Accuracy: 96 (plugin re-run plus on-disk chat_history tool results; sessionlog_query for requestId plugin-hmac-validate returned 0 so MCP persist of the concession turn is unproven, but concession exists in chat_history and dialog tool call)
Completeness: 94 (A1-A3 and applicable B scored; C/D classified N/A; did not require PLAN DoD)

AccuracyRating: 96
CompletenessRating: 94
