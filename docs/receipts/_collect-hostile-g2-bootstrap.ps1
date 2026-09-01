#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$markerPath = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$nonce = [guid]::NewGuid().ToString('N')

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    CompactUtc   = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    Workspace    = $workspace
    MarkerPath   = $markerPath
    PluginRoot   = $pluginRoot
}

Push-Location $workspace
try {
    $result.GitBranch = (git rev-parse --abbrev-ref HEAD).Trim()
    $result.GitHead   = (git rev-parse HEAD).Trim()
    $result.GitStatusPorcelain = @(git status --porcelain) -join "`n"
} finally {
    Pop-Location
}

$pluginJson = Join-Path $pluginRoot '.claude-plugin\plugin.json'
$versionFile = Join-Path $pluginRoot '.version'
$result.PluginJsonExists = Test-Path -LiteralPath $pluginJson
$result.PluginVersionFileExists = Test-Path -LiteralPath $versionFile
if ($result.PluginJsonExists) {
    $pj = Get-Content -LiteralPath $pluginJson -Raw | ConvertFrom-Json
    $result.PluginJsonName = [string]$pj.name
    $result.PluginJsonVersion = [string]$pj.version
}
if ($result.PluginVersionFileExists) {
    $result.PluginDotVersion = (Get-Content -LiteralPath $versionFile -Raw).Trim()
}

$statusCmd = @(
    'pwsh.exe', '-NoProfile', '-NonInteractive', '-File',
    (Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'),
    '-Command', 'Status',
    '-WorkspacePath', $workspace,
    '-PluginRoot', $pluginRoot
)
$statusOutput = & $statusCmd[0] $statusCmd[1..($statusCmd.Count-1)] 2>&1 | Out-String
$result.PluginStatusExit = $LASTEXITCODE
$result.PluginStatusOutput = $statusOutput.Trim()

$healthUrl = "http://PAYTON-LEGION2:7147/health?nonce=$nonce"
try {
    $health = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 15
    $result.HealthStatusCode = [int]$health.StatusCode
    $result.HealthBody = [string]$health.Content
    $result.NonceSent = $nonce
    $result.NonceEchoed = $health.Content -match [regex]::Escape($nonce)
} catch {
    $result.HealthError = $_.Exception.Message
    $result.NonceSent = $nonce
    $result.NonceEchoed = $false
}

$markerResolver = Join-Path $pluginRoot 'lib\marker-resolver.ps1'
$result.MarkerResolverExists = Test-Path -LiteralPath $markerResolver

$result | ConvertTo-Json -Depth 8
