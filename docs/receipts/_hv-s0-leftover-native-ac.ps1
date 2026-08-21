#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s0-leftover-verify'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Get-Prop {
    param($Obj, [string]$Name)
    if ($null -eq $Obj) { return $null }
    $prop = $Obj.PSObject.Properties[$Name]
    if ($null -eq $prop) { return $null }
    return $prop.Value
}

$script:McpSessionHeader = $null
$script:McpId = 0

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

function Get-ToolObject {
    param($Result)
    $raw = $Result.Body
    $json = $raw | ConvertFrom-Json
    $content = Get-Prop (Get-Prop $json 'result') 'content'
    if ($content) {
        $text = Get-Prop $content[0] 'text'
        if ($text) { return ($text | ConvertFrom-Json) }
    }
    return $json
}

function Get-Items {
    param($Parsed)
    $items = Get-Prop $Parsed 'items'
    if ($null -eq $items) { $items = Get-Prop $Parsed 'Items' }
    if ($null -eq $items) { return @() }
    return @($items)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2024-11-05'
    capabilities = @{}
    clientInfo = @{ name = 'leftover-native-ac'; version = '1.0' }
}
Invoke-McpRpc -Method 'notifications/initialized' | Out-Null
Write-Output ('INIT=' + $init.Status)

$targets = @(
    'FR-MCP-SESSIONATTR-001','FR-MCP-FAILSAFE-001','FR-MCP-STRICTCOUNT-001','FR-MCP-XAGENT-001','FR-MCP-SESSIONEND-001','FR-MCP-VERIFYWRAP-001','FR-MCP-TRANSCRIPT-SEARCH-001','FR-MCP-TEMPVOL-001'
    'TR-MCP-SESSIONATTR-001','TR-MCP-FAILSAFE-001','TR-MCP-STRICTCOUNT-001','TR-MCP-XAGENT-001','TR-MCP-SESSIONEND-001','TR-MCP-VERIFYWRAP-001','TR-MCP-TRANSCRIPT-SEARCH-001','TR-MCP-TEMPVOL-001'
    'TEST-MCP-SESSIONATTR-001','TEST-MCP-FAILSAFE-001','TEST-MCP-STRICTCOUNT-001','TEST-MCP-XAGENT-001','TEST-MCP-SESSIONEND-001','TEST-MCP-VERIFYWRAP-001','TEST-MCP-TRANSCRIPT-SEARCH-001','TEST-MCP-TEMPVOL-001'
)

$rows = [System.Collections.Generic.List[object]]::new()
foreach ($kind in @('fr','tr','test')) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $kind
    }
    $parsed = Get-ToolObject -Result $res
    $items = Get-Items -Parsed $parsed
    Write-Output ('KIND=' + $kind + ' TOTAL=' + $items.Count)
    foreach ($item in $items) {
        $id = [string](Get-Prop $item 'Id')
        if ([string]::IsNullOrWhiteSpace($id)) { $id = [string](Get-Prop $item 'id') }
        if ($targets -notcontains $id) { continue }
        $ac = Get-Prop $item 'AcceptanceCriteria'
        if ($null -eq $ac) { $ac = Get-Prop $item 'acceptanceCriteria' }
        $acCount = 0
        if ($null -ne $ac) { $acCount = @($ac).Count }
        $first = $null
        if ($acCount -gt 0) { $first = Get-Prop $ac[0] 'text'; if (-not $first) { $first = Get-Prop $ac[0] 'Text' } }
        $rows.Add([pscustomobject]@{
            Kind = $kind
            Id = $id
            AcCount = $acCount
            FirstText = [string]$first
        })
        Write-Output ('NATIVE ' + $id + ' ac=' + $acCount + ' first=' + $first)
    }
}

$rows | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outDir 'native-ac-summary.json') -Encoding utf8
Write-Output ('NATIVE_ROWS=' + $rows.Count)
