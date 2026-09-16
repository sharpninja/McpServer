#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-rerun-20260822T084835Z'
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$paths = @(
    'build/Build.PluginSessionLogIntegration.cs'
    'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs'
    'tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs'
    'tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs'
    'tests/McpServer.PluginIntegration.Tests/AiStrategyEvaluation.cs'
    'tests/McpServer.PluginIntegration.Tests/appsettings.aiunit.json'
    'tests/McpServer.PluginIntegration.Tests/McpServer.PluginIntegration.Tests.csproj'
    'docs/receipts/hostile-validator-20260822T073737Z.md'
    'docs/receipts/hostile-validator-20260822T073737Z.json'
    'docs/receipts/hostile-validator-20260822T081750Z.md'
    'docs/plans/PLAN-PLUGINHANDOFF-001.md'
)

$info = foreach ($rel in $paths) {
    $full = Join-Path 'F:\GitHub\McpServer' $rel
    $item = Get-Item -LiteralPath $full -ErrorAction SilentlyContinue
    if ($null -eq $item) {
        [ordered]@{ path = $rel; exists = $false }
    } else {
        [ordered]@{
            path = $rel
            exists = $true
            length = $item.Length
            lastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
            creationTimeUtc = $item.CreationTimeUtc.ToString('o')
        }
    }
}
$info | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'file-timestamps.json') -Encoding utf8

function Get-GrepHits {
    param([string]$Path, [string]$Pattern)
    $hits = @()
    if (-not (Test-Path -LiteralPath $Path)) { return $hits }
    $i = 0
    foreach ($line in Get-Content -LiteralPath $Path) {
        $i++
        if ($line -match $Pattern) {
            $hits += [ordered]@{ line = $i; text = $line.Trim() }
        }
    }
    return $hits
}

$buildPath = 'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs'
$buildGrep = [ordered]@{
    path = 'build/Build.PluginSessionLogIntegration.cs'
    aiUnit = @(Get-GrepHits -Path $buildPath -Pattern 'aiUnit')
    Preflight = @(Get-GrepHits -Path $buildPath -Pattern 'Preflight')
    Trait = @(Get-GrepHits -Path $buildPath -Pattern 'Trait')
    PluginInt = @(Get-GrepHits -Path $buildPath -Pattern 'PluginInt')
    SetFilter = @(Get-GrepHits -Path $buildPath -Pattern 'SetFilter')
    FailIfPluginSessionLogSkipped = @(Get-GrepHits -Path $buildPath -Pattern 'FailIfPluginSessionLogSkipped')
    notExecuted = @(Get-GrepHits -Path $buildPath -Pattern 'notExecuted')
    executed = @(Get-GrepHits -Path $buildPath -Pattern 'executed')
    skipped = @(Get-GrepHits -Path $buildPath -Pattern 'skipped')
    SharpNinja = @(Get-GrepHits -Path $buildPath -Pattern 'SharpNinja')
    grokBuild = @(Get-GrepHits -Path $buildPath -Pattern 'grok-build')
    appsettingsAiunit = @(Get-GrepHits -Path $buildPath -Pattern 'appsettings\.aiunit')
    ActiveStrategy = @(Get-GrepHits -Path $buildPath -Pattern 'ActiveStrategy')
    Deterministic = @(Get-GrepHits -Path $buildPath -Pattern 'Deterministic')
}
$buildGrep | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'build-grep.json') -Encoding utf8

$testRoot = 'F:\GitHub\McpServer\tests'
$named = [ordered]@{
    AiTheory_Agent_RequiresValidJsonFields = 0
    AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure = 0
    NukeTarget_SkipIsFailure = 0
    PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero = 0
    PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero = 0
    PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit = 0
}
$skipHits = @()
$inlineData = @()
Get-ChildItem -LiteralPath $testRoot -Recurse -Filter '*.cs' | ForEach-Object {
    $i = 0
    foreach ($line in Get-Content -LiteralPath $_.FullName) {
        $i++
        foreach ($key in @($named.Keys)) {
            if ($line -match [regex]::Escape($key)) { $named[$key]++ }
        }
        if ($line -match 'Fact\s*\(\s*Skip\s*=' -or $line -match 'Theory\s*\(\s*Skip\s*=' -or $line -match 'Assert\.Skip') {
            $skipHits += [ordered]@{ file = $_.FullName.Replace('F:\GitHub\McpServer\', ''); line = $i; text = $line.Trim() }
        }
        if ($_.Name -eq 'PluginSessionLogAiTheoryTests.cs' -and $line -match 'InlineData') {
            $inlineData += [ordered]@{ line = $i; text = $line.Trim() }
        }
    }
}
[ordered]@{
    namedCounts = $named
    skipHitsInTestsCs = $skipHits
    aiTheoryInlineData = $inlineData
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'p16-p19-skip-grep.json') -Encoding utf8

$procs = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match 'dotnet|testhost|vstest' -or ($_.CommandLine -and $_.CommandLine -match 'PluginIntegration|testhost|vstest')
} | Select-Object ProcessId, ParentProcessId, Name, CreationDate, CommandLine
$procList = @()
foreach ($p in $procs) {
    $procList += [ordered]@{
        pid = $p.ProcessId
        ppid = $p.ParentProcessId
        name = $p.Name
        command = $p.CommandLine
    }
}
[ordered]@{
    count = $procList.Count
    items = $procList
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'inspect-procs.json') -Encoding utf8

$py = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^python' -or ($_.CommandLine -and $_.CommandLine -match '(?i)\b(python|python3|py\.exe)\b') } | Select-Object ProcessId, Name, CommandLine)
$pyList = @()
foreach ($p in $py) {
    $pyList += [ordered]@{ pid = $p.ProcessId; name = $p.Name; command = $p.CommandLine }
}
[ordered]@{
    PythonProcessCount = $pyList.Count
    items = $pyList
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'python-procs.json') -Encoding utf8

$gitStatus = & git status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml tests/McpServer.PluginIntegration.Tests tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs build/Build.PluginSessionLogIntegration.cs 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $out 'git-status-pluginint.txt') -Value $gitStatus -Encoding utf8

$csproj = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj'
$csprojHits = Get-GrepHits -Path $csproj -Pattern 'SharpNinja\.aiUnit|PackageReference'
[ordered]@{ csproj = 'tests/McpServer.PluginIntegration.Tests/McpServer.PluginIntegration.Tests.csproj'; hits = $csprojHits } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'csproj-aiunit.json') -Encoding utf8

Write-Output 'INSPECT_OK'
