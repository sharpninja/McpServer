#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\04-ac-grep.json'
$lib = Join-Path $wt 'plugins\core\lib-ps'
$tests = Join-Path $wt 'plugins\core\test-fixtures\pester'

function Get-PatternHits {
    param([string]$Root, [string]$Pattern, [string]$Filter = '*.ps1')
    $hits = @()
    $files = Get-ChildItem -LiteralPath $Root -Filter $Filter -Recurse -File
    foreach ($f in $files) {
        $lines = Select-String -LiteralPath $f.FullName -Pattern $Pattern -SimpleMatch:$false
        foreach ($l in $lines) {
            $hits += [ordered]@{
                File = $f.FullName.Substring($wt.Length + 1)
                Line = $l.LineNumber
                Text = $l.Line.Trim()
            }
        }
    }
    return @($hits)
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Patterns = [ordered]@{
        CommandTimeout = @(Get-PatternHits -Root $lib -Pattern 'command_timeout')
        RetryableTrue = @(Get-PatternHits -Root $lib -Pattern 'retryable:\s*true')
        BackendUnavailable = @(Get-PatternHits -Root $lib -Pattern 'backend_unavailable')
        Http503 = @(Get-PatternHits -Root $lib -Pattern '503')
        DrainAttempts = @(Get-PatternHits -Root $lib -Pattern 'drainAttempts')
        CountCannot = @(Get-PatternHits -Root $tests -Pattern 'Count cannot be found')
        UpdateTurnTags = @(Get-PatternHits -Root $tests -Pattern 'updateTurn')
        SessionEndEmpty = @(Get-PatternHits -Root $tests -Pattern 'SessionEnd')
        CrossSource = @(Get-PatternHits -Root $tests -Pattern 'sourceType')
        DiskFull = @(Get-PatternHits -Root $tests -Pattern 'disk_full')
        DiskFullLib = @(Get-PatternHits -Root $lib -Pattern 'disk_full')
        PersistedFalse = @(Get-PatternHits -Root $tests -Pattern 'Persisted\s*=\s*\$false')
        SubmitAsyncTimeout = @(Get-PatternHits -Root $tests -Pattern 'SubmitAsync')
        TimeoutSecondsHonor = @(Get-PatternHits -Root $tests -Pattern 'TimeoutSeconds')
        TestReplFailsafe = @(Get-PatternHits -Root $lib -Pattern 'Test-ReplFailsafeBackendUnreachable')
        TestReplFailsafeTests = @(Get-PatternHits -Root $tests -Pattern 'Test-ReplFailsafeBackendUnreachable')
        CompleteTurn = @(Get-PatternHits -Root $tests -Pattern 'CompleteTurn')
        InvokeMcpPluginTimeout = @(Get-PatternHits -Root (Join-Path $wt 'plugins\core\lib-ps') -Pattern 'Plugin command timed out')
    }
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0}" -f $out)
