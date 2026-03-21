<#
.SYNOPSIS
    MCP Context PowerShell module for folder + URL ingestion and querying.

.DESCRIPTION
    Provides a practical operator workflow for:
    - Ingesting local document folders into docs/external and triggering sync.
    - Ingesting website URLs directly via REST endpoint.
    - Querying context and GraphRAG.

    By default, connection details come from AGENTS-README-FIRST.yaml
    (baseUrl, apiKey, workspacePath) discovered by walking up from CWD.

.NOTES
        Quickstart:
            Import-Module ./tools/powershell/McpContext.psm1 -Force
            Initialize-McpContext
            Import-McpContextFolder -Path .\docs\external\seed -Sync
            Import-McpContextUrl -Url "https://example.com" -TriggerGraphRagIndex
            Search-McpContext -Query "auth flow" -Limit 10
            Query-McpGraphRag -Query "What changed in auth?"

        Common workflow:
            1) Initialize module connection (marker auto-discovery or explicit values)
            2) Stage local files with Import-McpContextFolder
            3) Trigger sync with Invoke-McpSyncRun (or use -Sync on folder import)
            4) Ingest live pages with Import-McpContextUrl
            5) Query using Search-McpContext and Query-McpGraphRag

        Tip:
            Run Get-Help <command> -Detailed for each exported function.
#>

Set-StrictMode -Version Latest

# Module state
$script:McpBaseUrl = $null
$script:McpApiKey = $null
$script:McpWorkspacePath = $null
$script:McpHeaders = @{}
$script:McpTransportUrl = $null

function Initialize-McpContext {
    <#
    .SYNOPSIS
        Initializes module connection from marker file or explicit overrides.
    .DESCRIPTION
        Loads baseUrl, apiKey, and workspacePath from AGENTS-README-FIRST.yaml by default.
        You can override values explicitly for non-standard environments.
    .PARAMETER MarkerPath
        Optional explicit marker path.
    .PARAMETER BaseUrl
        Optional REST base URL override.
    .PARAMETER ApiKey
        Optional API key override.
    .PARAMETER WorkspacePath
        Optional workspace path override.
    .EXAMPLE
        Initialize-McpContext

        Auto-discovers AGENTS-README-FIRST.yaml from current directory upward.
    .EXAMPLE
        Initialize-McpContext -MarkerPath 'E:\github\McpServer\AGENTS-README-FIRST.yaml'

        Initializes from a specific marker file.
    .EXAMPLE
        Initialize-McpContext -BaseUrl 'http://localhost:7147' -ApiKey 'key' -WorkspacePath 'E:\github\McpServer'

        Uses explicit values, bypassing marker parsing.
    #>
    [CmdletBinding()]
    param(
        [string]$MarkerPath,
        [string]$BaseUrl,
        [string]$ApiKey,
        [string]$WorkspacePath
    )

    if ($BaseUrl -and $ApiKey -and $WorkspacePath) {
        $script:McpBaseUrl = $BaseUrl.TrimEnd('/')
        $script:McpApiKey = $ApiKey
        $script:McpWorkspacePath = $WorkspacePath
    }
    else {
        if (-not $MarkerPath) {
            $MarkerPath = Find-McpMarkerFile
        }

        if (-not $MarkerPath -or -not (Test-Path -LiteralPath $MarkerPath)) {
            throw "AGENTS-README-FIRST.yaml not found. Provide -MarkerPath or explicit BaseUrl/ApiKey/WorkspacePath."
        }

        $marker = Get-Content -LiteralPath $MarkerPath -Raw

        $parsedBase = ([regex]::Match($marker, 'baseUrl:\s*(\S+)')).Groups[1].Value
        $parsedKey = ([regex]::Match($marker, 'apiKey:\s*(\S+)')).Groups[1].Value
        $parsedWorkspace = ([regex]::Match($marker, 'workspacePath:\s*(.+)')).Groups[1].Value.Trim()

        $script:McpBaseUrl = if ($BaseUrl) { $BaseUrl.TrimEnd('/') } else { $parsedBase.TrimEnd('/') }
        $script:McpApiKey = if ($ApiKey) { $ApiKey } else { $parsedKey }
        $script:McpWorkspacePath = if ($WorkspacePath) { $WorkspacePath } else { $parsedWorkspace }
    }

    if ([string]::IsNullOrWhiteSpace($script:McpBaseUrl) -or
        [string]::IsNullOrWhiteSpace($script:McpApiKey) -or
        [string]::IsNullOrWhiteSpace($script:McpWorkspacePath)) {
        throw "Failed to initialize MCP context: missing baseUrl, apiKey, or workspacePath."
    }

    $script:McpTransportUrl = "$($script:McpBaseUrl)/mcp-transport"
    $script:McpHeaders = @{
        'X-Api-Key' = $script:McpApiKey
        'Content-Type' = 'application/json'
        'X-Workspace-Path' = $script:McpWorkspacePath
    }

    try {
        $null = Invoke-RestMethod -Uri "$($script:McpBaseUrl)/health" -Method Get -TimeoutSec 5
    }
    catch {
        Write-Warning "Connected settings loaded, but health check failed: $_"
    }

    [pscustomobject]@{
        BaseUrl = $script:McpBaseUrl
        WorkspacePath = $script:McpWorkspacePath
        TransportUrl = $script:McpTransportUrl
    }
}

