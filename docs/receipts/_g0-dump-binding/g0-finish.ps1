#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseUrl = 'http://PAYTON-LEGION2:7147/mcp-transport'
$workspacePath = 'F:\GitHub\McpServer'
$sessionId = 'GrokCode-20260822T132344Z-pluginhandoff-g0'
$requestId = 'req-20260822T132344Z-001-g0-dump-binding'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_g0-dump-binding'
$receiptUtc = '20260822T132429Z'
$noteUtc = [DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')

function Invoke-McpJsonRpc {
    param(
        [Parameter(Mandatory)][hashtable]$Body,
        [string]$McpSessionId
    )

    $headers = @{
        Accept = 'application/json, text/event-stream'
    }
    if (-not [string]::IsNullOrWhiteSpace($McpSessionId)) {
        $headers['Mcp-Session-Id'] = $McpSessionId
    }

    $json = ConvertTo-Json -InputObject $Body -Depth 30 -Compress
    $response = Invoke-WebRequest -Uri $baseUrl -Method POST -Headers $headers -ContentType 'application/json' -Body $json -UseBasicParsing
    $sessionHeader = $null
    if ($response.Headers['Mcp-Session-Id']) {
        $sessionHeader = [string]$response.Headers['Mcp-Session-Id']
    }

    $text = [string]$response.Content
    $payload = $text
    foreach ($line in ($text -split "`n")) {
        if ($line.StartsWith('data:')) {
            $payload = $line.Substring(5).Trim()
        }
    }

    [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        SessionId  = $sessionHeader
        Raw        = $text
        Payload    = $payload
    }
}

function Invoke-McpToolCall {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][hashtable]$Arguments,
        [string]$McpSessionId
    )

    Invoke-McpJsonRpc -Body @{
        jsonrpc = '2.0'
        id      = 2
        method  = 'tools/call'
        params  = @{
            name      = $Name
            arguments = $Arguments
        }
    } -McpSessionId $McpSessionId
}

function Get-ToolInnerObject {
    param([Parameter(Mandatory)][string]$Payload)
    $outer = $Payload | ConvertFrom-Json
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

$init = Invoke-McpJsonRpc -Body @{
    jsonrpc = '2.0'
    id      = 1
    method  = 'initialize'
    params  = @{
        protocolVersion = '2024-11-05'
        capabilities    = @{}
        clientInfo      = @{
            name    = 'GrokCode'
            version = '1.0.5'
        }
    }
}
$mcpSession = [string]$init.SessionId
try {
    Invoke-McpJsonRpc -Body @{
        jsonrpc = '2.0'
        method  = 'notifications/initialized'
        params  = @{}
    } -McpSessionId $mcpSession | Out-Null
} catch { }

$jsonTwin = [ordered]@{
    timestampUtc = '2026-08-22T13:24:29Z'
    agent        = 'GrokCode'
    sessionId    = $sessionId
    requestId    = $requestId
    turnId       = 42980
    todoId       = 'PLAN-PLUGINHANDOFF-001'
    phase        = 'G0'
    bound        = 'no'
    federationRegisterWorkspaceAsyncOutOfScope = $true
    recommendedFirstGRedConsumer = 'AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml'
    missingWrapperConsumers = @(
        'Director add-workspace --dump (BuildAddWorkspaceCommand)'
        'REPL client.Workspace.CreateAsync dump field'
        'plugin client.Workspace.CreateAsync dump field'
        'wiki requirements generate --include-dump (inspect only; not first G-red this hour)'
    )
    createAsync = [ordered]@{
        workspaceClient = 'McpServer.Client.WorkspaceClient.CreateAsync(WorkspaceCreateRequest request, CancellationToken cancellationToken = default) -> POST mcpserver/workspace'
        workspaceController = 'WorkspaceController.CreateAsync([FromBody] WorkspaceCreateRequest? request, CancellationToken ct) HTTP POST /mcpserver/workspace'
        workspaceService = 'WorkspaceService.CreateAsync(WorkspaceCreateRequest request, CancellationToken ct = default)'
        dumpParameterBound = $false
    }
    wrappers = [ordered]@{
        directorAddWorkspace = [ordered]@{
            found = $true
            type = 'McpServerManager.Director.Commands.DirectorCommands'
            method = 'BuildAddWorkspaceCommand'
            command = 'add-workspace'
            options = @('--workspace/-w', '--name/-n', '--server/-s')
            dumpOption = $false
        }
        replClientWorkspaceCreateAsync = [ordered]@{
            found = $true
            method = 'client.Workspace.CreateAsync'
            dumpField = $false
        }
        pluginWorkspaceSkill = [ordered]@{
            found = $true
            method = 'client.Workspace.CreateAsync'
            dumpField = $false
        }
    }
    wikiIncludeDump = [ordered]@{
        present = $false
        surfacesInspected = @(
            'IGenerateDocumentParams'
            'RequirementsWorkflow.GenerateDocumentAsync'
            'requirements_generate'
            'RequirementsClient.GenerateAsync'
            'RequirementsController.GenerateAsync'
            'IRequirementsDocumentService.GenerateWikiAsync'
            'DirectorCommands.Register (no wiki-export command)'
        )
    }
    federation = [ordered]@{
        type = 'McpServer.Client.FederationClient'
        method = 'RegisterWorkspaceAsync(string proxyId, FederationWorkspaceRegistrationRequest request, CancellationToken cancellationToken = default)'
        path = 'mcpserver/federation/proxies/{proxyId}/workspaces'
        outOfScope = $true
        dumpField = $false
    }
    todosRemainDoneFalse = @('PLAN-PLUGINHANDOFF-001', 'MCP-WIKIEXPORT-001', 'MCP-PLUGININT-001')
    gRedTestsWritten = $false
    createAsyncDumpImplemented = $false
    receiptMarkdown = "docs/receipts/g0-dump-binding-$receiptUtc.md"
}

$jsonPath = Join-Path 'F:\GitHub\McpServer\docs\receipts' "g0-dump-binding-$receiptUtc.json"
$jsonTwin | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$note = "$noteUtc. Session $sessionId. G0 dump-binding checkpoint docs/receipts/g0-dump-binding-$receiptUtc.md. bound=no. WorkspaceClient.CreateAsync(WorkspaceCreateRequest) and WorkspaceController.CreateAsync POST /mcpserver/workspace have no dump field. Wrappers exist without --dump: Director add-workspace, REPL client.Workspace.CreateAsync, plugin client.Workspace.CreateAsync. Wiki --include-dump absent. FederationClient.RegisterWorkspaceAsync out of scope. First G-red: AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml. PLAN/MCP-WIKIEXPORT-001/MCP-PLUGININT-001 remain done:false. No G-red tests and no CreateAsync dump implementation this hour."

$todoUpdate = Invoke-McpToolCall -Name 'todo_update' -McpSessionId $mcpSession -Arguments @{
    id            = 'PLAN-PLUGINHANDOFF-001'
    workspacePath = $workspacePath
    note          = $note
}

$planAfter = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
    id            = 'PLAN-PLUGINHANDOFF-001'
    workspacePath = $workspacePath
}
$wikiAfter = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
    id            = 'MCP-WIKIEXPORT-001'
    workspacePath = $workspacePath
}
$pluginAfter = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
    id            = 'MCP-PLUGININT-001'
    workspacePath = $workspacePath
}

