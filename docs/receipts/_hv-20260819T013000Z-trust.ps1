$ErrorActionPreference = 'Stop'
. 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1'
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sig = Test-MarkerSignature -MarkerFile $marker
Write-Output ('MARKER_SIGNATURE=' + $sig)
$nonce = [guid]::NewGuid().ToString('N')
Write-Output ('NONCE_SENT=' + $nonce)
$healthUrl = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
try {
  $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 15
  Write-Output ('HEALTH_STATUS=' + [int]$resp.StatusCode)
  Write-Output ('HEALTH_BODY=' + $resp.Content)
  $echoOk = $resp.Content -match [regex]::Escape($nonce)
  Write-Output ('NONCE_ECHO_OK=' + $echoOk)
} catch {
  Write-Output ('HEALTH_ERROR=' + $_.Exception.Message)
  Write-Output 'MCP_UNTRUSTED'
}
