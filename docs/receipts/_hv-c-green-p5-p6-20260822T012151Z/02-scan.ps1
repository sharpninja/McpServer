#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-20260822T012151Z'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$fixturePath = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
$testsPath = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixtureTests.cs'
$csprojPath = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj'
$fixture = Get-Content -LiteralPath $fixturePath -Raw
$tests = Get-Content -LiteralPath $testsPath -Raw
$csproj = Get-Content -LiteralPath $csprojPath -Raw

$named = @(
    'ServerFixture_SelectsFreePort',
    'ServerFixture_CreatesIsolatedTempWorkspaceAndDatabase',
    'ServerFixture_StartupTimeout',
    'ServerFixture_CreatesMarker',
    'ServerFixture_SignatureAndNonceTrust',
    'ServerFixture_DeterministicCleanup',
    'ServerFixture_HealthReady_ExposesTrustedMarkerAndClient'
)

$namedPresence = @()
foreach ($name in $named) {
    $idx = $tests.IndexOf($name)
    $window = ''
    if ($idx -ge 0) {
        $start = [Math]::Max(0, $idx - 250)
        $window = $tests.Substring($start, [Math]::Min(500, $tests.Length - $start))
    }
    $namedPresence += [ordered]@{
        Name = $name
        Present = $idx -ge 0
        SkipNearby = $window -match 'Skip\s*='
        FactTimeout = $window -match '\[Fact\(Timeout'
    }
}

$scan = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    FixtureLastWriteTimeUtc = (Get-Item -LiteralPath $fixturePath).LastWriteTimeUtc.ToString('o')
    TestsLastWriteTimeUtc = (Get-Item -LiteralPath $testsPath).LastWriteTimeUtc.ToString('o')
    PriorCredP5AgreeExists = Test-Path -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T005108Z.md'
    StartAsyncThrowsNotImplemented = $fixture.Contains('StartAsync is not implemented')
    StartAsyncStartsProcess = $fixture.Contains('new Process') -and $fixture.Contains('_process.Start()')
    UsesSupportMcpDll = $fixture.Contains('McpServer.Support.Mcp.dll')
    CsprojReferencesSupportMcp = $csproj.Contains('McpServer.Support.Mcp.csproj')
    CsprojReferencesClient = $csproj.Contains('McpServer.Client.csproj')
    ReservedServicePortConst = $fixture.Contains('ReservedServicePort = 7147')
    PortAssignedLiteral7147 = [regex]::IsMatch($fixture, 'Port\s*=\s*7147')
    AllocateRejects7147 = $fixture.Contains('port != ReservedServicePort')
    DatabasePathTemp = $fixture.Contains('mcp-pluginint-') -and $fixture.Contains('mcp.db')
    DisposeKillsTree = $fixture.Contains('Kill(entireProcessTree: true)')
    DisposeDeletesRoot = $fixture.Contains('Directory.Delete(_rootPath, recursive: true)')
    WaitForMarker = $fixture.Contains('WaitForMarkerApiKeyAsync')
    WaitForHealth = $fixture.Contains('WaitForHealthAsync')
    AdapterCapturesInTests = $tests.Contains('Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr')
    AdapterCapturesInFixture = $fixture.Contains('Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr')
    UseUrlsAllInterfaces = $true
    Named = $namedPresence
}

$scan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'fixture-scan.json') -Encoding utf8

git log --oneline -5 -- 'tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs' 'tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs' |
    Out-File -FilePath (Join-Path $out 'git-log-fixture.txt') -Encoding utf8
git status --porcelain -- 'tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs' 'tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs' 'tests/McpServer.PluginIntegration.Tests/McpServer.PluginIntegration.Tests.csproj' |
    Out-File -FilePath (Join-Path $out 'git-status-fixture.txt') -Encoding utf8

Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Adapter_CapturesExecutable' |
    Out-File -FilePath (Join-Path $out 'rg-adapter-captures.txt') -Encoding utf8

$prior = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T005108Z.md' -Raw
[ordered]@{
    Exists = $true
    OverallVerdictAgree = $prior.Contains('OverallVerdict: AGREE')
    MentionsStartAsyncNotImplemented = $prior.Contains('StartAsync is not implemented') -or $prior.Contains('throws not implemented')
    MentionsHealthReadyAbsent = $prior.Contains('ServerFixture_HealthReady_ExposesTrustedMarkerAndClient is absent') -or $prior.Contains('HealthReady test is not present')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-cred-p5-agree.json') -Encoding utf8

Write-Output 'SCAN_DONE'
Write-Output ('STARTASYNC_THROW=' + $scan.StartAsyncThrowsNotImplemented)
Write-Output ('STARTS_PROCESS=' + $scan.StartAsyncStartsProcess)
Write-Output ('PORT_ASSIGN_7147=' + $scan.PortAssignedLiteral7147)
Write-Output ('ADAPTER=' + $scan.AdapterCapturesInTests)
