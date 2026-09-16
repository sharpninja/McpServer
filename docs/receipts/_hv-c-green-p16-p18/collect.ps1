#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18'
$testsDir = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
$buildTests = 'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
$buildTarget = 'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$files = @(
    (Join-Path $testsDir 'PluginSessionLogAiTheoryTests.cs'),
    (Join-Path $testsDir 'AiStrategyFixture.cs'),
    (Join-Path $testsDir 'AiStrategyEvaluation.cs'),
    (Join-Path $testsDir 'PluginSessionLogCollection.cs'),
    (Join-Path $testsDir 'PluginSessionLogCatalog.cs'),
    $buildTests,
    $buildTarget
)
$timestamps = foreach ($path in $files) {
    $item = Get-Item -LiteralPath $path
    $text = Get-Content -LiteralPath $path -Raw
    [ordered]@{
        Name = $item.Name
        Path = $path
        LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        Length = $item.Length
        HasSkipAttribute = [bool]($text -match '\[(Fact|Theory)\s*\(\s*Skip')
        HasAssertSkip = [bool]($text -match 'Assert\.Skip')
        HasInvalidOperationNotImplemented = [bool]($text -match 'not implemented')
        HasEvaluateAsync = [bool]($text -match 'EvaluateAsync')
        HasJsonParse = [bool]($text -match 'JsonDocument\.Parse')
        HasDeterministicFailure = [bool]($text -match 'deterministicFailure')
        HasInvalidJsonCatch = [bool]($text -match 'invalid-json')
        HasRequiredFields = [bool]($text -match 'agent.*cacheFolder.*entrypoint.*status' -or $text -match 'RequiredFields')
        HasP16TheoryName = [bool]($text -match 'AiTheory_Agent_RequiresValidJsonFields')
        HasP17RejectsName = [bool]($text -match 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure')
        HasP18NukeSkipName = [bool]($text -match 'NukeTarget_SkipIsFailure')
        HasFailIfSkippedCall = [bool]($text -match 'FailIfPluginSessionLogSkipped')
        HasAiUnitLiteral = [bool]($text -match 'aiUnit')
        HasTraitLiteral = [bool]($text -match 'Trait\(')
        HasP19NativeSuite = [bool]($text -match 'PluginNativeSuite_')
        InlineDataHosts = @([regex]::Matches($text, 'InlineData\(PluginHostKind\.(\w+)\)') | ForEach-Object { $_.Groups[1].Value })
    }
}
$timestamps | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'file-timestamps.json') -Encoding utf8

