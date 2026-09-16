#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Set-Location -LiteralPath 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_d4-20260822T141644Z'
[void][System.IO.Directory]::CreateDirectory($outDir)

function Get-FileSha256 {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Get-TrxCounts {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return $null
    }
    try {
        [xml]$xml = Get-Content -LiteralPath $Path
        $counters = $xml.TestRun.ResultSummary.Counters
        if (-not $counters) { return $null }
        $total = [int]$counters.total
        $executed = [int]$counters.executed
        $passed = [int]$counters.passed
        $failed = [int]$counters.failed
        $notExecuted = 0
        if ($counters.notExecuted) { $notExecuted = [int]$counters.notExecuted }
        $skippedNames = New-Object System.Collections.Generic.List[string]
        $results = @()
        if ($xml.TestRun.Results -and $xml.TestRun.Results.UnitTestResult) {
            $results = @($xml.TestRun.Results.UnitTestResult)
        }
        foreach ($r in $results) {
            $outcome = [string]$r.outcome
            if ($outcome -eq 'NotExecuted' -or $outcome -eq 'Skipped' -or $outcome -eq 'NotRunnable') {
                $skippedNames.Add([string]$r.testName) | Out-Null
            }
        }
        return [ordered]@{
            total        = $total
            executed     = $executed
            passed       = $passed
            failed       = $failed
            notExecuted  = $notExecuted
            skipped      = $notExecuted
            skippedNames = @($skippedNames)
        }
    } catch {
        return [ordered]@{
            parseError = $_.Exception.Message
        }
    }
}

function Get-ConsoleTestCounts {
    param([string]$Text)
    $matchesFound = [regex]::Matches($Text, '(Passed|Failed)!\s+- Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)')
    if ($matchesFound.Count -eq 0) { return $null }
    $failed = 0; $passed = 0; $skipped = 0; $total = 0
    foreach ($m in $matchesFound) {
        $failed += [int]$m.Groups[2].Value
        $passed += [int]$m.Groups[3].Value
        $skipped += [int]$m.Groups[4].Value
        $total += [int]$m.Groups[5].Value
    }
    return [ordered]@{ failed = $failed; passed = $passed; skipped = $skipped; total = $total; lines = $matchesFound.Count }
}

function Save-CommandResult {
    param(
        [Parameter(Mandatory)][int]$Index,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$ExactCommand,
        [Parameter(Mandatory)][string]$CapturedCommand,
        [Parameter(Mandatory)][int]$ExitCode,
        [Parameter(Mandatory)][string]$LogPath,
        [string]$TrxPath,
        [datetimeoffset]$Started,
        [datetimeoffset]$Ended,
        [object]$ConsoleCounts,
        [object]$TrxCounts
    )

    $passed = $null; $failed = $null; $skipped = $null; $total = $null
    $countSource = 'none'
    if ($TrxCounts -and $null -ne $TrxCounts.passed) {
        $passed = [int]$TrxCounts.passed
        $failed = [int]$TrxCounts.failed
        $skipped = [int]$TrxCounts.skipped
        $total = [int]$TrxCounts.total
        $countSource = 'trx'
    } elseif ($ConsoleCounts) {
        $passed = [int]$ConsoleCounts.passed
        $failed = [int]$ConsoleCounts.failed
        $skipped = [int]$ConsoleCounts.skipped
        $total = [int]$ConsoleCounts.total
        $countSource = 'console'
    } else {
        if ($ExitCode -eq 0) {
            $passed = 0; $failed = 0; $skipped = 0; $total = 0
            $countSource = 'exit-zero-no-tests'
        } else {
            $passed = 0; $failed = 1; $skipped = 0; $total = 0
            $countSource = 'exit-nonzero-no-tests'
        }
    }

    $obj = [ordered]@{
        index           = $Index
        name            = $Name
        exactCommand    = $ExactCommand
        capturedCommand = $CapturedCommand
        startedUtc      = $Started.UtcDateTime.ToString('o')
        endedUtc        = $Ended.UtcDateTime.ToString('o')
        durationSec     = [math]::Round(($Ended - $Started).TotalSeconds, 3)
        exitCode        = $ExitCode
        passed          = $passed
        failed          = $failed
        skipped         = $skipped
        total           = $total
        countSource     = $countSource
        logPath         = $LogPath
        logSha256       = Get-FileSha256 -Path $LogPath
        trxPath         = $TrxPath
        trxSha256       = if ($TrxPath) { Get-FileSha256 -Path $TrxPath } else { $null }
        consoleCounts   = $ConsoleCounts
        trxCounts       = $TrxCounts
        gateFailed      = ($failed -gt 0)
        gateSkipped     = ($skipped -gt 0)
        gateExitFail    = ($ExitCode -ne 0)
    }
    $obj | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outDir ("{0:00}-{1}.json" -f $Index, $Name)) -Encoding utf8
    return $obj
}

