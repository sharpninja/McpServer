#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s0-leftover'
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

function Save-Body {
    param([string]$Name, $Result)
    $path = Join-Path $outDir $Name
    $Result.Body | Set-Content -LiteralPath $path -Encoding utf8
    Write-Output ('SAVED ' + $Name + ' HTTP=' + $Result.Status + ' LEN=' + $Result.Body.Length)
}

function Get-ToolObject {
    param($Result)
    $outer = $Result.Body | ConvertFrom-Json
    $resultProp = Get-Prop -Obj $outer -Name 'result'
    if ($null -eq $resultProp) { return $outer }
    $content = Get-Prop -Obj $resultProp -Name 'content'
    if ($null -eq $content) { return $resultProp }
    $first = @($content)[0]
    $text = [string](Get-Prop -Obj $first -Name 'text')
    try { return ($text | ConvertFrom-Json) } catch { return [pscustomobject]@{ rawText = $text } }
}

function Get-Items {
    param($Parsed)
    if ($null -eq $Parsed) { return @() }
    foreach ($name in @('items','Items','records','Records','requirements','Requirements','mappings','Mappings','value','Value')) {
        $prop = Get-Prop -Obj $Parsed -Name $name
        if ($null -ne $prop) { return @($prop) }
    }
    if ($Parsed -is [System.Array]) { return @($Parsed) }
    return @($Parsed)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-s0-leftover-2'; version = '1.0.0' }
}
Save-Body -Name 'mcp-init2.json' -Result $init
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$ids = @(106,107,108,113,116,117,118,120,121,122,125,130,134,140,142,144,147,150,151,152,153,154,155,156,157,158,159)
$bugRows = @()
foreach ($n in $ids) {
    $bugId = 'BUG-TRIAGE-' + $n.ToString()
    $res = Invoke-McpTool -Name 'todo_get' -Arguments @{
        id = $bugId
        workspacePath = $workspace
    }
    $obj = Get-ToolObject -Result $res
    $row = [ordered]@{
        id = $bugId
        storeId = [string](Get-Prop -Obj $obj -Name 'Id')
        exists = ([string](Get-Prop -Obj $obj -Name 'Id') -eq $bugId)
        Done = (Get-Prop -Obj $obj -Name 'Done')
        DoneSummary = [string](Get-Prop -Obj $obj -Name 'DoneSummary')
        CompletedDate = [string](Get-Prop -Obj $obj -Name 'CompletedDate')
        isError = $false
        errorText = ''
    }
    $rawText = [string](Get-Prop -Obj $obj -Name 'rawText')
    $err = Get-Prop -Obj $obj -Name 'error'
    if ($rawText -and $rawText -match 'not found|error') {
        $row.isError = $true
        $row.errorText = $rawText
    }
    if ($null -ne $err) {
        $row.isError = $true
        $row.errorText = [string]$err
    }
    $bugRows += [pscustomobject]$row
    Write-Output ('BUG ' + $bugId + ' exists=' + $row.exists + ' Done=' + $row.Done + ' Completed=' + $row.CompletedDate + ' err=' + $row.isError)
}
$bugRows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'bug-triage-27.json') -Encoding utf8
$doneTrue = @($bugRows | Where-Object { $_.Done -eq $true -or $_.Done -eq 'True' })
Write-Output ('BUG_DONE_TRUE=' + $doneTrue.Count)
Write-Output ('BUG_EXISTS=' + (@($bugRows | Where-Object exists).Count))

$targetFr = @(
    'FR-MCP-SESSIONATTR-001'
    'FR-MCP-FAILSAFE-001'
    'FR-MCP-STRICTCOUNT-001'
    'FR-MCP-XAGENT-001'
    'FR-MCP-SESSIONEND-001'
    'FR-MCP-VERIFYWRAP-001'
    'FR-MCP-TRANSCRIPT-SEARCH-001'
    'FR-MCP-TEMPVOL-001'
)
$targetTr = @(
    'TR-MCP-SESSIONATTR-001'
    'TR-MCP-FAILSAFE-001'
    'TR-MCP-STRICTCOUNT-001'
    'TR-MCP-XAGENT-001'
    'TR-MCP-SESSIONEND-001'
    'TR-MCP-VERIFYWRAP-001'
    'TR-MCP-TRANSCRIPT-SEARCH-001'
    'TR-MCP-TEMPVOL-001'
)
$targetTest = @(
    'TEST-MCP-SESSIONATTR-001'
    'TEST-MCP-FAILSAFE-001'
    'TEST-MCP-STRICTCOUNT-001'
    'TEST-MCP-XAGENT-001'
    'TEST-MCP-SESSIONEND-001'
    'TEST-MCP-VERIFYWRAP-001'
    'TEST-MCP-TRANSCRIPT-SEARCH-001'
    'TEST-MCP-TEMPVOL-001'
)

