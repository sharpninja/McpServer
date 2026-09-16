#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p20'
$root = 'F:\GitHub\McpServer'
$results = Join-Path $out 'results-p20-green'
New-Item -ItemType Directory -Force -Path $out, $results | Out-Null
Set-Location -LiteralPath $root

$dllVsSource = Join-Path $out 'dll-vs-source.json'
$useNoBuild = $false
if (Test-Path -LiteralPath $dllVsSource) {
    $cmp = Get-Content -LiteralPath $dllVsSource -Raw | ConvertFrom-Json
    $useNoBuild = [bool]$cmp.dllAfterSource
}

$logger = 'trx;LogFileName=p20-green.trx'
$filter = 'FullyQualifiedName~PluginUpdateServiceHarnessTests'
$log = Join-Path $out 'hv-dotnet-p20-filter.log'
$args = @(
    'test', 'tests/McpServer.PluginIntegration.Tests',
    '-c', 'Debug',
    '--filter', $filter,
    '--logger', $logger,
    '--results-directory', $results
)
if ($useNoBuild) { $args += '--no-build' }

$sw = [Diagnostics.Stopwatch]::StartNew()
& dotnet @args 2>&1 | Tee-Object -FilePath $log | Out-Null
$exit = $LASTEXITCODE
$sw.Stop()

[ordered]@{
    command = 'dotnet ' + ($args -join ' ')
    usedNoBuild = $useNoBuild
    exitCode = $exit
    durationMs = $sw.ElapsedMilliseconds
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-p20-filter-exit.json') -Encoding utf8

if ($exit -ne 0 -and $useNoBuild) {
    $log2 = Join-Path $out 'hv-dotnet-p20-filter-rebuild.log'
    $results2 = Join-Path $out 'results-p20-green-rebuild'
    New-Item -ItemType Directory -Force -Path $results2 | Out-Null
    $args2 = @(
        'test', 'tests/McpServer.PluginIntegration.Tests',
        '-c', 'Debug',
        '-m:1',
        '--filter', $filter,
        '--logger', 'trx;LogFileName=p20-green-rebuild.trx',
        '--results-directory', $results2
    )
    $sw2 = [Diagnostics.Stopwatch]::StartNew()
    & dotnet @args2 2>&1 | Tee-Object -FilePath $log2 | Out-Null
    $exit2 = $LASTEXITCODE
    $sw2.Stop()
    [ordered]@{
        command = 'dotnet ' + ($args2 -join ' ')
        usedNoBuild = $false
        rebuilt = $true
        exitCode = $exit2
        durationMs = $sw2.ElapsedMilliseconds
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-p20-filter-rebuild-exit.json') -Encoding utf8
}

$buildProj = Join-Path $root 'build/_build.csproj'
if (-not (Test-Path -LiteralPath $buildProj)) {
    $cands = @(Get-ChildItem -LiteralPath (Join-Path $root 'build') -Filter '*.csproj' -File -ErrorAction SilentlyContinue)
    if ($cands.Count -eq 1) { $buildProj = $cands[0].FullName }
    elseif ($cands.Count -gt 1) {
        $hit = $cands | Where-Object { $_.Name -eq '_build.csproj' } | Select-Object -First 1
        if ($hit) { $buildProj = $hit.FullName } else { $buildProj = $cands[0].FullName }
    }
}
$blog = Join-Path $out 'hv-dotnet-build-pluginpromotion.log'
$bargs = @('build', $buildProj, '-c', 'Debug', '-m:1', '--nologo')
$bsw = [Diagnostics.Stopwatch]::StartNew()
& dotnet @bargs 2>&1 | Tee-Object -FilePath $blog | Out-Null
$bexit = $LASTEXITCODE
$bsw.Stop()
[ordered]@{
    command = 'dotnet ' + ($bargs -join ' ')
    exitCode = $bexit
    durationMs = $bsw.ElapsedMilliseconds
    project = $buildProj
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-build-pluginpromotion-exit.json') -Encoding utf8

Write-Output ("TEST_EXIT=$exit NOBUILD=$useNoBuild BUILD_EXIT=$bexit")
