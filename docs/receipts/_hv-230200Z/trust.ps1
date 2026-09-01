Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1'
$sig = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
Write-Output ('MARKER_SIGNATURE=' + $sig)
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 16
$rng.GetBytes($bytes)
$nonce = [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
Write-Output ('NONCE=' + $nonce)
$uri = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
$resp = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 15
Write-Output ('HEALTH_STATUS_CODE=' + [int]$resp.StatusCode)
Write-Output ('HEALTH_BODY=' + $resp.Content)
Write-Output ('PLUGIN_JSON_VERSION=' + (Get-Content -Raw 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' | ConvertFrom-Json).version)
Write-Output ('PLUGIN_DOT_VERSION=' + (Get-Content -Raw 'F:\GitHub\mcpserver-grok-plugin\.version').Trim())
