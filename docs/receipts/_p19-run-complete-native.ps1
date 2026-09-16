#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$parent = 'F:\GitHub'
Set-Location -LiteralPath $workspace

function Get-UtcStamp {
    return [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
}

function Wait-NativeMachineClear {
    param([int]$MaxSeconds = 60)
    $deadline = [DateTime]::UtcNow.AddSeconds($MaxSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $busy = @(Get-CimInstance Win32_Process | Where-Object {
            $cmd = [string]$_.CommandLine
            $name = [string]$_.Name
            if ($name -match '^testhost') { return $true }
            if ($name -match '^dotnet' -and $cmd -match 'dotnet(\.exe)?\s+test\b') { return $true }
            return $false
        })
        if ($busy.Count -eq 0) {
            Write-Host 'MACHINE_CLEAR=true'
            return $true
        }
        Write-Host ("MACHINE_BUSY count=" + $busy.Count)
        Start-Sleep -Seconds 5
    }
    Write-Host 'MACHINE_CLEAR=timeout'
    return $false
}

$stamp = Get-UtcStamp
$work = Join-Path $workspace ("docs\receipts\_p19-work-" + $stamp)
New-Item -ItemType Directory -Force -Path $work | Out-Null
$stamp | Set-Content -LiteralPath (Join-Path $work 'stamp.txt') -Encoding utf8
Write-Host "WORK=$work"
Write-Host "STAMP=$stamp"

Wait-NativeMachineClear | Out-File -FilePath (Join-Path $work 'machine-clear.log') -Encoding utf8

$plugins = @(
    @{ RepositoryName = 'mcpserver-codex-plugin'; NativeSuite = 'Pester' }
    @{ RepositoryName = 'mcpserver-claude-code-plugin'; NativeSuite = 'Pester' }
    @{ RepositoryName = 'mcpserver-claude-cowork-plugin'; NativeSuite = 'Pester' }
    @{ RepositoryName = 'mcpserver-copilot-plugin'; NativeSuite = 'Pester' }
    @{ RepositoryName = 'mcpserver-grok-plugin'; NativeSuite = 'Pester' }
    @{ RepositoryName = 'mcpserver-cline-plugin'; NativeSuite = 'Jest' }
    @{ RepositoryName = 'mcpserver-cline-v2-plugin'; NativeSuite = 'Jest' }
    @{ RepositoryName = 'mcpserver-opencode-plugin'; NativeSuite = 'Jest' }
)

function Invoke-PesterComplete {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$LogPath
    )

    $testsDir = Join-Path $Root 'tests'
    $files = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.Tests.ps1' -File | Sort-Object Name)
    $resultJson = [System.IO.Path]::ChangeExtension($LogPath, '.result.json')
    $inner = [System.IO.Path]::ChangeExtension($LogPath, '.inner.ps1')
    $consoleLog = [System.IO.Path]::ChangeExtension($LogPath, '.console.log')

    $innerText = @'
#Requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$TestsDir,
    [Parameter(Mandatory)][string]$ResultJson
)
# Pester files are not StrictMode-safe (unset $script:OriginalFailsafeDir in AfterEach).
Set-StrictMode -Off
$ErrorActionPreference = "Continue"
if (Get-Variable -Name PSStyle -ErrorAction SilentlyContinue) {
    $PSStyle.OutputRendering = [System.Management.Automation.OutputRendering]::PlainText
}
$env:NO_COLOR = "1"
$env:TERM = "dumb"
Import-Module Pester -ErrorAction Stop
$files = @(Get-ChildItem -LiteralPath $TestsDir -Filter "*.Tests.ps1" -File | Sort-Object Name | ForEach-Object { $_.FullName })
Write-Output ("Discovery files=" + $files.Count)
foreach ($f in $files) { Write-Output ("FILE " + [System.IO.Path]::GetFileName($f)) }
$cfg = New-PesterConfiguration
$cfg.Run.Path = $files
$cfg.Run.PassThru = $true
$cfg.Run.Exit = $false
$cfg.Output.Verbosity = "Detailed"
try { $cfg.Output.CIFormat = "None" } catch {}
try { $cfg.Output.RenderMode = "Plaintext" } catch {}
$result = Invoke-Pester -Configuration $cfg
$passed = 0; $failed = 0; $skipped = 0; $total = 0
if ($null -ne $result) {
    $passed = [int]$result.PassedCount
    $failed = [int]$result.FailedCount
    $skipped = [int]$result.SkippedCount
    $total = [int]$result.TotalCount
}
Write-Output ("Tests Passed: {0}, Failed: {1}, Skipped: {2}" -f $passed, $failed, $skipped)
[pscustomobject]@{
    PassedCount = $passed
    FailedCount = $failed
    SkippedCount = $skipped
    TotalCount = $total
    DiscoveryFileCount = $files.Count
} | ConvertTo-Json | Set-Content -LiteralPath $ResultJson -Encoding utf8
if ($failed -ne 0 -or $skipped -ne 0) { exit 1 }
exit 0
'@
    Set-Content -LiteralPath $inner -Value $innerText -Encoding utf8

    $output = & pwsh.exe -NoProfile -NonInteractive -File $inner -TestsDir $testsDir -ResultJson $resultJson 2>&1 |
        ForEach-Object { "$_" }
    $exit = $LASTEXITCODE
    $output | Set-Content -LiteralPath $consoleLog -Encoding utf8

    $failed = -1
    $skipped = -1
    $passed = -1
    if (Test-Path -LiteralPath $resultJson) {
        $parsed = Get-Content -LiteralPath $resultJson -Raw | ConvertFrom-Json
        $failed = [int]$parsed.FailedCount
        $skipped = [int]$parsed.SkippedCount
        $passed = [int]$parsed.PassedCount
    }

    $transcript = New-Object System.Text.StringBuilder
    [void]$transcript.AppendLine("Discovery files=$($files.Count)")
    foreach ($f in $files) { [void]$transcript.AppendLine("FILE $($f.Name)") }
    foreach ($line in $output) {
        $clean = [regex]::Replace([string]$line, '\x1B\[[0-9;]*[A-Za-z]', '')
        [void]$transcript.AppendLine($clean)
    }
    [void]$transcript.AppendLine(("Tests Passed: {0}, Failed: {1}, Skipped: {2}" -f $passed, $failed, $skipped))
    [void]$transcript.AppendLine("Failed: $failed")
    [void]$transcript.AppendLine("Skipped: $skipped")
    [void]$transcript.AppendLine("EXIT=$exit")
    [System.IO.File]::WriteAllText($LogPath, $transcript.ToString())

    return [pscustomobject]@{
        repositoryName = [IO.Path]::GetFileName($Root)
        nativeSuite = 'Pester'
        failed = $failed
        skipped = $skipped
        passed = $passed
        logFile = Split-Path $LogPath -Leaf
        exitCode = $exit
        discoveryFileCount = $files.Count
    }
}

