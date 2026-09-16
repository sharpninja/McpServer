#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1-reverify'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Set-Content -LiteralPath (Join-Path $out 'stamp.txt') -Value $utc -Encoding utf8

$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

$pluginInvoke = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name, [int]$TimeoutSeconds = 120)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $pluginInvoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds $TimeoutSeconds 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

# 1. Marker signature BEFORE other server MCP work
$sigText = 'UNSET'
try {
    . 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1'
    $sig = Test-MarkerSignature -MarkerFile $marker
    $sigText = [string]$sig
} catch {
    $sigText = 'ERROR:' + $_.Exception.Message
}
Set-Content -LiteralPath (Join-Path $out 'marker-signature.txt') -Value $sigText -Encoding utf8
Write-Output ('MARKER_SIG=' + $sigText)

# 2. Health nonce
$nonce = [guid]::NewGuid().ToString('N')
$healthUrl = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
$healthObj = [ordered]@{ nonceSent = $nonce; statusCode = $null; echoed = $false; body = $null; error = $null }
try {
    $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 30
    $healthObj.statusCode = [int]$resp.StatusCode
    $healthObj.body = $resp.Content
    $healthObj.echoed = $resp.Content.Contains($nonce)
} catch {
    $healthObj.error = $_.Exception.Message
}
$healthObj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'health.json') -Encoding utf8
Write-Output ('HEALTH_STATUS=' + $healthObj.statusCode)
Write-Output ('HEALTH_ECHO=' + $healthObj.echoed)

# 3. Plugin identity
$pluginJsonPath = 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json'
$pluginIdentity = [ordered]@{
    pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
    pluginJsonPath = $pluginJsonPath
    pluginJsonVersion = $null
    sourceType = 'GrokSubagentHostile'
    pluginName = 'mcpserver-grok-plugin'
}
if (Test-Path -LiteralPath $pluginJsonPath) {
    $pj = Get-Content -LiteralPath $pluginJsonPath -Raw | ConvertFrom-Json
    $pluginIdentity.pluginJsonVersion = [string]$pj.version
}
$pluginIdentity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-identity.json') -Encoding utf8

try {
    $status = & $pluginInvoke -Command Status -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 90 2>&1 | Out-String
    Set-Content -LiteralPath (Join-Path $out 'plugin-status.txt') -Value $status -Encoding utf8
    Write-Output 'PLUGIN_STATUS_OK'
} catch {
    Set-Content -LiteralPath (Join-Path $out 'plugin-status.txt') -Value $_.Exception.ToString() -Encoding utf8
    Write-Output 'PLUGIN_STATUS_FAIL'
}

# 4. File existence (both catalog paths)
$projDir = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
$catalogInProject = Join-Path $projDir 'scenarios\plugin-sessionlog-scenarios.json'
$catalogRepoRoot = 'F:\GitHub\McpServer\scenarios\plugin-sessionlog-scenarios.json'
$exists = [ordered]@{
    pluginIntegrationDirExists = Test-Path -LiteralPath $projDir
    pluginIntegrationCsprojExists = Test-Path -LiteralPath (Join-Path $projDir 'McpServer.PluginIntegration.Tests.csproj')
    catalogInProjectExists = Test-Path -LiteralPath $catalogInProject
    catalogRepoRootExists = Test-Path -LiteralPath $catalogRepoRoot
    testFileExists = Test-Path -LiteralPath 'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
    nukeTargetFileExists = Test-Path -LiteralPath 'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs'
}
Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\tests' -Directory -ErrorAction SilentlyContinue |
    Select-Object Name |
    ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $out 'tests-dirs.json') -Encoding utf8
$exists | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'existence.json') -Encoding utf8
Write-Output ($exists | ConvertTo-Json -Compress)

# 5. Grep product files for P2-P3 and later C reds
$productGlobs = @(
    'F:\GitHub\McpServer\build\*.cs',
    'F:\GitHub\McpServer\McpServer.sln',
    'F:\GitHub\McpServer\tests\Build.Tests\*.cs'
)
$patterns = @(
    'PluginSessionLogIntegration',
    'McpServer.PluginIntegration.Tests',
    'plugin-sessionlog-scenarios',
    'PluginIntegrationProject_HasXmlDocsAndNonparallelCollection',
    'PluginSessionLogScenario',
    'PluginHostKind',
    'Catalog_UniqueAgentSourceCache',
    'Catalog_HasExactlyEightEnabledScenarios',
    'BuildTests_PluginSessionLogIntegrationTarget_ExistsAndTreatsSkipAsFail',
    'Solution_ContainsMcpServerPluginIntegrationTestsProject'
)
$grepRows = @()
foreach ($g in $productGlobs) {
    foreach ($p in $patterns) {
        $hits = Select-String -Path $g -Pattern $p -SimpleMatch -ErrorAction SilentlyContinue
        foreach ($h in @($hits)) {
            $grepRows += [pscustomobject]@{
                pattern = $p
                path = $h.Path
                line = $h.LineNumber
                text = $h.Line.Trim()
            }
        }
    }
}
$grepRows | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'product-grep.json') -Encoding utf8

