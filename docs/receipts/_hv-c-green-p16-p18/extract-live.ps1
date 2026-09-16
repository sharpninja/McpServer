#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18'

function Extract-TodoDone {
    param([string]$Path, [string]$Dest)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false; path = $Path } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $topDone = $null
    if ($text -match '(?m)^\s+done:\s+(true|false)\s*$') { $topDone = ($Matches[1] -eq 'true') }
    $tasks = [regex]::Matches($text, '(?m)^\s+- task:\s*(.+?)\r?\n\s+done:\s+(true|false)')
    $taskObjs = foreach ($m in $tasks) {
        [pscustomobject]@{ task = $m.Groups[1].Value.Trim(); done = ($m.Groups[2].Value -eq 'true') }
    }
    [ordered]@{
        exists = $true
        path = $Path
        length = (Get-Item -LiteralPath $Path).Length
        lastWriteTimeUtc = (Get-Item -LiteralPath $Path).LastWriteTimeUtc.ToString('o')
        topDone = $topDone
        taskCount = @($taskObjs).Count
        doneTrueCount = @($taskObjs | Where-Object { $_.done }).Count
        p16 = @($taskObjs | Where-Object { $_.task -match 'P16' })
        p17 = @($taskObjs | Where-Object { $_.task -match 'P17' })
        p18 = @($taskObjs | Where-Object { $_.task -match 'P18' })
        p19 = @($taskObjs | Where-Object { $_.task -match 'P19' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'C P16|C-green-P16|P16 red \+ hostile then P17' })
        tasks = $taskObjs
        hasError = [bool]($text -match '(?i)ERROR |not found|401|403')
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

function Extract-Req {
    param([string]$Path, [string]$Dest, [string]$Kind)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false; kind = $Kind } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $id = $null
    if ($text -match '(?m)^\s+id:\s+(\S+)') { $id = $Matches[1] }
    $title = $null
    if ($text -match '(?m)^\s+title:\s+(.+)$') { $title = $Matches[1].Trim() }
    $status = $null
    if ($text -match '(?m)^\s+status:\s+(\S+)') { $status = $Matches[1] }
    $isSatisfied = $null
    if ($text -match '(?m)^\s+isSatisfied:\s+(true|false)') { $isSatisfied = ($Matches[1] -eq 'true') }
    $acs = [regex]::Matches($text, '(?ms)^\s+- id:\s*(AC\d+)[^\n]*\r?\n(?:\s+(?!- id:).+?\r?\n)*')
    $acObjs = foreach ($m in [regex]::Matches($text, '(?ms)^\s+- id:\s*(AC\d+)\s*\r?\n\s+text:\s*(.+?)(?=\r?\n\s+- id:|\r?\n\s+[a-zA-Z]|\z)')) {
        [pscustomobject]@{ id = $m.Groups[1].Value; text = $m.Groups[2].Value.Trim() }
    }
    if (@($acObjs).Count -eq 0) {
        $acObjs = foreach ($m in [regex]::Matches($text, '(?m)^\s+- id:\s*(AC\d+)\s*$')) {
            [pscustomobject]@{ id = $m.Groups[1].Value; text = $null }
        }
    }
    [ordered]@{
        exists = $true
        kind = $Kind
        path = $Path
        length = (Get-Item -LiteralPath $Path).Length
        id = $id
        title = $title
        status = $status
        isSatisfied = $isSatisfied
        acCount = @($acObjs).Count
        acs = $acObjs
        snippet = if ($text.Length -gt 4000) { $text.Substring(0, 4000) } else { $text }
        hasError = [bool]($text -match '(?i)ERROR |not found|401|403')
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan.txt') -Dest (Join-Path $out 'todo-plan-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-plan-client.txt') -Dest (Join-Path $out 'todo-plan-client-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint.txt') -Dest (Join-Path $out 'todo-pluginint-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client.txt') -Dest (Join-Path $out 'todo-pluginint-client-done-extract.json')
Extract-Req -Path (Join-Path $out 'req-fr.txt') -Dest (Join-Path $out 'req-fr-extract.json') -Kind 'FR'
Extract-Req -Path (Join-Path $out 'req-tr.txt') -Dest (Join-Path $out 'req-tr-extract.json') -Kind 'TR'
Extract-Req -Path (Join-Path $out 'req-test.txt') -Dest (Join-Path $out 'req-test-extract.json') -Kind 'TEST'
Extract-Req -Path (Join-Path $out 'req-map.txt') -Dest (Join-Path $out 'req-map-extract.json') -Kind 'MAP'

function Extract-Session {
    param([string]$Path, [string]$Dest)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false; path = $Path } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    [ordered]@{
        exists = $true
        path = $Path
        length = (Get-Item -LiteralPath $Path).Length
        hasError = [bool]($text -match '(?i)^ERROR |method_not_found|401|turn_immutable')
        created = if ($text -match '(?m)^\s+created:\s+(true|false)') { $Matches[1] } else { $null }
        sessionId = if ($text -match '(?m)^\s+sessionId:\s+(\S+)') { $Matches[1] } else { $null }
        turnId = if ($text -match '(?m)^\s+turnId:\s+(\S+)') { $Matches[1] } else { $null }
        requestId = if ($text -match '(?m)^\s+requestId:\s+(\S+)') { $Matches[1] } else { $null }
        status = if ($text -match '(?m)^\s+status:\s+(\S+)') { $Matches[1] } else { $null }
        snippet = if ($text.Length -gt 2500) { $text.Substring(0, 2500) } else { $text }
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-Session -Path (Join-Path $out 'sl-open.txt') -Dest (Join-Path $out 'sl-open-extract.json')
Extract-Session -Path (Join-Path $out 'sl-begin.txt') -Dest (Join-Path $out 'sl-begin-extract.json')
Extract-Session -Path (Join-Path $out 'sl-query-sid.txt') -Dest (Join-Path $out 'sl-query-sid-extract.json')
Extract-Session -Path (Join-Path $out 'sl-query-history.txt') -Dest (Join-Path $out 'sl-query-history-extract.json')

Write-Output 'EXTRACT_DONE'
