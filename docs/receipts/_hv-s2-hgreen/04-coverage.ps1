#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$out = Join-Path $outDir '04-coverage.json'
$identity = Join-Path $wt 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$runtime = Join-Path $wt 'plugins\core\test-fixtures\pester\PluginPowerShellRuntime.Tests.ps1'

function Get-ItBlocks {
    param([string]$Path, [string]$FileLabel)
    $raw = Get-Content -LiteralPath $Path -Raw
    $matches = [regex]::Matches($raw, "(?ms)^\s+It '(?<name>[^']+)' \{(?<body>.*?)^\s+\}")
    $items = @()
    foreach ($m in $matches) {
        $name = $m.Groups['name'].Value
        if ($name -notmatch 'TEST-MCP-(STRICTCOUNT|FAILSAFE|SESSIONEND|XAGENT|VERIFYWRAP)-001|TEST-MCP-TRIAGEPLUGIN-004|session-end |workflow\.sessionlog\.updateTurn omitted|wrapper template documents|Invoke-McpPlugin sessionlog timeout') {
            continue
        }
        $body = $m.Groups['body'].Value
        $invokeCount = ([regex]::Matches($body, 'Invoke-[A-Za-z0-9]+')).Count
        $iex = ($body -match 'Invoke-Expression')
        $child = ($body -match 'ProcessStartInfo|Start-Process|pwsh\.exe')
        $matchOnly = ($body -match 'Should -Match' -or $body -match 'Should -BeLike') -and ($invokeCount -eq 0) -and -not $child
        $kind = 'unknown'
        if ($matchOnly) { $kind = 'regex-only' }
        elseif ($iex -and $invokeCount -gt 0) { $kind = 'extract-then-invoke' }
        elseif ($child) { $kind = 'child-process' }
        elseif ($invokeCount -gt 0) { $kind = 'behavioral' }
        $items += [ordered]@{
            Name = $name
            File = $FileLabel
            Kind = $kind
            InvokeCount = $invokeCount
            UsesInvokeExpression = $iex
            UsesChildProcess = $child
            RegexOnly = $matchOnly
            BodyLength = $body.Length
        }
    }
    return $items
}

$tests = @()
$tests += Get-ItBlocks -Path $identity -FileLabel 'TriagePluginIdentity.Tests.ps1'
$tests += Get-ItBlocks -Path $runtime -FileLabel 'PluginPowerShellRuntime.Tests.ps1'

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    IdentityExists = (Test-Path -LiteralPath $identity)
    RuntimeExists = (Test-Path -LiteralPath $runtime)
    SelectedCount = @($tests).Count
    RegexOnlyCount = @($tests | Where-Object { $_.Kind -eq 'regex-only' }).Count
    BehavioralCount = @($tests | Where-Object { $_.Kind -in @('behavioral','child-process','extract-then-invoke') }).Count
    Tests = $tests
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} selected={1} regexOnly={2} behavioral={3}" -f $out, $obj.SelectedCount, $obj.RegexOnlyCount, $obj.BehavioralCount)
