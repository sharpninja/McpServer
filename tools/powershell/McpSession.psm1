<#
.SYNOPSIS
    MCP Session Log PowerShell module - cmdlets for the /mcpserver/sessionlog API.

.DESCRIPTION
    Provides cmdlets to create, update, query, and manage session logs on an MCP Context Server.
    Automatically reads connection details from the AGENTS-README-FIRST.yaml marker file.
    For compaction workflows, persist the session log immediately before compaction and again after compaction to record the resulting context state.

.NOTES
    Usage:  Import-Module ./McpSession.psm1
            Initialize-McpSession -Agent "Copilotcli" -Model "gpt-5.3-codex"  # reads marker, sets connection, persists/reuses session slug
            $s = New-McpSessionLog -Title "My session"     # creates session
            Add-McpSessionTurn -Session $s -QueryTitle "Fix bug" -QueryText "Fix the auth bug" -Status in_progress
            Send-McpDialog -Session $s -RequestId req-20260304T113901Z-analysis -Content "Analyzing the issue..." -Category reasoning
            Update-McpSessionLog -Session $s               # pushes to server
#>

# ─── Module state ────────────────────────────────────────────────────────────
$script:McpBaseUrl       = $null
$script:McpApiKey        = $null
$script:McpWorkspacePath = $null
$script:McpHeaders       = @{}
$script:McpSessionAgent  = $null
$script:McpSessionModel  = $null
$script:McpSessionSlug   = $null

# ─── Connection ──────────────────────────────────────────────────────────────

function Initialize-McpSession {
    <#
    .SYNOPSIS  Read the AGENTS-README-FIRST.yaml marker and configure the module connection.
    .PARAMETER Agent       Canonical agent prefix used in the session slug.
    .PARAMETER Model       Model identifier used in the session slug.
    .PARAMETER MarkerPath  Path to the marker file. Defaults to searching upward from the current directory.
    .PARAMETER BaseUrl     Override the base URL instead of reading from the marker.
    .PARAMETER ApiKey      Override the API key instead of reading from the marker.
    .OUTPUTS               String session slug persisted/reused in .mcpServer/session.yaml.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Agent,
        [Parameter(Mandatory)][string]$Model,
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
        Write-Host "Connected to MCP server at $($script:McpBaseUrl) - status: $($health.status)" -ForegroundColor Green
    } catch {
        Write-Warning "MCP server at $($script:McpBaseUrl) is not responding: $_"
    }

    $script:McpSessionAgent = $Agent.Trim()
    $script:McpSessionModel = $Model.Trim()
    $script:McpSessionSlug = Initialize-McpSessionSlugState -Agent $script:McpSessionAgent -Model $script:McpSessionModel
    return $script:McpSessionSlug
}

# ─── Session object ──────────────────────────────────────────────────────────

function ConvertTo-McpSessionSlugToken {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Model is required."
    }

    $token = $Value.Trim().ToLowerInvariant()
    $token = $token -replace '[^a-z0-9]+', '-'
    $token = $token.Trim('-')

    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "Model '$Value' did not contain any valid slug characters."
    }

    return $token
}

function New-McpSessionLogSlug {
    <#
    .SYNOPSIS  Build a canonical session ID slug in the form <agent>-<timestamp>-<model>.
    .PARAMETER Agent         Agent/source prefix (must match ^[A-Z][A-Za-z0-9]*$).
    .PARAMETER Model         Model identifier used to build the suffix slug.
    .PARAMETER TimestampUtc  Optional UTC timestamp; defaults to now.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Agent,
        [Parameter(Mandatory)][string]$Model,
        [datetime]$TimestampUtc = (Get-Date).ToUniversalTime()
    )

    if ([string]::IsNullOrWhiteSpace($Agent)) {
        throw "Agent is required."
    }

    $agentToken = $Agent.Trim()
    if ($agentToken -cnotmatch '^[A-Z][A-Za-z0-9]*$') {
        throw "Agent '$Agent' must match ^[A-Z][A-Za-z0-9]*$ to build a valid SessionId prefix."
    }

    $modelToken = ConvertTo-McpSessionSlugToken -Value $Model
    $stamp = $TimestampUtc.ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    return "$agentToken-$stamp-$modelToken"
}

