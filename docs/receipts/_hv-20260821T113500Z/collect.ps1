#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260821T113500Z'
$baseUrl = 'http://PAYTON-LEGION2:7147'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

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
    $json = $payload | ConvertTo-Json -Depth 30 -Compress
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
    } finally {
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
    Write-Output ("SAVED " + $Name + " HTTP=" + $Result.Status + " LEN=" + $Result.Body.Length)
}

Write-Output 'MCP_INIT'
$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-add-profile'; version = '1.0.0' }
}
Save-Body -Name 'mcp-init.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$q1 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'GrokCode-20260821T113141Z-plugin-session'
    limit = 5
}
Save-Body -Name 'q-session-113141.json' -Result $q1

$q2 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'GrokCode-20260821T112802Z-plugin-session'
    limit = 5
}
Save-Body -Name 'q-session-112802.json' -Result $q2

$q3 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'req-20260821T113258Z-001-add-profile-new-session'
    limit = 5
}
Save-Body -Name 'q-turn-113258.json' -Result $q3

$q4 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'req-20260821T112813Z-prompt-ac0b'
    limit = 5
}
Save-Body -Name 'q-turn-112813.json' -Result $q4

$todos = Invoke-McpTool -Name 'todo_list' -Arguments @{
    workspacePath = $workspace
    done = $false
}
Save-Body -Name 'todo-list-done-false.json' -Result $todos

$mem = Invoke-McpTool -Name 'memory_list' -Arguments @{
    workspacePath = $workspace
    scope = 'Effective'
}
Save-Body -Name 'memory-list-effective.json' -Result $mem

$memLab = Invoke-McpTool -Name 'memory_list' -Arguments @{
    workspacePath = $workspace
    keyword = 'MEMORY-LAB'
}
Save-Body -Name 'memory-list-lab.json' -Result $memLab

Write-Output 'MCP_QUERIES_DONE'

Write-Output 'PLUGIN_STATUS'
$pluginExe = Join-Path $plugin 'lib\Invoke-McpPlugin.ps1'
$statusOut = & pwsh.exe -NoProfile -NonInteractive -File $pluginExe -Command Status -WorkspacePath $workspace -PluginRoot $plugin 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $outDir 'plugin-status.txt') -Value $statusOut -Encoding utf8
Write-Output ('PLUGIN_STATUS_LEN=' + $statusOut.Length)

Write-Output 'PLUGIN_TOOLS_SEARCH'
. (Join-Path $workspace 'plugins\core\lib-ps\yaml-object-mutation.ps1')
Import-McpYamlSerializer
$searchObj = [ordered]@{ keyword = 'mcpserver-grok-plugin' }
$searchPath = Join-Path $outDir 'tools-search-params.yaml'
Set-Content -LiteralPath $searchPath -Value (ConvertTo-Yaml -Data $searchObj -Options WithIndentedSequences) -Encoding utf8
$toolsOut = & pwsh.exe -NoProfile -NonInteractive -File $pluginExe -Command Invoke -Method 'client.Tools.SearchAsync' -ParamsPath $searchPath -WorkspacePath $workspace -PluginRoot $plugin -TimeoutSeconds 90 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $outDir 'tools-search.txt') -Value $toolsOut -Encoding utf8
Write-Output ('PLUGIN_TOOLS_LEN=' + $toolsOut.Length)

Write-Output 'GIT_TODO_STATUS'
Push-Location $workspace
$todoGit = git status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml 2>&1 | Out-String
$todoLog = git log -3 --oneline -- docs/Project/TODO.yaml 2>&1 | Out-String
Pop-Location
Set-Content -LiteralPath (Join-Path $outDir 'todo-git-status.txt') -Value ($todoGit + "`n---LOG---`n" + $todoLog) -Encoding utf8

Write-Output 'COLLECT_OK'
