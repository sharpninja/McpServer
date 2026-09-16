#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p19'
$testsDir = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
$receiptsRoot = 'F:\GitHub\McpServer\docs\receipts'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$p19Tests = Join-Path $testsDir 'PluginNativeSuiteReceiptTests.cs'
$p19Loader = Join-Path $testsDir 'PluginNativeSuiteReceipt.cs'
$catalogJson = Join-Path $testsDir 'scenarios\plugin-sessionlog-scenarios.json'
$csproj = Join-Path $testsDir 'McpServer.PluginIntegration.Tests.csproj'
$priorAgree = Join-Path $receiptsRoot 'hostile-validator-20260822T084649Z.md'
$priorAgreeJson = Join-Path $receiptsRoot 'hostile-validator-20260822T084649Z.json'
$plan = 'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'

$files = @(
    $p19Tests
    $p19Loader
    $catalogJson
    $csproj
    $priorAgree
    $priorAgreeJson
    $plan
)
$timestamps = foreach ($path in $files) {
    if (-not (Test-Path -LiteralPath $path)) {
        [ordered]@{ Name = [IO.Path]::GetFileName($path); Path = $path; Exists = $false }
        continue
    }
    $item = Get-Item -LiteralPath $path
    $text = Get-Content -LiteralPath $path -Raw
    [ordered]@{
        Name = $item.Name
        Path = $path
        Exists = $true
        LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        Length = $item.Length
        HasSkipAttribute = [bool]($text -match '\[(Fact|Theory)\s*\(\s*Skip')
        HasAssertSkip = [bool]($text -match 'Assert\.Skip')
        HasP19Each = [bool]($text -match 'PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero')
        HasP19AfterSync = [bool]($text -match 'PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero')
        HasP19Git = [bool]($text -match 'PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit')
        HasP20Harness = [bool]($text -match 'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero')
        HasP20Promotion = [bool]($text -match 'PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag')
        HasLoadLatest = [bool]($text -match 'LoadLatest')
        HasStampPattern = [bool]($text -match 'pluginint-p19-')
        HasAllowedSuites = [bool]($text -match 'Pester' -and $text -match 'Bats' -and $text -match 'Jest' -and $text -match 'xUnit')
        HasFileNotFoundMessage = [bool]($text -match 'No docs/receipts/pluginint-p19-<utc> receipt directory exists')
        HasTraitDeterministic = [bool]($text -match 'Trait\("PluginInt",\s*"Deterministic"\)')
        HasFactAttribute = [bool]($text -match '\[Fact\]')
        OverallVerdictAgree = [bool]($text -match 'OverallVerdict:\s*AGREE')
    }
}
$timestamps | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'file-timestamps.json') -Encoding utf8

$skipHits = [System.Collections.Generic.List[object]]::new()
$p19Hits = [System.Collections.Generic.List[object]]::new()
$p20Hits = [System.Collections.Generic.List[object]]::new()
$factSkipHits = [System.Collections.Generic.List[object]]::new()
$csFiles = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.cs' -Recurse -File)
$csFiles += Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\tests' -Filter '*.cs' -Recurse -File | Where-Object { $_.Name -match 'PluginSessionLog|PluginNative|PluginPromotion' }
$csFiles = @($csFiles | Sort-Object FullName -Unique)
foreach ($f in $csFiles) {
    $lines = Get-Content -LiteralPath $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '\[Skip|Fact\(Skip|Theory\(Skip|Assert\.Skip') {
            $skipHits.Add([ordered]@{ File = $f.FullName; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'PluginNativeSuite_|PluginInt_P19_') {
            $p19Hits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero|PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag') {
            $p20Hits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match '\[Fact' -or $line -match '\[Theory') {
            $factSkipHits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
    }
}

$p19TestFacts = @()
if (Test-Path -LiteralPath $p19Tests) {
    $p19Lines = Get-Content -LiteralPath $p19Tests
    for ($i = 0; $i -lt $p19Lines.Count; $i++) {
        if ($p19Lines[$i] -match '\[Fact|\[Theory|public void Plugin') {
            $p19TestFacts += [ordered]@{ Line = ($i + 1); Text = $p19Lines[$i].Trim() }
        }
    }
}

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    SkipHitCount = $skipHits.Count
    SkipHits = @($skipHits)
    P19HitCount = $p19Hits.Count
    P19Hits = @($p19Hits)
    P20HitCount = $p20Hits.Count
    P20Hits = @($p20Hits)
    P19FactLines = $p19TestFacts
    NamedEach = (@($p19Hits | Where-Object { $_.Text -match 'PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero' })).Count
    NamedAfterSync = (@($p19Hits | Where-Object { $_.Text -match 'PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero' })).Count
    NamedGit = (@($p19Hits | Where-Object { $_.Text -match 'PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit' })).Count
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'p19-p20-skip-grep.json') -Encoding utf8

