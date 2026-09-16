#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$nukeResults = Join-Path $out ("results-nuke-$stamp")
New-Item -ItemType Directory -Force -Path $nukeResults | Out-Null
$nukeLog = Join-Path $out 'hv-dotnet-nuke-filter.log'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& $dotnet test 'tests/Build.Tests' -c Debug --filter 'FullyQualifiedName~NukeTarget_SkipIsFailure' --logger 'trx;LogFileName=nuke-skip.trx' --logger 'console;verbosity=detailed' --results-directory $nukeResults *>&1 | Tee-Object -FilePath $nukeLog
$exit = $LASTEXITCODE
$sw.Stop()
[ordered]@{
    Command = 'nuke-skip-filter'
    ExitCode = $exit
    DurationMs = $sw.ElapsedMilliseconds
    Log = $nukeLog
    ResultsDirectory = $nukeResults
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
    Dotnet = $dotnet
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-nuke-filter-exit.json') -Encoding utf8
Write-Output ("NUKE_EXIT=$exit DurationMs=$($sw.ElapsedMilliseconds)")
