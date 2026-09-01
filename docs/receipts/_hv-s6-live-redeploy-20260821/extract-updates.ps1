#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-Prop {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    $p = $Object.PSObject.Properties[$Name]
    if ($null -eq $p) { return $null }
    return $p.Value
}

$updates = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\updates.jsonl'
Write-Output '===== 01f6458b in updates.jsonl ====='
$hits = Select-String -LiteralPath $updates -Pattern '01f6458b'
Write-Output ('HIT_COUNT=' + @($hits).Count)
$n = 0
foreach ($h in $hits) {
    $n++
    Write-Output ('--- hit ' + $n + ' line ' + $h.LineNumber + ' len=' + $h.Line.Length + ' ---')
    $previewLen = [Math]::Min(2000, $h.Line.Length)
    Write-Output $h.Line.Substring(0, $previewLen)
}

Write-Output '===== run-update-service in updates.jsonl ====='
$hits2 = Select-String -LiteralPath $updates -Pattern 'run-update-service'
Write-Output ('HIT_COUNT=' + @($hits2).Count)
$n = 0
foreach ($h in $hits2) {
    $n++
    if ($n -gt 20) { Write-Output 'TRUNCATED'; break }
    Write-Output ('--- hit ' + $n + ' line ' + $h.LineNumber + ' len=' + $h.Line.Length + ' ---')
    $previewLen = [Math]::Min(2000, $h.Line.Length)
    Write-Output $h.Line.Substring(0, $previewLen)
}

$chat = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\chat_history.jsonl'
Write-Output '===== 01f6458b in chat_history ====='
$hits3 = Select-String -LiteralPath $chat -Pattern '01f6458b|run-update-service'
Write-Output ('HIT_COUNT=' + @($hits3).Count)
$n = 0
foreach ($h in $hits3) {
    $n++
    if ($n -gt 15) { Write-Output 'TRUNCATED'; break }
    Write-Output ('--- hit ' + $n + ' line ' + $h.LineNumber + ' len=' + $h.Line.Length + ' ---')
    $previewLen = [Math]::Min(2000, $h.Line.Length)
    Write-Output $h.Line.Substring(0, $previewLen)
}