function New-McpRequestId {
    <#
    .SYNOPSIS  Build a canonical request ID in the form req-<timestamp>-<slug>.
    .PARAMETER SuffixSeed    Raw suffix text used to build the request-id slug.
    .PARAMETER TimestampUtc  Optional UTC timestamp; defaults to now.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SuffixSeed,
        [datetime]$TimestampUtc = (Get-Date).ToUniversalTime()
    )

    $suffixToken = ConvertTo-McpSessionSlugToken -Value $SuffixSeed
    $stamp = $TimestampUtc.ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    return "req-$stamp-$suffixToken"
}

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
        if (-not [string]::IsNullOrWhiteSpace($script:McpSessionSlug)) {
            $SessionId = $script:McpSessionSlug
        } else {
            $SessionId = New-McpSessionLogSlug -Agent $SourceType -Model $Model
        }
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
    Save-McpSessionState -Session $session
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
        [PSCustomObject]$Session,
        [ValidateSet("in_progress","completed")][string]$Status,
        [string]$Title
    )
    Assert-Initialized
    $Session = Resolve-McpSession -Session $Session

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

    if ($Session.status -eq "completed") {
        Remove-McpSessionStateFile
    } else {
        Save-McpSessionState -Session $Session
    }
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
        $entriesValue = $Session.entries
        if (-not ($entriesValue -is [System.Collections.Generic.List[object]])) {
            $entriesList = [System.Collections.Generic.List[object]]::new()
            foreach ($item in @($entriesValue)) {
                [void]$entriesList.Add($item)
            }
            $Session.entries = $entriesList
        }

        if (-not ($Session.PSObject.Properties.Name -contains "turns")) {
            $Session | Add-Member -NotePropertyName turns -NotePropertyValue $Session.entries -Force
        } else {
            $Session.turns = $Session.entries
        }

        foreach ($turn in @($Session.entries)) {
            Normalize-McpSessionTurnCollections -Turn $turn
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

    foreach ($turn in @($Session.entries)) {
        Normalize-McpSessionTurnCollections -Turn $turn
    }

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
        [PSCustomObject]$Session,
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

    $Session = Resolve-McpSession -Session $Session

    $turns = Get-McpSessionTurnList -Session $Session
    if (-not $RequestId) {
        $suffixSeed = $QueryTitle
        if ([string]::IsNullOrWhiteSpace($suffixSeed)) { $suffixSeed = $QueryText }
        if ([string]::IsNullOrWhiteSpace($suffixSeed)) { $suffixSeed = "turn" }

        $RequestId = New-McpRequestId -SuffixSeed $suffixSeed
        if (@($turns | Where-Object { $_.requestId -eq $RequestId }).Count -gt 0) {
            $RequestId = New-McpRequestId -SuffixSeed ("{0}-{1:D3}" -f $suffixSeed, ($turns.Count + 1))
        }
    }
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
    } else {
        Save-McpSessionState -Session $Session
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

    $resolvedSession = Resolve-McpSession -Session $Session -AllowMissing
    if (-not $Session -and $resolvedSession -and $Turn.PSObject.Properties.Name -contains 'requestId') {
        $candidateTurns = Get-McpSessionTurnList -Session $resolvedSession
        $match = @($candidateTurns | Where-Object { $_.requestId -eq $Turn.requestId } | Select-Object -First 1)
        if ($match.Count -gt 0) {
            $Turn = $match[0]
        }
    }

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

    if ($resolvedSession -and -not $NoPush) {
        Update-McpSessionLog -Session $resolvedSession
    } elseif ($resolvedSession) {
        Save-McpSessionState -Session $resolvedSession
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

    $actions = Get-McpSessionTurnObjectList -Turn $Turn -Field "actions"
    $action = [PSCustomObject]@{
        order       = $actions.Count + 1
        description = $Description
        type        = $Type
        status      = $Status
        filePath    = $FilePath
    }
    $actions.Add($action)
    $resolvedSession = Resolve-McpSession -Session $Session -AllowMissing
    if ($resolvedSession -and -not $NoPush) {
        Update-McpSessionLog -Session $resolvedSession
    } elseif ($resolvedSession) {
        Save-McpSessionState -Session $resolvedSession
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
    $resolvedSession = Resolve-McpSession -Session $Session -AllowMissing
    if ($resolvedSession -and -not $NoPush) {
        Update-McpSessionLog -Session $resolvedSession
    } elseif ($resolvedSession) {
        Save-McpSessionState -Session $resolvedSession
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
        [PSCustomObject]$Session,
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$Content,
        [ValidateSet("model","tool","system","user")][string]$Role = "model",
        [ValidateSet("reasoning","tool_call","tool_result","observation","decision")][string]$Category = "reasoning"
    )
    Assert-Initialized
    $Session = Resolve-McpSession -Session $Session

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
        throw "MCP session not initialized. Call Initialize-McpSession -Agent <Agent> -Model <Model> first."
    }
}

function Get-McpSessionStatePath {
    $workspacePath = $script:McpWorkspacePath
    if ([string]::IsNullOrWhiteSpace($workspacePath)) {
        $workspacePath = (Get-Location).Path
    }

    $stateDir = Join-Path $workspacePath ".mcpServer"
    return Join-Path $stateDir "session.yaml"
}

function Get-McpSessionState {
    $statePath = Get-McpSessionStatePath
    if (-not (Test-Path -LiteralPath $statePath)) {
        return $null
    }

    try {
        $raw = Get-Content -LiteralPath $statePath -Raw -ErrorAction Stop
        if ([string]::IsNullOrWhiteSpace($raw)) {
            return $null
        }

        return $raw | ConvertFrom-Json -Depth 50
    } catch {
        Write-Warning "Failed to parse session state file '$statePath': $_"
        return $null
    }
}

function Save-McpSessionState {
    param([PSCustomObject]$Session)

    $statePath = Get-McpSessionStatePath
    $stateDir = Split-Path -Parent $statePath
    New-Item -ItemType Directory -Path $stateDir -Force | Out-Null

    $existing = Get-McpSessionState
    $slugGeneratedAt = $existing.slugGeneratedAt
    if ([string]::IsNullOrWhiteSpace($slugGeneratedAt)) {
        $slugGeneratedAt = (Get-Date).ToUniversalTime().ToString('o')
    }

    $state = [PSCustomObject]@{
        apiKey = $script:McpApiKey
        agent = $script:McpSessionAgent
        model = $script:McpSessionModel
        slug = $script:McpSessionSlug
        slugGeneratedAt = $slugGeneratedAt
        session = $Session
    }

    $state | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $statePath -Encoding UTF8
}

function Initialize-McpSessionSlugState {
    param(
        [Parameter(Mandatory)][string]$Agent,
        [Parameter(Mandatory)][string]$Model
    )

    $state = Get-McpSessionState
    $now = (Get-Date).ToUniversalTime()
    $slug = $null
    $slugGeneratedAt = $null

    if ($state -and $state.slug -and $state.agent -eq $Agent -and $state.model -eq $Model) {
        $keyMatches = ($state.apiKey -eq $script:McpApiKey)
        $recentEnough = $false
        if (-not $keyMatches -and $state.slugGeneratedAt) {
            $parsedAt = [datetime]::MinValue
            if ([datetime]::TryParse(
                [string]$state.slugGeneratedAt,
                [System.Globalization.CultureInfo]::InvariantCulture,
                [System.Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$parsedAt
            )) {
                $recentEnough = (($now - $parsedAt.ToUniversalTime()) -lt [timespan]::FromHours(1))
            }
        }

        if ($keyMatches -or $recentEnough) {
            $slug = [string]$state.slug
            $slugGeneratedAt = [string]$state.slugGeneratedAt
        }
    }

    if ([string]::IsNullOrWhiteSpace($slug)) {
        $slug = New-McpSessionLogSlug -Agent $Agent -Model $Model -TimestampUtc $now
        $slugGeneratedAt = $now.ToString('o')
    }

    $script:McpSessionSlug = $slug

    $persistedSession = $null
    if ($state -and $state.session) {
        $persistedSession = ConvertTo-McpSessionRuntimeObject -Session $state.session
    }

    $persisted = [PSCustomObject]@{
        apiKey = $script:McpApiKey
        agent = $Agent
        model = $Model
        slug = $slug
        slugGeneratedAt = $slugGeneratedAt
        session = $persistedSession
    }

    $statePath = Get-McpSessionStatePath
    $stateDir = Split-Path -Parent $statePath
    New-Item -ItemType Directory -Path $stateDir -Force | Out-Null
    $persisted | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $statePath -Encoding UTF8
    return $slug
}

function Resolve-McpSession {
    param(
        [PSCustomObject]$Session,
        [switch]$AllowMissing
    )

    if ($Session) {
        return ConvertTo-McpSessionRuntimeObject -Session $Session
    }

    $state = Get-McpSessionState
    if ($state -and $state.session) {
        return ConvertTo-McpSessionRuntimeObject -Session $state.session
    }

    if ($AllowMissing) {
        return $null
    }

    $statePath = Get-McpSessionStatePath
    throw "No session provided and no persisted session found at '$statePath'. Create a session with New-McpSessionLog first."
}

function ConvertTo-McpSessionRuntimeObject {
    param([Parameter(Mandatory)][PSCustomObject]$Session)

    $sessionObject = $Session
    if ($sessionObject.PSObject.Properties.Name -contains 'session') {
        $sessionObject = $sessionObject.session
    }

    if (-not ($sessionObject.PSObject.Properties.Name -contains 'sourceType')) {
        throw 'Persisted session object is missing required sourceType property.'
    }

    [void](Get-McpSessionTurnList -Session $sessionObject)
    return $sessionObject
}

function Remove-McpSessionStateFile {
    $statePath = Get-McpSessionStatePath
    if (-not (Test-Path -LiteralPath $statePath)) {
        return
    }

    try {
        Remove-Item -LiteralPath $statePath -Force -ErrorAction Stop
    } catch {
        Write-Warning "Failed to delete session state file '$statePath': $_"
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

function Get-McpSessionTurnObjectList {
    param(
        [Parameter(Mandatory)][PSCustomObject]$Turn,
        [Parameter(Mandatory)][string]$Field
    )

    $current = $Turn.$Field
    if ($current -is [System.Collections.Generic.List[object]]) {
        return ,$current
    }

    $list = [System.Collections.Generic.List[object]]::new()
    foreach ($value in @($current)) {
        if ($null -ne $value) {
            [void]$list.Add($value)
        }
    }

    $Turn.$Field = $list
    return ,$list
}

function Normalize-McpSessionTurnCollections {
    param(
        [Parameter(Mandatory)][PSCustomObject]$Turn
    )

    foreach ($field in @("tags", "contextList", "designDecisions", "requirementsDiscovered", "filesModified", "blockers")) {
        [void](Get-McpSessionTurnStringList -Turn $Turn -Field $field)
    }

    foreach ($field in @("actions", "processingDialog")) {
        [void](Get-McpSessionTurnObjectList -Turn $Turn -Field $field)
    }
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
    'New-McpSessionLogSlug',
    'New-McpSessionLog',
    'Update-McpSessionLog',
    'Get-McpSessionLog',
    'Add-McpSessionTurn',
    'Set-McpSessionTurn',
    'Add-McpAction',
    'Add-McpTurnDetail',
    'Send-McpDialog'
)
