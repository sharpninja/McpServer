#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$workspace = 'F:\GitHub\McpServer'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$utcStamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utcStamp-h2-green-products"
$requestId = "req-$utcStamp-001-hostile-h2-green-products"
Write-Output ("UTC_STAMP=" + $utcStamp)
Write-Output ("SESSION_ID=" + $sessionId)
Write-Output ("REQUEST_ID=" + $requestId)

Write-Output '=== MARKER_SIGNATURE ==='
$sigOk = Test-MarkerSignature -MarkerFile $marker
Write-Output ("Test-MarkerSignature=" + $sigOk)

Write-Output '=== HEALTH_NONCE ==='
$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-WebRequest -Uri "$baseUrl/health?nonce=$nonce" -UseBasicParsing -TimeoutSec 10
$healthJson = $health.Content | ConvertFrom-Json
Write-Output ("HealthStatus=" + [int]$health.StatusCode)
Write-Output ("HealthNonceSent=" + $nonce)
Write-Output ("HealthNonceEcho=" + $healthJson.nonce)
Write-Output ("HealthNonceMatch=" + ($healthJson.nonce -eq $nonce))
Write-Output ("HealthStatusText=" + $healthJson.status)
Write-Output ("HealthVersion=" + $healthJson.version)
Write-Output ("HealthStorage=" + $healthJson.storage)
Write-Output ("FULL_BOOTSTRAP=" + ($sigOk -and ($healthJson.nonce -eq $nonce) -and ([int]$health.StatusCode -eq 200)))

$script:McpSessionHeader = $null
$script:McpId = 0

function Invoke-McpRpc {
    param(
        [Parameter(Mandatory)][string]$Method,
        $Params = $null,
        [string]$Label = $Method
    )
    $script:McpId++
    $payload = [ordered]@{
        jsonrpc = '2.0'
        id = $script:McpId
        method = $Method
    }
    if ($null -ne $Params) {
        $payload['params'] = $Params
    }
    $json = $payload | ConvertTo-Json -Depth 20 -Compress
    Write-Output ("---- MCP {0} id={1} ----" -f $Label, $script:McpId)
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/mcp-transport")
    $req.Headers.Accept.Clear()
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('application/json'))
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('text/event-stream'))
    [void]$req.Headers.TryAddWithoutValidation('X-Workspace-Path', $workspace)
    if ($script:McpSessionHeader) {
        [void]$req.Headers.TryAddWithoutValidation('Mcp-Session-Id', $script:McpSessionHeader)
    }
    $req.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, 'application/json')
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(180)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) {
            $script:McpSessionHeader = @($sid)[0]
        }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Write-Output ("HTTP=" + [int]$resp.StatusCode)
        Write-Output ("Mcp-Session-Id=" + $script:McpSessionHeader)
        if ($body.StartsWith('event:') -or $body.Contains("`ndata:")) {
            $dataLines = @()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) {
                    $dataLines += $trim.Substring(5).Trim()
                }
            }
            $body = ($dataLines -join "`n")
        }
        Write-Output $body
        return $body
    } finally {
        $client.Dispose()
        $req.Dispose()
    }
}

function Invoke-McpTool {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][hashtable]$Arguments
    )
    Invoke-McpRpc -Method 'tools/call' -Label $Name -Params @{
        name = $Name
        arguments = $Arguments
    }
}

$null = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-h2-green'; version = '1.0.0' }
}

Invoke-McpRpc -Method 'notifications/initialized' -Params @{} | Out-Null

Write-Output '=== TOOLS_LIST_COUNT ==='
$toolsBody = Invoke-McpRpc -Method 'tools/list' -Params @{}
try {
    $toolsObj = $toolsBody | ConvertFrom-Json
    $names = @($toolsObj.result.tools | ForEach-Object { $_.name } | Sort-Object -Unique)
    Write-Output ("TOOLS_UNIQUE=" + $names.Count)
    Write-Output ("HAS_SESSIONLOG_OPEN=" + ($names -contains 'sessionlog_open'))
    Write-Output ("HAS_TODO_GET=" + ($names -contains 'todo_get'))
    Write-Output ("HAS_REQUIREMENTS_LIST=" + ($names -contains 'requirements_list'))
} catch {
    Write-Output ("TOOLS_LIST_PARSE_ERROR=" + $_.Exception.Message)
}

