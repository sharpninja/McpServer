#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$sess = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7'
Write-Output '===== 01f6458b in events/updates/chat ====='
foreach ($name in @('events.jsonl','updates.jsonl','chat_history.jsonl','hunk_records.jsonl')) {
    $p = Join-Path $sess $name
    if (-not (Test-Path -LiteralPath $p)) { Write-Output ("MISSING " + $name); continue }
    Write-Output ('FILE=' + $name + ' Len=' + (Get-Item -LiteralPath $p).Length)
    $hits = Select-String -LiteralPath $p -Pattern '01f6458b|run-update-service|gsudo pwsh' -SimpleMatch:$false
    if ($hits) {
        $hits | Select-Object -First 30 | ForEach-Object {
            $line = $_.Line
            if ($line.Length -gt 500) { $line = $line.Substring(0, 500) }
            Write-Output ($name + ':' + $_.LineNumber + ':' + $line)
        }
    } else {
        Write-Output ($name + ': NO_HIT')
    }
}

Write-Output '===== terminal logs containing run-update-service ====='
$term = Join-Path $sess 'terminal'
Select-String -Path (Join-Path $term '*.log') -Pattern 'run-update-service|ExecutionPolicy Bypass -File' -ErrorAction SilentlyContinue |
    Select-Object -First 40 |
    ForEach-Object {
        $line = $_.Line
        if ($line.Length -gt 400) { $line = $line.Substring(0, 400) }
        Write-Output ($_.Filename + ':' + $_.LineNumber + ':' + $line)
    }

Write-Output '===== JSON sidecars near 01f6458b ====='
Get-ChildItem -LiteralPath $sess -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like '*01f6458b*' } |
    ForEach-Object { Write-Output ($_.FullName + ' ' + $_.Length) }

Write-Output '===== last 5 implementer terminal files by time ====='
Get-ChildItem -LiteralPath $term -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 15 | ForEach-Object {
    Write-Output ($_.LastWriteTimeUtc.ToString('o') + ' ' + $_.Name + ' ' + $_.Length)
}

Write-Output 'DONE'