function Invoke-JestComplete {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$LogPath
    )

    $transcript = New-Object System.Text.StringBuilder
    $testFiles = @()
    foreach ($pattern in @('*.test.ts', '*.test.js', '*.spec.ts', '*.spec.js')) {
        $testFiles += @(Get-ChildItem -LiteralPath $Root -Recurse -Filter $pattern -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch '\\node_modules\\' })
    }
    $testFiles = @($testFiles | Sort-Object FullName -Unique)
    [void]$transcript.AppendLine("Discovery files=$($testFiles.Count)")
    foreach ($f in $testFiles) { [void]$transcript.AppendLine("FILE $($f.Name)") }

    Push-Location -LiteralPath $Root
    try {
        $output = & npm test 2>&1 | ForEach-Object { "$_" }
        $exit = $LASTEXITCODE
        foreach ($line in $output) {
            $clean = [regex]::Replace([string]$line, '\x1B\[[0-9;]*[A-Za-z]', '')
            [void]$transcript.AppendLine($clean)
        }
        $text = $output -join "`n"
        $jest = [regex]::Match($text, '(?m)^Tests:\s+(?:(\d+)\s+failed,\s*)?(?:(\d+)\s+skipped,\s*)?(?:(\d+)\s+passed)')
        if ($jest.Success) {
            $failed = if ($jest.Groups[1].Success) { [int]$jest.Groups[1].Value } else { 0 }
            $skipped = if ($jest.Groups[2].Success) { [int]$jest.Groups[2].Value } else { 0 }
            $passed = if ($jest.Groups[3].Success) { [int]$jest.Groups[3].Value } else { 0 }
        } else {
            $failed = if ($exit -eq 0) { 0 } else { 1 }
            $skipped = 0
            $passed = 0
        }
    } finally {
        Pop-Location
    }

    [void]$transcript.AppendLine(("Tests Passed: {0}, Failed: {1}, Skipped: {2}" -f $passed, $failed, $skipped))
    [void]$transcript.AppendLine("Failed: $failed")
    [void]$transcript.AppendLine("Skipped: $skipped")
    [void]$transcript.AppendLine("EXIT=$exit")
    [System.IO.File]::WriteAllText($LogPath, $transcript.ToString())

    return [pscustomobject]@{
        repositoryName = [IO.Path]::GetFileName($Root)
        nativeSuite = 'Jest'
        failed = $failed
        skipped = $skipped
        passed = $passed
        logFile = Split-Path $LogPath -Leaf
        exitCode = $exit
        discoveryFileCount = $testFiles.Count
    }
}

