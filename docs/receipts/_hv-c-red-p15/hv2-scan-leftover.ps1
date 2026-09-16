#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
$items = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -and (
        $_.CommandLine -match 'PluginIntegration|mcp-pluginint-|testhost' -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'McpServer.Support.Mcp.dll')
    )
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 400) { $cmd = $cmd.Substring(0, 400) }
    [ordered]@{ ProcessId = $_.ProcessId; Name = $_.Name; CommandLine = $cmd }
})
$obj = [ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); Count = $items.Count; Items = $items }
$json = $obj | ConvertTo-Json -Depth 6
Set-Content -LiteralPath (Join-Path $out 'scan-leftover.json') -Value $json -Encoding utf8
Write-Output $json
