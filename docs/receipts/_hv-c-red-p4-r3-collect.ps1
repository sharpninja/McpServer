#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4-r3'
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
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Set-Content -LiteralPath (Join-Path $out 'stamp.txt') -Value $utc -Encoding utf8
Write-Output ("UTC=$utc")

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

$raw = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
function Get-Scalar([string]$text, [string]$key) {
    if ($text -match "(?m)^${key}:\s*(.+)$") { return $Matches[1].Trim() }
    throw "missing $key"
}
function Get-Endpoint([string]$text, [string]$key) {
    if ($text -match "(?m)^\s+${key}:\s*(.+)$") { return $Matches[1].Trim() }
    throw "missing endpoint $key"
}
$apiKey = REDACTED $raw 'apiKey'
$port = Get-Scalar $raw 'port'
$baseUrl = Get-Scalar $raw 'baseUrl'
$workspace = Get-Scalar $raw 'workspace'
$workspacePath = Get-Scalar $raw 'workspacePath'
$markerPid = Get-Scalar $raw 'pid'
$startedAt = Get-Scalar $raw 'startedAt'
$markerWrittenAtUtc = Get-Scalar $raw 'markerWrittenAtUtc'
$serverStartedAtUtc = Get-Scalar $raw 'serverStartedAtUtc'
$sigValue = if ($raw -match '(?m)^\s+value:\s*([0-9A-Fa-f]+)') { $Matches[1] } else { throw 'missing signature value' }
$policy = if ($raw -match '(?m)^\s+policy:\s*(.+)$') { $Matches[1].Trim() } else { throw 'missing policy' }
$digest = if ($raw -match '(?m)^\s+contract_digest:\s*(.+)$') { $Matches[1].Trim() } else { throw 'missing digest' }
$payload = @"
canonicalization=marker-v1
port=$port
baseUrl=$baseUrl
apiKey=REDACTED
workspace=$workspace
workspacePath=$workspacePath
pid=$markerPid
startedAt=$startedAt
markerWrittenAtUtc=$markerWrittenAtUtc
serverStartedAtUtc=$serverStartedAtUtc
endpoints.health=$(Get-Endpoint $raw 'health')
endpoints.swagger=$(Get-Endpoint $raw 'swagger')
endpoints.swaggerUi=$(Get-Endpoint $raw 'swaggerUi')
endpoints.mcpTransport=$(Get-Endpoint $raw 'mcpTransport')
endpoints.sessionLog=$(Get-Endpoint $raw 'sessionLog')
endpoints.sessionLogDialog=$(Get-Endpoint $raw 'sessionLogDialog')
endpoints.contextSearch=$(Get-Endpoint $raw 'contextSearch')
endpoints.contextPack=$(Get-Endpoint $raw 'contextPack')
endpoints.contextSources=$(Get-Endpoint $raw 'contextSources')
endpoints.todo=$(Get-Endpoint $raw 'todo')
endpoints.repo=$(Get-Endpoint $raw 'repo')
endpoints.desktop=$(Get-Endpoint $raw 'desktop')
endpoints.gitHub=$(Get-Endpoint $raw 'gitHub')
endpoints.tools=$(Get-Endpoint $raw 'tools')
endpoints.workspace=$(Get-Endpoint $raw 'workspace')
endpoints.serverStartupUtc=$(Get-Endpoint $raw 'serverStartupUtc')
endpoints.markerFileTimestamp=$(Get-Endpoint $raw 'markerFileTimestamp')
agentPlugins.policy=$policy
agentPlugins.contractDigest=$digest
"@
$hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($apiKey))
$hash = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($payload))
$hex = ([BitConverter]::ToString($hash) -replace '-', '').ToUpperInvariant()
$sigMatch = [string]::Equals($hex, $sigValue.ToUpperInvariant())
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Computed = $hex
    MarkerValue = $sigValue.ToUpperInvariant()
    Match = $sigMatch
    PayloadLength = $payload.Length
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'marker-sig.json') -Encoding utf8
Write-Output ("HOMEMADE_SIG_MATCH=$sigMatch")

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$sigOk = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$bootOk = $false
try {
    $bootOk = Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer'
} catch {
    $bootOk = $false
    Save-Text (Join-Path $out 'plugin-bootstrap.err.txt') $_.Exception.ToString()
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TestMarkerSignature = [bool]$sigOk
    InvokeFullBootstrap = [bool]$bootOk
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-marker-sig.json') -Encoding utf8
Write-Output ("PLUGIN_SIG=$sigOk BOOTSTRAP=$bootOk")

$nonce = [guid]::NewGuid().ToString('N')
try {
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 15
    [ordered]@{ nonceSent = $nonce; nonceEcho = $health.nonce; status = $health.status; match = ($health.nonce -eq $nonce); storage = $health.storage } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'health-nonce.json') -Encoding utf8
    Write-Output ("HEALTH_NONCE_MATCH=" + ($health.nonce -eq $nonce) + " STATUS=" + $health.status)
} catch {
    Save-Text (Join-Path $out 'health-nonce.json') $_.Exception.ToString()
    Write-Output 'HEALTH_NONCE_FAIL'
}

