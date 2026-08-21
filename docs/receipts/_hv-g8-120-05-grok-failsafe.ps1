#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.mcpServer\grok'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-05.json'
$failsafe = Join-Path $root 'failsafe'
$quarantine = Join-Path $failsafe 'quarantine'
$pending = Join-Path $root 'pending'
$session = Join-Path $root 'session-state.yaml'
$turn = Join-Path $root 'current-turn.yaml'

function Get-Inv {
    param([string]$Dir)
    if (-not (Test-Path -LiteralPath $Dir)) {
        return @()
    }
    $files = @(Get-ChildItem -LiteralPath $Dir -File -ErrorAction SilentlyContinue)
    $items = @()
    foreach ($f in $files) {
        $head = ''
        try { $head = (Get-Content -LiteralPath $f.FullName -TotalCount 80 -ErrorAction Stop) -join "`n" } catch { $head = "$_" }
        $kind = 'other'
        if ($head -match 'backend_unavailable|HTTP\s*503|\b503\b') { $kind = '503-or-backend_unavailable' }
        elseif ($head -match 'timeout|timed out|command_timeout') { $kind = 'timeout' }
        elseif ($head -match 'session_submit|SessionLog.SubmitAsync') { $kind = 'session_submit' }
        $method = $null
        if ($head -match '(?m)^method:\s*(.+)$') { $method = $Matches[1].Trim() }
        $label = $null
        if ($head -match '(?m)^label:\s*(.+)$') { $label = $Matches[1].Trim() }
        $code = $null
        if ($head -match '(?m)^\s*code:\s*(.+)$') { $code = $Matches[1].Trim() }
        $items += [ordered]@{
            Name = $f.Name
            Path = $f.FullName
            Length = $f.Length
            LastWriteTimeUtc = $f.LastWriteTimeUtc.ToString('o')
            KindGuess = $kind
            Method = $method
            Label = $label
            Code = $code
            Head = $head.Substring(0, [Math]::Min(900, $head.Length))
        }
    }
    return $items
}

$live = @(Get-Inv -Dir $failsafe)
$q = @()
if (Test-Path -LiteralPath $quarantine) {
    $q = @(Get-ChildItem -LiteralPath $quarantine -File -ErrorAction SilentlyContinue | ForEach-Object {
        [ordered]@{ Name = $_.Name; Length = $_.Length; LastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o') }
    })
}
$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    CacheDir = $root
    FailsafeDir = $failsafe
    LiveCount = $live.Count
    QuarantineCount = $q.Count
    KindCounts = @{
        timeout = @($live | Where-Object { $_.KindGuess -eq 'timeout' }).Count
        '503-or-backend_unavailable' = @($live | Where-Object { $_.KindGuess -eq '503-or-backend_unavailable' }).Count
        session_submit = @($live | Where-Object { $_.KindGuess -eq 'session_submit' }).Count
        other = @($live | Where-Object { $_.KindGuess -eq 'other' }).Count
    }
    Methods = @($live | Group-Object Method | ForEach-Object { [ordered]@{ Method = $_.Name; Count = $_.Count } })
    SessionState = if (Test-Path $session) { Get-Content -LiteralPath $session -Raw } else { $null }
    CurrentTurn = if (Test-Path $turn) { Get-Content -LiteralPath $turn -Raw } else { $null }
    LiveItems = $live
    QuarantinePreview = @($q | Select-Object -First 20)
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} live={1} quarantine={2}" -f $out, $live.Count, $q.Count)
