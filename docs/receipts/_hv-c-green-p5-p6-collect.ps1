#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6'
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

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T005108Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T005108Z.json'
$priorAgreeCutoff = [DateTime]::Parse('2026-08-22T00:51:08Z').ToUniversalTime()
$implLog = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-678ba5b2f579\implementer\p5-p6-fixture-tests.log'

$paths = @(
    $priorAgree
    $priorAgreeJson
    $implLog
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixtureTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationProjectTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj'
    'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'
    'F:\GitHub\McpServer\src\McpServer.Support.Mcp\bin\Debug\net10.0\McpServer.Support.Mcp.dll'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\bin\Debug\net10.0\McpServer.Support.Mcp.dll'
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
            AfterCRedP5Agree = ($lw -gt $priorAgreeCutoff)
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
            AfterCRedP5Agree = $null
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
        MentionsCRedP5 = ($agreeText -match 'C-red-P5')
        MentionsStartAsyncNotImplemented = ($agreeText -match 'StartAsync is not implemented')
        MentionsFailed6 = ($agreeText -match 'Failed: 6')
    }
    if (Test-Path -LiteralPath $priorAgreeJson) {
        $j = Get-Content -LiteralPath $priorAgreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
    }
    $agreeObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-cred-p5-agree.json') -Encoding utf8
}

if (Test-Path -LiteralPath $implLog) {
    $implText = Get-Content -LiteralPath $implLog -Raw
    [ordered]@{
        Exists = $true
        Length = (Get-Item -LiteralPath $implLog).Length
        LastWriteTimeUtc = (Get-Item -LiteralPath $implLog).LastWriteTimeUtc.ToString('o')
        AfterCRedP5Agree = ((Get-Item -LiteralPath $implLog).LastWriteTimeUtc -gt $priorAgreeCutoff)
        MentionsPassed7 = ($implText -match 'Passed:\s+7')
        MentionsFailed0 = ($implText -match 'Failed:\s+0')
        MentionsSkipped0 = ($implText -match 'Skipped:\s+0')
        Tail = (($implText -split "`n") | Select-Object -Last 8) -join "`n"
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'implementer-log-scan.json') -Encoding utf8
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
    'ServerFixture_HealthReady_ExposesTrustedMarkerAndClient'
)
$methodHits = foreach ($n in $expectedNames) {
    $escaped = [regex]::Escape($n)
    [pscustomobject]@{
        Name = $n
        Present = $testsSrc.Contains($n)
        FactNearby = [bool]($testsSrc -match ("\[Fact[^\]]*\][\s\S]{0,400}public async Task $escaped\("))
        SkipNearby = [bool]($testsSrc -match ("\[Fact\([^\]]*Skip[\s\S]{0,400}public async Task $escaped\("))
        TraitSkip = [bool]($testsSrc -match ("\[Trait\([^\]]*Skip[\s\S]{0,400}public async Task $escaped\("))
        QueryAsyncNearby = [bool]($n -eq 'ServerFixture_HealthReady_ExposesTrustedMarkerAndClient' -and $testsSrc.Contains('QueryAsync'))
    }
}

$testDir = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
$csFiles = Get-ChildItem -LiteralPath $testDir -Filter '*.cs' -File
$adapterHits = @()
foreach ($f in $csFiles) {
    $txt = Get-Content -LiteralPath $f.FullName -Raw
    if ($txt.Contains('Adapter_CapturesExecutable')) {
        $adapterHits += $f.Name
    }
}

