#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-20260819T000500Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Set-Location -LiteralPath $workspace

$drive = Get-PSDrive -Name F
$disk = [pscustomobject]@{
    UsedGB  = [math]::Round($drive.Used / 1GB, 2)
    FreeGB  = [math]::Round($drive.Free / 1GB, 2)
}
$disk | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'disk.json') -Encoding utf8
Write-Output ('DISK_FREE_GB=' + $disk.FreeGB)

. (Join-Path $workspace 'plugins\core\lib-ps\marker-resolver.ps1')
$sig = Test-MarkerSignature -MarkerFile (Join-Path $workspace 'AGENTS-README-FIRST.yaml')
Write-Output ('MARKER_SIGNATURE=' + $sig)

$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 16
$rng.GetBytes($bytes)
$nonce = [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
$uri = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
$resp = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 15
$health = $resp.Content | ConvertFrom-Json
$nonceOk = [string]$health.nonce -eq $nonce
$trust = [pscustomobject]@{
    timestampUtc        = [datetime]::UtcNow.ToString('o')
    signatureOk         = [bool]$sig
    nonce               = $nonce
    nonceOk             = $nonceOk
    healthStatusCode    = [int]$resp.StatusCode
    healthStatus        = [string]$health.status
    healthVersion       = [string]$health.version
    storage             = [string]$health.storage
    pluginJsonVersion   = (Get-Content -Raw 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' | ConvertFrom-Json).version
    pluginDotVersion    = (Get-Content -Raw 'F:\GitHub\mcpserver-grok-plugin\.version').Trim()
    markerPluginVersion = '1.93.0-not-authority'
    diskFreeGB          = $disk.FreeGB
}
$trust | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'trust.json') -Encoding utf8
Write-Output ('NONCE_OK=' + $nonceOk)
Write-Output ('HEALTH_STATUS=' + $health.status)
Write-Output ('HEALTH_VERSION=' + $health.version)
Write-Output ('STORAGE=' + $health.storage)
Write-Output ('PLUGIN_JSON_VERSION=' + $trust.pluginJsonVersion)

$mcpDir = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01756-32cb-72d0-886f-86e77fddbec1\mcp'
$testDump = Join-Path $mcpDir 'call-67649905-a884-4c0d-b736-c18d8f80185e-76.json'
$mapDump = Join-Path $mcpDir 'call-b1e5d6dd-5650-4f4e-9881-4ced40dd64a2-79.json'

$wantedTests = @(
    'TEST-MCP-TRIAGEERR-001',
    'TEST-MCP-TRIAGESCHEMA-001',
    'TEST-MCP-TRIAGESTORE-001',
    'TEST-MCP-TRIAGESTORE-002',
    'TEST-MCP-TRIAGESTORE-003',
    'TEST-MCP-TRIAGESTORE-004',
    'TEST-MCP-TRIAGESTORE-005',
    'TEST-MCP-TRIAGESTORE-006',
    'TEST-MCP-TRIAGESTORE-007',
    'TEST-MCP-TRIAGEPLUGIN-001',
    'TEST-MCP-TRIAGEPLUGIN-002',
    'TEST-MCP-TRIAGEPLUGIN-003',
    'TEST-MCP-TRIAGEPLUGIN-004',
    'TEST-MCP-TRIAGEPLUGIN-005',
    'TEST-MCP-TRIAGETODO-001',
    'TEST-MCP-TRIAGETODO-002',
    'TEST-MCP-TRIAGEREQ-001',
    'TEST-MCP-TRIAGEHELP-001'
)

$testDoc = Get-Content -LiteralPath $testDump -Raw | ConvertFrom-Json
$testRows = @()
foreach ($id in $wantedTests) {
    $item = @($testDoc.items | Where-Object { $_.Id -eq $id }) | Select-Object -First 1
    if ($null -eq $item) {
        $testRows += [pscustomobject]@{
            id = $id
            found = $false
            acCount = 0
            ac1Len = 0
            ac1Text = ''
            title = ''
        }
        continue
    }
    $ac1 = @($item.AcceptanceCriteria | Where-Object { $_.id -eq 'ac-1' }) | Select-Object -First 1
    $text = if ($ac1) { [string]$ac1.text } else { '' }
    $testRows += [pscustomobject]@{
        id = $id
        found = $true
        acCount = @($item.AcceptanceCriteria).Count
        ac1Len = $text.Length
        ac1Text = $text
        title = [string]$item.Title
        status = [string]$item.Status
    }
}
$testRows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'test-ac.json') -Encoding utf8
Write-Output 'TEST_AC_SUMMARY'
$testRows | ForEach-Object {
    Write-Output ($_.id + ' found=' + $_.found + ' acCount=' + $_.acCount + ' ac1Len=' + $_.ac1Len)
}

$wantedFr = @(
    'FR-MCP-TRIAGEERR-001',
    'FR-MCP-TRIAGESTORE-001',
    'FR-MCP-TRIAGESTORE-002',
    'FR-MCP-TRIAGESCHEMA-001',
    'FR-MCP-TRIAGEPLUGIN-001',
    'FR-MCP-TRIAGETODO-001',
    'FR-MCP-TRIAGEREQ-001',
    'FR-MCP-TRIAGEHELP-001'
)
$mapDoc = Get-Content -LiteralPath $mapDump -Raw | ConvertFrom-Json
$mapRows = @()
foreach ($id in $wantedFr) {
    $item = @($mapDoc.items | Where-Object { $_.FrId -eq $id }) | Select-Object -First 1
    if ($null -eq $item) {
        $mapRows += [pscustomobject]@{ frId = $id; found = $false; trIds = @(); testIds = @() }
        continue
    }
    $mapRows += [pscustomobject]@{
        frId = $id
        found = $true
        trIds = @($item.TrIds)
        testIds = @($item.TestIds)
    }
}
$mapRows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'mappings.json') -Encoding utf8
Write-Output 'MAPPING_SUMMARY'
$mapRows | ForEach-Object {
    Write-Output ($_.frId + ' found=' + $_.found + ' tr=' + (($_.trIds) -join ',') + ' tests=' + (($_.testIds) -join ','))
}

$truck = [pscustomobject]@{
    workspacePath = 'F:\GitHub\TruckMate'
    totalCount = 230
    invalidColumnName = $false
    agentHeaderFieldsPresent = $true
    observedFields = @('agentSessionId', 'agentSessionTranscriptFile', 'agentExecutablePath', 'agentExecutableVersion')
    sampleSessionId = 'ClaudeCode-20260818T231002Z-plugin-session'
    note = 'Independent mcpserver__sessionlog_query workspacePath=F:\GitHub\TruckMate returned totalCount=230 with AgentSession header fields populated. No Invalid column name.'
}
$truck | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'truckmate-query.json') -Encoding utf8

Write-Output 'TRUST_AND_STORE_DONE'
