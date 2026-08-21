$ErrorActionPreference = 'Continue'
$root = 'F:\GitHub\McpServer\docs\receipts'
Write-Output '=== newest hostile-validator receipts ==='
Get-ChildItem -LiteralPath $root -Filter 'hostile-validator-*.md' |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 15 |
    ForEach-Object { '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.Name }

Write-Output '=== newest json receipts ==='
Get-ChildItem -LiteralPath $root -Filter 'hostile-validator-*.json' |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 10 |
    ForEach-Object { '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.Name }

Write-Output '=== grok session-state ==='
$state = 'F:\GitHub\McpServer\.mcpServer\grok\cache\session-state.yaml'
$turn = 'F:\GitHub\McpServer\.mcpServer\grok\cache\current-turn.yaml'
if (Test-Path $state) { Get-Item $state | ForEach-Object { '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.FullName } }
if (Test-Path $turn) { Get-Item $turn | ForEach-Object { '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.FullName } }

Write-Output '=== files modified last 90 minutes under receipts mentioning HMAC/plugin ==='
$cutoff = [DateTime]::UtcNow.AddMinutes(-90)
Get-ChildItem -LiteralPath $root -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTimeUtc -gt $cutoff -and $_.Extension -in '.md','.json','.txt','.ps1' } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 40 |
    ForEach-Object { '{0:o}  {1}' -f $_.LastWriteTimeUtc, $_.FullName }
