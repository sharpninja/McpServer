#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p4'
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

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T001811Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T001811Z.json'
$priorAgreeCutoff = [DateTime]::Parse('2026-08-22T00:18:11Z').ToUniversalTime()

$paths = @(
    $priorAgree
    $priorAgreeJson
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogScenario.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostKind.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationProjectTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
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
            AfterCRedP4Agree = ($lw -gt $priorAgreeCutoff)
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
            AfterCRedP4Agree = $null
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
        MentionsCRedP4 = ($agreeText -match 'C-red-P4')
        MentionsLoadAndValidateThrows = ($agreeText -match 'not implemented')
    }
    if (Test-Path -LiteralPath $priorAgreeJson) {
        $j = Get-Content -LiteralPath $priorAgreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
    }
    $agreeObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-cred-p4-agree.json') -Encoding utf8
}

$catalogCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
$catalogTests = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs'
$scenarioCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogScenario.cs'
$catalogJsonPath = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'

$catalogSrc = Get-Content -LiteralPath $catalogCs -Raw
$testsSrc = Get-Content -LiteralPath $catalogTests -Raw
$scenarioSrc = Get-Content -LiteralPath $scenarioCs -Raw

$expectedNames = @(
    'Catalog_UniqueAgentSourceCache'
    'Catalog_RepositoryRootsExist'
    'Catalog_SupportedHostKinds'
    'Catalog_RequiredEntrypointFilesExist'
    'Catalog_VersionMetadataPresent'
    'Catalog_ExactlyEightEnabled'
)
$methodHits = foreach ($n in $expectedNames) {
    [pscustomobject]@{
        Name = $n
        Present = $testsSrc.Contains($n)
        FactNearby = [bool]($testsSrc -match ("\[Fact\][\s\S]{0,180}public void $n\("))
        SkipNearby = [bool]($testsSrc -match ("\[Fact\([^\]]*Skip[\s\S]{0,240}public void $n\("))
    }
}

$scan = [ordered]@{
    LoadAndValidateThrowsNotImplemented = ($catalogSrc -match 'LoadAndValidate is not implemented')
    ReadsCatalogJsonPath = ($catalogSrc -match 'plugin-sessionlog-scenarios\.json')
    MapsCacheFolder = ($catalogSrc -match 'CacheFolder')
    MapsEntrypoint = ($catalogSrc -match 'Entrypoint')
    MapsEnvVars = ($catalogSrc -match 'RequiredEnvironmentVariables')
    MapsAgentSourceType = ($catalogSrc -match 'AgentSourceType')
    MapsHostKind = ($catalogSrc -match 'HostKind')
    MapsRepositoryName = ($catalogSrc -match 'RepositoryName')
    ThrowsMissingRepo = ($catalogSrc -match 'Plugin repository root is missing')
    ThrowsMissingEntrypoint = ($catalogSrc -match 'Plugin entrypoint is missing')
    ThrowsMissingVersion = ($catalogSrc -match 'Plugin version metadata is missing')
    ThrowsMissingAc1 = ($catalogSrc -match 'missing TR-MCP-PLUGININT-001 AC1 fields')
    ThrowsNonUnique = ($catalogSrc -match 'agent-source plus cache-folder identities are not unique')
    ThrowsEnabledCount = ($catalogSrc -match 'exactly eight enabled scenarios')
    UniqueKeyIsAgentSourceCache = ($catalogSrc -match 'AgentSourceType \+ ":" \+ row\.CacheFolder')
    UniqueKeyIsRepositoryName = ($catalogSrc -match 'AgentSourceType \+ ":" \+ row\.RepositoryName')
    TestsUseCacheInUniquenessKey = ($testsSrc -match 'AgentSourceType \+ ":" \+ row\.CacheFolder')
    TestsUseRepoInUniquenessKey = ($testsSrc -match 'AgentSourceType \+ ":" \+ row\.RepositoryName')
    TestsAssertClineV2 = ($testsSrc -match 'Cline v2')
    TestsAssertEntrypointField = ($testsSrc -match 'row\.Entrypoint')
    TestsOrThreeEntrypointPaths = [bool]($testsSrc -match 'repl-invoke\.ps1' -and $testsSrc -match 'Invoke-McpPlugin\.ps1' -and $testsSrc -match 'dist\\index\.js|dist/index\.js')
    TestsAssertEnvVars = ($testsSrc -match 'RequiredEnvironmentVariables')
    TestsAssertEnvNotEmpty = ($testsSrc -match 'Assert\.NotEmpty\(row\.RequiredEnvironmentVariables\)')
    SkipLiteralInTests = ($testsSrc -match 'Skip\s*=')
    ScenarioHasCacheFolder = ($scenarioSrc -match 'CacheFolder')
    ScenarioHasEntrypoint = ($scenarioSrc -match 'Entrypoint')
    ScenarioHasEnvVars = ($scenarioSrc -match 'RequiredEnvironmentVariables')
}

