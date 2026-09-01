#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$implTerm = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\terminal'
Write-Output '===== implementer terminal files 10:10-10:25Z ====='
Get-ChildItem -LiteralPath $implTerm -File | Where-Object {
    $_.LastWriteTimeUtc -ge [datetime]'2026-08-21T10:10:00Z' -and $_.LastWriteTimeUtc -le [datetime]'2026-08-21T10:25:00Z'
} | Sort-Object LastWriteTimeUtc | ForEach-Object {
    Write-Output ($_.Name + ' Len=' + $_.Length + ' Utc=' + $_.LastWriteTimeUtc.ToString('o'))
}

Write-Output '===== gsudo hits in implementer terminal ====='
Select-String -Path (Join-Path $implTerm '*') -Pattern 'gsudo' -ErrorAction SilentlyContinue |
    ForEach-Object { Write-Output ($_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim()) }

Write-Output '===== Copy-Item ProgramData hits in implementer terminal 10:10-10:25 ====='
$files = Get-ChildItem -LiteralPath $implTerm -File | Where-Object {
    $_.LastWriteTimeUtc -ge [datetime]'2026-08-21T10:10:00Z' -and $_.LastWriteTimeUtc -le [datetime]'2026-08-21T10:25:00Z'
}
Select-String -Path $files.FullName -Pattern 'Copy-Item|ProgramData\\McpServer' -ErrorAction SilentlyContinue |
    Select-Object -First 40 |
    ForEach-Object { Write-Output ($_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim()) }

Write-Output '===== call-01f6458b head/tail ====='
$log = Join-Path $implTerm 'call-01f6458b-1de5-4dc8-90bc-0430f3dca317-162.log'
Write-Output ('LogLength=' + (Get-Item -LiteralPath $log).Length)
$lines = Get-Content -LiteralPath $log
Write-Output ('LineCount=' + $lines.Count)
Write-Output '--- FIRST 25 ---'
$lines | Select-Object -First 25 | ForEach-Object { Write-Output $_ }
Write-Output '--- LAST 25 ---'
$lines | Select-Object -Last 25 | ForEach-Object { Write-Output $_ }
Write-Output '--- gsudo/UpdateService/EXIT in this log ---'
Select-String -LiteralPath $log -Pattern 'gsudo|UpdateService|EXIT=|exit_code|Succeeded|Duration' |
    ForEach-Object { Write-Output ($_.LineNumber.ToString() + ':' + $_.Line.Trim()) }

Write-Output '===== python in implementer 10:10-10:25 ====='
Select-String -Path $files.FullName -Pattern '\bpython(3)?\b|\bpy\.exe\b' -ErrorAction SilentlyContinue |
    Select-Object -First 20 |
    ForEach-Object { Write-Output ($_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim()) }

Write-Output 'DONE'