$dialogItems = @(
    [ordered]@{
        timestamp = $noteUtc
        role      = 'model'
        category  = 'decision'
        content   = 'Decision: G0 binds add-workspace hydration to WorkspaceClient.CreateAsync / POST /mcpserver/workspace (WorkspaceController.CreateAsync), not FederationClient.RegisterWorkspaceAsync. Observation: dump is not bound yet. Consequence: first G-red consumer is AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml against Director/REPL/plugin CreateAsync wrappers that exist without --dump. Alternatives rejected: binding dump to federation proxy register; implementing dump on CreateAsync before red tests; writing G-red tests this hour.'
    }
    [ordered]@{
        timestamp = $noteUtc
        role      = 'model'
        category  = 'observation'
        content   = 'bound=no. Director add-workspace options are --workspace/-w, --name/-n, --server/-s. Plugin and REPL use client.Workspace.CreateAsync with WorkspaceCreateRequest fields and no dump. Wiki --include-dump absent on generateDocument / requirements_generate / GenerateWikiAsync / Director commands.'
    }
)
$dialogJson = ConvertTo-Json -InputObject $dialogItems -Depth 10 -Compress

$dialog = Invoke-McpToolCall -Name 'sessionlog_dialog' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    requestId     = $requestId
    workspacePath = $workspacePath
    itemsJson     = $dialogJson
}

