$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer'
. '.\plugins\core\lib-ps\marker-resolver.ps1'
$result = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$out = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('o')
    TestMarkerSignature = [bool]$result
}
($out | ConvertTo-Json) | Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-g11-out\marker-sig-plugin.json' -Encoding utf8
Write-Output ($out | ConvertTo-Json)
Write-Output ('UTCNOW=' + [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
