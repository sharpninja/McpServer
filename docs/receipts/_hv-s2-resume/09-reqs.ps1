#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$main = 'F:\GitHub\McpServer'
$marker = Join-Path $main 'AGENTS-README-FIRST.yaml'
$keyLine = Select-String -LiteralPath $marker -Pattern '^apiKey:\s*(?<v>.+)$' | Select-Object -First 1
$apiKey = $keyLine.Matches[0].Groups['v'].Value.Trim()
$headers = @{ 'X-Api-Key' = $apiKey }
$ids = @(
    'FR-MCP-STRICTCOUNT-001',
    'FR-MCP-FAILSAFE-001',
    'FR-MCP-SESSIONEND-001',
    'FR-MCP-XAGENT-001',
    'FR-MCP-VERIFYWRAP-001',
    'FR-MCP-TRIAGEPLUGIN-001',
    'TEST-MCP-STRICTCOUNT-001',
    'TEST-MCP-FAILSAFE-001',
    'TEST-MCP-SESSIONEND-001',
    'TEST-MCP-XAGENT-001',
    'TEST-MCP-VERIFYWRAP-001',
    'TEST-MCP-TRIAGEPLUGIN-004'
)

function Get-ReqType {
    param([string]$Type)
    $uri = "http://PAYTON-LEGION2:7147/mcpserver/requirements?type=$Type"
    try {
        return Invoke-RestMethod -Uri $uri -Headers $headers -TimeoutSec 60
    } catch {
        return [ordered]@{ Error = $_.Exception.Message }
    }
}

$fr = Get-ReqType -Type 'fr'
$tr = Get-ReqType -Type 'tr'
$test = Get-ReqType -Type 'test'
$map = Get-ReqType -Type 'mapping'

function Filter-Items {
    param($Resp, [string[]]$Want)
    $items = @()
    if ($null -eq $Resp) { return @() }
    if ($Resp -is [System.Array]) { $items = @($Resp) }
    elseif ($Resp.PSObject.Properties.Name -contains 'items') { $items = @($Resp.items) }
    elseif ($Resp.PSObject.Properties.Name -contains 'requirements') { $items = @($Resp.requirements) }
    else { $items = @($Resp) }
    $out = @()
    foreach ($it in $items) {
        $id = $null
        foreach ($n in @('id','requirementId','frId','trId','testId')) {
            if ($it.PSObject.Properties.Name -contains $n) { $id = [string]$it.$n; if ($id) { break } }
        }
        if ($id -and ($Want -contains $id -or ($Want | Where-Object { $id -like "*$_*" }))) {
            $ac = @()
            if ($it.PSObject.Properties.Name -contains 'acceptanceCriteria') { $ac = @($it.acceptanceCriteria) }
            $out += [ordered]@{
                Id = $id
                Title = $(if ($it.PSObject.Properties.Name -contains 'title') { [string]$it.title } else { $null })
                Status = $(if ($it.PSObject.Properties.Name -contains 'status') { [string]$it.status } else { $null })
                IsSatisfied = $(if ($it.PSObject.Properties.Name -contains 'isSatisfied') { $it.isSatisfied } else { $null })
                AcceptanceCriteriaCount = @($ac).Count
                AcceptanceCriteria = @($ac | ForEach-Object {
                    if ($_ -is [string]) { $_ }
                    elseif ($_.PSObject.Properties.Name -contains 'text') { [string]$_.text }
                    else { [string]$_ }
                })
            }
        }
    }
    return $out
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    FrHits = @(Filter-Items -Resp $fr -Want $ids)
    TrHits = @(Filter-Items -Resp $tr -Want @(
        'TR-MCP-STRICTCOUNT-001','TR-MCP-FAILSAFE-001','TR-MCP-SESSIONEND-001','TR-MCP-XAGENT-001','TR-MCP-VERIFYWRAP-001','TR-MCP-TRIAGEPLUGIN-001','TR-MCP-TRIAGEPLUGIN-004'
    ))
    TestHits = @(Filter-Items -Resp $test -Want $ids)
}
$obj | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outDir '09-reqs.json') -Encoding utf8
Write-Output ("WROTE reqs fr={0} tr={1} test={2}" -f @($obj.FrHits).Count, @($obj.TrHits).Count, @($obj.TestHits).Count)