$p19Dirs = @()
if (Test-Path -LiteralPath $receiptsRoot) {
    $p19Dirs = @(Get-ChildItem -LiteralPath $receiptsRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'pluginint-p19-*' } |
        ForEach-Object {
            [ordered]@{
                Name = $_.Name
                FullName = $_.FullName
                LastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o')
                FileCount = @(Get-ChildItem -LiteralPath $_.FullName -Recurse -File -ErrorAction SilentlyContinue).Count
                MatchesStamp = [bool]($_.Name -match '^pluginint-p19-\d{8}T\d{6}Z$')
            }
        })
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    PluginIntP19DirectoryCount = $p19Dirs.Count
    MatchingStampCount = @($p19Dirs | Where-Object { $_.MatchesStamp }).Count
    Directories = $p19Dirs
    ThisReviewDidNotCreatePluginIntP19 = $true
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'pluginint-p19-dirs.json') -Encoding utf8

$catalogNames = @()
$catalogEnabledCount = 0
if (Test-Path -LiteralPath $catalogJson) {
    $cat = Get-Content -LiteralPath $catalogJson -Raw | ConvertFrom-Json
    $enabled = @($cat.scenarios | Where-Object { $_.enabled -eq $true })
    $catalogEnabledCount = $enabled.Count
    $catalogNames = @($enabled | ForEach-Object { [string]$_.repositoryName })
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    EnabledCount = $catalogEnabledCount
    RepositoryNames = $catalogNames
    ExpectedEight = @(
        'mcpserver-codex-plugin'
        'mcpserver-claude-code-plugin'
        'mcpserver-claude-cowork-plugin'
        'mcpserver-copilot-plugin'
        'mcpserver-grok-plugin'
        'mcpserver-cline-plugin'
        'mcpserver-cline-v2-plugin'
        'mcpserver-opencode-plugin'
    )
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'catalog-names.json') -Encoding utf8

$loaderText = if (Test-Path -LiteralPath $p19Loader) { Get-Content -LiteralPath $p19Loader -Raw } else { '' }
$testsText = if (Test-Path -LiteralPath $p19Tests) { Get-Content -LiteralPath $p19Tests -Raw } else { '' }
$pin = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    LoaderExists = (Test-Path -LiteralPath $p19Loader)
    TestsExist = (Test-Path -LiteralPath $p19Tests)
    StampPatternLiteral = [bool]($loaderText -match 'pluginint-p19-\\d\{8\}T\\d\{6\}Z')
    LoadLatestThrowsWhenNoMatchingDir = [bool]($loaderText -match 'No docs/receipts/pluginint-p19-<utc> receipt directory exists')
    LoadLatestRequiresSummaryJson = [bool]($loaderText -match 'P19 summary.json is missing')
    ValidateRowsRequiresRepositoryName = [bool]($loaderText -match 'row.RepositoryName')
    ValidateRowsRequiresNativeSuite = [bool]($loaderText -match 'row.NativeSuite')
    AllowedSuitesPesterBatsJestXunit = [bool]($loaderText -match '"Pester"' -and $loaderText -match '"Bats"' -and $loaderText -match '"Jest"' -and $loaderText -match '"xUnit"')
    ValidateRowsRequiresLogFile = [bool]($loaderText -match 'row.LogFile')
    ValidateRowsChecksFailedSkipped = [bool]($loaderText -match 'ValidateRows[\s\S]{0,800}row\.Failed' -or $loaderText -match 'if \(row\.Failed')
    LoadLatestRequiresBranch = [bool]($loaderText -match 'P19 receipt is missing git.branch')
    LoadLatestRequiresSha40Hex = [bool]($loaderText -match 'git.sha must be a 40-character')
    ShaRegexIgnoreCase = [bool]($loaderText -match 'ShaPattern[\s\S]{0,80}IgnoreCase')
    UnrelatedCommitCountDefaultsZero = [bool]($loaderText -match 'public int UnrelatedCommitCount')
    TestsRequireEightCatalog = [bool]($testsText -match 'Assert.Equal\(8, catalog.Count\)')
    TestsRequireEightPluginRows = [bool]($testsText -match 'Assert.Equal\(8, receipt.Plugins.Count\)')
    TestsRequireEightAfterSyncRows = [bool]($testsText -match 'Assert.Equal\(8, receipt.AfterSync.Count\)')
    TestsMatchCatalogRepositoryName = [bool]($testsText -match 'scenario.RepositoryName')
    TestsAssertFailedZero = ([regex]::Matches($testsText, 'Assert.Equal\(0, row.Failed\)')).Count
    TestsAssertSkippedZero = ([regex]::Matches($testsText, 'Assert.Equal\(0, row.Skipped\)')).Count
    TestsRequireLogExists = [bool]($testsText -match 'native suite log missing' -and $testsText -match 'after-sync log missing')
    TestsParseFailedSkippedFromLog = [bool]($testsText -match 'ParseFailedSkipped')
    TestsRequireGitJson = [bool]($testsText -match 'P19 git.json is missing')
    TestsRequireGitJsonContainsBranchShaUnrelated = [bool]($testsText -match 'Assert.Contains\(receipt.Branch' -and $testsText -match 'Assert.Contains\(receipt.Sha' -and $testsText -match 'unrelatedCommitCount')
    TestsShaLowercaseOnly = [bool]($testsText -match 'Assert.Matches\("\^\[0-9a-f\]\{40\}\$"')
    TestsCallLoadLatest = ([regex]::Matches($testsText, 'PluginNativeSuiteReceipt.LoadLatest')).Count
    EmptyFolderWouldFailMissingSummary = $true
    NoMatchingDirWouldThrowFileNotFound = $true
    NamedMethods = @(
        'PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero'
        'PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero'
        'PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit'
    )
    NamedMethodsPresent = @(
        [bool]($testsText -match 'void PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero')
        [bool]($testsText -match 'void PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero')
        [bool]($testsText -match 'void PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit')
    )
}
$pin | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'a3-pin-analysis.json') -Encoding utf8