$headers = @{ 'X-Api-Key' = $apiKey }
try {
    $search = Invoke-RestMethod -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -Method Get -TimeoutSec 30
    $search | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'tool-search-grok.json') -Encoding utf8
    $exact = $false
    if ($search.PSObject.Properties.Name -contains 'items' -and $search.items) {
        $exact = @($search.items | Where-Object { $_.name -eq 'mcpserver-grok-plugin' }).Count -gt 0
    }
    Write-Output ("TOOL_SEARCH_EXACT=$exact")
} catch {
    Save-Text (Join-Path $out 'tool-search-grok.json') $_.Exception.ToString()
    Write-Output 'TOOL_SEARCH_FAIL'
}

try {
    $status = & $plugin -Command Status -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 60 2> (Join-Path $out 'plugin-status.err.txt')
    Save-Text (Join-Path $out 'plugin-status.txt') $status
    Write-Output 'PLUGIN_STATUS_OK'
} catch {
    Save-Text (Join-Path $out 'plugin-status.txt') $_.Exception.ToString()
    Write-Output 'PLUGIN_STATUS_FAIL'
}

$pluginJson = Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' -Raw | ConvertFrom-Json
$pluginVersionFile = Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.version' -Raw
[ordered]@{
    PluginJsonVersion = [string]$pluginJson.version
    VersionFile = $pluginVersionFile.Trim()
    PluginName = [string]$pluginJson.name
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-version.json') -Encoding utf8

$agent = 'GrokSubagentHostile'
$sessionId = "GrokSubagentHostile-$utc-c-red-p4-r3"
$requestId = "req-$utc-001-hostile-c-red-p4-r3"
Set-Content -LiteralPath (Join-Path $out 'session-id.txt') -Value $sessionId -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'request-id.txt') -Value $requestId -Encoding utf8

Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Name 'sl-bootstrap'
Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile C-red-P4 r3 re-gate after 235922Z DISAGREE'
    model = 'grok-build-subagent'
} -Name 'sl-open'
Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile C-red-P4 r3 re-gate catalog AC1 and TDD hold'
    queryText = 'CLASS 1 C-red-P4 re-gate after DISAGREE docs/receipts/hostile-validator-20260821T235922Z.md. Attack parent claims that C3/D5 are closed, tests still fail via not-implemented throw, and LoadAndValidate is still throw-only. Surfaces A+B+C+D. Review only. Do not implement LoadAndValidate. PLAN stays not done.'
    model = 'grok-build-subagent'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
} -Name 'sl-begin'
$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed first. 18 non-skill profile markdown files read. Work class 1. Surfaces A+B+C+D apply. Disk read before tests showed PluginSessionLogCatalog.LoadAndValidate is a full JSON loader with validation, not a not-implemented throw. Independent re-run of CatalogTests, live todo_get, sibling entrypoint probe, and deserialize simulation starting now.' }
    )
} -Name 'sl-dialog-start'

