<#
.SYNOPSIS
    Sends a YAML request envelope through the PowerShell MCP runtime.
.DESCRIPTION
    Constructs a YAML envelope and routes it through the configured
    PowerShell MCP invocation path.

    Translation shim: workflow.sessionlog.* methods are not server routes
    — the dispatcher rejects them as method_not_found. They are plugin-
    local verbs that update cache/current-turn.yaml so the Stop hook can
    verify completion, and persist a session-log turn via the real
    client.SessionLog.SubmitAsync route.

    Two usage modes:
      1. Script entry: pwsh -File repl-invoke.ps1 -Method <m> [-ParamsYaml <y>]
      2. Dot-source for Invoke-ReplMethod cmdlet:
             . .\repl-invoke.ps1
             Invoke-ReplMethod -Method workflow.sessionlog.completeTurn ...
#>
[CmdletBinding()]
param(
    [string]$Method,
    [string]$ParamsYaml = ''
)

$ErrorActionPreference = 'Stop'

$shimModule = Join-Path $PSScriptRoot 'McpPluginShim.psm1'
Import-Module $shimModule -ErrorAction Stop
. (Join-Path $PSScriptRoot 'yaml-object-mutation.ps1')
Import-McpYamlSerializer
if (-not (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'marker-resolver.ps1')
}

$script:ReplInvokePluginRoot = if ($env:PLUGIN_ROOT_OVERRIDE) {
    $env:PLUGIN_ROOT_OVERRIDE
} else {
    Split-Path -Parent $PSScriptRoot
}

# Agent for per-agent REPL cache and isolation. Must be passed to every mcpserver-repl call.
$script:AgentName = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME }
                   elseif ($env:PLUGIN_AGENT_NAME) { $env:PLUGIN_AGENT_NAME }
                   elseif ($env:PLUGIN_AGENT_DEFAULT) { $env:PLUGIN_AGENT_DEFAULT }
                   else { 'default' }

if (-not (Get-Command Resolve-McpCacheDir -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'resolve-cache-dir.ps1')
}

# Resolved lazily so per-call context (workspace / env) governs path.
function script:Get-ReplInvokeCacheDir { Resolve-McpCacheDir }

function Resolve-ReplWorkspaceDirectory {
    $candidates = @(
        $env:MCP_WORKSPACE_PATH,
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:MCP_WORKSPACE_START_DIR,
        $env:CLAUDE_PROJECT_DIR
    )

    try {
        $location = Get-Location
        if ($location.Provider.Name -eq 'FileSystem') {
            $candidates += $location.ProviderPath
        }
    } catch {
        # Fall through to the .NET current directory fallback.
    }

    $candidates += [Environment]::CurrentDirectory

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return (Resolve-Path -LiteralPath $candidate).ProviderPath
        }
    }

    return (Get-Location).ProviderPath
}