$turn = [ordered]@{
    requestId     = $requestId
    queryTitle    = 'G0 confirm add-workspace dump binding'
    interpretation = 'Operator asked for PLAN-PLUGINHANDOFF-001 Phase G0 only: confirm --dump binding on live add-workspace CreateAsync, inspect wrappers and wiki --include-dump, write receipts, optional NOTE-only todo_update, complete the session turn. No implementation and no G-red tests.'
    response      = "G0 complete. receipt=docs/receipts/g0-dump-binding-$receiptUtc.md bound=no. Wrappers found without --dump: Director add-workspace, REPL client.Workspace.CreateAsync, plugin client.Workspace.CreateAsync. Wiki --include-dump absent. FederationClient.RegisterWorkspaceAsync out of scope. First G-red: AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml."
    status        = 'completed'
    model         = 'grok-code'
    planFile      = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId        = 'PLAN-PLUGINHANDOFF-001'
    tags          = @('PLAN-PLUGINHANDOFF-001', 'MCP-WIKIEXPORT-001', 'G0', 'dump-binding')
    contextList   = @(
        'docs/plans/PLAN-PLUGINHANDOFF-001.md'
        'src/McpServer.Client/WorkspaceClient.cs'
        'src/McpServer.Support.Mcp/Controllers/WorkspaceController.cs'
        'src/McpServer.Client/FederationClient.cs'
    )
    filesModified = @(
        "docs/receipts/g0-dump-binding-$receiptUtc.md"
        "docs/receipts/g0-dump-binding-$receiptUtc.json"
        'docs/receipts/_g0-dump-binding/ids.json'
    )
    designDecisions = @(
        'Bind add-workspace --dump to WorkspaceClient.CreateAsync / WorkspaceController.CreateAsync POST /mcpserver/workspace, not FederationClient.RegisterWorkspaceAsync.'
        'G0 observation bound=no; wrappers exist without --dump so first G-red is AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml as a failing consumer of that CreateAsync contract.'
        'Do not implement dump/import or write G-red tests in G0. Keep PLAN-PLUGINHANDOFF-001, MCP-WIKIEXPORT-001, MCP-PLUGININT-001 done:false.'
    )
    requirementsDiscovered = @('FR-MCP-WIKIEXPORT-003', 'FR-MCP-WIKIEXPORT-004')
    blockers = @()
    actions = @(
        [ordered]@{ order = 1; type = 'tool_call'; status = 'completed'; description = 'Verified marker HMAC and /health nonce echo bdb708cdf47d4414858f5f4058997865'; filePath = 'AGENTS-README-FIRST.yaml' }
        [ordered]@{ order = 2; type = 'tool_call'; status = 'completed'; description = 'Plugin Status available GrokCode mcpserver-grok-plugin 1.105.0'; filePath = 'F:\GitHub\mcpserver-grok-plugin\.version' }
        [ordered]@{ order = 3; type = 'tool_call'; status = 'completed'; description = 'sessionlog_open created GrokCode-20260822T132344Z-pluginhandoff-g0'; filePath = '' }
        [ordered]@{ order = 4; type = 'inspect'; status = 'completed'; description = 'Inspected WorkspaceClient.CreateAsync, WorkspaceController.CreateAsync, DTOs, Director add-workspace, REPL/plugin wrappers, wiki generate surfaces, FederationClient.RegisterWorkspaceAsync'; filePath = 'src/McpServer.Client/WorkspaceClient.cs' }
        [ordered]@{ order = 5; type = 'create'; status = 'completed'; description = 'Wrote G0 dump-binding receipt markdown'; filePath = "docs/receipts/g0-dump-binding-$receiptUtc.md" }
        [ordered]@{ order = 6; type = 'create'; status = 'completed'; description = 'Wrote G0 dump-binding receipt JSON twin'; filePath = "docs/receipts/g0-dump-binding-$receiptUtc.json" }
        [ordered]@{ order = 7; type = 'design_decision'; status = 'completed'; description = 'Hydration bind target is WorkspaceClient.CreateAsync / POST /mcpserver/workspace. Federation register is out of scope. bound=no.'; filePath = '' }
    )
}
$turnJson = ConvertTo-Json -InputObject $turn -Depth 20 -Compress

$complete = Invoke-McpToolCall -Name 'sessionlog_complete_turn' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    requestId     = $requestId
    workspacePath = $workspacePath
    turnJson      = $turnJson
}

$query = Invoke-McpToolCall -Name 'sessionlog_query' -McpSessionId $mcpSession -Arguments @{
    workspacePath = $workspacePath
    agent         = 'GrokCode'
    text          = $sessionId
    limit         = 5
}

$finish = [ordered]@{
    noteUtc      = $noteUtc
    jsonTwin     = $jsonPath
    todoUpdate   = $todoUpdate.Payload
    planAfter    = $planAfter.Payload
    wikiAfter    = $wikiAfter.Payload
    pluginAfter  = $pluginAfter.Payload
    dialog       = $dialog.Payload
    complete     = $complete.Payload
    query        = $query.Payload
}
$finish | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $outDir 'finish.json') -Encoding utf8

$planObj = Get-ToolInnerObject $planAfter.Payload
$wikiObj = Get-ToolInnerObject $wikiAfter.Payload
$pluginObj = Get-ToolInnerObject $pluginAfter.Payload
Write-Output ('JSON_TWIN=' + $jsonPath)
Write-Output ('TODO_UPDATE=' + $todoUpdate.Payload)
Write-Output ('PLAN_DONE=' + $planObj.Done)
Write-Output ('PLAN_NOTE=' + $planObj.Note)
Write-Output ('WIKI_DONE=' + $wikiObj.Done)
Write-Output ('PLUGININT_DONE=' + $pluginObj.Done)
Write-Output ('DIALOG=' + $dialog.Payload)
Write-Output ('COMPLETE=' + $complete.Payload)
Write-Output ('QUERY_SNIP=' + $query.Payload.Substring(0, [Math]::Min(500, $query.Payload.Length)))
