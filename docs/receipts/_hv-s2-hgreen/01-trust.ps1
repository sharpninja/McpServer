#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
if (-not (Test-Path -LiteralPath $outDir)) {
    [void][System.IO.Directory]::CreateDirectory($outDir)
}
$out = Join-Path $outDir '01-trust.json'

Push-Location $wt
try {
    $utc = [datetime]::UtcNow
    $stamp = $utc.ToString('yyyyMMddTHHmmssZ')
    $iso = $utc.ToString('yyyy-MM-ddTHH:mm:ssZ')
    $branch = [string](git rev-parse --abbrev-ref HEAD)
    $sha = [string](git rev-parse HEAD)
    $dirty = @(git status --porcelain)
    $tracked = @(git diff --name-only HEAD)
    $untracked = @(git ls-files --others --exclude-standard)

    . (Join-Path $main 'plugins\core\lib-ps\marker-resolver.ps1')
    $marker = Join-Path $main 'AGENTS-README-FIRST.yaml'
    $sigOk = [bool](Test-MarkerSignature -MarkerFile $marker)
    $nonce = 'hv-s2hg-{0}-{1}' -f $stamp, $PID
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -TimeoutSec 10
    $echo = $null
    if ($health.PSObject.Properties.Name -contains 'nonce') { $echo = [string]$health.nonce }

    $pluginJsonPath = Join-Path $plugin '.grok-plugin\plugin.json'
    $pluginJson = Get-Content -LiteralPath $pluginJsonPath -Raw | ConvertFrom-Json
    $versionFile = Join-Path $plugin '.version'
    $pluginDotVersion = if (Test-Path -LiteralPath $versionFile) { (Get-Content -LiteralPath $versionFile -Raw).Trim() } else { $null }

    $apiKey = $null
    $keyLine = Select-String -LiteralPath $marker -Pattern '^apiKey:\s*(?<v>.+)$' | Select-Object -First 1
    if ($keyLine) { $apiKey = $keyLine.Matches[0].Groups['v'].Value.Trim() }
    $toolsExact = $null
    $toolsError = $null
    try {
        $headers = @{ 'X-Api-Key' = $apiKey }
        $tools = Invoke-RestMethod -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -TimeoutSec 20
        $names = @()
        if ($tools -is [System.Array]) { $names = @($tools | ForEach-Object { [string]$_.name }) }
        elseif ($tools.PSObject.Properties.Name -contains 'items') { $names = @($tools.items | ForEach-Object { [string]$_.name }) }
        elseif ($tools.PSObject.Properties.Name -contains 'tools') { $names = @($tools.tools | ForEach-Object { [string]$_.name }) }
        $toolsExact = @($names | Where-Object { $_ -eq 'mcpserver-grok-plugin' }).Count
    } catch {
        $toolsError = $_.Exception.Message
    }

    $obj = [ordered]@{
        TimestampUtc = $iso
        Stamp = $stamp
        Worktree = $wt
        Branch = $branch
        Sha = $sha
        DirtyCount = $dirty.Count
        DirtyPreview = @($dirty | Select-Object -First 80)
        DiffNameOnly = @($tracked)
        Untracked = @($untracked)
        MarkerLastWriteUtc = (Get-Item -LiteralPath $marker).LastWriteTimeUtc.ToString('o')
        SignatureOk = $sigOk
        NonceSent = $nonce
        NonceEcho = $echo
        NonceMatch = ($echo -eq $nonce)
        HealthStorage = [string]$health.storage
        HealthVersion = [string]$health.version
        HealthStatus = [string]$health.status
        PluginJsonVersion = [string]$pluginJson.version
        PluginJsonName = [string]$pluginJson.name
        PluginDotVersion = $pluginDotVersion
        PluginRoot = $plugin
        ToolsSearchExactNameCount = $toolsExact
        ToolsSearchError = $toolsError
        SuggestedSessionId = ('GrokCode-{0}-hostile-s2-hgreen' -f $stamp)
        SuggestedRequestId = ('req-{0}-001-hostile-s2-hgreen' -f $stamp)
    }
    $obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
    Write-Output ("WROTE {0} branch={1} sha={2} sig={3} nonce={4} stamp={5} dirty={6}" -f $out, $branch, $sha, $sigOk, $obj.NonceMatch, $stamp, $obj.DirtyCount)
} finally {
    Pop-Location
}
