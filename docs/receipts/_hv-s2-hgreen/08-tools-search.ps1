#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\08-tools-search.json'
$marker = Join-Path $main 'AGENTS-README-FIRST.yaml'
$keyLine = Select-String -LiteralPath $marker -Pattern '^apiKey:\s*(?<v>.+)$' | Select-Object -First 1
$apiKey = $keyLine.Matches[0].Groups['v'].Value.Trim()
$headers = @{ 'X-Api-Key' = $apiKey }
$uri = 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin'
try {
    $resp = Invoke-RestMethod -Uri $uri -Headers $headers -TimeoutSec 20
} catch {
    $resp = [ordered]@{ Error = $_.Exception.Message; Status = try { $_.Exception.Response.StatusCode.Value__ } catch { $null } }
}

$exact = @()
if ($resp -is [System.Array] -or $resp.PSObject.Properties.Name -contains 'Count') {
    $items = @($resp)
} elseif ($resp.PSObject.Properties.Name -contains 'tools') {
    $items = @($resp.tools)
} elseif ($resp.PSObject.Properties.Name -contains 'items') {
    $items = @($resp.items)
} else {
    $items = @($resp)
}
foreach ($t in $items) {
    $name = $null
    if ($t.PSObject.Properties.Name -contains 'name') { $name = [string]$t.name }
    elseif ($t.PSObject.Properties.Name -contains 'toolName') { $name = [string]$t.toolName }
    if ($name -eq 'mcpserver-grok-plugin') { $exact += $t }
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Uri = $uri
    ItemCount = @($items).Count
    ExactNameCount = $exact.Count
    ExactNames = @($exact | ForEach-Object { $_.name })
    Preview = @($items | Select-Object -First 5)
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} items={1} exact={2}" -f $out, $obj.ItemCount, $obj.ExactNameCount)
