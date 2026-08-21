#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\09-reqs.json'
$marker = Join-Path $main 'AGENTS-README-FIRST.yaml'
$keyLine = Select-String -LiteralPath $marker -Pattern '^apiKey:\s*(?<v>.+)$' | Select-Object -First 1
$apiKey = $keyLine.Matches[0].Groups['v'].Value.Trim()
$headers = @{ 'X-Api-Key' = $apiKey }
$base = 'http://PAYTON-LEGION2:7147'

function Get-Req {
    param([string]$Type, [string]$Id)
    $uris = @(
        "$base/mcpserver/requirements/$Type/$Id"
        "$base/mcpserver/requirements?type=$Type&id=$Id"
        "$base/mcpserver/requirements/$Id"
    )
    foreach ($uri in $uris) {
        try {
            $resp = Invoke-WebRequest -Uri $uri -Headers $headers -TimeoutSec 20 -SkipHttpErrorCheck
            return [ordered]@{
                Uri = $uri
                Status = [int]$resp.StatusCode
                Body = $resp.Content
            }
        } catch {
            # continue
        }
    }
    return [ordered]@{ Uri = $uris[0]; Status = $null; Body = 'all-uris-failed' }
}

$ids = [ordered]@{
    fr = @('FR-MCP-STRICTCOUNT-001','FR-MCP-FAILSAFE-001','FR-MCP-SESSIONEND-001','FR-MCP-XAGENT-001','FR-MCP-VERIFYWRAP-001','FR-MCP-TRIAGEPLUGIN-001','FR-MCP-TRIAGEERR-001','FR-MCP-TRIAGE-002')
    tr = @('TR-MCP-STRICTCOUNT-001','TR-MCP-FAILSAFE-001','TR-MCP-SESSIONEND-001','TR-MCP-XAGENT-001','TR-MCP-VERIFYWRAP-001','TR-MCP-TRIAGEPLUGIN-001','TR-MCP-TRIAGEPLUGIN-004','TR-MCP-TRIAGE-004')
    test = @('TEST-MCP-STRICTCOUNT-001','TEST-MCP-FAILSAFE-001','TEST-MCP-SESSIONEND-001','TEST-MCP-XAGENT-001','TEST-MCP-VERIFYWRAP-001','TEST-MCP-TRIAGEPLUGIN-001','TEST-MCP-TRIAGEPLUGIN-004')
}

$list = [ordered]@{}
foreach ($type in @('fr','tr','test','mapping')) {
    $uri = "$base/mcpserver/requirements?type=$type"
    try {
        $resp = Invoke-WebRequest -Uri $uri -Headers $headers -TimeoutSec 60 -SkipHttpErrorCheck
        $list[$type] = [ordered]@{ Status = [int]$resp.StatusCode; Length = $resp.Content.Length; BodyPreview = $resp.Content.Substring(0, [Math]::Min(4000, $resp.Content.Length)) }
        $path = "F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\09-list-$type.json"
        Set-Content -LiteralPath $path -Value $resp.Content -Encoding utf8
    } catch {
        $list[$type] = [ordered]@{ Error = $_.Exception.Message }
    }
}

$hits = [ordered]@{}
foreach ($k in $ids.Keys) {
    $hits[$k] = @()
    foreach ($id in $ids[$k]) {
        $hits[$k] += Get-Req -Type $k -Id $id
    }
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Lists = $list
    Gets = $hits
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0}" -f $out)
