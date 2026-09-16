#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p7'
$now = [DateTime]::UtcNow
$utc = $now.ToString('yyyyMMddTHHmmssZ')
$iso = $now.ToString('o')
Set-Content -LiteralPath (Join-Path $out 'receipt-stamp.txt') -Value $utc -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'receipt-iso.txt') -Value $iso -Encoding utf8
Write-Output ("RECEIPT_UTC=$utc")
Write-Output ("RECEIPT_ISO=$iso")

$files = @(
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapterTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\FakePluginProcessRunner.cs'
)
$hits = @()
foreach ($f in $files) {
    $t = Get-Content -LiteralPath $f -Raw
    if ($t.Contains([char]0x2014) -or $t.Contains([char]0x2013)) {
        $hits += $f
    }
}
[ordered]@{ emDashFiles = $hits; count = $hits.Count } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'emdash-scan.json') -Encoding utf8
$skipCsproj = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj' -Pattern 'Skip' -SimpleMatch)
Write-Output ("CSPROJ_SKIP=$($skipCsproj.Count)")
Write-Output ("EMDASH_COUNT=$($hits.Count)")
