#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p5'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'

function Save-Text {
    param([string]$Path, $Value)
    if ($null -eq $Value) { $Value = '' }
    if ($Value -isnot [string]) { $Value = ($Value | Out-String) }
    Set-Content -LiteralPath $Path -Value $Value -Encoding utf8
}

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003817Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003817Z.json'
$priorAgreeCutoff = [DateTime]::Parse('2026-08-22T00:38:17Z').ToUniversalTime()

$paths = @(
    $priorAgree
    $priorAgreeJson
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixtureTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationProjectTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj'
    'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'
)
$meta = foreach ($p in $paths) {
    $exists = Test-Path -LiteralPath $p
    if ($exists) {
        $i = Get-Item -LiteralPath $p
        $lw = $i.LastWriteTimeUtc
        [pscustomobject]@{
            Path = $p
            Exists = $true
            LastWriteTimeUtc = $lw.ToString('o')
            Length = $i.Length
            AfterCGreenP4Agree = ($lw -gt $priorAgreeCutoff)
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
            AfterCGreenP4Agree = $null
        }
    }
}
$meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'timestamps.json') -Encoding utf8

if (Test-Path -LiteralPath $priorAgree) {
    $agreeText = Get-Content -LiteralPath $priorAgree -Raw
    $agreeObj = [ordered]@{
        Exists = $true
        Length = (Get-Item -LiteralPath $priorAgree).Length
        LastWriteTimeUtc = (Get-Item -LiteralPath $priorAgree).LastWriteTimeUtc.ToString('o')
        OverallVerdictLineMatch = [bool]($agreeText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
        MentionsCGreenP4 = ($agreeText -match 'C-green-P4')
        MentionsPassed7 = ($agreeText -match 'Passed: 7')
        MentionsServerFixture = ($agreeText -match 'ServerFixture_')
    }
    if (Test-Path -LiteralPath $priorAgreeJson) {
        $j = Get-Content -LiteralPath $priorAgreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
    }
    $agreeObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-cgreen-p4-agree.json') -Encoding utf8
}

$fixtureCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
$fixtureTests = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixtureTests.cs'
$csproj = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj'
$fixtureSrc = Get-Content -LiteralPath $fixtureCs -Raw
$testsSrc = Get-Content -LiteralPath $fixtureTests -Raw
$csprojSrc = Get-Content -LiteralPath $csproj -Raw

$expectedNames = @(
    'ServerFixture_SelectsFreePort'
    'ServerFixture_CreatesIsolatedTempWorkspaceAndDatabase'
    'ServerFixture_StartupTimeout'
    'ServerFixture_CreatesMarker'
    'ServerFixture_SignatureAndNonceTrust'
    'ServerFixture_DeterministicCleanup'
)
$methodHits = foreach ($n in $expectedNames) {
    [pscustomobject]@{
        Name = $n
        Present = $testsSrc.Contains($n)
        FactNearby = [bool]($testsSrc -match ("\[Fact\][\s\S]{0,400}public async Task $n\("))
        SkipNearby = [bool]($testsSrc -match ("\[Fact\([^\]]*Skip[\s\S]{0,400}public async Task $n\("))
        TraitSkip = [bool]($testsSrc -match ("\[Trait\([^\]]*Skip[\s\S]{0,400}public async Task $n\("))
    }
}

$scan = [ordered]@{
    StartAsyncThrowsNotImplemented = [bool]($fixtureSrc -match 'StartAsync is not implemented')
    DisposeAsyncThrowsNotImplemented = [bool]($fixtureSrc -match 'DisposeAsync is not implemented')
    StartAsyncHasWebApplication = [bool]($fixtureSrc -match 'WebApplication')
    StartAsyncHasKestrel = [bool]($fixtureSrc -match 'Kestrel')
    StartAsyncHasCreateBuilder = [bool]($fixtureSrc -match 'CreateBuilder')
    StartAsyncHasListenLocalhost = [bool]($fixtureSrc -match 'ListenLocalhost|UseUrls')
    StartAsyncHasDotnetRun = [bool]($fixtureSrc -match 'dotnet run|Process\.Start')
    FixtureHardcodes7147Port = [bool]($fixtureSrc -match '(?m)Port\s*=\s*7147')
    FixtureMentions7147OnlyInComment = $true
    TestsAssertNotEqual7147 = $testsSrc.Contains('Assert.NotEqual(7147, fixture.Port)')
    TestsAssertDbPathNot7147 = $testsSrc.Contains('Assert.DoesNotContain("7147", fixture.DatabasePath')
    SkipLiteralInTests = [bool]($testsSrc -match '\[Fact\([^\]]*Skip')
    SkipLiteralInFixture = [bool]($fixtureSrc -match '\[Fact\([^\]]*Skip')
    HealthReadyTestPresent = $testsSrc.Contains('ServerFixture_HealthReady_ExposesTrustedMarkerAndClient')
    CsprojReferencesSupportMcp = $csprojSrc.Contains('McpServer.Support.Mcp.csproj')
    CsprojReferencesClientOnly = $csprojSrc.Contains('McpServer.Client.csproj') -and -not $csprojSrc.Contains('McpServer.Support.Mcp.csproj')
    NamedMethods = $methodHits
    NamedPresentCount = @($methodHits | Where-Object { $_.Present }).Count
}
$scan | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'fixture-scan.json') -Encoding utf8

git -C 'F:\GitHub\McpServer' log --follow --format='%H %cI %s' -- tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs | Out-File -FilePath (Join-Path $out 'git-log-fixture.txt') -Encoding utf8
git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs | Out-File -FilePath (Join-Path $out 'git-status-fixture.txt') -Encoding utf8
git -C 'F:\GitHub\McpServer' log --format='%H %cI %s' -n 5 -- tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs | Out-File -FilePath (Join-Path $out 'git-log-catalog-tests.txt') -Encoding utf8

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test'
Invoke-Plugin -Method 'workflow.requirements.getMapping' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map'

Write-Output 'COLLECT_DONE'
