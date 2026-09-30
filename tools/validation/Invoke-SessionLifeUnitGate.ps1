#Requires -Version 7.0
<#
.SYNOPSIS
    FR-MCP-107 / TR-MCP-PLAN-001: Orchestrates the session-life cumulative unit gate.
.DESCRIPTION
    Writes the pre-run source manifest, runs the whole Pester directory, runs Nuke Test
    with the same run id, runs Build.Tests outside the Nuke inventory, persists Pester
    native counters, and validates the artifacts through ValidateSessionLifeUnitGate.
    The selected project list comes from Nuke's selected-projects.json.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($RunId -match '[\\/:]' -or $RunId.Contains('..')) {
    throw "RunId must be a single path segment."
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).ProviderPath
$dotnetHome = Join-Path $HOME '.dotnet'
$dotnetTools = Join-Path $dotnetHome 'tools'
if (Test-Path -LiteralPath (Join-Path $dotnetHome 'dotnet')) {
    $env:DOTNET_ROOT = $dotnetHome
}
foreach ($candidate in @($dotnetTools, $dotnetHome)) {
    if ((Test-Path -LiteralPath $candidate) -and (($env:PATH -split [IO.Path]::PathSeparator) -notcontains $candidate)) {
        $env:PATH = "$candidate$([IO.Path]::PathSeparator)$env:PATH"
    }
}
$cleanTemp = Join-Path $env:LOCALAPPDATA 'Temp'
if (Test-Path -LiteralPath $cleanTemp) {
    $env:TEMP = $cleanTemp
    $env:TMP = $cleanTemp
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

$stagedRoot = Join-Path $repoRoot 'plugins\core\.staged-plugin'
$syncScript = Join-Path $repoRoot 'plugins\core\sync\sync-plugin-core.ps1'
$wrapperScript = Join-Path $repoRoot 'plugins\core\hooks-templates\generate-wrappers.ps1'
if (-not (Test-Path -LiteralPath (Join-Path $stagedRoot 'lib\plugin-hook.ps1'))) {
    New-Item -ItemType Directory -Force -Path $stagedRoot | Out-Null
    & pwsh -NoLogo -NoProfile -NonInteractive -File $syncScript -PluginRoot $stagedRoot
    if ($LASTEXITCODE -ne 0) { throw "sync-plugin-core failed with exit $LASTEXITCODE" }
    & pwsh -NoLogo -NoProfile -NonInteractive -File $wrapperScript -HostName 'claude-code' -PluginRoot $stagedRoot
    if ($LASTEXITCODE -ne 0) { throw "generate-wrappers failed with exit $LASTEXITCODE" }
}

# Copy sibling plugin skills so Pester contracts that read
# .staged-plugin/skills/{session,triage}/SKILL.md can resolve.
$skillCandidates = @(
    (Join-Path (Split-Path $repoRoot -Parent) 'mcpserver-claude-code-plugin\skills'),
    'F:\GitHub\mcpserver-claude-code-plugin\skills',
    'F:\github\mcpserver-claude-code-plugin\skills'
)
$skillSource = $skillCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($skillSource) {
    $skillDest = Join-Path $stagedRoot 'skills'
    New-Item -ItemType Directory -Force -Path $skillDest | Out-Null
    foreach ($name in @('session', 'triage')) {
        $from = Join-Path $skillSource $name
        if (Test-Path -LiteralPath $from) {
            $to = Join-Path $skillDest $name
            if (Test-Path -LiteralPath $to) { Remove-Item -LiteralPath $to -Recurse -Force }
            Copy-Item -LiteralPath $from -Destination $to -Recurse -Force
        }
    }
    Write-Host "Copied session/triage skills from $skillSource into $skillDest"
} else {
    Write-Warning 'No sibling plugin skills directory found for staged-plugin skill contracts.'
}

$started = [DateTimeOffset]::UtcNow
$started = [DateTimeOffset]::new(
    $started.Year, $started.Month, $started.Day,
    $started.Hour, $started.Minute, $started.Second,
    [TimeSpan]::Zero)
$startedText = $started.ToString('yyyy-MM-ddTHH:mm:ssZ')

$resultsRoot = Join-Path $repoRoot (Join-Path 'TestResults' $RunId)
if (Test-Path -LiteralPath $resultsRoot) {
    Remove-Item -LiteralPath $resultsRoot -Recurse -Force
}
$logDir = Join-Path $resultsRoot 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$script:GateCommands = [System.Collections.Generic.List[object]]::new()

function Add-GateCommand {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][int]$ExitCode,
        [Parameter(Mandatory)][datetimeoffset]$StartedAt,
        [Parameter(Mandatory)][datetimeoffset]$FinishedAt
    )
    $script:GateCommands.Add([ordered]@{
        name = $Name
        exitCode = $ExitCode
        startedAtUtc = $StartedAt.ToString('o')
        finishedAtUtc = $FinishedAt.ToString('o')
    })
}

function Invoke-GateNative {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Command
    )
    $logPath = Join-Path $logDir "$Name.log"
    $commandStarted = [DateTimeOffset]::UtcNow
    & $Command *> $logPath
    $exitCode = $LASTEXITCODE
    if (-not $? -and ($null -eq $exitCode -or $exitCode -eq 0)) { $exitCode = 1 }
    if ($null -eq $exitCode) { $exitCode = 1 }
    Get-Content -LiteralPath $logPath | Write-Host
    Add-GateCommand -Name $Name -ExitCode $exitCode -StartedAt $commandStarted -FinishedAt ([DateTimeOffset]::UtcNow)
    return $exitCode
}

