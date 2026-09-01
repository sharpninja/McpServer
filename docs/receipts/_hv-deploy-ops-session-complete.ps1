#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = 'F:\GitHub\McpServer\docs\receipts'
$sessionId = 'GrokCode-20260818T181311Z-deploy-ops'
$requestId = 'req-20260818T181311Z-001-hostile-deploy-ops'
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
    $names = @($outer.PSObject.Properties.Name)
    if ($names -contains 'error') {
        throw ("MCP error: " + ($outer.error | ConvertTo-Json -Depth 8 -Compress))
    }
    $resultNames = @($outer.result.PSObject.Properties.Name)
    if ($resultNames -contains 'isError' -and $outer.result.isError) {
        throw ("MCP tool error: " + $outer.result.content[0].text)
    }
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-deploy-ops-complete'; version = '1.0.0' }
}
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$now = [datetime]::UtcNow.ToString('o')
$dialogItems = @(
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile. Excluded add-profile.grok.md.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Classified CLASS 2 user-directed lab/ops (redeploy service, install REPL, sync plugins). Surface C N/A. Surface D N/A (planFile None, todoId None). Defaulted claims FAIL/UNKNOWN until independent re-verify.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Re-verified live: Service Running; health nonce hv-deploy-ops-f76c604e7e204730b8b9d92725d14ac9 echoed; version 1.4.26+298c5fde; marker startedAt 2026-08-18T18:02:40.9427094+00:00; deployment json generatedBy build/Build.UpdateService.cs generatedUtc 2026-08-18T18:02:20.3911263Z; workspace 38/38 enabled; swagger /mcpserver/products; GET products 200 []; effective productScope; REPL 1.4.26+298c5fde; plugins 1.94.0; grok CORE-MANIFEST 298c5fde.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE for this class-2 deploy/REPL/plugin/products ops review. Consequence: parent may treat the ops claims as independently confirmed; this review did not flip any MCP TODO. Alternatives rejected: DISAGREE because _deploy-update-service-20260818T180100Z.txt is SyncAgentPlugins content (parent forbade that FAIL when json + health prove UpdateService; dated nuke log also proves it); DISAGREE because marker still says plugin 1.93.0 (authoritative plugin .version is 1.94.0); DISAGREE because SkipVersionBump is not a log token (live and publish version stayed 1.4.26). Affected: none (no TODO / no FR).'
        category = 'decision'
    }
)
$dialogJson = $dialogItems | ConvertTo-Json -Depth 8 -Compress
$dialog = Invoke-McpTool -Name 'sessionlog_dialog' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    itemsJson = $dialogJson
    workspacePath = $workspace
}
Save-Body -Name '_hv-deploy-ops-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Marker HMAC match DAB0AC6970CA8AF6D864E6057AAB3C4C788DF2AECFD0BBC6DDEB0AF4959840D3; health nonce echoed; version 1.4.26+298c5fde'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Service Running; deployment json generatedBy build/Build.UpdateService.cs generatedUtc 2026-08-18T18:02:20.3911263Z'; type = 'edit'; status = 'completed'; filePath = 'C:\ProgramData\McpServer\.mcpservice-deployment.json' }
    [ordered]@{ order = 4; description = 'Nuke UpdateService log WSHealth 38/38 and live GET /mcpserver/workspace 38 enabled'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\.nuke\temp\build.2026-08-18_13-00-49.log' }
    [ordered]@{ order = 5; description = 'Live mcpserver-repl --version 1.4.26+298c5fde; swagger products + GET 200 []; effective productScope'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-deploy-ops-collect-20260818T181033Z.txt' }
    [ordered]@{ order = 6; description = 'Plugin .version 1.94.0 siblings; grok CORE-MANIFEST coreVersion 298c5fde; grok cache 1.94.0'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\mcpserver-grok-plugin\CORE-MANIFEST.yaml' }
    [ordered]@{ order = 7; description = 'Wrote hostile deploy-ops receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T181430Z.md' }
    [ordered]@{ order = 8; description = 'Decision: AGREE class-2 deploy/REPL/plugin/products review; do not flip TODOs; do not FAIL mislabeled update-service txt'; type = 'design_decision'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T181430Z.md' }
)
$section = [ordered]@{ actions = $actions }
$sectionJson = $section | ConvertTo-Json -Depth 8 -Compress
$repl = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'actions'
    sectionJson = $sectionJson
    workspacePath = $workspace
}
Save-Body -Name '_hv-deploy-ops-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$response = @'
OverallVerdict AGREE. Class 2 user-directed lab/ops. Surface C N/A. Surface D N/A.

Independent re-verify:
- Service Running. GET /health nonce hv-deploy-ops-f76c604e7e204730b8b9d92725d14ac9 echoed. version 1.4.26+298c5fde3d1438ff7741ebec82ced796b207433e. Marker HMAC match. startedAt 2026-08-18T18:02:40.9427094+00:00.
- Deployment json generatedBy build/Build.UpdateService.cs generatedUtc 2026-08-18T18:02:20.3911263Z. Nuke log WSHealth OK (38/38). Live workspace 38/38 enabled.
- mcpserver-repl --version 1.4.26+298c5fde. SHA prefix and package 1.4.26 match.
- Plugins .version 1.94.0 including grok. CORE-MANIFEST coreVersion 298c5fde. Grok cache 1.94.0 refreshed.
- Live swagger has /mcpserver/products. GET /mcpserver/products 200 []. GET /mcpserver/requirements/effective has productScope.
- Implementer receipts _deploy-install-repl-20260818T180300Z.txt and _deploy-sync-plugins-20260818T180600Z.txt exist. UpdateService proved by deployment json + health + dated nuke log. Mislabeled _deploy-update-service-20260818T180100Z.txt is SyncAgentPlugins content; not a FAIL.

Receipt: docs/receipts/hostile-validator-20260818T181430Z.md
TODO not flipped.
'@

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile review of service REPL plugin redeploy'
    queryText = 'Hostile validator CLASS 2 user-directed lab/ops: independently re-verify UpdateService, InstallReplTool, SyncAgentPlugins, and live Products REST. Surface C N/A. planFile None. todoId None. Do not flip TODOs. Do not implement.'
    response = $response
    interpretation = 'Operator asked for an independent hostile review of the service/REPL/plugin redeploy and live Products REST. Class 2 ops. AGREE only if live health, deployment json, REPL, plugins, swagger, and GET products re-verify. Do not fail missing FR/TR. Do not flip TODOs.'
    status = 'completed'
    tags = @('hostile-validator', 'CLASS-2', 'deploy-ops', 'AGREE', 'UpdateService', 'InstallReplTool', 'SyncAgentPlugins', 'products')
    contextList = @(
        'C:\ProgramData\McpServer\.mcpservice-deployment.json'
        'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
        'F:\GitHub\McpServer\.nuke\temp\build.2026-08-18_13-00-49.log'
        'docs/receipts/_deploy-install-repl-20260818T180300Z.txt'
        'docs/receipts/_deploy-sync-plugins-20260818T180600Z.txt'
        'docs/receipts/hostile-validator-20260818T181430Z.md'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T181430Z.md'
        'docs/receipts/hostile-validator-20260818T181430Z.json'
    )
    planFile = 'None'
    todoId = 'None'
    designDecisions = @(
        'AGREE class-2 deploy/REPL/plugin/products review. Live health, deployment json, dated Nuke UpdateService log, REPL version, plugin 1.94.0, and Products GET 200 [] independently match. Do not flip TODOs. Do not FAIL the mislabeled update-service txt.'
    )
    requirementsDiscovered = @()
}
$turnJson = $turn | ConvertTo-Json -Depth 8 -Compress
$complete = Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    turnJson = $turnJson
}
Save-Body -Name '_hv-deploy-ops-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-18T18:13:00Z'
    limit = 10
}
Save-Body -Name '_hv-deploy-ops-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
Write-Output ('QUERY=' + ($proof | ConvertTo-Json -Depth 10 -Compress))
Write-Output 'COMPLETE_DONE'
