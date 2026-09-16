#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$worktree = 'C:\Users\kingd\.grok\worktrees\github-mcpserver\subagent-01a029d1-24e7-7453-b027-93176ddfd033'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hostile-g-red-20260822T151548Z'
$csproj = Join-Path $worktree 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$testFile = Join-Path $worktree 'tests\McpServer.Support.Mcp.Tests\Services\WikiDumpPhaseGTests.cs'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$section11 = @(
    'WikiExport_WithoutDumpFlag_UnchangedBehavior',
    'WikiExport_WithDumpFlag_WritesVersionedJsonKeyedByWorkspace',
    'Dump_ContainsTodoRowsAndRequirementLinksMatchingStore',
    'Dump_Sha256_MatchesCanonicalUtf8Json',
    'AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml',
    'Import_RemapsWorkspaceIdAndPaths_OldIdAbsent',
    'Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected',
    'Import_IdempotentReimport_NoDuplicateTodos',
    'TodoYaml_NotSourceOfTruth_WhenDumpPresent',
    'TodoYaml_Cleanup_ArchivesWithEvidence_NoSilentDelete',
    'DumpAndTodoYaml_Conflict_DumpWins_DiagnosticNamesBoth'
)
$g0Extra = 'CreateAsync_DumpParam_BoundOnWorkspaceCreateRequest'
$allNames = $section11 + @($g0Extra)

$testText = Get-Content -LiteralPath $testFile -Raw
$onDisk = foreach ($name in $allNames) {
    [ordered]@{
        name = $name
        onDisk = $testText.Contains("public async Task $name(") -or $testText.Contains("public void $name(")
        factAttribute = [regex]::IsMatch($testText, "(?s)\[Fact[^\]]*\]\s*public (async Task|void) $name\(")
    }
}

$filter = ($allNames | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
$logPath = Join-Path $outDir 'hostile-dotnet-test.txt'
$trxName = 'hostile-g-red.trx'

$srcHits = @()
$srcPatterns = @('includeDump', 'IncludeDump', 'include-dump', 'mcp-wiki-dump', 'DumpParam', 'archive/todo-yaml')
foreach ($pat in $srcPatterns) {
    $hits = Get-ChildItem -LiteralPath (Join-Path $worktree 'src') -Recurse -Filter *.cs |
        Select-String -Pattern $pat -SimpleMatch |
        Select-Object -First 20
    foreach ($h in $hits) {
        $srcHits += [ordered]@{ Pattern = $pat; Path = $h.Path; Line = $h.LineNumber; Text = $h.Line.Trim() }
    }
}

$createDump = Select-String -Path (Join-Path $worktree 'src\McpServer.Client\Models\WorkspaceModels.cs') -Pattern 'Dump' -SimpleMatch
$serviceDump = Select-String -Path (Join-Path $worktree 'src\McpServer.Services\Services\IWorkspaceService.cs') -Pattern 'Dump' -SimpleMatch
$fedDump = Select-String -Path (Join-Path $worktree 'src\McpServer.Client\Models\FederationModels.cs') -Pattern 'Dump' -SimpleMatch
$wikiSig = Select-String -Path (Join-Path $worktree 'src\McpServer.Services\Requirements\IRequirementsDocumentService.cs') -Pattern 'GenerateWikiAsync'

Push-Location $worktree
try {
    $buildLog = Join-Path $outDir 'hostile-dotnet-build.txt'
    $buildArgs = @('build', $csproj, '-c', 'Debug', '--no-incremental')
    $buildOut = & dotnet @buildArgs 2>&1 | Tee-Object -FilePath $buildLog
    $buildExit = $LASTEXITCODE

    $testExit = $null
    if ($buildExit -eq 0) {
        $testArgs = @(
            'test', $csproj,
            '-c', 'Debug',
            '--no-build',
            '--filter', $filter,
            '--logger', "trx;LogFileName=$trxName",
            '--results-directory', $outDir,
            '-v', 'n'
        )
        $testOut = & dotnet @testArgs 2>&1 | Tee-Object -FilePath $logPath
        $testExit = $LASTEXITCODE
    } else {
        "BUILD FAILED EXIT=$buildExit`n$($buildOut | Out-String)" | Set-Content -LiteralPath $logPath -Encoding utf8
        $testOut = $buildOut
        $testExit = $buildExit
    }
}
finally {
    Pop-Location
}

$trxPath = Get-ChildItem -LiteralPath $outDir -Filter '*.trx' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
$counters = $null
$outcomes = @()
if ($trxPath) {
    [xml]$trx = Get-Content -LiteralPath $trxPath.FullName
    $c = $trx.TestRun.ResultSummary.Counters
    $counters = [ordered]@{
        total = [int]$c.total
        executed = [int]$c.executed
        passed = [int]$c.passed
        failed = [int]$c.failed
        error = [int]$c.error
        timeout = [int]$c.timeout
        aborted = [int]$c.aborted
        inconclusive = [int]$c.inconclusive
        notExecuted = [int]$c.notExecuted
        skipped = if ($null -ne $c.notExecuted) { [int]$c.notExecuted } else { 0 }
    }
    $results = @($trx.TestRun.Results.UnitTestResult)
    foreach ($r in $results) {
        $msg = ''
        if ($r.Output -and $r.Output.ErrorInfo -and $r.Output.ErrorInfo.Message) {
            $msg = [string]$r.Output.ErrorInfo.Message
        }
        $outcomes += [ordered]@{
            testName = [string]$r.testName
            outcome = [string]$r.outcome
            duration = [string]$r.duration
            message = $msg.Substring(0, [Math]::Min(400, $msg.Length))
        }
    }
}

$summary = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Worktree = $worktree
    Csproj = $csproj
    TestFile = $testFile
    Filter = $filter
    NamedOnDisk = @($onDisk)
    NamedCount = @($onDisk | Where-Object { $_.onDisk }).Count
    FactCount = @($onDisk | Where-Object { $_.factAttribute }).Count
    BuildExit = $buildExit
    TestExit = $testExit
    TrxPath = if ($trxPath) { $trxPath.FullName } else { $null }
    Counters = $counters
    Outcomes = @($outcomes)
    SrcHits = @($srcHits)
    WorkspaceModelsDumpHits = @($createDump | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" })
    ServiceCreateRequestDumpHits = @($serviceDump | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" })
    FederationDumpHits = @($fedDump | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" })
    GenerateWikiAsyncLines = @($wikiSig | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" })
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'hostile-run-summary.json') -Encoding utf8
Write-Output ($summary | ConvertTo-Json -Depth 8)
exit $(if ($null -ne $testExit) { $testExit } else { $buildExit })
