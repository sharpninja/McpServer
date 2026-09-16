#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$rows = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match 'testhost|vstest|dotnet' -and
    $null -ne $_.CommandLine -and
    $_.CommandLine -match 'PluginIntegration|McpServer.PluginIntegration'
})
$payload = foreach ($r in $rows) {
    $cmd = [string]$r.CommandLine
    if ($cmd.Length -gt 240) { $cmd = $cmd.Substring(0, 240) }
    [ordered]@{
        ProcessId = $r.ProcessId
        Name = $r.Name
        CommandLine = $cmd
    }
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = @($payload).Count
    Rows = @($payload)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'dotnet-procs.json') -Encoding utf8
Write-Output ("PLUGININT_PROCS=" + @($payload).Count)
