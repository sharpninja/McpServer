<#
.SYNOPSIS
Runs only Task005 changed mock classes in an isolated Linux worktree at an exact source commit.
.DESCRIPTION
2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-RevokedNodeCannotPublish/ac-LocalAppUserCannotPush;
TR-MCP-FEDERATION-012; TEST-MCP-233. Nuke owns all build/test effects. No product service/database is started.
Original worktrees, sources and receipts are retained. WhatIf reports the bounded mutation before fetching/creating/running.
.PARAMETER SourceCommit
Exact 40-character commit whose test sources must execute.
.PARAMETER RunId
Safe run directory name passed to the existing Nuke TestGroups target.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low')]
param(
    [Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{40}$')][string]$SourceCommit,
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9-]+$')][string]$RunId
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = '/home/sharpninja/github/McpServer'
$worktreePath = '/home/sharpninja/github/McpServer/.worktrees/fedph0-task005-20261119'
$sourceBranch = 'codex/fedph0-task005-20261119'
if (-not $IsLinux) { throw 'This retained lane is Linux-only.' }
if (-not (Test-Path -LiteralPath "$repositoryRoot/.git")) { throw 'Expected repository root is absent.' }
if (-not $PSCmdlet.ShouldProcess($worktreePath, "Fetch $sourceBranch, create/verify isolated worktree at $SourceCommit and run Task005 mocks via Nuke ($RunId)")) { return }
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
    & ./build.ps1 TestGroups --test-groups 'QBrainAi.Support.Mcp.Tests:Phase0IdentityPlatformMockGateTests,Phase0IdentityManagementMockGateTests,Phase0WorkerPermissionMockGateTests,Phase0IdentityAdmissionMockGateTests' --test-run-id $RunId
    if ($LASTEXITCODE -ne 0) { throw "Nuke TestGroups failed with exit $LASTEXITCODE." }
    Write-Output "Task005SourceCommit=$SourceCommit"
    Write-Output "Task005Trx=$worktreePath/TestResults/$RunId/groups/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.trx"
} finally { Pop-Location }