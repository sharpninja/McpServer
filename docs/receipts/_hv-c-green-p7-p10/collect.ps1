#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p7-p10'
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
$cacheRoot = Join-Path $out 'plugin-cache'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T014442Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T014442Z.json'
$priorAgreeCutoff = [DateTime]::Parse('2026-08-22T01:44:42Z').ToUniversalTime()

$paths = @(
    $priorAgree
    $priorAgreeJson
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapterTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\FakePluginProcessRunner.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\IPluginProcessRunner.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginProcessLaunchRequest.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginProcessLaunchResult.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostKind.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixtureTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationProjectTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
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
            AfterCRedP7Agree = ($lw -gt $priorAgreeCutoff)
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
            AfterCRedP7Agree = $null
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
        MentionsCRedP7 = ($agreeText -match 'C-red-P7')
        MentionsNotImplemented = ($agreeText -match 'not implemented')
        MentionsFailed8 = ($agreeText -match 'Failed 8')
        MentionsPassed14 = ($agreeText -match 'Passed 14')
        MentionsSkipped0 = ($agreeText -match 'Skipped 0')
        MentionsP8Absent = ($agreeText -match 'P8-P10 named tests are absent')
    }
    if (Test-Path -LiteralPath $priorAgreeJson) {
        $j = Get-Content -LiteralPath $priorAgreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
        $agreeObj.LaunchAsyncThrowsNotImplemented = [bool]$j.TestResults.LaunchAsyncThrowsNotImplemented
        $agreeObj.P8NamedTestsPresent = [bool]$j.TestResults.P8NamedTestsPresent
        $agreeObj.FilterFailed = $j.TestResults.FilterFailed
        $agreeObj.AllPassed = $j.TestResults.AllPassed
        $agreeObj.AllFailed = $j.TestResults.AllFailed
    }
    $agreeObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-cred-p7-agree.json') -Encoding utf8
}

$testsCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapterTests.cs'
$adapterCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
$fakeCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\FakePluginProcessRunner.cs'
$testsSrc = if (Test-Path $testsCs) { Get-Content -LiteralPath $testsCs -Raw } else { '' }
$adapterSrc = if (Test-Path $adapterCs) { Get-Content -LiteralPath $adapterCs -Raw } else { '' }
$fakeSrc = if (Test-Path $fakeCs) { Get-Content -LiteralPath $fakeCs -Raw } else { '' }

$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$inlineHitsP7 = foreach ($h in $hostKinds) {
    [pscustomobject]@{
        HostKind = $h
        InlineDataPresent = [bool]($testsSrc -match ("\[InlineData\(PluginHostKind\.$h\)\]"))
    }
}
$psHookKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok')
$inlineHitsP8 = foreach ($h in $psHookKinds) {
    [pscustomobject]@{
        HostKind = $h
        P8Inline = [bool]($testsSrc -match ("Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint[\s\S]{0,1200}\[InlineData\(PluginHostKind\.$h\)\]|\[InlineData\(PluginHostKind\.$h\)\][\s\S]{0,400}public async Task Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint"))
    }
}

$p11Hits = @(Get-ChildItem -Path 'F:\GitHub\McpServer\tests','F:\GitHub\McpServer\src' -Recurse -Include *.cs,*.csproj -ErrorAction SilentlyContinue |
    Select-String -Pattern 'Theory_EachScenario_FailsUntilAdapterOperational' -SimpleMatch)

$named = @(
    'Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr'
    'Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint'
    'Adapter_ClineV1_StdioSessionLogTools'
    'Adapter_ClineV2AndOpenCode_UseProductionExport'
)
$namedHits = foreach ($n in $named) {
    $csHits = @(Get-ChildItem -Path 'F:\GitHub\McpServer\tests','F:\GitHub\McpServer\src' -Recurse -Include *.cs,*.csproj -ErrorAction SilentlyContinue |
        Select-String -Pattern $n -SimpleMatch)
    [pscustomobject]@{
        Name = $n
        MatchCount = $csHits.Count
        Paths = @($csHits | ForEach-Object { $_.Path + ':' + $_.LineNumber })
    }
}

