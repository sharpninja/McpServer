<#
.SYNOPSIS
    MCP Session Log PowerShell module — cmdlets for the /mcp/sessionlog API.

.DESCRIPTION
    Provides cmdlets to create, update, query, and manage session logs on an MCP Context Server.
    Automatically reads connection details from the AGENTS-README-FIRST.yaml marker file.

.NOTES
    Usage:  Import-Module ./McpSession.psm1
            Initialize-McpSession                          # reads marker, sets connection
            $s = New-McpSessionLog -Title "My session"     # creates session
            Add-McpSessionEntry -Session $s -QueryTitle "Fix bug" -QueryText "Fix the auth bug" -Status in_progress
            Send-McpDialog -Session $s -RequestId req-001 -Content "Analyzing the issue..." -Category reasoning
            Update-McpSessionLog -Session $s               # pushes to server
#>

# ─── Module state ────────────────────────────────────────────────────────────
$script:McpBaseUrl       = $null
$script:McpApiKey        = $null
$script:McpWorkspacePath = $null
$script:McpHeaders       = @{}

# ─── Connection ──────────────────────────────────────────────────────────────

function Initialize-McpSession {
    <#
    .SYNOPSIS  Read the AGENTS-README-FIRST.yaml marker and configure the module connection.
    .PARAMETER MarkerPath  Path to the marker file. Defaults to searching upward from the current directory.
    .PARAMETER BaseUrl     Override the base URL instead of reading from the marker.
    .PARAMETER ApiKey      Override the API key instead of reading from the marker.
    #>
    [CmdletBinding()]
    param(
        [string]$MarkerPath,
        [string]$BaseUrl,
        [string]$ApiKey
    )

    if ($BaseUrl -and $ApiKey) {
        $script:McpBaseUrl = $BaseUrl.TrimEnd('/')
        $script:McpApiKey  = $ApiKey
    } else {
        if (-not $MarkerPath) {
            $dir = (Get-Location).Path
            while ($dir) {
                $candidate = Join-Path $dir "AGENTS-README-FIRST.yaml"
                if (Test-Path $candidate) { $MarkerPath = $candidate; break }
                $parent = Split-Path $dir -Parent
                if (-not $parent -or $parent -eq $dir) { break }
                $dir = $parent
            }
        }
        if (-not $MarkerPath -or -not (Test-Path $MarkerPath)) {
            throw "AGENTS-README-FIRST.yaml not found. Provide -MarkerPath, or run from within a workspace."
        }
        $content = Get-Content $MarkerPath -Raw
        $script:McpBaseUrl       = ([regex]::Match($content, 'baseUrl:\s*(\S+)')).Groups[1].Value
        $script:McpApiKey        = ([regex]::Match($content, 'apiKey:\s*(\S+)')).Groups[1].Value
        $script:McpWorkspacePath = ([regex]::Match($content, 'workspacePath:\s*(.+)')).Groups[1].Value.Trim()
    }

    $script:McpHeaders = @{
        "X-Api-Key"        = $script:McpApiKey
        "Content-Type"     = "application/json"
        "X-Workspace-Path" = $script:McpWorkspacePath
    }

    # Verify connectivity
    try {
        $health = Invoke-RestMethod -Uri "$($script:McpBaseUrl)/health" -TimeoutSec 5
        Write-Host "Connected to MCP server at $($script:McpBaseUrl) — status: $($health.status)" -ForegroundColor Green
    } catch {
        Write-Warning "MCP server at $($script:McpBaseUrl) is not responding: $_"
    }
}

# ─── Session object ──────────────────────────────────────────────────────────

function New-McpSessionLog {
    <#
    .SYNOPSIS  Create a new session log object and POST it to the server.
    .PARAMETER SourceType  Agent identifier (e.g. "Copilot", "Cline", "Cursor").
    .PARAMETER SessionId   Stable session ID prefixed with agent name. Auto-generated if omitted.
    .PARAMETER Title       Brief session summary.
    .PARAMETER Model       AI model name (e.g. "claude-sonnet-4-20250514").
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SourceType,
        [string]$SessionId,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Model
    )
    Assert-Initialized

    if (-not $SessionId) {
        $SessionId = "$SourceType-$(New-Guid)"
    }

    $now = (Get-Date).ToUniversalTime().ToString("o")
    $session = [PSCustomObject]@{
        sourceType  = $SourceType
        sessionId   = $SessionId
        title       = $Title
        model       = $Model
        started     = $now
        lastUpdated = $now
        status      = "in_progress"
        entries     = [System.Collections.Generic.List[object]]::new()
    }

    Push-SessionLog $session
    return $session
}

