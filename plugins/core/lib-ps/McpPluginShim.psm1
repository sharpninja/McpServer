#Requires -Version 7.0

Set-StrictMode -Version Latest

class McpPluginInvocationOptions {
    # Command selected by Invoke-McpPlugin.ps1: Status, Invoke, or CompleteTurn.
    [string]$Command

    # Workflow or client method forwarded to repl-invoke.ps1 when Command is Invoke.
    [string]$Method

    # Inline YAML parameters supplied by the caller.
    [string]$Params

    # Optional path to a YAML parameter document.
    [string]$ParamsPath

    # Inline final response text supplied by the caller.
    [string]$Response

    # Optional path to a final response document.
    [string]$ResponsePath

    # Workspace directory used as process working directory and marker scope.
    [string]$WorkspacePath

    # Plugin root that contains lib, hooks, skills, and manifest files.
    [string]$PluginRoot

    # Optional cache root override used by tests and plugin package hosts.
    [string]$CacheRoot

    # Child process timeout in seconds.
    [int]$TimeoutSeconds

    McpPluginInvocationOptions(
        [string]$command,
        [string]$method,
        [string]$params,
        [string]$paramsPath,
        [string]$response,
        [string]$responsePath,
        [string]$workspacePath,
        [string]$pluginRoot,
        [string]$cacheRoot,
        [int]$timeoutSeconds
    ) {
        $this.Command = $command
        $this.Method = $method
        $this.Params = $params
        $this.ParamsPath = $paramsPath
        $this.Response = $response
        $this.ResponsePath = $responsePath
        $this.WorkspacePath = $workspacePath
        $this.PluginRoot = $pluginRoot
        $this.CacheRoot = $cacheRoot
        $this.TimeoutSeconds = $timeoutSeconds
    }
}

class McpPluginReplRequest {
    # Envelope type sent to mcpserver-repl.
    [string]$Type = 'request'

    # Correlation ID generated for the request envelope.
    [string]$RequestId

    # Workflow or client method to invoke.
    [string]$Method

    # Optional method parameters already parsed to PowerShell objects.
    [object]$Params

    McpPluginReplRequest([string]$requestId, [string]$method, [object]$params) {
        $this.RequestId = $requestId
        $this.Method = $method
        $this.Params = $params
    }

    [object] ToEnvelopeObject() {
        $payload = [ordered]@{
            requestId = $this.RequestId
            method = $this.Method
        }

        if ($null -ne $this.Params) {
            $payload.params = $this.Params
        }

        return [ordered]@{
            type = $this.Type
            payload = $payload
        }
    }
}

class McpPluginReplResult {
    # True when the REPL process completed successfully and did not return an error envelope.
    [bool]$Success

    # Raw YAML output emitted by mcpserver-repl.
    [string]$Output

    # Process exit code when one is available; null for pre-process failures.
    [System.Nullable[int]]$ExitCode

    # Error message captured by the shim when invocation failed before a result envelope.
    [string]$Error

    McpPluginReplResult([bool]$success, [string]$output, [System.Nullable[int]]$exitCode, [string]$error) {
        $this.Success = $success
        $this.Output = $output
        $this.ExitCode = $exitCode
        $this.Error = $error
    }
}

class McpPluginSessionMeta {
    # Agent or source type prefix recorded in session-state.yaml.
    [string]$SourceType

    # Durable MCP session identifier.
    [string]$SessionId

    McpPluginSessionMeta([string]$sourceType, [string]$sessionId) {
        $this.SourceType = $sourceType
        $this.SessionId = $sessionId
    }
}

class McpPluginActionRecord {
    # Mutable action fields parsed from an actions YAML block.
    [System.Collections.Specialized.OrderedDictionary]$Values

    McpPluginActionRecord([hashtable]$values) {
        $this.Values = [ordered]@{}
        foreach ($key in $values.Keys) {
            $this.Values[$key] = $values[$key]
        }
    }

    [object] ToMap() {
        return $this.Values
    }
}

class McpPluginTurnUpsertRequest {
    # Session log source type or agent name.
    [string]$Agent

    # Session identifier to upsert into.
    [string]$SessionId

    # Ordered turn payload accepted by client.SessionLog.UpsertTurnAsync.
    [System.Collections.Specialized.OrderedDictionary]$Turn

    McpPluginTurnUpsertRequest([string]$agent, [string]$sessionId, [System.Collections.Specialized.OrderedDictionary]$turn) {
        $this.Agent = $agent
        $this.SessionId = $sessionId
        $this.Turn = $turn
    }

    [object] ToParamsObject() {
        return [ordered]@{
            agent = $this.Agent
            sessionId = $this.SessionId
            turn = $this.Turn
        }
    }
}

class McpPluginFailsafeRecord {
    # Method that should be replayed from the failsafe record.
    [string]$Method

    # Human-readable queue label used in the failsafe filename.
    [string]$Label

    # UTC timestamp string written into the failsafe document.
    [string]$Timestamp

