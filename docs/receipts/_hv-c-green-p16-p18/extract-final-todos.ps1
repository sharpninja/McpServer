#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18'
function Extract-TodoDone {
    param([string]$Path, [string]$Dest)
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
        p16 = @($taskObjs | Where-Object { $_.task -match 'P16' })
        p17 = @($taskObjs | Where-Object { $_.task -match 'P17' })
        p18 = @($taskObjs | Where-Object { $_.task -match 'P18' })
        p19 = @($taskObjs | Where-Object { $_.task -match 'P19' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'C P16' })
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}
Extract-TodoDone -Path (Join-Path $out 'todo-plan-client-final.txt') -Dest (Join-Path $out 'todo-plan-final-done.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client-final.txt') -Dest (Join-Path $out 'todo-pluginint-final-done.json')
Write-Output ('UTC=' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
Write-Output 'EXTRACT_FINAL_DONE'
