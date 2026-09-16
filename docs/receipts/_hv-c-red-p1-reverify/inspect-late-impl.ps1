#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1-reverify'
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$paths = @(
    'build\Build.PluginSessionLogIntegration.cs',
    'tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs',
    'tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj',
    'tests\McpServer.PluginIntegration.Tests\PluginHostKind.cs',
    'tests\McpServer.PluginIntegration.Tests\PluginIntegrationProjectTests.cs',
    'tests\McpServer.PluginIntegration.Tests\PluginSessionLogCollection.cs',
    'tests\McpServer.PluginIntegration.Tests\PluginSessionLogScenario.cs',
    'tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json',
    'McpServer.sln'
)
$rows = foreach ($rel in $paths) {
    $full = Join-Path 'F:\GitHub\McpServer' $rel
    $item = Get-Item -LiteralPath $full -ErrorAction SilentlyContinue
    [pscustomobject]@{
        path = $rel
        exists = [bool]$item
        lastWriteUtc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
        length = if ($item) { $item.Length } else { $null }
    }
}
$rows | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'late-impl-timestamps.json') -Encoding utf8

$slnHits = Select-String -LiteralPath 'F:\GitHub\McpServer\McpServer.sln' -Pattern 'PluginIntegration' -SimpleMatch -ErrorAction SilentlyContinue
if ($slnHits) {
    ($slnHits | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line }) -join "`n" |
        Set-Content -LiteralPath (Join-Path $out 'sln-pluginintegration.txt') -Encoding utf8
} else {
    Set-Content -LiteralPath (Join-Path $out 'sln-pluginintegration.txt') -Value 'NO_MATCH' -Encoding utf8
}

git status --porcelain -- build/Build.PluginSessionLogIntegration.cs tests/McpServer.PluginIntegration.Tests tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs McpServer.sln |
    Set-Content -LiteralPath (Join-Path $out 'git-status-late.txt') -Encoding utf8

$catalog = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
if (Test-Path -LiteralPath $catalog) {
    Copy-Item -LiteralPath $catalog -Destination (Join-Path $out 'plugin-sessionlog-scenarios.json') -Force
    $raw = Get-Content -LiteralPath $catalog -Raw
    $enabled = [regex]::Matches($raw, '"enabled"\s*:\s*true', 'IgnoreCase').Count
    $names = @('Codex','Claude Code','Claude Cowork','Copilot','Grok','Cline v2','Cline','OpenCode')
    $contains = @{}
    foreach ($n in $names) { $contains[$n] = $raw.Contains($n) }
    [ordered]@{ enabledTrueCount = $enabled; contains = $contains; length = $raw.Length } |
        ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath (Join-Path $out 'catalog-summary.json') -Encoding utf8
}

Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests' -Recurse -File |
    Select-Object FullName, Length, LastWriteTimeUtc |
    ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $out 'pluginint-files.json') -Encoding utf8

Write-Output 'INSPECT_DONE'
$rows | ForEach-Object { $_.path + ' exists=' + $_.exists + ' utc=' + $_.lastWriteUtc }
