#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$out = Join-Path $outDir '02-files.json'

Push-Location $wt
try {
    $named = @(
        'plugins/core/lib-ps/McpPluginShim.psm1',
        'plugins/core/lib-ps/repl-invoke.ps1',
        'plugins/core/lib-ps/plugin-hook.ps1',
        'plugins/core/lib-ps/Invoke-McpPlugin.ps1',
        'plugins/core/lib-ps/resolve-cache-dir.ps1',
        'plugins/core/lib-ps/cache-manager.ps1',
        'plugins/core/hooks-templates/wrapper.ps1.template',
        'plugins/core/test-fixtures/pester/TriagePluginIdentity.Tests.ps1',
        'plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1'
    )
    $stats = @()
    foreach ($rel in $named) {
        $p = Join-Path $wt ($rel -replace '/', '\')
        $exists = Test-Path -LiteralPath $p
        $item = $null
        if ($exists) { $item = Get-Item -LiteralPath $p }
        $vsDevelop = ''
        if ($exists) {
            $vsDevelop = [string](git diff --stat origin/develop -- $rel)
        }
        $stats += [ordered]@{
            Rel = $rel
            Exists = $exists
            LastWriteTimeUtc = $(if ($exists) { $item.LastWriteTimeUtc.ToString('o') } else { $null })
            Length = $(if ($exists) { $item.Length } else { $null })
            DiffStatVsDevelop = $vsDevelop
        }
    }

    $identity = Get-Content -LiteralPath (Join-Path $wt 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1') -Raw
    $runtime = Get-Content -LiteralPath (Join-Path $wt 'plugins\core\test-fixtures\pester\PluginPowerShellRuntime.Tests.ps1') -Raw
    $repl = Get-Content -LiteralPath (Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1') -Raw
    $hook = Get-Content -LiteralPath (Join-Path $wt 'plugins\core\lib-ps\plugin-hook.ps1') -Raw

    $itMatch = [regex]::Match($identity, "(?ms)It 'TEST-MCP-TRIAGEPLUGIN-004 PersistTurn\.SubmitAsyncChildTimeout_ReturnsDegradedQueued' \{.*?^\s{4}\}")
    $itBody = if ($itMatch.Success) { $itMatch.Value } else { '' }
    $flushFailMatch = [regex]::Match($runtime, "(?ms)It 'session-end identified-workspace flush failure is not a silent \{\} success' \{.*?^\s{4}\}")
    $flushFailBody = if ($flushFailMatch.Success) { $flushFailMatch.Value } else { '' }
    $flushOkMatch = [regex]::Match($runtime, "(?ms)It 'session-end flushes pending YAML identified by CLAUDE_PROJECT_DIR' \{.*?^\s{4}\}")
    $flushOkBody = if ($flushOkMatch.Success) { $flushOkMatch.Value } else { '' }
    $diskMatch = [regex]::Match($identity, "(?ms)It 'TEST-MCP-VERIFYWRAP-001 disk-full IOException is typed and bounded process honors timeout' \{.*?^\s{4}\}")
    $diskBody = if ($diskMatch.Success) { $diskMatch.Value } else { '' }

    $obj = [ordered]@{
        TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Branch = [string](git rev-parse --abbrev-ref HEAD)
        Sha = [string](git rev-parse HEAD)
        FileStats = $stats
        IdentitySubmitTimeoutItLength = $itBody.Length
        IdentitySubmitTimeoutStubsInvokeReplRaw = ($itBody -match 'function Invoke-ReplRaw')
        IdentitySubmitTimeoutInjectsPersistedFalse = ($itBody -match 'Persisted\s*=\s*false' -or $itBody -match 'persisted\s*=\s*false')
        IdentitySubmitTimeoutUsesHangingCmd = ($itBody -match 'mcpserver-repl\.cmd' -and $itBody -match 'ping -n 21')
        IdentitySubmitTimeoutDotsReplInvoke = ($itBody -match 'repl-invoke\.ps1')
        IdentitySubmitTimeoutSleepStub = ($itBody -match 'Start-Sleep')
        FlushOkItLength = $flushOkBody.Length
        FlushOkAssertsPendingGone = ($flushOkBody -match 'Should -BeFalse')
        FlushFailItLength = $flushFailBody.Length
        FlushFailAssertsExit1 = ($flushFailBody -match 'Should -Be 1')
        FlushFailAssertsFlushFailedToken = ($flushFailBody -match 'flush-failed')
        FlushFailOnlyNegatesEmptySuccess = ($flushFailBody -match 'looksLikeUnresolvedSuccess' -and ($flushFailBody -notmatch 'Should -Be 1'))
        DiskItAssertsAudit = ($diskBody -match 'auditActions: 2' -and $diskBody -match 'lastBuildStatus: unknown')
        ReplPersistTimeoutBranch = ($repl -match 'beginTurn persist timed out; failsafe retained')
        HookFlushFailedExit1 = ($hook -match "status = 'flush-failed'" -and $hook -match 'exit 1')
        DiskFullHandlerMutatesFile = ([regex]::Match($hook, '(?ms)^function Invoke-PluginCodeVerifyHandleDiskFull \{.*?^\}').Value -match 'Set-YamlScalar|WriteAllText|Write-McpYamlObject|Set-Content')
    }
    $obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
    Write-Output ("WROTE {0} hangCmd={1} stubRaw={2} flushFailOnlyNegate={3} pendingGoneAssert={4}" -f $out, $obj.IdentitySubmitTimeoutUsesHangingCmd, $obj.IdentitySubmitTimeoutStubsInvokeReplRaw, $obj.FlushFailOnlyNegatesEmptySuccess, $obj.FlushOkAssertsPendingGone)
} finally {
    Pop-Location
}
