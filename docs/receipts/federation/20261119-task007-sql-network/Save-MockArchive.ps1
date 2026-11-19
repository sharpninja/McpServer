<#
.SYNOPSIS
Retains byte-exact raw Task007 TRX and Nuke log in a ZIP without deleting original evidence.
.DESCRIPTION
2026-11-19: TASK-FEDPH0-007 B10-B12; FR-MCP-179/184/185; TR-MCP-FEDERATION-006/011/012; TEST-MCP-227/232/233.
Only creates a new run-owned evidence archive after WhatIf. No source, test state, service or database changes.
.PARAMETER Worktree
Exact isolated worktree containing the original Nuke evidence.
.PARAMETER RunId
Existing TestGroups run identifier.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low')]
param(
    [Parameter(Mandatory)][string]$Worktree,
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9-]+$')][string]$RunId
)
$ErrorActionPreference = 'Stop'
$trxPath = "$Worktree/TestResults/$RunId/groups/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.trx"
$logPath = "$Worktree/.nuke/temp/build.log"
$archive = "$Worktree/TestResults/$RunId/task007-members.raw.zip"
if (-not (Get-Content -LiteralPath $logPath -Raw).Contains($RunId, [StringComparison]::Ordinal)) { throw 'Current retained Nuke log belongs to another run.' }
if (Test-Path -LiteralPath $archive) { throw 'Retain existing archive without overwrite.' }
if (-not $PSCmdlet.ShouldProcess($archive, 'Create raw TRX/Nuke log ZIP and retain originals')) { return }
Compress-Archive -LiteralPath $trxPath,$logPath -DestinationPath $archive
[ordered]@{
    scriptSha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
    archive=$archive; archiveSha256=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
    trxSha256=(Get-FileHash -LiteralPath $trxPath -Algorithm SHA256).Hash
    logSha256=(Get-FileHash -LiteralPath $logPath -Algorithm SHA256).Hash
} | ConvertTo-Json -Depth 4
