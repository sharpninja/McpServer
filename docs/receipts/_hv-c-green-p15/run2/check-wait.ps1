#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
Start-Sleep -Seconds 20

Write-Output ('NOW=' + [DateTime]::UtcNow.ToString('o'))
Write-Output ('WAIT_PID_32996=' + [bool](Get-Process -Id 32996 -ErrorAction SilentlyContinue))
Write-Output ('WAIT_FINAL=' + (Test-Path -LiteralPath (Join-Path $out 'wait-leftover-final.json')))
Write-Output ('TESTS_PID_JSON=' + (Test-Path -LiteralPath (Join-Path $out 'hv-tests-run.pid.json')))
if (Test-Path -LiteralPath (Join-Path $out 'hv-tests-run.pid.json')) {
    Get-Content -LiteralPath (Join-Path $out 'hv-tests-run.pid.json') -Raw
}
if (Test-Path -LiteralPath (Join-Path $out 'wait-leftover-final.json')) {
    Get-Content -LiteralPath (Join-Path $out 'wait-leftover-final.json') -Raw
}
if (Test-Path -LiteralPath (Join-Path $out 'wait-leftover.jsonl')) {
    Write-Output '--- WAIT LAST ---'
    Get-Content -LiteralPath (Join-Path $out 'wait-leftover.jsonl') -Tail 5
}

$procs = @(Get-CimInstance Win32_Process | Where-Object {
    $cmd = [string]$_.CommandLine
    $cmd -match 'PluginIntegration|_hv-c-green-p15\\run2\\tests-run'
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 400) { $cmd = $cmd.Substring(0, 400) }
    'PID=' + $_.ProcessId + ' NAME=' + $_.Name + ' CMD=' + $cmd
})
Write-Output ('MATCH_COUNT=' + $procs.Count)
$procs | ForEach-Object { Write-Output $_ }
