#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$obj = [ordered]@{
    TimestampUtc = '2026-08-22T00:51:08Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 1
    Phase = 'C-red-P5'
    Plan = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    TodoId = 'PLAN-PLUGINHANDOFF-001'
    ChildTodoId = 'MCP-PLUGININT-001'
    AddProfile = [ordered]@{
        executed = $true
        profileFilesRead = 18
        excludedSkillPorts = @('add-profile.grok.md')
    }
    Plugin = [ordered]@{
        root = 'F:\GitHub\mcpserver-grok-plugin'
        version = '1.99.0'
    }
    PriorReceipt = 'docs/receipts/hostile-validator-20260822T003817Z.md'
    Collector = 'docs/receipts/_hv-c-red-p5/'
    SessionId = 'GrokSubagentHostile-20260822T004733Z-c-red-p5'
    RequestId = 'req-20260822T004733Z-001-hostile-c-red-p5-fixture'
    TurnId = 42827
    HealthNonce = '22f296d079bc456491d751e4e67d0861'
    HealthNonceMatch = $true
    PluginMarkerSignature = $true
    InvokeFullBootstrap = $true
    HomemadeHmacMatch = $false
    ToolSearchExactNamePresent = $true
    OverallVerdict = 'AGREE'
    FailCount = 0
    UnknownCount = 0
    PassCount = 14
    Accuracy = 97
    Completeness = 96
    ExplicitFailList = @()
    MandatorySurfacesUnevaluated = @()
    TestResults = [ordered]@{
        FilterCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginIntegrationServerFixtureTests'
        FilterExitCode = 1
        FilterPassed = 0
        FilterFailed = 6
        FilterSkippedConsole = 0
        FilterTrxNotExecuted = 0
        FilterNamedFailed = @(
            'McpServer.PluginIntegration.Tests.PluginIntegrationServerFixtureTests.ServerFixture_SignatureAndNonceTrust'
            'McpServer.PluginIntegration.Tests.PluginIntegrationServerFixtureTests.ServerFixture_CreatesMarker'
            'McpServer.PluginIntegration.Tests.PluginIntegrationServerFixtureTests.ServerFixture_CreatesIsolatedTempWorkspaceAndDatabase'
            'McpServer.PluginIntegration.Tests.PluginIntegrationServerFixtureTests.ServerFixture_DeterministicCleanup'
            'McpServer.PluginIntegration.Tests.PluginIntegrationServerFixtureTests.ServerFixture_StartupTimeout'
            'McpServer.PluginIntegration.Tests.PluginIntegrationServerFixtureTests.ServerFixture_SelectsFreePort'
        )
        AllCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug'
        AllExitCode = 1
        AllPassed = 7
        AllFailed = 6
        AllSkippedConsole = 0
        AllTrxNotExecuted = 0
        StartAsyncThrowsNotImplemented = $true
        HealthReadyTestPresent = $false
    }
    TodoFlags = [ordered]@{
        'PLAN-PLUGINHANDOFF-001' = $false
        'MCP-PLUGININT-001' = $false
        'MCP-PLUGININT-001-P5' = $false
        'MCP-PLUGININT-001-P6' = $false
    }
    Timestamps = [ordered]@{
        CGreenP4AgreeName = '2026-08-22T00:38:17Z'
        FixtureTestsLastWriteTimeUtc = '2026-08-22T00:42:25.5806890Z'
        FixtureCsLastWriteTimeUtc = '2026-08-22T00:43:19.5225186Z'
        CatalogTestsLastWriteTimeUtc = '2026-08-22T00:06:51.8604136Z'
        FixtureFilesAfterCGreenP4Agree = $true
        CatalogTestsAfterCGreenP4Agree = $false
        FixtureGitStatus = 'untracked'
    }
    FixtureFacts = [ordered]@{
        NamedPresentCount = 6
        StartAsyncThrowsNotImplemented = $true
        DisposeAsyncThrowsNotImplemented = $true
        StartAsyncHasWebApplication = $false
        StartAsyncHasKestrel = $false
        FixtureHardcodes7147Port = $false
        TestsAssertNotEqual7147 = $true
        TestsAssertDbPathNot7147 = $true
        SkipLiteralInTests = $false
        CsprojReferencesSupportMcp = $false
        CsprojReferencesClientOnly = $true
    }
    Attacks = [ordered]@{
        FixtureAlreadyStartsRealHost = $false
        TestsSkipped = $false
        P5GreenMixedIn = $false
        Developer7147Reused = $false
        PlanDone = $false
    }
}

$json = $obj | ConvertTo-Json -Depth 8
Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T005108Z.json' -Value $json -Encoding utf8
Write-Output 'JSON_WRITTEN'
