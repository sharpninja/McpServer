#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19-r2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$receiptsRoot = 'F:\GitHub\McpServer\docs\receipts'
$latest = Get-ChildItem -LiteralPath $receiptsRoot -Directory | Where-Object { $_.Name -match '^pluginint-p19-\d{8}T\d{6}Z$' } | Sort-Object Name -Descending | Select-Object -First 1
$allDirs = @(Get-ChildItem -LiteralPath $receiptsRoot -Directory | Where-Object { $_.Name -match '^pluginint-p19-\d{8}T\d{6}Z$' } | Sort-Object Name | ForEach-Object { $_.Name })
[ordered]@{
    allPluginintP19 = $allDirs
    latestName = if ($latest) { $latest.Name } else { $null }
    latestPath = if ($latest) { $latest.FullName } else { $null }
    claimed102645Exists = Test-Path -LiteralPath (Join-Path $receiptsRoot 'pluginint-p19-20260822T102645Z')
    claimed095842Exists = Test-Path -LiteralPath (Join-Path $receiptsRoot 'pluginint-p19-20260822T095842Z')
    latestIs102645 = $latest -and $latest.Name -eq 'pluginint-p19-20260822T102645Z'
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'receipt-dirs.json') -Encoding utf8

$receiptDir = 'F:\GitHub\McpServer\docs\receipts\pluginint-p19-20260822T102645Z'
$files = @(Get-ChildItem -LiteralPath $receiptDir -File | ForEach-Object { $_.Name }) | Sort-Object
[ordered]@{ files = $files; count = $files.Count } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-files.json') -Encoding utf8

function Get-Plain([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return '' }
    return [regex]::Replace($text, '\x1B\[[0-9;]*[A-Za-z]', '')
}

function Parse-Log([string]$path) {
    $raw = if (Test-Path -LiteralPath $path) { Get-Content -LiteralPath $path -Raw } else { $null }
    $plain = Get-Plain $raw
    $disc = [regex]::Match($plain, 'Starting discovery in (\d+) files')
    $found = [regex]::Match($plain, 'Discovery found (\d+) tests')
    $passedLine = [regex]::Match($plain, 'Tests Passed:\s*(\d+),\s*Failed:\s*(\d+),\s*Skipped:\s*(\d+)')
    $jest = [regex]::Match($plain, '(?im)^Tests:\s+(?:(\d+)\s+failed,\s*)?(?:(\d+)\s+skipped,\s*)?(?:(\d+)\s+passed)')
    $plus = @([regex]::Matches($plain, '\[\+\]\s+(\S+\.Tests\.ps1)') | ForEach-Object { [IO.Path]::GetFileName($_.Groups[1].Value) })
    $failFooter = [regex]::Matches($plain, '(?im)^Failed(?:!)?:\s*(\d+)\s*$')
    $skipFooter = [regex]::Matches($plain, '(?im)^Skipped:\s*(\d+)\s*$')
    $exit = [regex]::Match($plain, '(?im)^EXIT=(\d+)\s*$')
    $jestRanAll = $plain -match 'Ran all test suites'
    $jestSuites = [regex]::Match($plain, 'Test Suites:\s+(\d+) passed')
    return [ordered]@{
        exists = [bool]$raw
        length = if ($raw) { $raw.Length } else { 0 }
        discoveryFiles = if ($disc.Success) { [int]$disc.Groups[1].Value } else { $null }
        discoveryTests = if ($found.Success) { [int]$found.Groups[1].Value } else { $null }
        pesterPassed = if ($passedLine.Success) { [int]$passedLine.Groups[1].Value } else { $null }
        pesterFailed = if ($passedLine.Success) { [int]$passedLine.Groups[2].Value } else { $null }
        pesterSkipped = if ($passedLine.Success) { [int]$passedLine.Groups[3].Value } else { $null }
        plusFiles = $plus
        plusCount = $plus.Count
        footerFailed = if ($failFooter.Count -gt 0) { [int]$failFooter[$failFooter.Count - 1].Groups[1].Value } else { $null }
        footerSkipped = if ($skipFooter.Count -gt 0) { [int]$skipFooter[$skipFooter.Count - 1].Groups[1].Value } else { $null }
        exitCode = if ($exit.Success) { [int]$exit.Groups[1].Value } else { $null }
        jestPassed = if ($jest.Success -and $jest.Groups[3].Success) { [int]$jest.Groups[3].Value } else { $null }
        jestFailed = if ($jest.Success -and $jest.Groups[1].Success) { [int]$jest.Groups[1].Value } else { 0 }
        jestSkipped = if ($jest.Success -and $jest.Groups[2].Success) { [int]$jest.Groups[2].Value } else { 0 }
        jestRanAll = $jestRanAll
        jestSuitesPassed = if ($jestSuites.Success) { [int]$jestSuites.Groups[1].Value } else { $null }
    }
}

