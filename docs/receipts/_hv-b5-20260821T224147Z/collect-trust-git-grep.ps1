$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
Set-Location 'F:\GitHub\McpServer'

# Health nonce
$nonce = [guid]::NewGuid().ToString('N')
$healthUrl = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
try {
    $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 30
    $healthObj = [ordered]@{
        nonceSent = $nonce
        statusCode = [int]$resp.StatusCode
        body = $resp.Content
        echoed = $resp.Content.Contains($nonce)
    }
} catch {
    $healthObj = [ordered]@{
        nonceSent = $nonce
        error = $_.Exception.Message
    }
}
$healthObj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'health.json') -Encoding utf8
Write-Output ('HEALTH_STATUS=' + $healthObj.statusCode)
Write-Output ('HEALTH_ECHO=' + $healthObj.echoed)

# Marker signature
$sig = $null
try {
    Import-Module 'F:\GitHub\McpServer\tools\powershell\McpTrust.psm1' -Force -ErrorAction Stop
    $sig = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
} catch {
    try {
        . 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1'
        $sig = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
    } catch {
        $sig = 'ERROR:' + $_.Exception.Message
    }
}
Set-Content -LiteralPath (Join-Path $out 'marker-signature.txt') -Value ([string]$sig) -Encoding utf8
Write-Output ('MARKER_SIG=' + $sig)

# return 2 in Get-ReplMethodTimeoutSeconds
$repl = 'F:\GitHub\McpServer\plugins\core\lib-ps\repl-invoke.ps1'
$return2 = Select-String -LiteralPath $repl -Pattern 'return 2' -SimpleMatch
$drainFn = Select-String -LiteralPath $repl -Pattern 'Get-ReplMethodTimeoutSeconds|REPL_FAILSAFE_DRAIN_TIMEOUT|ReplFailsafeDraining|client.SessionLog.SubmitAsync'
$return2 | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line } | Set-Content -LiteralPath (Join-Path $out 'grep-return2.txt') -Encoding utf8
if (-not $return2) { Set-Content -LiteralPath (Join-Path $out 'grep-return2.txt') -Value 'NO_MATCH' -Encoding utf8 }
$drainFn | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line } | Set-Content -LiteralPath (Join-Path $out 'grep-drain-timeout.txt') -Encoding utf8
Write-Output ('RETURN2_COUNT=' + @($return2).Count)

# Get-McpFailsafeDir
$cache = 'F:\GitHub\McpServer\plugins\core\lib-ps\resolve-cache-dir.ps1'
$fs = Select-String -LiteralPath $cache -Pattern 'function Get-McpFailsafeDir|MCPSERVER_FAILSAFE_DIR|MCP_FAILSAFE_DIR|failsafe/.+agent|Join-Path \(Resolve-McpCacheDir\)'
$fs | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line } | Set-Content -LiteralPath (Join-Path $out 'grep-failsafe-dir.txt') -Encoding utf8

# Copy-CoreFile
$sync = 'F:\GitHub\McpServer\plugins\core\sync\sync-plugin-core.ps1'
$copy = Select-String -LiteralPath $sync -Pattern 'function Copy-CoreFile|Copy-Item|Set-Content|LF|NewLine'
$copy | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line } | Set-Content -LiteralPath (Join-Path $out 'grep-copy-corefile.txt') -Encoding utf8

# Git timestamps vs B2 AGREE
$b2 = [datetimeoffset]::Parse('2026-08-21T22:01:15Z')
$files = @(
    'plugins/core/lib-ps/repl-invoke.ps1',
    'plugins/core/lib-ps/resolve-cache-dir.ps1',
    'plugins/core/sync/sync-plugin-core.ps1',
    'src/McpServer.Repl.Core/ReplCommandDispatcher.cs',
    'src/McpServer.Repl.Core/SessionLogPersistence.cs',
    'src/McpServer.Repl.Host/ServiceCollectionExtensions.cs',
    'tests/McpServer.Repl.Core.Tests/SessionLogPersistenceCoordinatorIsolationTests.cs',
    'tests/Build.Tests/SyncAgentPluginsChecksumTests.cs',
    'plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1'
)
$gitRows = foreach ($f in $files) {
    $log = git log -n 5 --format='%H %cI %s' -- $f
    $status = git status --porcelain -- $f
    $item = Get-Item -LiteralPath (Join-Path 'F:\GitHub\McpServer' $f) -ErrorAction SilentlyContinue
    [pscustomobject]@{
        file = $f
        lastWriteUtc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { 'missing' }
        lastWriteAfterB2 = if ($item) { $item.LastWriteTimeUtc -gt $b2.UtcDateTime } else { $false }
        porcelain = [string]$status
        log = @($log)
    }
}
$gitRows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'git-file-history.json') -Encoding utf8
git status --porcelain | Set-Content -LiteralPath (Join-Path $out 'git-porcelain.txt') -Encoding utf8
git log -n 20 --format='%H %cI %s' | Set-Content -LiteralPath (Join-Path $out 'git-log-head.txt') -Encoding utf8

# Plugin checksums vs canonical
$canonicalDir = 'F:\GitHub\McpServer\plugins\core\lib-ps'
$githubRoot = 'F:\GitHub'
$official = @(
    'mcpserver-codex-plugin',
    'mcpserver-claude-code-plugin',
    'mcpserver-copilot-plugin',
    'mcpserver-cline-plugin',
    'mcpserver-grok-plugin'
)
$mismatches = New-Object System.Collections.Generic.List[string]
$comparisons = New-Object System.Collections.Generic.List[object]
foreach ($plugin in $official) {
    $pluginRoot = Join-Path $githubRoot $plugin
    $canonicalFiles = Get-ChildItem -LiteralPath $canonicalDir -File | Where-Object { $_.Name -ne 'GAPS.md' }
    foreach ($cf in $canonicalFiles) {
        $candidates = @(
            (Join-Path $pluginRoot (Join-Path 'lib' $cf.Name)),
            (Join-Path $pluginRoot (Join-Path 'lib-ps' $cf.Name))
        )
        $copy = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if (-not $copy) {
            $mismatches.Add($plugin + ': missing ' + $cf.Name) | Out-Null
            $comparisons.Add([ordered]@{ plugin = $plugin; file = $cf.Name; status = 'missing' }) | Out-Null
            continue
        }
        $h1 = (Get-FileHash -LiteralPath $cf.FullName -Algorithm SHA256).Hash
        $h2 = (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash
        $match = $h1 -eq $h2
        if (-not $match) { $mismatches.Add($plugin + ': drift ' + $cf.Name) | Out-Null }
        $comparisons.Add([ordered]@{
            plugin = $plugin
            file = $cf.Name
            canonical = $h1
            copy = $h2
            path = $copy
            match = $match
        }) | Out-Null
    }
}
[ordered]@{ mismatchCount = $mismatches.Count; mismatches = @($mismatches); comparisons = @($comparisons) } |
    ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'plugin-checksums.json') -Encoding utf8
Write-Output ('CHECKSUM_MISMATCHES=' + $mismatches.Count)

# B2 receipt verdict
$b2Receipt = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T220115Z.md' -Raw
$b2Line = ($b2Receipt -split "`n" | Where-Object { $_ -match 'OverallVerdict' } | Select-Object -First 3) -join ' | '
Set-Content -LiteralPath (Join-Path $out 'b2-verdict.txt') -Value $b2Line -Encoding utf8
Write-Output ('B2=' + $b2Line)

Write-Output 'COLLECT_TRUST_DONE'
