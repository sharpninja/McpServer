$ErrorActionPreference = 'Continue'
Write-Output '=== grok session-state lastwrite ==='
Get-Item 'F:\GitHub\McpServer\.mcpServer\grok\session-state.yaml','F:\GitHub\McpServer\.mcpServer\grok\current-turn.yaml' |
    ForEach-Object { '{0:o}  {1}  {2}b' -f $_.LastWriteTimeUtc, $_.Name, $_.Length }

Write-Output '=== search grok transcripts for HMACSHA256 last 2h ==='
$cutoff = [DateTime]::UtcNow.AddHours(-3)
$roots = @(
    'C:\Users\kingd\.grok',
    'C:\Users\kingd\.local\share\grok',
    'F:\GitHub\McpServer\.mcpServer\grok'
)
foreach ($r in $roots) {
    if (-not (Test-Path $r)) { Write-Output "missing $r"; continue }
    Write-Output "ROOT $r"
    Get-ChildItem -LiteralPath $r -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -gt $cutoff } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 40 |
        ForEach-Object { '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.FullName }
}

Write-Output '=== grep HMACSHA256 in grok failsafe last files ==='
Select-String -Path 'F:\GitHub\McpServer\.mcpServer\grok\failsafe\20260821T*.yaml' -Pattern 'HMACSHA256|Test-MarkerSignature|homemade|false-negative|computed' -ErrorAction SilentlyContinue |
    Select-Object -First 40 |
    ForEach-Object { '{0}:{1}:{2}' -f $_.Filename, $_.LineNumber, $_.Line.Trim() }
