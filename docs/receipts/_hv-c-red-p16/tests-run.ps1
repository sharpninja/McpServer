#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$transcript = Join-Path $out 'hv-tests-run.transcript.log'
try { Stop-Transcript | Out-Null } catch { }
Start-Transcript -Path $transcript -Force | Out-Null

Write-Output ("START_TESTS=" + [DateTime]::UtcNow.ToString('o'))

$waitStart = [DateTime]::UtcNow
$clear = $false
for ($i = 0; $i -lt 60; $i++) {
    $busy = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'testhost|vstest' -and $_.CommandLine -match 'PluginIntegration' })
    [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Iteration = $i
        BusyCount = $busy.Count
        Pids = @($busy | ForEach-Object { $_.ProcessId })
    } | ConvertTo-Json | Add-Content -LiteralPath (Join-Path $out 'wait-leftover.jsonl') -Encoding utf8
    if ($busy.Count -eq 0) {
        $clear = $true
        break
    }
    Start-Sleep -Seconds 5
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    WaitedSeconds = ([DateTime]::UtcNow - $waitStart).TotalSeconds
    MachineClear = $clear
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'wait-clear.json') -Encoding utf8
Write-Output ("MACHINE_CLEAR=$clear")

$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$swList = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests *>&1 | Tee-Object -FilePath $listLog
$listExit = $LASTEXITCODE
$swList.Stop()
$listText = Get-Content -LiteralPath $listLog -Raw
$listP16 = @([regex]::Matches($listText, 'AiTheory_Agent_RequiresValidJsonFields') | ForEach-Object { $_.Value })
$listP17 = @([regex]::Matches($listText, 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure') | ForEach-Object { $_.Value })
$listP18 = @([regex]::Matches($listText, 'NukeTarget_SkipIsFailure') | ForEach-Object { $_.Value })
$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$listHosts = foreach ($h in $hostKinds) {
    $pat = "AiTheory_Agent_RequiresValidJsonFields\($h\)"
    [ordered]@{ HostKind = $h; Count = ([regex]::Matches($listText, [regex]::Escape("AiTheory_Agent_RequiresValidJsonFields($h)"))).Count }
}
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests'
    ExitCode = $listExit
    DurationMs = $swList.ElapsedMilliseconds
    Log = $listLog
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
    ListAiTheoryRequiresValidJsonFields = $listP16.Count
    ListP17Rejects = $listP17.Count
    ListP18NukeSkip = $listP18.Count
    HostCounts = $listHosts
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-list-tests-exit.json') -Encoding utf8
Write-Output ("LIST_EXIT=$listExit P16=$($listP16.Count) P17=$($listP17.Count) P18=$($listP18.Count)")

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$resultsDir = Join-Path $out ("results-$stamp")
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$filterLog = Join-Path $out 'hv-dotnet-p16-filter.log'
$swFilter = [System.Diagnostics.Stopwatch]::StartNew()
$filterArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'FullyQualifiedName~AiTheory_Agent_RequiresValidJsonFields',
    '--logger', 'trx;LogFileName=p16-filter.trx',
    '--logger', 'console;verbosity=detailed',
    '--results-directory', $resultsDir
)
& dotnet @filterArgs *>&1 | Tee-Object -FilePath $filterLog
$filterExit = $LASTEXITCODE
$swFilter.Stop()
$trxFiles = @(Get-ChildItem -LiteralPath $resultsDir -Filter '*.trx' -Recurse -ErrorAction SilentlyContinue)
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~AiTheory_Agent_RequiresValidJsonFields'
    ExitCode = $filterExit
    DurationMs = $swFilter.ElapsedMilliseconds
    Log = $filterLog
    ResultsDirectory = $resultsDir
    TrxFiles = @($trxFiles | ForEach-Object { $_.FullName })
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-p16-filter-exit.json') -Encoding utf8
Write-Output ("FILTER_EXIT=$filterExit")
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'HV_TESTS_DONE'
try { Stop-Transcript | Out-Null } catch { }