function Get-McpContextConnection {
    <#
    .SYNOPSIS
        Returns active module connection settings.
    .EXAMPLE
        Get-McpContextConnection

        Shows BaseUrl, WorkspacePath, and MCP transport URL currently in use.
    #>
    [CmdletBinding()]
    param()

    Assert-McpInitialized
    [pscustomobject]@{
        BaseUrl = $script:McpBaseUrl
        WorkspacePath = $script:McpWorkspacePath
        TransportUrl = $script:McpTransportUrl
    }
}

function Import-McpContextFolder {
    <#
    .SYNOPSIS
        Stages a local folder into workspace docs/external and optionally triggers sync.
    .DESCRIPTION
        Copies matching files into <workspace>/docs/external/<subfolder> so the existing
        sync pipeline can ingest them. Use -Sync to immediately run sync_run via MCP transport.
    .PARAMETER Path
        Source folder to ingest.
    .PARAMETER DestinationSubfolder
        Optional subfolder under docs/external. Defaults to ingest-<timestamp>.
    .PARAMETER Include
        Optional wildcard patterns. Defaults to common text/docs formats.
    .PARAMETER Sync
        Trigger sync_run tool after staging.
    .EXAMPLE
        Import-McpContextFolder -Path '.\seed-docs' -Sync

        Stages supported document files and runs sync immediately.
    .EXAMPLE
        Import-McpContextFolder -Path '.\exports' -DestinationSubfolder 'release-20260307' -Include '*.md','*.txt'

        Stages only markdown/text files to a deterministic destination folder without triggering sync.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$DestinationSubfolder,
        [string[]]$Include = @('*.md','*.txt','*.json','*.yml','*.yaml','*.csv','*.xml','*.html','*.htm','*.log'),
        [switch]$Sync
    )

    Assert-McpInitialized

    $source = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    if (-not (Test-Path -LiteralPath $source -PathType Container)) {
        throw "Path must be a directory: $Path"
    }

    $docsExternalRoot = Join-Path $script:McpWorkspacePath 'docs/external'
    New-Item -ItemType Directory -Path $docsExternalRoot -Force | Out-Null

    if (-not $DestinationSubfolder) {
        $DestinationSubfolder = "ingest-$((Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'))"
    }

    $destinationRoot = Join-Path $docsExternalRoot $DestinationSubfolder
    New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null

    $copied = 0
    foreach ($pattern in $Include) {
        Get-ChildItem -LiteralPath $source -File -Recurse -Filter $pattern -ErrorAction SilentlyContinue |
            ForEach-Object {
                $relative = [System.IO.Path]::GetRelativePath($source.Path, $_.FullName)
                $target = Join-Path $destinationRoot $relative
                $targetDir = Split-Path -Path $target -Parent
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
                Copy-Item -LiteralPath $_.FullName -Destination $target -Force
                $copied++
            }
    }

    if ($copied -eq 0) {
        Write-Warning "No files matched include patterns. Folder staged but empty for ingestion."
    }

    $syncResult = $null
    if ($Sync) {
        $syncResult = Invoke-McpSyncRun
    }

    [pscustomobject]@{
        SourcePath = $source.Path
        DestinationPath = $destinationRoot
        FilesCopied = $copied
        SyncTriggered = [bool]$Sync
        SyncResult = $syncResult
    }
}