$prior = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    MdExists = (Test-Path -LiteralPath $priorAgree)
    JsonExists = (Test-Path -LiteralPath $priorAgreeJson)
}
if ($prior.MdExists) {
    $md = Get-Item -LiteralPath $priorAgree
    $mdText = Get-Content -LiteralPath $priorAgree -Raw
    $prior.MdLastWriteTimeUtc = $md.LastWriteTimeUtc.ToString('o')
    $prior.MdLength = $md.Length
    $prior.MdOverallVerdictAgree = [bool]($mdText -match 'OverallVerdict:\s*AGREE')
    $prior.MdPhase = 'C-green-P16-P18'
}
if ($prior.JsonExists) {
    $js = Get-Content -LiteralPath $priorAgreeJson -Raw | ConvertFrom-Json
    $prior.JsonOverallVerdict = [string]$js.OverallVerdict
    $prior.JsonFailCount = $js.FailCount
    $prior.JsonPhase = [string]$js.Phase
    $prior.JsonTimestampUtc = [string]$js.TimestampUtc
}
$prior | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'prior-receipts.json') -Encoding utf8

$planText = if (Test-Path -LiteralPath $plan) { Get-Content -LiteralPath $plan -Raw } else { '' }
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    HasCRedP19ThenExecute = [bool]($planText -match 'C-red-P19 AGREE, then P19 execute, then C-green-P19 AGREE')
    HasNamedEach = [bool]($planText -match 'PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero')
    HasNamedAfterSync = [bool]($planText -match 'PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero')
    HasNamedGit = [bool]($planText -match 'PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit')
    HasP20Harness = [bool]($planText -match 'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero')
    HasP20Promotion = [bool]($planText -match 'PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag')
    HasEvidenceFolder = [bool]($planText -match 'docs/receipts/pluginint-p19-<utc>/')
    HasFailUntilReceipts = [bool]($planText -match 'named tests fail until those TRX/Pester receipts exist')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plan-p19.json') -Encoding utf8

try {
    $gitStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceipt.cs 2>&1 | Out-String
    $gitBranch = git -C 'F:\GitHub\McpServer' rev-parse --abbrev-ref HEAD 2>&1 | Out-String
    $gitSha = git -C 'F:\GitHub\McpServer' rev-parse HEAD 2>&1 | Out-String
} catch {
    $gitStatus = $_.Exception.ToString()
    $gitBranch = ''
    $gitSha = ''
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Porcelain = $gitStatus.Trim()
    Branch = $gitBranch.Trim()
    Sha = $gitSha.Trim()
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'git-status.json') -Encoding utf8

$py = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match '^(python|python3|py)\.exe$' -or ($_.CommandLine -and $_.CommandLine -match '(?i)python(\.exe)?\s')
} | ForEach-Object {
    [ordered]@{ Pid = $_.ProcessId; Name = $_.Name; CommandLine = $_.CommandLine }
})
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    PythonProcessCount = $py.Count
    Processes = $py
    ValidatorInvokedPython = $false
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'python-procs.json') -Encoding utf8

$csprojText = if (Test-Path -LiteralPath $csproj) { Get-Content -LiteralPath $csproj -Raw } else { '' }
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    CompileRemoveNativeSuite = [bool]($csprojText -match 'PluginNativeSuite')
    EnableDefaultCompileItemsLikely = [bool]($csprojText -notmatch 'EnableDefaultCompileItems>\s*false')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'csproj-include.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