$scan = [ordered]@{
    StartAsyncThrowsNotImplemented = [bool]($fixtureSrc -match 'StartAsync is not implemented')
    DisposeAsyncThrowsNotImplemented = [bool]($fixtureSrc -match 'DisposeAsync is not implemented')
    StartAsyncHasWebApplication = [bool]($fixtureSrc -match 'WebApplication')
    StartAsyncHasWebApplicationFactory = [bool]($fixtureSrc -match 'WebApplicationFactory')
    StartAsyncHasKestrel = [bool]($fixtureSrc -match 'Kestrel')
    StartAsyncHasCreateBuilder = [bool]($fixtureSrc -match 'CreateBuilder')
    FileNameDotnet = $fixtureSrc.Contains('FileName = "dotnet"')
    ArgumentListAddsSupportDll = $fixtureSrc.Contains('McpServer.Support.Mcp.dll')
    ProcessStartCall = [bool]($fixtureSrc -match '_process\.Start\(\)')
    AllocateFreePortNever7147 = $fixtureSrc.Contains('ReservedServicePort = 7147') -and $fixtureSrc.Contains('port != ReservedServicePort')
    FixtureHardcodes7147PortAssignment = [bool]($fixtureSrc -match '(?m)Port\s*=\s*7147')
    IsolatedTempPrefix = $fixtureSrc.Contains('mcp-pluginint-')
    WorkspaceNotRepoRootAssignment = $fixtureSrc.Contains('WorkspacePath = Path.Combine(_rootPath, "workspace")')
    DatabaseUnderDataPath = $fixtureSrc.Contains('DatabasePath = Path.Combine(dataPath, "mcp.db")')
    MarkerWaitRequiresSignatureAndApiKey = REDACTED'text.Contains("signature:"') -and $fixtureSrc.Contains('TryReadMarkerScalar(text, "apiKey"')
    HealthNonceEchoWait = $fixtureSrc.Contains('/health?nonce=') -and $fixtureSrc.Contains('body.Contains(nonce')
    KillEntireProcessTree = $fixtureSrc.Contains('Kill(entireProcessTree: true)')
    DeletesRootPathRecursive = $fixtureSrc.Contains('Directory.Delete(_rootPath, recursive: true)')
    CreateTrustedClientFactory = $fixtureSrc.Contains('McpServerClientFactory.Create')
    TestsAssertNotEqual7147 = $testsSrc.Contains('Assert.NotEqual(7147, fixture.Port)')
    TestsAssertDbPathNot7147 = $testsSrc.Contains('Assert.DoesNotContain("7147", fixture.DatabasePath')
    TestsAssertWorkspaceNotRepo = $testsSrc.Contains('F:\GitHub\McpServer')
    TestsStartupTimeoutOneSecond = $testsSrc.Contains('CancellationTokenSource(TimeSpan.FromSeconds(1))') -and $testsSrc.Contains('OperationCanceledException')
    TestsHealthReadyQueryAsync = $testsSrc.Contains('client.Todo.QueryAsync')
    SkipLiteralInTests = [bool]($testsSrc -match '\[Fact\([^\]]*Skip')
    HealthReadyTestPresent = $testsSrc.Contains('ServerFixture_HealthReady_ExposesTrustedMarkerAndClient')
    AdapterCapturesExecutableInAnyCs = ($adapterHits.Count -gt 0)
    AdapterCapturesExecutableFiles = $adapterHits
    CsprojReferencesSupportMcp = $csprojSrc.Contains('McpServer.Support.Mcp.csproj')
    CsprojReferencesClient = $csprojSrc.Contains('McpServer.Client.csproj')
    NamedMethods = $methodHits
    NamedPresentCount = @($methodHits | Where-Object { $_.Present }).Count
    CsFileCount = @($csFiles).Count
}
$scan | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'fixture-scan.json') -Encoding utf8

git -C 'F:\GitHub\McpServer' log --follow --format='%H %cI %s' -- tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs | Out-File -FilePath (Join-Path $out 'git-log-fixture.txt') -Encoding utf8
git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests | Out-File -FilePath (Join-Path $out 'git-status-fixture.txt') -Encoding utf8

$rgOut = Join-Path $out 'rg-adapter-captures.txt'
$hits = @(Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\tests','F:\GitHub\McpServer\src' -Recurse -Include '*.cs','*.csproj' -File -ErrorAction SilentlyContinue | Select-String -Pattern 'Adapter_CapturesExecutable' -SimpleMatch)
if ($hits.Count -eq 0) {
    Set-Content -LiteralPath $rgOut -Value 'NO_MATCH' -Encoding utf8
} else {
    $hits | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line } | Set-Content -LiteralPath $rgOut -Encoding utf8
}

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test'
Invoke-Plugin -Method 'workflow.requirements.getMapping' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map-list'

Write-Output 'COLLECT_DONE'