Write-Output '=== SESSIONLOG_OPEN ==='
Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H2-green products share review'
    model = 'grok'
} | Out-Null

Write-Output '=== SESSIONLOG_BEGIN_TURN ==='
Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    queryTitle = 'Hostile H2-green products share review'
    queryText = 'Hostile validator H2-green: attack Phase 2 share implementation claims for MCP-PRODUCTS-001. Not claiming TODO done, Phase 3-5, or full unit suite.'
} | Out-Null

Write-Output '=== TODO_GET MCP-PRODUCTS-001 ==='
Invoke-McpTool -Name 'todo_get' -Arguments @{
    id = 'MCP-PRODUCTS-001'
    workspacePath = $workspace
} | Out-Null

Write-Output '=== REQUIREMENTS_LIST FR ==='
$frBody = Invoke-McpTool -Name 'requirements_list' -Arguments @{
    workspacePath = $workspace
    type = 'fr'
}
Write-Output '=== REQUIREMENTS_LIST TR ==='
$trBody = Invoke-McpTool -Name 'requirements_list' -Arguments @{
    workspacePath = $workspace
    type = 'tr'
}
Write-Output '=== REQUIREMENTS_LIST TEST ==='
$testBody = Invoke-McpTool -Name 'requirements_list' -Arguments @{
    workspacePath = $workspace
    type = 'test'
}
Write-Output '=== REQUIREMENTS_LIST MAPPING ==='
$mapBody = Invoke-McpTool -Name 'requirements_list' -Arguments @{
    workspacePath = $workspace
    type = 'mapping'
}

function Get-ProductEntries {
    param($Body, [string]$Kind)
    try {
        $obj = $Body | ConvertFrom-Json
        $text = $obj.result.content[0].text
        $parsed = $text | ConvertFrom-Json
        Write-Output ("${Kind}_TOTAL=" + $parsed.Count)
        foreach ($item in $parsed) {
            $id = $item.id
            if ($null -eq $id) { $id = $item.Id }
            if ($null -eq $id) { $id = $item.frId }
            if ($null -eq $id) { $id = $item.FrId }
            $idText = [string]$id
            if ($idText -match 'PRODUCT') {
                $acs = $item.acceptanceCriteria
                if ($null -eq $acs) { $acs = $item.AcceptanceCriteria }
                $acCount = 0
                if ($null -ne $acs) { $acCount = @($acs).Count }
                Write-Output ("${Kind}_HIT id=$idText status=$($item.status) isSatisfied=$($item.isSatisfied) acCount=$acCount")
                if ($null -ne $acs) {
                    foreach ($ac in @($acs)) {
                        Write-Output ("  AC id=$($ac.id) isSatisfied=$($ac.isSatisfied) text=$($ac.text)")
                    }
                }
            }
        }
    } catch {
        Write-Output ("${Kind}_PARSE_ERROR=" + $_.Exception.Message)
    }
}

Write-Output '=== PRODUCT_REQUIREMENT_HITS ==='
Get-ProductEntries -Body $frBody -Kind 'FR'
Get-ProductEntries -Body $trBody -Kind 'TR'
Get-ProductEntries -Body $testBody -Kind 'TEST'
Get-ProductEntries -Body $mapBody -Kind 'MAP'

