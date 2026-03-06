<#
.SYNOPSIS
    MCP Session Log PowerShell module — cmdlets for the /mcpserver/sessionlog API.

.DESCRIPTION
    Provides cmdlets to create, update, query, and manage session logs on an MCP Context Server.
    Automatically reads connection details from the AGENTS-README-FIRST.yaml marker file.

.NOTES
    Usage:  Import-Module ./McpSession.psm1
            Initialize-McpSession                          # reads marker, sets connection
            $s = New-McpSessionLog -Title "My session"     # creates session
            Add-McpSessionTurn -Session $s -QueryTitle "Fix bug" -QueryText "Fix the auth bug" -Status in_progress
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
        entryCount  = 0
        totalTokens = 0
        entries     = [System.Collections.Generic.List[object]]::new()
    }
    # Keep legacy "turns" alias in-memory for older scripts/tests.
    $session | Add-Member -NotePropertyName turns -NotePropertyValue $session.entries -Force

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

    $turns = Get-McpSessionTurnList -Session $Session
    $Session.lastUpdated = (Get-Date).ToUniversalTime().ToString("o")
    $Session.entryCount = $turns.Count
    $totalTokens = @($turns | ForEach-Object {
        if ($_.PSObject.Properties.Name -contains "tokenCount" -and $null -ne $_.tokenCount) { [int]$_.tokenCount } else { 0 }
    } | Measure-Object -Sum).Sum
    $Session.totalTokens = if ($null -eq $totalTokens) { 0 } else { [int]$totalTokens }
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
    $uri = "$($script:McpBaseUrl)/mcpserver/sessionlog?limit=$Limit&offset=$Offset"
    return Invoke-RestMethod -Uri $uri -Headers $script:McpHeaders
}

# ─── Turns ───────────────────────────────────────────────────────────────────

function Get-McpSessionTurnList {
    <#
    .SYNOPSIS  Ensure the session object exposes a "turns" list and return it.
    .PARAMETER Session  The session object.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Session
    )

    if ($Session.PSObject.Properties.Name -contains "entries") {
        if (-not ($Session.PSObject.Properties.Name -contains "turns")) {
            $Session | Add-Member -NotePropertyName turns -NotePropertyValue $Session.entries -Force
        } else {
            $Session.turns = $Session.entries
        }
        return ,$Session.entries
    }

    $entries = [System.Collections.Generic.List[object]]::new()
    if ($Session.PSObject.Properties.Name -contains "turns") {
        foreach ($item in @($Session.turns)) {
            [void]$entries.Add($item)
        }
    }

    $Session | Add-Member -NotePropertyName entries -NotePropertyValue $entries -Force
    $Session | Add-Member -NotePropertyName turns -NotePropertyValue $entries -Force
    return ,$Session.entries
}

function Add-McpSessionTurn {
    <#
    .SYNOPSIS  Add a request turn to the session and push to server.
    .PARAMETER Session        The session object.
    .PARAMETER RequestId      Unique ID for this request. Auto-generated if omitted.
    .PARAMETER QueryTitle     Short summary of the query.
    .PARAMETER QueryText      Full user query or task description.
    .PARAMETER Interpretation Your understanding of what was asked.
    .PARAMETER Response       Your response text.
    .PARAMETER Status         "in_progress" or "completed".
    .PARAMETER Model          Model used for this turn. Defaults to session model.
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
        [Nullable[int]]$TokenCount,
        [string]$ModelProvider = "",
        [string]$FailureNote = "",
        [Nullable[double]]$Score,
        [Nullable[bool]]$IsPremium,
        [string[]]$DesignDecisions = @(),
        [string[]]$RequirementsDiscovered = @(),
        [string[]]$FilesModified = @(),
        [string[]]$Blockers = @(),
        [switch]$NoPush
    )

    $turns = Get-McpSessionTurnList -Session $Session
    if (-not $RequestId) { $RequestId = "req-$('{0:D3}' -f ($turns.Count + 1))" }
    if (-not $Model) { $Model = $Session.model }

    $turn = [PSCustomObject]@{
        requestId              = $RequestId
        timestamp              = (Get-Date).ToUniversalTime().ToString("o")
        queryText              = $QueryText
        queryTitle             = $QueryTitle
        response               = $Response
        interpretation         = $Interpretation
        status                 = $Status
        model                  = $Model
        modelProvider          = $ModelProvider
        tokenCount             = if ($TokenCount.HasValue) { $TokenCount.Value } else { $null }
        failureNote            = if ([string]::IsNullOrWhiteSpace($FailureNote)) { $null } else { $FailureNote }
        score                  = if ($Score.HasValue) { $Score.Value } else { $null }
        isPremium              = if ($IsPremium.HasValue) { $IsPremium.Value } else { $null }
        tags                   = [System.Collections.Generic.List[string]]::new($Tags)
        contextList            = [System.Collections.Generic.List[string]]::new($ContextList)
        designDecisions        = [System.Collections.Generic.List[string]]::new($DesignDecisions)
        requirementsDiscovered = [System.Collections.Generic.List[string]]::new($RequirementsDiscovered)
        filesModified          = [System.Collections.Generic.List[string]]::new($FilesModified)
        blockers               = [System.Collections.Generic.List[string]]::new($Blockers)
        actions                = [System.Collections.Generic.List[object]]::new()
        processingDialog       = [System.Collections.Generic.List[object]]::new()
    }

    [void]$turns.Add($turn)

    if (-not $NoPush) {
        Update-McpSessionLog -Session $Session
    }
    return $turn
}

