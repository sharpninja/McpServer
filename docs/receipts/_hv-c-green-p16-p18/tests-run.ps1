#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18'
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

$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$listExit = Invoke-DotnetTest -DotnetArgs @('test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests') -Log $listLog -ExitJson (Join-Path $out 'hv-dotnet-list-tests-exit.json') -CommandLabel 'list-pluginint'
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
    ListedFqnApprox = @([regex]::Matches($listText, '(?m)^\s{2,}\S+\.\S+')).Count
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$aiResults = Join-Path $out ("results-ai-$stamp")
New-Item -ItemType Directory -Force -Path $aiResults | Out-Null
$aiLog = Join-Path $out 'hv-dotnet-ai-filter.log'
$aiExit = Invoke-DotnetTest -DotnetArgs @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'FullyQualifiedName~PluginSessionLogAiTheoryTests',
    '--logger', 'trx;LogFileName=ai-theory.trx',
    '--logger', 'console;verbosity=detailed',
    '--results-directory', $aiResults
) -Log $aiLog -ExitJson (Join-Path $out 'hv-dotnet-ai-filter-exit.json') -CommandLabel 'ai-theory-filter'

$nukeResults = Join-Path $out ("results-nuke-$stamp")
New-Item -ItemType Directory -Force -Path $nukeResults | Out-Null
$nukeLog = Join-Path $out 'hv-dotnet-nuke-filter.log'
$nukeExit = Invoke-DotnetTest -DotnetArgs @(
    'test', 'tests/Build.Tests', '-c', 'Debug',
    '--filter', 'FullyQualifiedName~NukeTarget_SkipIsFailure',
    '--logger', 'trx;LogFileName=nuke-skip.trx',
    '--logger', 'console;verbosity=detailed',
    '--results-directory', $nukeResults
) -Log $nukeLog -ExitJson (Join-Path $out 'hv-dotnet-nuke-filter-exit.json') -CommandLabel 'nuke-skip-filter'

$fullResults = Join-Path $out ("results-full-$stamp")
New-Item -ItemType Directory -Force -Path $fullResults | Out-Null
$fullLog = Join-Path $out 'hv-dotnet-pluginint-all.log'
$fullExit = Invoke-DotnetTest -DotnetArgs @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--logger', 'trx;LogFileName=pluginint-all.trx',
    '--logger', 'console;verbosity=detailed',
    '--results-directory', $fullResults
) -Log $fullLog -ExitJson (Join-Path $out 'hv-dotnet-pluginint-all-exit.json') -CommandLabel 'pluginint-all'

Get-ChildItem -LiteralPath $out -Recurse -Filter '*.trx' | ForEach-Object { $_.FullName } |
    Set-Content -LiteralPath (Join-Path $out 'trx-files.txt') -Encoding utf8

Write-Output ("AI_EXIT=$aiExit NUKE_EXIT=$nukeExit FULL_EXIT=$fullExit")
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'HV_TESTS_DONE'
try { Stop-Transcript | Out-Null } catch { }
