<#
.SYNOPSIS
    MCP Todo PowerShell module - cmdlets for the /mcpserver/todo API.

.DESCRIPTION
    Provides cmdlets to list, create, update, complete, and delete todos on an MCP Context Server.
    Automatically reads connection details from the AGENTS-README-FIRST.yaml marker file.

.NOTES
    Usage:  Import-Module ./McpTodo.psm1
            Initialize-McpTodo                             # reads marker, sets connection
            Get-McpTodo                                     # list all todos
            New-McpTodo -Id "fix-auth" -Title "Fix auth" -Section Backend -Priority high
            Update-McpTodo -Id "fix-auth" -Remaining "Need tests"
            Complete-McpTodo -Id "fix-auth" -DoneSummary "Auth fixed with JWT"
            Remove-McpTodo -Id "fix-auth"
#>

# ─── Module state ────────────────────────────────────────────────────────────
$script:McpBaseUrl       = $null
$script:McpApiKey        = $null
$script:McpWorkspacePath = $null
$script:McpHeaders       = @{}

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
    param([Parameter(Mandatory)][pscustomobject]$Marker)

    return (@(
        "canonicalization=$([string]$Marker.signature.canonicalization)"
        "port=$([string]$Marker.port)"
        "baseUrl=$([string]$Marker.baseUrl)"
        "apiKey=$([string]$Marker.apiKey)"
        "workspace=$([string]$Marker.workspace)"
        "workspacePath=$([string]$Marker.workspacePath)"
        "pid=$([string]$Marker.pid)"
        "startedAt=$([string]$Marker.startedAt)"
        "markerWrittenAtUtc=$([string]$Marker.markerWrittenAtUtc)"
        "serverStartedAtUtc=$([string]$Marker.serverStartedAtUtc)"
        "endpoints.health=$([string]$Marker.endpoints.health)"
        "endpoints.swagger=$([string]$Marker.endpoints.swagger)"
        "endpoints.swaggerUi=$([string]$Marker.endpoints.swaggerUi)"
        "endpoints.mcpTransport=$([string]$Marker.endpoints.mcpTransport)"
        "endpoints.sessionLog=$([string]$Marker.endpoints.sessionLog)"
        "endpoints.sessionLogDialog=$([string]$Marker.endpoints.sessionLogDialog)"
        "endpoints.contextSearch=$([string]$Marker.endpoints.contextSearch)"
        "endpoints.contextPack=$([string]$Marker.endpoints.contextPack)"
        "endpoints.contextSources=$([string]$Marker.endpoints.contextSources)"
        "endpoints.todo=$([string]$Marker.endpoints.todo)"
        "endpoints.repo=$([string]$Marker.endpoints.repo)"
        "endpoints.desktop=$([string]$Marker.endpoints.desktop)"
        "endpoints.gitHub=$([string]$Marker.endpoints.gitHub)"
        "endpoints.tools=$([string]$Marker.endpoints.tools)"
        "endpoints.workspace=$([string]$Marker.endpoints.workspace)"
        "endpoints.serverStartupUtc=$([string]$Marker.endpoints.serverStartupUtc)"
        "endpoints.markerFileTimestamp=$([string]$Marker.endpoints.markerFileTimestamp)"
    ) -join "`n") + "`n"
}

function Test-McpMarkerSignature {
    param([Parameter(Mandatory)][pscustomobject]$Marker)

    if (-not $Marker.signature -or [string]::IsNullOrWhiteSpace([string]$Marker.signature.value)) {
        return $false
    }

    $hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes([string]$Marker.apiKey))
    try {
        $payloadBytes = [System.Text.Encoding]::UTF8.GetBytes((Get-McpMarkerSignaturePayload -Marker $Marker))
        $expected = [Convert]::ToHexString($hmac.ComputeHash($payloadBytes))
        return [string]::Equals($expected, [string]$Marker.signature.value, [System.StringComparison]::OrdinalIgnoreCase)
    } finally {
        $hmac.Dispose()
    }
}

