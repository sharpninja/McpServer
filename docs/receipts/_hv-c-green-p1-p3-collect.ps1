#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p1-p3'
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

$agreePath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T232545Z.md'
$agreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T232545Z.json'
$agreeCutoff = [DateTime]::Parse('2026-08-21T23:25:45Z').ToUniversalTime()

$paths = @(
    $agreePath
    $agreeJson
    'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
    'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationProjectTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCollection.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostKind.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogScenario.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
    'F:\GitHub\McpServer\scenarios\plugin-sessionlog-scenarios.json'
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
            AfterCRedP1Agree = ($lw -gt $agreeCutoff)
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
            AfterCRedP1Agree = $null
        }
    }
}
$meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'timestamps.json') -Encoding utf8

# C-red-P1 AGREE text
if (Test-Path -LiteralPath $agreePath) {
    $agreeText = Get-Content -LiteralPath $agreePath -Raw
    $agreeHasAgree = $agreeText -match '(?m)^OverallVerdict:\s*AGREE\s*$'
    $agreeObj = [ordered]@{
        Exists = $true
        Length = (Get-Item -LiteralPath $agreePath).Length
        LastWriteTimeUtc = (Get-Item -LiteralPath $agreePath).LastWriteTimeUtc.ToString('o')
        OverallVerdictLineMatch = [bool]$agreeHasAgree
        JsonExists = (Test-Path -LiteralPath $agreeJson)
    }
    if (Test-Path -LiteralPath $agreeJson) {
        $j = Get-Content -LiteralPath $agreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
    }
    $agreeObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'cred-p1-agree.json') -Encoding utf8
} else {
    '{"Exists":false}' | Set-Content -LiteralPath (Join-Path $out 'cred-p1-agree.json') -Encoding utf8
}

# Grep sln / build / catalog
$slnHits = Select-String -Path 'F:\GitHub\McpServer\McpServer.sln' -Pattern 'PluginIntegration|PluginSessionLogIntegration' -ErrorAction SilentlyContinue
Save-Text (Join-Path $out 'grep-sln.txt') ($(if ($slnHits) { $slnHits | ForEach-Object { $_.ToString() } } else { 'NO_MATCH' }) | Out-String)

$buildHits = Select-String -Path 'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs' -Pattern 'PluginSessionLogIntegration|FailIfPluginSessionLogSkipped|SkipIsFailure' -ErrorAction SilentlyContinue
Save-Text (Join-Path $out 'grep-build-pluginsessionlog.txt') ($(if ($buildHits) { $buildHits | ForEach-Object { $_.ToString() } } else { 'NO_MATCH' }) | Out-String)

$csprojHits = Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj' -Pattern 'GenerateDocumentationFile|TargetFramework' -ErrorAction SilentlyContinue
Save-Text (Join-Path $out 'grep-csproj.txt') ($(if ($csprojHits) { $csprojHits | ForEach-Object { $_.ToString() } } else { 'NO_MATCH' }) | Out-String)

$collHits = Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCollection.cs' -Pattern 'DisableParallelization|CollectionDefinition' -ErrorAction SilentlyContinue
Save-Text (Join-Path $out 'grep-collection.txt') ($(if ($collHits) { $collHits | ForEach-Object { $_.ToString() } } else { 'NO_MATCH' }) | Out-String)

# Catalog parse
$catalogPath = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
$rootCatalogPath = 'F:\GitHub\McpServer\scenarios\plugin-sessionlog-scenarios.json'
$catalogObj = [ordered]@{
    HarnessCatalogExists = (Test-Path -LiteralPath $catalogPath)
    RootCatalogExists = (Test-Path -LiteralPath $rootCatalogPath)
}
if (Test-Path -LiteralPath $catalogPath) {
    $cat = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
    $rows = @($cat.scenarios)
    $catalogObj.RowCount = $rows.Count
    $catalogObj.Names = @($rows | ForEach-Object { $_.name })
    $catalogObj.EnabledCount = @($rows | Where-Object { $_.enabled -eq $true }).Count
    $catalogObj.HasClineV2 = @($rows | Where-Object { $_.name -eq 'Cline v2' }).Count -gt 0
    $catalogObj.HostKinds = @($rows | ForEach-Object { $_.hostKind })
    $expected = @('Codex','Claude Code','Claude Cowork','Copilot','Grok','Cline','Cline v2','OpenCode')
    $catalogObj.MissingExpectedNames = @($expected | Where-Object { $_ -notin $catalogObj.Names })
}
$catalogObj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'catalog-parse.json') -Encoding utf8

# Types exist
$typeScan = [ordered]@{
    PluginHostKindExists = (Test-Path -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostKind.cs')
    PluginSessionLogScenarioExists = (Test-Path -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogScenario.cs')
}
if ($typeScan.PluginHostKindExists) {
    $hk = Get-Content -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostKind.cs' -Raw
    $typeScan.PluginHostKindHasClineV2 = ($hk -match 'ClineV2')
    $typeScan.EnumMembers = [regex]::Matches($hk, '(?m)^\s+(Codex|ClaudeCode|ClaudeCowork|Copilot|Grok|Cline|ClineV2|OpenCode)\b') | ForEach-Object { $_.Groups[1].Value }
}
$typeScan | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'types-scan.json') -Encoding utf8

# Git
$gitPaths = @(
    'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs'
    'build/Build.PluginSessionLogIntegration.cs'
    'tests/McpServer.PluginIntegration.Tests'
    'tests/McpServer.PluginIntegration.Tests/scenarios/plugin-sessionlog-scenarios.json'
    'McpServer.sln'
)
$gitStatus = git status --porcelain -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-status-scope.txt') $gitStatus
$gitLog = git log --date=iso-strict --format='%H %cI %s' -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-log-scope.txt') $gitLog
$gitLogAll = git log --all --date=iso-strict --format='%H %cI %s' -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-log-all-scope.txt') $gitLogAll
$firstGreen = git log --diff-filter=A --follow --format='%H %cI %s' -- 'build/Build.PluginSessionLogIntegration.cs' 2>&1
Save-Text (Join-Path $out 'git-add-nuke-target.txt') $firstGreen
$gitDiffStat = git diff --stat -- @gitPaths 2>&1
Save-Text (Join-Path $out 'git-diffstat-scope.txt') $gitDiffStat

# Skip scan
$skipHits = Select-String -Path 'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs','F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip|Fact\(|Theory\(' -ErrorAction SilentlyContinue
Save-Text (Join-Path $out 'skip-scan.txt') ($skipHits | ForEach-Object { $_.ToString() } | Out-String)

# Live TODOs
foreach ($id in @('PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','MCP-PLUGINCORE-004','MCP-WORKSPACEHYGIENE-002')) {
    $name = 'todo-' + ($id.ToLowerInvariant())
    Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = $id } -Name $name
}

# Requirements
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map-pluginint-001'

# Plugin version
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