$plugins = @(
    @{ name = 'mcpserver-codex-plugin'; root = 'F:\GitHub\mcpserver-codex-plugin'; kind = 'Pester' }
    @{ name = 'mcpserver-claude-code-plugin'; root = 'F:\GitHub\mcpserver-claude-code-plugin'; kind = 'Pester' }
    @{ name = 'mcpserver-claude-cowork-plugin'; root = 'F:\GitHub\mcpserver-claude-cowork-plugin'; kind = 'Pester' }
    @{ name = 'mcpserver-copilot-plugin'; root = 'F:\GitHub\mcpserver-copilot-plugin'; kind = 'Pester' }
    @{ name = 'mcpserver-grok-plugin'; root = 'F:\GitHub\mcpserver-grok-plugin'; kind = 'Pester' }
    @{ name = 'mcpserver-cline-plugin'; root = 'F:\GitHub\mcpserver-cline-plugin'; kind = 'Jest' }
    @{ name = 'mcpserver-cline-v2-plugin'; root = 'F:\GitHub\mcpserver-cline-v2-plugin'; kind = 'Jest' }
    @{ name = 'mcpserver-opencode-plugin'; root = 'F:\GitHub\mcpserver-opencode-plugin'; kind = 'Jest' }
)

$inventory = @()
foreach ($p in $plugins) {
    $testsDir = Join-Path $p.root 'tests'
    $ps1 = @()
    $bats = @()
    $jest = @()
    $shLib = @()
    $shHooks = @()
    if (Test-Path -LiteralPath $testsDir) {
        $ps1 = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.Tests.ps1' -File | ForEach-Object { $_.Name } | Sort-Object)
        $bats = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.bats' -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name } | Sort-Object)
        $jest = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.test.ts' -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name } | Sort-Object)
    }
    $libDir = Join-Path $p.root 'lib'
    $hooksDir = Join-Path $p.root 'hooks'
    if (Test-Path -LiteralPath $libDir) {
        $shLib = @(Get-ChildItem -LiteralPath $libDir -Recurse -Include '*.sh','*.bash' -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
    }
    if (Test-Path -LiteralPath $hooksDir) {
        $shHooks = @(Get-ChildItem -LiteralPath $hooksDir -Recurse -Include '*.sh','*.bash' -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
    }
    $before = Parse-Log (Join-Path $receiptDir ($p.name + '-before.log'))
    $after = Parse-Log (Join-Path $receiptDir ($p.name + '-after-sync.log'))
    $missingBefore = @()
    $missingAfter = @()
    if ($p.kind -eq 'Pester') {
        $missingBefore = @($ps1 | Where-Object { $before.plusFiles -notcontains $_ })
        $missingAfter = @($ps1 | Where-Object { $after.plusFiles -notcontains $_ })
    }
    if ($p.kind -eq 'Jest') {
        $missingBefore = @($jest | Where-Object { (Get-Plain (Get-Content -LiteralPath (Join-Path $receiptDir ($p.name + '-before.log')) -Raw -ErrorAction SilentlyContinue)) -notmatch [regex]::Escape($_) })
        $missingAfter = @($jest | Where-Object { (Get-Plain (Get-Content -LiteralPath (Join-Path $receiptDir ($p.name + '-after-sync.log')) -Raw -ErrorAction SilentlyContinue)) -notmatch [regex]::Escape($_) })
    }
    $inventory += [ordered]@{
        name = $p.name
        kind = $p.kind
        onDiskPs1 = $ps1
        onDiskPs1Count = $ps1.Count
        onDiskBats = $bats
        onDiskJest = $jest
        onDiskJestCount = $jest.Count
        shLibCount = $shLib.Count
        shHooksCount = $shHooks.Count
        shLib = $shLib
        shHooks = $shHooks
        before = $before
        after = $after
        missingPs1Before = $missingBefore
        missingPs1After = $missingAfter
    }
}
$inventory | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'plugin-inventory.json') -Encoding utf8

$oldLog = Parse-Log 'F:\GitHub\McpServer\docs\receipts\pluginint-p19-20260822T095842Z\mcpserver-claude-code-plugin-before.log'
[ordered]@{ old095842ClaudeBefore = $oldLog } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'old-subset.json') -Encoding utf8

