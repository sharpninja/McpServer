#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-234800Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$script:McpSessionHeader = $null
$script:McpId = 0
$headerPath = Join-Path $outDir 'mcp-session-header.txt'
if (Test-Path -LiteralPath $headerPath) {
    $script:McpSessionHeader = (Get-Content -LiteralPath $headerPath -Raw).Trim()
}

function Invoke-McpRpc {
    param(
        [Parameter(Mandatory)][string]$Method,
        $Params = $null
    )
    $script:McpId++
    $payload = [ordered]@{
        jsonrpc = '2.0'
        id = $script:McpId
        method = $Method
    }
    if ($null -ne $Params) { $payload['params'] = $Params }
    $json = $payload | ConvertTo-Json -Depth 20 -Compress
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
        if ($body.StartsWith('event:') -or $body.Contains("`ndata:")) {
            $dataLines = [System.Collections.Generic.List[string]]::new()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) {
                    [void]$dataLines.Add($trim.Substring(5).Trim())
                }
            }
            $body = [string]::Join("`n", $dataLines)
        }
        return [pscustomobject]@{ Status = [int]$resp.StatusCode; Body = $body }
    }
    finally {
        $client.Dispose()
        $req.Dispose()
    }
}

function Invoke-McpTool {
    param([string]$Name, [hashtable]$Arguments)
    Invoke-McpRpc -Method 'tools/call' -Params @{ name = $Name; arguments = $Arguments }
}

function Save-Body {
    param([string]$Name, $Result)
    $path = Join-Path $outDir $Name
    $Result.Body | Set-Content -LiteralPath $path -Encoding utf8
    Write-Output ('SAVED ' + $Name + ' HTTP=' + $Result.Status + ' LEN=' + $Result.Body.Length)
}

function Get-ToolObject {
    param($Result)
    $outer = $Result.Body | ConvertFrom-Json
    if ($outer.PSObject.Properties.Name -contains 'error' -and $null -ne $outer.error) {
        throw ('MCP RPC error: ' + ($outer.error | ConvertTo-Json -Compress -Depth 8))
    }
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-hgreen-234800-store'; version = '1.0.0' }
}
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})
Write-Output ('INIT_HTTP=' + $init.Status)

$todoIds = @(
    'PLAN-TRIAGECLUSTER-001',
    'BUG-TRIAGE-110','BUG-TRIAGE-111','BUG-TRIAGE-112','BUG-TRIAGE-114','BUG-TRIAGE-115',
    'BUG-TRIAGE-119','BUG-TRIAGE-123','BUG-TRIAGE-124','BUG-TRIAGE-126','BUG-TRIAGE-128',
    'BUG-TRIAGE-131','BUG-TRIAGE-132','BUG-TRIAGE-139','BUG-TRIAGE-143','BUG-TRIAGE-148','BUG-TRIAGE-149'
)
$todoRows = @()
foreach ($id in $todoIds) {
    $res = Invoke-McpTool -Name 'todo_get' -Arguments @{ id = $id; workspacePath = $workspace }
    Save-Body -Name ('todo-' + $id + '.json') -Result $res
    $obj = Get-ToolObject -Result $res
    $todoRows += [pscustomobject]@{
        id = [string]$obj.id
        done = [bool]$obj.done
        completedDate = [string]$obj.completedDate
    }
    Write-Output ('TODO ' + $obj.id + ' done=' + $obj.done)
}
$todoRows | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outDir 'todos.json') -Encoding utf8

foreach ($kind in @('fr', 'tr', 'test', 'mapping')) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $kind
    }
    Save-Body -Name ('req-' + $kind + '.json') -Result $res
}

$testObj = Get-ToolObject -Result (Invoke-McpTool -Name 'requirements_list' -Arguments @{ workspacePath = $workspace; type = 'test' })
$items = @()
foreach ($name in @('items', 'Items', 'requirements', 'Requirements')) {
    if ($testObj.PSObject.Properties.Name -contains $name -and $null -ne $testObj.$name) {
        $items = @($testObj.$name)
        break
    }
}
if ($items.Count -eq 0 -and $testObj -is [System.Array]) { $items = @($testObj) }

$wanted = @(
    'TEST-MCP-TRIAGEERR-001','TEST-MCP-TRIAGESCHEMA-001',
    'TEST-MCP-TRIAGESTORE-001','TEST-MCP-TRIAGESTORE-002','TEST-MCP-TRIAGESTORE-003',
    'TEST-MCP-TRIAGESTORE-004','TEST-MCP-TRIAGESTORE-005','TEST-MCP-TRIAGESTORE-006','TEST-MCP-TRIAGESTORE-007',
    'TEST-MCP-TRIAGEPLUGIN-001','TEST-MCP-TRIAGEPLUGIN-002','TEST-MCP-TRIAGEPLUGIN-003','TEST-MCP-TRIAGEPLUGIN-004','TEST-MCP-TRIAGEPLUGIN-005',
    'TEST-MCP-TRIAGETODO-001','TEST-MCP-TRIAGETODO-002','TEST-MCP-TRIAGEREQ-001','TEST-MCP-TRIAGEHELP-001'
)
$acRows = @()
foreach ($id in $wanted) {
    $hit = $items | Where-Object {
        $cid = if ($_.PSObject.Properties.Name -contains 'id') { $_.id } elseif ($_.PSObject.Properties.Name -contains 'Id') { $_.Id } else { $null }
        $cid -eq $id
    } | Select-Object -First 1
    if (-not $hit) {
        $acRows += [pscustomobject]@{ id = $id; found = $false; acCount = 0; ac1Len = 0; status = $null }
        Write-Output ('TEST_MISSING ' + $id)
        continue
    }
    $acs = @()
    foreach ($n in @('acceptanceCriteria', 'AcceptanceCriteria')) {
        if ($hit.PSObject.Properties.Name -contains $n -and $null -ne $hit.$n) { $acs = @($hit.$n); break }
    }
    $ac1 = if ($acs.Count -gt 0) { [string]($acs[0].text) } else { '' }
    $acRows += [pscustomobject]@{
        id = $id
        found = $true
        acCount = $acs.Count
        ac1Len = $ac1.Length
        status = [string]$hit.status
        isSatisfied = [string]$hit.isSatisfied
        ac1 = $ac1
    }
    Write-Output ('TEST ' + $id + ' found=true acCount=' + $acs.Count + ' ac1Len=' + $ac1.Length)
}
$acRows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'test-ac.json') -Encoding utf8

if ($script:McpSessionHeader) {
    Set-Content -LiteralPath $headerPath -Value $script:McpSessionHeader -Encoding utf8
}
Write-Output 'STORE_DONE'
