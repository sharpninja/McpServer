#Requires -Version 7.0
<#
.SYNOPSIS
Joins source-pinned Task004 semantic judgments without promoting them to runtime evidence.
.DESCRIPTION
2026-11-19: TASK-FEDPH0-004 B4; FR-MCP-182 ac-TwoNodesCreateDistinctIds/ac-LegacyIdsRemainValid;
TR-MCP-FEDERATION-009; TEST-MCP-230. Verifies the exact184 supplemental judgments, all215 candidate
source hashes and918 lexical anchors, and retains32 earlier targeted reviews (31 candidates plus1 document).
Inputs and originals are retained. This writes receipts only, never product, task state, services or databases.
.PARAMETER WorkspaceRoot
Isolated source tree to verify against the fixed roster and partition.
.PARAMETER ReviewPaths
Seven separately authored JSON supplements, including the controller review. The owner REPL review is defined below.
.PARAMETER ReviewHashes
Expected SHA256 values in the same order as ReviewPaths, obtained from the authoring agents.
.PARAMETER OutputDirectory
Existing receipt directory. New files are created; existing different files are never overwritten.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$WorkspaceRoot,
    [Parameter(Mandatory)][string[]]$ReviewPaths,
    [Parameter(Mandatory)][string[]]$ReviewHashes,
    [Parameter(Mandatory)][string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($ReviewPaths.Count -ne 7 -or $ReviewHashes.Count -ne 7) { throw 'Expected seven pinned supplements.' }
$receiptRoot = Join-Path $WorkspaceRoot 'docs/receipts/federation/20261119-task004-readiness'
$partitionPath = Join-Path $receiptRoot 'remaining-consumer-partition.json'
$rosterPath = Join-Path $receiptRoot 'identity-consumer-roster.json'
$originalPath = Join-Path $WorkspaceRoot 'docs/receipts/federation/20261117-phase0-source-catalog/identity-consumer-review.json'
if ((Get-FileHash -LiteralPath $partitionPath).Hash -cne '0231FABE8027B8D8289F12D6BEFB70E2210B3B48546AEB6923A4ADB10982F39F') { throw 'Partition pin differs.' }
if ((Get-FileHash -LiteralPath $rosterPath).Hash -cne '7F9E41E5FC93861D5E544767784060EABABA951C3517E26F509A9FE382F5F8DE') { throw 'Roster pin differs.' }
$partition = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($partitionPath)) -AsHashtable
$roster = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($rosterPath)) -AsHashtable
$original = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($originalPath)) -AsHashtable
$partitionById = @{}
foreach ($candidate in $partition.partitions) {
    if ($partitionById.ContainsKey($candidate.candidateId)) { throw "Duplicate partition ID: $($candidate.candidateId)" }
    $partitionById[$candidate.candidateId] = $candidate
}
if ($partitionById.Count -ne 184) { throw 'Partition denominator differs.' }
$replId = 'src/QBrainAi.Repl.Core/ReplCommandDispatcher.cs'
$replEntry = [ordered]@{
    candidateId = $replId; path = $replId
    sha256 = 'FC18C910E80078DF727A6320BD40CD92589085A0AF4D6B4A0722EBD1FC8D16CE'
    sourceCommit = '64f098b2bdd9cdd5c04a4e6eb6bf727eaf85292d'; fullSourceRead = $true
    readRanges = @('1-300','301-650','651-1000','1001-1350','1351-1700','1701-2050','2051-2400','2401-2750','2751-3100','3101-3368')
    observation = 'Full3368line read distinguishes operational session/request/triage/HV/handoff/recovery IDs, protocol1.0, method-name splitting and limits/counts from true authored TODO, memory, requirement, criterion, layer and link references. TODO DTOs carry string ID/dependency/FR/TR arrays. CurrentTodoId in triage, TodoId in AgentHelp and plan/todo context in session turns are mixed authored soft references inside operational records. Generic client passthrough evaluates mutation policy; validation runs before dispatch. Numeric JSON normalization uses Int64 before double, so canonical strings must remain strings and numeric standalone precision must remain exact. Scope parsing recognizes Global/Workspace rather than numeric identities. Requirement recovery carries an operational idempotency key plus authored item references and typed exception classification. No fixed-width identity allocator is implemented in this file.'
    b4Effect = 'Bind opted-in project reads/writes to the existing central family/parser/typed origin-scoped resolver and capability boundary before mutation. Keep standalone numeric/string DTOs, operational session/request/recovery/triage IDs, version/order/count fields, audit text, source paths and transport version unchanged. Carry owning immutable project, entity kind and origin with authored references inside mixed operational payloads; resolve links without rewriting historical text or importing live session writers into project history. Preserve Global memory routing and method mutation-policy checks; generic client dispatch is not a SQL or shell bypass. Layer key projection, requirement metadata, recovery item IDs and TODO child/link arrays use the same approved semantic boundary; do not infer creator from positional splitting or suffix shape. Retain existing error classification and approved unsupported contract/capability outcomes. Later disposable bindings invoke schema/typed dispatch, independent stored reads and reopen using the same frozen cases; source judgment is not native/plugin compatibility proof.'
    anchors = @('63-70 transport timeout/version and JSON converters','181-276 validation and namespace dispatch','345-548 operational session lifecycle and mixed planFile/TodoId refs','658-853 verbatim session fields and operational query counts','956-1125 TODO string refs and guarded writer entrypoints','1128-1215 memory scope/identity versus version counter','1218-1607 triage/HV/handoff/AgentHelp operational identities with mixed TODO refs','1609-1782 GraphRAG operational graph and query counts','1784-2017 typed requirement CRUD/AC/layer/mapping/recovery with operational idempotency','2019-2072 method namespace split and mutation-policy guarded passthrough','2120-2194 value/typed DTO conversion','2234-2400 TODO and memory builders plus CurrentTodoId and AgentHelp TodoId','2466-2560 scope parser and precision-preserving Int64 JSON normalization','2623-2730 numeric limits and string primitives','2784-2900 ingest document paths/content and string reference arrays','2928-3003 subtask content and Done fields','3005-3085 streaming request identity and sequence counter','3088-3112 string ID delete/mapping receipts','3139-3199 TODO identity/link adapters','3208-3224 hello protocol capabilities, not canonical identity adoption','3292-3367 mutation policy and existing classified errors')
    classification = 'MixedTypedDispatchIdentityConsumerWithOperationalCountersAndMethodSplitFalsePositive'
    nativeProof = $false
}
$replReview = [ordered]@{
    schemaVersion = 1; artifactType = 'Task004ConsumerSemanticDisposition'; taskId = 'TASK-FEDPH0-004'; partition = 'ReplSupplement'
    sourceCommit = '64f098b2bdd9cdd5c04a4e6eb6bf727eaf85292d'; verifiedUnchangedAt = 'dfea410831e88dc7e963ed5e1d4501f7de13648c'
    sourceWorktree = $WorkspaceRoot; agent = 'Codex'; model = 'gpt-6.1-sol'; effort = 'xhigh'
    sessionId = 'Codex-20261119T153852Z-plugin-session'; requestId = 'req-20261119T171650Z-prompt-b74a'
    parentSessionId = 'Codex-20261116T174245Z-plugin-session'; parentRequestId = 'req-20261119T171310Z-prompt-5c2d'
    reviewedFileCount = 1; fullSourceReadCount = 1; sourceHashMismatchCount = 0; entries = @($replEntry)
    productFilesModified = $false; taskStateChanged = $false; testsRun = $false; nativeProof = $false
    limitations = @('Full-source semantic judgment only; no actual native dispatch or installed-plugin invocation.','Finish readiness only; parent owns all-nineReady review and pause.')
}
$reviewInputs = [Collections.Generic.List[object]]::new()
$reviewsById = @{}
for ($reviewIndex = 0; $reviewIndex -lt $ReviewPaths.Count; $reviewIndex++) {
    $reviewPath = $ReviewPaths[$reviewIndex]
    $hash = (Get-FileHash -LiteralPath $reviewPath).Hash
    if ($hash -cne $ReviewHashes[$reviewIndex]) { throw "Supplement pin differs: $reviewPath" }
    $document = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($reviewPath)) -AsHashtable
    if ($document.taskId -notin @('TASK-FEDPH0-004','EXEC-TODO-FEDPH0-004')) { throw "Wrong task: $reviewPath" }
    $entries = if ($document.ContainsKey('entries')) { @($document.entries) } else { @($document.rows) }
    $reviewInputs.Add([ordered]@{fileName = Split-Path $reviewPath -Leaf; sourcePath = $reviewPath; sha256 = $hash; count = $entries.Count; original = $document})
    foreach ($entry in $entries) {
        $id = $entry.candidateId
        if ($reviewsById.ContainsKey($id)) { throw "Duplicate supplemental judgment: $id" }
        if (-not $partitionById.ContainsKey($id)) { throw "Extra supplemental judgment: $id" }
        if (-not $entry.fullSourceRead) { throw "Supplement lacks full read: $id" }
        $entryHash = if ($entry.ContainsKey('sha256')) { $entry.sha256 } else { $entry.sourceSha256 }
        if ($entryHash -cne $partitionById[$id].sha256) { throw "Source pin differs from partition: $id" }
        if ($entry.ContainsKey('designGap') -and $null -ne $entry.designGap -and $entry.designGap -ne '') { throw "Unresolved declared design gap: $id" }
        $reviewsById[$id] = [ordered]@{ receiptFile = Split-Path $reviewPath -Leaf; sessionId = $document.sessionId; requestId = $document.requestId; judgment = $entry }
    }
}
if ($reviewsById.ContainsKey($replId)) { throw 'REPL review overlaps another reader.' }
$reviewsById[$replId] = [ordered]@{receiptFile = 'repl-dispatcher-semantic-disposition.json'; sessionId = $replReview.sessionId; requestId = $replReview.requestId; judgment = $replEntry}
if ($reviewsById.Count -ne 184) { throw "Incomplete supplemental union: $($reviewsById.Count)" }
foreach ($id in $partitionById.Keys) { if (-not $reviewsById.ContainsKey($id)) { throw "Missing judgment: $id" } }
$originalByPath = @{}
foreach ($entry in $original.files) {
    if ($originalByPath.ContainsKey($entry.path)) { throw "Duplicate original review: $($entry.path)" }
    $sourcePath = Join-Path $WorkspaceRoot $entry.path
    if ((Get-FileHash -LiteralPath $sourcePath).Hash -cne $entry.sha256) { throw "Original review source changed: $($entry.path)" }
    $sourceLines = [IO.File]::ReadAllText($sourcePath) -split '\r?\n'
    foreach ($excerpt in $entry.reviewedExcerpts) {
        if ($excerpt.lines.Count -ne $excerpt.end - $excerpt.start + 1) { throw "Excerpt extent differs: $($entry.path)" }
        for ($lineIndex = 0; $lineIndex -lt $excerpt.lines.Count; $lineIndex++) {
            if ($sourceLines[$excerpt.start - 1 + $lineIndex] -cne $excerpt.lines[$lineIndex]) { throw "Original excerpt differs: $($entry.path)" }
        }
    }
    $originalByPath[$entry.path] = $entry
}
if ($originalByPath.Count -ne 32) { throw 'Original review denominator differs.' }
$candidateRows = foreach ($candidate in $roster.files) {
    $sourcePath = Join-Path $WorkspaceRoot $candidate.path
    if ((Get-FileHash -LiteralPath $sourcePath).Hash -cne $candidate.catalogSha256) { throw "Current candidate source changed: $($candidate.path)" }
    $sourceLines = [IO.File]::ReadAllText($sourcePath) -split '\r?\n'
    foreach ($hit in $candidate.hits) { if ($sourceLines[$hit.line - 1] -cne $hit.sourceText) { throw "Lexical anchor differs: $($candidate.path):$($hit.line)" } }
    if ($originalByPath.ContainsKey($candidate.path)) {
        if ($reviewsById.ContainsKey($candidate.path)) { throw "Original/supplement overlap: $($candidate.path)" }
        [ordered]@{candidateId = $candidate.path; sha256 = $candidate.catalogSha256; evidenceKind = 'RetainedTargetedExcerpts'; fullSourceReadClaim = $false; lexicalHits = $candidate.hits; judgment = $originalByPath[$candidate.path]}
    } else {
        if (-not $reviewsById.ContainsKey($candidate.path)) { throw "Candidate lacks judgment: $($candidate.path)" }
        [ordered]@{candidateId = $candidate.path; sha256 = $candidate.catalogSha256; evidenceKind = 'SupplementalFullSourceRead'; fullSourceReadClaim = $true; lexicalHits = $candidate.hits; review = $reviewsById[$candidate.path]}
    }
}
$targeted = @($candidateRows | Where-Object evidenceKind -EQ 'RetainedTargetedExcerpts')
$full = @($candidateRows | Where-Object evidenceKind -EQ 'SupplementalFullSourceRead')
$supplemental = @($original.files | Where-Object path -NotIn $roster.files.path)
if (@($candidateRows).Count -ne 215 -or $targeted.Count -ne 31 -or $full.Count -ne 184 -or $supplemental.Count -ne 1) { throw 'Combined review denominator differs.' }
$combined = [ordered]@{
    schemaVersion = 1; artifactType = 'Task004FiniteConsumerSemanticReview'; taskId = 'TASK-FEDPH0-004'
    timestampLocal = Get-Date -Format s; sourceCommit = (git -C $WorkspaceRoot rev-parse HEAD).Trim()
    sourceWorktree = $WorkspaceRoot; agent = 'Codex'; model = 'gpt-6.1-sol'; effort = 'xhigh'
    sessionId = $replReview.sessionId; requestId = $replReview.requestId; parentSessionId = $replReview.parentSessionId; parentRequestId = $replReview.parentRequestId
    summary = [ordered]@{candidateFiles = 215; lexicalHits = 918; supplementalFullReads = 184; retainedTargetedCandidateReviews = 31; retainedOriginalReviews = 32; supplementalReviewedDocuments = 1; missingJudgments = 0; extraJudgments = 0; duplicateJudgments = 0; sourceHashMismatches = 0; exactAnchorMismatches = 0; finiteSemanticDispositionComplete = $true; nativeProof = $false; dynamicCallerClosure = $false; taskReadyClaim = $false}
    partitionSha256 = (Get-FileHash -LiteralPath $partitionPath).Hash; rosterSha256 = (Get-FileHash -LiteralPath $rosterPath).Hash
    originalReviewSha256 = (Get-FileHash -LiteralPath $originalPath).Hash
    inputReceipts = @($reviewInputs); ownerReplReview = $replReview
    candidates = @($candidateRows); retainedSupplementalDocuments = $supplemental
    limitations = @('Finite source/design judgments only. Full original reader payloads and exact attribution are retained.','The31 original candidate judgments are targeted excerpts, not newly claimed full-file reads.','No actual dynamic caller graph, installed-plugin invocation, provider/native compatibility or task readiness follows from this ledger.','Implementation bindings and B-output evidence remain later approved work; this ledger creates no new product behavior, API, enrollment or approval gate.','Operator pause boundary remains in force; parent owns all-nine readiness review and pause.')
}
$combinedPath = Join-Path $OutputDirectory 'consumer-semantic-review.json'
$replPath = Join-Path $OutputDirectory 'repl-dispatcher-semantic-disposition.json'
foreach ($newPath in @($combinedPath,$replPath)) { if (Test-Path -LiteralPath $newPath) { throw "Refusing to overwrite receipt: $newPath" } }
if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) { throw 'Output directory must already exist.' }
if ($PSCmdlet.ShouldProcess($OutputDirectory, 'Copy/hash/retain seven original supplements and create verified consumer ledger plus owner REPL review')) {
    foreach ($receipt in $reviewInputs) {
        $destination = Join-Path $OutputDirectory $receipt.fileName
        if ([IO.Path]::GetFullPath($receipt.sourcePath) -eq [IO.Path]::GetFullPath($destination)) { continue }
        if (Test-Path -LiteralPath $destination) {
            if ((Get-FileHash -LiteralPath $destination).Hash -cne $receipt.sha256) { throw "Existing receipt differs: $destination" }
        } else { Copy-Item -LiteralPath $receipt.sourcePath -Destination $destination }
        if ((Get-FileHash -LiteralPath $destination).Hash -cne $receipt.sha256) { throw "Copied receipt differs: $destination" }
    }
    $var1 = ($replReview | ConvertTo-Json -Depth 80).TrimEnd()
    Add-LinesToFile -Path $replPath -Content $var1 | Out-Null
    $var1 = ($combined | ConvertTo-Json -Depth 100).TrimEnd()
    Add-LinesToFile -Path $combinedPath -Content $var1 | Out-Null
    $combined.summary
    Get-FileHash -LiteralPath $combinedPath,$replPath -Algorithm SHA256 | Select-Object Path,Hash
}