    # YAML or JSON payload body indented under params.
    [string]$ParamsYaml

    # Optional absolute path where the failsafe record is written.
    [string]$Path

    McpPluginFailsafeRecord([string]$method, [string]$label, [string]$timestamp, [string]$paramsYaml, [string]$path) {
        $this.Method = $method
        $this.Label = $label
        $this.Timestamp = $timestamp
        $this.ParamsYaml = $paramsYaml
        $this.Path = $path
    }

    [string] ToYaml() {
        $doc = "method: $($this.Method)`nlabel: $($this.Label)`ntimestamp: $($this.Timestamp)`nparams:`n"
        $doc += (($this.ParamsYaml -split "`n" | ForEach-Object { "  $_" }) -join "`n") + "`n"
        return $doc
    }
}

function New-McpPluginInvocationOptions {
    <#
    .SYNOPSIS
        Creates the Invoke-McpPlugin.ps1 invocation DTO.
    .DESCRIPTION
        Returns McpPluginInvocationOptions, the DTO that documents and carries
        every public wrapper parameter used by Invoke-McpPlugin.ps1.

        Members:
        Command - Status, Invoke, or CompleteTurn.
        Method - Workflow or client method used when Command is Invoke.
        Params - Inline YAML parameter text.
        ParamsPath - File containing YAML parameter text.
        Response - Inline response text for CompleteTurn.
        ResponsePath - File containing response text for CompleteTurn.
        WorkspacePath - Workspace directory and child process working directory.
        PluginRoot - Plugin root containing lib, hooks, skills, and manifest.
        CacheRoot - Optional cache directory override.
        TimeoutSeconds - Child process timeout in seconds.
    .EXAMPLE
        New-McpPluginInvocationOptions -Command Invoke -Method workflow.todo.query -Params 'done: false' -WorkspacePath F:\GitHub\McpServer -PluginRoot $env:MCP_PLUGIN_ROOT -TimeoutSeconds 90
    #>
    [CmdletBinding()]
    param(
        [ValidateSet('Status', 'Invoke', 'CompleteTurn')]
        [string]$Command = 'Status',
        [string]$Method = '',
        [string]$Params = '',
        [string]$ParamsPath = '',
        [string]$Response = '',
        [string]$ResponsePath = '',
        [string]$WorkspacePath = '',
        [string]$PluginRoot = '',
        [string]$CacheRoot = '',
        [int]$TimeoutSeconds = 90
    )

    return [McpPluginInvocationOptions]::new(
        $Command,
        $Method,
        $Params,
        $ParamsPath,
        $Response,
        $ResponsePath,
        $WorkspacePath,
        $PluginRoot,
        $CacheRoot,
        $TimeoutSeconds)
}

function New-McpPluginReplRequest {
    <#
    .SYNOPSIS
        Creates a typed REPL request envelope DTO.
    .DESCRIPTION
        Returns McpPluginReplRequest for the single-line JSON envelope sent to
        mcpserver-repl.

        Members:
        Type - Envelope type, always request.
        RequestId - Correlation ID for the REPL request.
        Method - Workflow or client method name.
        Params - Optional parsed parameter object included as payload.params.
    .EXAMPLE
        $request = New-McpPluginReplRequest -RequestId req-20260628T010000Z-demo -Method workflow.todo.query -Params @{ done = $false }
        ConvertTo-McpPluginJson $request
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$Method,
        [object]$Params = $null
    )

    return [McpPluginReplRequest]::new($RequestId, $Method, $Params)
}

function New-McpPluginReplResult {
    <#
    .SYNOPSIS
        Creates the REPL invocation result DTO.
    .DESCRIPTION
        Returns McpPluginReplResult, the object consumed by callers after a
        PowerShell shim invocation.

        Members:
        Success - Boolean success flag after process and envelope checks.
        Output - Raw YAML output emitted by mcpserver-repl.
        ExitCode - Child process exit code when known.
        Error - Error text for pre-process or catch-path failures.
    .EXAMPLE
        New-McpPluginReplResult -Success $true -Output 'type: result' -ExitCode 0
    #>
    [CmdletBinding()]
    param(
        [bool]$Success,
        [string]$Output = '',
        [System.Nullable[int]]$ExitCode = $null,
        [string]$Error = ''
    )

    return [McpPluginReplResult]::new($Success, $Output, $ExitCode, $Error)
}

function New-McpPluginSessionMeta {
    <#
    .SYNOPSIS
        Creates the session metadata DTO.
    .DESCRIPTION
        Returns McpPluginSessionMeta for values read from session-state.yaml.

        Members:
        SourceType - Agent/source prefix used for session log persistence.
        SessionId - Durable MCP session identifier.
    .EXAMPLE
        New-McpPluginSessionMeta -SourceType Codex -SessionId Codex-20260628T010000Z-demo
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SourceType,
        [Parameter(Mandatory)][string]$SessionId
    )

    return [McpPluginSessionMeta]::new($SourceType, $SessionId)
}

