#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
$pidPath = Join-Path $out 'hv-tests-run.pid.json'
$wmiPid = $null
if (Test-Path $pidPath) {
    $pidInfo = Get-Content -LiteralPath $pidPath -Raw | ConvertFrom-Json
    $wmiPid = [int]$pidInfo.ProcessId
}

$procs = @(Get-CimInstance Win32_Process | Where-Object {
    ($null -ne $wmiPid -and ($_.ProcessId -eq $wmiPid -or $_.ParentProcessId -eq $wmiPid)) -or
    ($_.CommandLine -and ($_.CommandLine -match 'PluginIntegration' -or $_.CommandLine -match '_hv-c-red-p16' -or $_.CommandLine -match 'testhost'))
} | ForEach-Object {
    [ordered]@{
        Pid = $_.ProcessId
        Parent = $_.ParentProcessId
        Name = $_.Name
        CommandLine = $_.CommandLine
    }
})
$procs | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'inspect-now.json') -Encoding utf8
Write-Output ('PROC_COUNT=' + @($procs).Count)
$procs | ForEach-Object { Write-Output ("PID=$($_.Pid) PARENT=$($_.Parent) NAME=$($_.Name)") }

Write-Output '---FILES---'
Get-ChildItem -LiteralPath $out -File | Sort-Object LastWriteTimeUtc | ForEach-Object {
    Write-Output ("{0} {1} {2}" -f $_.LastWriteTimeUtc.ToString('o'), $_.Length, $_.Name)
}

Write-Output ('DONE_EXISTS=' + (Test-Path (Join-Path $out 'hv-tests-done.json')))
Write-Output ('LIST_EXISTS=' + (Test-Path (Join-Path $out 'hv-dotnet-list-tests.log')))
Write-Output ('FILTER_EXISTS=' + (Test-Path (Join-Path $out 'hv-dotnet-p16-filter.log')))
Write-Output ('TRANSCRIPT_EXISTS=' + (Test-Path (Join-Path $out 'hv-tests-run.transcript.log')))

if (Test-Path (Join-Path $out 'hv-dotnet-list-tests.log')) {
    Write-Output '---LIST_TAIL---'
    Get-Content -LiteralPath (Join-Path $out 'hv-dotnet-list-tests.log') -Tail 50
}
if (Test-Path (Join-Path $out 'hv-dotnet-p16-filter.log')) {
    Write-Output '---FILTER_TAIL---'
    Get-Content -LiteralPath (Join-Path $out 'hv-dotnet-p16-filter.log') -Tail 50
}
if (Test-Path (Join-Path $out 'hv-tests-run.transcript.log')) {
    Write-Output '---TRANSCRIPT_TAIL---'
    Get-Content -LiteralPath (Join-Path $out 'hv-tests-run.transcript.log') -Tail 80
}
