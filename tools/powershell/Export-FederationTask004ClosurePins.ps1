#Requires -Version 7.0
<#
.SYNOPSIS
Writes immutable source and receipt pins for the bounded Task004 design/mock candidate.
.DESCRIPTION
2026-11-19: TASK-FEDPH0-004; FR-MCP-182 ac-TwoNodesCreateDistinctIds/ac-LegacyIdsRemainValid;
FR-MCP-184 ac-BackgroundWritersDoNotDirtyVersionedBranch; TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.
Reads source and retained mock evidence only. No product, native, service, database or task-state mutation.
.PARAMETER WorkspaceRoot
Isolated source checkout containing the final candidate.
.PARAMETER OutputPath
New receipt; an existing file is never replaced.
#>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$WorkspaceRoot,[Parameter(Mandatory)][string]$OutputPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $OutputPath) { throw 'Refusing to overwrite pins.' }
$classes = @('Phase0IdentityIsolationMockGateTests','Phase0IdentityLifecycleMockGateTests','Phase0IdentityReconciliationMockGateTests','Phase0IdentityDiagnosticsMockGateTests','Phase0IdentityGrammarMockGateTests','Phase0AutomaticWriterMockGateTests','Phase0AllocatorSafetyMockGateTests','Phase0IdentityDtoBoundaryMockGateTests','Phase0ReferenceManifestMockGateTests')
$counts = @(270,137,466,83,9973,5724,375,272,13308)
$sourceRoot = Join-Path $WorkspaceRoot 'tests/QBrainAi.Support.Mcp.Tests/Federation/Phase0'
$selectedSources = foreach ($className in $classes) {
    $stem = $className.Replace('MockGateTests','')
    Get-ChildItem -LiteralPath $sourceRoot -Filter "$stem*.cs"
}
$paths = [Collections.Generic.List[string]]::new()
foreach ($file in $selectedSources) { $paths.Add($file.FullName) }
foreach ($relative in @('tests/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.csproj','docs/plans/2026-11-19-federation-task004-readiness.md','docs/plans/2026-11-17-federation-identity-design.md','tools/powershell/Join-FederationConsumerSemanticReviews.ps1','tools/powershell/Export-FederationTask004ClosurePins.ps1')) { $paths.Add((Join-Path $WorkspaceRoot $relative)) }
$receiptRoot = Join-Path $WorkspaceRoot 'docs/receipts/federation/20261119-task004-readiness'
foreach ($file in Get-ChildItem -LiteralPath $receiptRoot -File | Where-Object { $_.Name -notlike '*.log' -and $_.Name -ne 'windows-allocator-final.txt' -and $_.Name -ne 'inspect-consumer-roster.ps1' }) { $paths.Add($file.FullName) }
$hashes = foreach ($path in @($paths | Sort-Object -Unique)) { [ordered]@{path = [IO.Path]::GetRelativePath($WorkspaceRoot,$path).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $path).Hash; bytes = (Get-Item -LiteralPath $path).Length} }
$dtoTrxPath = Join-Path $receiptRoot 'windows-dto-consumers.trx'
$dtoTrx = [xml][IO.File]::ReadAllText($dtoTrxPath)
$results = @($dtoTrx.SelectNodes('//*[local-name()="UnitTestResult"]'))
if ($results.Count -ne 272 -or @($results | Where-Object outcome -NE 'Passed').Count -ne 0) { throw 'Final DTO run is not272passed/0other.' }
$zipPins = foreach ($pair in @(@('windows-dto-issue.log.zip','build.2026-11-19_11-23-26.log'),@('windows-dto-consumers.log.zip','build.2026-11-19_11-35-31.log'))) {
    $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $receiptRoot $pair[0]))
    try {
        if ($archive.Entries.Count -ne 1) { throw 'Expected single raw-log ZIP entry.' }
        $stream = $archive.Entries[0].Open()
        try { $entryHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
        $originalPath = Join-Path $WorkspaceRoot ".nuke/temp/$($pair[1])"
        if ($entryHash -cne (Get-FileHash -LiteralPath $originalPath).Hash) { throw 'ZIP entry differs from retained original.' }
        [ordered]@{zip = $pair[0]; original = ".nuke/temp/$($pair[1])"; rawSha256 = $entryHash; originalRetained = $true}
    } finally { $archive.Dispose() }
}
$caseClasses = for ($classIndex = 0; $classIndex -lt $classes.Count; $classIndex++) { [ordered]@{className = $classes[$classIndex]; expectedCases = $counts[$classIndex]} }
$payload = [ordered]@{schemaVersion = 1; taskId = 'TASK-FEDPH0-004'; timestampLocal = Get-Date -Format s; sourceHeadBeforeCommit = (git -C $WorkspaceRoot rev-parse HEAD).Trim(); agent = 'Codex'; model = 'gpt-6.1-sol'; effort = 'xhigh'; sessionId = 'Codex-20261119T153852Z-plugin-session'; requestId = 'req-20261119T171650Z-prompt-b74a'; parentSessionId = 'Codex-20261116T174245Z-plugin-session'; parentRequestId = 'req-20261119T171310Z-prompt-5c2d'; classes = @($caseClasses); expectedCases = 30608; selector = "QBrainAi.Support.Mcp.Tests:$($classes -join ',')"; integratedCaseNameHash = $null; integratedCaseNameHashOwner = 'Parent computes ordinal case-name hash from actual integrated Windows TRX and compares actual Linux roster.'; finalDtoRun = '20261119T113400-task004-dto-consumers'; finalDtoPassed = 272; finalDtoFailed = 0; finalDtoSkipped = 0; hashes = @($hashes); zipEntryPins = @($zipPins); nativeProof = $false; taskStateChanged = $false; isHV = $false; limitations = @('Sources and retained per-class evidence are pinned, not claimed as one new cumulative execution.','Shared Task002/005 provider/migration/relational-model evidence remains separate.','Parent owns integrated paired-host gate, live definition review, Ready and all-nine pause. No native implementation starts here.')}
if ($PSCmdlet.ShouldProcess($OutputPath,'Create verified Task004 source/receipt pins')) {
    $var1 = ($payload | ConvertTo-Json -Depth 30).TrimEnd()
    Add-LinesToFile -Path $OutputPath -Content $var1 | Out-Null
    Get-FileHash -LiteralPath $OutputPath | Select-Object Path,Hash
}