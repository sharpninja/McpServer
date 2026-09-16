#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$testhosts = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match 'testhost|vstest|dotnet' -and $_.CommandLine -match 'PluginIntegration' })
$python = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match 'python|python3|py.exe' })

$thOut = foreach ($p in $testhosts) {
    [ordered]@{
        ProcessId = $p.ProcessId
        Name = $p.Name
        CreationDate = [string]$p.CreationDate
        CommandLine = $p.CommandLine
    }
}
$pyOut = foreach ($p in $python) {
    [ordered]@{
        ProcessId = $p.ProcessId
        Name = $p.Name
        CommandLine = $p.CommandLine
    }
}

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    PluginIntegrationProcessCount = $testhosts.Count
    PluginIntegrationProcesses = @($thOut)
    PythonProcessCount = $python.Count
    PythonProcesses = @($pyOut)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'inspect-procs.json') -Encoding utf8

Write-Output ("PLUGININT_PROCS=" + $testhosts.Count)
Write-Output ("PYTHON_PROCS=" + $python.Count)
