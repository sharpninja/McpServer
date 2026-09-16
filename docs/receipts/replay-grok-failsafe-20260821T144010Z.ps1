#Requires -Version 7.0
# Replay Grok failsafe SessionLog.SubmitAsync records outside workflow.failsafe.drain.
# Drain hardcodes a 2s SubmitAsync timeout, which cannot post these payloads.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$failsafeDir = Join-Path $workspace '.mcpServer\grok\failsafe'
$receiptPath = Join-Path $workspace 'docs\receipts\failsafe-replay-20260821T144010Z.json'
$repl = Join-Path $pluginRoot 'lib\repl-invoke.ps1'

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
$env:REPL_TIMEOUT = '120'
$env:MCP_PLUGIN_TIMEOUT_SECONDS = '120'
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'

Set-Location -LiteralPath $workspace
. (Join-Path $pluginRoot 'lib\yaml-object-mutation.ps1')
Import-McpYamlSerializer

function Save-ReplayReceipt {
    param($Document)
    $json = $Document | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($receiptPath, $json)
}

$items = [System.Collections.Generic.List[object]]::new()
$files = @(Get-ChildItem -LiteralPath $failsafeDir -Filter '*.yaml' -File | Sort-Object Name)
$posted = 0
$failed = 0
$startedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')

foreach ($file in $files) {
    $row = [ordered]@{
        file = $file.Name
        startedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        success = $false
        exitCode = $null
        elapsedSec = $null
        error = $null
        sessionId = $null
        requestId = $null
    }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $doc = Read-McpYamlObject -Path $file.FullName
        $params = $doc['params']
        if ($params -is [System.Collections.IDictionary] -and $params.Contains('sessionLog')) {
            $sl = $params['sessionLog']
            if ($sl -is [System.Collections.IDictionary]) {
                if ($sl.Contains('sessionId')) { $row.sessionId = [string]$sl['sessionId'] }
                if ($sl.Contains('turns')) {
                    $t0 = @($sl['turns'])[0]
                    if ($t0 -is [System.Collections.IDictionary] -and $t0.Contains('requestId')) {
                        $row.requestId = [string]$t0['requestId']
                    }
                }
            }
        }
        $paramsYaml = ConvertTo-Yaml -Data $params -Options WithIndentedSequences
        $out = & $repl -Method 'client.SessionLog.SubmitAsync' -ParamsYaml $paramsYaml 2>&1 | Out-String
        $row.exitCode = $LASTEXITCODE
        $ok = ($LASTEXITCODE -eq 0 -and $out -match '(?m)^type:\s*result\s*$')
        if ($ok) {
            Remove-Item -LiteralPath $file.FullName -Force
            $row.success = $true
            $posted++
        } else {
            $failed++
            $err = ($out -replace '\s+', ' ').Trim()
            if ($err.Length -gt 400) { $err = $err.Substring(0, 400) }
            $row.error = $err
            if ($doc -is [System.Collections.IDictionary]) {
                $attempts = 0
                if ($doc.Contains('drainAttempts')) {
                    try { $attempts = [int]$doc['drainAttempts'] } catch { $attempts = 0 }
                }
                $doc['drainAttempts'] = $attempts + 1
                $doc['lastDrainError'] = if ($row.error) { $row.error } else { 'submit failed' }
                Write-McpYamlObject -Path $file.FullName -Document $doc
            }
        }
    } catch {
        $failed++
        $row.error = $_.Exception.Message
    } finally {
        $sw.Stop()
        $row.elapsedSec = [int]$sw.Elapsed.TotalSeconds
        $items.Add([pscustomobject]$row)
        $remaining = @(Get-ChildItem -LiteralPath $failsafeDir -Filter '*.yaml' -File).Count
        Save-ReplayReceipt -Document ([ordered]@{
            startedUtc = $startedUtc
            updatedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
            posted = $posted
            failed = $failed
            remaining = $remaining
            items = $items
        })
    }
}

$remaining = @(Get-ChildItem -LiteralPath $failsafeDir -Filter '*.yaml' -File).Count
Save-ReplayReceipt -Document ([ordered]@{
    startedUtc = $startedUtc
    updatedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    completedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    posted = $posted
    failed = $failed
    remaining = $remaining
    items = $items
})
Write-Output ("posted={0} failed={1} remaining={2}" -f $posted, $failed, $remaining)