function Reset-McpTodoConnectionState {
    $script:McpBaseUrl = $null
    $script:McpApiKey = $null
    $script:McpWorkspacePath = $null
    $script:McpHeaders = @{}
}

function Throw-McpUntrusted {
    param([Parameter(Mandatory)][string]$Reason)

    Reset-McpTodoConnectionState
    $message = "MCP_UNTRUSTED: $Reason"
    Write-Warning $message
    throw $message
}

function Invoke-McpTrustedHealthCheck {
    param([Parameter(Mandatory)][string]$BaseUrl)

    $nonce = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(18)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    $separator = if ($BaseUrl.Contains('?')) { '&' } else { '?' }
    $health = Invoke-RestMethod -Uri "$BaseUrl/health${separator}nonce=$nonce" -TimeoutSec 5
    if ([string]$health.nonce -ne $nonce) {
        Throw-McpUntrusted -Reason "The /health response nonce did not match the caller nonce."
    }

    return [string]$health.status
}

function Initialize-McpTodo {
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
        $script:McpWorkspacePath = (Get-Location).Path
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
        $status = Invoke-McpTrustedHealthCheck -BaseUrl $script:McpBaseUrl
        Write-Host "Connected to MCP server at $($script:McpBaseUrl) - status: $status" -ForegroundColor Green
    } catch {
        if ($_.Exception.Message -like 'MCP_UNTRUSTED:*') {
            throw
        }

        Throw-McpUntrusted -Reason "The /health trust handshake failed: $($_.Exception.Message)"
    }
}

# ─── Read ────────────────────────────────────────────────────────────────────

function Get-McpTodo {
    <#
    .SYNOPSIS  List all todos, or get a specific todo by ID.
    .PARAMETER Id  Optional todo ID. If omitted, lists all todos.
    #>
    [CmdletBinding()]
    param(
        [string]$Id
    )
    Assert-Initialized

    if ($Id) {
        $uri = "$($script:McpBaseUrl)/mcpserver/todo/$Id"
        return Invoke-RestMethod -Uri $uri -Headers $script:McpHeaders
    } else {
        $uri = "$($script:McpBaseUrl)/mcpserver/todo"
        $result = Invoke-RestMethod -Uri $uri -Headers $script:McpHeaders
        return $result.items
    }
}

function Get-McpTodoPrompt {
    <#
    .SYNOPSIS  Get an AI prompt for a todo (implement, plan, or status).
    .PARAMETER Id    The todo ID.
    .PARAMETER Type  Prompt type: "implement", "plan", or "status".
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][ValidateSet("implement","plan","status")][string]$Type
    )
    Assert-Initialized
    $uri = "$($script:McpBaseUrl)/mcpserver/todo/$Id/prompt/$Type"
    return Invoke-RestMethod -Uri $uri -Headers $script:McpHeaders
}

# ─── Create ──────────────────────────────────────────────────────────────────