function Import-McpContextUrl {
    <#
    .SYNOPSIS
        Ingests a URL directly via /mcpserver/context/ingest-website.
    .DESCRIPTION
        Calls the direct website ingestion endpoint with crawl and size controls.
        Uses SSE by default to stream progress in real time.
        Optionally triggers GraphRAG indexing after successful ingestion.
    .EXAMPLE
        Import-McpContextUrl -Url 'https://example.com' -MaxPages 1 -MaxDepth 0

        Ingests one page with no subpage crawl.
    .EXAMPLE
        Import-McpContextUrl -Url 'https://docs.example.com' -IncludeSubpages -MaxPages 25 -MaxDepth 2 -TriggerGraphRagIndex

        Ingests a bounded same-host crawl and then requests GraphRAG indexing.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Url,
        [switch]$IncludeSubpages,
        [int]$MaxPages = 20,
        [int]$MaxDepth = 1,
        [int]$MaxBytesPerPage = 262144,
        [switch]$ForceRefresh,
        [switch]$TriggerGraphRagIndex,
        [switch]$NoStream
    )

    Assert-McpInitialized

    $body = @{
        url = $Url
        includeSubpages = [bool]$IncludeSubpages
        maxPages = $MaxPages
        maxDepth = $MaxDepth
        maxBytesPerPage = $MaxBytesPerPage
        forceRefresh = [bool]$ForceRefresh
        triggerGraphRagIndex = [bool]$TriggerGraphRagIndex
    }

    if ($NoStream) {
        return Invoke-McpRestJson -Method Post -Path 'mcpserver/context/ingest-website' -Body $body
    }

    return Invoke-McpWebsiteIngestSse -Body $body
}

function Invoke-McpSyncRun {
    <#
    .SYNOPSIS
        Triggers full ingestion via MCP transport sync_run tool.
    .EXAMPLE
        Invoke-McpSyncRun

        Runs full ingestion for repo, session logs, and docs/external content.
    #>
    [CmdletBinding()]
    param()

    Assert-McpInitialized
    Invoke-McpTool -ToolName 'sync_run' -Arguments @{ workspacePath = $script:McpWorkspacePath }
}

function Get-McpSyncStatus {
    <#
    .SYNOPSIS
        Gets sync status via MCP transport sync_status tool.
    .EXAMPLE
        Get-McpSyncStatus

        Returns last run status and counters from the sync pipeline.
    #>
    [CmdletBinding()]
    param()

    Assert-McpInitialized
    Invoke-McpTool -ToolName 'sync_status' -Arguments @{ workspacePath = $script:McpWorkspacePath }
}

function Search-McpContext {
    <#
    .SYNOPSIS
        Queries context search endpoint.
    .DESCRIPTION
        Uses /mcpserver/context/search for hybrid retrieval with optional source filtering.
    .EXAMPLE
        Search-McpContext -Query 'oauth token refresh' -Limit 10

        Returns top context chunks and source keys.
    .EXAMPLE
        Search-McpContext -Query 'requirements mapping' -SourceType 'repo' -Limit 15

        Restricts results to a source type.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Query,
        [int]$Limit = 20,
        [string]$SourceType
    )

    Assert-McpInitialized

    $body = @{ query = $Query; limit = $Limit }
    if ($PSBoundParameters.ContainsKey('SourceType')) {
        $body.sourceType = $SourceType
    }

    Invoke-McpRestJson -Method Post -Path 'mcpserver/context/search' -Body $body
}

