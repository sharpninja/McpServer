#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4-r3'
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

$catalogJsonFile = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
$testsText = Get-Content -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs' -Raw
$catalogText = Get-Content -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs' -Raw
$jsonObj = Get-Content -LiteralPath $catalogJsonFile -Raw | ConvertFrom-Json
$jsonRows = @($jsonObj.scenarios)
$parent = 'F:\GitHub'
$enumNames = @('Codex','ClaudeCode','ClaudeCowork','Copilot','Grok','Cline','ClineV2','OpenCode')
$loaderThrowsNotImplemented = $catalogText -match 'LoadAndValidate is not implemented'
$loaderHasDeserialize = $catalogText.Contains('JsonSerializer.Deserialize')

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
    TestsAssertSpecificEnvNames = $testsText.Contains('CODEX_PLUGIN_ROOT')
    TestsAssertSpecificCacheNames = $testsText.Contains('"codex"')
    TestsLoadJsonFileDirectly = $testsText.Contains('plugin-sessionlog-scenarios.json')
    LoaderIsThrowOnly = [bool]$loaderThrowsNotImplemented -and -not $loaderHasDeserialize
    MissingEntrypointNames = @($enabledProbe | Where-Object { -not $_.catalogEntrypointExists } | ForEach-Object { $_.name })
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'deserialize-simulate.json') -Encoding utf8

$codexInvokes = @(Get-ChildItem -LiteralPath 'F:\GitHub\mcpserver-codex-plugin' -Recurse -Filter '*Invoke-CodexMcpPlugin.ps1' -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
Save-Text (Join-Path $out 'codex-invoke-files.txt') ($codexInvokes -join "`n")

try {
    git status --short -- tests/McpServer.PluginIntegration.Tests docs/plans/PLAN-PLUGINHANDOFF-001.md | Out-File -FilePath (Join-Path $out 'git-status-scope.txt') -Encoding utf8
    git log -n 8 --format='%H %cI %s' -- tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs | Out-File -FilePath (Join-Path $out 'git-log-scope.txt') -Encoding utf8
} catch {
    Save-Text (Join-Path $out 'git-status-scope.txt') $_.Exception.ToString()
}

Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip\s*=' |
    ForEach-Object { $_.Line } | Out-File -FilePath (Join-Path $out 'skip-scan.txt') -Encoding utf8

# Tool search exact name from already-saved payload
$search = Get-Content -LiteralPath (Join-Path $out 'tool-search-grok.json') -Raw | ConvertFrom-Json
$exact = @($search.tools | Where-Object { $_.name -eq 'mcpserver-grok-plugin' }).Count -gt 0
[ordered]@{ ExactNamePresent = $exact; ToolCount = @($search.tools).Count } | ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $out 'tool-search-exact.json') -Encoding utf8
Write-Output ("TOOL_SEARCH_EXACT=$exact")

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-pluginhandoff-001'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-mcp-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test-pluginint-001'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map-pluginint-001'

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
        $msg = ''
        if ($_.Output -and $_.Output.ErrorInfo -and $_.Output.ErrorInfo.Message) {
            $msg = [string]$_.Output.ErrorInfo.Message
        }
        [ordered]@{
            name = $_.testName
            outcome = $_.outcome
            duration = $_.duration
            output = $msg
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
Write-Output 'CONTINUE_DONE'