Write-Output '=== FILE_INVENTORY ==='
$paths = @(
    'src\McpServer.Support.Mcp\Products\ProductShareHelper.cs',
    'src\McpServer.Support.Mcp\Products\Queries\GetProductEffectiveRequirementsQuery.cs',
    'src\McpServer.Support.Mcp\Products\ProductServiceCollectionExtensions.cs',
    'src\McpServer.Support.Mcp\Products\ProductCqrsHelpers.cs',
    'tests\McpServer.Support.Mcp.Tests\Products\GetProductEffectiveRequirementsQueryHandlerTests.cs',
    'src\McpServer.Services\Requirements\Models\RequirementsModels.cs',
    'src\McpServer.Services\Requirements\RequirementsDatabaseDocumentService.cs',
    'src\McpServer.Support.Mcp\Controllers\RequirementsController.cs',
    'src\McpServer.Client\Models\RequirementsModels.cs',
    'docs\plans\mcp-products-001.md'
)
foreach ($rel in $paths) {
    $full = Join-Path $workspace $rel
    if (Test-Path -LiteralPath $full) {
        $item = Get-Item -LiteralPath $full
        Write-Output ("EXISTS " + $rel + " LastWriteUtc=" + $item.LastWriteTimeUtc.ToString('o') + " Length=" + $item.Length)
    } else {
        Write-Output ("ABSENT " + $rel)
    }
}

$absent = @(
    'src\McpServer.Support.Mcp\Controllers\ProductsController.cs',
    'src\McpServer.Client\ProductClient.cs',
    'tests\McpServer.Support.Mcp.Tests\Controllers\ProductsControllerTests.cs',
    'tests\McpServer.Client.Tests\ProductClientTests.cs',
    'tests\McpServer.Support.Mcp.Tests\Products\ProductRequirementContextTests.cs',
    'src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.Products.cs',
    'src\McpServer.McpStdio\FwhMcpTools.Products.cs'
)
foreach ($rel in $absent) {
    $full = Join-Path $workspace $rel
    Write-Output ("PHASE35 " + $rel + " EXISTS=" + (Test-Path -LiteralPath $full))
}

Write-Output '=== PLAN_HASH ==='
Get-FileHash -LiteralPath (Join-Path $workspace 'docs\plans\mcp-products-001.md') -Algorithm SHA256 | ForEach-Object { Write-Output ("PLAN_SHA256=" + $_.Hash) }

$sessionPlanCandidates = @(
    'C:\Users\kingd\.grok\projects\F-GitHub-McpServer\plans\plan.md',
    'C:\Users\kingd\.claude\plans\mcp-products-001.md'
)
Get-ChildItem -Path 'C:\Users\kingd\.grok' -Filter 'plan.md' -Recurse -ErrorAction SilentlyContinue |
    Select-Object -First 8 FullName, LastWriteTimeUtc |
    ForEach-Object { Write-Output ("SESSION_PLAN_CANDIDATE " + $_.FullName + " " + $_.LastWriteTimeUtc.ToString('o')) }

Write-Output '=== GREP_IPRODUCTSERVICE ==='
$csHits = Get-ChildItem -Path $workspace -Recurse -Filter '*.cs' -File |
    Select-String -Pattern 'IProductService' -SimpleMatch
Write-Output ("IPRODUCTSERVICE_CS_HITS=" + @($csHits).Count)
$csHits | ForEach-Object { Write-Output ("  " + $_.Path + ":" + $_.LineNumber + ":" + $_.Line.Trim()) }

Write-Output '=== GREP_PRODUCTSHAREHELPER ==='
Get-ChildItem -Path $workspace -Recurse -Filter '*.cs' -File |
    Select-String -Pattern 'ProductShareHelper' -SimpleMatch |
    ForEach-Object { Write-Output ("  " + $_.Path + ":" + $_.LineNumber + ":" + $_.Line.Trim()) }

Write-Output '=== CLIENT_PRODUCTKEYS ==='
$clientModel = Join-Path $workspace 'src\McpServer.Client\Models\RequirementsModels.cs'
$pkHits = Select-String -LiteralPath $clientModel -Pattern 'ProductKeys'
Write-Output ("CLIENT_PRODUCTKEYS_HITS=" + @($pkHits).Count)

Write-Output ("MCP_SESSION_HEADER=" + $script:McpSessionHeader)
Write-Output 'MCP_PHASE1_DONE'
Write-Output ("SESSION_ID_FINAL=" + $sessionId)
Write-Output ("REQUEST_ID_FINAL=" + $requestId)
