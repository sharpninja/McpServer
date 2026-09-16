#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p7'
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

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T012355Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T012355Z.json'
$priorAgreeCutoff = [DateTime]::Parse('2026-08-22T01:23:55Z').ToUniversalTime()

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
            AfterCGreenP5P6Agree = ($lw -gt $priorAgreeCutoff)
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
            AfterCGreenP5P6Agree = $null
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
        MentionsCGreenP5P6 = ($agreeText -match 'C-green-P5-P6')
        MentionsAdapterNoMatch = ($agreeText -match 'Adapter_CapturesExecutable')
        MentionsNoMatchLiteral = ($agreeText -match 'NO_MATCH')
        MentionsPassed14 = ($agreeText -match 'Passed 14')
    }
    if (Test-Path -LiteralPath $priorAgreeJson) {
        $j = Get-Content -LiteralPath $priorAgreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
    }
    $agreeObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-cgreen-p5-p6-agree.json') -Encoding utf8
}

$testsCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapterTests.cs'
$adapterCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
$fakeCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\FakePluginProcessRunner.cs'
$testsSrc = if (Test-Path $testsCs) { Get-Content -LiteralPath $testsCs -Raw } else { '' }
$adapterSrc = if (Test-Path $adapterCs) { Get-Content -LiteralPath $adapterCs -Raw } else { '' }
$fakeSrc = if (Test-Path $fakeCs) { Get-Content -LiteralPath $fakeCs -Raw } else { '' }

$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$inlineHits = foreach ($h in $hostKinds) {
    [pscustomobject]@{
        HostKind = $h
        InlineDataPresent = [bool]($testsSrc -match ("\[InlineData\(PluginHostKind\.$h\)\]"))
    }
}

$forbidden = @(
    'Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint'
    'Adapter_ClineV1_StdioSessionLogTools'
    'Adapter_ClineV2AndOpenCode_UseProductionExport'
)
$forbiddenHits = foreach ($n in $forbidden) {
    $csHits = @(Get-ChildItem -Path 'F:\GitHub\McpServer\tests','F:\GitHub\McpServer\src' -Recurse -Include *.cs,*.csproj -ErrorAction SilentlyContinue |
        Select-String -Pattern $n -SimpleMatch)
    [pscustomobject]@{
        Name = $n
        MatchCount = $csHits.Count
        Paths = @($csHits | ForEach-Object { $_.Path + ':' + $_.LineNumber })
    }
}

$p7NameHits = @(Get-ChildItem -Path 'F:\GitHub\McpServer\tests','F:\GitHub\McpServer\src' -Recurse -Include *.cs,*.csproj -ErrorAction SilentlyContinue |
    Select-String -Pattern 'Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr' -SimpleMatch)

$skipHits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapterTests.cs' -Pattern 'Skip\s*=' -ErrorAction SilentlyContinue)

$scan = [ordered]@{
    NamedTestPresent = $testsSrc.Contains('Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr')
    IsTheory = [bool]($testsSrc -match '\[Theory\][\s\S]{0,800}public async Task Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr')
    SkipAttributeOnTheory = ($skipHits.Count -gt 0)
    SkipLiteralFactSkip = [bool]($testsSrc -match '\[Fact\([^\]]*Skip')
    SkipLiteralTheorySkip = [bool]($testsSrc -match '\[Theory\([^\]]*Skip')
    InlineDataCount = @($inlineHits | Where-Object { $_.InlineDataPresent }).Count
    InlineData = $inlineHits
    LaunchAsyncThrowsNotImplemented = [bool]($adapterSrc -match 'PluginHostProcessAdapter\.LaunchAsync is not implemented\.')
    LaunchAsyncHasOtherReturn = [bool]($adapterSrc -match 'return\s+')
    LaunchAsyncStoresRunner = [bool]($adapterSrc -match '_runner')
    FakeLastRequestPresent = $fakeSrc.Contains('LastRequest')
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
    P7NameMatchCount = $p7NameHits.Count
    P7NamePaths = @($p7NameHits | ForEach-Object { $_.Path + ':' + $_.LineNumber })
    ForbiddenNamedTests = $forbiddenHits
}
$scan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'adapter-scan.json') -Encoding utf8

$repos = @(
    'mcpserver-codex-plugin'
    'mcpserver-claude-code-plugin'
    'mcpserver-claude-cowork-plugin'
    'mcpserver-copilot-plugin'
    'mcpserver-grok-plugin'
    'mcpserver-cline-plugin'
    'mcpserver-cline-v2-plugin'
    'mcpserver-opencode-plugin'
)
$parent = Split-Path -Parent 'F:\GitHub\McpServer'
$catalog = Get-Content -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json' -Raw | ConvertFrom-Json
$sibling = foreach ($row in $catalog.scenarios) {
    $root = Join-Path $parent $row.repositoryName
    $entry = Join-Path $root ($row.entrypoint -replace '/', [IO.Path]::DirectorySeparatorChar)
    [pscustomobject]@{
        Name = $row.name
        HostKind = $row.hostKind
        Root = $root
        RootExists = (Test-Path -LiteralPath $root -PathType Container)
        Entrypoint = $entry
        EntrypointExists = (Test-Path -LiteralPath $entry -PathType Leaf)
        Enabled = [bool]$row.enabled
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