function New-McpTodo {
    <#
    .SYNOPSIS  Create a new todo item.
    .PARAMETER Id                   Unique kebab-case ID (e.g. "add-jwt-auth").
    .PARAMETER Title                Brief title.
    .PARAMETER Section              Grouping category (e.g. "Backend", "Frontend").
    .PARAMETER Priority             Priority: "critical", "high", "medium", or "low".
    .PARAMETER Estimate             Effort estimate (e.g. "2h", "1d").
    .PARAMETER Description          Array of description lines.
    .PARAMETER TechnicalDetails     Array of technical notes.
    .PARAMETER ImplementationTasks  Array of hashtables: @{ task="..."; done=$false }.
    .PARAMETER DependsOn            Array of prerequisite todo IDs.
    .PARAMETER FunctionalRequirements  Array of FR IDs.
    .PARAMETER TechnicalRequirements   Array of TR IDs.
    .PARAMETER Note                 Additional context.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Section,
        [Parameter(Mandatory)][ValidateSet("critical","high","medium","low")][string]$Priority,
        [string]$Estimate,
        [string[]]$Description,
        [string[]]$TechnicalDetails,
        [hashtable[]]$ImplementationTasks,
        [string[]]$DependsOn,
        [string[]]$FunctionalRequirements,
        [string[]]$TechnicalRequirements,
        [string]$Note
    )
    Assert-Initialized

    $todo = @{
        id       = $Id
        title    = $Title
        section  = $Section
        priority = $Priority
    }

    if ($Estimate)                { $todo.estimate                = $Estimate }
    if ($Description)             { $todo.description             = $Description }
    if ($TechnicalDetails)        { $todo.technicalDetails        = $TechnicalDetails }
    if ($Note)                    { $todo.note                    = $Note }
    if ($DependsOn)               { $todo.dependsOn               = $DependsOn }
    if ($FunctionalRequirements)  { $todo.functionalRequirements  = $FunctionalRequirements }
    if ($TechnicalRequirements)   { $todo.technicalRequirements   = $TechnicalRequirements }
    if ($ImplementationTasks) {
        $todo.implementationTasks = $ImplementationTasks | ForEach-Object {
            @{ task = $_.task; done = [bool]$_.done }
        }
    }

    $body = $todo | ConvertTo-Json -Depth 5
    return Invoke-RestMethod -Uri "$($script:McpBaseUrl)/mcpserver/todo" -Method Post -Headers $script:McpHeaders -Body $body
}

# ─── Update ──────────────────────────────────────────────────────────────────

function Update-McpTodo {
    <#
    .SYNOPSIS  Update fields on an existing todo. Only include fields you want to change.
    .PARAMETER Id                   The todo ID.
    .PARAMETER Title                Updated title.
    .PARAMETER Priority             Updated priority.
    .PARAMETER Section              Updated section.
    .PARAMETER Done                 Set to $true to mark complete.
    .PARAMETER Estimate             Updated estimate.
    .PARAMETER Description          Updated description lines.
    .PARAMETER TechnicalDetails     Updated technical notes.
    .PARAMETER ImplementationTasks  Updated subtasks: @{ task="..."; done=$true }.
    .PARAMETER Remaining            What work remains.
    .PARAMETER DoneSummary          Summary of completed work.
    .PARAMETER CompletedDate        ISO 8601 completion date.
    .PARAMETER Note                 Updated note.
    .PARAMETER DependsOn            Updated dependency list.
    .PARAMETER FunctionalRequirements  Updated FR IDs.
    .PARAMETER TechnicalRequirements   Updated TR IDs.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Id,
        [string]$Title,
        [ValidateSet("critical","high","medium","low")][string]$Priority,
        [string]$Section,
        [Nullable[bool]]$Done,
        [string]$Estimate,
        [string[]]$Description,
        [string[]]$TechnicalDetails,
        [hashtable[]]$ImplementationTasks,
        [string]$Remaining,
        [string]$DoneSummary,
        [string]$CompletedDate,
        [string]$Note,
        [string[]]$DependsOn,
        [string[]]$FunctionalRequirements,
        [string[]]$TechnicalRequirements
    )
    Assert-Initialized

    $update = @{}
    if ($PSBoundParameters.ContainsKey('Title'))                { $update.title                = $Title }
    if ($PSBoundParameters.ContainsKey('Priority'))             { $update.priority             = $Priority }
    if ($PSBoundParameters.ContainsKey('Section'))              { $update.section              = $Section }
    if ($PSBoundParameters.ContainsKey('Done'))                 { $update.done                 = $Done }
    if ($PSBoundParameters.ContainsKey('Estimate'))             { $update.estimate             = $Estimate }
    if ($PSBoundParameters.ContainsKey('Description'))          { $update.description          = $Description }
    if ($PSBoundParameters.ContainsKey('TechnicalDetails'))     { $update.technicalDetails     = $TechnicalDetails }
    if ($PSBoundParameters.ContainsKey('Remaining'))            { $update.remaining            = $Remaining }
    if ($PSBoundParameters.ContainsKey('DoneSummary'))          { $update.doneSummary          = $DoneSummary }
    if ($PSBoundParameters.ContainsKey('CompletedDate'))        { $update.completedDate        = $CompletedDate }
    if ($PSBoundParameters.ContainsKey('Note'))                 { $update.note                 = $Note }
    if ($PSBoundParameters.ContainsKey('DependsOn'))            { $update.dependsOn            = $DependsOn }
    if ($PSBoundParameters.ContainsKey('FunctionalRequirements')) { $update.functionalRequirements = $FunctionalRequirements }
    if ($PSBoundParameters.ContainsKey('TechnicalRequirements'))  { $update.technicalRequirements  = $TechnicalRequirements }
    if ($ImplementationTasks) {
        $update.implementationTasks = $ImplementationTasks | ForEach-Object {
            @{ task = $_.task; done = [bool]$_.done }
        }
    }

    $body = $update | ConvertTo-Json -Depth 5
    return Invoke-RestMethod -Uri "$($script:McpBaseUrl)/mcpserver/todo/$Id" -Method Put -Headers $script:McpHeaders -Body $body
}

