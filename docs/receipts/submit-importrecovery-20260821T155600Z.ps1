#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
Import-Module powershell-yaml -ErrorAction Stop

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:REPL_TIMEOUT = '120'
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_remaining-submit'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$jobs = @(
    @{
        name = 'ir1'
        yaml = 'F:\GitHub\McpServer\.mcpServer\Codex\transcripts\runs\upload-20260723T160335237Z-cfb1b75f\019d6472-5ba7-77d0-9b90-ee0d3f99599a.3fe421946dfc1353.sessionlog.yaml'
        pending = 'F:\GitHub\McpServer\.mcpServer\codex\failsafe\pending\019d6472-5ba7-77d0-9b90-ee0d3f99599a.3fe421946dfc1353.importRecovery.yaml'
    },
    @{
        name = 'ir2'
        yaml = 'F:\GitHub\McpServer\.mcpServer\Codex\transcripts\runs\upload-20260723T160338864Z-e25ddecc\019d6481-c77a-74b3-8bd7-103167e0e0bb.b8611bc5364bf417.sessionlog.yaml'
        pending = 'F:\GitHub\McpServer\.mcpServer\codex\failsafe\pending\019d6481-c77a-74b3-8bd7-103167e0e0bb.b8611bc5364bf417.importRecovery.yaml'
    }
)

$rows = foreach ($j in $jobs) {
    $sl = ConvertFrom-Yaml ([IO.File]::ReadAllText($j.yaml))
    $paramsPath = Join-Path $outDir ($j.name + '-params.yaml')
    $yamlText = ConvertTo-Yaml @{ sessionLog = $sl }
    [IO.File]::WriteAllText($paramsPath, $yamlText)
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $out = & $invoke -Command Invoke -Method 'client.SessionLog.SubmitAsync' -WorkspacePath 'F:\GitHub\McpServer' -ParamsPath $paramsPath 2>&1 | Out-String
    $ok = ($LASTEXITCODE -eq 0 -and $out -match '(?m)^type:\s*result\s*$')
    $head = ($out -replace '\s+', ' ').Trim()
    if ($head.Length -gt 320) { $head = $head.Substring(0, 320) }
    [pscustomobject]@{
        name = $j.name
        sessionId = [string]$sl.sessionId
        pending = $j.pending
        exitCode = $LASTEXITCODE
        success = $ok
        elapsedSec = [int]$sw.Elapsed.TotalSeconds
        head = $head
    }
}

$receipt = Join-Path $outDir 'importrecovery-submit.json'
[IO.File]::WriteAllText($receipt, ($rows | ConvertTo-Json -Depth 6))
$rows | ConvertTo-Json -Compress
