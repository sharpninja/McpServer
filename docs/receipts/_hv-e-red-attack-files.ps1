#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$worktree = 'C:\Users\kingd\.grok\worktrees\github-mcpserver\subagent-01a029d1-24e5-7603-879b-832cfd4fe7f7'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z'
[void][System.IO.Directory]::CreateDirectory($outDir)

$planNames = @(
    'HostileReviewEntity_RoundTrip_SqlitePgSqlServer'
    'HostileReviewSubmit_ValidRequest_CreatesQueueItem'
    'HostileReviewSubmit_OversizedPayload_Rejected'
    'HostileReviewSubmit_ForeignWorkspace_403'
    'HostileReviewSubmit_MissingArtifact_ReturnsDiagnosticNotSilentOmit'
    'HostileReviewSubmit_StaleOrUnauthorizedLink_Diagnostic'
    'HostileReviewSubmit_AmbiguousLink_Diagnostic'
    'HostileReviewExecution_RecordsModelEffortAgentTemplateRunId'
    'HostileReviewExecution_OmitsFabricatedTokenCounts'
    'HostileReviewGet_NormalizedFindings_TaxonomyComplete'
    'HostileReview_RequestQuality_ScoresDisclosureScopeObjectiveConfidenceContext'
    'HostileReviewQuery_ByModelAndEffort_ReturnsOnlyMatchingRuns'
    'HostileReviewQuery_ByRequesterAndTargetType_AndFilters'
    'HostileReviewQuery_NoMatch_EmptyList'
    'HostileReview_Default_DoesNotMutateProductFiles'
    'HostileReview_SurfaceParity_RestReplDirectorPlugin'
)

$testRoot = Join-Path $worktree 'tests'
$nameHits = [ordered]@{}
foreach ($name in $planNames) {
    $files = @(Get-ChildItem -LiteralPath $testRoot -Recurse -Filter '*.cs' -File | Select-String -Pattern ("\b" + [regex]::Escape($name) + "\b") -List)
    $methodHits = @()
    foreach ($f in $files) {
        $lines = @(Select-String -LiteralPath $f.Path -Pattern ("(public|internal|protected|private).*\b" + [regex]::Escape($name) + "\s*\("))
        $methodHits += @($lines | ForEach-Object { '{0}:{1}:{2}' -f $_.Path, $_.LineNumber, $_.Line.Trim() })
    }
    $nameHits[$name] = [ordered]@{
        fileCount   = $files.Count
        files       = @($files | ForEach-Object { $_.Path })
        methodHits  = $methodHits
        methodCount = $methodHits.Count
    }
}

$productPaths = @(
    'src\McpServer.Support.Mcp\Controllers\HostileReviewController.cs'
    'src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.HostileReview.cs'
    'src\McpServer.Repl.Core\HostileReviewCommandShapes.cs'
    'src\McpServer.Cqrs.Mvvm\HostileReviewDirectorCommands.cs'
    'src\McpServer.Client\HostileReviewClient.cs'
    'plugins\core\skills\hostile-review\SKILL.md'
)
$productExists = [ordered]@{}
foreach ($rel in $productPaths) {
    $full = Join-Path $worktree $rel
    $productExists[$rel] = [System.IO.File]::Exists($full)
}

$dbContextPath = Join-Path $worktree 'src\McpServer.Storage\McpDbContext.cs'
$dbHits = @()
if (Test-Path -LiteralPath $dbContextPath) {
    $dbHits = @(Select-String -LiteralPath $dbContextPath -Pattern 'HostileReview' | ForEach-Object { '{0}:{1}:{2}' -f $_.Path, $_.LineNumber, $_.Line.Trim() })
}

$storageHits = @()
$storageRoot = Join-Path $worktree 'src\McpServer.Storage'
if (Test-Path -LiteralPath $storageRoot) {
    $storageHits = @(Get-ChildItem -LiteralPath $storageRoot -Recurse -Include *.cs -File | Select-String -Pattern 'HostileReview' | ForEach-Object { '{0}:{1}:{2}' -f $_.Path, $_.LineNumber, $_.Line.Trim() })
}

$fwhPath = Join-Path $worktree 'src\McpServer.Support.Mcp\McpStdio'
$fwhHits = @()
if (Test-Path -LiteralPath $fwhPath) {
    $fwhHits = @(Get-ChildItem -LiteralPath $fwhPath -Filter '*.cs' -File | Select-String -Pattern 'HostileReview' | ForEach-Object { '{0}:{1}:{2}' -f $_.Path, $_.LineNumber, $_.Line.Trim() })
}

$controllerHits = @()
$controllerRoot = Join-Path $worktree 'src\McpServer.Support.Mcp\Controllers'
if (Test-Path -LiteralPath $controllerRoot) {
    $controllerHits = @(Get-ChildItem -LiteralPath $controllerRoot -Filter '*.cs' -File | Select-String -Pattern 'HostileReview' | ForEach-Object { '{0}:{1}:{2}' -f $_.Path, $_.LineNumber, $_.Line.Trim() })
}