$reqSummary = [ordered]@{}
foreach ($kind in @('fr', 'tr', 'test', 'mapping')) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $kind
    }
    Save-Body -Name ('req-' + $kind + '.json') -Result $res
    $parsed = Get-ToolObject -Result $res
    $items = Get-Items -Parsed $parsed
    Write-Output ('KIND=' + $kind + ' TOTAL=' + $items.Count + ' KEYS=' + (($parsed.PSObject.Properties.Name) -join ','))
    $filtered = @()
    foreach ($item in $items) {
        $blob = ($item | ConvertTo-Json -Depth 16 -Compress)
        $id = [string](Get-Prop -Obj $item -Name 'Id')
        if ([string]::IsNullOrWhiteSpace($id)) { $id = [string](Get-Prop -Obj $item -Name 'id') }
        if ([string]::IsNullOrWhiteSpace($id)) { $id = [string](Get-Prop -Obj $item -Name 'FrId') }
        $needles = $targetFr + $targetTr + $targetTest + @('SESSIONATTR','FAILSAFE','STRICTCOUNT','XAGENT','SESSIONEND','VERIFYWRAP','TRANSCRIPT-SEARCH','TEMPVOL')
        $hit = $false
        foreach ($n in $needles) {
            if ($blob.IndexOf($n, [StringComparison]::OrdinalIgnoreCase) -ge 0) { $hit = $true; break }
        }
        if ($hit) {
            $ac = Get-Prop -Obj $item -Name 'AcceptanceCriteria'
            if ($null -eq $ac) { $ac = Get-Prop -Obj $item -Name 'acceptanceCriteria' }
            $acCount = 0
            if ($null -ne $ac) { $acCount = @($ac).Count }
            $trIds = Get-Prop -Obj $item -Name 'TrIds'
            if ($null -eq $trIds) { $trIds = Get-Prop -Obj $item -Name 'trIds' }
            $testIds = Get-Prop -Obj $item -Name 'TestIds'
            if ($null -eq $testIds) { $testIds = Get-Prop -Obj $item -Name 'testIds' }
            $filtered += [pscustomobject]@{
                id = $id
                acCount = $acCount
                trIds = $trIds
                testIds = $testIds
                blob = $blob
            }
        }
    }
    $filtered | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir ('req-' + $kind + '-leftover.json')) -Encoding utf8
    $reqSummary[$kind] = @{
        total = $items.Count
        leftoverHits = $filtered.Count
        leftoverIds = @($filtered | ForEach-Object { $_.id })
        leftoverAcCounts = @($filtered | ForEach-Object { $_.id.ToString() + ':' + $_.acCount })
    }
}

$reqSummary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'req-summary.json') -Encoding utf8
Write-Output ('REQ_SUMMARY=' + ($reqSummary | ConvertTo-Json -Depth 8 -Compress))

foreach ($fr in $targetFr) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = 'fr'
        id = $fr
    }
    $obj = Get-ToolObject -Result $res
    $json = $obj | ConvertTo-Json -Depth 16
    $json | Set-Content -LiteralPath (Join-Path $outDir ('fr-' + $fr + '.json')) -Encoding utf8
    Write-Output ('GETFR ' + $fr + ' LEN=' + $json.Length)
}

foreach ($tr in $targetTr) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = 'tr'
        id = $tr
    }
    $obj = Get-ToolObject -Result $res
    $json = $obj | ConvertTo-Json -Depth 16
    $json | Set-Content -LiteralPath (Join-Path $outDir ('tr-' + $tr + '.json')) -Encoding utf8
    Write-Output ('GETTR ' + $tr + ' LEN=' + $json.Length)
}

foreach ($test in $targetTest) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = 'test'
        id = $test
    }
    $obj = Get-ToolObject -Result $res
    $json = $obj | ConvertTo-Json -Depth 16
    $json | Set-Content -LiteralPath (Join-Path $outDir ('test-' + $test + '.json')) -Encoding utf8
    Write-Output ('GETTEST ' + $test + ' LEN=' + $json.Length)
}

foreach ($fr in $targetFr) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = 'mapping'
        frId = $fr
    }
    $obj = Get-ToolObject -Result $res
    $json = $obj | ConvertTo-Json -Depth 16
    $json | Set-Content -LiteralPath (Join-Path $outDir ('map-' + $fr + '.json')) -Encoding utf8
    Write-Output ('GETMAP ' + $fr + ' LEN=' + $json.Length)
}

Write-Output 'COLLECT2_DONE'
