#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16\hv-self'
$root = 'F:\GitHub\McpServer'
$trxDir = Join-Path $out 'trx-p16-hv'
New-Item -ItemType Directory -Force -Path $out | Out-Null
New-Item -ItemType Directory -Force -Path $trxDir | Out-Null
Set-Location -LiteralPath $root

$transcript = Join-Path $out 'hv-tests-run.transcript.log'
try { Stop-Transcript | Out-Null } catch { }
Start-Transcript -LiteralPath $transcript -Force | Out-Null
Write-Output ("START=" + [DateTime]::UtcNow.ToString('o'))

function Save-Exit([string]$Name, [int]$Code, [datetime]$Started, [datetime]$Ended) {
    [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Name = $Name
        ExitCode = $Code
        StartedUtc = $Started.ToUniversalTime().ToString('o')
        EndedUtc = $Ended.ToUniversalTime().ToString('o')
        DurationMs = [int]($Ended - $Started).TotalMilliseconds
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out ($Name + '-exit.json')) -Encoding utf8
}

$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$listStarted = Get-Date
& dotnet test 'tests/McpServer.PluginIntegration.Tests' -c Debug --list-tests --nologo *>&1 |
    Tee-Object -FilePath $listLog | Out-Null
$listCode = $LASTEXITCODE
$listEnded = Get-Date
Save-Exit -Name 'hv-dotnet-list-tests' -Code $listCode -Started $listStarted -Ended $listEnded

$filterLog = Join-Path $out 'hv-dotnet-p16-filter.log'
$filterStarted = Get-Date
& dotnet test 'tests/McpServer.PluginIntegration.Tests' -c Debug --filter 'FullyQualifiedName~AiTheory_Agent_RequiresValidJsonFields' --logger 'trx;LogFileName=p16-filter.trx' --logger 'console;verbosity=detailed' --results-directory $trxDir --nologo *>&1 |
    Tee-Object -FilePath $filterLog | Out-Null
$filterCode = $LASTEXITCODE
$filterEnded = Get-Date
Save-Exit -Name 'hv-dotnet-p16-filter' -Code $filterCode -Started $filterStarted -Ended $filterEnded

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Owner = 'GrokSubagentHostile-20260822T072417Z-c-red-p16'
    ListExitCode = $listCode
    FilterExitCode = $filterCode
    FilterStartedUtc = $filterStarted.ToUniversalTime().ToString('o')
    FilterEndedUtc = $filterEnded.ToUniversalTime().ToString('o')
    FilterDurationMs = [int]($filterEnded - $filterStarted).TotalMilliseconds
    TrxDir = $trxDir
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-tests-done.json') -Encoding utf8

Write-Output 'TESTS_DONE'
try { Stop-Transcript | Out-Null } catch { }