function Query-McpGraphRag {
    <#
    .SYNOPSIS
        Runs GraphRAG query endpoint.
    .DESCRIPTION
        Calls /mcpserver/graphrag/query and returns answer, citations, and optional chunks.
    .EXAMPLE
        Query-McpGraphRag -Query 'Summarize auth model changes' -Mode local -MaxChunks 8

        Executes a local GraphRAG query with chunk context included.
    .EXAMPLE
        Query-McpGraphRag -Query 'Show major entities' -NoContextChunks

        Returns high-level answer/citations without chunk payloads.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Query,
        [string]$Mode = 'local',
        [int]$MaxChunks = 20,
        [switch]$NoContextChunks
    )

    Assert-McpInitialized

    $body = @{
        query = $Query
        mode = $Mode
        maxChunks = $MaxChunks
        includeContextChunks = -not [bool]$NoContextChunks
    }

    Invoke-McpRestJson -Method Post -Path 'mcpserver/graphrag/query' -Body $body
}

function Get-McpGraphRagStatus {
    <#
    .SYNOPSIS
        Returns GraphRAG status.
    .EXAMPLE
        Get-McpGraphRagStatus

        Returns GraphRAG enabled/state/index metadata.
    #>
    [CmdletBinding()]
    param()

    Assert-McpInitialized
    Invoke-McpRestJson -Method Get -Path 'mcpserver/graphrag/status'
}

function Invoke-McpGraphRagIndex {
    <#
    .SYNOPSIS
        Starts GraphRAG indexing.
    .EXAMPLE
        Invoke-McpGraphRagIndex

        Starts a normal index operation.
    .EXAMPLE
        Invoke-McpGraphRagIndex -Force

        Forces rebuild semantics when supported.
    #>
    [CmdletBinding()]
    param(
        [switch]$Force
    )

    Assert-McpInitialized
    Invoke-McpRestJson -Method Post -Path 'mcpserver/graphrag/index' -Body @{ force = [bool]$Force }
}

function Invoke-McpTool {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ToolName,
        [Parameter(Mandatory)][hashtable]$Arguments
    )

    Assert-McpInitialized

    # Most servers tolerate repeated initialize; this keeps tool invocation stateless.
    $initPayload = @{
        jsonrpc = '2.0'
        id = 1
        method = 'initialize'
        params = @{
            protocolVersion = '2025-03-26'
            capabilities = @{}
            clientInfo = @{ name = 'McpContext.psm1'; version = '1.0.0' }
        }
    } | ConvertTo-Json -Depth 10

    $toolPayload = @{
        jsonrpc = '2.0'
        id = 2
        method = 'tools/call'
        params = @{
            name = $ToolName
            arguments = $Arguments
        }
    } | ConvertTo-Json -Depth 12

    $headers = @{
        'Accept' = 'application/json, text/event-stream'
        'Content-Type' = 'application/json'
    }

    $null = Invoke-RestMethod -Uri $script:McpTransportUrl -Method Post -Headers $headers -Body $initPayload
    $response = Invoke-RestMethod -Uri $script:McpTransportUrl -Method Post -Headers $headers -Body $toolPayload

    $jsonPayload = Extract-McpDataJson -Response $response
    if (-not $jsonPayload) {
        return $response
    }

    $parsed = $jsonPayload | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($null -eq $parsed) {
        return $jsonPayload
    }

    if ($parsed.result -and $parsed.result.content -and $parsed.result.content.Count -gt 0) {
        $text = $parsed.result.content[0].text
        if ($text) {
            $toolText = $text | ConvertFrom-Json -ErrorAction SilentlyContinue
            if ($null -ne $toolText) {
                return $toolText
            }
            return $text
        }
    }

    return $parsed
}

function Invoke-McpRestJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidateSet('Get','Post','Put','Delete')][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [object]$Body
    )

    Assert-McpInitialized

    $uri = "$($script:McpBaseUrl)/$Path"

    if ($Method -eq 'Get' -or $Method -eq 'Delete') {
        return Invoke-RestMethod -Uri $uri -Method $Method -Headers $script:McpHeaders
    }

    $json = if ($null -ne $Body) { $Body | ConvertTo-Json -Depth 20 } else { '{}' }
    return Invoke-RestMethod -Uri $uri -Method $Method -Headers $script:McpHeaders -Body $json
}

