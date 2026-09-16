#Requires -Version 7.0
# Repair and submit the 3 remaining Codex quarantine SessionLog records.
# Does not overwrite prior failsafe-replay receipts.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$receiptPath = Join-Path $workspace 'docs\receipts\failsafe-remaining-codex-20260821T155103Z.json'
$repl = Join-Path $pluginRoot 'lib\repl-invoke.ps1'
$qdir = Join-Path $workspace '.mcpServer\codex\failsafe\quarantine'

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
$env:REPL_TIMEOUT = '120'
$env:MCP_PLUGIN_TIMEOUT_SECONDS = '120'
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'

Set-Location -LiteralPath $workspace
Import-Module powershell-yaml -ErrorAction Stop

function Save-ReplayReceipt {
    param($Document)
    [System.IO.File]::WriteAllText($receiptPath, ($Document | ConvertTo-Json -Depth 8))
}

function ConvertTo-TurnList {
    param($Turns)
    $list = [System.Collections.Generic.List[object]]::new()
    if ($null -eq $Turns) { return $list }
    if ($Turns -is [System.Collections.IList] -and -not ($Turns -is [string])) {
        foreach ($item in $Turns) { $list.Add($item) }
        return $list
    }
    if ($Turns -is [System.Collections.IDictionary]) {
        $keys = @($Turns.Keys)
        $looksLikeIndexMap = ($keys.Count -gt 0 -and ($keys | Where-Object { $_ -match '^\d+$' }).Count -eq $keys.Count)
        if ($looksLikeIndexMap) {
            foreach ($k in ($keys | Sort-Object { [int]$_ })) { $list.Add($Turns[$k]) }
            return $list
        }
        $list.Add($Turns)
        return $list
    }
    $list.Add($Turns)
    return $list
}

function Set-TurnRequiredContext {
    param($Turn)
    if ($Turn -is [System.Collections.IDictionary]) {
        if (-not $Turn.Contains('planFile') -or [string]::IsNullOrWhiteSpace([string]$Turn['planFile'])) {
            $Turn['planFile'] = 'None'
        }
        if (-not $Turn.Contains('todoId') -or [string]::IsNullOrWhiteSpace([string]$Turn['todoId'])) {
            $Turn['todoId'] = 'None'
        }
    }
}

$items = [System.Collections.Generic.List[object]]::new()
$posted = 0
$failed = 0
$startedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$files = @(Get-ChildItem -LiteralPath $qdir -Filter '*.yaml' -File | Sort-Object Name)

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
        repair = $null
    }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $raw = [System.IO.File]::ReadAllText($file.FullName)
        $doc = ConvertFrom-Yaml $raw
        if ($null -eq $doc.params -or $null -eq $doc.params.sessionLog) {
            throw 'record has no params.sessionLog'
        }
        $sl = $doc.params.sessionLog
        $row.sessionId = [string]$sl.sessionId
        $turnList = ConvertTo-TurnList -Turns $sl.turns
        foreach ($turn in $turnList) { Set-TurnRequiredContext -Turn $turn }
        $sl.turns = $turnList
        if ($turnList.Count -gt 0 -and $turnList[0] -is [System.Collections.IDictionary] -and $turnList[0].Contains('requestId')) {
            $row.requestId = [string]$turnList[0]['requestId']
        }
        $row.repair = 'turns-as-list plus planFile/todoId None'
        $paramsYaml = ConvertTo-Yaml -Data $doc.params -Options None
        $out = & $repl -Method 'client.SessionLog.SubmitAsync' -ParamsYaml $paramsYaml 2>&1 | Out-String
        $row.exitCode = $LASTEXITCODE
        $ok = ($LASTEXITCODE -eq 0 -and $out -match '(?m)^type:\s*result\s*$')
        if ($ok) {
            Remove-Item -LiteralPath $file.FullName -Force
            $reason = $file.FullName + '.reason.txt'
            if (Test-Path -LiteralPath $reason) { Remove-Item -LiteralPath $reason -Force }
            $row.success = $true
            $posted++
        } else {
            $failed++
            $err = ($out -replace '\s+', ' ').Trim()
            if ($err.Length -gt 400) { $err = $err.Substring(0, 400) }
            $row.error = $err
        }
    } catch {
        $failed++
        $row.error = $_.Exception.Message
    } finally {
        $sw.Stop()
        $row.elapsedSec = [int]$sw.Elapsed.TotalSeconds
        $items.Add([pscustomobject]$row)
        Save-ReplayReceipt -Document ([ordered]@{
            startedUtc = $startedUtc
            updatedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
            posted = $posted
            failed = $failed
            remaining = @(Get-ChildItem -LiteralPath $qdir -Filter '*.yaml' -File).Count
            items = $items
        })
    }
}

Save-ReplayReceipt -Document ([ordered]@{
    startedUtc = $startedUtc
    updatedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    completedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    posted = $posted
    failed = $failed
    remaining = @(Get-ChildItem -LiteralPath $qdir -Filter '*.yaml' -File).Count
    items = $items
})
Write-Output ("posted={0} failed={1} remaining={2}" -f $posted, $failed, @(Get-ChildItem -LiteralPath $qdir -Filter '*.yaml' -File).Count)
