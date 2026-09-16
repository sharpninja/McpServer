#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$receiptDir = 'F:\GitHub\McpServer\docs\receipts\pluginint-p19-20260822T095842Z'
$credTrx = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p19\results-p19-20260822T092241Z\p19-red.trx'
$credMd = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T092424Z.md'
$credJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T092424Z.json'
$pluginInt = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'

function Parse-FailedSkipped {
    param([string]$Log)
    $failed = -1
    $skipped = -1
    $path = $null
    if ([string]::IsNullOrWhiteSpace($Log)) {
        return [ordered]@{ parsed = $false; failed = $failed; skipped = $skipped; path = 'empty'; pester = $false; jest = $false; fallback = $false }
    }
    $pester = [regex]::Match($Log, 'Tests Passed:\s*\d+,\s*Failed:\s*(\d+),\s*Skipped:\s*(\d+)', 'IgnoreCase')
    if ($pester.Success) {
        return [ordered]@{
            parsed = $true
            failed = [int]$pester.Groups[1].Value
            skipped = [int]$pester.Groups[2].Value
            path = 'pester'
            pester = $true
            jest = $false
            fallback = $false
            pesterPassed = [int]([regex]::Match($Log, 'Tests Passed:\s*(\d+)', 'IgnoreCase').Groups[1].Value)
        }
    }
    $jest = [regex]::Match($Log, '(?im)^Tests:\s+(?:(\d+)\s+failed,\s*)?(?:(\d+)\s+skipped,\s*)?(?:(\d+)\s+passed)')
    if ($jest.Success) {
        $jf = if ($jest.Groups[1].Success) { [int]$jest.Groups[1].Value } else { 0 }
        $js = if ($jest.Groups[2].Success) { [int]$jest.Groups[2].Value } else { 0 }
        return [ordered]@{
            parsed = $true
            failed = $jf
            skipped = $js
            path = 'jest'
            pester = $false
            jest = $true
            fallback = $false
            jestPassed = if ($jest.Groups[3].Success) { [int]$jest.Groups[3].Value } else { $null }
        }
    }
    $failedLine = [regex]::Matches($Log, '(?im)^Failed(?:!)?:\s*(\d+)\s*$')
    $skippedLine = [regex]::Matches($Log, '(?im)^Skipped:\s*(\d+)\s*$')
    if ($failedLine.Count -eq 0 -or $skippedLine.Count -eq 0) {
        return [ordered]@{ parsed = $false; failed = $failed; skipped = $skipped; path = 'none'; pester = $false; jest = $false; fallback = $false }
    }
    return [ordered]@{
        parsed = $true
        failed = [int]$failedLine[$failedLine.Count - 1].Groups[1].Value
        skipped = [int]$skippedLine[$skippedLine.Count - 1].Groups[1].Value
        path = 'fallback'
        pester = $false
        jest = $false
        fallback = $true
        failedLineCount = $failedLine.Count
        skippedLineCount = $skippedLine.Count
    }
}

$plugins = @(
    'mcpserver-codex-plugin'
    'mcpserver-claude-code-plugin'
    'mcpserver-claude-cowork-plugin'
    'mcpserver-copilot-plugin'
    'mcpserver-grok-plugin'
    'mcpserver-cline-plugin'
    'mcpserver-cline-v2-plugin'
    'mcpserver-opencode-plugin'
)

