#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
$rows = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match 'testhost|vstest|dotnet|pwsh' -and
    $null -ne $_.CommandLine -and
    ($_.CommandLine -match 'PluginIntegration|McpServer.PluginIntegration|_hv-c-green-p14|p14-green|wait-then-tests|hv-tests-run|tests.ps1')
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 300) { $cmd = $cmd.Substring(0, 300) }
    [pscustomobject]@{ Pid = $_.ProcessId; Name = $_.Name; Cmd = $cmd }
})
Write-Output ('COUNT=' + @($rows).Count)
foreach ($r in $rows) {
    Write-Output ($r.Pid.ToString() + '|' + $r.Name + '|' + $r.Cmd)
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = @($rows).Count
    Rows = @($rows)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'hv-procs-now.json') -Encoding utf8

Get-ChildItem -LiteralPath $out -File | Where-Object {
    $_.Name -match 'dotnet|trx|wait-p14|hv-dotnet|filter|list-tests'
} | Sort-Object Name | ForEach-Object {
    Write-Output ($_.Name + ' len=' + $_.Length + ' utc=' + $_.LastWriteTimeUtc.ToString('o'))
}
