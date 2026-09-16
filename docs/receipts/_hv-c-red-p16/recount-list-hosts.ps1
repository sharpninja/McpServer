#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
$listText = Get-Content -LiteralPath (Join-Path $out 'hv-dotnet-list-tests.log') -Raw
$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$counts = foreach ($h in $hostKinds) {
    $pat = "AiTheory_Agent_RequiresValidJsonFields\(hostKind: $h\)"
    [ordered]@{ HostKind = $h; Count = ([regex]::Matches($listText, $pat)).Count }
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    ListAiTheoryHostKindRows = ([regex]::Matches($listText, 'AiTheory_Agent_RequiresValidJsonFields\(hostKind:')).Count
    P17 = ([regex]::Matches($listText, 'AiTheory_RejectsInvalidJson')).Count
    P18 = ([regex]::Matches($listText, 'NukeTarget_SkipIsFailure')).Count
    SkipInList = ([regex]::Matches($listText, '\[SKIP')).Count
    TotalAvailable = ([regex]::Matches($listText, '(?m)^\s+McpServer\.PluginIntegration\.Tests\.')).Count
    HostCounts = $counts
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8
Write-Output 'RECOUNT_DONE'