$parent = 'F:\GitHub'
$jsonScan = [ordered]@{ Exists = (Test-Path -LiteralPath $catalogJsonPath) }
if ($jsonScan.Exists) {
    $cat = Get-Content -LiteralPath $catalogJsonPath -Raw | ConvertFrom-Json
    $rows = @($cat.scenarios)
    $jsonScan.RowCount = $rows.Count
    $jsonScan.Names = @($rows | ForEach-Object { [string]$_.name })
    $jsonScan.EnabledCount = @($rows | Where-Object { $_.enabled -eq $true }).Count
    $jsonScan.PropertyNames = @($rows[0].PSObject.Properties.Name)
    $jsonScan.HasCacheFolderProperty = $jsonScan.PropertyNames -contains 'cacheFolder'
    $jsonScan.HasEntrypointProperty = $jsonScan.PropertyNames -contains 'entrypoint'
    $jsonScan.HasEnvProperty = $jsonScan.PropertyNames -contains 'requiredEnvironmentVariables'
    $jsonScan.AgentSourceTypes = @($rows | ForEach-Object { [string]$_.agentSourceType })
    $jsonScan.CacheFolders = @($rows | ForEach-Object { [string]$_.cacheFolder })
    $jsonScan.Entrypoints = @($rows | ForEach-Object { [string]$_.entrypoint })
    $jsonScan.DuplicateAgentSourceTypes = @($jsonScan.AgentSourceTypes | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
    $jsonScan.DuplicateCacheFolders = @($jsonScan.CacheFolders | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
    $jsonScan.HasClineV2 = $jsonScan.Names -contains 'Cline v2'
    $jsonScan.ClineV2CacheFolder = [string](@($rows | Where-Object { $_.name -eq 'Cline v2' } | Select-Object -First 1).cacheFolder)
    $jsonScan.ClineCacheFolder = [string](@($rows | Where-Object { $_.name -eq 'Cline' } | Select-Object -First 1).cacheFolder)
    $jsonScan.CodexEntrypoint = [string](@($rows | Where-Object { $_.name -eq 'Codex' } | Select-Object -First 1).entrypoint)
    $jsonScan.ExpectedEightNames = @('Codex','Claude Code','Claude Cowork','Copilot','Grok','Cline','Cline v2','OpenCode')
    $jsonScan.MissingExpectedNames = @($jsonScan.ExpectedEightNames | Where-Object { $jsonScan.Names -notcontains $_ })

    $probe = foreach ($row in $rows) {
        $pluginRoot = Join-Path $parent ([string]$row.repositoryName)
        $ep = [string]$row.entrypoint
        $epPath = Join-Path $pluginRoot ($ep.Replace('/', [IO.Path]::DirectorySeparatorChar))
        $libEp = Join-Path $pluginRoot ('lib' + [IO.Path]::DirectorySeparatorChar + 'Invoke-CodexMcpPlugin.ps1')
        $rootEp = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
        $envVars = @()
        if ($null -ne $row.requiredEnvironmentVariables) { $envVars = @($row.requiredEnvironmentVariables | ForEach-Object { [string]$_ }) }
        [pscustomobject]@{
            name = [string]$row.name
            hostKind = [string]$row.hostKind
            agentSourceType = [string]$row.agentSourceType
            repositoryName = [string]$row.repositoryName
            cacheFolder = [string]$row.cacheFolder
            entrypoint = $ep
            enabled = [bool]$row.enabled
            dirExists = (Test-Path -LiteralPath $pluginRoot -PathType Container)
            catalogEntrypointExists = (Test-Path -LiteralPath $epPath -PathType Leaf)
            libCodexEntrypointExists = (Test-Path -LiteralPath $libEp -PathType Leaf)
            rootCodexEntrypointExists = (Test-Path -LiteralPath $rootEp -PathType Leaf)
            entrypointHasDotDot = $ep.Contains('..')
            envCount = $envVars.Count
            envVars = $envVars
            envAllNonEmpty = (($envVars.Count -gt 0) -and (@($envVars | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -eq 0))
            versionExists = (Test-Path -LiteralPath (Join-Path $pluginRoot '.version') -PathType Leaf)
            packageJsonExists = (Test-Path -LiteralPath (Join-Path $pluginRoot 'package.json') -PathType Leaf)
            uniqueKey = ([string]$row.agentSourceType) + ':' + ([string]$row.cacheFolder)
            repoUniqueKey = ([string]$row.agentSourceType) + ':' + ([string]$row.repositoryName)
        }
    }
    $probe | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'sibling-plugin-probe.json') -Encoding utf8

    $keys = @($probe | ForEach-Object { $_.uniqueKey })
    $repoKeys = @($probe | ForEach-Object { $_.repoUniqueKey })
    $uniqueKeyCount = @($keys | Select-Object -Unique).Count
    $repoKeyCount = @($repoKeys | Select-Object -Unique).Count
    $cacheUniqueCount = @($probe | ForEach-Object { $_.cacheFolder } | Select-Object -Unique).Count

    [ordered]@{
        UniqueKeyCount = $uniqueKeyCount
        RepoKeyedCount = $repoKeyCount
        UniqueCacheFolderCount = $cacheUniqueCount
        CodexCatalogEntrypoint = $jsonScan.CodexEntrypoint
        CodexRootEntrypointExists = [bool](@($probe | Where-Object { $_.name -eq 'Codex' } | Select-Object -First 1).rootCodexEntrypointExists)
        CodexLibEntrypointExists = [bool](@($probe | Where-Object { $_.name -eq 'Codex' } | Select-Object -First 1).libCodexEntrypointExists)
        ClineCacheFolder = $jsonScan.ClineCacheFolder
        ClineV2CacheFolder = $jsonScan.ClineV2CacheFolder
        ClineV2Distinct = ($jsonScan.ClineCacheFolder -ne $jsonScan.ClineV2CacheFolder)
        AllCatalogEntrypointsExist = (@($probe | Where-Object { -not $_.catalogEntrypointExists }).Count -eq 0)
        AllReposExist = (@($probe | Where-Object { -not $_.dirExists }).Count -eq 0)
        AllVersionPresent = (@($probe | Where-Object { -not ($_.versionExists -or $_.packageJsonExists) }).Count -eq 0)
        UniqueKeysEqualRowCount = ($uniqueKeyCount -eq $probe.Count)
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'catalog-probe-summary.json') -Encoding utf8
}

[ordered]@{
    Methods = $methodHits
    CatalogScan = $scan
    JsonScan = $jsonScan
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'catalog-scan.json') -Encoding utf8

$skipHits = Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip\s*=|\[Fact\(|\[Theory\(' -ErrorAction SilentlyContinue
Save-Text (Join-Path $out 'skip-scan.txt') ($skipHits | ForEach-Object { $_.ToString() } | Out-String)

$gitPaths = @(
    'tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs'
    'tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs'
    'tests/McpServer.PluginIntegration.Tests/PluginSessionLogScenario.cs'
    'tests/McpServer.PluginIntegration.Tests/scenarios/plugin-sessionlog-scenarios.json'
)
$gitStatus = git status --porcelain -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-status-scope.txt') $gitStatus
$gitLog = git log --date=iso-strict --format='%H %cI %s' -n 8 -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-log-scope.txt') $gitLog

$codexRoot = 'F:\GitHub\mcpserver-codex-plugin'
$codexFiles = Get-ChildItem -LiteralPath $codexRoot -Filter 'Invoke-CodexMcpPlugin.ps1' -Recurse -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty FullName
Save-Text (Join-Path $out 'codex-invoke-files.txt') ($codexFiles | Out-String)

foreach ($id in @('PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001')) {
    $name = 'todo-' + ($id.ToLowerInvariant())
    Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = $id } -Name $name
}

Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map-pluginint-001'

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
