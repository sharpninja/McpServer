#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_h0-hostile-raw'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'

function Invoke-Save {
    param([string]$Method, [string]$Params, [string]$Name)
    $text = ''
    try {
        $raw = & $invoke -Command Invoke -Method $Method -Params $Params -WorkspacePath $workspace 2>&1
        $text = ($raw | Out-String)
    } catch {
        $text = [string]$_
    }
    if (-not $text) { $text = '(empty)' }
    Set-Content -LiteralPath (Join-Path $outDir $Name) -Value $text -Encoding utf8
    Write-Output ("WROTE {0} chars={1}" -f $Name, $text.Length)
}

$dialog = @'
dialogItems:
  - timestamp: 2026-08-18T19:38:42Z
    role: model
    content: |
      Decision: Classify this run as class 1 S0/H0. Surface C applies. Extra plan TEST ids STORE-003-007, PLUGIN-002-005, TODO-002 are observations, not H0 FAILs, because the operator DoD for this gate is the eight mapped FR/TR/TEST pairs plus export plus ValidateTraceability.
      Alternatives considered: FAIL D on missing extra TEST ids (rejected: contradicts the locked H0 DoD in the brief); skip store re-query and trust markdown (rejected: accuracy-first).
      Consequence: OverallVerdict AGREE. PLAN-TRIAGECLUSTER-001 stays Done=false until the operator or parent applies this AGREE.
    category: decision
  - timestamp: 2026-08-18T19:38:50Z
    role: model
    content: Re-queried MCP store via plugin getFr/getTr/getTest/listMappings/todo.get and client.SessionLog.QueryAsync. Re-ran ValidateTraceability. Confirmed McpToolErrors.cs still old.
    category: observation
'@
Invoke-Save -Method 'workflow.sessionlog.appendDialog' -Params $dialog -Name '41-appendDialog.txt'

$actions = @'
actions:
  - order: 1
    description: 'add-profile: read 18 non-skill profile markdown files'
    type: edit
    status: completed
    filePath: C:\Users\kingd\.claude\profile
  - order: 2
    description: Test-MarkerSignature true; health nonce nonce-20260818142908-26972 echoed
    type: edit
    status: completed
    filePath: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
  - order: 3
    description: Store getFr FR-MCP-TRIAGEERR-001 title and ac-1 and 2026-08-18 amendment notes
    type: edit
    status: completed
    filePath: docs/receipts/_h0-hostile-raw/10-fr-FR-MCP-TRIAGEERR-001.txt
  - order: 4
    description: ValidateTraceability Succeeded findings=0
    type: edit
    status: completed
    filePath: build.ps1
  - order: 5
    description: Wrote hostile receipt twin AGREE
    type: create
    status: completed
    filePath: docs/receipts/hostile-validator-20260818T193842Z.md
  - order: 6
    description: 'Decision: extra plan TEST ids are not an H0 FAIL; operator DoD is eight mapped triples plus VT'
    type: design_decision
    status: completed
    filePath: docs/plans/triage-cluster-001.md
'@
Invoke-Save -Method 'workflow.sessionlog.appendActions' -Params $actions -Name '42-appendActions.txt'

$complete = @'
response: |
  OverallVerdict AGREE. Receipt docs/receipts/hostile-validator-20260818T193842Z.md plus json twin. PASS 23 FAIL 0 UNKNOWN 0. ValidateTraceability Succeeded findings=0. No product implementation.
'@
Invoke-Save -Method 'workflow.sessionlog.completeTurn' -Params $complete -Name '43-completeTurn.txt'

$prove = @'
agent: GrokCode
text: Hostile H0 review of PLAN-TRIAGECLUSTER-001 S0
limit: 5
'@
Invoke-Save -Method 'client.SessionLog.QueryAsync' -Params $prove -Name '44-sessionlog-query-hostile-complete.txt'
Write-Output 'PERSIST2 DONE'
