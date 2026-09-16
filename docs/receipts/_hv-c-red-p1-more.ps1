#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

function Save-Text {
    param($Path, $Value)
    if ($null -eq $Value) { $Value = '' }
    if ($Value -isnot [string]) { $Value = ($Value | Out-String) }
    Set-Content -LiteralPath $Path -Value $Value -Encoding utf8
}

# Timestamps
$paths = @(
    'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs',
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T230457Z.md',
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T230457Z.json',
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T213854Z.md',
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T220115Z.md'
)
$meta = foreach ($p in $paths) {
    $exists = Test-Path -LiteralPath $p
    if ($exists) {
        $i = Get-Item -LiteralPath $p
        [pscustomobject]@{ Path = $p; Exists = $true; LastWriteTimeUtc = $i.LastWriteTimeUtc.ToString('o'); Length = $i.Length }
    } else {
        [pscustomobject]@{ Path = $p; Exists = $false; LastWriteTimeUtc = $null; Length = 0 }
    }
}
$meta | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'timestamps.json') -Encoding utf8

# Git history of the new test file
$gitLog = git log --follow --format='%H %cI %s' -- 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' 2>&1
Save-Text (Join-Path $out 'git-log-testfile.txt') $gitLog
$gitBlame = git log -1 --format='%H %cI %s' -- 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' 2>&1
Save-Text (Join-Path $out 'git-last-testfile.txt') $gitBlame
$gitStatus = git status --porcelain -- 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' 'tests/McpServer.PluginIntegration.Tests' 'scenarios' 'build' 2>&1
Save-Text (Join-Path $out 'git-status-scope.txt') $gitStatus
$gitDiffStat = git diff --stat -- 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' 2>&1
Save-Text (Join-Path $out 'git-diffstat-testfile.txt') $gitDiffStat

# Broader file search (no python)
$hits = Get-ChildItem -LiteralPath 'F:\GitHub\McpServer' -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(\.git|bin|obj|\.nuke\\temp)\\' } |
    Where-Object {
        $_.Name -match 'PluginIntegration|PluginSessionLog|plugin-sessionlog-scenarios'
    } |
    Select-Object -ExpandProperty FullName
Save-Text (Join-Path $out 'file-search-pluginint.txt') ($hits -join "`n")

# Skip attributes in the new tests
$skipHits = Select-String -Path 'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs' -Pattern 'Skip|Fact\(|Theory\(' 
Save-Text (Join-Path $out 'skip-scan.txt') ($skipHits | ForEach-Object { $_.ToString() } | Out-String)

# Target property on Build type via files
$buildProps = Select-String -Path 'F:\GitHub\McpServer\build\*.cs' -Pattern 'Target PluginSessionLog|PluginSessionLogIntegration' -ErrorAction SilentlyContinue
Save-Text (Join-Path $out 'build-cs-target-scan.txt') ($(if ($buildProps) { $buildProps | ForEach-Object { $_.ToString() } } else { 'NO_MATCH' }) | Out-String)

# Health nonce
$nonce = [guid]::NewGuid().ToString('N')
try {
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 15
    $healthObj = [ordered]@{ nonceSent = $nonce; nonceEcho = $health.nonce; status = $health.status; match = ($health.nonce -eq $nonce); raw = $health }
    $healthObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'health-nonce.json') -Encoding utf8
    Write-Output ("HEALTH_NONCE_MATCH=" + $healthObj.match)
} catch {
    Save-Text (Join-Path $out 'health-nonce.json') $_.Exception.ToString()
    Write-Output 'HEALTH_NONCE_FAIL'
}

# Marker signature via plugin Status
try {
    $status = & $plugin -Command Status -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 60 2> (Join-Path $out 'plugin-status.err.txt')
    Save-Text (Join-Path $out 'plugin-status.txt') $status
    Write-Output 'PLUGIN_STATUS_OK'
} catch {
    Save-Text (Join-Path $out 'plugin-status.txt') $_.Exception.ToString()
    Write-Output 'PLUGIN_STATUS_FAIL'
}

# Session query proof
function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120 2> $errFile
        Save-Text $outFile $result
        Write-Output ('OK ' + $Name)
    } catch {
        Save-Text $outFile ('ERROR ' + $_.Exception.ToString())
        Write-Output ('FAIL ' + $Name)
    }
}

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokSubagentHostile'
    text = 'GrokSubagentHostile-20260821T231421Z-c-red-p1'
    limit = 5
} -Name 'sl-query-text'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokSubagentHostile'
    todoId = 'PLAN-PLUGINHANDOFF-001'
    limit = 5
} -Name 'sl-query-todo'

# Requirements via client Get* to compare with earlier getFr timestamps
Invoke-Plugin -Method 'client.Requirements.GetFrAsync' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr-client'
Invoke-Plugin -Method 'client.Requirements.GetTrAsync' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr-client'
Invoke-Plugin -Method 'client.Requirements.GetTestAsync' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test-client'

# TRX summary
$trx = Join-Path $out 'hv-c-red-p1.trx'
if (Test-Path -LiteralPath $trx) {
    [xml]$x = Get-Content -LiteralPath $trx
    $counters = $x.TestRun.ResultSummary.Counters
    $summary = [ordered]@{
        outcome = $x.TestRun.ResultSummary.outcome
        total = $counters.total
        executed = $counters.executed
        passed = $counters.passed
        failed = $counters.failed
        skipped = if ($counters.skipped) { $counters.skipped } else { '0' }
    }
    $summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'trx-summary.json') -Encoding utf8
    Write-Output ('TRX ' + ($summary | ConvertTo-Json -Compress))
}

Write-Output 'MORE_DONE'
