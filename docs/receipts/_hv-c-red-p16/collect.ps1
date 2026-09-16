#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
$testsDir = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$files = @(
    'PluginSessionLogAiTheoryTests.cs',
    'AiStrategyFixture.cs',
    'AiStrategyEvaluation.cs',
    'PluginSessionLogCollection.cs',
    'PluginSessionLogCatalog.cs'
)
$timestamps = foreach ($name in $files) {
    $path = Join-Path $testsDir $name
    $item = Get-Item -LiteralPath $path
    $text = Get-Content -LiteralPath $path -Raw
    [ordered]@{
        Name = $name
        Path = $path
        LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        Length = $item.Length
        HasSkipAttribute = [bool]($text -match '\[(Fact|Theory)\s*\(\s*Skip')
        HasTheorySkip = [bool]($text -match 'Theory\s*\(\s*Skip')
        HasAssertSkip = [bool]($text -match 'Assert\.Skip')
        Has7147 = [bool]($text -match '7147')
        HasServerFixture = [bool]($text -match 'PluginIntegrationServerFixture|WebApplication|StartServer|LaunchAsync')
        HasLoadAndValidate = [bool]($text -match 'LoadAndValidate')
        HasRedactedReceipt = [bool]($text -match 'redactedReceipt')
        HasInvalidOperationNotImplemented = [bool]($text -match 'not implemented')
        HasRequiredValid = [bool]($text -match 'required bool Valid')
        HasRequiredMissingFields = [bool]($text -match 'required IReadOnlyList<string> MissingFields')
        HasRequiredContradictions = [bool]($text -match 'required IReadOnlyList<string> Contradictions')
        HasP16TheoryName = [bool]($text -match 'AiTheory_Agent_RequiresValidJsonFields')
        HasP17RejectsName = [bool]($text -match 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure')
        HasP18NukeSkipName = [bool]($text -match 'NukeTarget_SkipIsFailure')
        InlineDataHosts = @([regex]::Matches($text, 'InlineData\(PluginHostKind\.(\w+)\)') | ForEach-Object { $_.Groups[1].Value })
    }
}
$timestamps | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'file-timestamps.json') -Encoding utf8

$csFiles = Get-ChildItem -LiteralPath $testsDir -Filter '*.cs' -File
$skipHits = @()
$p17Hits = @()
$p18Hits = @()
$p16Hits = @()
foreach ($f in $csFiles) {
    $lines = Get-Content -LiteralPath $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '\[Skip|Fact\(Skip|Theory\(Skip|Assert\.Skip') {
            $skipHits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
        if ($line -match 'AiTheory_RejectsInvalidJson') {
            $p17Hits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
        if ($line -match 'NukeTarget_SkipIsFailure') {
            $p18Hits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
        if ($line -match 'AiTheory_') {
            $p16Hits += [ordered]@{ File = $f.Name; Line = ($i + 1); Text = $line.Trim() }
        }
    }
}

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
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'p16-p17-skip-grep.json') -Encoding utf8

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T071202Z.md'
$priorJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T071202Z.json'
$priorMd = Get-Content -LiteralPath $priorAgree -Raw
$priorJ = Get-Content -LiteralPath $priorJson -Raw | ConvertFrom-Json
[ordered]@{
    MdPath = $priorAgree
    MdLastWriteTimeUtc = (Get-Item -LiteralPath $priorAgree).LastWriteTimeUtc.ToString('o')
    MdOverallVerdictMatch = [bool]($priorMd -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    MdP15FilterLine = [bool]($priorMd -match 'Passed:\s*24,\s*Skipped:\s*0,\s*Total:\s*24')
    MdFull86 = [bool]($priorMd -match 'Passed:\s*86,\s*Skipped:\s*0,\s*Total:\s*86')
    MdP16Absent = [bool]($priorMd -match 'P16 names absent|P16 AiTheory_ named tests are absent|p16Count 0')
    JsonOverallVerdict = [string]$priorJ.OverallVerdict
    JsonFilterPassed = $priorJ.TestResults.FilterConsolePassed
    JsonFilterFailed = $priorJ.TestResults.FilterConsoleFailed
    JsonFilterSkipped = $priorJ.TestResults.FilterConsoleSkipped
    JsonFullPassed = $priorJ.TestResults.FullConsolePassed
    JsonFullFailed = $priorJ.TestResults.FullConsoleFailed
    JsonFullSkipped = $priorJ.TestResults.FullConsoleSkipped
    JsonP16NamedTestsPresent = $priorJ.TestResults.P16NamedTestsPresent
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'prior-c-green-p15-agree.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
