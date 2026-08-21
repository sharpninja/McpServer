#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$ok = Test-MarkerSignature -MarkerFile $marker
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    MarkerPath = $marker
    SignatureValid = [bool]$ok
} | ConvertTo-Json