function Invoke-McpWebsiteIngestSse {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Body
    )

    Assert-McpInitialized

    $uri = "$($script:McpBaseUrl)/mcpserver/context/ingest-website/stream"
    $jsonBody = $Body | ConvertTo-Json -Depth 20

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    try {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, $uri)
        $request.Headers.Accept.ParseAdd('text/event-stream')
        $request.Headers.Add('X-Api-Key', $script:McpApiKey)
        $request.Headers.Add('X-Workspace-Path', $script:McpWorkspacePath)
        $request.Content = [System.Net.Http.StringContent]::new($jsonBody, [System.Text.Encoding]::UTF8, 'application/json')

        $response = $client.SendAsync($request, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        if (-not $response.IsSuccessStatusCode) {
            $errorBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            throw "SSE ingest request failed: HTTP $([int]$response.StatusCode) $errorBody"
        }

        $stream = $response.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
        $reader = [System.IO.StreamReader]::new($stream)

        $currentEvent = 'message'
        $dataLines = New-Object System.Collections.Generic.List[string]
        $finalResult = $null

        while (-not $reader.EndOfStream) {
            $line = $reader.ReadLine()
            if ($null -eq $line) {
                break
            }

            if ([string]::IsNullOrWhiteSpace($line)) {
                if ($dataLines.Count -gt 0) {
                    $payloadRaw = ($dataLines -join "`n")
                    $payload = $payloadRaw | ConvertFrom-Json -ErrorAction SilentlyContinue

                    if ($currentEvent -eq 'page' -and $payload -and $payload.urlResult) {
                        Write-Host ("[page {0}] {1} - {2}" -f $payload.pagesProcessed, $payload.urlResult.status, $payload.urlResult.url)
                    }
                    elseif ($currentEvent -eq 'persisted' -and $payload -and $payload.urlResult) {
                        Write-Host ("[persisted] docs={0} chunks={1} url={2}" -f $payload.documentsIngested, $payload.chunksWritten, $payload.urlResult.url)
                    }
                    elseif ($currentEvent -eq 'indexing' -and $payload) {
                        Write-Host ("[indexing] {0}" -f $payload.status)
                    }
                    elseif ($currentEvent -eq 'result') {
                        $finalResult = $payload
                    }
                    elseif ($currentEvent -eq 'started') {
                        Write-Host '[started] Website ingestion started.'
                    }

                    $dataLines.Clear()
                    $currentEvent = 'message'
                }

                continue
            }

            if ($line.StartsWith('event:', [System.StringComparison]::OrdinalIgnoreCase)) {
                $currentEvent = $line.Substring(6).Trim()
                continue
            }

            if ($line.StartsWith('data:', [System.StringComparison]::OrdinalIgnoreCase)) {
                $dataLines.Add($line.Substring(5).TrimStart())
                continue
            }
        }

        if ($null -eq $finalResult) {
            throw 'SSE ingest stream completed without a final result payload.'
        }

        return $finalResult
    }
    finally {
        if ($null -ne $client) {
            $client.Dispose()
        }
    }
}

function Extract-McpDataJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Response
    )

    $raw = [string]$Response
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }

    $lines = $raw -split "`r?`n"
    $dataLine = $lines | Where-Object { $_ -like 'data:*' } | Select-Object -Last 1
    if (-not $dataLine) {
        return $raw
    }

    return ($dataLine -replace '^data:\s*', '')
}

function Find-McpMarkerFile {
    [CmdletBinding()]
    param()

    $dir = (Get-Location).Path
    while ($dir) {
        $candidate = Join-Path $dir 'AGENTS-README-FIRST.yaml'
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }

        $parent = Split-Path -Path $dir -Parent
        if (-not $parent -or $parent -eq $dir) {
            break
        }
        $dir = $parent
    }

    return $null
}

function Assert-McpInitialized {
    if ([string]::IsNullOrWhiteSpace($script:McpBaseUrl) -or
        [string]::IsNullOrWhiteSpace($script:McpApiKey) -or
        [string]::IsNullOrWhiteSpace($script:McpWorkspacePath)) {
        throw 'Module not initialized. Run Initialize-McpContext first.'
    }
}

Export-ModuleMember -Function @(
    'Initialize-McpContext',
    'Get-McpContextConnection',
    'Import-McpContextFolder',
    'Import-McpContextUrl',
    'Invoke-McpSyncRun',
    'Get-McpSyncStatus',
    'Search-McpContext',
    'Query-McpGraphRag',
    'Get-McpGraphRagStatus',
    'Invoke-McpGraphRagIndex'
)