function Set-McpSessionTurn {
    <#
    .SYNOPSIS  Update fields on an existing turn and optionally push.
    .PARAMETER Turn      The turn object returned by Add-McpSessionTurn.
    .PARAMETER Session   The parent session object.
    .PARAMETER Response  Updated response text.
    .PARAMETER Status    Updated status.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Turn,
        [PSCustomObject]$Session,
        [string]$Response,
        [string]$Interpretation,
        [ValidateSet("in_progress","completed")][string]$Status,
        [Nullable[int]]$TokenCount,
        [string]$ModelProvider,
        [string]$FailureNote,
        [Nullable[double]]$Score,
        [Nullable[bool]]$IsPremium,
        [string[]]$Tags,
        [string[]]$ContextList,
        [string[]]$FilesModified,
        [string[]]$DesignDecisions,
        [string[]]$RequirementsDiscovered,
        [string[]]$Blockers,
        [switch]$NoPush
    )

    if ($Response)        { $Turn.response = $Response }
    if ($Interpretation)  { $Turn.interpretation = $Interpretation }
    if ($Status)          { $Turn.status = $Status }
    if ($TokenCount.HasValue) { $Turn.tokenCount = $TokenCount.Value }
    if ($ModelProvider)   { $Turn.modelProvider = $ModelProvider }
    if ($FailureNote)     { $Turn.failureNote = $FailureNote }
    if ($Score.HasValue)  { $Turn.score = $Score.Value }
    if ($IsPremium.HasValue) { $Turn.isPremium = $IsPremium.Value }
    if ($Tags) {
        $list = Get-McpSessionTurnStringList -Turn $Turn -Field "tags"
        foreach ($t in $Tags) { $list.Add($t) }
    }
    if ($ContextList) {
        $list = Get-McpSessionTurnStringList -Turn $Turn -Field "contextList"
        foreach ($c in $ContextList) { $list.Add($c) }
    }
    if ($FilesModified) {
        $list = Get-McpSessionTurnStringList -Turn $Turn -Field "filesModified"
        foreach ($f in $FilesModified) { $list.Add($f) }
    }
    if ($DesignDecisions) {
        $list = Get-McpSessionTurnStringList -Turn $Turn -Field "designDecisions"
        foreach ($d in $DesignDecisions) { $list.Add($d) }
    }
    if ($RequirementsDiscovered) {
        $list = Get-McpSessionTurnStringList -Turn $Turn -Field "requirementsDiscovered"
        foreach ($r in $RequirementsDiscovered) { $list.Add($r) }
    }
    if ($Blockers) {
        $list = Get-McpSessionTurnStringList -Turn $Turn -Field "blockers"
        foreach ($b in $Blockers) { $list.Add($b) }
    }

    if ($Session -and -not $NoPush) {
        Update-McpSessionLog -Session $Session
    }
}

# ─── Actions ─────────────────────────────────────────────────────────────────