$skipHits = @()
$p16Hits = @()
$p17Hits = @()
$p18Hits = @()
$p19Hits = @()
$evaluateHits = @()
$csFiles = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.cs' -File)
$csFiles += Get-Item -LiteralPath $buildTests
foreach ($f in $csFiles) {
    $lines = Get-Content -LiteralPath $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '\[Skip|Fact\(Skip|Theory\(Skip|Assert\.Skip') {
            $skipHits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
        if ($line -match 'AiTheory_Agent_RequiresValidJsonFields') {
            $p16Hits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
        if ($line -match 'AiTheory_RejectsInvalidJson') {
            $p17Hits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
        if ($line -match 'NukeTarget_SkipIsFailure') {
            $p18Hits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
        if ($line -match 'PluginNativeSuite_|PluginInt_P19_') {
            $p19Hits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
        if ($line -match 'EvaluateAsync|not implemented|deterministicFailure|invalid-json') {
            $evaluateHits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
    }
}

$buildText = Get-Content -LiteralPath $buildTarget -Raw
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    SkipHitCount = $skipHits.Count
    SkipHits = $skipHits
    P16AiTheoryHitCount = $p16Hits.Count
    P16AiTheoryHits = $p16Hits
    P17RejectsHitCount = $p17Hits.Count
    P17RejectsHits = $p17Hits
    P18NukeSkipHitCount = $p18Hits.Count
    P18NukeSkipHits = $p18Hits
    P19HitCount = $p19Hits.Count
    P19Hits = $p19Hits
    EvaluateHitCount = $evaluateHits.Count
    EvaluateHits = $evaluateHits
    BuildTargetHasAiUnit = [bool]($buildText -match 'aiUnit')
    BuildTargetHasTrait = [bool]($buildText -match 'Trait')
    BuildTargetHasDeterministic = [bool]($buildText -match 'deterministic')
    BuildTargetHasFailIfSkipped = [bool]($buildText -match 'FailIfPluginSessionLogSkipped')
    BuildTargetHasDotNetTest = [bool]($buildText -match 'DotNetTest')
    BuildTargetHasPreflight = [bool]($buildText -match 'preflight|Preflight')
    BuildTargetLength = $buildText.Length
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'p16-p19-skip-grep.json') -Encoding utf8

$priorMd = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T073737Z.md'
$priorJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T073737Z.json'
$priorObj = $null
$priorText = $null
if (Test-Path -LiteralPath $priorMd) { $priorText = Get-Content -LiteralPath $priorMd -Raw }
if (Test-Path -LiteralPath $priorJson) { $priorObj = Get-Content -LiteralPath $priorJson -Raw | ConvertFrom-Json }
[ordered]@{
    MdExists = (Test-Path -LiteralPath $priorMd)
    MdLastWriteTimeUtc = if (Test-Path -LiteralPath $priorMd) { (Get-Item -LiteralPath $priorMd).LastWriteTimeUtc.ToString('o') } else { $null }
    MdHasAgree = if ($priorText) { [bool]($priorText -match '(?m)^OverallVerdict:\s*AGREE\s*$') } else { $false }
    MdHasDisagree = if ($priorText) { [bool]($priorText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$') } else { $false }
    JsonExists = (Test-Path -LiteralPath $priorJson)
    JsonOverallVerdict = if ($priorObj) { [string]$priorObj.OverallVerdict } else { $null }
    JsonFailCount = if ($priorObj) { $priorObj.FailCount } else { $null }
    JsonPhase = if ($priorObj) { [string]$priorObj.Phase } else { $null }
    FilterTrxFailed = if ($priorObj -and $priorObj.TestResults) { $priorObj.TestResults.FilterTrxFailed } else { $null }
    FilterTrxPassed = if ($priorObj -and $priorObj.TestResults) { $priorObj.TestResults.FilterTrxPassed } else { $null }
    EvaluateAsyncImplemented = if ($priorObj -and $priorObj.TestResults) { $priorObj.TestResults.EvaluateAsyncImplemented } else { $null }
    P17NamedTestsPresent = if ($priorObj -and $priorObj.TestResults) { $priorObj.TestResults.P17NamedTestsPresent } else { $null }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-c-red-p16-agree.json') -Encoding utf8

$gitStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs build/Build.PluginSessionLogIntegration.cs docs/Project/TODO.yaml docs/todo.yaml
$gitLog = git -C 'F:\GitHub\McpServer' log -5 --format='%H %cI %s' -- tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs build/Build.PluginSessionLogIntegration.cs
Set-Content -LiteralPath (Join-Path $out 'git-status-pluginint.txt') -Value ($gitStatus | Out-String) -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'git-log-p16-p18.txt') -Value ($gitLog | Out-String) -Encoding utf8
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Porcelain = @($gitStatus)
    TodoYamlPorcelain = [string](git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

$py = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'python' } | ForEach-Object {
    [ordered]@{ Pid = $_.ProcessId; Name = $_.Name; CommandLine = $_.CommandLine }
})
[ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); PythonProcessCount = $py.Count; Processes = $py } |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'python-procs.json') -Encoding utf8

$leftover = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'testhost|vstest|dotnet' -and $_.CommandLine -match 'PluginIntegration' } | ForEach-Object {
    [ordered]@{ Pid = $_.ProcessId; Name = $_.Name; CreationDate = [string]$_.CreationDate; CommandLine = $_.CommandLine }
})
[ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); LeftoverCount = $leftover.Count; Processes = $leftover } |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'inspect-procs.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