$gitBefore = & git status --porcelain=v1
$gitBefore | Set-Content -LiteralPath (Join-Path $outDir 'git-status-before.txt') -Encoding utf8

$commands = @(
    [ordered]@{
        index = 1; name = 'client-tests'; kind = 'dotnet-test'
        exact = 'dotnet test tests\McpServer.Client.Tests\McpServer.Client.Tests.csproj'
        file  = 'tests\McpServer.Client.Tests\McpServer.Client.Tests.csproj'
        trx   = '01-client-tests.trx'
    }
    [ordered]@{
        index = 2; name = 'support-mcp-tests'; kind = 'dotnet-test'
        exact = 'dotnet test tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
        file  = 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
        trx   = '02-support-mcp-tests.trx'
    }
    [ordered]@{
        index = 3; name = 'repl-core-tests'; kind = 'dotnet-test'
        exact = 'dotnet test tests\McpServer.Repl.Core.Tests\McpServer.Repl.Core.Tests.csproj'
        file  = 'tests\McpServer.Repl.Core.Tests\McpServer.Repl.Core.Tests.csproj'
        trx   = '03-repl-core-tests.trx'
    }
    [ordered]@{
        index = 4; name = 'support-mcp-integration'; kind = 'dotnet-test'
        exact = 'dotnet test tests\McpServer.Support.Mcp.IntegrationTests\McpServer.Support.Mcp.IntegrationTests.csproj'
        file  = 'tests\McpServer.Support.Mcp.IntegrationTests\McpServer.Support.Mcp.IntegrationTests.csproj'
        trx   = '04-support-mcp-integration.trx'
    }
    [ordered]@{
        index = 5; name = 'repl-integration'; kind = 'dotnet-test'
        exact = 'dotnet test tests\McpServer.Repl.IntegrationTests\McpServer.Repl.IntegrationTests.csproj'
        file  = 'tests\McpServer.Repl.IntegrationTests\McpServer.Repl.IntegrationTests.csproj'
        trx   = '05-repl-integration.trx'
    }
    [ordered]@{
        index = 6; name = 'compile'; kind = 'nuke'
        exact = '.\build.ps1 Compile'
        nuke  = 'Compile'
    }
    [ordered]@{
        index = 7; name = 'test'; kind = 'nuke'
        exact = '.\build.ps1 Test'
        nuke  = 'Test'
    }
    [ordered]@{
        index = 8; name = 'validate-traceability'; kind = 'nuke'
        exact = '.\build.ps1 ValidateTraceability'
        nuke  = 'ValidateTraceability'
    }
    [ordered]@{
        index = 9; name = 'sync-agent-plugins'; kind = 'nuke'
        exact = '.\build.ps1 SyncAgentPlugins'
        nuke  = 'SyncAgentPlugins'
    }
)

$results = New-Object System.Collections.Generic.List[object]
$stoppedReason = $null
$overallFailed = 0
$overallSkipped = 0
$allGreen = $true