function Add-McpAction {
    <#
    .SYNOPSIS  Add an action to a session turn.
    .PARAMETER Turn         The turn object.
    .PARAMETER Description  What was done.
    .PARAMETER Type         Action type: edit, create, delete, commit, design_decision, etc.
    .PARAMETER FilePath     Affected file path (empty string if N/A).
    .PARAMETER Status       "completed", "in_progress", or "failed".
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Turn,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][ValidateSet(
            "edit","create","delete","design_decision","commit",
            "pr_comment","issue_comment","web_reference",
            "dependency_add","license_violation","origin_violation",
            "origin_review","entity_violation","copilot_invocation","policy_change"
        )][string]$Type,
        [PSCustomObject]$Session,
        [string]$FilePath = "",
        [ValidateSet("completed","in_progress","failed")][string]$Status = "completed",
        [switch]$NoPush
    )

    $action = [PSCustomObject]@{
        order       = $Turn.actions.Count + 1
        description = $Description
        type        = $Type
        status      = $Status
        filePath    = $FilePath
    }
    $Turn.actions.Add($action)
    if ($Session -and -not $NoPush) {
        Update-McpSessionLog -Session $Session
    }
    return $action
}

function Add-McpTurnDetail {
    <#
    .SYNOPSIS  Append detail text to a turn list field and optionally push.
    .PARAMETER Turn      The turn object.
    .PARAMETER Field     One of tags/contextList/designDecisions/requirementsDiscovered/filesModified/blockers.
    .PARAMETER Value     Detail string to append.
    .PARAMETER Session   Optional parent session for immediate persistence.
    .PARAMETER NoPush    When set, do not push even when Session is provided.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][PSCustomObject]$Turn,
        [Parameter(Mandatory)][ValidateSet("tags","contextList","designDecisions","requirementsDiscovered","filesModified","blockers")][string]$Field,
        [Parameter(Mandatory)][string]$Value,
        [PSCustomObject]$Session,
        [switch]$NoPush
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $list = Get-McpSessionTurnStringList -Turn $Turn -Field $Field
    $list.Add($Value)
    if ($Session -and -not $NoPush) {
        Update-McpSessionLog -Session $Session
    }
}

# ─── Dialog ──────────────────────────────────────────────────────────────────

function Send-McpDialog {
    <#
    .SYNOPSIS  Post reasoning dialog items to the session log dialog endpoint.
    .PARAMETER Session    The session object.
    .PARAMETER RequestId  The request turn ID.
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

    $uri = "$($script:McpBaseUrl)/mcpserver/sessionlog/$($Session.sourceType)/$($Session.sessionId)/$RequestId/dialog"
    $body = ConvertTo-Json @($item) -Depth 5
    Invoke-RestMethod -Uri $uri -Method Post -Headers $script:McpHeaders -Body $body | Out-Null
}

# ─── Helpers ─────────────────────────────────────────────────────────────────

function Assert-Initialized {
    if (-not $script:McpBaseUrl) {
        throw "MCP session not initialized. Call Initialize-McpSession first."
    }
}

function Get-McpSessionTurnStringList {
    param(
        [Parameter(Mandatory)][PSCustomObject]$Turn,
        [Parameter(Mandatory)][string]$Field
    )

    $current = $Turn.$Field
    if ($current -is [System.Collections.Generic.List[string]]) {
        return ,$current
    }

    $list = [System.Collections.Generic.List[string]]::new()
    foreach ($value in @($current)) {
        if ($null -ne $value) {
            [void]$list.Add([string]$value)
        }
    }
    $Turn.$Field = $list
    return ,$list
}

function Push-SessionLog {
    param([PSCustomObject]$Session)
    $turns = Get-McpSessionTurnList -Session $Session
    $payload = [ordered]@{}
    foreach ($property in $Session.PSObject.Properties) {
        if ($property.Name -ne "turns") {
            $payload[$property.Name] = $property.Value
        }
    }
    $payload.entries = $turns
    $payload.entryCount = $turns.Count
    $totalTokens = @($turns | ForEach-Object {
        if ($_.PSObject.Properties.Name -contains "tokenCount" -and $null -ne $_.tokenCount) { [int]$_.tokenCount } else { 0 }
    } | Measure-Object -Sum).Sum
    $payload.totalTokens = if ($null -eq $totalTokens) { 0 } else { [int]$totalTokens }
    $body = $payload | ConvertTo-Json -Depth 12
    Invoke-RestMethod -Uri "$($script:McpBaseUrl)/mcpserver/sessionlog" -Method Post -Headers $script:McpHeaders -Body $body | Out-Null
}

# ─── Exports ─────────────────────────────────────────────────────────────────
Export-ModuleMember -Function @(
    'Initialize-McpSession',
    'New-McpSessionLog',
    'Update-McpSessionLog',
    'Get-McpSessionLog',
    'Add-McpSessionTurn',
    'Set-McpSessionTurn',
    'Add-McpAction',
    'Add-McpTurnDetail',
    'Send-McpDialog'
)
