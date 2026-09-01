# Re-run Build.Tests SyncAgentPluginsVendorNameTests with restore. Write summary only.
$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer'
$outDir = Join-Path $root 'docs\receipts\_hv-g11-out'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$proj = Join-Path $root 'tests\Build.Tests\Build.Tests.csproj'
$log = Join-Path $outDir 'dotnet-test.log'
$trx = Join-Path $outDir 'SyncAgentPluginsVendorNameTests.trx'

$out = & dotnet test $proj -c Debug --filter 'FullyQualifiedName~SyncAgentPluginsVendorNameTests' --logger "trx;LogFileName=SyncAgentPluginsVendorNameTests.trx" --results-directory $outDir 2>&1
$exit = $LASTEXITCODE
$out | Set-Content -LiteralPath $log -Encoding utf8
$text = $out | Out-String

$summary = [ordered]@{
    ExitCode = $exit
    Failed = $null
    Skipped = $null
    Passed = $null
    Total = $null
    Filter = 'FullyQualifiedName~SyncAgentPluginsVendorNameTests'
    Project = $proj
}
if ($text -match 'Failed:\s+(\d+)') { $summary.Failed = [int]$Matches[1] }
if ($text -match 'Skipped:\s+(\d+)') { $summary.Skipped = [int]$Matches[1] }
if ($text -match 'Passed:\s+(\d+)') { $summary.Passed = [int]$Matches[1] }
if ($text -match 'Total:\s+(\d+)') { $summary.Total = [int]$Matches[1] }

# Also parse trx if present
$trxFiles = @(Get-ChildItem -LiteralPath $outDir -Filter '*.trx' -Recurse -ErrorAction SilentlyContinue)
$summary.TrxFiles = @($trxFiles | ForEach-Object { $_.FullName })

($summary | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath (Join-Path $outDir 'dotnet-test-summary.json') -Encoding utf8
Write-Output ($summary | ConvertTo-Json -Depth 5)
Write-Output '---TAIL---'
$text.Substring([Math]::Max(0, $text.Length - 4000))
