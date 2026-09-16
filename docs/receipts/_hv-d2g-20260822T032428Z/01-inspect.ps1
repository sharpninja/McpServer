#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
Set-Location -LiteralPath $workspace

function Save-Json {
    param([string]$Name, $Object)
    $path = Join-Path $outDir $Name
    ($Object | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $path -Encoding utf8
    Write-Output ('SAVED ' + $Name)
}

function Get-Sha256Hex {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    $hash = Get-FileHash -LiteralPath $Path -Algorithm SHA256
    return $hash.Hash
}

$coreInvoke = Join-Path $workspace 'plugins\core\skills\handoff\invoke.ps1'
$grokInvoke = Join-Path $plugin 'skills\handoff\invoke.ps1'
$coreSkill = Join-Path $workspace 'plugins\core\skills\handoff\SKILL.md'
$grokSkill = Join-Path $plugin 'skills\handoff\SKILL.md'
$service = Join-Path $workspace 'src\McpServer.Services\Services\HandoffIngestionService.cs'
$pester = Join-Path $workspace 'plugins\core\test-fixtures\pester\PluginHandoffSkill.Tests.ps1'
$durability = Join-Path $workspace 'tests\McpServer.Support.Mcp.Tests\Services\HandoffDurabilityTests.cs'
$d15 = Join-Path $workspace 'docs\receipts\hostile-validator-20260822T013032Z.md'
$d15json = Join-Path $workspace 'docs\receipts\hostile-validator-20260822T013032Z.json'
$d2 = Join-Path $workspace 'docs\receipts\d2-implement-20260822T022012Z.md'

$paths = @(
    $coreInvoke, $grokInvoke, $coreSkill, $grokSkill, $service, $pester, $durability, $d15, $d15json, $d2
)
$pathInfo = foreach ($p in $paths) {
    $item = Get-Item -LiteralPath $p -ErrorAction SilentlyContinue
    [ordered]@{
        path = $p
        exists = [bool]$item
        lastWriteTimeUtc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
        length = if ($item) { $item.Length } else { $null }
        sha256 = Get-Sha256Hex -Path $p
    }
}
Save-Json -Name 'path-info.json' -Object $pathInfo

$shaCompare = [ordered]@{
    coreInvoke = Get-Sha256Hex -Path $coreInvoke
    grokInvoke = Get-Sha256Hex -Path $grokInvoke
    invokeMatch = ((Get-Sha256Hex -Path $coreInvoke) -eq (Get-Sha256Hex -Path $grokInvoke))
    coreSkill = Get-Sha256Hex -Path $coreSkill
    grokSkill = Get-Sha256Hex -Path $grokSkill
    skillMatch = ((Get-Sha256Hex -Path $coreSkill) -eq (Get-Sha256Hex -Path $grokSkill))
}
Save-Json -Name 'sha256-compare.json' -Object $shaCompare

Push-Location $workspace
try {
    git status --porcelain -- plugins/core/skills/handoff src/McpServer.Services/Services/HandoffIngestionService.cs plugins/core/sync/sync-plugin-core.ps1 plugins/core/test-fixtures/pester/PluginHandoffSkill.Tests.ps1 tests/McpServer.Support.Mcp.Tests/Services/HandoffDurabilityTests.cs | Set-Content -LiteralPath (Join-Path $outDir 'git-status-handoff.txt') -Encoding utf8
    git diff --numstat -- plugins/core/skills/handoff src/McpServer.Services/Services/HandoffIngestionService.cs plugins/core/sync/sync-plugin-core.ps1 plugins/core/test-fixtures/pester/PluginHandoffSkill.Tests.ps1 tests/McpServer.Support.Mcp.Tests/Services/HandoffDurabilityTests.cs | Set-Content -LiteralPath (Join-Path $outDir 'git-diff-numstat.txt') -Encoding utf8
    git log -5 --format='%h %cI %s' -- src/McpServer.Services/Services/HandoffIngestionService.cs | Set-Content -LiteralPath (Join-Path $outDir 'git-log-handoff-service.txt') -Encoding utf8
    git diff -- plugins/core/test-fixtures/pester/PluginHandoffSkill.Tests.ps1 | Set-Content -LiteralPath (Join-Path $outDir 'git-diff-pester.txt') -Encoding utf8
    git diff -- src/McpServer.Services/Services/HandoffIngestionService.cs | Set-Content -LiteralPath (Join-Path $outDir 'git-diff-service.txt') -Encoding utf8
    git diff -- plugins/core/sync/sync-plugin-core.ps1 | Set-Content -LiteralPath (Join-Path $outDir 'git-diff-sync.txt') -Encoding utf8
} finally {
    Pop-Location
}

$migRoots = @(
    (Join-Path $workspace 'src\McpServer.Storage.SqliteMigrations\Migrations'),
    (Join-Path $workspace 'src\McpServer.Storage.SqlServerMigrations\Migrations'),
    (Join-Path $workspace 'src\McpServer.Storage.PostgreSqlMigrations\Migrations')
)
$handoffMigs = foreach ($root in $migRoots) {
    Get-ChildItem -LiteralPath $root -Filter '*Handoff*' -ErrorAction SilentlyContinue | ForEach-Object {
        [ordered]@{
            path = $_.FullName
            name = $_.Name
            lastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o')
        }
    }
}
Save-Json -Name 'handoff-migrations.json' -Object @($handoffMigs)

$allMigsAfter = foreach ($root in $migRoots) {
    Get-ChildItem -LiteralPath $root -Filter '*.cs' -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^2026081[7-9]|^2026082' } |
        ForEach-Object {
            [ordered]@{
                path = $_.FullName
                name = $_.Name
                lastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o')
            }
        }
}
Save-Json -Name 'migrations-after-handoff.json' -Object @($allMigsAfter)

Select-String -LiteralPath $service -Pattern 'RenewProcessingLeaseLoopAsync|ProcessingLeaseExpiresAtUtc|PromptVersion|TemplateVersion|ReplayIdentity|HeartbeatInterval' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() } |
    Set-Content -LiteralPath (Join-Path $outDir 'service-grep.txt') -Encoding utf8

