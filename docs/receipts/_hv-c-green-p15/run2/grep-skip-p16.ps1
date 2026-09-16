#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$root = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
$cs = @(Get-ChildItem -LiteralPath $root -Recurse -Filter '*.cs' -File)

function Count-Matches([string]$pattern) {
    $hits = @($cs | Select-String -Pattern $pattern -ErrorAction SilentlyContinue)
    [ordered]@{
        Pattern = $pattern
        Count = $hits.Count
        Files = @($hits | Select-Object -First 20 | ForEach-Object { $_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
    }
}

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Root = $root
    CsFileCount = $cs.Count
    SkipAttr = (Count-Matches '\[Skip')
    FactSkip = (Count-Matches 'Fact\(Skip')
    TheorySkip = (Count-Matches 'Theory\(Skip')
    AssertSkip = (Count-Matches 'Assert\.Skip')
    AiTheory = (Count-Matches 'AiTheory_')
    P16Ident = (Count-Matches '\bP16\b')
    JsonSkip = (Count-Matches 'JsonCommentHandling\.Skip')
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'p15-p16-skip-grep.json') -Encoding utf8
Write-Output 'GREP_DONE'
