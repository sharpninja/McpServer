#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2'
$testsDir = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
$buildTests = 'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
$buildTarget = 'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs'
$csproj = Join-Path $testsDir 'McpServer.PluginIntegration.Tests.csproj'
$aiunit = Join-Path $testsDir 'appsettings.aiunit.json'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$files = @(
    (Join-Path $testsDir 'PluginSessionLogAiTheoryTests.cs'),
    (Join-Path $testsDir 'AiStrategyFixture.cs'),
    (Join-Path $testsDir 'AiStrategyEvaluation.cs'),
    (Join-Path $testsDir 'PluginSessionLogCollection.cs'),
    (Join-Path $testsDir 'PluginSessionLogCatalog.cs'),
    $csproj,
    $aiunit,
    $buildTests,
    $buildTarget
)
$timestamps = foreach ($path in $files) {
    if (-not (Test-Path -LiteralPath $path)) {
        [ordered]@{ Name = [IO.Path]::GetFileName($path); Path = $path; Exists = $false }
        continue
    }
    $item = Get-Item -LiteralPath $path
    $text = Get-Content -LiteralPath $path -Raw
    [ordered]@{
        Name = $item.Name
        Path = $path
        Exists = $true
        LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        Length = $item.Length
        HasSkipAttribute = [bool]($text -match '\[(Fact|Theory)\s*\(\s*Skip')
        HasAssertSkip = [bool]($text -match 'Assert\.Skip')
        HasInvalidOperationNotImplemented = [bool]($text -match 'not implemented')
        HasEvaluateAsync = [bool]($text -match 'EvaluateAsync')
        HasJsonParse = [bool]($text -match 'JsonDocument\.Parse')
        HasDeterministicFailure = [bool]($text -match 'deterministicFailure')
        HasInvalidJsonCatch = [bool]($text -match 'invalid-json')
        HasRequiredFields = [bool]($text -match 'RequiredFields')
        HasP16TheoryName = [bool]($text -match 'AiTheory_Agent_RequiresValidJsonFields')
        HasP17RejectsName = [bool]($text -match 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure')
        HasP18NukeSkipName = [bool]($text -match 'NukeTarget_SkipIsFailure')
        HasFailIfSkippedCall = [bool]($text -match 'FailIfPluginSessionLogSkipped')
        HasPreflightCall = [bool]($text -match 'PreflightPluginSessionLogAiUnitStrategy')
        HasSharpNinjaAiUnit = [bool]($text -match 'SharpNinja\.aiUnit')
        HasActiveStrategy = [bool]($text -match 'ActiveStrategy')
        HasStrategies = [bool]($text -match 'Strategies')
        HasGrokBuild = [bool]($text -match 'grok-build')
        HasTraitAi = [bool]($text -match 'Trait\("PluginInt",\s*"AI"\)')
        HasTraitDeterministic = [bool]($text -match 'Trait\("PluginInt",\s*"Deterministic"\)')
        HasP19NativeSuite = [bool]($text -match 'PluginNativeSuite_')
        InlineDataHosts = @([regex]::Matches($text, 'InlineData\(PluginHostKind\.(\w+)\)') | ForEach-Object { $_.Groups[1].Value })
    }
}
$timestamps | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'file-timestamps.json') -Encoding utf8