function Assert-ReplMarkerFresh {
    $workspace = Resolve-ReplWorkspaceDirectory
    $sessionFile = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'

    try {
        $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
        $state = Read-McpYamlObject -Path $sessionFile -Create
        if ($state -isnot [System.Collections.IDictionary]) {
            $state = [ordered]@{}
        }

        $status = if ($state.Contains('status')) { [string]$state['status'] } else { '' }
        $cachedPath = if ($state.Contains('markerFilePath')) { [string]$state['markerFilePath'] } else { '' }
        $cachedWriteUtc = if ($state.Contains('markerLastWriteUtc')) { [string]$state['markerLastWriteUtc'] } else { '' }

        if ($status -eq 'verified' -and
            $cachedPath -eq $snapshot.markerFilePath -and
            $cachedWriteUtc -eq $snapshot.markerLastWriteUtc) {
            return $true
        }

        if (-not (Invoke-FullBootstrap -StartDir $workspace)) {
            throw 'marker bootstrap failed'
        }

        $state['status'] = 'verified'
        if (-not $state.Contains('agent')) {
            $state['agent'] = $script:AgentName
        }
        $state['lastUpdated'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $state['markerFilePath'] = $snapshot.markerFilePath
        $state['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        Write-McpYamlObject -Path $sessionFile -Document $state
        return $true
    } catch {
        $untrustedState = [ordered]@{
            status = 'MCP_UNTRUSTED'
            lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        try {
            $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
            $untrustedState['markerFilePath'] = $snapshot.markerFilePath
            $untrustedState['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        } catch {
        }
        Write-McpYamlObject -Path $sessionFile -Document $untrustedState
        [Console]::Error.WriteLine("MCP_UNTRUSTED: marker refresh failed before REPL request: $_")
        return $false
    }
}

function Set-ReplProcessWorkspace {
    param([Parameter(Mandatory)][System.Diagnostics.ProcessStartInfo]$StartInfo)

    $workspace = Resolve-ReplWorkspaceDirectory
    $StartInfo.WorkingDirectory = $workspace
    $StartInfo.Environment['MCP_WORKSPACE_PATH'] = $workspace
    $StartInfo.Environment['MCPSERVER_WORKSPACE_PATH'] = $workspace
    $StartInfo.Environment['MCP_WORKSPACE_START_DIR'] = $workspace
    $StartInfo.Environment['CLAUDE_PROJECT_DIR'] = $workspace
}

function Convert-ReplParamsYamlToObject {
    param([string]$ParamsYaml)

    if (-not $ParamsYaml) { return $null }

    $normalized = $ParamsYaml -replace "`r`n", "`n" -replace "`r", ""
    if ($normalized.TrimStart() -match '^[\{\[]') {
        return ($normalized | ConvertFrom-Json -Depth 100 -ErrorAction Stop)
    }

    if (Get-Command ConvertFrom-Yaml -ErrorAction SilentlyContinue) {
        try {
            return ($normalized | ConvertFrom-Yaml -ErrorAction Stop)
        } catch {
            # Fall through to the local subset parser so plugin runtime remains
            # self-contained when the optional YAML module cannot parse input.
        }
    }

    return (ConvertFrom-ReplYamlSubset -Text $normalized)
}

function ConvertFrom-ReplYamlScalar {
    param([string]$Value)

    $trimmed = $Value.Trim()
    if ($trimmed -eq '') { return '' }
    if ($trimmed -eq '{}') { return [ordered]@{} }
    if ($trimmed -eq '[]') { return @() }
    if ($trimmed -match '^(true|false)$') { return [bool]::Parse($trimmed) }
    if ($trimmed -match '^-?\d+$') { return [int64]$trimmed }
    if (($trimmed.StartsWith('"') -and $trimmed.EndsWith('"')) -or ($trimmed.StartsWith("'") -and $trimmed.EndsWith("'"))) {
        return $trimmed.Substring(1, $trimmed.Length - 2)
    }

    return $trimmed
}

function Get-ReplYamlIndent {
    param([string]$Line)

    return ([regex]::Match($Line, '^\s*').Value.Length)
}

function ConvertFrom-ReplYamlSubset {
    param([string]$Text)

    $lines = @($Text -split "`n")
    $index = 0
    return (Read-ReplYamlBlock -Lines $lines -Index ([ref]$index) -Indent 0)
}

function Read-ReplYamlBlock {
    param(
        [string[]]$Lines,
        [ref]$Index,
        [int]$Indent
    )

    while ($Index.Value -lt $Lines.Count -and $Lines[$Index.Value].Trim() -eq '') {
        $Index.Value++
    }

    if ($Index.Value -ge $Lines.Count) { return [ordered]@{} }

    $first = $Lines[$Index.Value]
    $isList = (Get-ReplYamlIndent $first) -eq $Indent -and $first.Substring($Indent).TrimStart().StartsWith('- ')
    if ($isList) {
        $items = [System.Collections.Generic.List[object]]::new()
        while ($Index.Value -lt $Lines.Count) {
            $line = $Lines[$Index.Value]
            if ($line.Trim() -eq '') { $Index.Value++; continue }
            $currentIndent = Get-ReplYamlIndent $line
            if ($currentIndent -lt $Indent) { break }
            if ($currentIndent -ne $Indent) { break }
            $content = $line.Substring($Indent).TrimStart()
            if (-not $content.StartsWith('- ')) { break }

            $itemText = $content.Substring(2).Trim()
            $Index.Value++
            if ($itemText -match '^([^:]+):\s*(.*)$') {
                $item = [ordered]@{}
                $key = $Matches[1].Trim()
                $value = $Matches[2]
                if ($value -eq '') {
                    $item[$key] = Read-ReplYamlBlock -Lines $Lines -Index $Index -Indent ($Indent + 2)
                } else {
                    $item[$key] = ConvertFrom-ReplYamlScalar $value
                }

                while ($Index.Value -lt $Lines.Count) {
                    $nextLine = $Lines[$Index.Value]
                    if ($nextLine.Trim() -eq '') { $Index.Value++; continue }
                    $nextIndent = Get-ReplYamlIndent $nextLine
                    if ($nextIndent -le $Indent) { break }
                    $nextContent = $nextLine.Substring($nextIndent)
                    if ($nextContent -notmatch '^([^:]+):\s*(.*)$') { break }
                    $nextKey = $Matches[1].Trim()
                    $nextValue = $Matches[2]
                    $Index.Value++
                    if ($nextValue -eq '|') {
                        $item[$nextKey] = Read-ReplYamlLiteralBlock -Lines $Lines -Index $Index -Indent ($nextIndent + 2)
                    } elseif ($nextValue -eq '') {
                        $item[$nextKey] = Read-ReplYamlBlock -Lines $Lines -Index $Index -Indent ($nextIndent + 2)
                    } else {
                        $item[$nextKey] = ConvertFrom-ReplYamlScalar $nextValue
                    }
                }

                $items.Add([pscustomobject]$item)
            } else {
                $items.Add((ConvertFrom-ReplYamlScalar $itemText))
            }
        }

        return $items.ToArray()
    }

    $map = [ordered]@{}
    while ($Index.Value -lt $Lines.Count) {
        $line = $Lines[$Index.Value]
        if ($line.Trim() -eq '') { $Index.Value++; continue }
        $currentIndent = Get-ReplYamlIndent $line
        if ($currentIndent -lt $Indent) { break }
        if ($currentIndent -gt $Indent) { break }

        $content = $line.Substring($Indent)
        if ($content -notmatch '^([^:]+):\s*(.*)$') { $Index.Value++; continue }
        $key = $Matches[1].Trim()
        $value = $Matches[2]
        $Index.Value++

        if ($value -eq '|') {
            $map[$key] = Read-ReplYamlLiteralBlock -Lines $Lines -Index $Index -Indent ($Indent + 2)
        } elseif ($value -eq '') {
            $map[$key] = Read-ReplYamlBlock -Lines $Lines -Index $Index -Indent ($Indent + 2)
        } else {
            $map[$key] = ConvertFrom-ReplYamlScalar $value
        }
    }

    return [pscustomobject]$map
}

function Read-ReplYamlLiteralBlock {
    param(
        [string[]]$Lines,
        [ref]$Index,
        [int]$Indent
    )

    $items = [System.Collections.Generic.List[string]]::new()
    while ($Index.Value -lt $Lines.Count) {
        $line = $Lines[$Index.Value]
        if ($line.Trim() -ne '' -and (Get-ReplYamlIndent $line) -lt $Indent) { break }
        if ($line.Length -ge $Indent) {
            $items.Add($line.Substring($Indent))
        } else {
            $items.Add('')
        }
        $Index.Value++
    }

    return ($items -join "`n").TrimEnd()
}

function ConvertTo-ReplCanonicalSessionId {
    param([Parameter(Mandatory)][string]$SessionId)

    if ($SessionId -match '^[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$') {
        return $SessionId
    }

    if ($SessionId -match '^(?<agent>[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)*)-(?<stamp>\d{8}T\d{6}Z)$') {
        return '{0}-{1}-plugin-session' -f $Matches['agent'], $Matches['stamp']
    }

    return $SessionId
}

function Get-ReplSessionMeta {
    $f = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    if (-not (Test-Path $f)) { return $null }
    $line = Select-String -Path $f -Pattern '^sessionId:' -SimpleMatch:$false |
        Select-Object -First 1
    if (-not $line) { return $null }
    $sid = ($line.Line -replace '^sessionId:\s*', '').Trim()
    if (-not $sid) { return $null }
    $canonicalSessionId = ConvertTo-ReplCanonicalSessionId -SessionId $sid
    if ($canonicalSessionId -ne $sid) {
        $state = Read-McpYamlObject -Path $f
        $state['sessionId'] = $canonicalSessionId
        if (-not $state.Contains('lastUpdated')) {
            $state['lastUpdated'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        Write-McpYamlObject -Path $f -Document $state
        $sid = $canonicalSessionId
    }
    $prefix = ($sid -split '-', 2)[0]
    New-McpPluginSessionMeta -SourceType $prefix -SessionId $sid
}

function Invoke-ReplRaw {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )
    if (-not (Assert-ReplMarkerFresh)) {
        return (New-McpPluginReplResult -Success $false -Output '' -Error 'MCP_UNTRUSTED: marker refresh failed before REPL request')
    }

    if (-not (Get-Command mcpserver-repl -ErrorAction SilentlyContinue)) {
        Write-Error 'mcpserver-repl not found on PATH'
        return (New-McpPluginReplResult -Success $false -Output '' -Error 'mcpserver-repl not found on PATH')
    }

    $requestId = "req-$(Get-Date -AsUTC -Format 'yyyyMMddTHHmmssZ')-$((Get-Random -Maximum 0xFFFF).ToString('x4'))"
    $timeout = if ($env:REPL_TIMEOUT) { [int]$env:REPL_TIMEOUT } else { 30 }

    # Build as an object and serialize to JSON so request envelopes keep a
    # single canonical shape across plugin hosts.
    $paramsObject = $null
    if ($ParamsYaml) {
        $paramsObject = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    }
    $request = New-McpPluginReplRequest -RequestId $requestId -Method $Method -Params $paramsObject
    $envelope = ConvertTo-McpPluginJson -InputObject $request -Depth 20 -Compress

    try {
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = 'mcpserver-repl'
        $psi.ArgumentList.Add('--agent-stdio')
        if ($script:AgentName -and $script:AgentName -ne 'default') {
            $psi.ArgumentList.Add('--agent')
            $psi.ArgumentList.Add($script:AgentName)
        }
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        # Do NOT redirect stderr: mcpserver-repl logs verbose 'info:' lines
        # to stderr, and an unread redirected stream blocks the child once
        # its pipe buffer fills (Windows ~4 KB), causing WaitForExit to hang.
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        Set-ReplProcessWorkspace -StartInfo $psi
        # mcpserver-repl writes UTF-8 (with BOM). Without explicit encoding,
        # PowerShell decodes as cp437 and BOM bytes (EF BB BF) become box-
        # drawing glyphs that break the '^type: error' regex anchor.
        $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8

        $proc = [System.Diagnostics.Process]::Start($psi)
        $envFile = Join-Path (Get-ReplInvokeCacheDir) "envelope-$requestId.tmp"
        [System.IO.File]::WriteAllText($envFile, $envelope, [System.Text.Encoding]::UTF8)
        try {
            $fs = [System.IO.File]::OpenRead($envFile)
            $fs.CopyTo($proc.StandardInput.BaseStream)
            $fs.Close()
        } finally {
            Remove-Item $envFile -ErrorAction SilentlyContinue
        }
        $proc.StandardInput.Close()

        # Drain stdout BEFORE waiting for exit. With a redirected pipe, the
        # child blocks on stdout writes once the pipe buffer (~4 KB on
        # Windows) fills, and WaitForExit then deadlocks. ReadToEndAsync
        # streams the buffer concurrently and resolves when the child closes
        # stdout (which happens at process exit).
        $readTask = $proc.StandardOutput.ReadToEndAsync()
        if (-not $readTask.Wait($timeout * 1000)) {
            $proc.Kill()
            Write-Error "mcpserver-repl timed out after ${timeout}s"
            return (New-McpPluginReplResult -Success $false -Output '' -Error "mcpserver-repl timed out after ${timeout}s")
        }
        $output = $readTask.Result
        $proc.WaitForExit()

        # mcpserver-repl writes a UTF-8 BOM before the YAML doc and may
        # interleave logger 'info:' lines on stdout — strip BOM and ignore
        # leading log noise so the regex anchor matches the real header.
        $output = $output -replace "[\uFEFF]", ''
        $isError = $output -match '(?m)^type:\s*error\b'
        if ($proc.ExitCode -ne 0 -or $isError) {
            return (New-McpPluginReplResult -Success $false -Output $output -ExitCode $proc.ExitCode)
        }
        return (New-McpPluginReplResult -Success $true -Output $output -ExitCode $proc.ExitCode)
    }
    catch {
        Write-Error "mcpserver-repl invocation failed for method ${Method}: $_"
        return (New-McpPluginReplResult -Success $false -Output '' -Error $_.ToString())
    }
}

function Get-ReplSessionStateValue {
    # PowerShell twin of _repl_session_state_value: first scalar for a
    # top-level key in session-state.yaml.
    param([Parameter(Mandatory)][string]$Key)
    $f = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    if (-not (Test-Path $f)) { return '' }
    $line = Select-String -Path $f -Pattern "^${Key}:" | Select-Object -First 1
    if (-not $line) { return '' }
    return ($line.Line -replace "^${Key}:\s*", '').Trim()
}

function Get-ReplCurrentTurnValue {
    param([Parameter(Mandatory)][string]$Key)
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return '' }
    $line = Select-String -Path $turnFile -Pattern "^${Key}:" | Select-Object -First 1
    if (-not $line) { return '' }
    return ($line.Line -replace "^${Key}:\s*", '').Trim()
}

function Get-ReplCurrentTurnFile {
    return (Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml')
}

function Deny-ReplMissingCurrentTurn {
    param([Parameter(Mandatory)][string]$Method)

    $cacheDir = Get-ReplInvokeCacheDir
    [Console]::Error.WriteLine("$Method requires current-turn.yaml in '$cacheDir'. Run the active agent prompt hook in the workspace, or set MCP_CACHE_DIR_OVERRIDE to the cache that contains the active turn.")
    return $false
}

function Get-ReplCurrentTurnQueryText {
    # Extract the queryText literal block from current-turn.yaml (twin of
    # _repl_yaml_block_get for the one block the upsert path needs).
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return '' }
    try {
        $turnState = Read-McpYamlObject -Path $turnFile
        if ($turnState -and $turnState.Contains('queryText')) {
            return [string]$turnState['queryText']
        }
    } catch {
        # Fall through to the legacy literal-block parser.
    }
    $text = Get-Content -Path $turnFile -Raw
    if ($text -match '(?ms)^queryText:\s*\|\s*\r?\n(.*?)(?=^\S|\z)') {
        $block = $Matches[1]
        $lines = $block -split "`n" | ForEach-Object { $_ -replace '^\s{0,4}', '' }
        return (($lines -join "`n").TrimEnd())
    }
    return ''
}

function Get-ReplFailsafeDir {
    if ($env:MCPSERVER_FAILSAFE_DIR) { return $env:MCPSERVER_FAILSAFE_DIR }
    if ($env:MCP_FAILSAFE_DIR) { return $env:MCP_FAILSAFE_DIR }
    return (Join-Path (Get-ReplInvokeCacheDir) 'failsafe')
}

function Write-ReplFailsafe {
    # Twin of _repl_failsafe_write: capture the payload before attempting the
    # remote call so a crash cannot lose the turn. Returns the failsafe path.
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$ParamsYaml,
        [Parameter(Mandatory)][string]$Label
    )
    try {
        $dir = Get-ReplFailsafeDir
        [void][System.IO.Directory]::CreateDirectory($dir)
        $stamp = Get-Date -AsUTC -Format 'yyyyMMddTHHmmssZ'
        $file = Join-Path $dir ("{0}-{1}-{2:x4}.yaml" -f $stamp, $Label, (Get-Random -Maximum 0xFFFF))
        $record = New-McpPluginFailsafeRecord -Method $Method -Label $Label -Timestamp $stamp -ParamsYaml $ParamsYaml -Path $file
        $doc = $record.ToYaml()
        Set-Content -Path $file -Value $doc -NoNewline
        return $file
    }
    catch {
        return ''
    }
}

function Clear-ReplFailsafe {
    param([string]$Path)
    if ($Path -and (Test-Path $Path)) {
        Remove-Item -Path $Path -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-ReplTurnUpsertParams {
    # Build as object, will be serialized to JSON in the caller (no text YAML).
    # This eliminates indentation and block-scalar errors.
    param(
        [Parameter(Mandatory)][string]$SourceType,
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [string]$ActionsYaml = '',
        [object[]]$ProcessingDialog = @()
    )

    $queryText = Get-ReplCurrentTurnQueryText
    if (-not $queryText) { $queryText = $Title }
    $timestamp = Get-ReplCurrentTurnValue -Key 'openedAt'
    if (-not $timestamp) { $timestamp = (Get-Date -AsUTC -Format "yyyy-MM-ddTHH:mm:ssZ") }
    $model = Get-ReplSessionStateValue -Key 'model'
    if (-not $model) {
        $model = if ($env:MCP_SESSION_MODEL) { $env:MCP_SESSION_MODEL }
                 elseif ($env:PLUGIN_MODEL_DEFAULT) { $env:PLUGIN_MODEL_DEFAULT }
                 else { 'codex' }
    }

    $actions = @()
    if ($ActionsYaml) {
        $actionParams = Convert-ReplParamsYamlToObject -ParamsYaml ("actions:`n$ActionsYaml")
        if ($actionParams -and $actionParams.actions) {
            foreach ($action in @($actionParams.actions)) {
                $actions += (New-McpPluginActionRecord -Values $action).ToMap()
            }
        }
    }

    $filePaths = @($actions | ForEach-Object {
        if ($_ -is [hashtable] -and $_.ContainsKey('filePath') -and $_.filePath) {
            $_.filePath
        } elseif ($_.PSObject.Properties.Name -contains 'filePath' -and $_.filePath) {
            $_.filePath
        }
    } | Where-Object { $_ })

    $request = New-McpPluginTurnUpsertRequest `
        -Agent $SourceType `
        -SessionId $SessionId `
        -RequestId $RequestId `
        -Timestamp $timestamp `
        -QueryText $queryText `
        -Title $Title `
        -Status $Status `
        -ResponseText $ResponseText `
        -Model $model `
        -FilesModified $filePaths `
        -Actions $actions `
        -ProcessingDialog $ProcessingDialog

    return $request.ToParamsObject()
}

function Invoke-ReplSubmitSessionFallback {
    # Pre-upsert behavior: build the full sessionLog envelope and submit via
    # client.SessionLog.SubmitAsync. Used only when UpsertTurnAsync is
    # missing on the server (method_not_found).
    param(
        [Parameter(Mandatory)][pscustomobject]$Meta,
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [string]$ActionsYaml = '',
        [object[]]$ProcessingDialog = @()
    )

    $turn = [ordered]@{
        requestId = $RequestId
        queryTitle = $Title
        status = $Status
        response = $ResponseText
    }
    if ($ActionsYaml) {
        $actionParams = Convert-ReplParamsYamlToObject -ParamsYaml ("actions:`n$ActionsYaml")
        if ($actionParams -and $actionParams.actions) {
            $turn.actions = @($actionParams.actions)
        }
    }
    if ($ProcessingDialog.Count -gt 0) {
        $turn.processingDialog = @($ProcessingDialog)
    }

    $paramsObject = [ordered]@{
        sessionLog = [ordered]@{
            sourceType = $Meta.SourceType
            sessionId = $Meta.SessionId
            title = $Title
            status = 'in_progress'
            turns = @($turn)
        }
    }
    $params = ConvertTo-Yaml -Data $paramsObject -Options WithIndentedSequences

    $r = Invoke-ReplRaw -Method 'client.SessionLog.SubmitAsync' -ParamsYaml $params
    return $r.Success
}

function Invoke-ReplPersistTurn {
    # Persist the turn via client.SessionLog.UpsertTurnAsync with a failsafe
    # capture first, falling back to full-session SubmitAsync only when the
    # upsert method is missing (sh twin: _repl_persist_turn, commit 97aab2d).
    param(
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [string]$ActionsYaml = '',
        [object[]]$ProcessingDialog = @()
    )
    $meta = Get-ReplSessionMeta
    if (-not $meta) { throw 'Session log persistence failed because no session metadata is cached.' }

    $turnObj = Invoke-ReplTurnUpsertParams -SourceType $meta.SourceType `
        -SessionId $meta.SessionId -RequestId $RequestId -Title $Title `
        -Status $Status -ResponseText $ResponseText -ActionsYaml $ActionsYaml `
        -ProcessingDialog $ProcessingDialog

    $envelope = New-McpPluginReplRequest `
        -RequestId "req-$(Get-Date -AsUTC -Format 'yyyyMMddTHHmmssZ')-$(Get-Random -Maximum 0xffff)" `
        -Method 'client.SessionLog.UpsertTurnAsync' `
        -Params $turnObj
    $jsonEnvelope = ConvertTo-McpPluginJson -InputObject $envelope -Depth 10 -Compress

    $failsafe = Write-ReplFailsafe -Method 'client.SessionLog.UpsertTurnAsync' `
        -ParamsYaml $jsonEnvelope -Label 'session_upsertTurn'

    # Send as JSON envelope (reliable serialization, no manual YAML text)
    $tmp = Join-Path (Get-ReplInvokeCacheDir) "envelope-$RequestId.json"
    [System.IO.File]::WriteAllText($tmp, $jsonEnvelope, [System.Text.Encoding]::UTF8)
    try {
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = 'mcpserver-repl'
        $psi.ArgumentList.Add('--agent-stdio')
        if ($script:AgentName -and $script:AgentName -ne 'default') {
            $psi.ArgumentList.Add('--agent'); $psi.ArgumentList.Add($script:AgentName)
        }
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        Set-ReplProcessWorkspace -StartInfo $psi
        $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
        $proc = [System.Diagnostics.Process]::Start($psi)
        $fs = [System.IO.File]::OpenRead($tmp)
        $fs.CopyTo($proc.StandardInput.BaseStream)
        $fs.Close()
        $proc.StandardInput.Close()
        $outTask = $proc.StandardOutput.ReadToEndAsync()
        $outTask.Wait(30000) | Out-Null
        $output = $outTask.Result
        $proc.WaitForExit()
        $output = $output -replace "[\uFEFF]", ''
        $isErr = $output -match '(?m)^type:\s*error\b'
        if ($proc.ExitCode -eq 0 -and -not $isErr) {
            Clear-ReplFailsafe -Path $failsafe
            return $true
        }
        if ($output -match 'method_not_found' -or $output -match 'Session not found') {
            Clear-ReplFailsafe -Path $failsafe
            $fallbackOk = Invoke-ReplSubmitSessionFallback -Meta $meta -RequestId $RequestId `
                -Title $Title -Status $Status -ResponseText $ResponseText `
                -ActionsYaml $ActionsYaml -ProcessingDialog $ProcessingDialog
            if (-not $fallbackOk) {
                throw "Session log fallback submit failed for request '$RequestId'."
            }
            return $true
        }
        $message = "Session log persistence failed for request '$RequestId'. ExitCode=$($proc.ExitCode). Output=$output"
        throw $message
    } finally {
        Remove-Item $tmp -ErrorAction SilentlyContinue
    }
}

function Update-ReplTurnCacheStatus {
    param([Parameter(Mandatory)][string]$NewStatus)
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return $false }
    $lines = Get-Content -Path $turnFile
    $updated = $lines | ForEach-Object {
        if ($_ -match '^status:') { "status: $NewStatus" } else { $_ }
    }
    Set-Content -Path $turnFile -Value $updated -NoNewline:$false
    return $true
}

function Update-ReplTurnCacheEdits {
    param([Parameter(Mandatory)][int]$Increment)
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return $false }
    $lines = Get-Content -Path $turnFile
    $current = 0
    foreach ($l in $lines) {
        if ($l -match '^codeEdits:\s*(\d+)') {
            $current = [int]$Matches[1]
            break
        }
    }
    $new = $current + $Increment
    $updated = $lines | ForEach-Object {
        if ($_ -match '^codeEdits:') { "codeEdits: $new" } else { $_ }
    }
    Set-Content -Path $turnFile -Value $updated -NoNewline:$false
    return $true
}

function Get-ReplTurnCacheField {
    param([Parameter(Mandatory)][string]$Field)
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return '' }
    $line = Select-String -Path $turnFile -Pattern "^${Field}:" |
        Select-Object -First 1
    if (-not $line) { return '' }
    return ($line.Line -replace "^${Field}:\s*", '').Trim()
}

function Get-ReplNormalizedActionsBlock {
    # Returns bare list content under the 'actions:' key (common indent stripped).
    # Twin of _repl_normalized_actions_block + _repl_list_block_get.
    # Safe for map-style turn docs (preserves nesting) and top-level actions: lists.
    param([string]$ParamsYaml)
    if (-not $ParamsYaml) { return '' }
    $text = $ParamsYaml -replace "`r`n", "`n" -replace "`r", ""
    $lines = $text -split "`n"
    $capture = $false
    $keyIndent = -1
    $stripIndent = -1
    $result = @()
    foreach ($line in $lines) {
        if (-not $capture) {
            if ($line -match '^\s*actions:\s*$') {
                $capture = $true
                $m = [regex]::Match($line, '^(\s*)')
                $keyIndent = $m.Groups[1].Value.Length
                continue
            }
            continue
        }
        if ($line -match '^\s*$') {
            $result += $line
            continue
        }
        $m = [regex]::Match($line, '^(\s*)')
        $lineIndent = $m.Groups[1].Value.Length
        if ($lineIndent -le $keyIndent -and $line -notmatch '^\s*-') {
            break
        }
        if ($stripIndent -lt 0) { $stripIndent = $lineIndent }
        if ($stripIndent -gt 0 -and $line.Length -ge $stripIndent) {
            $result += $line.Substring($stripIndent)
        } else {
            $result += $line
        }
    }
    return ($result -join "`n").TrimEnd()
}

function Update-ReplTurnAudit {
    param([Parameter(Mandatory)][string]$Field, [int]$Increment = 0)
    if ($Increment -le 0) { return $false }
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return $false }
    $lines = Get-Content -Path $turnFile
    $current = 0
    foreach ($l in $lines) {
        if ($l -match "^${Field}:\s*(\d+)") {
            $current = [int]$Matches[1]
            break
        }
    }
    $new = $current + $Increment
    $found = $false
    $updated = $lines | ForEach-Object {
        if ($_ -match "^${Field}:") { $found = $true; "${Field}: $new" } else { $_ }
    }
    if (-not $found) {
        # append if field missing (defensive; init should have written audits)
        $updated += "${Field}: $new"
    }
    Set-Content -Path $turnFile -Value ($updated -join "`n") -NoNewline:$false
    return $true
}

function Invoke-WorkflowAppendActions {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.appendActions')
    }

    $added = 0
    $actionsBlock = ''
    if ($ParamsYaml) {
        $p = $ParamsYaml -replace "`r`n", "`n" -replace "`r", ""
        $actionsBlock = Get-ReplNormalizedActionsBlock -ParamsYaml $p
        # Count only real filePath: fields (with value) for codeEdits. Substring matches in
        # descriptions must be ignored. Non-file actions (design_decision etc.) must persist.
        $added = ([regex]::Matches($p, '(?m)^\s*filePath:\s*\S')).Count
    }

    if ($ParamsYaml -and $ParamsYaml.Trim()) {
        if ($added -gt 0) {
            Update-ReplTurnCacheEdits -Increment $added | Out-Null
        }
        $actionC = ([regex]::Matches($ParamsYaml, '(?m)^\s*type:')).Count
        $decC = ([regex]::Matches($ParamsYaml, '(?m)^\s*type:\s*design_decision\b')).Count
        $comC = ([regex]::Matches($ParamsYaml, '(?m)^\s*type:\s*commit\b')).Count
        Update-ReplTurnAudit -Field 'auditActions' -Increment $actionC | Out-Null
        Update-ReplTurnAudit -Field 'auditFiles' -Increment $added | Out-Null
        Update-ReplTurnAudit -Field 'auditDecisions' -Increment $decC | Out-Null
        Update-ReplTurnAudit -Field 'auditCommits' -Increment $comC | Out-Null

        $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
        $title = Get-ReplTurnCacheField -Field 'queryTitle'
        $null = Invoke-ReplPersistTurn -RequestId $reqId -Title $title `
            -Status 'in_progress' -ResponseText 'Actions appended.' `
            -ActionsYaml $actionsBlock
    }
    return $true
}

function Get-ReplDialogItemsFromParams {
    param([string]$ParamsYaml)

    if (-not $ParamsYaml) { return @() }
    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    if (-not $params) { return @() }

    $propertyNames = @($params.PSObject.Properties.Name)
    $rawItems = @()
    if ($propertyNames -contains 'dialogItems') {
        $rawItems = @($params.dialogItems)
    } elseif ($propertyNames -contains 'dialog') {
        $rawItems = @($params.dialog)
    }

    $items = @()
    foreach ($rawItem in $rawItems) {
        if (-not $rawItem) { continue }
        $item = [ordered]@{}
        if ($rawItem -is [System.Collections.IDictionary]) {
            foreach ($key in $rawItem.Keys) {
                $item[[string]$key] = $rawItem[$key]
            }
        } else {
            foreach ($property in $rawItem.PSObject.Properties) {
                $item[$property.Name] = $property.Value
            }
        }
        if ($item.Count -gt 0) {
            $items += $item
        }
    }

    return @($items)
}

function Invoke-WorkflowAppendDialog {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.appendDialog')
    }

    $dialogItems = @(Get-ReplDialogItemsFromParams -ParamsYaml $ParamsYaml)
    if ($dialogItems.Count -gt 0) {
        Update-ReplTurnAudit -Field 'auditDialog' -Increment $dialogItems.Count | Out-Null
        $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
        $title = Get-ReplTurnCacheField -Field 'queryTitle'
        $null = Invoke-ReplPersistTurn -RequestId $reqId -Title $title `
            -Status 'in_progress' -ResponseText 'Dialog appended.' `
            -ProcessingDialog $dialogItems
    }

    return $true
}

function Invoke-WorkflowCompleteTurn {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.completeTurn')
    }

    $responseText = '(no response provided)'
    if ($ParamsYaml) {
        $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
        if ($params -and ($params.PSObject.Properties.Name -contains 'response')) {
            $responseText = [string]$params.response
        }
    }

    $actionsBlock = ''
    if ($ParamsYaml -and ($ParamsYaml -match '(?m)^\s*actions:' -or $ParamsYaml -match '(?m)^\s*actions:\s*\S')) {
        $actionsBlock = Get-ReplNormalizedActionsBlock -ParamsYaml ($ParamsYaml -replace "`r`n", "`n" -replace "`r", "")
    }

    Update-ReplTurnCacheStatus -NewStatus 'completed' | Out-Null

    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    $title = Get-ReplTurnCacheField -Field 'queryTitle'
    $null = Invoke-ReplPersistTurn -RequestId $reqId -Title $title `
        -Status 'completed' -ResponseText $responseText -ActionsYaml $actionsBlock
    return $true
}

function Invoke-ReplMethod {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )

    # Local plugin-shim verbs: record the boolean outcome on the script-scoped
    # success flag (so the script-entry exit code is truthful) and return without
    # emitting the boolean to stdout. Emitting it leaked "True" lines, and leaving
    # the flag unset made the script-entry exit 1 even on a successful persist.
    switch -Wildcard ($Method) {
        'workflow.sessionlog.beginTurn'       { $script:LastInvokeReplMethodSuccess = $true; return }
        'workflow.sessionlog.openSession'     { $script:LastInvokeReplMethodSuccess = $true; return }
        'workflow.sessionlog.appendActions'   { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowAppendActions -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.appendDialog'    { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowAppendDialog -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.completeTurn'    { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowCompleteTurn -ParamsYaml $ParamsYaml); return }
    }

    $r = Invoke-ReplRaw -Method $Method -ParamsYaml $ParamsYaml
    if ($r.Output) {
        $script:LastInvokeReplMethodSuccess = [bool]$r.Success
        $r.Output
        return
    }

    $script:LastInvokeReplMethodSuccess = [bool]$r.Success
    return [bool]$r.Success
}

# Script-entry: only when invoked directly with -Method (not when dot-sourced).
if ($Method -and $MyInvocation.InvocationName -ne '.') {
    $script:LastInvokeReplMethodSuccess = $false
    Invoke-ReplMethod -Method $Method -ParamsYaml $ParamsYaml
    if (-not $script:LastInvokeReplMethodSuccess) { exit 1 }
    exit 0
}