Select-String -LiteralPath $durability -Pattern 'ProcessingLease_RenewsAndFencesTerminalUpdates|Provenance_IncludesEffectiveCustomPromptIdentityAndVersion|IngestAsync_CustomPromptTemplate_IsRejected|IngestAsync_StaleLease_IsTakenOverBySecondInstance|IngestAsync_LeaseExpiresDuringLiveExtraction_TakeoverWinsAndFirstCannotCreate|ApproveAsync_LiveClaimant_RejectsStaleSecondClaim' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() } |
    Set-Content -LiteralPath (Join-Path $outDir 'durability-grep.txt') -Encoding utf8

Select-String -Path (Join-Path $workspace 'tests\McpServer.Support.Mcp.Tests\**\*.cs') -Pattern 'PluginSync_HandoffSkill_MatchesCoreArtifact' |
    ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() } |
    Set-Content -LiteralPath (Join-Path $outDir 'pluginsync-grep.txt') -Encoding utf8

$syncCore = Join-Path $workspace 'plugins\core\sync\sync-plugin-core.ps1'
Select-String -LiteralPath $syncCore -Pattern 'handoff|invoke.ps1|SKILL.md' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() } |
    Set-Content -LiteralPath (Join-Path $outDir 'sync-core-grep.txt') -Encoding utf8

$localDb = [ordered]@{
    testhost = @(Get-Process testhost, testhost.x86, VSTest.Console, dotnet -ErrorAction SilentlyContinue | Select-Object Name, Id, StartTime)
    sqlservr = @(Get-Process sqlservr -ErrorAction SilentlyContinue | Select-Object Name, Id, StartTime)
}
Save-Json -Name 'process-snapshot.json' -Object $localDb

$d15Agree = Select-String -LiteralPath $d15 -Pattern 'OverallVerdict' | Select-Object -First 1
$d15Stamp = Select-String -LiteralPath $d15 -Pattern 'TimestampUtc' | Select-Object -First 1
$gate = [ordered]@{
    d15Exists = Test-Path -LiteralPath $d15
    d15AgreeLine = if ($d15Agree) { $d15Agree.Line.Trim() } else { $null }
    d15TimestampLine = if ($d15Stamp) { $d15Stamp.Line.Trim() } else { $null }
    d2Exists = Test-Path -LiteralPath $d2
    invokeCoreExists = Test-Path -LiteralPath $coreInvoke
    invokeGrokExists = Test-Path -LiteralPath $grokInvoke
}
Save-Json -Name 'gate-files.json' -Object $gate

Write-Output 'INSPECT_DONE'
