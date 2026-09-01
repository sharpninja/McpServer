$ErrorActionPreference = 'Continue'
$paths = @(
    'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\chat_history.jsonl',
    'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\updates.jsonl',
    'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\summary.json'
)
Write-Output '=== parent session grep HMAC / plugin / homemade ==='
foreach ($p in $paths) {
    if (-not (Test-Path $p)) { Write-Output "missing $p"; continue }
    Write-Output "FILE $p"
    Select-String -Path $p -Pattern 'HMACSHA256|Test-MarkerSignature|Invoke-FullBootstrap|homemade|false-negative|plugin-hmac|roll your own HMAC' -ErrorAction SilentlyContinue |
        Select-Object -First 40 |
        ForEach-Object { 'L{0}: {1}' -f $_.LineNumber, $_.Line.Substring(0, [Math]::Min(400, $_.Line.Length)) }
}

Write-Output '=== parent subagent metas ==='
$sa = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\subagents'
if (Test-Path $sa) {
    Get-ChildItem -LiteralPath $sa -Directory | ForEach-Object {
        $meta = Join-Path $_.FullName 'meta.json'
        $out = Join-Path $_.FullName 'output.json'
        Write-Output "SUB $($_.Name)"
        if (Test-Path $meta) { Get-Content $meta -Raw | Select-Object -First 1 }
        if (Test-Path $out) {
            $t = Get-Content $out -Raw
            if ($t -match 'HMAC|homemade|Test-MarkerSignature|false-negative') {
                Write-Output "OUTPUT_HIT length=$($t.Length)"
                Write-Output $t.Substring(0, [Math]::Min(1500, $t.Length))
            } else {
                Write-Output 'OUTPUT_NO_HMAC_HIT'
            }
        }
    }
}

Write-Output '=== parent terminal logs last 30 min with HMAC ==='
$term = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\terminal'
if (Test-Path $term) {
    Get-ChildItem -LiteralPath $term -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 20 | ForEach-Object {
        '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.Name
    }
    Select-String -Path (Join-Path $term '*.log') -Pattern 'HMACSHA256|Test-MarkerSignature|Invoke-FullBootstrap|homemade|false-negative|computed' -ErrorAction SilentlyContinue |
        Select-Object -First 50 |
        ForEach-Object { '{0}:{1}:{2}' -f $_.Filename, $_.LineNumber, $_.Line.Trim().Substring(0, [Math]::Min(300, $_.Line.Trim().Length)) }
}