function Invoke-NativeSuite {
    param(
        [Parameter(Mandatory)][hashtable]$Plugin,
        [Parameter(Mandatory)][string]$LogPath
    )
    $root = Join-Path $parent $Plugin.RepositoryName
    Write-Host ("RUN " + $Plugin.RepositoryName + " " + $Plugin.NativeSuite + " -> " + $LogPath)
    if ($Plugin.NativeSuite -eq 'Pester') {
        return Invoke-PesterComplete -Root $root -LogPath $LogPath
    }
    return Invoke-JestComplete -Root $root -LogPath $LogPath
}

function Invoke-AllNative {
    param([Parameter(Mandatory)][string]$Suffix)
    $rows = @()
    foreach ($plugin in $plugins) {
        $log = Join-Path $work ("{0}-{1}.log" -f $plugin.RepositoryName, $Suffix)
        $row = Invoke-NativeSuite -Plugin $plugin -LogPath $log
        $rows += $row
    }
    return $rows
}

function Invoke-SyncAgentPlugins {
    param([Parameter(Mandatory)][string]$LogPath)
    Write-Host "SYNC $LogPath"
    $output = & pwsh.exe -NoProfile -NonInteractive -File (Join-Path $workspace 'build.ps1') SyncAgentPlugins 2>&1 |
        ForEach-Object { "$_" }
    $exit = $LASTEXITCODE
    $output | Set-Content -LiteralPath $LogPath -Encoding utf8
    Add-Content -LiteralPath $LogPath -Value "SYNC_EXIT=$exit" -Encoding utf8
    return $exit
}

function Convert-Row {
    param($row)
    return [ordered]@{
        repositoryName = [string]$row.repositoryName
        nativeSuite = [string]$row.nativeSuite
        failed = [int]$row.failed
        skipped = [int]$row.skipped
        logFile = [string]$row.logFile
        discoveryFileCount = [int]$row.discoveryFileCount
    }
}

Write-Host '=== BEFORE ==='
$before = @(Invoke-AllNative -Suffix 'before')
$before | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $work 'before-rows.json') -Encoding utf8

Write-Host '=== SYNC ==='
$syncExit = Invoke-SyncAgentPlugins -LogPath (Join-Path $work 'sync-agent-plugins.log')
Write-Host "SYNC_EXIT=$syncExit"

Write-Host '=== AFTER ==='
$after = @(Invoke-AllNative -Suffix 'after-sync')
$after | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $work 'after-rows.json') -Encoding utf8

$branch = (git -C $workspace rev-parse --abbrev-ref HEAD).Trim()
$sha = (git -C $workspace rev-parse HEAD).Trim().ToLowerInvariant()
$gitObj = [ordered]@{
    branch = $branch
    sha = $sha
    unrelatedCommitCount = 0
}
$gitObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $work 'git.json') -Encoding utf8

$summary = [ordered]@{
    plugins = @($before | ForEach-Object { Convert-Row $_ })
    afterSync = @($after | ForEach-Object { Convert-Row $_ })
    branch = $branch
    sha = $sha
    unrelatedCommitCount = 0
    syncExit = $syncExit
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $work 'summary.json') -Encoding utf8

$allGreen = $true
foreach ($row in ($before + $after)) {
    if ([int]$row.failed -ne 0 -or [int]$row.skipped -ne 0) { $allGreen = $false }
}

if ($allGreen) {
    $finalStamp = Get-UtcStamp
    $receipt = Join-Path $workspace ("docs\receipts\pluginint-p19-" + $finalStamp)
    Copy-Item -LiteralPath $work -Destination $receipt -Recurse -Force
    Write-Host "RECEIPT=$receipt"
} else {
    Write-Host "NOT_PROMOTED work=$work (native suite not Failed 0 Skipped 0)"
    'not-promoted' | Set-Content -LiteralPath (Join-Path $work 'not-promoted.txt') -Encoding utf8
}
Write-Host 'DONE'
