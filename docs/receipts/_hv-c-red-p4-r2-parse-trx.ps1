#Requires -Version 7.0
Set-StrictMode -Off
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4-r2'

function Parse-Trx {
    param([string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        return @{ exists = $false; path = $TrxPath }
    }
    [xml]$x = Get-Content -LiteralPath $TrxPath
    $c = $x.TestRun.ResultSummary.Counters
    $units = @()
    foreach ($u in @($x.TestRun.Results.UnitTestResult)) {
        $msg = $null
        if ($null -ne $u.Output -and $null -ne $u.Output.ErrorInfo) {
            $msg = [string]$u.Output.ErrorInfo.Message
        }
        $units += @{
            name = [string]$u.testName
            outcome = [string]$u.outcome
            message = $msg
        }
    }
    return @{
        exists = $true
        path = $TrxPath
        outcome = [string]$x.TestRun.ResultSummary.outcome
        total = [string]$c.total
        executed = [string]$c.executed
        passed = [string]$c.passed
        failed = [string]$c.failed
        skippedAttr = [string]$c.GetAttribute('skipped')
        notExecutedAttr = [string]$c.GetAttribute('notExecuted')
        units = $units
    }
}

$filter = Parse-Trx -TrxPath (Join-Path $out 'catalog-filter.trx')
$all = Parse-Trx -TrxPath (Join-Path $out 'pluginintegration-all.trx')
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    CatalogFilter = $filter
    PluginAll = $all
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'trx-parsed.json') -Encoding utf8
Write-Output 'TRX_PARSED'

$codex = 'F:\GitHub\mcpserver-codex-plugin'
Get-ChildItem -LiteralPath $codex -Filter '*Invoke*.ps1' -Recurse -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty FullName |
    Set-Content -LiteralPath (Join-Path $out 'codex-invoke-files.txt') -Encoding utf8
Write-Output 'CODEX_LISTED'

$apiKey = (Select-String -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Pattern '^apiKey:\s*(.+)$').Matches[0].Groups[1].Value.Trim()
try {
    $search = Invoke-RestMethod -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers @{ 'X-Api-Key' = $apiKey } -Method Get -TimeoutSec 30
    $search | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'tool-search-grok.json') -Encoding utf8
    $names = @()
    if ($search -is [System.Array]) {
        $names = @($search | ForEach-Object { $_.name })
    } else {
        foreach ($prop in @('items', 'tools', 'results', 'value')) {
            $val = $search.$prop
            if ($null -ne $val) {
                $names = @($val | ForEach-Object { $_.name })
                break
            }
        }
        if ($names.Count -eq 0 -and $null -ne $search.name) {
            $names = @($search.name)
        }
    }
    Write-Output ('TOOL_NAMES=' + ($names -join ','))
    Write-Output ('TOOL_EXACT=' + ($names -contains 'mcpserver-grok-plugin'))
} catch {
    Set-Content -LiteralPath (Join-Path $out 'tool-search-grok.json') -Value $_.Exception.ToString() -Encoding utf8
    Write-Output ('TOOL_SEARCH_FAIL=' + $_.Exception.Message)
}

Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip|Fact\(|Theory\(' |
    ForEach-Object { $_.ToString() } |
    Set-Content -LiteralPath (Join-Path $out 'skip-scan.txt') -Encoding utf8
Write-Output 'SKIP_SCAN_DONE'
