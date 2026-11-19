<#
.SYNOPSIS
Reads source-pinned Task005 Linux mock evidence without changing the worktree or test state.
.DESCRIPTION
2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.
Reports exact source/head, raw and canonical source hashes, complete TRX counters and ordered case-name hash.
No token, credential, private identity payload or live provider state is inspected.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$worktreePath = '/home/sharpninja/github/McpServer/.worktrees/fedph0-task005-20261119'
$runId = 'fedph0-task005-20261119-linux-r1'
$sourceCommit = '74bbc8120757467372821a40af998594b2b5c55f'
$head = (& git -C $worktreePath rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $sourceCommit) { throw 'Linux source head differs from the tested child source.' }
$sourcePaths = @('docs/plans/2026-11-17-federation-identity-platform-design.md') + @(Get-ChildItem -LiteralPath "$worktreePath/tests/QBrainAi.Support.Mcp.Tests/Federation/Phase0" -Filter '*.cs' | Where-Object Name -Match '^Phase0(IdentityPlatform|IdentityManagement|IdentityAdmission|WorkerPermission)(Contract|MockGateTests)\.cs$' | ForEach-Object { 'tests/QBrainAi.Support.Mcp.Tests/Federation/Phase0/' + $_.Name })
& git -C $worktreePath diff --exit-code -- $sourcePaths
if ($LASTEXITCODE -ne 0) { throw 'A tested source file has uncommitted changes.' }
$hashes = foreach ($relative in $sourcePaths) {
    $text = [System.IO.File]::ReadAllText("$worktreePath/$relative").Replace("`r`n", "`n")
    [ordered]@{ path=$relative; sha256=(Get-FileHash -LiteralPath "$worktreePath/$relative" -Algorithm SHA256).Hash; canonicalLfSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text))) }
}
$trxPath = "$worktreePath/TestResults/$runId/groups/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.trx"
[xml]$trx = Get-Content -LiteralPath $trxPath -Raw
$counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
[string[]]$names = @($trx.SelectNodes("//*[local-name()='UnitTest']") | ForEach-Object { $_.GetAttribute('name') })
[Array]::Sort($names, [StringComparer]::Ordinal)
$caseHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($names -join "`n"))))
$logs = @(Get-ChildItem -LiteralPath "$worktreePath/.nuke/temp" -Filter 'build.*.log' | Where-Object { (Get-Content -LiteralPath $_.FullName -Raw).Contains($runId, [StringComparison]::Ordinal) } | Sort-Object Name)
if ($logs.Count -ne 1) { throw 'Expected exactly one retained Nuke log for the Linux run.' }
[ordered]@{
    readerSha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
    host=[Environment]::MachineName; sourceCommit=$head; runId=$runId; worktree=$worktreePath
    powershellVersion=$PSVersionTable.PSVersion.ToString(); timestampLocal=(Get-Date -Format s)
    trxPath=$trxPath; trxSha256=(Get-FileHash -LiteralPath $trxPath -Algorithm SHA256).Hash
    total=[int]$counters.GetAttribute('total'); passed=[int]$counters.GetAttribute('passed'); failed=[int]$counters.GetAttribute('failed'); notExecuted=[int]$counters.GetAttribute('notExecuted')
    caseCount=$names.Length; orderedCaseNameSha256=$caseHash; sourceHashes=@($hashes)
    nukeLogPath=$logs[0].FullName; nukeLogSha256=(Get-FileHash -LiteralPath $logs[0].FullName -Algorithm SHA256).Hash
    mockOnly=$true; nativeProof=$false; implementationBarrier='Closed'; done=$false
} | ConvertTo-Json -Depth 8