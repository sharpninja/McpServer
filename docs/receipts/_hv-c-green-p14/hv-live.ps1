#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
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

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Set-Content -LiteralPath (Join-Path $out 'hv-live-stamp.txt') -Value $utc -Encoding utf8
Write-Output ("LIVE_UTC=$utc")

$procRows = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match 'testhost|vstest|dotnet|pwsh' -and
    $null -ne $_.CommandLine -and
    ($_.CommandLine -match 'PluginIntegration|McpServer.PluginIntegration|_hv-c-green-p14|p14-green')
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 300) { $cmd = $cmd.Substring(0, 300) }
    [ordered]@{
        ProcessId = $_.ProcessId
        Name = $_.Name
        CommandLine = $cmd
    }
})
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = $procRows.Count
    Rows = $procRows
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'hv-live-procs.json') -Encoding utf8
Write-Output ("PLUGININT_PROCS=" + $procRows.Count)

$pyRows = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match '^(python|python3|py)\.exe$'
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 240) { $cmd = $cmd.Substring(0, 240) }
    [ordered]@{ ProcessId = $_.ProcessId; Name = $_.Name; CommandLine = $cmd }
})
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    PythonProcessCount = $pyRows.Count
    Processes = $pyRows
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'hv-live-python.json') -Encoding utf8

$gitTodo = & git status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml 2>&1 | Out-String
$gitPluginInt = & git status --porcelain -- tests/McpServer.PluginIntegration.Tests 2>&1 | Out-String
$gitAdapter = & git diff --stat -- tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs tests/McpServer.PluginIntegration.Tests/RealPluginProcessRunner.cs tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowResult.cs tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs 2>&1 | Out-String
$gitDiffAdapter = & git diff -- tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs 2>&1 | Out-String
$gitDiffRunner = & git diff -- tests/McpServer.PluginIntegration.Tests/RealPluginProcessRunner.cs 2>&1 | Out-String
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TodoYamlPorcelain = $gitTodo.Trim()
    PluginIntPorcelain = $gitPluginInt.Trim()
    DiffStat = $gitAdapter.Trim()
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-live-git.json') -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'hv-live-diff-adapter.patch') -Value $gitDiffAdapter -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'hv-live-diff-runner.patch') -Value $gitDiffRunner -Encoding utf8

