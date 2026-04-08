<#
.SYNOPSIS
    MCP Session Log PowerShell module - cmdlets for the /mcpserver/sessionlog API.

.DESCRIPTION
    Provides exported cmdlets to initialize MCP session-log connectivity, create session-log
    records, append or update turn data, append structured actions and dialog items, and query
    recent session-log records from an MCP Context Server. The module reads connection details
    from AGENTS-README-FIRST.yaml unless explicit connection overrides are supplied.

    Initialize-McpSession configures module-scoped connection state and returns only a reusable
    session-slug string. It does not create a session-log record and it does not return a
    session object. New-McpSessionLog creates the session object, posts it to the server, and
    persists it locally for later resolution by the other exported cmdlets.

    For compaction workflows, persist the session log immediately before compaction and again
    after compaction to record the resulting context state.

.NOTES
    Usage:  Import-Module ./McpSession.psm1
            $slug = Initialize-McpSession -Agent "Copilotcli" -Model "gpt-5.3-codex"  # returns a string session slug only
            $s = New-McpSessionLog -SourceType "Copilotcli" -Title "My session" -Model "gpt-5.3-codex"  # creates the session object
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
$script:McpTrustBootstrapPendingNote = $null
$script:McpTrustBootstrapPendingRecordedAt = $null

# ─── Connection ──────────────────────────────────────────────────────────────

function Find-McpMarkerFile {
    $dir = (Get-Location).Path
    while ($dir) {
        $candidate = Join-Path $dir "AGENTS-README-FIRST.yaml"
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }

        $parent = Split-Path $dir -Parent
        if (-not $parent -or $parent -eq $dir) {
            break
        }

        $dir = $parent
    }

    return $null
}

function Read-McpMarkerNestedMap {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string[]]$Lines,
        [Parameter(Mandatory)][int]$StartIndex
    )

    $values = [ordered]@{}
    $index = $StartIndex

    while ($index -lt $Lines.Count) {
        $line = $Lines[$index]
        if ($line -match '^\S') {
            break
        }

        if ($line -match '^\s{2}(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<value>.*)$') {
            $values[$Matches.key] = Normalize-McpMarkerScalar -Value $Matches.value
        }

        $index++
    }

    return @{
        Values = $values
        NextIndex = $index
    }
}

function Normalize-McpMarkerScalar {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$Value
    )

    $normalized = $Value.Trim()
    if ($normalized.Length -ge 2) {
        $first = $normalized[0]
        $last = $normalized[$normalized.Length - 1]
        if (($first -eq '"' -and $last -eq '"') -or ($first -eq "'" -and $last -eq "'")) {
            return $normalized.Substring(1, $normalized.Length - 2)
        }
    }

    return $normalized
}

function ConvertFrom-McpMarkerContent {
    param(
        [Parameter(Mandatory)][string]$Content
    )

    $normalized = $Content.ReplaceLineEndings("`n")
    $lines = $normalized -split "`n"
    $marker = [ordered]@{
        endpoints = [ordered]@{}
        signature = [ordered]@{}
        trust_bootstrap = [ordered]@{}
        prompt = ''
    }

    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line -match '^(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<value>.*)$') {
            $key = $Matches.key
            $value = $Matches.value

            if ($value -in @('|', '|-')) {
                $index++
                $promptLines = [System.Collections.Generic.List[string]]::new()
                while ($index -lt $lines.Count) {
                    $promptLine = $lines[$index]
                    if ($promptLine -match '^\S') {
                        $index--
                        break
                    }

                    if ($promptLine.StartsWith('  ')) {
                        [void]$promptLines.Add($promptLine.Substring(2))
                    } else {
                        [void]$promptLines.Add($promptLine)
                    }

                    $index++
                }

                $marker[$key] = ($promptLines -join "`n").TrimEnd("`n")
                continue
            }

            if ([string]::IsNullOrWhiteSpace($value)) {
                $section = Read-McpMarkerNestedMap -Lines $lines -StartIndex ($index + 1)
                $marker[$key] = $section.Values
                $index = $section.NextIndex - 1
                continue
            }

            $marker[$key] = Normalize-McpMarkerScalar -Value $value
        }
    }

    return [pscustomobject]$marker
}

