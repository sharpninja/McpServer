# Live MCP + sibling vendor package.json collector. Read-only GETs.
$ErrorActionPreference = 'Stop'
$markerPath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-g11-out'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Parse key fields from marker without YAML module: line-oriented for known keys only.
$marker = Get-Content -LiteralPath $markerPath
$apiKey = ($marker | Where-Object { $_ -match '^apiKey:\s' }) -replace '^apiKey:\s+', ''
$baseUrl = ($marker | Where-Object { $_ -match '^baseUrl:\s' }) -replace '^baseUrl:\s+', ''
$port = ($marker | Where-Object { $_ -match '^port:\s' }) -replace '^port:\s+', ''

$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -Method Get -TimeoutSec 30

$headers = @{ 'X-Api-Key' = $apiKey; 'X-Workspace-Path' = 'F:\GitHub\McpServer' }
function Get-Req([string]$path) {
    try {
        return Invoke-RestMethod -Uri "$baseUrl$path" -Headers $headers -Method Get -TimeoutSec 60
    } catch {
        return [ordered]@{ error = $_.Exception.Message; status = $_.Exception.Response.StatusCode.value__ }
    }
}

$ids = [ordered]@{
    Health = [ordered]@{ RequestNonce = $nonce; Echo = $health; NonceMatch = ($health.ToString() -match $nonce -or ($health.nonce -eq $nonce) -or ($health.Nonce -eq $nonce)) }
    HealthRawType = $health.GetType().FullName
    TR_SYNC = Get-Req '/mcpserver/requirements/tr/TR-MCP-SYNC-001'
    TEST_194 = Get-Req '/mcpserver/requirements/test/TEST-MCP-194'
    FR_143 = Get-Req '/mcpserver/requirements/fr/FR-MCP-143'
    MAP_143 = Get-Req '/mcpserver/requirements/mapping/FR-MCP-143'
    FR_TRIAGE_002 = Get-Req '/mcpserver/requirements/fr/FR-MCP-TRIAGE-002'
    TR_TRIAGE_004 = Get-Req '/mcpserver/requirements/tr/TR-MCP-TRIAGE-004'
}

# Serialize health object too
$ids.HealthObject = $health

$consumer = @()
foreach ($repo in @('mcpserver-cline-plugin','mcpserver-cline-v2-plugin','mcpserver-opencode-plugin')) {
    $pj = "F:\GitHub\$repo\package.json"
    $obj = $null
    $deps = $null
    if (Test-Path -LiteralPath $pj) {
        $obj = Get-Content -LiteralPath $pj -Raw | ConvertFrom-Json
        $deps = $obj.dependencies
    }
    $coreDep = $null
    if ($null -ne $deps) {
        $coreDep = $deps.'@sharpninja/mcpserver-plugin-core'
    }
    $consumer += [ordered]@{
        Repo = $repo
        PackageJson = $pj
        Exists = (Test-Path -LiteralPath $pj)
        CoreDep = $coreDep
    }
}
$ids.ConsumerPackageJson = $consumer

($ids | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath (Join-Path $outDir 'mcp-reqs.json') -Encoding utf8
Write-Output ('BASEURL=' + $baseUrl)
Write-Output ('NONCE=' + $nonce)
Write-Output ('HEALTH_TYPE=' + $health.GetType().FullName)
Write-Output ($health | ConvertTo-Json -Depth 5 -Compress)
Write-Output '---CONSUMERS---'
$ids.ConsumerPackageJson | ConvertTo-Json -Depth 5
Write-Output '---TR-MCP-SYNC-001---'
$ids.TR_SYNC | ConvertTo-Json -Depth 6 -Compress
Write-Output '---TEST-MCP-194---'
$ids.TEST_194 | ConvertTo-Json -Depth 6 -Compress
Write-Output '---MAP-143---'
$ids.MAP_143 | ConvertTo-Json -Depth 6 -Compress