$syncLog = Get-Content -LiteralPath (Join-Path $receiptDir 'sync-agent-plugins.log') -Raw
$syncPlain = Get-Plain $syncLog
$syncSucceeded = $syncPlain -match 'SyncAgentPlugins' -and $syncPlain -match 'Succeeded'
[ordered]@{
    syncSucceededToken = [bool]($syncPlain -match 'Succeeded')
    containsTargetName = [bool]($syncPlain -match 'SyncAgentPlugins')
    containsBuildSucceeded = [bool]($syncPlain -match 'Build succeeded')
    coreSha = if ($syncPlain -match 'core 910a4449') { '910a4449' } else { $null }
    durationToken = [bool]($syncPlain -match '0:38')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'sync-audit.json') -Encoding utf8

$gitLive = [ordered]@{
    branch = (git -C 'F:\GitHub\McpServer' rev-parse --abbrev-ref HEAD).Trim()
    sha = (git -C 'F:\GitHub\McpServer' rev-parse HEAD).Trim()
    porcelain = @(git -C 'F:\GitHub\McpServer' status --porcelain)
}
$gitLive | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'git-live.json') -Encoding utf8

$pluginGit = @()
foreach ($p in $plugins) {
    $pluginGit += [ordered]@{
        name = $p.name
        branch = (git -C $p.root rev-parse --abbrev-ref HEAD).Trim()
        sha = (git -C $p.root rev-parse HEAD).Trim()
        porcelainCount = @((git -C $p.root status --porcelain)).Count
    }
}
$pluginGit | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'plugin-git-live.json') -Encoding utf8

$p20Tests = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero|PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag' -ErrorAction SilentlyContinue)
[ordered]@{
    p20HitsInPluginIntegrationCs = $p20Tests.Count
    hits = @($p20Tests | ForEach-Object { $_.Path + ':' + $_.LineNumber })
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p20-grep.json') -Encoding utf8

$todoPorcelain = @(git -C 'F:\GitHub\McpServer' status --porcelain -- 'docs/Project/TODO.yaml' 'docs/todo.yaml')
[ordered]@{ todoYamlPorcelain = $todoPorcelain } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-porcelain.json') -Encoding utf8

$py = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^(python|python3|py)\.exe$' } | Select-Object ProcessId, Name, CommandLine)
[ordered]@{ pythonProcessCount = $py.Count; processes = $py } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'python-procs.json') -Encoding utf8

$requiredClaude = @(
    'ClaudeHookWiring.Tests.ps1'
    'CurrentTurnSessionRebind.Tests.ps1'
    'HookTurnDedupe.Tests.ps1'
    'NativeSmoke.Tests.ps1'
    'ReplFailsafe.Tests.ps1'
    'ReplMethodTimeout.Tests.ps1'
    'ReplWorkspaceResolution.Tests.ps1'
    'SessionIdCanonicalAgent.Tests.ps1'
    'Skills.Tests.ps1'
    'StopGateHardening.Tests.ps1'
)
$claudeDisk = @(Get-ChildItem -LiteralPath 'F:\GitHub\mcpserver-claude-code-plugin\tests' -Filter '*.Tests.ps1' -File | ForEach-Object { $_.Name } | Sort-Object)
[ordered]@{
    required = $requiredClaude
    onDisk = $claudeDisk
    requiredCount = $requiredClaude.Count
    onDiskCount = $claudeDisk.Count
    missingRequired = @($requiredClaude | Where-Object { $claudeDisk -notcontains $_ })
    extraOnDisk = @($claudeDisk | Where-Object { $requiredClaude -notcontains $_ })
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'claude-10-files.json') -Encoding utf8

$failsafe = Select-String -Path 'F:\GitHub\mcpserver-claude-code-plugin\tests\ReplFailsafe.Tests.ps1' -Pattern 'MCPSERVER_FAILSAFE_DIR'
$dedupe = Select-String -Path 'F:\GitHub\mcpserver-claude-code-plugin\tests\HookTurnDedupe.Tests.ps1' -Pattern 'WorkspacePath \$script:TestRoot'
$rebind = Select-String -Path 'F:\GitHub\mcpserver-claude-code-plugin\tests\CurrentTurnSessionRebind.Tests.ps1' -Pattern "Should -Be 'ClaudeCode-20260716T020000Z-plugin-session'"
[ordered]@{
    failsafeDirHits = @($failsafe | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
    workspacePathHits = @($dedupe | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
    rebindExpectHits = @($rebind | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'test-fix-anchors.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