function Get-McpMarkerSignaturePayload {
    param(
        [Parameter(Mandatory)][pscustomobject]$Marker
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("canonicalization=$([string]$Marker.signature.canonicalization)")
    $lines.Add("port=$([string]$Marker.port)")
    $lines.Add("baseUrl=$([string]$Marker.baseUrl)")
    $lines.Add("apiKey=$([string]$Marker.apiKey)")
    $lines.Add("workspace=$([string]$Marker.workspace)")
    $lines.Add("workspacePath=$([string]$Marker.workspacePath)")
    $lines.Add("pid=$([string]$Marker.pid)")
    $lines.Add("startedAt=$([string]$Marker.startedAt)")
    $lines.Add("markerWrittenAtUtc=$([string]$Marker.markerWrittenAtUtc)")
    $lines.Add("serverStartedAtUtc=$([string]$Marker.serverStartedAtUtc)")
    $lines.Add("endpoints.health=$([string]$Marker.endpoints.health)")
    $lines.Add("endpoints.swagger=$([string]$Marker.endpoints.swagger)")
    $lines.Add("endpoints.swaggerUi=$([string]$Marker.endpoints.swaggerUi)")
    $lines.Add("endpoints.mcpTransport=$([string]$Marker.endpoints.mcpTransport)")
    $lines.Add("endpoints.sessionLog=$([string]$Marker.endpoints.sessionLog)")
    $lines.Add("endpoints.sessionLogDialog=$([string]$Marker.endpoints.sessionLogDialog)")
    $lines.Add("endpoints.contextSearch=$([string]$Marker.endpoints.contextSearch)")
    $lines.Add("endpoints.contextPack=$([string]$Marker.endpoints.contextPack)")
    $lines.Add("endpoints.contextSources=$([string]$Marker.endpoints.contextSources)")
    $lines.Add("endpoints.todo=$([string]$Marker.endpoints.todo)")
    $lines.Add("endpoints.repo=$([string]$Marker.endpoints.repo)")
    $lines.Add("endpoints.desktop=$([string]$Marker.endpoints.desktop)")
    $lines.Add("endpoints.gitHub=$([string]$Marker.endpoints.gitHub)")
    $lines.Add("endpoints.tools=$([string]$Marker.endpoints.tools)")
    $lines.Add("endpoints.workspace=$([string]$Marker.endpoints.workspace)")
    $lines.Add("endpoints.serverStartupUtc=$([string]$Marker.endpoints.serverStartupUtc)")
    $lines.Add("endpoints.markerFileTimestamp=$([string]$Marker.endpoints.markerFileTimestamp)")
    return (($lines -join "`n") + "`n")
}

function Get-McpMarkerSignatureValue {
    param(
        [Parameter(Mandatory)][pscustomobject]$Marker
    )

    $hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes([string]$Marker.apiKey))
    try {
        $payloadBytes = [System.Text.Encoding]::UTF8.GetBytes((Get-McpMarkerSignaturePayload -Marker $Marker))
        return [Convert]::ToHexString($hmac.ComputeHash($payloadBytes))
    } finally {
        $hmac.Dispose()
    }
}

function Test-McpMarkerSignature {
    param(
        [Parameter(Mandatory)][pscustomobject]$Marker
    )

    if (-not $Marker.signature -or [string]::IsNullOrWhiteSpace([string]$Marker.signature.value)) {
        return $false
    }

    $expected = (Get-McpMarkerSignatureValue -Marker $Marker)
    return [string]::Equals(
        $expected,
        [string]$Marker.signature.value,
        [System.StringComparison]::OrdinalIgnoreCase)
}