function Update-McpSessionLog {
    <#
    .SYNOPSIS  Push the current session log state to the server.
    .PARAMETER Session  The session object returned by New-McpSessionLog.
    .PARAMETER Status   Optionally change status to "completed".
    .PARAMETER Title    Optionally update the title.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Session,
        [ValidateSet("in_progress","completed")][string]$Status,
        [string]$Title
    )
    Assert-Initialized

    $Session.lastUpdated = (Get-Date).ToUniversalTime().ToString("o")
    if ($Status) { $Session.status = $Status }
    if ($Title)  { $Session.title  = $Title }

    Push-SessionLog $Session
}

function Get-McpSessionLog {
    <#
    .SYNOPSIS  Query recent session logs from the server.
    .PARAMETER Limit   Number of sessions to return (default 5).
    .PARAMETER Offset  Pagination offset.
    #>
    [CmdletBinding()]
    param(
        [int]$Limit = 5,
        [int]$Offset = 0
    )
    Assert-Initialized
    $uri = "$($script:McpBaseUrl)/mcp/sessionlog?limit=$Limit&offset=$Offset"
    return Invoke-RestMethod -Uri $uri -Headers $script:McpHeaders
}

# ─── Entries ─────────────────────────────────────────────────────────────────

function Add-McpSessionEntry {
    <#
    .SYNOPSIS  Add a request entry to the session and push to server.
    .PARAMETER Session        The session object.
    .PARAMETER RequestId      Unique ID for this request. Auto-generated if omitted.
    .PARAMETER QueryTitle     Short summary of the query.
    .PARAMETER QueryText      Full user query or task description.
    .PARAMETER Interpretation Your understanding of what was asked.
    .PARAMETER Response       Your response text.
    .PARAMETER Status         "in_progress" or "completed".
    .PARAMETER Model          Model used for this entry. Defaults to session model.
    .PARAMETER Tags           Array of tags (e.g. "refactor", "bugfix").
    .PARAMETER ContextList    Array of files or resources referenced.
    .PARAMETER Push           If set, immediately push to server. Default: true.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Session,
        [string]$RequestId,
        [Parameter(Mandatory)][string]$QueryTitle,
        [Parameter(Mandatory)][string]$QueryText,
        [string]$Interpretation = "",
        [string]$Response = "",
        [ValidateSet("in_progress","completed")][string]$Status = "in_progress",
        [string]$Model,
        [string[]]$Tags = @(),
        [string[]]$ContextList = @(),
        [switch]$NoPush
    )

    if (-not $RequestId) { $RequestId = "req-$('{0:D3}' -f ($Session.entries.Count + 1))" }
    if (-not $Model) { $Model = $Session.model }

    $entry = [PSCustomObject]@{
        requestId              = $RequestId
        timestamp              = (Get-Date).ToUniversalTime().ToString("o")
        queryText              = $QueryText
        queryTitle             = $QueryTitle
        response               = $Response
        interpretation         = $Interpretation
        status                 = $Status
        model                  = $Model
        tags                   = $Tags
        contextList            = $ContextList
        designDecisions        = [System.Collections.Generic.List[string]]::new()
        requirementsDiscovered = [System.Collections.Generic.List[string]]::new()
        filesModified          = [System.Collections.Generic.List[string]]::new()
        blockers               = [System.Collections.Generic.List[string]]::new()
        actions                = [System.Collections.Generic.List[object]]::new()
        processingDialog       = [System.Collections.Generic.List[object]]::new()
    }

    $Session.entries.Add($entry)

    if (-not $NoPush) {
        Update-McpSessionLog -Session $Session
    }
    return $entry
}

