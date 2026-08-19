$ErrorActionPreference = 'Stop'
$bytes = New-Object byte[] 16
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$nonce = [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
Write-Output "NONCE=$nonce"
$r = Invoke-WebRequest -Uri "http://PAYTON-LEGION2:7147/health?nonce=$nonce" -UseBasicParsing -TimeoutSec 15
Write-Output "STATUS=$($r.StatusCode)"
Write-Output $r.Content
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$sig = Test-MarkerSignature -MarkerPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
Write-Output "SIG=$sig"
Write-Output 'PLUGIN_VERSION_FILE='
Get-Content 'F:\GitHub\mcpserver-grok-plugin\.version' -ErrorAction SilentlyContinue
Write-Output 'PLUGIN_JSON='
Get-Content 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' -Raw -ErrorAction SilentlyContinue