function New-McpTrustNonce {
    $bytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(18)
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Reset-McpSessionConnectionState {
    $script:McpBaseUrl = $null
    $script:McpApiKey = $null
    $script:McpWorkspacePath = $null
    $script:McpHeaders = @{}
}

function Throw-McpUntrusted {
    param(
        [Parameter(Mandatory)][string]$Reason
    )

    Reset-McpSessionConnectionState
    $message = "MCP_UNTRUSTED: $Reason"
    Write-Warning $message
    throw $message
}

function Invoke-McpTrustedHealthCheck {
    param(
        [Parameter(Mandatory)][string]$BaseUrl
    )

    $nonce = New-McpTrustNonce
    $separator = if ($BaseUrl.Contains('?')) { '&' } else { '?' }
    $health = Invoke-RestMethod -Uri "$BaseUrl/health${separator}nonce=$nonce" -TimeoutSec 5
    if ([string]$health.nonce -ne $nonce) {
        Throw-McpUntrusted -Reason "The /health response nonce did not match the caller nonce."
    }

    return [pscustomobject]@{
        Nonce = $nonce
        Status = [string]$health.status
        VerifiedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    }
}

function Set-McpPendingTrustBootstrapNote {
    param(
        [Parameter(Mandatory)][string]$Message,
        [Parameter(Mandatory)][string]$RecordedAtUtc
    )

    $script:McpTrustBootstrapPendingNote = $Message
    $script:McpTrustBootstrapPendingRecordedAt = $RecordedAtUtc
}

function Initialize-McpSession {
    <#
    .SYNOPSIS
        Configure module-scoped MCP connection state and return the reusable session slug.

    .DESCRIPTION
        Reads MCP connection settings from AGENTS-README-FIRST.yaml, or from -BaseUrl and
        -ApiKey when both overrides are supplied, then stores the resolved base URL, API key,
        workspace header, agent identity, model identifier, and reusable session slug in
        module-scoped state. When the marker file is used, the function verifies the marker
        signature before contacting the server. The function then calls /health with a random
        nonce and requires the response to echo that exact nonce. If signature or nonce
        verification fails, the function emits MCP_UNTRUSTED, clears module connection state,
        and throws before any follow-on MCP usage can occur.

        This function does not create a session-log record, does not POST to
        /mcpserver/sessionlog, and does not return a session object. Its only return value is
        the session-slug string that later commands may reuse when New-McpSessionLog is called.

    .PARAMETER Agent
        Pascal-Case agent identity used as the leading token in generated session IDs.

    .PARAMETER Model
        Model identifier used in generated session IDs and in persisted session-slug state.

    .PARAMETER MarkerPath
        Explicit path to AGENTS-README-FIRST.yaml. When omitted, the function searches upward
        from the current directory until it finds AGENTS-README-FIRST.yaml.

    .PARAMETER BaseUrl
        MCP server base URL override. This parameter is used only when -ApiKey is also
        supplied. If either override parameter is missing, the function falls back to the
        marker file.

    .PARAMETER ApiKey
        MCP API key override. This parameter is used only when -BaseUrl is also supplied. If
        either override parameter is missing, the function falls back to the marker file.

    .OUTPUTS
        System.String. Returns only the reusable session-slug string. The slug is persisted in
        .mcpServer/session.yaml. If a serialized current session already exists, it remains
        stored for later resolution, but it is not returned by this function.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Agent,
        [Parameter(Mandatory)][string]$Model,
        [string]$MarkerPath,
        [string]$BaseUrl,
        [string]$ApiKey
    )

    $signatureVerified = $false
    $verifiedAtUtc = $null

    if ($BaseUrl -and $ApiKey) {
        $script:McpBaseUrl = $BaseUrl.TrimEnd('/')
        $script:McpApiKey  = $ApiKey
        if ([string]::IsNullOrWhiteSpace($script:McpWorkspacePath)) {
            $script:McpWorkspacePath = (Get-Location).Path
        }
    } else {
        if (-not $MarkerPath) {
            $MarkerPath = Find-McpMarkerFile
        }
        if (-not $MarkerPath -or -not (Test-Path $MarkerPath)) {
            throw "AGENTS-README-FIRST.yaml not found. Provide -MarkerPath, or run from within a workspace."
        }
        $marker = ConvertFrom-McpMarkerContent -Content (Get-Content -LiteralPath $MarkerPath -Raw)
        if (-not (Test-McpMarkerSignature -Marker $marker)) {
            Throw-McpUntrusted -Reason "Marker signature verification failed."
        }

        $signatureVerified = $true
        $script:McpBaseUrl = ([string]$marker.baseUrl).TrimEnd('/')
        $script:McpApiKey = [string]$marker.apiKey
        $script:McpWorkspacePath = [string]$marker.workspacePath
    }

    $script:McpHeaders = @{
        "X-Api-Key"        = $script:McpApiKey
        "Content-Type"     = "application/json"
        "X-Workspace-Path" = $script:McpWorkspacePath
    }

    try {
        $handshake = Invoke-McpTrustedHealthCheck -BaseUrl $script:McpBaseUrl
        $verifiedAtUtc = $handshake.VerifiedAtUtc
        Write-Host "Connected to MCP server at $($script:McpBaseUrl) - status: $($handshake.Status)" -ForegroundColor Green
    } catch {
        if ($_.Exception.Message -like 'MCP_UNTRUSTED:*') {
            throw
        }

        Throw-McpUntrusted -Reason "The /health trust handshake failed: $($_.Exception.Message)"
    }

    $script:McpSessionAgent = $Agent.Trim()
    $script:McpSessionModel = $Model.Trim()
    if ($signatureVerified) {
        Set-McpPendingTrustBootstrapNote `
            -Message "Agent successfully trusted MCP Server at $verifiedAtUtc via nonce and signature verification." `
            -RecordedAtUtc $verifiedAtUtc
    } else {
        Set-McpPendingTrustBootstrapNote `
            -Message "Agent established MCP connectivity at $verifiedAtUtc via nonce verification using explicit connection overrides." `
            -RecordedAtUtc $verifiedAtUtc
    }
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
    .SYNOPSIS
        Build a canonical session ID string without creating a session record.

    .DESCRIPTION
        Returns a session ID in the form <Agent>-<yyyyMMddTHHmmssZ>-<model-slug>. This
        function performs no network I/O, writes no local state files, and does not create or
        update a session-log record.

    .PARAMETER Agent
        Pascal-Case agent/source prefix. The value must match ^[A-Z][A-Za-z0-9]*$.

    .PARAMETER Model
        Raw model identifier used to build the normalized trailing slug segment.

    .PARAMETER TimestampUtc
        Optional UTC timestamp used for the middle timestamp token. Defaults to the current
        UTC time.

    .OUTPUTS
        System.String. Returns only the formatted session ID.
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
    .SYNOPSIS
        Create a new session object, POST it to the server, and persist it locally.

    .DESCRIPTION
        Constructs a new session-log object, immediately POSTs it to /mcpserver/sessionlog,
        then persists the created session to the local session-state files used by the module.
        The returned object is the session object that subsequent exported cmdlets expect when
        you want to work against a specific session explicitly.

        If -SessionId is omitted, the function first reuses the initialized session slug from
        Initialize-McpSession when one is available. If no initialized slug exists, the
        function generates a new canonical session ID from -SourceType and -Model.

    .PARAMETER SourceType
        Actual agent identity recorded in the session log. This value becomes the session
        record sourceType and should match the agent prefix used for the session ID.

    .PARAMETER SessionId
        Explicit session ID to assign to the new session record. If omitted, the initialized
        reusable slug is used when available; otherwise a new canonical session ID is generated.

    .PARAMETER Title
        Human-readable title for the session record.

    .PARAMETER Model
        Model identifier recorded in the session record.

    .OUTPUTS
        PSCustomObject. Returns the newly created in-memory session object after it has been
        posted to the server and persisted locally.
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
        turnCount   = 0
        totalTokens = 0
        turns       = [System.Collections.Generic.List[object]]::new()
    }

    Push-SessionLog $session
    Save-McpSessionState -Session $session
    return $session
}

