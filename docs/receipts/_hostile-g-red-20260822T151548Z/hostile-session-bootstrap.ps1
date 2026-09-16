#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$worktree = 'C:\Users\kingd\.grok\worktrees\github-mcpserver\subagent-01a029d1-24e7-7453-b027-93176ddfd033'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hostile-g-red-20260822T151548Z'
$cacheRoot = 'F:\GitHub\McpServer\.mcpServer\grok-subagent-hostile-g-red'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

$utc = [DateTime]::UtcNow
$stamp = $utc.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokSubagentHostile-$stamp-g-red-hostile"
$turnId = "req-$stamp-001-g-red-hostile-validate"
$agent = 'GrokSubagentHostile'

. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')

$signatureOk = Test-MarkerSignature -MarkerFile $marker
$baseUrl = Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl'
$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 10
$nonceOk = [string]$health.nonce -eq $nonce
$pluginVersion = (Get-Content -LiteralPath (Join-Path $pluginRoot '.version') -Raw).Trim()
$pluginJson = Get-Content -LiteralPath (Join-Path $pluginRoot '.claude-plugin\plugin.json') -Raw | ConvertFrom-Json

function Invoke-HostilePlugin {
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = @{},
        [int]$TimeoutSeconds = 120
    )
    $splat = @{
        Command = 'Invoke'
        Method = $Method
        WorkspacePath = $workspace
        PluginRoot = $pluginRoot
        CacheRoot = $cacheRoot
        TimeoutSeconds = $TimeoutSeconds
    }
    if ($Params.Count -gt 0) {
        $splat['ParamsObject'] = $Params
    }
    & $invoke @splat
}

$trust = [ordered]@{
    TimestampUtc = $utc.ToString('o')
    Stamp = $stamp
    SessionId = $sessionId
    RequestId = $turnId
    Agent = $agent
    MarkerPath = $marker
    MarkerSignatureOk = [bool]$signatureOk
    BaseUrl = $baseUrl
    HealthNonce = $nonce
    HealthNonceEcho = [string]$health.nonce
    HealthNonceOk = [bool]$nonceOk
    HealthStatus = [string]$health.status
    PluginRoot = $pluginRoot
    PluginVersionFile = $pluginVersion
    PluginJsonName = [string]$pluginJson.name
    PluginJsonVersion = [string]$pluginJson.version
    CacheRoot = $cacheRoot
}

$trust | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'trust.json') -Encoding utf8

if (-not $signatureOk) { throw 'MCP_UNTRUSTED: marker signature failed' }
if (-not $nonceOk) { throw 'MCP_UNTRUSTED: health nonce mismatch' }

$bootstrap = Invoke-HostilePlugin -Method 'workflow.sessionlog.bootstrap' -Params @{}
$open = Invoke-HostilePlugin -Method 'workflow.sessionlog.openSession' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile G-red validation PLAN-PLUGINHANDOFF-001 Phase G'
    model = 'grok-4'
}
$begin = Invoke-HostilePlugin -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $turnId
    queryTitle = 'Hostile G-red validation Phase G named tests'
    queryText = 'Adversarial review of G-RED claims A1-A5 for PLAN-PLUGINHANDOFF-001 Phase G. Class 1. Review only. No dump/import implementation.'
}

$planTodo = Invoke-HostilePlugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' }
$wikiTodo = Invoke-HostilePlugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-WIKIEXPORT-001' }

$reqIds = @(
    'FR-MCP-WIKIEXPORT-003',
    'FR-MCP-WIKIEXPORT-004',
    'FR-MCP-WIKIEXPORT-005'
)
$trIds = @(
    'TR-MCP-WIKIEXPORT-003',
    'TR-MCP-WIKIEXPORT-004',
    'TR-MCP-WIKIEXPORT-005'
)
$testIds = @(
    'TEST-MCP-WIKIEXPORT-003',
    'TEST-MCP-WIKIEXPORT-004',
    'TEST-MCP-WIKIEXPORT-005'
)

$fr = @{}
foreach ($id in $reqIds) {
    $fr[$id] = Invoke-HostilePlugin -Method 'workflow.requirements.getFr' -Params @{ id = $id }
}
$tr = @{}
foreach ($id in $trIds) {
    $tr[$id] = Invoke-HostilePlugin -Method 'workflow.requirements.getTr' -Params @{ id = $id }
}
$tests = @{}
foreach ($id in $testIds) {
    $tests[$id] = Invoke-HostilePlugin -Method 'workflow.requirements.getTest' -Params @{ id = $id }
}
$maps = @{}
foreach ($id in $reqIds) {
    $maps[$id] = Invoke-HostilePlugin -Method 'workflow.requirements.listMappings' -Params @{ frId = $id }
}

$gitStatus = git -C $worktree status --short
$gitSrc = git -C $worktree diff --name-only -- src
$gitTests = git -C $worktree diff --name-only -- tests
$gitStat = git -C $worktree diff --stat -- src tests docs/receipts docs/plans

$sessionOut = [ordered]@{
    Trust = $trust
    Bootstrap = [string]$bootstrap
    Open = [string]$open
    Begin = [string]$begin
    PlanTodo = [string]$planTodo
    WikiTodo = [string]$wikiTodo
    Fr = $fr
    Tr = $tr
    Tests = $tests
    Mappings = $maps
    GitStatus = @($gitStatus)
    GitSrcDiffNames = @($gitSrc)
    GitTestDiffNames = @($gitTests)
    GitStat = [string]$gitStat
}
$sessionOut | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outDir 'session-bootstrap.json') -Encoding utf8

Write-Output ($sessionOut | ConvertTo-Json -Depth 10)