$inventory = @()
foreach ($name in $plugins) {
    $root = Join-Path 'F:\GitHub' $name
    $testsDir = Join-Path $root 'tests'
    $pesterFiles = @()
    $batsFiles = @()
    $jestFiles = @()
    if (Test-Path -LiteralPath $testsDir) {
        $pesterFiles = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.Tests.ps1' -File | Select-Object -ExpandProperty Name)
        $batsFiles = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.bats' -File | Select-Object -ExpandProperty Name)
        $jestFiles = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.test.ts' -File | Select-Object -ExpandProperty Name)
    }
    $shLibHooks = @()
    foreach ($rel in @('lib','hooks')) {
        $dir = Join-Path $root $rel
        if (Test-Path -LiteralPath $dir) {
            $shLibHooks += @(Get-ChildItem -LiteralPath $dir -Recurse -Include *.sh,*.bash -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace('\','/') })
        }
    }
    $beforeLog = Join-Path $receiptDir ($name + '-before.log')
    $afterLog = Join-Path $receiptDir ($name + '-after-sync.log')
    $beforeText = if (Test-Path -LiteralPath $beforeLog) { Get-Content -LiteralPath $beforeLog -Raw } else { $null }
    $afterText = if (Test-Path -LiteralPath $afterLog) { Get-Content -LiteralPath $afterLog -Raw } else { $null }
    $executedBefore = @()
    $executedAfter = @()
    if ($beforeText) {
        $executedBefore = @([regex]::Matches($beforeText, '(?m)\[(?:\+|-)\]\s+.+\\tests\\([^\\\r\n]+\.Tests\.ps1)') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
        $executedBefore += @([regex]::Matches($beforeText, '(?m)^PASS tests/([^ \r\n]+)') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
    }
    if ($afterText) {
        $executedAfter = @([regex]::Matches($afterText, '(?m)\[(?:\+|-)\]\s+.+\\tests\\([^\\\r\n]+\.Tests\.ps1)') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
        $executedAfter += @([regex]::Matches($afterText, '(?m)^PASS tests/([^ \r\n]+)') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
    }
    $omittedPester = @($pesterFiles | Where-Object { $_ -notin $executedBefore })
    $batsAssertSh = $false
    foreach ($bats in $batsFiles) {
        $batsPath = Join-Path $testsDir $bats
        $batsText = Get-Content -LiteralPath $batsPath -Raw
        if ($batsText -match 'session-start\.sh|hook-lib\.sh|plugin-env\.sh') { $batsAssertSh = $true }
    }
    $failsafeNoiseBefore = 0
    $failsafeNoiseAfter = 0
    if ($beforeText) { $failsafeNoiseBefore = @([regex]::Matches($beforeText, 'failsafe replay flushed=\d+ failed=\d+')).Count }
    if ($afterText) { $failsafeNoiseAfter = @([regex]::Matches($afterText, 'failsafe replay flushed=\d+ failed=\d+')).Count }
    $inventory += [ordered]@{
        repositoryName = $name
        testsDirExists = (Test-Path -LiteralPath $testsDir)
        pesterFiles = $pesterFiles
        batsFiles = $batsFiles
        jestFiles = $jestFiles
        shInLibOrHooks = $shLibHooks
        executedBefore = $executedBefore
        executedAfter = $executedAfter
        omittedPesterBefore = $omittedPester
        batsAssertSh = $batsAssertSh
        beforeExists = [bool]$beforeText
        afterExists = [bool]$afterText
        beforeParse = if ($beforeText) { Parse-FailedSkipped $beforeText } else { $null }
        afterParse = if ($afterText) { Parse-FailedSkipped $afterText } else { $null }
        failsafeNoiseBefore = $failsafeNoiseBefore
        failsafeNoiseAfter = $failsafeNoiseAfter
        discoveryBefore = if ($beforeText -and $beforeText -match 'Starting discovery in (\d+) files') { [int]$Matches[1] } else { $null }
        discoveryAfter = if ($afterText -and $afterText -match 'Starting discovery in (\d+) files') { [int]$Matches[1] } else { $null }
        versionBefore = if ($beforeText -and $beforeText -match '@[\w\./-]+@([\d.]+)\s+test') { $Matches[1] } else { $null }
        versionAfter = if ($afterText -and $afterText -match '@[\w\./-]+@([\d.]+)\s+test') { $Matches[1] } else { $null }
    }
}

$inventory | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'plugin-inventory.json') -Encoding utf8

$summaryPath = Join-Path $receiptDir 'summary.json'
$gitPath = Join-Path $receiptDir 'git.json'
$syncLog = Join-Path $receiptDir 'sync-agent-plugins.log'
$trxPath = Join-Path $receiptDir 'PluginNativeSuiteReceiptTests.trx'
$summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
$gitJson = Get-Content -LiteralPath $gitPath -Raw | ConvertFrom-Json

$liveSha = (git -C 'F:\GitHub\McpServer' rev-parse HEAD).Trim()
$liveBranch = (git -C 'F:\GitHub\McpServer' rev-parse --abbrev-ref HEAD).Trim()
$porcelain = git -C 'F:\GitHub\McpServer' status --porcelain
$todoPorcelain = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{
    liveSha = $liveSha
    liveBranch = $liveBranch
    receiptSha = [string]$gitJson.sha
    receiptBranch = [string]$gitJson.branch
    shaMatchLive = ($liveSha -eq [string]$gitJson.sha)
    unrelatedCommitCount = [int]$gitJson.unrelatedCommitCount
    shaIs40Hex = [bool]([regex]::IsMatch([string]$gitJson.sha, '^[0-9a-f]{40}$'))
    porcelain = @($porcelain)
    todoYamlPorcelain = @($todoPorcelain)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'git-live.json') -Encoding utf8

$p20Hits = @(Select-String -Path (Join-Path $pluginInt '*.cs') -Pattern 'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero|PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag' -SimpleMatch:$false)
$p20Any = @(Get-ChildItem -LiteralPath $pluginInt -Filter '*.cs' -File | Select-String -Pattern 'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero|PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag')
[ordered]@{
    p20HitCount = @($p20Any).Count
    p20Files = @($p20Any | ForEach-Object { $_.Path })
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p20-grep.json') -Encoding utf8

$credMdText = Get-Content -LiteralPath $credMd -Raw
$credJsonObj = Get-Content -LiteralPath $credJson -Raw | ConvertFrom-Json
$credTrxText = Get-Content -LiteralPath $credTrx -Raw
[ordered]@{
    credMdExists = $true
    credMdOverall = if ($credMdText -match 'OverallVerdict:\s+(\w+)') { $Matches[1] } else { $null }
    credJsonOverall = [string]$credJsonObj.OverallVerdict
    credJsonFailCount = [int]$credJsonObj.FailCount
    credTrxFileNotFoundCount = @([regex]::Matches($credTrxText, 'FileNotFoundException')).Count
    credTrxFailed = if ($credTrxText -match 'failed="(\d+)"') { $Matches[1] } else { $null }
    credTrxPassed = if ($credTrxText -match 'passed="(\d+)"') { $Matches[1] } else { $null }
    credTrxMessage = ($credTrxText -match 'No docs/receipts/pluginint-p19') 
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'c-red-receipt.json') -Encoding utf8

$receiptFiles = @(Get-ChildItem -LiteralPath $receiptDir -File | Select-Object -ExpandProperty Name)
[ordered]@{
    directory = $receiptDir
    files = $receiptFiles
    hasSummary = ($receiptFiles -contains 'summary.json')
    hasGit = ($receiptFiles -contains 'git.json')
    hasSyncLog = ($receiptFiles -contains 'sync-agent-plugins.log')
    hasTrx = ($receiptFiles -contains 'PluginNativeSuiteReceiptTests.trx')
    pluginCount = @($summary.plugins).Count
    afterSyncCount = @($summary.afterSync).Count
    catalogNames = @($summary.plugins | ForEach-Object { $_.repositoryName })
    afterNames = @($summary.afterSync | ForEach-Object { $_.repositoryName })
    allFailedZero = (@($summary.plugins + $summary.afterSync | Where-Object { $_.failed -ne 0 }).Count -eq 0)
    allSkippedZero = (@($summary.plugins + $summary.afterSync | Where-Object { $_.skipped -ne 0 }).Count -eq 0)
    nativeSuites = @($summary.plugins | ForEach-Object { [ordered]@{ name = $_.repositoryName; suite = $_.nativeSuite; log = $_.logFile } })
    branch = [string]$summary.branch
    sha = [string]$summary.sha
    unrelated = [int]$summary.unrelatedCommitCount
    syncSucceeded = ((Get-Content -LiteralPath $syncLog -Raw) -match 'SyncAgentPlugins\s+Succeeded')
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'summary-audit.json') -Encoding utf8

$python = @(Get-CimInstance Win32_Process -Filter "Name='python.exe' OR Name='python3.exe' OR Name='py.exe'" -ErrorAction SilentlyContinue |
    Select-Object ProcessId, Name, CommandLine)
[ordered]@{
    pythonProcessCount = @($python).Count
    processes = @($python)
    validatorInvokedPython = $false
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'python-procs.json') -Encoding utf8

$nativeSmoke = @()
foreach ($name in @('mcpserver-codex-plugin','mcpserver-claude-code-plugin','mcpserver-claude-cowork-plugin','mcpserver-copilot-plugin','mcpserver-grok-plugin')) {
    $p = Join-Path 'F:\GitHub' (Join-Path $name 'tests\NativeSmoke.Tests.ps1')
    $nativeSmoke += [ordered]@{
        plugin = $name
        exists = (Test-Path -LiteralPath $p)
        lastWriteUtc = if (Test-Path -LiteralPath $p) { (Get-Item -LiteralPath $p).LastWriteTimeUtc.ToString('o') } else { $null }
        length = if (Test-Path -LiteralPath $p) { (Get-Item -LiteralPath $p).Length } else { 0 }
    }
}
$nativeSmoke | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'native-smoke.json') -Encoding utf8

$copied = @('Skills.Tests.ps1','ReplMethodTimeout.Tests.ps1','ReplWorkspaceResolution.Tests.ps1','SessionIdCanonicalAgent.Tests.ps1')
$copyCompare = @()
$claudeTests = 'F:\GitHub\mcpserver-claude-code-plugin\tests'
foreach ($other in @('mcpserver-codex-plugin','mcpserver-claude-cowork-plugin','mcpserver-copilot-plugin','mcpserver-grok-plugin')) {
    foreach ($file in $copied) {
        $a = Join-Path $claudeTests $file
        $b = Join-Path (Join-Path 'F:\GitHub' $other) (Join-Path 'tests' $file)
        $ha = if (Test-Path -LiteralPath $a) { (Get-FileHash -LiteralPath $a -Algorithm SHA256).Hash } else { $null }
        $hb = if (Test-Path -LiteralPath $b) { (Get-FileHash -LiteralPath $b -Algorithm SHA256).Hash } else { $null }
        $copyCompare += [ordered]@{
            file = $file
            other = $other
            exists = (Test-Path -LiteralPath $b)
            hashEqual = ($ha -and $hb -and $ha -eq $hb)
        }
    }
}
$copyCompare | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'copied-pester.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
