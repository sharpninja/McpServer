#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s2'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sigOk = [bool](Test-MarkerSignature -MarkerFile $marker)

$nonce = 'hv-closeout-s2-' + [guid]::NewGuid().ToString('N')
$healthUri = "http://PAYTON-LEGION2:7147/health?nonce=$nonce"
$health = Invoke-WebRequest -Uri $healthUri -UseBasicParsing -TimeoutSec 15
$healthBody = $health.Content
$nonceEchoed = $healthBody -match [regex]::Escape($nonce)

$pluginJson = Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' -Raw | ConvertFrom-Json
$pluginVersionFile = (Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.version' -Raw).Trim()

$worktree = 'F:\GitHub\McpServer\.worktrees\triage-closeout'
$git = @{
    Branch = (git -C $worktree rev-parse --abbrev-ref HEAD)
    Head = (git -C $worktree rev-parse HEAD)
    StatusPorcelain = @(git -C $worktree status --porcelain)
    Plan002Exists = [bool](Test-Path -LiteralPath (Join-Path $worktree 'docs\plans\triage-cluster-002.md'))
}

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    MarkerPath = $marker
    MarkerWrittenAtUtc = (Get-Item -LiteralPath $marker).LastWriteTimeUtc.ToString('o')
    SignatureValid = $sigOk
    HealthStatusCode = [int]$health.StatusCode
    Nonce = $nonce
    NonceEchoed = $nonceEchoed
    HealthBody = $healthBody
    PluginName = $pluginJson.name
    PluginVersionJson = $pluginJson.version
    PluginVersionFile = $pluginVersionFile
    Worktree = $worktree
    Git = $git
}

$jsonPath = Join-Path $outDir 'trust.json'
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 8)
exit 0
