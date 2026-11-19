#Requires -Version 7.0
<#
.SYNOPSIS
Creates a finite, source-pinned design roster from the existing identity consumer search.
.DESCRIPTION
2026-11-19: TASK-FEDPH0-004 B4; FR-MCP-182 ac-TwoNodesCreateDistinctIds/ac-LegacyIdsRemainValid;
TR-MCP-FEDERATION-009; TEST-MCP-230. Preserves the original32 reviewed and215 candidate denominator.
Checks source hashes and exact retained line anchors. Category dispositions are design proposals,
not semantic review, dynamic caller closure, runtime compatibility, native proof or Task readiness.
.PARAMETER WorkspaceRoot
Isolated source checkout to read. No source, service or database is modified.
.PARAMETER OutputPath
New JSON receipt to create. Existing receipts are never replaced.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$WorkspaceRoot,
    [Parameter(Mandatory)][string]$OutputPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$catalogRoot = Join-Path $WorkspaceRoot 'docs/receipts/federation/20261117-phase0-source-catalog'
$candidates = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText((Join-Path $catalogRoot 'identity-consumer-candidates.json'))) -AsHashtable
$review = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText((Join-Path $catalogRoot 'identity-consumer-review.json'))) -AsHashtable
$reviewByPath = @{}
foreach ($entry in $review.files) { $reviewByPath[$entry.path] = $entry }
$dispositions = @{
    FixedDigitGrammar = 'Use central generated parser only at the versioned boundary; retain exact legacy/standalone validation and scoped aliases.'
    RequirementGrammar = 'Carry canonical identity separately from descriptive AC/human identities; parsed registered family supplies metadata.'
    TodoGrammar = 'Use explicit generated-versus-human mode and registered provenance; never infer ownership from suffix shape.'
    IdentifierSplit = 'Use parsed semantic family metadata; creator tag is not a positional area/subarea.'
    NumericIdDto = 'Preserve existing standalone numeric DTOs; versioned identity-bearing surfaces use additive canonical-string DTOs and typed legacy references, with capability refusal before mutation.'
    NumericParse = 'Do not globally convert numeric parsing. Retain operational counters/process/protocol values; any actual versioned identity consumer uses the central boundary. Individual semantic attribution remains explicit.'
    IdOrdering = 'Distinguish numeric identity presentation order from ordinal deterministic serialization; retain exact strings, duplicate entries and source order where required.'
    IdLength = 'Preserve standalone limits; explicitly validate new canonical-key/reference length and provider/collation limits before adoption; never truncate or cast.'
}
$rows = foreach ($entry in $candidates.files) {
    $path = Join-Path $WorkspaceRoot $entry.path
    $exists = Test-Path -LiteralPath $path -PathType Leaf
    $sourceText = if ($exists) { [IO.File]::ReadAllText($path) } else { '' }
    $sourceLines = $sourceText -split '\r?\n'
    $currentHash = if ($exists) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash } else { $null }
    $hits = foreach ($hit in $entry.hits) {
        $anchorMatches = $exists -and $hit.line -ge 1 -and $hit.line -le $sourceLines.Count -and $sourceLines[$hit.line - 1] -ceq $hit.text
        if (-not $dispositions.ContainsKey($hit.category)) { throw "Unaccounted source category: $($hit.category)" }
        [ordered]@{ category = $hit.category; line = $hit.line; sourceText = $hit.text; currentLineMatches = $anchorMatches; proposedBoundaryDisposition = $dispositions[$hit.category] }
    }
    $manuallyReviewed = $reviewByPath.ContainsKey($entry.path)
    [ordered]@{
        path = $entry.path; catalogSha256 = $entry.sha256; currentSha256 = $currentHash
        sourceExists = $exists; catalogHashMatches = $currentHash -ceq $entry.sha256
        originalManualReview = $manuallyReviewed
        manualReviewRecord = if ($manuallyReviewed) { $reviewByPath[$entry.path] } else { $null }
        manualReviewState = if ($manuallyReviewed) { 'OriginalReviewedRecordRetained' } else { 'RosteredNotIndividuallyReviewed' }
        runtimeOwner = if ($entry.path -like 'plugins/core/lib-node/*') { 'RetiredNodeRuntime: retire only; no new Node implementation' } else { 'Owning existing source component; future versioned adapter is additive' }
        hits = @($hits); semanticConsumerClosure = $false; runtimeProof = $false
    }
}
$allHits = @($rows | ForEach-Object { $_.hits })
$summary = [ordered]@{
    candidateFiles = @($rows).Count; candidateHits = $allHits.Count
    originalManuallyReviewedFiles = @($review.files).Count
    candidateManuallyReviewedFiles = @($rows | Where-Object originalManualReview).Count
    supplementalReviewedFiles = @($review.files | Where-Object path -NotIn $candidates.files.path).Count
    rosteredNotIndividuallyReviewedFiles = @($rows | Where-Object { -not $_.originalManualReview }).Count
    sourceHashMismatches = @($rows | Where-Object { -not $_.catalogHashMatches }).Count
    missingSources = @($rows | Where-Object { -not $_.sourceExists }).Count
    lineAnchorMismatches = @($allHits | Where-Object { -not $_.currentLineMatches }).Count
}
if ($summary.candidateFiles -ne 215 -or $summary.candidateHits -ne 918 -or $summary.originalManuallyReviewedFiles -ne 32) {
    throw 'Original finite consumer denominator changed; reconcile rather than silently changing it.'
}
$result = [ordered]@{
    capturedLocal = Get-Date -Format s; taskId = 'TASK-FEDPH0-004'; sourceHead = (& git -C $WorkspaceRoot rev-parse HEAD)
    sourceCatalog = 'docs/receipts/federation/20261117-phase0-source-catalog/identity-consumer-candidates.json'
    sourceCatalogSha256 = (Get-FileHash -LiteralPath (Join-Path $catalogRoot 'identity-consumer-candidates.json')).Hash
    originalReviewSha256 = (Get-FileHash -LiteralPath (Join-Path $catalogRoot 'identity-consumer-review.json')).Hash
    summary = $summary; categories = $dispositions; files = @($rows)
    supplementalReviewedRecords = @($review.files | Where-Object path -NotIn $candidates.files.path)
    completeFiniteDesignRoster = $true; completeSemanticConsumerReview = $false; dynamicCallerClosure = $false
    externalInstalledPluginClosure = $false; nativeProof = $false; taskReady = $false
    limitations = @('The215source candidates are a fixed lexical denominator, not an exhaustive transitive caller graph.',
        'All32original manual-review records are retained:31match candidate files and one is supplemental canonical documentation. The other184candidate files remain explicitly unreviewed.',
        'Hash/line matches establish source anchoring only. Category proposals do not certify semantic compatibility.',
        'Installed plugin/UI/remote runtime bindings and native allocator/parser/provider behavior remain after the implementation barrier.')
}
if (Test-Path -LiteralPath $OutputPath) { throw 'Receipt already exists; retain it and choose a new path.' }
if ($PSCmdlet.ShouldProcess($OutputPath, 'Create source-pinned finite consumer design roster')) {
    $var1 = $result | ConvertTo-Json -Depth 40
    Add-LinesToFile -Path $OutputPath -Content $var1 | Out-Null
}
[pscustomobject]$summary