function Update-McpSessionLog {
    <#
    .SYNOPSIS
        Persist the current session object to the server and refresh local session state.

    .DESCRIPTION
        Resolves the session object, recalculates lastUpdated, turnCount, and totalTokens,
        applies optional scalar updates such as -Status and -Title, and POSTs the full session
        payload to /mcpserver/sessionlog. If -Session is omitted, the function loads the
        current session from the local session-state cache.

        When the resulting session status is completed, the function removes the local session
        state files after a successful push. Otherwise it rewrites the local session state with
        the updated payload.

    .PARAMETER Session
        Optional session object to push. If omitted, the current persisted session is resolved
        from local session-state files.

    .PARAMETER Status
        Optional replacement value for the session status before the session is pushed.

    .PARAMETER Title
        Optional replacement value for the session title before the session is pushed.

    .OUTPUTS
        None. The function updates server-side and local session state but does not return a
        value.
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
    Set-McpSessionScalarProperty -Session $Session -Name 'turnCount' -Value $turns.Count
    $totalTokens = @($turns | ForEach-Object {
        if ($_.PSObject.Properties.Name -contains "tokenCount" -and $null -ne $_.tokenCount) { [int]$_.tokenCount } else { 0 }
    } | Measure-Object -Sum).Sum
    $resolvedTotalTokens = if ($null -eq $totalTokens) { 0 } else { [int]$totalTokens }
    Set-McpSessionScalarProperty -Session $Session -Name 'totalTokens' -Value $resolvedTotalTokens
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
    .SYNOPSIS
        Query recent session-log records from the server.

    .DESCRIPTION
        Sends a read-only request to /mcpserver/sessionlog using the current module
        connection headers. This function does not create, update, or delete local session
        state files.

    .PARAMETER Limit
        Maximum number of session records to request. Defaults to 5.

    .PARAMETER Offset
        Pagination offset applied to the server-side query.

    .OUTPUTS
        System.Object. Returns the deserialized API response, which includes paging metadata
        such as totalCount, limit, offset, and the items collection.
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

    if ($Session.PSObject.Properties.Name -contains "turns") {
        $turnsValue = $Session.turns
        if (-not ($turnsValue -is [System.Collections.Generic.List[object]])) {
            $turnsList = [System.Collections.Generic.List[object]]::new()
            foreach ($item in @($turnsValue)) {
                [void]$turnsList.Add($item)
            }
            $Session.turns = $turnsList
        }

        foreach ($turn in @($Session.turns)) {
            Normalize-McpSessionTurnCollections -Turn $turn
        }

        return ,$Session.turns
    }

    $turns = [System.Collections.Generic.List[object]]::new()
    $Session | Add-Member -NotePropertyName turns -NotePropertyValue $turns -Force

    foreach ($turn in @($Session.turns)) {
        Normalize-McpSessionTurnCollections -Turn $turn
    }

    return ,$Session.turns
}

