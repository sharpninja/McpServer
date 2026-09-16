#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$stampIso = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$collectorName = "_hv-c-green-p16-p18-rerun-$utc"
$collector = Join-Path 'F:\GitHub\McpServer\docs\receipts' $collectorName
New-Item -ItemType Directory -Force -Path $collector | Out-Null
$cacheRoot = Join-Path $collector 'plugin-cache'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

$meta = [ordered]@{
    TimestampUtc = $stampIso
    CollectorUtc = $utc
    Collector = $collector
    CacheRoot = $cacheRoot
}
$meta | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $collector '00-meta.json') -Encoding utf8

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$invokePlugin = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$markerResolver = Join-Path $pluginRoot 'lib\marker-resolver.ps1'
$pluginJsonPath = Join-Path $pluginRoot '.grok-plugin\plugin.json'
$versionPath = Join-Path $pluginRoot '.version'

$pluginJson = Get-Content -LiteralPath $pluginJsonPath -Raw | ConvertFrom-Json
$pluginVersion = [string]$pluginJson.version
$dotVersion = if (Test-Path -LiteralPath $versionPath) { (Get-Content -LiteralPath $versionPath -Raw).Trim() } else { '' }

[ordered]@{
    pluginJsonPath = $pluginJsonPath
    pluginJsonName = [string]$pluginJson.name
    pluginJsonVersion = $pluginVersion
    dotVersionPath = $versionPath
    dotVersion = $dotVersion
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $collector '01-plugin-version.json') -Encoding utf8

$markerPath = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$markerText = Get-Content -LiteralPath $markerPath -Raw
$apiKey = $null
$baseUrl = $null
if ($markerText -match '(?m)^apiKey:\s*(\S+)') { $apiKey = $Matches[1].Trim() }
if ($markerText -match '(?m)^baseUrl:\s*(\S+)') { $baseUrl = $Matches[1].Trim() }

