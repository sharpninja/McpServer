#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'

function Extract-TodoDone {
    param([string]$Path, [string]$Dest)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $topDone = $false
    if ($text -match '(?m)^\s+done:\s+(true|false)\s*$') { $topDone = ($Matches[1] -eq 'true') }
    $tasks = [regex]::Matches($text, '(?m)^\s+- task:\s*(.+?)\r?\n\s+done:\s+(true|false)')
    $taskObjs = foreach ($m in $tasks) {
        [pscustomobject]@{ task = $m.Groups[1].Value.Trim(); done = ($m.Groups[2].Value -eq 'true') }
    }
    [ordered]@{
        exists = $true
        topDone = $topDone
        taskCount = @($taskObjs).Count
        doneTrueCount = @($taskObjs | Where-Object { $_.done }).Count
        p14 = @($taskObjs | Where-Object { $_.task -match 'P14' })
        p15 = @($taskObjs | Where-Object { $_.task -match 'P15' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'C P14' })
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan.txt') -Dest (Join-Path $out 'todo-plan-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint.txt') -Dest (Join-Path $out 'todo-pluginint-done-extract.json')
Write-Output ([DateTime]::UtcNow.ToString('yyyy-MM-ddTHHmmssZ'))