# File timestamps and scans
$catalogCs = Get-Item -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
$catalogTests = Get-Item -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs'
$scenarioCs = Get-Item -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogScenario.cs'
$catalogJsonFile = Get-Item -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
$agreeP1P3 = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T234422Z.md'
$disagreeP4 = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T235922Z.md'
$agreeP4r2 = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T001811Z.md'

$catalogText = Get-Content -LiteralPath $catalogCs.FullName -Raw
$testsText = Get-Content -LiteralPath $catalogTests.FullName -Raw
$scenarioText = Get-Content -LiteralPath $scenarioCs.FullName -Raw
$jsonObj = Get-Content -LiteralPath $catalogJsonFile.FullName -Raw | ConvertFrom-Json

$methods = @(
    'Catalog_UniqueAgentSourceCache',
    'Catalog_RepositoryRootsExist',
    'Catalog_SupportedHostKinds',
    'Catalog_RequiredEntrypointFilesExist',
    'Catalog_VersionMetadataPresent',
    'Catalog_ExactlyEightEnabled'
)
$methodScan = foreach ($name in $methods) {
    $present = $testsText.Contains($name)
    $idx = $testsText.IndexOf("public void $name")
    $nearby = if ($idx -ge 0) { $testsText.Substring([Math]::Max(0, $idx - 200), [Math]::Min(400, $testsText.Length - [Math]::Max(0, $idx - 200))) } else { '' }
    [ordered]@{
        Name = $name
        Present = $present
        FactNearby = $nearby.Contains('[Fact]')
        SkipNearby = $nearby -match 'Skip\s*='
    }
}

$loaderThrowsNotImplemented = $catalogText -match 'LoadAndValidate is not implemented'
$loaderHasDeserialize = $catalogText.Contains('JsonSerializer.Deserialize')
$loaderHasReadAllText = $catalogText.Contains('File.ReadAllText')
$loaderHasEntrypointExists = $catalogText.Contains('Plugin entrypoint is missing')
$loaderHasUniqueKeys = $catalogText.Contains('AgentSourceType + ":" + row.CacheFolder')
$loaderLineCount = @(Get-Content -LiteralPath $catalogCs.FullName).Count

$jsonRows = @($jsonObj.scenarios)
$propertyNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($row in $jsonRows) {
    foreach ($p in $row.PSObject.Properties.Name) { [void]$propertyNames.Add($p) }
}

