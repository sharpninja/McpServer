#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
Write-Output ("WAIT_START=" + [DateTime]::UtcNow.ToString('o'))

$waitStart = [DateTime]::UtcNow
$clear = $false
$leftoverSnapshot = @()
for ($i = 0; $i -lt 48; $i++) {
    $busy = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and ($_.CommandLine -match 'McpServer\.PluginIntegration\.Tests') -and ($_.Name -match 'testhost|vstest|dotnet') })
    $leftoverSnapshot = @($busy | ForEach-Object {
        [ordered]@{ Pid = $_.ProcessId; Name = $_.Name; CommandLine = $_.CommandLine }
    })
    [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Iteration = $i
        BusyCount = $busy.Count
        Pids = @($busy | ForEach-Object { $_.ProcessId })
    } | ConvertTo-Json | Add-Content -LiteralPath (Join-Path $out 'wait-leftover-r2.jsonl') -Encoding utf8
    if ($busy.Count -eq 0) {
        $clear = $true
        break
    }
    Start-Sleep -Seconds 15
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    WaitedSeconds = ([DateTime]::UtcNow - $waitStart).TotalSeconds
    MachineClear = $clear
    LastLeftover = $leftoverSnapshot
    DotnetPath = $dotnet
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'wait-clear.json') -Encoding utf8
Write-Output ("MACHINE_CLEAR=$clear WaitedSeconds=$([int]([DateTime]::UtcNow - $waitStart).TotalSeconds)")

if (-not $clear) {
    Write-Output 'LEFTOVER_STILL_RUNNING'
    exit 2
}

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

$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$listExit = Invoke-DotnetTest -DotnetArgs @('test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests') -Log $listLog -ExitJson (Join-Path $out 'hv-dotnet-list-tests-exit.json') -CommandLabel 'list-pluginint'
$listAiLog = Join-Path $out 'hv-dotnet-list-ai.log'
$listAiExit = Invoke-DotnetTest -DotnetArgs @('test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests', '--filter', 'PluginInt=AI') -Log $listAiLog -ExitJson (Join-Path $out 'hv-dotnet-list-ai-exit.json') -CommandLabel 'list-pluginint-ai'
$listDetLog = Join-Path $out 'hv-dotnet-list-det.log'
$listDetExit = Invoke-DotnetTest -DotnetArgs @('test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests', '--filter', 'PluginInt=Deterministic') -Log $listDetLog -ExitJson (Join-Path $out 'hv-dotnet-list-det-exit.json') -CommandLabel 'list-pluginint-det'

function Count-ListedNames {
    param([string]$LogPath)
    if (-not (Test-Path -LiteralPath $LogPath)) { return 0 }
    $lines = Get-Content -LiteralPath $LogPath
    $inList = $false
    $n = 0
    foreach ($line in $lines) {
        if ($line -match 'The following Tests are available') { $inList = $true; continue }
        if ($inList) {
            if ($line -match '^\s*$') { continue }
            if ($line -match '^(Passed|Failed|Total tests|Test Run|Starting test|A total of)') { break }
            if ($line.Trim().Length -gt 0) { $n++ }
        }
    }
    return $n
}

$listText = if (Test-Path -LiteralPath $listLog) { Get-Content -LiteralPath $listLog -Raw } else { '' }
$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$listHosts = foreach ($h in $hostKinds) {
    [ordered]@{ HostKind = $h; Count = ([regex]::Matches($listText, [regex]::Escape("AiTheory_Agent_RequiresValidJsonFields($h)"))).Count }
}
[ordered]@{
    ListExit = $listExit
    ListAiTheoryRequiresValidJsonFields = ([regex]::Matches($listText, 'AiTheory_Agent_RequiresValidJsonFields')).Count
    ListP17Rejects = ([regex]::Matches($listText, 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure')).Count
    ListP18NukeSkip = ([regex]::Matches($listText, 'NukeTarget_SkipIsFailure')).Count
    ListP19Native = ([regex]::Matches($listText, 'PluginNativeSuite_|PluginInt_P19_')).Count
    ListAiTheoryClass = ([regex]::Matches($listText, 'PluginSessionLogAiTheoryTests')).Count
    HostCounts = $listHosts
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    ListAllExit = $listExit
    ListAiExit = $listAiExit
    ListDetExit = $listDetExit
    ListedAllApprox = (Count-ListedNames -LogPath $listLog)
    ListedAiApprox = (Count-ListedNames -LogPath $listAiLog)
    ListedDetApprox = (Count-ListedNames -LogPath $listDetLog)
    ListedAiP16 = ([regex]::Matches((Get-Content -LiteralPath $listAiLog -Raw), 'AiTheory_Agent_RequiresValidJsonFields')).Count
    ListedAiP17 = ([regex]::Matches((Get-Content -LiteralPath $listAiLog -Raw), 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure')).Count
    ListedAiP19 = ([regex]::Matches((Get-Content -LiteralPath $listAiLog -Raw), 'PluginNativeSuite_|PluginInt_P19_')).Count
    ListedDetP19 = ([regex]::Matches((Get-Content -LiteralPath $listDetLog -Raw), 'PluginNativeSuite_|PluginInt_P19_')).Count
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'list-trait-split.json') -Encoding utf8

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$aiResults = Join-Path $out ("results-ai-$stamp")
New-Item -ItemType Directory -Force -Path $aiResults | Out-Null
$aiLog = Join-Path $out 'hv-dotnet-ai-filter.log'
$aiExit = Invoke-DotnetTest -DotnetArgs @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'PluginInt=AI',
    '--logger', 'trx;LogFileName=ai-theory.trx',
    '--logger', 'console;verbosity=detailed',
    '--results-directory', $aiResults
) -Log $aiLog -ExitJson (Join-Path $out 'hv-dotnet-ai-filter-exit.json') -CommandLabel 'pluginint-ai-filter'

Get-ChildItem -LiteralPath $out -Recurse -Filter '*.trx' | ForEach-Object { $_.FullName } |
    Set-Content -LiteralPath (Join-Path $out 'trx-files.txt') -Encoding utf8
Write-Output ("AI_EXIT=$aiExit LIST_ALL=$listExit LIST_AI=$listAiExit LIST_DET=$listDetExit")
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'HV_AI_DONE'
exit 0