function New-McpPluginActionRecord {
    <#
    .SYNOPSIS
        Creates an action-record DTO from parsed YAML action fields.
    .DESCRIPTION
        Returns McpPluginActionRecord. The DTO preserves all input keys in the
        Values member and emits an ordered map through ToMap().

        Members:
        Values - Ordered map of action fields such as type, status, filePath,
        message, command, or any future action member.
    .EXAMPLE
        $action = New-McpPluginActionRecord -Values @{ type = 'edit'; filePath = 'src/App.cs' }
        $action.ToMap()
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Values
    )

    return [McpPluginActionRecord]::new($Values)
}

function New-McpPluginTurnUpsertRequest {
    <#
    .SYNOPSIS
        Creates the session-log turn upsert DTO.
    .DESCRIPTION
        Returns McpPluginTurnUpsertRequest for client.SessionLog.UpsertTurnAsync.

        Members:
        Agent - Source type or agent name.
        SessionId - Target session identifier.
        Turn - Ordered turn payload with requestId, timestamp, queryText,
        queryTitle, response, status, model, tokenCount, filesModified,
        actions, and processingDialog when those optional collections are
        present.
    .EXAMPLE
        New-McpPluginTurnUpsertRequest -Agent Codex -SessionId Codex-1 -RequestId req-1 -Timestamp 2026-06-28T01:00:00Z -QueryText Hello -Title Hello -Status completed -Model codex
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Agent,
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$Timestamp,
        [Parameter(Mandatory)][string]$QueryText,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [Parameter(Mandatory)][string]$Model,
        [string[]]$FilesModified = @(),
        [object[]]$Actions = @(),
        [object[]]$ProcessingDialog = @()
    )

    $turn = [ordered]@{
        requestId = $RequestId
        timestamp = $Timestamp
        queryText = $QueryText
        queryTitle = $Title
        response = $ResponseText
        status = $Status
        model = $Model
        tokenCount = 0
    }

    if ($FilesModified.Count -gt 0) {
        $turn.filesModified = @($FilesModified)
    }

    if ($Actions.Count -gt 0) {
        $turn.actions = @($Actions)
    }

    if ($ProcessingDialog.Count -gt 0) {
        $turn.processingDialog = @($ProcessingDialog)
    }

    return [McpPluginTurnUpsertRequest]::new($Agent, $SessionId, $turn)
}

function New-McpPluginFailsafeRecord {
    <#
    .SYNOPSIS
        Creates a failsafe replay record DTO.
    .DESCRIPTION
        Returns McpPluginFailsafeRecord for records written when a REPL call must
        be replayable after a failure.

        Members:
        Method - Workflow or client method to replay.
        Label - Queue label used in the filename.
        Timestamp - UTC timestamp written into the YAML document.
        ParamsYaml - YAML or JSON payload body indented below params.
        Path - Optional absolute path where the record is stored.
    .EXAMPLE
        New-McpPluginFailsafeRecord -Method client.SessionLog.UpsertTurnAsync -Label session_upsertTurn -Timestamp 20260628T010000Z -ParamsYaml '{}'
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$Timestamp,
        [Parameter(Mandatory)][string]$ParamsYaml,
        [string]$Path = ''
    )

    return [McpPluginFailsafeRecord]::new($Method, $Label, $Timestamp, $ParamsYaml, $Path)
}

function ConvertTo-McpPluginJson {
    <#
    .SYNOPSIS
        Serializes shim DTOs through PowerShell-native JSON.
    .DESCRIPTION
        Converts a supported DTO to its wire object before calling
        ConvertTo-Json. McpPluginReplRequest uses ToEnvelopeObject(); any other
        object is serialized directly.

        Parameters:
        InputObject - DTO or PowerShell object to serialize.
        Depth - JSON depth forwarded to ConvertTo-Json.
        Compress - Emits compact JSON when set.
    .EXAMPLE
        ConvertTo-McpPluginJson -InputObject $request -Depth 20 -Compress
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)][object]$InputObject,
        [int]$Depth = 20,
        [switch]$Compress
    )

    process {
        $serializable = $InputObject
        if ($InputObject -is [McpPluginReplRequest]) {
            $serializable = $InputObject.ToEnvelopeObject()
        } elseif ($InputObject -is [McpPluginTurnUpsertRequest]) {
            $serializable = $InputObject.ToParamsObject()
        } elseif ($InputObject -is [McpPluginActionRecord]) {
            $serializable = $InputObject.ToMap()
        }

        if ($Compress) {
            return ($serializable | ConvertTo-Json -Depth $Depth -Compress)
        }

        return ($serializable | ConvertTo-Json -Depth $Depth)
    }
}

Export-ModuleMember -Function @(
    'New-McpPluginInvocationOptions',
    'New-McpPluginReplRequest',
    'New-McpPluginReplResult',
    'New-McpPluginSessionMeta',
    'New-McpPluginActionRecord',
    'New-McpPluginTurnUpsertRequest',
    'New-McpPluginFailsafeRecord',
    'ConvertTo-McpPluginJson'
)
