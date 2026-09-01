#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$doc = [ordered]@{
    TimestampUtc = '2026-08-18T00:52:58Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 'user-directed-incident-correction-class-2'
    AddProfileExecuted = $true
    ProfileFilesRead = 18
    PluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
    PluginVersion = '1.93.0'
    MarkerSignature = $true
    HealthNonce = 'a5aabdd823f642b8b82084b9b7a86d76'
    HealthNonceEchoed = $true
    HealthStorage = 'reachable'
    FullBootstrap = $true
    SessionId = 'GrokCode-20260818T005258Z-hostile-grok-503'
    RequestId = 'req-20260818T005258Z-001-hostile-validate-grok-503'
    ServerTurnId = 41618
    PlanFile = 'None'
    TodoId = 'None'
    OverallVerdict = 'AGREE'
    FailList = @()
    SurfacesNotEvaluated = @()
    Accuracy = 97
    Completeness = 96
    SessionLogQuery = [ordered]@{
        TextEqualsSessionIdTotalCount = 0
        TextHostileGrok503Hit = 'GrokCode-20260818T001225Z-plugin-session'
        AgentFromTotalCount = 1
        AgentFromSessionId = 'GrokCode-20260818T005258Z-hostile-grok-503'
        AgentFromTurnRequestId = 'req-20260818T005258Z-001-hostile-validate-grok-503'
        AgentFromTurnStatus = 'completed'
        AgentFromTurnId = 41618
        DialogCount = 5
        ActionCount = 5
        PlanFile = 'None'
        TodoId = 'None'
    }
    Service = [ordered]@{
        State = 'Running'
        ProcessId = 57744
        StartMode = 'Auto'
        StartName = 'LocalSystem'
        PathName = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147'
    }
    LiveYaml = [ordered]@{
        Path = 'C:\ProgramData\McpServer\appsettings.yaml'
        LastWriteTimeUtc = '2026-08-17T23:30:09.0404870Z'
        Length = 58975
        Sha256 = 'B42E2462D67EADE136EC3BF64A1224BF1253ADB73EA6596CFED1BC7C7A4E3D46'
        DatabaseProvider = 'sqlserver'
        TodoStorageProvider = 'database'
    }
    Failsafe = [ordered]@{
        Path = 'F:\GitHub\McpServer\.mcpServer\grok\failsafe\20260818T001239Z-session_submit-a650.yaml'
        Method = 'client.SessionLog.SubmitAsync'
        Has503 = $false
        HasBackendUnavailable = $false
        HasInternalServerError = $true
        TurnStatus = 'canceled'
        TurnRequestId = 'req-20260818T001131Z-prompt-b813'
        HasPlanFileKey = $false
        HasTodoIdKey = $false
        SessionId = 'GrokCode-20260818T001225Z-plugin-session'
        SourceType = 'GrokCode'
    }
    LogWindow1838 = [ordered]@{
        Lines = 842
        Unreachable = 0
        BackendUnavailable = 0
        Status503 = 0
        FirstHealthStorage = 'reachable'
        FirstHealthNonce = 'ffbf87a5a57c46cdada44497d922e256'
    }
    LogWindow1852 = [ordered]@{
        ProbeTimeoutLocal = '2026-08-17 18:52:13.856 -05:00'
        UnhandledTrace = '00-aab0888980690d5c55a8af5c029f0bd1-9c0f446ccbcb5618-01'
        ReplaceSectionSessionId = 'GrokCode-20260817T120000Z-agent-help-grok-cli'
        InteractionLogStatus = 200
        InteractionLogOutput = '(none)'
        SqlException = 'Named Pipes Provider, error: 40'
        InnerException = 'Win32Exception (5): Access is denied'
        Stack = 'WorkspaceService.EnsureBootstrappedAsync line 407'
        Health185226Storage = 'unreachable'
    }
    Claims = [ordered]@{
        A1 = 'PASS'
        A2 = 'PASS'
        A3 = 'PASS'
        A4 = 'PASS'
        A5 = 'PASS'
        A6 = 'PASS'
        B1 = 'PASS'
        B2 = 'PASS'
        B3 = 'PASS'
        B4 = 'PASS'
        B5 = 'PASS'
        C = 'N/A'
        D = 'N/A'
    }
}

$out = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T005258Z.json'
$json = $doc | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($out, $json)
$item = Get-Item -LiteralPath $out
Write-Output ('WROTE=' + $out)
Write-Output ('LEN=' + $item.Length)
Write-Output ('LWT_UTC=' + $item.LastWriteTimeUtc.ToString('o'))
$round = Get-Content -LiteralPath $out -Raw | ConvertFrom-Json
Write-Output ('ROUNDTRIP_VERDICT=' + $round.OverallVerdict)
Write-Output ('ROUNDTRIP_SESSION=' + $round.SessionId)
Write-Output ('ROUNDTRIP_A2=' + $round.Claims.A2)
Write-Output ('ROUNDTRIP_FAILS=' + @($round.FailList).Count)
