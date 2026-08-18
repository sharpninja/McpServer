#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$obj = [ordered]@{
    TimestampUtc = '2026-08-18T00:04:00Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 'user-directed-general-action-class-2'
    AddProfileExecuted = $true
    ProfileFilesRead = 18
    PluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
    PluginVersion = '1.93.0'
    MarkerSignature = $true
    HealthNonce = '36d0cbc1c48647afa537ca0a4e50d71d'
    HealthNonceEchoed = $true
    HealthStorage = 'reachable'
    ReadyStatus = 200
    ReadyStorage = 'reachable'
    FullBootstrap = $true
    SessionId = 'GrokCode-20260818T000358Z-hostile-restart'
    RequestId = 'req-20260818T000358Z-001-hostile-validate-restart'
    ServerTurnId = 41570
    PlanFile = 'None'
    TodoId = 'None'
    OverallVerdict = 'DISAGREE'
    FailList = @(
        'A3 first post-restart /health storage=unreachable is false; first logged GET /health Output is storage=reachable'
        'B5 honesty: implementer first-health storage=unreachable and 503 backend_unavailable do not match server log'
    )
    SurfacesNotEvaluated = @()
    Accuracy = 96
    Completeness = 95
    SessionLogQuery = [ordered]@{
        TextEqualsSessionIdTotalCount = 0
        TextHostileRestartHit = 'GrokCode-20260817T235647Z-plugin-session'
        AgentFromTotalCount = 1
        AgentFromSessionId = 'GrokCode-20260818T000358Z-hostile-restart'
        AgentFromTurnRequestId = 'req-20260818T000358Z-001-hostile-validate-restart'
        AgentFromTurnStatus = 'completed'
        AgentFromTurnId = 41570
        DialogCount = 5
        ActionCount = 6
    }
    Service = [ordered]@{
        Name = 'McpServer'
        State = 'Running'
        StartMode = 'Auto'
        StartName = 'LocalSystem'
        PathName = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147'
        ProcessId = 57744
        ProcessCreationDateUtc = '2026-08-17T23:38:29.5863800Z'
        PriorIndependentProcessId = 5572
    }
    Marker = [ordered]@{
        Pid = 57744
        PidMatchService = $true
        LastWriteTimeUtc = '2026-08-17T23:38:48.7976227Z'
        StartedAt = '2026-08-17T23:38:48.7047470+00:00'
        ServerStartedAtUtc = '2026-08-17T23:38:29.7115442+00:00'
        ApiKeyPrefix4 = 'N3fW'
        ApiKeySuffix4 = 'RMao'
        ApiKeySha256 = 'E7B163CCB214AB8176C372809B7A8CBE4B87B9C74FBBFB2C407994B566849664'
        PreRestartWorkspaceKeyPrefix4 = 'IHOW'
        PreRestartWorkspaceKeySuffix4 = 'idDI'
        Version = '1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e'
    }
    FirstHealth = [ordered]@{
        LocalTime = '2026-08-17 18:38:56.584 -05:00'
        HttpStatus = 200
        Nonce = 'ffbf87a5a57c46cdada44497d922e256'
        Storage = 'reachable'
        UnreachableHitsIn1838To1842 = 0
        BackendOr503HitsIn1838To1842 = 0
    }
    LaterHealth = [ordered]@{
        Postrestart3LocalTime = '2026-08-17 18:56:24.098 -05:00'
        Postrestart3Storage = 'reachable'
        IndependentNonce = '36d0cbc1c48647afa537ca0a4e50d71d'
        IndependentStorage = 'reachable'
        IndependentReadyStatus = 200
    }
    LiveYaml = [ordered]@{
        Path = 'C:\ProgramData\McpServer\appsettings.yaml'
        LastWriteTimeUtc = '2026-08-17T23:30:09.0404870Z'
        Length = 58975
        Sha256 = 'B42E2462D67EADE136EC3BF64A1224BF1253ADB73EA6596CFED1BC7C7A4E3D46'
        DefaultExecutionStrategy = 'grok-cli'
        HelperModel = 'grok-4.5'
        Enabled = $true
    }
    Exe = [ordered]@{
        LastWriteTimeUtc = '2026-08-12T21:55:30.4271605Z'
        FileVersion = '1.4.26.0'
        ProductVersion = '1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e'
        Sha256 = 'A95B178712D30BE73CB55AEC8DF98127F44DDDEE4A62C932E52C1D3B09AF5529'
        DeployJsonLastWriteTimeUtc = '2026-08-12T21:55:34.9448414Z'
    }
    Claims = @(
        @{ Surface = 'A'; Id = 'A1'; Claim = 'One elevated Restart-Service; old PID 5572; new PID 57744; Status Running'; Verdict = 'PASS' }
        @{ Surface = 'A'; Id = 'A2'; Claim = 'Marker pid 57744 and rotated apiKey'; Verdict = 'PASS' }
        @{ Surface = 'A'; Id = 'A3'; Claim = 'First post-restart /health 200 nonce echo storage unreachable; later health/ready 200 reachable'; Verdict = 'FAIL' }
        @{ Surface = 'A'; Id = 'A4'; Claim = 'AgentHelp survived grok-cli / grok-4.5 / Enabled true'; Verdict = 'PASS' }
        @{ Surface = 'A'; Id = 'A5'; Claim = 'No binary deploy; no SCM account/start-type change'; Verdict = 'PASS' }
        @{ Surface = 'B'; Id = 'B1'; Claim = 'Byrd v4'; Verdict = 'PASS' }
        @{ Surface = 'B'; Id = 'B2'; Claim = 'Always bring the receipts'; Verdict = 'PASS' }
        @{ Surface = 'B'; Id = 'B3'; Claim = 'MCP-only storage'; Verdict = 'PASS' }
        @{ Surface = 'B'; Id = 'B4'; Claim = 'PowerShell-only / no Python'; Verdict = 'PASS' }
        @{ Surface = 'B'; Id = 'B5'; Claim = 'Honesty / no fabricated results'; Verdict = 'FAIL' }
        @{ Surface = 'C'; Id = 'C'; Claim = 'Requirements'; Verdict = 'N/A' }
        @{ Surface = 'D'; Id = 'D'; Claim = 'Current plan holistically'; Verdict = 'N/A' }
    )
    PassCount = 8
    FailCount = 2
    UnknownCount = 0
    NaCount = 2
}

$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T000400Z.json'
$json = $obj | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($jsonPath, $json + [Environment]::NewLine)
$item = Get-Item -LiteralPath $jsonPath
$md = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T000400Z.md'
Write-Output ('JsonPath=' + $item.FullName)
Write-Output ('JsonLength=' + $item.Length)
Write-Output ('JsonLw=' + $item.LastWriteTimeUtc.ToString('o'))
Write-Output ('MdPath=' + $md.FullName)
Write-Output ('MdLength=' + $md.Length)
Write-Output ('MdLw=' + $md.LastWriteTimeUtc.ToString('o'))
$parsed = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
Write-Output ('ParsedVerdict=' + $parsed.OverallVerdict)
Write-Output ('ParsedFailCount=' + @($parsed.FailList).Count)
Write-Output ('ParsedSessionId=' + $parsed.SessionId)
Write-Output 'JSON_WRITE_DONE'
