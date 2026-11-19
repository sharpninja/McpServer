<#
.SYNOPSIS
Runs the complete seven-class Task007 member mock gate in a source-pinned, isolated Linux worktree.
.DESCRIPTION
2026-11-19: TASK-FEDPH0-007 B10-B12; FR-MCP-179/184/185; TR-MCP-FEDERATION-006/011/012; TEST-MCP-227/232/233.
This includes the supporting FR-MCP-185 ac-SqlPortUnreachableAcrossLink assertion repair.
Nuke owns build/test effects. No native sockets, SQL services, trust stores or product databases are exercised.
Original worktrees and evidence are retained. WhatIf precedes fetch, worktree creation and Nuke execution.
.PARAMETER SourceCommit
Exact 40-character candidate source commit.
.PARAMETER RunId
Safe Nuke TestGroups run identifier.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low')]
param(
    [Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{40}$')][string]$SourceCommit,
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9-]+$')][string]$RunId
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = '/home/sharpninja/github/McpServer'
$worktreePath = '/home/sharpninja/github/McpServer/.worktrees/fedph0-task007-network-20261119'
$sourceBranch = 'codex/fedph0-task007-network-20261119'
Write-Output ('LaneScriptSha256=' + (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash)
if (-not $IsLinux) { throw 'This retained lane is Linux-only.' }
if (-not (Test-Path -LiteralPath "$repositoryRoot/.git")) { throw 'Expected repository root is absent.' }
if (-not $PSCmdlet.ShouldProcess($worktreePath, "Fetch $sourceBranch, create/verify isolated worktree at $SourceCommit and run seven Task007 mock classes ($RunId)")) { return }
& git -C $repositoryRoot fetch origin $sourceBranch
if ($LASTEXITCODE -ne 0) { throw 'Git fetch failed.' }
$resolved = (& git -C $repositoryRoot rev-parse --verify "$SourceCommit^{commit}").Trim()
if ($LASTEXITCODE -ne 0 -or $resolved -ne $SourceCommit) { throw 'Source commit did not resolve exactly.' }
if (Test-Path -LiteralPath $worktreePath) {
    $existingHead = (& git -C $worktreePath rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $existingHead -ne $SourceCommit) { throw 'Existing isolated worktree differs; retain it without checkout/reset.' }
} else {
    & git -C $repositoryRoot worktree add --detach $worktreePath $SourceCommit
    if ($LASTEXITCODE -ne 0) { throw 'Git worktree creation failed.' }
}
Push-Location $worktreePath
try {
    & ./build.ps1 TestGroups --test-groups 'QBrainAi.Support.Mcp.Tests:Phase0CandidateRetentionMockGateTests,Phase0SubscriptionStateMockGateTests,Phase0RecoverySecurityMockGateTests,Phase0ReviewOperationsMockGateTests,Phase0SubscriptionPolicyMockGateTests,Phase0OperationsReadinessMockGateTests,Phase0SqlNetworkMockGateTests' --test-run-id $RunId
    if ($LASTEXITCODE -ne 0) { throw "Nuke TestGroups failed with exit $LASTEXITCODE." }
    Write-Output "Task007SourceCommit=$SourceCommit"
    Write-Output "Task007Trx=$worktreePath/TestResults/$RunId/groups/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.trx"
} finally { Pop-Location }
