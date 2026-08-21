#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$stamp = '20260821T103620Z'
$ts = '2026-08-21T10:36:20Z'
$mdPath = Join-Path $workspace "docs\receipts\hostile-validator-$stamp.md"
$jsonPath = Join-Path $workspace "docs\receipts\hostile-validator-$stamp.json"

$json = [ordered]@{
    TimestampUtc = $ts
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 'Class 2 (user-directed ops: operator ordered Nuke UpdateService redeploy; not new product implementation)'
    AddProfileExecuted = $true
    ProfileFileCount = 18
    ActivePlan = 'docs/plans/sessionlog-remediate-001.md'
    TodoId = 'PLAN-SESSIONLOGREMEDIATE-001'
    ReviewSessionId = 'GrokCode-20260821T103450Z-plugin-session'
    ReviewRequestId = 'req-20260821T103448Z-001-hostile-s6-nuke-redeploy'
    PluginVersion = '1.97.0'
    GitHead = 'ee89cd63f6d16aa43d8e8dfac2388246c6ba39f8'
    LiveHostedVersion = '1.4.30+ee89cd63f6d16aa43d8e8dfac2388246c6ba39f8'
    DefaultPosture = 'FAIL until independently re-verified'
    OverallVerdict = 'AGREE'
    PassCount = 12
    FailCount = 0
    UnknownCount = 0
    NaCount = 2
    Accuracy = 96
    Completeness = 95
    ExplicitFailList = @()
    UnknownList = @()
    NaList = @('B1 Byrd v4 (class 2 ops; not project implementation)', 'C Requirements (class 2; FR/TR/TEST N/A)')
    Claims = @(
        @{ id = 'A1'; surface = 'A'; verdict = 'PASS'; claim = 'Single elevated gsudo ran Nuke UpdateService via run-update-service.ps1; EXIT 0; duration 3:19' }
        @{ id = 'A2'; surface = 'A'; verdict = 'PASS'; claim = 'GitVersion bump 1.4.29 -> 1.4.30; live ProductVersion 1.4.30+ee89cd63; marker version line; service Running' }
        @{ id = 'A3'; surface = 'A'; verdict = 'PASS'; claim = 'Health HTTP 200 Healthy, storage reachable, WSHealth 38/38, config/data restored, archive zip exists' }
        @{ id = 'A4'; surface = 'A'; verdict = 'PASS'; claim = 'After restart plugin HMAC True; Status available; implementer did not roll HMACSHA256' }
        @{ id = 'A5'; surface = 'A'; verdict = 'PASS'; claim = 'No manual copy into ProgramData as Nuke substitute; deploy through Build.UpdateService.cs' }
        @{ id = 'A6'; surface = 'A'; verdict = 'PASS'; claim = 'GitVersion.yml bumped and git-added; no new commit of it' }
        @{ id = 'B2'; surface = 'B'; verdict = 'PASS'; claim = 'Receipts exist and this validator re-ran live checks' }
        @{ id = 'B3'; surface = 'B'; verdict = 'PASS'; claim = 'MCP-only storage; no TODO.yaml edit this turn; FR docs mtimes predate deploy' }
        @{ id = 'B4'; surface = 'B'; verdict = 'PASS'; claim = 'pwsh.exe only; no Python in this deploy' }
        @{ id = 'B5'; surface = 'B'; verdict = 'PASS'; claim = 'Honesty: live artifacts match claims; yml HEAD 1.4.28 vs log 1.4.29 explained' }
        @{ id = 'B6'; surface = 'B'; verdict = 'PASS'; claim = 'Nuke-only deploy; live manifest generatedBy build/Build.UpdateService.cs' }
        @{ id = 'D'; surface = 'D'; verdict = 'PASS'; claim = 'No plan/TODO done flip; class-2 redeploy does not require leftover S6 product persist proofs' }
    )
}

$json | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output ('JSON=' + $jsonPath)
Write-Output ('JSON_LEN=' + (Get-Item -LiteralPath $jsonPath).Length)
Write-Output ('JSON_UTC=' + (Get-Item -LiteralPath $jsonPath).LastWriteTimeUtc.ToString('o'))
