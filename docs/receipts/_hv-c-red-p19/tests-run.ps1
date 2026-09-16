#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p19'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$transcript = Join-Path $out 'hv-tests-run.transcript.log'
try { Stop-Transcript | Out-Null } catch { }
Start-Transcript -Path $transcript -Force | Out-Null

$dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
Write-Output ("START_TESTS=" + [DateTime]::UtcNow.ToString('o'))
Write-Output ("DOTNET=$dotnet")

$waitStart = [DateTime]::UtcNow
$clear = $false
for ($i = 0; $i -lt 90; $i++) {
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
    DotnetPath = $dotnet
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'wait-clear.json') -Encoding utf8
Write-Output ("MACHINE_CLEAR=$clear")

function Invoke-DotnetTest {
    param(
        [string[]]$DotnetArgs,
        [string]$Log,
        [string]$ExitJson,
        [string]$CommandLabel
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $dotnet @DotnetArgs *>&1 | Tee-Object -FilePath $Log
    $exit = $LASTEXITCODE
    $sw.Stop()
    [ordered]@{
        Command = $CommandLabel
        ExitCode = $exit
        DurationMs = $sw.ElapsedMilliseconds
        Log = $Log
        FinishedUtc = [DateTime]::UtcNow.ToString('o')
        Dotnet = $dotnet
        DotnetArgs = $DotnetArgs
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ExitJson -Encoding utf8
    Write-Output ("EXIT $CommandLabel $exit DurationMs=$($sw.ElapsedMilliseconds)")
    return $exit
}

function Count-ListedNames {
    param([string]$LogPath)
    if (-not (Test-Path -LiteralPath $LogPath)) { return 0 }
    $lines = Get-Content -LiteralPath $LogPath
    $inList = $false
    $n = 0
    $names = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $lines) {
        if ($line -match 'The following Tests are available') { $inList = $true; continue }
        if ($inList) {
            if ($line -match '^\s*$') { continue }
            if ($line -match '^(Passed|Failed|Total tests|Test Run|Starting test)') { break }
            $trim = $line.Trim()
            if ($trim.Length -gt 0) {
                $n++
                $names.Add($trim)
            }
        }
    }
    return [pscustomobject]@{ Count = $n; Names = @($names) }
}

function Get-ConsoleCount {
    param([string]$Text, [string]$Label)
    $escaped = [regex]::Escape($Label)
    $m1 = [regex]::Match($Text, '(?m)^' + $escaped + ':\s+(\d+)')
    if ($m1.Success) { return [int]$m1.Groups[1].Value }
    $m2 = [regex]::Match($Text, $escaped + '!\s*:\s*(\d+)')
    if ($m2.Success) { return [int]$m2.Groups[1].Value }
    return $null
}

$listLog = Join-Path $out 'hv-dotnet-list-p19.log'
$listExit = Invoke-DotnetTest -DotnetArgs @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--list-tests',
    '--filter', 'FullyQualifiedName~PluginNativeSuiteReceiptTests'
) -Log $listLog -ExitJson (Join-Path $out 'hv-dotnet-list-p19-exit.json') -CommandLabel 'list-p19'

$listAll = Count-ListedNames -LogPath $listLog
$listText = if (Test-Path -LiteralPath $listLog) { Get-Content -LiteralPath $listLog -Raw } else { '' }
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    ListExit = $listExit
    ListedCount = $listAll.Count
    ListedNames = $listAll.Names
    HasEach = [bool]($listText -match 'PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero')
    HasAfterSync = [bool]($listText -match 'PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero')
    HasGit = [bool]($listText -match 'PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit')
    HasP20Harness = [bool]($listText -match 'PluginSessionLogHarness_AgainstUpdateService')
    HasP20Promotion = [bool]($listText -match 'PluginPromotion_StagingOrProduction')
    EachCount = ([regex]::Matches($listText, 'PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero')).Count
    AfterSyncCount = ([regex]::Matches($listText, 'PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero')).Count
    GitCount = ([regex]::Matches($listText, 'PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit')).Count
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$results = Join-Path $out ("results-p19-$stamp")
New-Item -ItemType Directory -Force -Path $results | Out-Null
$runLog = Join-Path $out 'hv-dotnet-p19-filter.log'
$runExit = Invoke-DotnetTest -DotnetArgs @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'FullyQualifiedName~PluginNativeSuiteReceiptTests',
    '--logger', 'trx;LogFileName=p19-red.trx',
    '--logger', 'console;verbosity=detailed',
    '--results-directory', $results
) -Log $runLog -ExitJson (Join-Path $out 'hv-dotnet-p19-filter-exit.json') -CommandLabel 'pluginint-p19-filter'

$runText = if (Test-Path -LiteralPath $runLog) { Get-Content -LiteralPath $runLog -Raw } else { '' }
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    ExitCode = $runExit
    ConsoleTotal = Get-ConsoleCount $runText 'Total tests'
    ConsolePassed = Get-ConsoleCount $runText 'Passed'
    ConsoleFailed = Get-ConsoleCount $runText 'Failed'
    ConsoleSkipped = Get-ConsoleCount $runText 'Skipped'
    ContainsFileNotFound = [bool]($runText -match 'No docs/receipts/pluginint-p19-<utc> receipt directory exists')
    FileNotFoundCount = ([regex]::Matches($runText, 'No docs/receipts/pluginint-p19-<utc> receipt directory exists')).Count
    ContainsSkip = [bool]($runText -match '(?i)skipped')
    ResultsDirectory = $results
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-p19-filter-console.json') -Encoding utf8

Get-ChildItem -LiteralPath $out -Recurse -Filter '*.trx' | ForEach-Object { $_.FullName } |
    Set-Content -LiteralPath (Join-Path $out 'trx-files.txt') -Encoding utf8

Write-Output ("P19_EXIT=$runExit LIST_EXIT=$listExit")
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'HV_TESTS_DONE'
try { Stop-Transcript | Out-Null } catch { }
