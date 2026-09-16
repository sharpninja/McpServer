#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
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

$agreePath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T234422Z.md'
$agreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T234422Z.json'
$agreeCutoff = [DateTime]::Parse('2026-08-21T23:44:22Z').ToUniversalTime()

$paths = @(
    $agreePath
    $agreeJson
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogScenario.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostKind.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationProjectTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
    'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
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
            AfterCGreenP1P3Agree = ($lw -gt $agreeCutoff)
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
            AfterCGreenP1P3Agree = $null
        }
    }
}
$meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'timestamps.json') -Encoding utf8

if (Test-Path -LiteralPath $agreePath) {
    $agreeText = Get-Content -LiteralPath $agreePath -Raw
    $agreeHasAgree = $agreeText -match '(?m)^OverallVerdict:\s*AGREE\s*$'
    $agreeObj = [ordered]@{
        Exists = $true
        Length = (Get-Item -LiteralPath $agreePath).Length
        LastWriteTimeUtc = (Get-Item -LiteralPath $agreePath).LastWriteTimeUtc.ToString('o')
        OverallVerdictLineMatch = [bool]$agreeHasAgree
        JsonExists = (Test-Path -LiteralPath $agreeJson)
        MentionsCGreenP1P3 = ($agreeText -match 'C-green-P1-P3')
        MentionsCRedP4 = ($agreeText -match 'C-red-P4')
    }
    if (Test-Path -LiteralPath $agreeJson) {
        $j = Get-Content -LiteralPath $agreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
        $agreeObj.JsonWorkClass = [string]$j.WorkClass
    }
    $agreeObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'cgreen-p1-p3-agree.json') -Encoding utf8
} else {
    '{"Exists":false}' | Set-Content -LiteralPath (Join-Path $out 'cgreen-p1-p3-agree.json') -Encoding utf8
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
    LoadAndValidateThrows = ($catalogSrc -match 'throw new InvalidOperationException\("PluginSessionLogCatalog\.LoadAndValidate is not implemented\."\)')
    LoadAndValidateHasOtherStatements = $false
    ScenarioHasCacheFolder = ($scenarioSrc -match 'CacheFolder|cacheFolder|CachePath')
    ScenarioHasEntrypoint = ($scenarioSrc -match 'Entrypoint|entrypoint')
    ScenarioHasEnvVars = ($scenarioSrc -match 'EnvironmentVariable|environmentVariables|EnvVar')
    TestsUseCacheInUniquenessKey = ($testsSrc -match 'CacheFolder|cacheFolder|CachePath')
    TestsAssertClineV2 = ($testsSrc -match 'Cline v2')
    SkipLiteralInTests = ($testsSrc -match 'Skip\s*=')
    TraitSkip = ($testsSrc -match 'Skip')
}
$loaderBody = [regex]::Match($catalogSrc, 'LoadAndValidate\([\s\S]*?\{([\s\S]*?)\}\s*$', 'Singleline')
if ($loaderBody.Success) {
    $body = $loaderBody.Groups[1].Value
    $scan.LoaderBody = $body.Trim()
    $scan.LoadAndValidateHasOtherStatements = ($body -match 'File\.ReadAllText|Deserialize|JsonSerializer|return ')
}

$jsonScan = [ordered]@{ Exists = (Test-Path -LiteralPath $catalogJsonPath) }
if ($jsonScan.Exists) {
    $cat = Get-Content -LiteralPath $catalogJsonPath -Raw | ConvertFrom-Json
    $rows = @($cat.scenarios)
    $jsonScan.RowCount = $rows.Count
    $jsonScan.Names = @($rows | ForEach-Object { $_.name })
    $jsonScan.EnabledCount = @($rows | Where-Object { $_.enabled -eq $true }).Count
    $jsonScan.PropertyNames = @($rows[0].PSObject.Properties.Name)
    $jsonScan.HasCacheFolderProperty = ($jsonScan.PropertyNames -contains 'cacheFolderName') -or ($jsonScan.PropertyNames -contains 'cacheFolder')
    $jsonScan.HasEntrypointProperty = $jsonScan.PropertyNames -contains 'entrypoint'
    $jsonScan.HasEnvProperty = ($jsonScan.PropertyNames -contains 'environmentVariables') -or ($jsonScan.PropertyNames -contains 'env')
    $jsonScan.AgentSourceTypes = @($rows | ForEach-Object { $_.agentSourceType })
    $jsonScan.DuplicateAgentSourceTypes = @($jsonScan.AgentSourceTypes | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
}

[ordered]@{
    Methods = $methodHits
    CatalogScan = $scan
    JsonScan = $jsonScan
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'catalog-scan.json') -Encoding utf8

$skipHits = Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip|Fact\(|Theory\(' -ErrorAction SilentlyContinue
Save-Text (Join-Path $out 'skip-scan.txt') ($skipHits | ForEach-Object { $_.ToString() } | Out-String)

$gitPaths = @(
    'tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs'
    'tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs'
    'tests/McpServer.PluginIntegration.Tests/PluginSessionLogScenario.cs'
    'tests/McpServer.PluginIntegration.Tests/scenarios/plugin-sessionlog-scenarios.json'
)
$gitStatus = git status --porcelain -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-status-scope.txt') $gitStatus
$gitLog = git log --date=iso-strict --format='%H %cI %s' -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-log-scope.txt') $gitLog
$gitLogAll = git log --all --date=iso-strict --format='%H %cI %s' -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-log-all-scope.txt') $gitLogAll

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