$skipHits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapterTests.cs' -Pattern 'Skip\s*=' -ErrorAction SilentlyContinue)
$skipIfHits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Assert\.Skip|Skip\.If|\[Fact\([^\]]*Skip|\[Theory\([^\]]*Skip' -ErrorAction SilentlyContinue)
$httpHits = @(Select-String -Path $adapterCs -Pattern 'HttpClient|Invoke-RestMethod|/mcpserver/' -ErrorAction SilentlyContinue)
$notImplHits = @(Select-String -Path $adapterCs -Pattern 'not implemented' -ErrorAction SilentlyContinue)
$runnerCallHits = @(Select-String -Path $adapterCs -Pattern '_runner\.RunAsync' -ErrorAction SilentlyContinue)
$dummyReturnHits = @(Select-String -Path $adapterCs -Pattern 'new PluginProcessLaunchResult' -ErrorAction SilentlyContinue)
$lastRequestAssign = @(Select-String -Path $fakeCs -Pattern 'LastRequest\s*=' -ErrorAction SilentlyContinue)

$emDashAdapter = $adapterSrc.Contains([char]0x2014) -or $adapterSrc.Contains([char]0x2013)
$emDashTests = $testsSrc.Contains([char]0x2014) -or $testsSrc.Contains([char]0x2013)

$scan = [ordered]@{
    NamedTests = $namedHits
    P7IsTheory = [bool]($testsSrc -match '\[Theory\][\s\S]{0,800}public async Task Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr')
    P8IsTheory = [bool]($testsSrc -match '\[Theory\][\s\S]{0,800}public async Task Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint')
    P9IsFact = [bool]($testsSrc -match '\[Fact\][\s\S]{0,400}public async Task Adapter_ClineV1_StdioSessionLogTools')
    P10IsTheory = [bool]($testsSrc -match '\[Theory\][\s\S]{0,800}public async Task Adapter_ClineV2AndOpenCode_UseProductionExport')
    SkipAttributeOnAdapterTests = ($skipHits.Count -gt 0)
    SkipLiteralFactSkip = [bool]($testsSrc -match '\[Fact\([^\]]*Skip')
    SkipLiteralTheorySkip = [bool]($testsSrc -match '\[Theory\([^\]]*Skip')
    SkipIfOrAssertSkipCount = $skipIfHits.Count
    SkipIfPaths = @($skipIfHits | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
    P7InlineDataCount = @($inlineHitsP7 | Where-Object { $_.InlineDataPresent }).Count
    P7InlineData = $inlineHitsP7
    P8InlineData = $inlineHitsP8
    LaunchAsyncThrowsNotImplemented = [bool]($adapterSrc -match 'PluginHostProcessAdapter\.LaunchAsync is not implemented\.')
    NotImplementedHitCount = $notImplHits.Count
    RunnerRunAsyncHitCount = $runnerCallHits.Count
    RunnerRunAsyncLines = @($runnerCallHits | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
    DummyLaunchResultCtorCount = $dummyReturnHits.Count
    DummyLaunchResultLines = @($dummyReturnHits | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
    AdapterHttpClientHits = $httpHits.Count
    FakeLastRequestPresent = $fakeSrc.Contains('LastRequest')
    FakeLastRequestAssignCount = $lastRequestAssign.Count
    FakeLastRequestAssignLines = @($lastRequestAssign | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
    FakeLastRequestPrivateSet = [bool]($fakeSrc -match 'LastRequest\s*\{\s*get;\s*private set;')
    AssertLastRequestNotNull = $testsSrc.Contains('Assert.NotNull(fake.LastRequest)')
    AssertExpectedExecutable = $testsSrc.Contains('Assert.Equal(ExpectedExecutable(hostKind), fake.LastRequest.Executable')
    ExpectedExecutablePwsh = $testsSrc.Contains('IsPowerShellHost(hostKind) ? "pwsh.exe" : "node"')
    PowerShellHostsIncludeFive = (
        $testsSrc.Contains('PluginHostKind.Codex') -and
        $testsSrc.Contains('PluginHostKind.ClaudeCode') -and
        $testsSrc.Contains('PluginHostKind.ClaudeCowork') -and
        $testsSrc.Contains('PluginHostKind.Copilot') -and
        $testsSrc.Contains('PluginHostKind.Grok')
    )
    AssertEntrypointInArgs = $testsSrc.Contains('Assert.Contains(entrypointPath, fake.LastRequest.Arguments')
    AssertNoProfile = $testsSrc.Contains('Assert.Contains("-NoProfile", fake.LastRequest.Arguments')
    AssertNonInteractive = $testsSrc.Contains('Assert.Contains("-NonInteractive", fake.LastRequest.Arguments')
    AssertFile = $testsSrc.Contains('Assert.Contains("-File", fake.LastRequest.Arguments')
    AssertStdinExact = $testsSrc.Contains('Assert.Equal(stdin, fake.LastRequest.StandardInput)')
    AssertWorkingDirectoryPluginRoot = $testsSrc.Contains('Assert.Equal(pluginRoot, fake.LastRequest.WorkingDirectory')
    AssertTimeoutForwarded = $testsSrc.Contains('Assert.Equal(timeout, fake.LastRequest.Timeout)')
    AssertExitFromFake = $testsSrc.Contains('Assert.Equal(expectedExit, result.ExitCode)')
    AssertStdoutFromFake = $testsSrc.Contains('Assert.Equal(expectedStdout, result.StandardOutput)')
    AssertStderrFromFake = $testsSrc.Contains('Assert.Equal(expectedStderr, result.StandardError)')
    AssertRequiredEnvPresent = $testsSrc.Contains('Assert.All(scenario.RequiredEnvironmentVariables')
    AssertPluginAgentName = $testsSrc.Contains('Assert.Equal(scenario.AgentSourceType, fake.LastRequest.Environment["PLUGIN_AGENT_NAME"])')
    AssertPluginRootEnv = $testsSrc.Contains('Assert.Equal(pluginRoot, fake.LastRequest.Environment[rootVariable]')
    P8AssertPwsh = $testsSrc.Contains('Assert.Equal("pwsh.exe", fake.LastRequest!.Executable')
    P8AssertCodexEntrypoint = $testsSrc.Contains('Assert.EndsWith("Invoke-CodexMcpPlugin.ps1", entrypointPath')
    P8AssertInvokeMcpPlugin = $testsSrc.Contains('Assert.Contains("Invoke-McpPlugin.ps1", scenario.Entrypoint')
    P8AssertNoReplInvoke = $testsSrc.Contains('Assert.DoesNotContain(fake.LastRequest.Arguments, argument => argument.Contains("repl-invoke.ps1"')
    P9AssertNode = [bool]($testsSrc -match 'Adapter_ClineV1_StdioSessionLogTools[\s\S]{0,1200}Assert\.Equal\("node"')
    P9AssertDistIndex = $testsSrc.Contains('Assert.EndsWith("dist" + Path.DirectorySeparatorChar + "index.js"')
    P9AssertSessionlogStdin = $testsSrc.Contains('Assert.Contains("sessionlog", fake.LastRequest.StandardInput')
    P10AssertNode = [bool]($testsSrc -match 'Adapter_ClineV2AndOpenCode_UseProductionExport[\s\S]{0,900}Assert\.Equal\("node"')
    P10AssertNoInvokeMcpPlugin = $testsSrc.Contains('argument.Contains("Invoke-McpPlugin.ps1"')
    P11MatchCountTestsSrc = $p11Hits.Count
    P11Paths = @($p11Hits | ForEach-Object { $_.Path + ':' + $_.LineNumber })
    EmDashAdapter = $emDashAdapter
    EmDashTests = $emDashTests
    AdapterReturnsRunnerRunAsync = [bool]($adapterSrc -match 'return\s+_runner\.RunAsync\(')
    AdapterMapsPwshFile = [bool]($adapterSrc -match 'executable\s*=\s*"pwsh\.exe"' -and $adapterSrc -match 'arguments\.Add\("-File"\)')
    AdapterMapsNode = [bool]($adapterSrc -match 'executable\s*=\s*"node"')
    AdapterSetsPluginAgentName = $adapterSrc.Contains('environment["PLUGIN_AGENT_NAME"] = scenario.AgentSourceType')
    AdapterSetsPluginRootEnv = [bool]($adapterSrc -match 'EndsWith\("_PLUGIN_ROOT"')
    AdapterWorkingDirectoryPluginRoot = $adapterSrc.Contains('WorkingDirectory = pluginRoot')
    AdapterTimeoutForwarded = $adapterSrc.Contains('Timeout = timeout')
    AdapterStdinForwarded = $adapterSrc.Contains('StandardInput = standardInput')
}
$scan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'adapter-scan.json') -Encoding utf8

$catalog = Get-Content -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json' -Raw | ConvertFrom-Json
$parent = Split-Path -Parent 'F:\GitHub\McpServer'
$sibling = foreach ($row in $catalog.scenarios) {
    $root = Join-Path $parent $row.repositoryName
    $entry = Join-Path $root ($row.entrypoint -replace '/', [IO.Path]::DirectorySeparatorChar)
    [pscustomobject]@{
        Name = $row.name
        HostKind = $row.hostKind
        Root = $root
        RootExists = (Test-Path -LiteralPath $root -PathType Container)
        Entrypoint = $entry
        EntrypointDeclared = [string]$row.entrypoint
        EntrypointExists = (Test-Path -LiteralPath $entry -PathType Leaf)
        Enabled = [bool]$row.enabled
        IsReplInvoke = ([string]$row.entrypoint -match 'repl-invoke\.ps1')
        AgentSourceType = $row.agentSourceType
        RequiredEnv = @($row.requiredEnvironmentVariables)
    }
}
$sibling | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'sibling-plugin-probe.json') -Encoding utf8

git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests | Out-File -FilePath (Join-Path $out 'git-status-pluginint.txt') -Encoding utf8
git -C 'F:\GitHub\McpServer' log --format='%H %cI %s' -n 8 -- tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapterTests.cs tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapter.cs | Out-File -FilePath (Join-Path $out 'git-log-adapter.txt') -Encoding utf8

$pythonHits = @(Get-ChildItem -Path $out -Filter *.ps1 -ErrorAction SilentlyContinue | Select-String -Pattern '\bpython(3)?\b|\bpy\.exe\b')
[ordered]@{
    CollectorPythonHits = $pythonHits.Count
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'no-python-scan.json') -Encoding utf8

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map'

$verPath = 'F:\GitHub\mcpserver-grok-plugin\.version'
$pjPath = 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json'
$verObj = [ordered]@{
    VersionFile = if (Test-Path $verPath) { (Get-Content -LiteralPath $verPath -Raw).Trim() } else { $null }
    PluginJsonVersion = $null
}
if (Test-Path $pjPath) {
    $pj = Get-Content -LiteralPath $pjPath -Raw | ConvertFrom-Json
    $verObj.PluginJsonVersion = $pj.version
}
$verObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-version.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