$searchUrl = "$baseUrl/mcpserver/tools/search?keyword=mcpserver-grok-plugin"
$headers = @{ 'X-Api-Key' = $apiKey }
$searchError = $null
$searchResult = $null
try {
    $searchResult = Invoke-RestMethod -Uri $searchUrl -Headers $headers -TimeoutSec 30
} catch {
    $searchError = [string]$_
}
$searchJsonPath = Join-Path $collector '02-tools-search.json'
$names = @()
if ($null -ne $searchResult) {
    if ($searchResult -is [System.Collections.IEnumerable] -and $searchResult -isnot [string]) {
        foreach ($item in $searchResult) {
            if ($item.PSObject.Properties['name']) { $names += [string]$item.name }
            elseif ($item.PSObject.Properties['Name']) { $names += [string]$item.Name }
            elseif ($item.PSObject.Properties['toolName']) { $names += [string]$item.toolName }
        }
    }
    if ($searchResult.PSObject.Properties['items']) {
        foreach ($item in @($searchResult.items)) {
            if ($item.PSObject.Properties['name']) { $names += [string]$item.name }
        }
    }
    if ($searchResult.PSObject.Properties['tools']) {
        foreach ($item in @($searchResult.tools)) {
            if ($item.PSObject.Properties['name']) { $names += [string]$item.name }
        }
    }
}
$exact = @($names | Where-Object { $_ -eq 'mcpserver-grok-plugin' })
[ordered]@{
    url = $searchUrl
    error = $searchError
    names = @($names | Select-Object -Unique)
    exactNamePresent = ($exact.Count -gt 0)
    rawType = if ($null -eq $searchResult) { 'null' } else { $searchResult.GetType().FullName }
    raw = $searchResult
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $searchJsonPath -Encoding utf8

. $markerResolver
$sig = Test-MarkerSignature -MarkerFile $markerPath
$boot = Invoke-FullBootstrap -StartDir $workspace
[ordered]@{
    markerPath = $markerPath
    TestMarkerSignature = [bool]$sig
    InvokeFullBootstrap = [bool]$boot
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $collector '03-marker-bootstrap.json') -Encoding utf8

$nonce = "nonce-hv-$utc-$PID"
$health = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 10
[ordered]@{
    nonceSent = $nonce
    nonceEchoed = [string]$health.nonce
    nonceMatch = ([string]$health.nonce -eq $nonce)
    status = [string]$health.status
    raw = $health
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $collector '04-health-nonce.json') -Encoding utf8

function Invoke-HvPlugin {
    param(
        [Parameter(Mandatory)][string]$Command,
        [string]$Method,
        [object]$ParamsObject,
        [string]$OutName
    )
    $pluginArgs = @{
        Command = $Command
        WorkspacePath = $workspace
        PluginRoot = $pluginRoot
        CacheRoot = $cacheRoot
        TimeoutSeconds = 180
    }
    if ($Method) { $pluginArgs['Method'] = $Method }
    if ($PSBoundParameters.ContainsKey('ParamsObject')) { $pluginArgs['ParamsObject'] = $ParamsObject }
    $outPath = Join-Path $collector $OutName
    $errPath = Join-Path $collector ($OutName + '.err.txt')
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $output = $null
    $invokeError = $null
    try {
        $output = & $invokePlugin @pluginArgs
    } catch {
        $invokeError = [string]$_
    }
    $sw.Stop()
    $text = if ($null -eq $output) { '' } elseif ($output -is [string]) { $output } else { ($output | Out-String) }
    [ordered]@{
        command = $Command
        method = $Method
        durationMs = $sw.ElapsedMilliseconds
        error = $invokeError
        output = $text
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $outPath -Encoding utf8
    if ($invokeError) { Set-Content -LiteralPath $errPath -Value $invokeError -Encoding utf8 }
    return $text
}

$statusOut = Invoke-HvPlugin -Command Status -OutName '05-plugin-status.json'

$sid = "GrokSubagentHostile-$utc-c-green-p16-p18-rerun"
$reqId = "req-$utc-001-hostile-c-green-p16-p18-rerun"

$bootOut = Invoke-HvPlugin -Command Invoke -Method 'workflow.sessionlog.bootstrap' -ParamsObject ([ordered]@{}) -OutName '06-sl-bootstrap.json'
$openOut = Invoke-HvPlugin -Command Invoke -Method 'workflow.sessionlog.openSession' -ParamsObject ([ordered]@{
    agent = 'GrokSubagentHostile'
    sessionId = $sid
    title = 'Hostile C-green-P16-P18 rerun validation'
    model = 'grok'
}) -OutName '07-sl-open.json'
$beginOut = Invoke-HvPlugin -Command Invoke -Method 'workflow.sessionlog.beginTurn' -ParamsObject ([ordered]@{
    requestId = $reqId
    queryTitle = 'Hostile C-green-P16-P18 rerun validation'
    queryText = 'Independent hostile re-verification of PLAN-PLUGINHANDOFF-001 Phase C-green-P16-P18 after claimed P18 preflight/Trait-split closeout.'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
}) -OutName '08-sl-begin.json'

$dialogOut = Invoke-HvPlugin -Command Invoke -Method 'workflow.sessionlog.appendDialog' -ParamsObject ([ordered]@{
    dialogItems = @(
        [ordered]@{
            timestamp = $stampIso
            role = 'model'
            content = 'Decision: classify this review as class 1 project implementation for PLAN-PLUGINHANDOFF-001 C-green-P16-P18 only. Surfaces A+B+C+D all apply. Review only; write receipt only; do not implement P19-P20 or mark TODOs done.'
            category = 'decision'
        }
        [ordered]@{
            timestamp = $stampIso
            role = 'model'
            content = 'add-profile executed first: 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile\. Plugin bootstrap, marker signature, health nonce, and dedicated GrokSubagentHostile session follow before claim checks.'
            category = 'observation'
        }
    )
}) -OutName '09-sl-dialog.json'

$histOut = Invoke-HvPlugin -Command Invoke -Method 'workflow.sessionlog.queryHistory' -ParamsObject ([ordered]@{
    agent = 'GrokSubagentHostile'
    limit = 10
    offset = 0
}) -OutName '10-sl-query-history.json'

[ordered]@{
    SessionId = $sid
    RequestId = $reqId
    Collector = $collector
    PluginVersion = $pluginVersion
    DotVersion = $dotVersion
    ExactToolNamePresent = ($exact.Count -gt 0)
    TestMarkerSignature = [bool]$sig
    InvokeFullBootstrap = [bool]$boot
    NonceMatch = ([string]$health.nonce -eq $nonce)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $collector '11-bootstrap-summary.json') -Encoding utf8

Write-Output $collector
Write-Output $sid
Write-Output $reqId