function Write-GateJson {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Document
    )
    $directory = Split-Path -Parent $Path
    if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }
    $json = $Document | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($Path, $json + [Environment]::NewLine)
}

Push-Location $repoRoot
try {
    $captureExit = Invoke-GateNative -Name 'capture-manifest' -Command {
        & pwsh -NoLogo -NoProfile -File (Join-Path $repoRoot 'build.ps1') CaptureSessionLifeUnitGate --test-run-id $RunId
    }
    if ($captureExit -ne 0) {
        throw "CaptureSessionLifeUnitGate exited $captureExit."
    }

    # Pester tests share this process. The orchestrator's strict mode must not
    # turn unset script variables inside the suite into false failures.
    Set-StrictMode -Off
    $pesterStarted = [DateTimeOffset]::UtcNow
    $pesterExit = 1
    $pesterResult = $null
    $pesterDirectory = Join-Path $resultsRoot 'pester'
    New-Item -ItemType Directory -Force -Path $pesterDirectory | Out-Null
    try {
        $pesterConfig = New-PesterConfiguration
        $pesterConfig.Run.Path = Join-Path $repoRoot 'plugins/core/test-fixtures/pester'
        $pesterConfig.Run.PassThru = $true
        $pesterConfig.Run.Exit = $false
        $pesterConfig.Output.Verbosity = 'Detailed'
        $pesterConfig.TestResult.Enabled = $true
        $pesterConfig.TestResult.OutputFormat = 'NUnitXml'
        $pesterConfig.TestResult.OutputPath = Join-Path $pesterDirectory 'results.xml'
        $pesterResult = Invoke-Pester -Configuration $pesterConfig
        $adverse = [int]$pesterResult.FailedCount + [int]$pesterResult.SkippedCount + [int]$pesterResult.InconclusiveCount + [int]$pesterResult.NotRunCount + [int]$pesterResult.FailedBlocksCount + [int]$pesterResult.FailedContainersCount
        $pesterExit = $(if ($adverse -eq 0 -and [int]$pesterResult.TotalCount -gt 0) { 0 } else { 1 })
    } catch {
        Write-Warning $_
        $pesterResult = $null
        $pesterExit = 1
    } finally {
        Add-GateCommand -Name 'pester' -ExitCode $pesterExit -StartedAt $pesterStarted -FinishedAt ([DateTimeOffset]::UtcNow)
        $native = [ordered]@{
            RunId = $RunId
            ExecutedAt = $pesterStarted.ToString('o')
            Total = $(if ($pesterResult) { [int]$pesterResult.TotalCount } else { 0 })
            Passed = $(if ($pesterResult) { [int]$pesterResult.PassedCount } else { 0 })
            Failed = $(if ($pesterResult) { [int]$pesterResult.FailedCount } else { 0 })
            Skipped = $(if ($pesterResult) { [int]$pesterResult.SkippedCount } else { 0 })
            Inconclusive = $(if ($pesterResult) { [int]$pesterResult.InconclusiveCount } else { 0 })
            NotRun = $(if ($pesterResult) { [int]$pesterResult.NotRunCount } else { 0 })
            FailedBlocks = $(if ($pesterResult) { [int]$pesterResult.FailedBlocksCount } else { 0 })
            FailedContainers = $(if ($pesterResult) { [int]$pesterResult.FailedContainersCount } else { 0 })
        }
        Write-GateJson -Path (Join-Path $pesterDirectory 'native-run.json') -Document $native
    }

    $nukeExit = Invoke-GateNative -Name 'nuke-test' -Command {
        & pwsh -NoLogo -NoProfile -File (Join-Path $repoRoot 'build.ps1') Test --test-run-id $RunId
    }

    $buildTestsDir = Join-Path $resultsRoot 'build-tests'
    New-Item -ItemType Directory -Force -Path $buildTestsDir | Out-Null
    $buildExit = Invoke-GateNative -Name 'build-tests' -Command {
        # The TRX logger is the report. Hang-dump collection is omitted because it
        # writes multi-hundred-megabyte dumps after the counters are already complete.
        & dotnet test (Join-Path $repoRoot 'tests/Build.Tests/Build.Tests.csproj') -c Debug --filter 'Category!=Integration' --logger "trx;LogFileName=Build.Tests.trx" --results-directory $buildTestsDir
    }
    Get-ChildItem -LiteralPath $buildTestsDir -Filter '*.trx' -Recurse -File |
        Where-Object { $_.Name -ne 'Build.Tests.trx' } |
        Remove-Item -Force

    Write-GateJson -Path (Join-Path $resultsRoot 'command-results.json') -Document ([ordered]@{
        schemaVersion = 1
        runId = $RunId
        scope = 'unit'
        commands = @($script:GateCommands | Where-Object { $_.name -in @('pester', 'nuke-test', 'build-tests') })
    })

    $validateExit = Invoke-GateNative -Name 'validate' -Command {
        & pwsh -NoLogo -NoProfile -File (Join-Path $repoRoot 'build.ps1') CheckSessionLifeUnitGate --test-run-id $RunId --gate-started-at-utc $startedText
    }

    if ($validateExit -ne 0 -or $pesterExit -ne 0 -or $nukeExit -ne 0 -or $buildExit -ne 0) {
        exit 1
    }
} finally {
    Pop-Location
}