foreach ($c in $commands) {
    $logPath = Join-Path $outDir ('{0:00}-{1}.log' -f $c.index, $c.name)
    $started = [DateTimeOffset]::UtcNow
    Write-Output ('START index=' + $c.index + ' name=' + $c.name + ' utc=' + $started.ToString('yyyyMMddTHHmmssZ'))

    $exitCode = 1
    if ($c.kind -eq 'dotnet-test') {
        $trxName = [string]$c.trx
        $captured = 'dotnet test ' + $c.file + ' --logger trx;LogFileName=' + $trxName + ' --results-directory ' + $outDir + ' --nologo'
        $argList = @(
            'test'
            [string]$c.file
            '--logger'
            ('trx;LogFileName=' + $trxName)
            '--results-directory'
            $outDir
            '--nologo'
        )
        $output = & dotnet @argList 2>&1 | ForEach-Object { $_.ToString() }
        $exitCode = $LASTEXITCODE
        $output | Set-Content -LiteralPath $logPath -Encoding utf8
        $trxPath = Join-Path $outDir $trxName
        $consoleCounts = Get-ConsoleTestCounts -Text ($output -join "`n")
        $trxCounts = Get-TrxCounts -Path $trxPath
        $rec = Save-CommandResult -Index $c.index -Name $c.name -ExactCommand $c.exact -CapturedCommand $captured -ExitCode $exitCode -LogPath $logPath -TrxPath $trxPath -Started $started -Ended ([DateTimeOffset]::UtcNow) -ConsoleCounts $consoleCounts -TrxCounts $trxCounts
    } else {
        $captured = 'pwsh.exe -NoProfile -NonInteractive -File .\build.ps1 ' + $c.nuke
        $output = & pwsh.exe -NoProfile -NonInteractive -File '.\build.ps1' $c.nuke 2>&1 | ForEach-Object { $_.ToString() }
        $exitCode = $LASTEXITCODE
        $output | Set-Content -LiteralPath $logPath -Encoding utf8
        $consoleCounts = Get-ConsoleTestCounts -Text ($output -join "`n")
        $trxPath = $null
        $trxCounts = $null
        if ($c.name -eq 'test') {
            $copied = Join-Path $outDir '07-nuke-test-trx'
            [void][System.IO.Directory]::CreateDirectory($copied)
            $cutoff = $started.UtcDateTime.AddSeconds(-2)
            if (Test-Path -LiteralPath 'F:\GitHub\McpServer\TestResults') {
                Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\TestResults' -Filter '*.trx' -File -ErrorAction SilentlyContinue |
                    Where-Object { $_.LastWriteTimeUtc -ge $cutoff } |
                    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $copied $_.Name) -Force }
            }
            $trxFiles = @(Get-ChildItem -LiteralPath $copied -Filter '*.trx' -File -ErrorAction SilentlyContinue)
            $sumPassed = 0; $sumFailed = 0; $sumSkipped = 0; $sumTotal = 0
            $per = @()
            foreach ($tf in $trxFiles) {
                $tc = Get-TrxCounts -Path $tf.FullName
                if ($tc -and $null -ne $tc.passed) {
                    $sumPassed += [int]$tc.passed
                    $sumFailed += [int]$tc.failed
                    $sumSkipped += [int]$tc.skipped
                    $sumTotal += [int]$tc.total
                    $per += [ordered]@{ file = $tf.Name; sha256 = (Get-FileSha256 $tf.FullName); counts = $tc }
                }
            }
            if ($trxFiles.Count -gt 0) {
                $trxCounts = [ordered]@{
                    passed = $sumPassed
                    failed = $sumFailed
                    skipped = $sumSkipped
                    total = $sumTotal
                    files = $per
                }
            }
        }
        $rec = Save-CommandResult -Index $c.index -Name $c.name -ExactCommand $c.exact -CapturedCommand $captured -ExitCode $exitCode -LogPath $logPath -TrxPath $trxPath -Started $started -Ended ([DateTimeOffset]::UtcNow) -ConsoleCounts $consoleCounts -TrxCounts $trxCounts
    }

    $results.Add($rec) | Out-Null
    Write-Output ('END index=' + $c.index + ' exit=' + $rec.exitCode + ' passed=' + $rec.passed + ' failed=' + $rec.failed + ' skipped=' + $rec.skipped)

    $overallFailed += [int]$rec.failed
    $overallSkipped += [int]$rec.skipped
    if ($rec.gateFailed -or $rec.gateSkipped -or $rec.gateExitFail) {
        $allGreen = $false
        $stoppedReason = ('stop after index {0} name {1}: exit={2} failed={3} skipped={4}' -f $c.index, $c.name, $rec.exitCode, $rec.failed, $rec.skipped)
        Write-Output ('STOP ' + $stoppedReason)
        break
    }
}

$gitAfter = & git status --porcelain=v1
$gitAfter | Set-Content -LiteralPath (Join-Path $outDir 'git-status-after.txt') -Encoding utf8

$summary = [ordered]@{
    timestampUtc  = [DateTimeOffset]::UtcNow.ToString('o')
    agent         = 'GrokCode'
    sessionId     = 'GrokCode-20260822T141644Z-pluginhandoff-d4-gate'
    requestId     = 'req-20260822T141644Z-001-pluginhandoff-d4-gate'
    todoId        = 'PLAN-PLUGINHANDOFF-001'
    plan          = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    phase         = 'D4'
    stoppedReason = $stoppedReason
    overall       = [ordered]@{
        failed      = $overallFailed
        skipped     = $overallSkipped
        allGreen    = [bool]($allGreen -and $overallFailed -eq 0 -and $overallSkipped -eq 0)
        commandsRun = $results.Count
    }
    commands      = @($results)
}

$summary | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath (Join-Path $outDir 'summary.json') -Encoding utf8
Write-Output ('OVERALL_FAILED=' + $overallFailed)
Write-Output ('OVERALL_SKIPPED=' + $overallSkipped)
Write-Output ('ALL_GREEN=' + $summary.overall.allGreen)
Write-Output ('STOPPED=' + $stoppedReason)