$skipHits = [System.Collections.Generic.List[object]]::new()
$p16Hits = [System.Collections.Generic.List[object]]::new()
$p17Hits = [System.Collections.Generic.List[object]]::new()
$p18Hits = [System.Collections.Generic.List[object]]::new()
$p19Hits = [System.Collections.Generic.List[object]]::new()
$evaluateHits = [System.Collections.Generic.List[object]]::new()
$preflightHits = [System.Collections.Generic.List[object]]::new()
$traitHits = [System.Collections.Generic.List[object]]::new()
$csFiles = @(Get-ChildItem -LiteralPath $testsDir -Filter '*.cs' -File)
$csFiles += Get-Item -LiteralPath $buildTests
$csFiles += Get-Item -LiteralPath $buildTarget
foreach ($f in $csFiles) {
    $lines = Get-Content -LiteralPath $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '\[Skip|Fact\(Skip|Theory\(Skip|Assert\.Skip') {
            $skipHits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'AiTheory_Agent_RequiresValidJsonFields') {
            $p16Hits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'AiTheory_RejectsInvalidJson') {
            $p17Hits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'NukeTarget_SkipIsFailure') {
            $p18Hits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'PluginNativeSuite_|PluginInt_P19_') {
            $p19Hits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'EvaluateAsync|not implemented|deterministicFailure|invalid-json') {
            $evaluateHits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'PreflightPluginSessionLogAiUnitStrategy|SharpNinja\.aiUnit|ActiveStrategy|FailIfPluginSessionLogSkipped|PluginInt=') {
            $preflightHits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
        if ($line -match 'Trait\("PluginInt"') {
            $traitHits.Add([ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() })
        }
    }
}

$buildText = Get-Content -LiteralPath $buildTarget -Raw
$csprojText = Get-Content -LiteralPath $csproj -Raw
$aiunitText = Get-Content -LiteralPath $aiunit -Raw
$nukeTestText = Get-Content -LiteralPath $buildTests -Raw
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    SkipHitCount = $skipHits.Count
    SkipHits = @($skipHits)
    P16AiTheoryHitCount = $p16Hits.Count
    P16AiTheoryHits = @($p16Hits)
    P17RejectsHitCount = $p17Hits.Count
    P17RejectsHits = @($p17Hits)
    P18NukeSkipHitCount = $p18Hits.Count
    P18NukeSkipHits = @($p18Hits)
    P19HitCount = $p19Hits.Count
    P19Hits = @($p19Hits)
    EvaluateHitCount = $evaluateHits.Count
    EvaluateHits = @($evaluateHits)
    PreflightHitCount = $preflightHits.Count
    PreflightHits = @($preflightHits)
    TraitHitCount = $traitHits.Count
    TraitHits = @($traitHits)
    BuildTargetHasAiUnit = [bool]($buildText -match 'aiUnit|SharpNinja\.aiUnit')
    BuildTargetHasSharpNinjaAiUnit = [bool]($buildText -match 'SharpNinja\.aiUnit')
    BuildTargetHasTrait = [bool]($buildText -match 'PluginInt=')
    BuildTargetHasDeterministicFilter = [bool]($buildText -match 'PluginInt=Deterministic')
    BuildTargetHasAiFilter = [bool]($buildText -match 'PluginInt=AI')
    BuildTargetHasFailIfSkipped = [bool]($buildText -match 'FailIfPluginSessionLogSkipped')
    BuildTargetFailIfSkippedCallCount = ([regex]::Matches($buildText, 'FailIfPluginSessionLogSkipped\(')).Count
    BuildTargetHasDotNetTest = [bool]($buildText -match 'DotNetTest')
    BuildTargetDotNetTestCount = ([regex]::Matches($buildText, 'DotNetTest\(')).Count
    BuildTargetHasPreflight = [bool]($buildText -match 'PreflightPluginSessionLogAiUnitStrategy')
    BuildTargetPreflightCallCount = ([regex]::Matches($buildText, 'PreflightPluginSessionLogAiUnitStrategy\(')).Count
    BuildTargetHasActiveStrategy = [bool]($buildText -match 'ActiveStrategy')
    BuildTargetHasStrategies = [bool]($buildText -match 'Strategies')
    BuildTargetHasGrokBuild = [bool]($buildText -match 'grok-build')
    BuildTargetHasAppsettingsAiunit = [bool]($buildText -match 'appsettings\.aiunit\.json')
    BuildTargetLength = $buildText.Length
    CsprojHasSharpNinjaAiUnit = [bool]($csprojText -match 'SharpNinja\.aiUnit')
    AiunitHasActiveStrategy = [bool]($aiunitText -match '"ActiveStrategy"')
    AiunitHasStrategies = [bool]($aiunitText -match '"Strategies"')
    AiunitHasGrokBuild = [bool]($aiunitText -match 'grok-build')
    NukeTestCallsPreflight = [bool]($nukeTestText -match 'Build\.PreflightPluginSessionLogAiUnitStrategy')
    NukeTestCallsFailIfSkipped = [bool]($nukeTestText -match 'Build\.FailIfPluginSessionLogSkipped')
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'p16-p19-skip-grep.json') -Encoding utf8