# Skip attributes in the named test file
$testFile = 'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
$skipHits = Select-String -LiteralPath $testFile -Pattern 'Skip|SkippableFact|Trait' -ErrorAction SilentlyContinue
if ($skipHits) {
    ($skipHits | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line }) -join "`n" |
        Set-Content -LiteralPath (Join-Path $out 'skip-scan.txt') -Encoding utf8
} else {
    Set-Content -LiteralPath (Join-Path $out 'skip-scan.txt') -Value 'NO_MATCH' -Encoding utf8
}

# Relabel hunt: names in other test files
$relabel = Select-String -Path 'F:\GitHub\McpServer\tests\**\*.cs' -Pattern 'BuildTests_PluginSessionLogIntegrationTarget_ExistsAndTreatsSkipAsFail|Solution_ContainsMcpServerPluginIntegrationTestsProject|Catalog_HasExactlyEightEnabledScenarios' -ErrorAction SilentlyContinue
$relabel | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line } |
    Set-Content -LiteralPath (Join-Path $out 'name-locations.txt') -Encoding utf8

# 6. Git
Push-Location 'F:\GitHub\McpServer'
git log -n 15 --format='%H %cI %s' -- tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs |
    Set-Content -LiteralPath (Join-Path $out 'git-log-testfile.txt') -Encoding utf8
git log -n 5 --follow --format='%H %cI %s' -- tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs |
    Set-Content -LiteralPath (Join-Path $out 'git-follow-testfile.txt') -Encoding utf8
git log -n 20 --diff-filter=R --summary -- tests/Build.Tests/ |
    Set-Content -LiteralPath (Join-Path $out 'git-renames-buildtests.txt') -Encoding utf8
git log -S 'BuildTests_PluginSessionLogIntegrationTarget_ExistsAndTreatsSkipAsFail' --all --format='%H %cI %s' -- '*.cs' |
    Set-Content -LiteralPath (Join-Path $out 'git-pickaxe-t1.txt') -Encoding utf8
git log -S 'Solution_ContainsMcpServerPluginIntegrationTestsProject' --all --format='%H %cI %s' -- '*.cs' |
    Set-Content -LiteralPath (Join-Path $out 'git-pickaxe-t2.txt') -Encoding utf8
git log -S 'Catalog_HasExactlyEightEnabledScenarios' --all --format='%H %cI %s' -- '*.cs' |
    Set-Content -LiteralPath (Join-Path $out 'git-pickaxe-t3.txt') -Encoding utf8
git status --porcelain -- tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs tests/McpServer.PluginIntegration.Tests build McpServer.sln |
    Set-Content -LiteralPath (Join-Path $out 'git-status-scope.txt') -Encoding utf8
git diff --stat -- tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs |
    Set-Content -LiteralPath (Join-Path $out 'git-diffstat-testfile.txt') -Encoding utf8
$item = Get-Item -LiteralPath $testFile -ErrorAction SilentlyContinue
$ts = [ordered]@{
    testFileLastWriteUtc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { 'missing' }
    stampUtc = $utc
}
$ts | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'timestamps.json') -Encoding utf8
Pop-Location

# 7. Session bootstrap after trust
$agent = 'GrokSubagentHostile'
$sessionId = "GrokSubagentHostile-$utc-c-red-p1"
$requestId = "req-$utc-001-hostile-validate-c-red-p1"
Set-Content -LiteralPath (Join-Path $out 'session-id.txt') -Value $sessionId -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'request-id.txt') -Value $requestId -Encoding utf8
$now = [DateTime]::UtcNow.ToString('o')

Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Name 'sl-bootstrap'

Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile C-red-P1 gate PLAN-PLUGINHANDOFF-001 Phase C'
    model = 'grok-build-subagent'
} -Name 'sl-open'

Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile C-red-P1 plugin sessionlog red tests'
    queryText = 'Independent hostile validation of PLAN-PLUGINHANDOFF-001 Phase C-red-P1 claims: named P1 red tests exist and currently fail, P2-P3 not started, greens not relabeled, PLAN/PLUGININT still open, PLUGINCORE close allowed only via B5 AGREE receipt.'
    model = 'grok-build-subagent'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
} -Name 'sl-begin'

# 8. Live TODOs and requirements
foreach ($id in @('MCP-PLUGINCORE-004','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','MCP-WORKSPACEHYGIENE-002')) {
    $name = 'todo-' + ($id.ToLowerInvariant())
    Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = $id } -Name $name
}

Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test-pluginint-001'
Invoke-Plugin -Method 'client.Requirements.ListMappingsAsync' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map-pluginint-001'

Write-Output ('SESSION_ID=' + $sessionId)
Write-Output ('REQUEST_ID=' + $requestId)
Write-Output 'COLLECT_PRETEST_DONE'
