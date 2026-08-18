#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$receipt = [ordered]@{
    TimestampUtc = '2026-08-17T23:36:18Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 'user-directed-general-action-class-2'
    AddProfileExecuted = $true
    ProfileFilesRead = 18
    PluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
    PluginVersion = '1.93.0'
    MarkerSignature = $true
    HealthNonce = 'db6d47b89040455c82e9d233da40c195'
    HealthNonceEchoed = $true
    FullBootstrap = $true
    SessionId = 'GrokCode-20260817T233333Z-hostile-svc-cfg'
    RequestId = 'req-20260817T233333Z-001-hostile-validate-svc-cfg'
    ServerTurnId = 41539
    PlanFile = 'None'
    TodoId = 'None'
    OverallVerdict = 'AGREE'
    FailList = @()
    SurfacesNotEvaluated = @()
    Accuracy = 97
    Completeness = 96
    SessionLogQuery = [ordered]@{
        TextEqualsSessionIdTotalCount = 0
        TextHostileSvcCfgHit = 'GrokCode-20260817T232250Z-hostile-effort'
        AgentFromTotalCount = 1
        AgentFromSessionId = 'GrokCode-20260817T233333Z-hostile-svc-cfg'
        AgentFromTurnRequestId = 'req-20260817T233333Z-001-hostile-validate-svc-cfg'
        AgentFromTurnStatus = 'completed'
    }
    LiveYaml = [ordered]@{
        Path = 'C:\ProgramData\McpServer\appsettings.yaml'
        LastWriteTimeUtc = '2026-08-17T23:30:09.0404870Z'
        Length = 58975
        Sha256 = 'B42E2462D67EADE136EC3BF64A1224BF1253ADB73EA6596CFED1BC7C7A4E3D46'
        AgentHelpKeys = @('DefaultExecutionStrategy', 'HelperModel', 'Enabled')
        DefaultExecutionStrategy = 'grok-cli'
        HelperModel = 'grok-4.5'
        Enabled = $true
        EffortLikeKeys = @()
    }
    Service = [ordered]@{
        Name = 'McpServer'
        State = 'Running'
        StartMode = 'Auto'
        StartName = 'LocalSystem'
        PathName = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147'
        ProcessId = 5572
    }
    AgentHelp = [ordered]@{
        ClaimedSessionId = 'help-20260817233017-0bf8ab01a3af4e92a0c6c38ab8dba245'
        ClaimedStatusExecutionStrategy = 'grok-cli'
        ClaimedStatus = 'idle'
        IndependentSessionId = 'help-20260817233334-998196a156f24f1f9577015aea5ac98b'
        IndependentExecutionStrategy = 'grok-cli'
        IndependentModelRequested = 'grok-4.5'
        IndependentModelResolved = 'grok-4.5'
    }
    Claims = @(
        @{ Id = 'A1'; Surface = 'A'; Verdict = 'PASS'; Claim = 'Class 2 ops; live ProgramData yaml updated; no product code; no plan done' }
        @{ Id = 'A2'; Surface = 'A'; Verdict = 'PASS'; Claim = 'McpServer Running Auto LocalSystem PathName ProgramData exe --urls http://+:7147' }
        @{ Id = 'A3'; Surface = 'A'; Verdict = 'PASS'; Claim = 'Live AgentHelp grok-cli / grok-4.5 / Enabled=true' }
        @{ Id = 'A4'; Surface = 'A'; Verdict = 'PASS'; Claim = 'Claimed create-session strategy grok-cli; models reproduced by independent create-session' }
        @{ Id = 'A5'; Surface = 'A'; Verdict = 'PASS'; Claim = 'No unbound effort key; grok-cli hardcoded high' }
        @{ Id = 'A6'; Surface = 'A'; Verdict = 'PASS'; Claim = 'Repo appsettings.yaml unchanged vs HEAD' }
        @{ Id = 'B1'; Surface = 'B'; Verdict = 'PASS'; Claim = 'Byrd v4 N/A for class 2' }
        @{ Id = 'B2'; Surface = 'B'; Verdict = 'PASS'; Claim = 'Receipts re-verified independently' }
        @{ Id = 'B3'; Surface = 'B'; Verdict = 'PASS'; Claim = 'MCP-only storage' }
        @{ Id = 'B4'; Surface = 'B'; Verdict = 'PASS'; Claim = 'PowerShell only; no Python used' }
        @{ Id = 'B5'; Surface = 'B'; Verdict = 'PASS'; Claim = 'Honesty; claims match artifacts' }
        @{ Id = 'C1'; Surface = 'C'; Verdict = 'N/A'; Claim = 'Requirements N/A for class 2 ops' }
        @{ Id = 'D1'; Surface = 'D'; Verdict = 'N/A'; Claim = 'No plan-step completion claimed' }
    )
}

$out = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260817T233618Z.json'
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
$item = Get-Item -LiteralPath $out
Write-Output ('Wrote=' + $item.FullName)
Write-Output ('Length=' + $item.Length)
Write-Output ('LastWriteTimeUtc=' + $item.LastWriteTimeUtc.ToString('o'))