function Add-McpSessionTurn {
    <#
    .SYNOPSIS
        Create a new turn, append it to the session, and persist the change by default.

    .DESCRIPTION
        Resolves the target session, creates a new turn object, appends it to the session's
        turns collection, and then persists the updated session unless -NoPush is supplied.
        If -Session is omitted, the function resolves the current persisted session from local
        state.

        When -RequestId is omitted, the function generates a canonical request ID from
        -QueryTitle, then from -QueryText, and finally from the literal seed "turn" if both
        text inputs are blank. If the generated ID already exists in the session, the function
        generates a second canonical ID using a numeric suffix.

    .PARAMETER Session
        Optional session object to update. If omitted, the current persisted session is
        resolved from local session-state files.

    .PARAMETER RequestId
        Explicit request ID for the new turn. When omitted, the function generates a unique
        canonical request ID.

    .PARAMETER QueryTitle
        Short human-readable summary of the user request represented by the turn.

    .PARAMETER QueryText
        Full request text or task description represented by the turn.

    .PARAMETER Interpretation
        Agent interpretation of the request at the time the turn is created.

    .PARAMETER Response
        Initial response text to store on the turn. This value may be empty when the turn is
        first created.

    .PARAMETER Status
        Initial turn status. Must be in_progress or completed.

    .PARAMETER Model
        Model identifier recorded on the turn. If omitted, the session model is used.

    .PARAMETER Tags
        Initial tags collection to store on the turn.

    .PARAMETER ContextList
        Initial context-reference collection to store on the turn.

    .PARAMETER TokenCount
        Optional token-count value to store on the turn.

    .PARAMETER ModelProvider
        Optional model-provider identifier to store on the turn.

    .PARAMETER FailureNote
        Optional failure note to store on the turn.

    .PARAMETER Score
        Optional numeric score to store on the turn.

    .PARAMETER IsPremium
        Optional premium-model flag to store on the turn.

    .PARAMETER DesignDecisions
        Initial design-decision entries to store on the turn.

    .PARAMETER RequirementsDiscovered
        Initial requirement IDs or requirement notes to store on the turn.

    .PARAMETER FilesModified
        Initial file-path entries to store on the turn.

    .PARAMETER Blockers
        Initial blocker entries to store on the turn.

    .PARAMETER NoPush
        When supplied, the function writes the updated session only to local session-state
        files and does not POST the session to the server.

    .OUTPUTS
        PSCustomObject. Returns the newly created turn object after it has been appended to the
        in-memory session.
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

    if (-not [string]::IsNullOrWhiteSpace($script:McpTrustBootstrapPendingNote)) {
        $turn.processingDialog.Add([PSCustomObject]@{
            timestamp = if ([string]::IsNullOrWhiteSpace($script:McpTrustBootstrapPendingRecordedAt)) {
                (Get-Date).ToUniversalTime().ToString("o")
            } else {
                $script:McpTrustBootstrapPendingRecordedAt
            }
            role = "system"
            content = $script:McpTrustBootstrapPendingNote
            category = "observation"
        })

        $script:McpTrustBootstrapPendingNote = $null
        $script:McpTrustBootstrapPendingRecordedAt = $null
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
    .SYNOPSIS
        Update an existing turn and persist the containing session by default.

    .DESCRIPTION
        Updates scalar fields on an existing turn and appends supplied list values to the turn's
        existing list fields. This function does not replace list-valued properties such as
        tags, contextList, designDecisions, requirementsDiscovered, filesModified, or blockers;
        each supplied value is appended in order.

        If -Session is omitted, the function resolves the current persisted session from local
        state. When the supplied -Turn includes a requestId and the resolved session contains a
        turn with the same requestId, the function updates the turn instance from the resolved
        session before applying changes. Unless -NoPush is supplied, the updated session is
        POSTed to the server immediately.

    .PARAMETER Turn
        Turn object to update. The object is typically the value returned by Add-McpSessionTurn.

    .PARAMETER Session
        Optional parent session object. If omitted, the current persisted session is resolved
        from local session-state files.

    .PARAMETER Response
        Replacement response text for the turn.

    .PARAMETER Interpretation
        Replacement interpretation text for the turn.

    .PARAMETER Status
        Replacement turn status.

    .PARAMETER TokenCount
        Replacement token-count value.

    .PARAMETER ModelProvider
        Replacement model-provider identifier.

    .PARAMETER FailureNote
        Replacement failure note.

    .PARAMETER Score
        Replacement score value.

    .PARAMETER IsPremium
        Replacement premium-model flag.

    .PARAMETER Tags
        Values to append to the turn's tags collection.

    .PARAMETER ContextList
        Values to append to the turn's contextList collection.

    .PARAMETER FilesModified
        Values to append to the turn's filesModified collection.

    .PARAMETER DesignDecisions
        Values to append to the turn's designDecisions collection.

    .PARAMETER RequirementsDiscovered
        Values to append to the turn's requirementsDiscovered collection.

    .PARAMETER Blockers
        Values to append to the turn's blockers collection.

    .PARAMETER NoPush
        When supplied, the function writes the updated session only to local session-state
        files and does not POST the session to the server.

    .OUTPUTS
        None. The function mutates the turn and persists the containing session, but it does
        not return a value.
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
    .SYNOPSIS
        Append a structured action record to a turn.

    .DESCRIPTION
        Adds a new action object to Turn.actions and assigns the next sequential order value
        based on the current action count. If -Session is supplied, or if the module can
        resolve the current persisted session from local state, the containing session is
        persisted immediately unless -NoPush is supplied. If no session can be resolved, the
        function updates only the in-memory turn object.

    .PARAMETER Turn
        Turn object whose actions collection will receive the new action.

    .PARAMETER Description
        Human-readable description of the action that was taken.

    .PARAMETER Type
        Action type recorded on the action object. The value must be one of the exported
        validate-set literals.

    .PARAMETER Session
        Optional parent session object used for immediate persistence.

    .PARAMETER FilePath
        Related file path or external identifier for the action. Use the empty string when the
        action is not tied to a specific path.

    .PARAMETER Status
        Action execution status. Must be completed, in_progress, or failed.

    .PARAMETER NoPush
        When supplied, the function updates only in-memory and local session-state data; it
        does not POST the containing session to the server.

    .OUTPUTS
        PSCustomObject. Returns the newly created action object.
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
    .SYNOPSIS
        Append one string value to a list-valued turn field.

    .DESCRIPTION
        Appends a single non-empty string value to one of the supported list-valued turn
        fields: tags, contextList, designDecisions, requirementsDiscovered, filesModified, or
        blockers. The function never replaces the existing collection and silently ignores
        null, empty, or whitespace-only values.

        If -Session is supplied, or if the module can resolve the current persisted session
        from local state, the containing session is persisted immediately unless -NoPush is
        supplied. If no session can be resolved, the function updates only the in-memory turn
        object.

    .PARAMETER Turn
        Turn object whose list-valued field will receive the appended value.

    .PARAMETER Field
        Name of the list-valued field that will receive the new string.

    .PARAMETER Value
        String value to append. Whitespace-only values are ignored.

    .PARAMETER Session
        Optional parent session object used for immediate persistence.

    .PARAMETER NoPush
        When supplied, the function updates only in-memory and local session-state data; it
        does not POST the containing session to the server.

    .OUTPUTS
        None. The function mutates the turn and optionally persists the containing session, but
        it does not return a value.
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
    .SYNOPSIS
        POST one dialog item to the dialog endpoint for a specific session turn.

    .DESCRIPTION
        Resolves the target session, constructs a single dialog item payload, and POSTs that
        item to /mcpserver/sessionlog/{sourceType}/{sessionId}/{requestId}/dialog. If -Session
        is omitted, the function resolves the current persisted session from local state. This
        function does not modify the local turn object and does not rewrite local session-state
        files.

    .PARAMETER Session
        Optional session object that identifies the session owning the target turn. If omitted,
        the current persisted session is resolved from local session-state files.

    .PARAMETER RequestId
        Request ID of the existing turn that will receive the dialog item.

    .PARAMETER Content
        Dialog content to post.

    .PARAMETER Role
        Role value recorded for the dialog item. Must be model, tool, system, or user.

    .PARAMETER Category
        Category value recorded for the dialog item. Must be reasoning, tool_call,
        tool_result, observation, or decision.

    .OUTPUTS
        None. The function sends the dialog item to the server but does not return a value.
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

function Get-McpSessionWorkspacePath {
    if (-not [string]::IsNullOrWhiteSpace($script:McpWorkspacePath)) {
        return $script:McpWorkspacePath
    }

    return (Get-Location).Path
}

function Get-McpSessionStatePath {
    $workspacePath = Get-McpSessionWorkspacePath
    $stateDir = Join-Path $workspacePath ".mcpServer"
    return Join-Path $stateDir "session.yaml"
}

function Get-McpCurrentSessionCacheDirectoryPath {
    $workspacePath = Get-McpSessionWorkspacePath
    return Join-Path $workspacePath ".mcpSession"
}

function Get-McpCurrentSessionCachePath {
    $cacheDir = Get-McpCurrentSessionCacheDirectoryPath
    return Join-Path $cacheDir "current-session.json"
}

function Get-McpCurrentSessionCacheCandidatePaths {
    $cacheDir = Get-McpCurrentSessionCacheDirectoryPath
    return @(
        (Join-Path $cacheDir "current-session.json"),
        (Join-Path $cacheDir "session.json"),
        (Join-Path $cacheDir "current.json")
    )
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

function ConvertTo-McpCachedSessionObject {
    param(
        [Parameter(Mandatory)][object]$Candidate,
        [Parameter(Mandatory)][string]$Path
    )

    $sessionCandidate = $Candidate
    $propertyNames = @($sessionCandidate.PSObject.Properties.Name)
    if (-not ($propertyNames -contains 'sourceType' -and $propertyNames -contains 'sessionId')) {
        foreach ($propertyName in @('currentSession', 'current', 'session')) {
            if ($propertyNames -contains $propertyName -and $null -ne $sessionCandidate.$propertyName) {
                $sessionCandidate = $sessionCandidate.$propertyName
                break
            }
        }
    }

    if ($null -eq $sessionCandidate) {
        return $null
    }

    $sessionPropertyNames = @($sessionCandidate.PSObject.Properties.Name)
    if (-not ($sessionPropertyNames -contains 'sourceType' -and $sessionPropertyNames -contains 'sessionId')) {
        return $null
    }

    try {
        return ConvertTo-McpSessionRuntimeObject -Session $sessionCandidate
    } catch {
        Write-Warning "Failed to normalize current session cache file '$Path': $_"
        return $null
    }
}

function Get-McpCurrentSessionCache {
    foreach ($cachePath in @(Get-McpCurrentSessionCacheCandidatePaths)) {
        if (-not (Test-Path -LiteralPath $cachePath)) {
            continue
        }

        try {
            $raw = Get-Content -LiteralPath $cachePath -Raw -ErrorAction Stop
            if ([string]::IsNullOrWhiteSpace($raw)) {
                continue
            }

            $candidate = $raw | ConvertFrom-Json -Depth 50
            $session = ConvertTo-McpCachedSessionObject -Candidate $candidate -Path $cachePath
            if ($null -ne $session) {
                return $session
            }
        } catch {
            Write-Warning "Failed to parse current session cache file '$cachePath': $_"
        }
    }

    return $null
}

function Save-McpCurrentSessionCache {
    param([PSCustomObject]$Session)

    $cachePath = Get-McpCurrentSessionCachePath
    $cacheDir = Split-Path -Parent $cachePath
    New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null

    $payload = Get-McpSessionSerializableObject -Session $Session
    $payload | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $cachePath -Encoding UTF8
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
        pendingTrustNote = $script:McpTrustBootstrapPendingNote
        pendingTrustRecordedAt = $script:McpTrustBootstrapPendingRecordedAt
        session = Get-McpSessionSerializableObject -Session $Session
    }

    $state | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $statePath -Encoding UTF8
    Save-McpCurrentSessionCache -Session $Session
}

function Initialize-McpSessionSlugState {
    param(
        [Parameter(Mandatory)][string]$Agent,
        [Parameter(Mandatory)][string]$Model
    )

    $state = Get-McpSessionState
    $currentSession = Get-McpCurrentSessionCache
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

    if ([string]::IsNullOrWhiteSpace($slug) -and $currentSession) {
        $sessionId = [string]$currentSession.sessionId
        $sessionModel = [string]$currentSession.model
        $sessionStatus = [string]$currentSession.status
        $sessionSourceType = [string]$currentSession.sourceType
        $statusIsReusable = [string]::IsNullOrWhiteSpace($sessionStatus) -or $sessionStatus -notin @('completed', 'closed')
        $agentMatches = $sessionSourceType -eq $Agent -or (
            -not [string]::IsNullOrWhiteSpace($sessionId) -and
            $sessionId.StartsWith("$Agent-", [System.StringComparison]::Ordinal)
        )

        if ($statusIsReusable -and $agentMatches -and $sessionModel -eq $Model) {
            $slug = $sessionId
            $slugGeneratedAt = if (-not [string]::IsNullOrWhiteSpace([string]$currentSession.lastUpdated)) {
                [string]$currentSession.lastUpdated
            } elseif (-not [string]::IsNullOrWhiteSpace([string]$currentSession.started)) {
                [string]$currentSession.started
            } else {
                $now.ToString('o')
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($slug)) {
        $slug = New-McpSessionLogSlug -Agent $Agent -Model $Model -TimestampUtc $now
        $slugGeneratedAt = $now.ToString('o')
    }

    $script:McpSessionSlug = $slug
    if ($state -and -not [string]::IsNullOrWhiteSpace([string]$state.pendingTrustNote)) {
        $script:McpTrustBootstrapPendingNote = [string]$state.pendingTrustNote
    }

    if ($state -and -not [string]::IsNullOrWhiteSpace([string]$state.pendingTrustRecordedAt)) {
        $script:McpTrustBootstrapPendingRecordedAt = [string]$state.pendingTrustRecordedAt
    }

    $persistedSession = $null
    if ($currentSession) {
        $persistedSession = $currentSession
    } elseif ($state -and $state.session) {
        $persistedSession = ConvertTo-McpSessionRuntimeObject -Session $state.session
    }

    $persisted = [PSCustomObject]@{
        apiKey = $script:McpApiKey
        agent = $Agent
        model = $Model
        slug = $slug
        slugGeneratedAt = $slugGeneratedAt
        pendingTrustNote = $script:McpTrustBootstrapPendingNote
        pendingTrustRecordedAt = $script:McpTrustBootstrapPendingRecordedAt
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

    $currentSession = Get-McpCurrentSessionCache
    if ($currentSession) {
        return $currentSession
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
    foreach ($path in @((Get-McpSessionStatePath)) + @(Get-McpCurrentSessionCacheCandidatePaths)) {
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        try {
            Remove-Item -LiteralPath $path -Force -ErrorAction Stop
        } catch {
            Write-Warning "Failed to delete session state file '$path': $_"
        }
    }

    try {
        $cacheDir = Get-McpCurrentSessionCacheDirectoryPath
        if ((Test-Path -LiteralPath $cacheDir) -and $null -eq (Get-ChildItem -LiteralPath $cacheDir -Force -ErrorAction Stop | Select-Object -First 1)) {
            Remove-Item -LiteralPath $cacheDir -Force -ErrorAction Stop
        }
    } catch {
        Write-Warning "Failed to delete current session cache directory '$cacheDir': $_"
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

function Set-McpSessionScalarProperty {
    param(
        [Parameter(Mandatory)][PSCustomObject]$Session,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][object]$Value
    )

    if ($Session.PSObject.Properties.Name -contains $Name) {
        $Session.$Name = $Value
        return
    }

    $Session | Add-Member -NotePropertyName $Name -NotePropertyValue $Value -Force
}

function Get-McpSessionSerializableObject {
    param([Parameter(Mandatory)][PSCustomObject]$Session)

    $turns = Get-McpSessionTurnList -Session $Session
    $payload = [ordered]@{}
    foreach ($property in $Session.PSObject.Properties) {
        if ($property.Name -in @('entries', 'entryCount', 'turns', 'turnCount')) {
            continue
        }

        $payload[$property.Name] = $property.Value
    }

    $payload.turns = $turns
    $payload.turnCount = $turns.Count
    $totalTokens = @($turns | ForEach-Object {
        if ($_.PSObject.Properties.Name -contains "tokenCount" -and $null -ne $_.tokenCount) { [int]$_.tokenCount } else { 0 }
    } | Measure-Object -Sum).Sum
    $payload.totalTokens = if ($null -eq $totalTokens) { 0 } else { [int]$totalTokens }
    return [PSCustomObject]$payload
}

function Push-SessionLog {
    param([PSCustomObject]$Session)

    $payload = Get-McpSessionSerializableObject -Session $Session
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
