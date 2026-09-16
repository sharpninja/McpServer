#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$names = @('testhost', 'dotnet', 'VBCSCompiler', 'pwsh')
$procs = @(Get-CimInstance Win32_Process | Where-Object {
    $n = [string]$_.Name
    $cmd = [string]$_.CommandLine
    ($n -match 'testhost|dotnet|VBCSCompiler|pwsh') -and (
        $cmd -match 'PluginIntegration' -or
        $cmd -match 'McpServer.PluginIntegration' -or
        $cmd -match '_hv-c-green-p15' -or
        $cmd -match '_hv-c-red-p15' -or
        $n -match 'testhost'
    )
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 400) { $cmd = $cmd.Substring(0, 400) }
    [ordered]@{
        ProcessId = $_.ProcessId
        ParentProcessId = $_.ParentProcessId
        Name = $_.Name
        CreationDate = [string]$_.CreationDate
        CommandLine = $cmd
    }
})
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = $procs.Count
    Processes = $procs
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'inspect-procs.json') -Encoding utf8
Write-Output ("PROC_COUNT=" + $procs.Count)
$procs | ForEach-Object { Write-Output ("PID=$($_.ProcessId) NAME=$($_.Name) CMD=$($_.CommandLine)") }
