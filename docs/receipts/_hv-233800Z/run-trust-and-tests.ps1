$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer'
New-Item -ItemType Directory -Force -Path 'F:\GitHub\McpServer\docs\receipts\_hv-233800Z' | Out-Null

. '.\plugins\core\lib-ps\marker-resolver.ps1'
$sig = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
Write-Output ("Test-MarkerSignature=" + $sig)

$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 16
$rng.GetBytes($bytes)
$nonce = [System.BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
Write-Output ("nonce=" + $nonce)

$marker = Get-Content 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$apiKey = ($marker | Select-String '^apiKey:\s*(.+)$').Matches[0].Groups[1].Value
$uri = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
$resp = Invoke-RestMethod -Uri $uri -Headers @{ 'X-Api-Key' = $apiKey } -TimeoutSec 30
$resp | ConvertTo-Json -Depth 8
Write-Output ("nonceMatch=" + ($resp.nonce -eq $nonce))
Write-Output ("status=" + $resp.status)
Write-Output ("version=" + $resp.version)
if ($resp.storage) { Write-Output ("storage=" + ($resp.storage | ConvertTo-Json -Compress)) }

dotnet test 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj' -c Debug --filter 'FullyQualifiedName~McpToolErrorEnvelopeTests' --nologo --verbosity normal | Tee-Object -FilePath 'F:\GitHub\McpServer\docs\receipts\_hv-233800Z\tool-envelope.log'
Write-Output ("TOOL_ENVELOPE_EXIT=" + $LASTEXITCODE)