$serviceRoot = Join-Path $worktree 'src\McpServer.Services\Services\HostileReview'
$serviceFiles = @()
if (Test-Path -LiteralPath $serviceRoot) {
    $serviceFiles = @(Get-ChildItem -LiteralPath $serviceRoot -Recurse -File | ForEach-Object { $_.FullName })
}

$throwHits = @()
if (Test-Path -LiteralPath $serviceRoot) {
    $throwHits = @(Get-ChildItem -LiteralPath $serviceRoot -Recurse -Filter '*.cs' -File | Select-String -Pattern 'NotImplementedException|throw new' | ForEach-Object { '{0}:{1}:{2}' -f $_.Path, $_.LineNumber, $_.Line.Trim() })
}

$restHits = @()
$srcRoot = Join-Path $worktree 'src'
$restHits = @(Get-ChildItem -LiteralPath $srcRoot -Recurse -Include *.cs -File | Select-String -Pattern 'HttpPost|\[Route\(|Map(Get|Post|Put|Delete)|/mcpserver/hostile-review' | Where-Object { $_.Path -match 'HostileReview' -or $_.Line -match 'hostile-review|HostileReview' } | ForEach-Object { '{0}:{1}:{2}' -f $_.Path, $_.LineNumber, $_.Line.Trim() })

$pluginSkillHits = @()
$pluginRoot = Join-Path $worktree 'plugins'
if (Test-Path -LiteralPath $pluginRoot) {
    $pluginSkillHits = @(Get-ChildItem -LiteralPath $pluginRoot -Recurse -Include *.md,*.ps1,*.json -File -ErrorAction SilentlyContinue | Select-String -Pattern 'hostile.review|HostileReview' | ForEach-Object { '{0}:{1}:{2}' -f $_.Path, $_.LineNumber, $_.Line.Trim() })
}

$testFile = Join-Path $worktree 'tests\McpServer.Support.Mcp.Tests\Services\HostileReviewQueueTests.cs'
$testFileExists = [System.IO.File]::Exists($testFile)
$testFactNames = @()
if ($testFileExists) {
    $testFactNames = @(Select-String -LiteralPath $testFile -Pattern 'public (async )?(Task|void) \w+' | ForEach-Object { '{0}:{1}:{2}' -f $_.LineNumber, $_.Line.Trim(), '' })
}

$report = [ordered]@{
    worktree            = $worktree
    planNameCount       = $planNames.Count
    nameHits            = $nameHits
    missingMethodNames  = @($planNames | Where-Object { $nameHits[$_].methodCount -lt 1 })
    extraMethodNote     = 'see testFactNames'
    productExists       = $productExists
    dbHits              = $dbHits
    storageHits         = $storageHits
    fwhHits             = $fwhHits
    controllerHits      = $controllerHits
    serviceFiles        = $serviceFiles
    throwHits           = $throwHits
    restHits            = $restHits
    pluginSkillHits     = $pluginSkillHits
    testFileExists      = $testFileExists
    testFactNames       = $testFactNames
}

$jsonPath = Join-Path $outDir 'a1-a3-files.json'
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output ('OUT=' + $jsonPath)
Write-Output ('MISSING_METHODS=' + (($report.missingMethodNames) -join ','))
Write-Output ('TEST_FILE_EXISTS=' + $testFileExists)
Write-Output ('SERVICE_FILES=' + ($serviceFiles -join '|'))
Write-Output ('CONTROLLER_EXISTS=' + $productExists['src\McpServer.Support.Mcp\Controllers\HostileReviewController.cs'])
Write-Output ('FWH_EXISTS=' + $productExists['src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.HostileReview.cs'])
Write-Output ('CLIENT_EXISTS=' + $productExists['src\McpServer.Client\HostileReviewClient.cs'])
Write-Output ('DIRECTOR_EXISTS=' + $productExists['src\McpServer.Cqrs.Mvvm\HostileReviewDirectorCommands.cs'])
Write-Output ('REPL_EXISTS=' + $productExists['src\McpServer.Repl.Core\HostileReviewCommandShapes.cs'])
Write-Output ('SKILL_EXISTS=' + $productExists['plugins\core\skills\hostile-review\SKILL.md'])
Write-Output ('DB_HIT_COUNT=' + $dbHits.Count)
Write-Output ('STORAGE_HIT_COUNT=' + $storageHits.Count)
Write-Output ('FWH_HIT_COUNT=' + $fwhHits.Count)
Write-Output ('CONTROLLER_HIT_COUNT=' + $controllerHits.Count)
Write-Output ('PLUGIN_HIT_COUNT=' + $pluginSkillHits.Count)
Write-Output ('THROW_HIT_COUNT=' + $throwHits.Count)
foreach ($name in $planNames) {
    Write-Output ('METHOD_' + $name + '=' + $nameHits[$name].methodCount)
}