# ─── Complete ────────────────────────────────────────────────────────────────

function Complete-McpTodo {
    <#
    .SYNOPSIS  Mark a todo as done with a completion summary.
    .PARAMETER Id           The todo ID.
    .PARAMETER DoneSummary  Summary of what was accomplished.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$DoneSummary
    )
    Assert-Initialized

    $update = @{
        done          = $true
        completedDate = (Get-Date).ToUniversalTime().ToString("o")
        doneSummary   = $DoneSummary
    }
    $body = $update | ConvertTo-Json -Depth 5
    return Invoke-RestMethod -Uri "$($script:McpBaseUrl)/mcpserver/todo/$Id" -Method Put -Headers $script:McpHeaders -Body $body
}

# ─── Delete ──────────────────────────────────────────────────────────────────

function Remove-McpTodo {
    <#
    .SYNOPSIS  Delete a todo item.
    .PARAMETER Id  The todo ID.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Id
    )
    Assert-Initialized
    Invoke-RestMethod -Uri "$($script:McpBaseUrl)/mcpserver/todo/$Id" -Method Delete -Headers $script:McpHeaders | Out-Null
    Write-Host "Deleted todo: $Id" -ForegroundColor Yellow
}

# ─── Requirements ────────────────────────────────────────────────────────────

function Add-McpTodoRequirements {
    <#
    .SYNOPSIS  Add requirements to a todo.
    .PARAMETER Id                      The todo ID.
    .PARAMETER FunctionalRequirements  FR IDs to add.
    .PARAMETER TechnicalRequirements   TR IDs to add.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Id,
        [string[]]$FunctionalRequirements,
        [string[]]$TechnicalRequirements
    )
    Assert-Initialized

    $body = @{}
    if ($FunctionalRequirements) { $body.functionalRequirements = $FunctionalRequirements }
    if ($TechnicalRequirements)  { $body.technicalRequirements  = $TechnicalRequirements }

    $json = $body | ConvertTo-Json -Depth 3
    return Invoke-RestMethod -Uri "$($script:McpBaseUrl)/mcpserver/todo/$Id/requirements" -Method Post -Headers $script:McpHeaders -Body $json
}

# ─── Helpers ─────────────────────────────────────────────────────────────────

function Assert-Initialized {
    if (-not $script:McpBaseUrl) {
        throw "MCP todo not initialized. Call Initialize-McpTodo first."
    }
}

# ─── Exports ─────────────────────────────────────────────────────────────────
Export-ModuleMember -Function @(
    'Initialize-McpTodo',
    'Get-McpTodo',
    'Get-McpTodoPrompt',
    'New-McpTodo',
    'Update-McpTodo',
    'Complete-McpTodo',
    'Remove-McpTodo',
    'Add-McpTodoRequirements'
)