[ordered]@{
    Methods = @($methodScan)
    CatalogScan = [ordered]@{
        LoadAndValidateThrowsNotImplemented = [bool]$loaderThrowsNotImplemented
        LoadAndValidateHasDeserialize = [bool]$loaderHasDeserialize
        LoadAndValidateHasReadAllText = [bool]$loaderHasReadAllText
        LoadAndValidateHasEntrypointExistsCheck = [bool]$loaderHasEntrypointExists
        LoadAndValidateHasUniqueKeys = [bool]$loaderHasUniqueKeys
        LoadAndValidateLineCount = $loaderLineCount
        ScenarioHasCacheFolder = $scenarioText.Contains('CacheFolder')
        ScenarioHasEntrypoint = $scenarioText.Contains('Entrypoint')
        ScenarioHasEnvVars = $scenarioText.Contains('RequiredEnvironmentVariables')
        TestsUseCacheInUniquenessKey = $testsText.Contains('AgentSourceType + ":" + row.CacheFolder')
        TestsUseRepoInUniquenessKey = $testsText.Contains('AgentSourceType + ":" + row.RepositoryName')
        TestsAssertClineV2 = $testsText.Contains('"Cline v2"')
        TestsAssertEntrypointField = $testsText.Contains('row.Entrypoint')
        TestsOrThreeEntrypointPaths = ($testsText.Contains('hooks/session-start') -and $testsText.Contains('||'))
        TestsAssertEnvVars = $testsText.Contains('RequiredEnvironmentVariables')
        TestsAssertEnvNotEmpty = $testsText.Contains('Assert.NotEmpty(row.RequiredEnvironmentVariables)')
        SkipLiteralInTests = $testsText.Contains('[Fact(Skip')
    }
    JsonScan = [ordered]@{
        Exists = $true
        RowCount = $jsonRows.Count
        Names = @($jsonRows | ForEach-Object { $_.name })
        EnabledCount = @($jsonRows | Where-Object { $_.enabled }).Count
        PropertyNames = @($propertyNames)
        HasCacheFolderProperty = $propertyNames.Contains('cacheFolder')
        HasEntrypointProperty = $propertyNames.Contains('entrypoint')
        HasEnvProperty = $propertyNames.Contains('requiredEnvironmentVariables')
        AgentSourceTypes = @($jsonRows | ForEach-Object { $_.agentSourceType })
        CacheFolders = @($jsonRows | ForEach-Object { $_.cacheFolder })
        Entrypoints = @($jsonRows | ForEach-Object { $_.entrypoint })
        DuplicateAgentSourceTypes = @($jsonRows | Group-Object agentSourceType | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
        DuplicateCacheFolders = @($jsonRows | Group-Object cacheFolder | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
        HasClineV2 = @($jsonRows | Where-Object { $_.name -eq 'Cline v2' }).Count -gt 0
        ClineV2CacheFolder = [string](@($jsonRows | Where-Object { $_.name -eq 'Cline v2' } | Select-Object -First 1).cacheFolder)
        ClineCacheFolder = [string](@($jsonRows | Where-Object { $_.name -eq 'Cline' } | Select-Object -First 1).cacheFolder)
        MissingExpectedNames = @(
            'Codex','Claude Code','Claude Cowork','Copilot','Grok','Cline','Cline v2','OpenCode' |
            Where-Object { $_ -notin @($jsonRows | ForEach-Object { $_.name }) }
        )
    }
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'catalog-scan.json') -Encoding utf8

[ordered]@{
    CatalogCsLastWriteTimeUtc = $catalogCs.LastWriteTimeUtc.ToString('o')
    CatalogCsLength = $catalogCs.Length
    CatalogTestsLastWriteTimeUtc = $catalogTests.LastWriteTimeUtc.ToString('o')
    ScenarioCsLastWriteTimeUtc = $scenarioCs.LastWriteTimeUtc.ToString('o')
    CatalogJsonLastWriteTimeUtc = $catalogJsonFile.LastWriteTimeUtc.ToString('o')
    CGreenP1P3AgreeLastWriteTimeUtc = $agreeP1P3.LastWriteTimeUtc.ToString('o')
    PriorDisagreeLastWriteTimeUtc = $disagreeP4.LastWriteTimeUtc.ToString('o')
    PriorR2AgreeLastWriteTimeUtc = $agreeP4r2.LastWriteTimeUtc.ToString('o')
    CatalogCsAfterR2Agree = $catalogCs.LastWriteTimeUtc -gt $agreeP4r2.LastWriteTimeUtc
    CatalogCsAfterPriorDisagree = $catalogCs.LastWriteTimeUtc -gt ([datetime]'2026-08-21T23:59:22Z')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'timestamps.json') -Encoding utf8

# Sibling probe and deserialize simulation
$parent = 'F:\GitHub'
$hostKinds = [enum]::GetNames([int]) # placeholder replaced below
$enumNames = @('Codex','ClaudeCode','ClaudeCowork','Copilot','Grok','Cline','ClineV2','OpenCode')
$probe = foreach ($row in $jsonRows) {
    $pluginRoot = Join-Path $parent $row.repositoryName
    $entrypointRel = [string]$row.entrypoint
    $entrypointPath = Join-Path $pluginRoot ($entrypointRel.Replace('/', [IO.Path]::DirectorySeparatorChar))
    $envVars = @($row.requiredEnvironmentVariables)
    [ordered]@{
        name = [string]$row.name
        hostKind = [string]$row.hostKind
        hostKindDefined = $enumNames -contains [string]$row.hostKind
        agentSourceType = [string]$row.agentSourceType
        repositoryName = [string]$row.repositoryName
        cacheFolder = [string]$row.cacheFolder
        entrypoint = $entrypointRel
        enabled = [bool]$row.enabled
        dirExists = [IO.Directory]::Exists($pluginRoot)
        catalogEntrypointExists = [IO.File]::Exists($entrypointPath)
        entrypointHasDotDot = $entrypointRel.Contains('..')
        envCount = $envVars.Count
        envVars = $envVars
        envAllNonEmpty = ($envVars.Count -gt 0) -and (@($envVars | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -eq 0)
        versionExists = [IO.File]::Exists((Join-Path $pluginRoot '.version'))
        packageJsonExists = [IO.File]::Exists((Join-Path $pluginRoot 'package.json'))
        uniqueKey = ([string]$row.agentSourceType + ':' + [string]$row.cacheFolder)
        sourceNonEmpty = -not [string]::IsNullOrWhiteSpace([string]$row.agentSourceType)
        cacheNonEmpty = -not [string]::IsNullOrWhiteSpace([string]$row.cacheFolder)
        entrypointNonEmpty = -not [string]::IsNullOrWhiteSpace($entrypointRel)
    }
}
$probe | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'sibling-plugin-probe.json') -Encoding utf8

$enabledProbe = @($probe | Where-Object { $_.enabled })
$keys = @($enabledProbe | ForEach-Object { $_.uniqueKey })
$uniqueKeyCount = @($keys | Select-Object -Unique).Count
$uniqueWouldPass = ($uniqueKeyCount -eq $enabledProbe.Count) -and (@($enabledProbe | Where-Object { -not $_.sourceNonEmpty -or -not $_.cacheNonEmpty }).Count -eq 0)
$rootsWouldPass = @($enabledProbe | Where-Object { -not $_.dirExists }).Count -eq 0
$hostWouldPass = @($enabledProbe | Where-Object { -not $_.hostKindDefined }).Count -eq 0
$entryWouldPass = @($enabledProbe | Where-Object {
        -not $_.entrypointNonEmpty -or $_.entrypointHasDotDot -or -not $_.catalogEntrypointExists -or -not $_.envAllNonEmpty -or $_.envCount -eq 0
    }).Count -eq 0
$versionWouldPass = @($enabledProbe | Where-Object { -not ($_.versionExists -or $_.packageJsonExists) }).Count -eq 0
$eightWouldPass = ($enabledProbe.Count -eq 8) -and (@($enabledProbe | Where-Object { $_.name -eq 'Cline v2' }).Count -ge 1)
$codexEntry = @($enabledProbe | Where-Object { $_.name -eq 'Codex' } | Select-Object -First 1)

[ordered]@{
    UniqueAgentSourceCacheWouldPass = [bool]$uniqueWouldPass
    RepositoryRootsExistWouldPass = [bool]$rootsWouldPass
    SupportedHostKindsWouldPass = [bool]$hostWouldPass
    RequiredEntrypointFilesExistWouldPass = [bool]$entryWouldPass
    VersionMetadataPresentWouldPass = [bool]$versionWouldPass
    ExactlyEightEnabledWouldPass = [bool]$eightWouldPass
    AllSixWouldPass = [bool]($uniqueWouldPass -and $rootsWouldPass -and $hostWouldPass -and $entryWouldPass -and $versionWouldPass -and $eightWouldPass)
    UniqueKeyCount = $uniqueKeyCount
    RowCount = $enabledProbe.Count
    UniqueCacheFolderCount = @($enabledProbe | ForEach-Object { $_.cacheFolder } | Select-Object -Unique).Count
    CodexCatalogEntrypointExists = [bool]$codexEntry.catalogEntrypointExists
    CodexEntrypoint = [string]$codexEntry.entrypoint
    TestsAssertUniqueCacheFolderAlone = $testsText.Contains('row.CacheFolder') -and -not $testsText.Contains('AgentSourceType + ":" + row.CacheFolder')
    TestsAssertAllEightNames = $false
    TestsAssertSpecificEnvNames = $testsText.Contains('CODEX_PLUGIN_ROOT')
    TestsAssertSpecificCacheNames = $testsText.Contains('"codex"')
    TestsLoadJsonFileDirectly = $testsText.Contains('plugin-sessionlog-scenarios.json')
    LoaderIsThrowOnly = [bool]$loaderThrowsNotImplemented -and -not $loaderHasDeserialize
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'deserialize-simulate.json') -Encoding utf8

$codexInvokes = @(Get-ChildItem -LiteralPath 'F:\GitHub\mcpserver-codex-plugin' -Recurse -Filter '*Invoke-CodexMcpPlugin.ps1' -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
Save-Text (Join-Path $out 'codex-invoke-files.txt') ($codexInvokes -join "`n")

# Git
try {
    git status --short -- tests/McpServer.PluginIntegration.Tests docs/plans/PLAN-PLUGINHANDOFF-001.md | Out-File -FilePath (Join-Path $out 'git-status-scope.txt') -Encoding utf8
    git log -n 8 --format='%H %cI %s' -- tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs | Out-File -FilePath (Join-Path $out 'git-log-scope.txt') -Encoding utf8
} catch {
    Save-Text (Join-Path $out 'git-status-scope.txt') $_.Exception.ToString()
}

Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip\s*=' -SimpleMatch:$false |
    ForEach-Object { $_.Line } | Out-File -FilePath (Join-Path $out 'skip-scan.txt') -Encoding utf8

# Live TODO / requirements
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-pluginhandoff-001'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-mcp-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map-pluginint-001'

# Tests
$filterTrx = Join-Path $out 'catalog-filter.trx'
$allTrx = Join-Path $out 'pluginintegration-all.trx'
$filterLog = Join-Path $out 'dotnet-catalog-filter.log'
$filterErr = Join-Path $out 'dotnet-catalog-filter.err.log'
$allLog = Join-Path $out 'dotnet-pluginintegration-all.log'
$allErr = Join-Path $out 'dotnet-pluginintegration-all.err.log'

$filterProc = Start-Process -FilePath 'dotnet' -ArgumentList @(
    'test','tests/McpServer.PluginIntegration.Tests','-c','Debug',
    '--filter','FullyQualifiedName~PluginSessionLogCatalogTests',
    '--logger',"trx;LogFileName=$filterTrx"
) -WorkingDirectory 'F:\GitHub\McpServer' -NoNewWindow -Wait -PassThru -RedirectStandardOutput $filterLog -RedirectStandardError $filterErr
$allProc = Start-Process -FilePath 'dotnet' -ArgumentList @(
    'test','tests/McpServer.PluginIntegration.Tests','-c','Debug',
    '--logger',"trx;LogFileName=$allTrx"
) -WorkingDirectory 'F:\GitHub\McpServer' -NoNewWindow -Wait -PassThru -RedirectStandardOutput $allLog -RedirectStandardError $allErr

function Get-TrxSummary([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) {
        return [ordered]@{ exists = $false; path = $path }
    }
    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $units = @($xml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
        [ordered]@{
            name = $_.testName
            outcome = $_.outcome
            duration = $_.duration
            output = [string]$_.Output.ErrorInfo.Message
        }
    })
    [ordered]@{
        exists = $true
        path = $path
        outcome = [string]$xml.SelectSingleNode('//t:ResultSummary', $ns).outcome
        total = [string]$counters.total
        executed = [string]$counters.executed
        passed = [string]$counters.passed
        failed = [string]$counters.failed
        skipped = [string]$counters.skipped
        notExecuted = [string]$counters.notExecuted
        unitOutcomes = $units
    }
}

$filterSummary = Get-TrxSummary $filterTrx
$allSummary = Get-TrxSummary $allTrx
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    CatalogFilterExitCode = $filterProc.ExitCode
    PluginAllExitCode = $allProc.ExitCode
    CatalogFilterTrx = $filterSummary
    PluginAllTrx = $allSummary
} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $out 'trx-summary.json') -Encoding utf8
$filterSummary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $out 'trx-parsed.json') -Encoding utf8

Write-Output ("FILTER_EXIT=" + $filterProc.ExitCode)
Write-Output ("ALL_EXIT=" + $allProc.ExitCode)
Write-Output ("SESSION_ID=$sessionId")
Write-Output ("REQUEST_ID=$requestId")
Write-Output 'COLLECT_DONE'
