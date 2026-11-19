<#
.SYNOPSIS
Reads Task007 seven-class member evidence and byte-preserving ZIP receipts without changing state.
.DESCRIPTION
2026-11-19: TASK-FEDPH0-007 B10-B12; FR-MCP-179/184/185; TR-MCP-FEDERATION-006/011/012; TEST-MCP-227/232/233.
Reports raw and LF-canonical source hashes, ordinal case inventory, exact counters and archived evidence hashes.
This is mock evidence only. No native network, SQL, restore, measurement or trust acceptance is inferred.
.PARAMETER Worktree
Exact isolated worktree containing the tested source and original evidence.
.PARAMETER RunId
Existing Nuke TestGroups run identifier.
.PARAMETER Archive
Existing ZIP containing raw TRX and full Nuke log copied without text conversion.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Worktree,
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9-]+$')][string]$RunId,
    [Parameter(Mandatory)][string]$Archive
)
$ErrorActionPreference = 'Stop'
$classStems = @('CandidateRetention', 'SubscriptionState', 'RecoverySecurity', 'ReviewOperations', 'SubscriptionPolicy', 'OperationsReadiness', 'SqlNetwork')
$relativePaths = @('docs/plans/2026-11-19-federation-task007-operations-readiness.md')
foreach ($stem in $classStems) {
    foreach ($suffix in @('Contract', 'MockGateTests')) {
        $relativePaths += "tests/QBrainAi.Support.Mcp.Tests/Federation/Phase0/Phase0$stem$suffix.cs"
    }
}
$sourceHashes = foreach ($relative in $relativePaths) {
    $path = Join-Path $Worktree $relative
    $text = [System.IO.File]::ReadAllText($path).Replace("`r`n", "`n")
    [ordered]@{ path=$relative; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash; canonicalLfSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text))) }
}
$trxPath = "$Worktree/TestResults/$RunId/groups/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.trx"
[xml]$trx = Get-Content -LiteralPath $trxPath -Raw
$counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
[string[]]$names = @($trx.SelectNodes("//*[local-name()='UnitTest']") | ForEach-Object { $_.GetAttribute('name') })
[Array]::Sort($names, [StringComparer]::Ordinal)
$caseHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($names -join "`n"))))
$classCounts = foreach ($stem in $classStems) {
    $className = 'Phase0' + $stem + 'MockGateTests'
    [ordered]@{ class=$className; count=@($names | Where-Object { $_.Contains('.' + $className + '.', [StringComparison]::Ordinal) }).Count }
}
$archiveFile = [IO.Compression.ZipFile]::OpenRead($Archive)
try {
    $entries = foreach ($entry in $archiveFile.Entries) {
        $stream = $entry.Open()
        try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
        [ordered]@{ name=$entry.FullName; length=$entry.Length; sha256=$hash }
    }
} finally { $archiveFile.Dispose() }
$rawTrxHash = (Get-FileHash -LiteralPath $trxPath -Algorithm SHA256).Hash
if (@($entries | Where-Object { $_.name.EndsWith('.trx', [StringComparison]::Ordinal) -and $_.sha256 -eq $rawTrxHash }).Count -ne 1) { throw 'ZIP does not retain the exact raw TRX bytes.' }
[ordered]@{
    readerSha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
    host=[Environment]::MachineName; sourceCommit=(& git -C $Worktree rev-parse HEAD).Trim(); worktree=$Worktree; runId=$RunId
    powershellVersion=$PSVersionTable.PSVersion.ToString(); timestampLocal=(Get-Date -Format s)
    trxPath=$trxPath; trxSha256=$rawTrxHash
    total=[int]$counters.GetAttribute('total'); passed=[int]$counters.GetAttribute('passed'); failed=[int]$counters.GetAttribute('failed'); notExecuted=[int]$counters.GetAttribute('notExecuted')
    caseCount=$names.Length; ordinalCaseNameSha256=$caseHash; classCounts=@($classCounts); sourceHashes=@($sourceHashes)
    archivePath=$Archive; archiveSha256=(Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash; archiveEntries=@($entries)
    mockOnly=$true; nativeProof=$false; implementationBarrier='Closed'; done=$false
} | ConvertTo-Json -Depth 8