$testsDir = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
$p15Hits = @(Select-String -Path (Join-Path $testsDir '*.cs') -Pattern 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending|AiTheory_' -ErrorAction SilentlyContinue)
$skipHits = @(Select-String -Path (Join-Path $testsDir '*.cs') -Pattern 'Skip\s*=|Assert\.Skip|Fact\(Skip|Theory\(Skip' -ErrorAction SilentlyContinue)
$overrideHits = @(Select-String -Path (Join-Path $testsDir 'PluginSessionLogWorkflowAdapter.cs') -Pattern 'PluginRootOverrideRejected|PLUGIN_ROOT_OVERRIDE|extraEnvironment' -ErrorAction SilentlyContinue)
$runnerHits = @(Select-String -Path (Join-Path $testsDir 'RealPluginProcessRunner.cs') -Pattern 'Environment\.Remove' -ErrorAction SilentlyContinue)
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    P15P16HitCount = $p15Hits.Count
    P15P16Hits = @($p15Hits | ForEach-Object { $_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
    SkipHitCount = $skipHits.Count
    SkipHits = @($skipHits | ForEach-Object { $_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
    AdapterOverrideHits = @($overrideHits | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
    RunnerRemoveHits = @($runnerHits | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'hv-live-grep.json') -Encoding utf8

$adapter = Get-Content -LiteralPath (Join-Path $testsDir 'PluginSessionLogWorkflowAdapter.cs') -Raw
$tests = Get-Content -LiteralPath (Join-Path $testsDir 'PluginSessionLogWorkflowAdapterTests.cs') -Raw
$resultType = Get-Content -LiteralPath (Join-Path $testsDir 'PluginSessionLogWorkflowResult.cs') -Raw
$fixture = Get-Content -LiteralPath (Join-Path $testsDir 'PluginIntegrationServerFixture.cs') -Raw
$runner = Get-Content -LiteralPath (Join-Path $testsDir 'RealPluginProcessRunner.cs') -Raw
$p14Inline = [regex]::Matches($tests, '(?s)Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty.*?public async Task Theory_Agent_PluginRootOverride')
$p14Block = if ($tests -match '(?s)(public async Task Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty.*?\n    \})') { $Matches[1] } else { '' }
$p14InlineCount = ([regex]::Matches(($tests.Substring(0, [Math]::Max(0, $tests.IndexOf('Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty')))), '\[InlineData\(PluginHostKind\.')).Count
# Count InlineData immediately above the P14 method
$p14Header = [regex]::Match($tests, '(?s)(\[Theory[^\]]*\](?:\s*\[InlineData\(PluginHostKind\.[^\]]+\)\]){1,16}\s*public async Task Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty)')
$p14InlineExact = if ($p14Header.Success) { ([regex]::Matches($p14Header.Value, '\[InlineData\(PluginHostKind\.')).Count } else { -1 }
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    AdapterHasPluginRootOverrideRejectedAssign = ($adapter -match 'PluginRootOverrideRejected\s*=\s*pluginRootOverrideRejected')
    AdapterReadsPluginRootOverrideEnv = ($adapter -match 'GetEnvironmentVariable\("PLUGIN_ROOT_OVERRIDE"\)')
    AdapterSetsExtraEnvEmptyOverride = ($adapter -match '\["PLUGIN_ROOT_OVERRIDE"\]\s*=\s*string\.Empty')
    AdapterCachePathMcpServerCacheFolder = ($adapter -match 'Path\.Combine\(fixture\.WorkspacePath,\s*"\.mcpServer",\s*scenario\.CacheFolder\)')
    AdapterHasCreateTrustedClient = ($adapter -match 'CreateTrustedClient')
    AdapterDiscardsLaunchResult = ($adapter -match '_\s*=\s*await processAdapter\.LaunchAsync')
    AdapterFabricatesSessionId = ($adapter -match 'sessionId = scenario\.AgentSourceType \+ "-" \+ utc')
    ResultPropertyExists = ($resultType -match 'PluginRootOverrideRejected')
    ResultCommentStillSaysRed = ($resultType -match 'C-red-P14 stays false')
    TestsNamedMethod = ($tests -match 'Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty')
    TestsAssertRejected = ($tests -match 'must reject PLUGIN_ROOT_OVERRIDE')
    TestsAssertPoisonEmpty = ($tests -match 'Assert\.Empty\(Directory\.GetFileSystemEntries\(poison\)\)')
    TestsAssertCacheContainsMcpServer = ($tests -match 'Assert\.Contains\(Path\.Combine\("\.mcpServer", scenario\.CacheFolder\)')
    TestsAssertCacheContainsWorkspace = ($tests -match 'Assert\.Contains\(fixture\.WorkspacePath, result\.CachePath')
    TestsAssertSiblingCache = ($tests -match 'sibling')
    TestsSkipAttribute = ($tests -match 'Skip\s*=')
    TestsP15Success = ($tests -match 'Success_NoPendingFailsafe')
    TestsP15FailedSubmit = ($tests -match 'FailedSubmit_RetainsRootIdPending')
    TestsP15Retry = ($tests -match 'RetrySuccess_DeletesOnlyMatchingPending')
    TestsP16AiTheory = ($tests -match 'AiTheory_')
    P14InlineDataCount = $p14InlineExact
    FixtureReserved7147 = ($fixture -match 'ReservedServicePort = 7147')
    FixtureAllocateExcludes7147 = ($fixture -match 'port != ReservedServicePort')
    RunnerRemovesEmptyEnv = ($runner -match 'startInfo\.Environment\.Remove\(pair\.Key\)')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-live-adapter-scan.json') -Encoding utf8

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$sigOk = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$bootOk = $false
try { $bootOk = Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer' } catch { $bootOk = $false; Save-Text (Join-Path $out 'hv-live-plugin-bootstrap.err.txt') $_.Exception.ToString() }
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TestMarkerSignature = [bool]$sigOk
    InvokeFullBootstrap = [bool]$bootOk
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-live-plugin-marker-sig.json') -Encoding utf8
Write-Output ("PLUGIN_SIG=$sigOk BOOTSTRAP=$bootOk")

$nonce = 'nonce-hv-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '-' + (Get-Random -Maximum 99999)
try {
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 15
    [ordered]@{ nonceSent = $nonce; nonceEcho = $health.nonce; status = $health.status; match = ($health.nonce -eq $nonce); storage = $health.storage } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'hv-live-health-nonce.json') -Encoding utf8
    Write-Output ("HEALTH_NONCE_MATCH=" + ($health.nonce -eq $nonce) + " NONCE=$nonce")
} catch {
    Save-Text (Join-Path $out 'hv-live-health-nonce.json') $_.Exception.ToString()
    Write-Output 'HEALTH_NONCE_FAIL'
}

$raw = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
$apiKey = if ($raw -match '(?m)^apiKey:\s*(.+)$') { $Matches[1].Trim() } else { '' }
$headers = @{ 'X-Api-Key' = $apiKey }
try {
    $search = Invoke-RestMethod -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -Method Get -TimeoutSec 30
    $search | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'hv-live-tool-search-grok.json') -Encoding utf8
    $exact = $false
    if ($search.PSObject.Properties.Name -contains 'tools' -and $null -ne $search.tools) {
        $exact = @($search.tools | Where-Object { $_.name -eq 'mcpserver-grok-plugin' }).Count -gt 0
    }
    if (-not $exact -and $search.PSObject.Properties.Name -contains 'items' -and $null -ne $search.items) {
        $exact = @($search.items | Where-Object { $_.name -eq 'mcpserver-grok-plugin' }).Count -gt 0
    }
    Write-Output ("TOOL_SEARCH_EXACT=$exact")
} catch {
    Save-Text (Join-Path $out 'hv-live-tool-search-grok.json') $_.Exception.ToString()
    Write-Output 'TOOL_SEARCH_FAIL'
}

$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()
$agent = 'GrokSubagentHostile'
$now = [DateTime]::UtcNow.ToString('o')

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'hv-live-todo-plan'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'hv-live-todo-plan-client'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'hv-live-todo-pluginint'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'hv-live-todo-pluginint-client'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'hv-live-req-fr'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'hv-live-req-tr'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'hv-live-req-test'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'hv-live-req-map'

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Hostile validator spawn continued after add-profile (18 non-skill profile markdown files). Independent live re-query of PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 plus FR/TR/TEST. Adapter source read: PluginRootOverrideRejected assigned from PLUGIN_ROOT_OVERRIDE, extraEnvironment sets PLUGIN_ROOT_OVERRIDE to empty, RealPluginProcessRunner removes empty env, cachePath is fixture.WorkspacePath/.mcpServer/{cacheFolder}. Persist still uses CreateTrustedClient. Result XML still says C-red-P14 stays false. Will wait for leftover PluginIntegration testhosts then independently re-run named P14 filter and full PluginIntegration.Tests.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: CLASS 1 C-green-P14 only. AGREE only if independent filter is Failed 0 Passed 8 Skipped 0, full suite Failed 0 Skipped 0, poison empty/cache under workspace .mcpServer, P15/P16 absent, PLAN and MCP-PLUGININT remain done false. CreateTrustedClient persist is residual unless P14 DoD requires production plugin persist; plan P14 is the named PoisonEmpty theory plus TEST AC4. Alternatives rejected: requiring P15-P20 or plan closeout.' }
    )
} -Name 'hv-live-sl-dialog'

Write-Output 'HV_LIVE_DONE'
