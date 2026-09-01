#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ws = 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-03.json'
$env:MCP_WORKSPACE_PATH = $ws
$env:MCP_PLUGIN_ROOT = $plugin
$env:GROK_PLUGIN_ROOT = $plugin
. (Join-Path $plugin 'lib\resolve-cache-dir.ps1')
$cacheDir = Resolve-McpCacheDir -StartPath $ws
$failsafeDir = try { Get-McpFailsafeDir } catch { Join-Path $cacheDir 'failsafe' }
$quarantineDir = try { Get-McpFailsafeQuarantineDir } catch { Join-Path $failsafeDir 'quarantine' }
$pendingDir = Join-Path $cacheDir 'pending'
$roots = @(
    $failsafeDir
    $quarantineDir
    $pendingDir
    (Join-Path $plugin 'cache\failsafe')
    (Join-Path $plugin 'cache\pending')
)
$items = @()
foreach ($root in $roots) {
    if (-not $root) { continue }
    if (-not (Test-Path -LiteralPath $root)) { continue }
    $files = @(Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue)
    foreach ($f in $files) {
        $head = ''
        try { $head = (Get-Content -LiteralPath $f.FullName -TotalCount 50 -ErrorAction Stop) -join "`n" } catch { $head = "$_" }
        $kind = 'other'
        if ($head -match 'backend_unavailable|HTTP\s*503|\b503\b') { $kind = '503-or-backend_unavailable' }
        elseif ($head -match 'timeout|timed out|command_timeout') { $kind = 'timeout' }
        elseif ($head -match 'session_submit|SessionLog.SubmitAsync') { $kind = 'session_submit' }
        $items += [ordered]@{
            Path = $f.FullName
            Length = $f.Length
            LastWriteTimeUtc = $f.LastWriteTimeUtc.ToString('o')
            KindGuess = $kind
            Head = $head.Substring(0, [Math]::Min(700, $head.Length))
        }
    }
}
$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    CacheDir = $cacheDir
    FailsafeDir = $failsafeDir
    QuarantineDir = $quarantineDir
    PendingDir = $pendingDir
    PendingCount = $items.Count
    KindCounts = @{
        timeout = @($items | Where-Object { $_.KindGuess -eq 'timeout' }).Count
        '503-or-backend_unavailable' = @($items | Where-Object { $_.KindGuess -eq '503-or-backend_unavailable' }).Count
        session_submit = @($items | Where-Object { $_.KindGuess -eq 'session_submit' }).Count
        other = @($items | Where-Object { $_.KindGuess -eq 'other' }).Count
    }
    Items = $items
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} cache={1} pendingCount={2}" -f $out, $cacheDir, $items.Count)