function Set-McpSessionEntry {
    <#
    .SYNOPSIS  Update fields on an existing entry and optionally push.
    .PARAMETER Entry     The entry object returned by Add-McpSessionEntry.
    .PARAMETER Session   The parent session object.
    .PARAMETER Response  Updated response text.
    .PARAMETER Status    Updated status.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Entry,
        [PSCustomObject]$Session,
        [string]$Response,
        [ValidateSet("in_progress","completed")][string]$Status,
        [string[]]$FilesModified,
        [string[]]$DesignDecisions,
        [switch]$NoPush
    )

    if ($Response)         { $Entry.response = $Response }
    if ($Status)           { $Entry.status   = $Status }
    if ($FilesModified)    { foreach ($f in $FilesModified)    { $Entry.filesModified.Add($f) } }
    if ($DesignDecisions)  { foreach ($d in $DesignDecisions)  { $Entry.designDecisions.Add($d) } }

    if ($Session -and -not $NoPush) {
        Update-McpSessionLog -Session $Session
    }
}

# ─── Actions ─────────────────────────────────────────────────────────────────

function Add-McpAction {
    <#
    .SYNOPSIS  Add an action to a session entry.
    .PARAMETER Entry        The entry object.
    .PARAMETER Description  What was done.
    .PARAMETER Type         Action type: edit, create, delete, commit, design_decision, etc.
    .PARAMETER FilePath     Affected file path (empty string if N/A).
    .PARAMETER Status       "completed", "in_progress", or "failed".
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Entry,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][ValidateSet(
            "edit","create","delete","design_decision","commit",
            "pr_comment","issue_comment","web_reference",
            "dependency_add","license_violation","origin_violation",
            "origin_review","entity_violation","copilot_invocation","policy_change"
        )][string]$Type,
        [string]$FilePath = "",
        [ValidateSet("completed","in_progress","failed")][string]$Status = "completed"
    )

    $action = [PSCustomObject]@{
        order       = $Entry.actions.Count + 1
        description = $Description
        type        = $Type
        status      = $Status
        filePath    = $FilePath
    }
    $Entry.actions.Add($action)
    return $action
}

# ─── Dialog ──────────────────────────────────────────────────────────────────

function Send-McpDialog {
    <#
    .SYNOPSIS  Post reasoning dialog items to the session log dialog endpoint.
    .PARAMETER Session    The session object.
    .PARAMETER RequestId  The request entry ID.
    .PARAMETER Content    The reasoning text or observation.
    .PARAMETER Role       "model", "tool", "system", or "user".
    .PARAMETER Category   "reasoning", "tool_call", "tool_result", "observation", or "decision".
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Session,
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$Content,
        [ValidateSet("model","tool","system","user")][string]$Role = "model",
        [ValidateSet("reasoning","tool_call","tool_result","observation","decision")][string]$Category = "reasoning"
    )
    Assert-Initialized

    $item = @{
        timestamp = (Get-Date).ToUniversalTime().ToString("o")
        role      = $Role
        content   = $Content
        category  = $Category
    }

    $uri = "$($script:McpBaseUrl)/mcp/sessionlog/$($Session.sourceType)/$($Session.sessionId)/$RequestId/dialog"
    $body = ConvertTo-Json @($item) -Depth 5
    Invoke-RestMethod -Uri $uri -Method Post -Headers $script:McpHeaders -Body $body | Out-Null
}

# ─── Helpers ─────────────────────────────────────────────────────────────────

function Assert-Initialized {
    if (-not $script:McpBaseUrl) {
        throw "MCP session not initialized. Call Initialize-McpSession first."
    }
}

function Push-SessionLog {
    param([PSCustomObject]$Session)
    $body = $Session | ConvertTo-Json -Depth 10
    Invoke-RestMethod -Uri "$($script:McpBaseUrl)/mcp/sessionlog" -Method Post -Headers $script:McpHeaders -Body $body | Out-Null
}

# ─── Exports ─────────────────────────────────────────────────────────────────
Export-ModuleMember -Function @(
    'Initialize-McpSession',
    'New-McpSessionLog',
    'Update-McpSessionLog',
    'Get-McpSessionLog',
    'Add-McpSessionEntry',
    'Set-McpSessionEntry',
    'Add-McpAction',
    'Send-McpDialog'
)
