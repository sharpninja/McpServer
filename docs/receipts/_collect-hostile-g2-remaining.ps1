#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$trx = 'F:\GitHub\McpServer\docs\receipts\_hv-g2-testout\results-nobuild\SessionLogSchemaGuardTests-nobuild.trx'
$testJson = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b4f-1f63-7bd2-ae2a-142fdf4e51df\mcp\call-ec6e628b-6832-48c0-98c1-1e6ffd36be35-91.json'
$effJson = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b4f-1f63-7bd2-ae2a-142fdf4e51df\mcp\call-be5cd97d-2c7c-44aa-aa02-eb51a6eccd00-83.json'

$out = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
}

# TRX outcomes
if (Test-Path -LiteralPath $trx) {
    [xml]$x = Get-Content -LiteralPath $trx
    $ns = @{ t = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010' }
    $counters = Select-Xml -Xml $x -XPath '//t:ResultSummary/t:Counters' -Namespace $ns | Select-Object -First 1
    if ($counters) {
        $n = $counters.Node
        $out.TrxTotal = $n.total
        $out.TrxPassed = $n.passed
        $out.TrxFailed = $n.failed
        $out.TrxSkipped = if ($n.outcome) { $n.GetAttribute('notExecuted') } else { $n.notExecuted }
        foreach ($attr in $n.Attributes) {
            $out["Trx_$($attr.Name)"] = $attr.Value
        }
    }
    $unit = Select-Xml -Xml $x -XPath '//t:UnitTestResult' -Namespace $ns
    $out.TrxResults = @(
        foreach ($u in $unit) {
            [ordered]@{
                Name = $u.Node.testName
                Outcome = $u.Node.outcome
            }
        }
    )
}

# File timestamps
$files = @(
    'src\McpServer.Storage\SessionLogSchemaGuard.cs',
    'tests\McpServer.Support.Mcp.Tests\Services\SessionLogSchemaGuardTests.cs',
    'src\McpServer.Services\Services\SessionLogService.cs',
    'src\McpServer.Storage.SqliteMigrations\Migrations\20260818205751_AddSessionLogTagsAndAgentSessionHeaders.cs',
    'src\McpServer.Storage.SqlServerMigrations\Migrations\20260818205807_AddSessionLogTagsAndAgentSessionHeaders.cs',
    'src\McpServer.Storage.PostgreSqlMigrations\Migrations\20260818205822_AddSessionLogTagsAndAgentSessionHeaders.cs',
    'src\McpServer.Storage\Migrations\20260722214500_AddAgentSessionHeaderFields.cs',
    'tests\McpServer.Support.Mcp.Tests\bin\Debug\net10.0\McpServer.Support.Mcp.Tests.dll',
    'src\McpServer.Storage\bin\Debug\net10.0\McpServer.Storage.dll'
)
$out.Files = @(
    foreach ($rel in $files) {
        $p = Join-Path $workspace $rel
        if (Test-Path -LiteralPath $p) {
            $i = Get-Item -LiteralPath $p
            [ordered]@{
                Path = $rel
                LastWriteTimeUtc = $i.LastWriteTimeUtc.ToString('o')
                Length = $i.Length
            }
        } else {
            [ordered]@{ Path = $rel; Missing = $true }
        }
    }
)

# Git tracked?
Push-Location $workspace
try {
    $out.GitLsGuard = @(git ls-files 'src/McpServer.Storage/SessionLogSchemaGuard.cs') -join ''
    $out.GitLogGuard = @(git log -1 --format='%H %ci %s' -- 'src/McpServer.Storage/SessionLogSchemaGuard.cs') -join ''
    $out.GitLogSqliteMig = @(git log -1 --format='%H %ci %s' -- 'src/McpServer.Storage.SqliteMigrations/Migrations/20260818205751_AddSessionLogTagsAndAgentSessionHeaders.cs') -join ''
    $out.GitLogSqlServerMig = @(git log -1 --format='%H %ci %s' -- 'src/McpServer.Storage.SqlServerMigrations/Migrations/20260818205807_AddSessionLogTagsAndAgentSessionHeaders.cs') -join ''
    $out.GitShowSqliteUpHasAgent = [bool]((git show HEAD:src/McpServer.Storage.SqliteMigrations/Migrations/20260818205751_AddSessionLogTagsAndAgentSessionHeaders.cs) -match 'AgentSessionId|AgentExecutablePath')
    $out.GitShowSqlServerUpHasAgent = [bool]((git show HEAD:src/McpServer.Storage.SqlServerMigrations/Migrations/20260818205807_AddSessionLogTagsAndAgentSessionHeaders.cs) -match 'AgentSessionId')
    $out.GitShowPgUpHasAgent = [bool]((git show HEAD:src/McpServer.Storage.PostgreSqlMigrations/Migrations/20260818205822_AddSessionLogTagsAndAgentSessionHeaders.cs) -match 'AgentSessionId')
    $out.GitShowHandwrittenAttrs = [bool]((git show HEAD:src/McpServer.Storage/Migrations/20260722214500_AddAgentSessionHeaderFields.cs) -match '\[Migration')
} finally {
    Pop-Location
}

# TEST-MCP-TRIAGESCHEMA-001 from requirements_list dump
if (Test-Path -LiteralPath $testJson) {
    $tr = Get-Content -LiteralPath $testJson -Raw
    $idx = $tr.IndexOf('TEST-MCP-TRIAGESCHEMA-001')
    $out.TestIdPresent = $idx -ge 0
    if ($idx -ge 0) {
        $start = [Math]::Max(0, $idx - 80)
        $out.TestSnippet = $tr.Substring($start, [Math]::Min(1800, $tr.Length - $start))
    }
}

# Mapping row
if (Test-Path -LiteralPath $effJson) {
    $er = Get-Content -LiteralPath $effJson -Raw
    $midx = $er.IndexOf('FR-MCP-TRIAGESCHEMA-001')
    # find mapping-like later occurrence if any
    $out.EffHasMappingPhrase = $er.Contains('TEST-MCP-TRIAGESCHEMA-001')
}

# Scratch sqlite
$scratch = Join-Path $workspace 'tests\McpServer.Support.Mcp.IntegrationTests\ScratchSqliteSchema.cs'
if (Test-Path -LiteralPath $scratch) {
    $out.ScratchHasAgentAlter = [bool]((Get-Content -LiteralPath $scratch -Raw) -match 'AgentSessionId')
}

# Update-McpService.ps1
$out.UpdateMcpServiceExists = [bool](Get-ChildItem -LiteralPath $workspace -Recurse -Filter 'Update-McpService.ps1' -ErrorAction SilentlyContinue | Select-Object -First 1)

# Live service config guess
$candidates = @(
    'C:\ProgramData\McpServer\appsettings.yaml',
    'C:\ProgramData\McpServer\current\appsettings.yaml',
    'C:\ProgramData\SharpNinja\McpServer\appsettings.yaml'
)
$out.ProgramDataConfigs = @(
    foreach ($c in $candidates) {
        [ordered]@{ Path = $c; Exists = Test-Path -LiteralPath $c }
    }
)

$out | ConvertTo-Json -Depth 8