$priorMd = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T081750Z.md'
$priorJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T081750Z.json'
$cRedMd = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T073737Z.md'
$cRedJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T073737Z.json'
$priorObj = $null
$priorText = $null
if (Test-Path -LiteralPath $priorMd) { $priorText = Get-Content -LiteralPath $priorMd -Raw }
if (Test-Path -LiteralPath $priorJson) { $priorObj = Get-Content -LiteralPath $priorJson -Raw | ConvertFrom-Json }
$cRedObj = $null
$cRedText = $null
if (Test-Path -LiteralPath $cRedMd) { $cRedText = Get-Content -LiteralPath $cRedMd -Raw }
if (Test-Path -LiteralPath $cRedJson) { $cRedObj = Get-Content -LiteralPath $cRedJson -Raw | ConvertFrom-Json }
[ordered]@{
    MdExists = (Test-Path -LiteralPath $priorMd)
    MdLastWriteTimeUtc = if (Test-Path -LiteralPath $priorMd) { (Get-Item -LiteralPath $priorMd).LastWriteTimeUtc.ToString('o') } else { $null }
    MdHasAgree = if ($priorText) { [bool]($priorText -match '(?m)^OverallVerdict:\s*AGREE\s*$') } else { $false }
    MdHasDisagree = if ($priorText) { [bool]($priorText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$') } else { $false }
    JsonExists = (Test-Path -LiteralPath $priorJson)
    JsonOverallVerdict = if ($priorObj) { [string]$priorObj.OverallVerdict } else { $null }
    JsonFailCount = if ($priorObj) { $priorObj.FailCount } else { $null }
    JsonPhase = if ($priorObj) { [string]$priorObj.Phase } else { $null }
    JsonExplicitFailList = if ($priorObj) { @($priorObj.ExplicitFailList) } else { @() }
    CRedMdExists = (Test-Path -LiteralPath $cRedMd)
    CRedMdLastWriteTimeUtc = if (Test-Path -LiteralPath $cRedMd) { (Get-Item -LiteralPath $cRedMd).LastWriteTimeUtc.ToString('o') } else { $null }
    CRedHasAgree = if ($cRedText) { [bool]($cRedText -match '(?m)^OverallVerdict:\s*AGREE\s*$') } else { $false }
    CRedJsonOverallVerdict = if ($cRedObj) { [string]$cRedObj.OverallVerdict } else { $null }
    CRedFilterTrxFailed = if ($cRedObj -and $cRedObj.TestResults) { $cRedObj.TestResults.FilterTrxFailed } else { $null }
    CRedFilterTrxPassed = if ($cRedObj -and $cRedObj.TestResults) { $cRedObj.TestResults.FilterTrxPassed } else { $null }
    CRedEvaluateAsyncImplemented = if ($cRedObj -and $cRedObj.TestResults) { $cRedObj.TestResults.EvaluateAsyncImplemented } else { $null }
    CRedP17NamedTestsPresent = if ($cRedObj -and $cRedObj.TestResults) { $cRedObj.TestResults.P17NamedTestsPresent } else { $null }
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'prior-receipts.json') -Encoding utf8

$gitStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs build/Build.PluginSessionLogIntegration.cs docs/Project/TODO.yaml docs/todo.yaml
$gitLog = git -C 'F:\GitHub\McpServer' log -8 --format='%H %cI %s' -- tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs build/Build.PluginSessionLogIntegration.cs
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
