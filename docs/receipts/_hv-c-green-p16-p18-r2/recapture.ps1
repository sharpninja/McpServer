#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2'
$paths = @(
    'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs',
    'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs',
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogAiTheoryTests.cs',
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\AiStrategyFixture.cs',
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\appsettings.aiunit.json',
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj'
)
$items = foreach ($p in $paths) {
    $i = Get-Item -LiteralPath $p
    $t = Get-Content -LiteralPath $p -Raw
    [ordered]@{
        Name = $i.Name
        LastWriteTimeUtc = $i.LastWriteTimeUtc.ToString('o')
        Length = $i.Length
        HasFailedCounter = [bool]($t -match 'failed > 0|failed:\s*1|Attribute\("failed"\)')
        HasTotalZeroFail = [bool]($t -match 'total == 0')
        HasPreflight = [bool]($t -match 'PreflightPluginSessionLogAiUnitStrategy')
        HasDeterministicFilter = [bool]($t -match 'PluginInt=Deterministic')
        HasAiFilter = [bool]($t -match 'PluginInt=AI')
        HasSharpNinjaAiUnit = [bool]($t -match 'SharpNinja\.aiUnit')
        HasActiveStrategy = [bool]($t -match 'ActiveStrategy')
        HasGrokBuild = [bool]($t -match 'grok-build')
        HasP19 = [bool]($t -match 'PluginNativeSuite_|PluginInt_P19_')
    }
}
$items | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'file-timestamps-reread.json') -Encoding utf8
$gitStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs build/Build.PluginSessionLogIntegration.cs docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Porcelain = @($gitStatus)
    TodoYamlPorcelain = [string](git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-final.json') -Encoding utf8
Write-Output 'RECAPTURE_DONE'
