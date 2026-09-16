#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$baseUrl = Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl'
$apiKey = Get-MarkerField -MarkerFile $marker -FieldName 'apiKey'
$headers = @{ 'X-Api-Key' = $apiKey }
$files = @(
    'F:\GitHub\McpServer\docs\receipts\_remaining-submit\ir1.json',
    'F:\GitHub\McpServer\docs\receipts\_remaining-submit\ir2.json'
)
$rows = foreach ($f in $files) {
    $body = [IO.File]::ReadAllText($f)
    $sw = [Diagnostics.Stopwatch]::StartNew()
    try {
        $resp = Invoke-RestMethod -Uri "$baseUrl/mcpserver/sessionlog" -Headers $headers -Method Post -ContentType 'application/json; charset=utf-8' -Body $body -TimeoutSec 120
        $ok = $true
        $head = ($resp | ConvertTo-Json -Compress -Depth 4)
    } catch {
        $ok = $false
        $head = $_.Exception.Message
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $head = $_.ErrorDetails.Message }
    }
    if ($head.Length -gt 400) { $head = $head.Substring(0, 400) }
    [pscustomobject]@{
        file = [IO.Path]::GetFileName($f)
        success = $ok
        elapsedSec = [int]$sw.Elapsed.TotalSeconds
        head = $head
    }
}
$receipt = 'F:\GitHub\McpServer\docs\receipts\_remaining-submit\importrecovery-rest.json'
[IO.File]::WriteAllText($receipt, ($rows | ConvertTo-Json -Depth 6))
$rows | ConvertTo-Json -Compress
