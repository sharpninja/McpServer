$ErrorActionPreference = 'Continue'
Write-Output '=== grok cache tree ==='
$g = 'F:\GitHub\McpServer\.mcpServer\grok'
if (Test-Path $g) {
    Get-ChildItem -LiteralPath $g -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 30 |
        ForEach-Object { '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.FullName }
} else { Write-Output "MISSING $g" }

Write-Output '=== files last 20 min with HMAC or MarkerSignature ==='
$cutoff = [DateTime]::UtcNow.AddMinutes(-20)
$roots = @(
    'F:\GitHub\McpServer\docs',
    'F:\GitHub\McpServer\.mcpServer',
    'C:\Users\kingd\.grok'
)
foreach ($r in $roots) {
    if (-not (Test-Path $r)) { continue }
    Get-ChildItem -LiteralPath $r -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -gt $cutoff -and $_.Extension -in '.md','.json','.txt','.ps1','.yaml','.log' } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 50 |
        ForEach-Object { '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.FullName }
}

Write-Output '=== plugin json version ==='
$pj = 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json'
if (Test-Path $pj) { Get-Content $pj -Raw }
