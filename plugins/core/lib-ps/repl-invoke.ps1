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
Import-Module $shimModule -Force -ErrorAction Stop
$shimCommand = Get-Command New-McpPluginTurnUpsertRequest -ErrorAction Stop
if (-not $shimCommand.Parameters.ContainsKey('ProcessingDialog')) {
    Remove-Module McpPluginShim -Force -ErrorAction SilentlyContinue
    Import-Module $shimModule -Force -ErrorAction Stop
    $shimCommand = Get-Command New-McpPluginTurnUpsertRequest -ErrorAction Stop
    if (-not $shimCommand.Parameters.ContainsKey('ProcessingDialog')) {
        throw 'McpPluginShim.psm1 is stale or invalid because New-McpPluginTurnUpsertRequest lacks ProcessingDialog.'
    }
}
. (Join-Path $PSScriptRoot 'yaml-object-mutation.ps1')
Import-McpYamlSerializer
. (Join-Path $PSScriptRoot 'marker-resolver.ps1')

$script:ReplInvokePluginRoot = if ($env:MCP_PLUGIN_ROOT) {
    $env:MCP_PLUGIN_ROOT
} else {
    Split-Path -Parent $PSScriptRoot
}

# Agent for per-agent REPL cache and isolation. Must be passed to every mcpserver-repl call.
# Keep this precedence aligned with the shell resolve-cache-dir counterpart and MarkerFileClientOptionsResolver.ResolveAgentKey.
$script:AgentName = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME }
                   elseif ($env:PLUGIN_AGENT_NAME) { $env:PLUGIN_AGENT_NAME }
                   elseif ($env:PLUGIN_AGENT_DEFAULT) { $env:PLUGIN_AGENT_DEFAULT }
                   elseif ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST }
                   else { 'default' }

function Get-ReplCanonicalAgentName {
    # TR-MCP-REPL-011: map the resolved agent (which can fall back to lowercase 'default' or a
    # lowercase host key like 'claude-code') to a PascalCase source type so composed session ids
    # satisfy the server regex ^[A-Z][A-Za-z0-9]*-... (BUG-TRIAGE-085).
    param([string]$AgentName)

    if ([string]::IsNullOrWhiteSpace($AgentName)) { return 'ClaudeCode' }

    switch (($AgentName.Trim().ToLowerInvariant() -replace '[^a-z0-9]', '')) {
        'claude'       { return 'ClaudeCode' }
        'claudecode'   { return 'ClaudeCode' }
        'claudecowork' { return 'ClaudeCowork' }
        'codex'        { return 'Codex' }
        'copilot'      { return 'Copilot' }
        'grok'         { return 'GrokCode' }
        'grokcode'     { return 'GrokCode' }
        'cline'        { return 'Cline' }
        'clinev2'      { return 'Cline' }
        'opencode'     { return 'OpenCode' }
    }

    $parts = [regex]::Split($AgentName.Trim(), '[^A-Za-z0-9]+') | Where-Object { $_ }
    if (-not $parts) { return 'ClaudeCode' }
    return (-join ($parts | ForEach-Object { $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1) }))
}

if (-not (Get-Command Resolve-McpCacheDir -ErrorAction SilentlyContinue) -or
    -not (Get-Command Get-McpFailsafeDir -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'resolve-cache-dir.ps1')
}

# Resolved lazily so per-call context (workspace / env) governs path.
function script:Get-ReplInvokeCacheDir { Resolve-McpCacheDir }

function New-ReplPluginSessionId {
    if ($env:MCP_SESSION_ID) { return $env:MCP_SESSION_ID }

    $timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $suffix = if ($env:MCP_SESSION_SUFFIX) { $env:MCP_SESSION_SUFFIX } else { 'plugin-session' }
    $suffix = ($suffix.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    if (-not $suffix) { $suffix = 'plugin-session' }

    return '{0}-{1}-{2}' -f (Get-ReplCanonicalAgentName $script:AgentName), $timestamp, $suffix
}

function Invoke-WorkflowOpenSession {
    # TR-MCP-REPL-011: persist an explicit valid sessionId into session-state.yaml instead of the
    # historical no-op, so an explicit openSession can recover a bad/rotated local session id
    # (BUG-TRIAGE-085; previously documented as a gap in GAPS.md).
    param([string]$ParamsYaml)

    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    $sessionId = ''
    if ($params -is [System.Collections.IDictionary] -and $params.Contains('sessionId')) {
        $sessionId = [string]$params['sessionId']
    } elseif ($params -and $params.PSObject.Properties['sessionId']) {
        $sessionId = [string]$params.PSObject.Properties['sessionId'].Value
    }
    if ([string]::IsNullOrWhiteSpace($sessionId)) { return $false }

    $sessionId = ConvertTo-ReplCanonicalSessionId -SessionId $sessionId.Trim()
    $sessionFile = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    $state = Read-McpYamlObject -Path $sessionFile -Create
    if ($state -isnot [System.Collections.IDictionary]) { $state = [ordered]@{} }
    $state['status'] = 'verified'
    $state['sessionId'] = $sessionId
    if (-not $state.Contains('agent') -or [string]::IsNullOrWhiteSpace([string]$state['agent'])) {
        $state['agent'] = Get-ReplCanonicalAgentName $script:AgentName
    }
    $state['lastUpdated'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Write-McpYamlObject -Path $sessionFile -Document $state
    return $true
}


function Resolve-ReplWorkspaceDirectory {
    # A marker-bearing current directory outranks the workspace env vars: ambient
    # MCP_WORKSPACE_PATH values leak across workspaces through host processes and
    # persistent consoles, and an inherited value must not re-bind marker trust,
    # API keys, and session logs to another workspace while the cache directory
    # stays local (triage-report-7c84e6437f7b42d0a67fbe32679a686a). This matches
    # the plugin hook's Get-PluginStartPath precedence.
    $providerLocation = $null
    try {
        $location = Get-Location
        if ($location.Provider.Name -eq 'FileSystem') {
            $providerLocation = $location.ProviderPath
        }
    } catch {
        # Fall through to the env and .NET current directory fallbacks.
    }

    # Only the PowerShell provider location is trusted for the marker check: the
    # process-wide [Environment]::CurrentDirectory can be stale in shared hosts.
    if (-not [string]::IsNullOrWhiteSpace($providerLocation) -and
        (Test-Path -LiteralPath $providerLocation -PathType Container)) {
        try {
            if ((Get-Command Find-MarkerFile -ErrorAction SilentlyContinue) -and
                (Find-MarkerFile -StartDir $providerLocation)) {
                return (Resolve-Path -LiteralPath $providerLocation).ProviderPath
            }
        } catch {
            # No marker above the current directory; consult the env fallbacks below.
        }
    }

    $candidates = @(
        $env:MCP_WORKSPACE_PATH,
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:MCP_WORKSPACE_START_DIR,
        $env:CLAUDE_PROJECT_DIR,
        $providerLocation,
        [Environment]::CurrentDirectory
    )

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
        $cachedSessionId = if ($state.Contains('sessionId')) { [string]$state['sessionId'] } else { '' }

        if ($status -eq 'verified' -and
            $cachedPath -eq $snapshot.markerFilePath -and
            $cachedWriteUtc -eq $snapshot.markerLastWriteUtc -and
            -not [string]::IsNullOrWhiteSpace($cachedSessionId)) {
            return $true
        }

        if (-not (Invoke-FullBootstrap -StartDir $workspace)) {
            throw 'marker bootstrap failed'
        }
        $snapshot = Get-MarkerFileSnapshot -StartDir $workspace

        $state['status'] = 'verified'
        if (-not $state.Contains('agent')) {
            $state['agent'] = $script:AgentName
        }
        if (-not $state.Contains('sessionId') -or [string]::IsNullOrWhiteSpace([string]$state['sessionId'])) {
            $state['sessionId'] = New-ReplPluginSessionId
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

function Get-ReplMethodTimeoutSeconds {
    # TR-MCP-REPL-012: per-method timeout. Long-running requirement/agent methods (which invoke
    # external CLIs and can take minutes) get an extended, env-configurable budget; everything else
    # keeps the short default so sessionlog calls fail fast (BUG-TRIAGE-072).
    param([Parameter(Mandatory)][string]$Method)

    $default = if ($env:REPL_TIMEOUT) { [int]$env:REPL_TIMEOUT } else { 30 }
    $long = if ($env:REPL_LONG_TIMEOUT) { [int]$env:REPL_LONG_TIMEOUT } else { 300 }

    switch -Wildcard ($Method) {
        'workflow.todo.analyzeRequirements'       { return $long }
        'workflow.requirements.generateDocument'  { return $long }
        'workflow.requirements.ingestDocument'    { return $long }
        'workflow.requirements.analyze*'          { return $long }
        'client.Requirements.Analyze*'            { return $long }
        default                                   { return $default }
    }
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
    $timeout = Get-ReplMethodTimeoutSeconds -Method $Method

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
        $psi.ArgumentList.Add('--agent')
        $psi.ArgumentList.Add($script:AgentName)
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
        # TR-MCP-REPL-016: this is the first proof in the process that the backend
        # answers, so it is the safe moment to replay anything the failsafe queue
        # captured while it was unreachable. Guarded to run at most once.
        Invoke-ReplFailsafeDrainOnFirstSuccess
        return (New-McpPluginReplResult -Success $true -Output $output -ExitCode $proc.ExitCode)
    }
    catch {
        Write-Error "mcpserver-repl invocation failed for method ${Method}: $_"
        return (New-McpPluginReplResult -Success $false -Output '' -Error $_.ToString())
    }
}

function Get-ReplSessionStateValue {
    param([Parameter(Mandatory)][string]$Key)
    $f = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    if (-not (Test-Path $f)) { return '' }
    $state = Read-McpYamlObject -Path $f
    if (-not $state -or -not $state.Contains($Key) -or $null -eq $state[$Key]) { return '' }
    return [string]$state[$Key]
}

function Set-ReplSessionStateValue {
    # TR-MCP-REPL-014/015: object-first read-modify-write of a single session-state
    # key (e.g. the stable session 'title'). Never edits YAML as text.
    param(
        [Parameter(Mandatory)][string]$Key,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Value)
    $f = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    $state = Read-McpYamlObject -Path $f -Create
    $state[$Key] = $Value
    Write-McpYamlObject -Path $f -Document $state
    return $true
}

function Get-ReplCurrentTurnValue {
    param([Parameter(Mandatory)][string]$Key)
    $state = Read-ReplCurrentTurnState
    if (-not $state -or -not $state.Contains($Key) -or $null -eq $state[$Key]) { return '' }
    return [string]$state[$Key]
}

function Get-ReplCurrentTurnFile {
    return (Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml')
}

function Get-ReplRecoveryGuidance {
    return 'Run the active agent prompt hook; it will health-check the marker, recreate session state when AGENTS-README-FIRST.yaml changes, submit triage if create fails after healthy bootstrap, and continue through failsafe session logging while degraded.'
}

function Deny-ReplMissingCurrentTurn {
    param([Parameter(Mandatory)][string]$Method)

    $cacheDir = Get-ReplInvokeCacheDir
    [Console]::Error.WriteLine("$Method requires current-turn.yaml in '$cacheDir'. $(Get-ReplRecoveryGuidance) Set MCP_CACHE_DIR_OVERRIDE only when intentionally targeting a different active-turn cache.")
    return $false
}

function Read-ReplCurrentTurnState {
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return $null }
    try {
        return Read-McpYamlObject -Path $turnFile
    } catch {
        return $null
    }
}

function Write-ReplCurrentTurnState {
    param([Parameter(Mandatory)]$State)
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    Write-McpYamlObject -Path $turnFile -Document $State
}

function Get-ReplCurrentTurnQueryText {
    $turnState = Read-ReplCurrentTurnState
    if ($turnState -and $turnState.Contains('queryText')) {
        return [string]$turnState['queryText']
    }
    return ''
}

function Get-ReplFailsafeDir {
    # TR-MCP-REPL-016: the queue location is resolved by the shared helper so the
    # writer, the drain, and mcp-status.ps1 can never disagree about which
    # directory holds the pending records.
    return (Get-McpFailsafeDir)
}

function Get-ReplFailsafeQuarantineDir {
    # TR-MCP-REPL-017: unreplayable records are parked here instead of being
    # deleted, so a bad record is recoverable by hand and never blocks the queue.
    return (Get-McpFailsafeQuarantineDir)
}

# TR-MCP-REPL-016: records written by the submit currently in flight. The drain
# must not replay (and must not delete) a record whose original submit has not
# resolved yet, otherwise a bootstrap drain would double-submit the live turn.
$script:ReplFailsafeInFlight = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
# Re-entrancy guard: the drain calls Invoke-ReplRaw, which is the same place the
# drain is triggered from.
$script:ReplFailsafeDraining = $false
# One drain attempt per process. A second pass in the same process would only
# re-walk records the first pass already decided about.
$script:ReplFailsafeDrainCompleted = $false

function Write-ReplFailsafe {
    # Capture the serialized request before the remote call so a crash cannot lose
    # the turn. The YAML document is written through the shared object serializer.
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
        $paramsObject = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
        $record = [ordered]@{
            method = $Method
            label = $Label
            timestamp = $stamp
            params = $paramsObject
        }
        Write-McpYamlObject -Path $file -Document $record
        [void]$script:ReplFailsafeInFlight.Add($file)
        return $file
    }
    catch {
        return ''
    }
}
function Clear-ReplFailsafe {
    param([string]$Path)
    if ($Path) {
        [void]$script:ReplFailsafeInFlight.Remove($Path)
        if (Test-Path $Path) {
            Remove-Item -Path $Path -Force -ErrorAction SilentlyContinue
        }
    }
}

function Test-ReplFailsafeBackendUnreachable {
    # TR-MCP-REPL-016: distinguish "the backend never answered" from "the backend
    # answered and rejected this record". Only the first aborts the drain; the
    # second must let the walk continue so one bad record cannot dam the queue.
    param([AllowEmptyString()][string]$Detail)

    if ([string]::IsNullOrWhiteSpace($Detail)) { return $true }

    $markers = @(
        'MCP_UNTRUSTED',
        'not found on PATH',
        'timed out',
        'invocation failed',
        'No connection could be made',
        'actively refused',
        'connection refused',
        'Connection refused',
        'Unable to resolve the active workspace cache'
    )
    foreach ($marker in $markers) {
        if ($Detail -like "*$marker*") { return $true }
    }
    return $false
}

function Move-ReplFailsafeToQuarantine {
    # TR-MCP-REPL-017: park a record that cannot be replayed, with the reason next
    # to it. Never delete: a malformed record still holds the only copy of a turn.
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Reason
    )

    try {
        $quarantineDir = Get-ReplFailsafeQuarantineDir
        [void][System.IO.Directory]::CreateDirectory($quarantineDir)
        $target = Join-Path $quarantineDir ([System.IO.Path]::GetFileName($Path))
        if (Test-Path -LiteralPath $target) {
            $target = Join-Path $quarantineDir ("{0}-{1:x4}{2}" -f
                [System.IO.Path]::GetFileNameWithoutExtension($Path),
                (Get-Random -Maximum 0xFFFF),
                [System.IO.Path]::GetExtension($Path))
        }
        [System.IO.File]::Move($Path, $target)
        $reasonText = "quarantinedAtUtc: $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))" +
            [Environment]::NewLine + "originalPath: $Path" +
            [Environment]::NewLine + "reason: $Reason" + [Environment]::NewLine
        [System.IO.File]::WriteAllText(($target + '.reason.txt'), $reasonText)
        return $target
    }
    catch {
        [Console]::Error.WriteLine("Failsafe quarantine failed for '$Path': $($_.Exception.Message)")
        return ''
    }
}

function Invoke-ReplFailsafeDrain {
    <#
    .SYNOPSIS
        TR-MCP-REPL-016/017: replay queued failsafe records against a reachable backend.
    .DESCRIPTION
        Walks the failsafe queue oldest-first (the file name is a UTC stamp) and
        re-issues each captured request. A record is deleted only after its submit
        succeeds, so a failure never loses data. client.SessionLog.SubmitAsync is an
        upsert keyed by sessionId plus requestId, which makes a replay idempotent.

        Safety rules:
          - A record rejected by the backend stays on disk, its attempt counter is
            incremented, and the walk continues with the newer records behind it.
          - A record that cannot be parsed, or that has burned MaxAttempts, is moved
            to the quarantine directory with a reason file instead of being retried
            forever or deleted.
          - A transport failure aborts the whole pass without consuming attempts,
            because the backend, not the record, is the problem.
          - The record for the submit currently in flight is skipped.
    .PARAMETER MaxRecords
        Maximum records to consider in one pass. 0 means the whole queue.
    .PARAMETER MaxAttempts
        Attempt budget before a repeatedly rejected record is quarantined.
    #>
    [CmdletBinding()]
    param(
        [int]$MaxRecords = 0,
        [int]$MaxAttempts = 5
    )

    $summary = [ordered]@{
        failsafeDir = ''
        scanned = 0
        replayed = 0
        failed = 0
        quarantined = 0
        skipped = 0
        aborted = $false
        abortReason = ''
    }

    if ($script:ReplFailsafeDraining) {
        $summary.aborted = $true
        $summary.abortReason = 'a drain is already running in this process'
        return $summary
    }

    try {
        $dir = Get-ReplFailsafeDir
    } catch {
        $summary.aborted = $true
        $summary.abortReason = "failsafe directory could not be resolved: $($_.Exception.Message)"
        return $summary
    }

    $summary.failsafeDir = $dir
    if (-not (Test-Path -LiteralPath $dir -PathType Container)) { return $summary }

    $script:ReplFailsafeDraining = $true
    try {
        # The file name starts with the capture stamp, so a name sort is an
        # oldest-first order and stays stable across passes.
        $records = @(Get-ChildItem -LiteralPath $dir -Filter '*.yaml' -File -ErrorAction SilentlyContinue |
            Sort-Object -Property Name)

        foreach ($record in $records) {
            if ($MaxRecords -gt 0 -and $summary.scanned -ge $MaxRecords) { break }
            if ($script:ReplFailsafeInFlight.Contains($record.FullName)) {
                $summary.skipped++
                continue
            }

            $summary.scanned++

            $document = $null
            $parseError = ''
            try {
                $document = Read-McpYamlObject -Path $record.FullName
            } catch {
                $parseError = "record is not readable YAML: $($_.Exception.Message)"
            }

            $method = ''
            $recordParams = $null
            if (-not $parseError) {
                if ($document -isnot [System.Collections.IDictionary]) {
                    $parseError = 'record root is not a YAML mapping'
                } else {
                    if ($document.Contains('method')) { $method = [string]$document['method'] }
                    if ($document.Contains('params')) { $recordParams = $document['params'] }
                    if ([string]::IsNullOrWhiteSpace($method)) {
                        $parseError = 'record has no method'
                    } elseif ($null -eq $recordParams) {
                        $parseError = 'record has no params'
                    }
                }
            }

            if ($parseError) {
                if (Move-ReplFailsafeToQuarantine -Path $record.FullName -Reason $parseError) {
                    $summary.quarantined++
                }
                continue
            }

            $attempts = 0
            if ($document.Contains('drainAttempts')) {
                try { $attempts = [int]$document['drainAttempts'] } catch { $attempts = 0 }
            }
            if ($attempts -ge $MaxAttempts) {
                $reason = "exceeded the drain attempt budget of $MaxAttempts"
                if ($document.Contains('lastDrainError')) {
                    $reason += ". Last error: $([string]$document['lastDrainError'])"
                }
                if (Move-ReplFailsafeToQuarantine -Path $record.FullName -Reason $reason) {
                    $summary.quarantined++
                }
                continue
            }

            $paramsYaml = ConvertTo-Yaml -Data $recordParams -Options WithIndentedSequences
            $result = Invoke-ReplRaw -Method $method -ParamsYaml $paramsYaml
            if ($result.Success) {
                Clear-ReplFailsafe -Path $record.FullName
                $summary.replayed++
                continue
            }

            $summary.failed++
            $detail = "$($result.Error) $($result.Output)".Trim()
            if (Test-ReplFailsafeBackendUnreachable -Detail $detail) {
                # Leave the attempt counter alone: the backend, not the record,
                # failed, and burning attempts here would quarantine good turns.
                $summary.aborted = $true
                $summary.abortReason = "backend unreachable: $detail"
                break
            }

            try {
                $document['drainAttempts'] = $attempts + 1
                $document['lastDrainError'] = if ($detail.Length -gt 500) { $detail.Substring(0, 500) } else { $detail }
                Write-McpYamlObject -Path $record.FullName -Document $document
            } catch {
                [Console]::Error.WriteLine("Failsafe attempt counter update failed for '$($record.FullName)': $($_.Exception.Message)")
            }
        }
    }
    finally {
        $script:ReplFailsafeDraining = $false
    }

    return $summary
}

function Invoke-ReplFailsafeDrainOnFirstSuccess {
    <#
    .SYNOPSIS
        TR-MCP-REPL-016: run one queue drain after the first confirmed backend call.
    .DESCRIPTION
        The drain is wired here rather than to bootstrap on purpose. Bootstrap
        (Assert-ReplMarkerFresh) only proves the marker file is fresh, it does not
        prove the backend answers, and it runs inside Invoke-ReplRaw, so draining
        there would recurse. A successful Invoke-ReplRaw is the first point where
        reachability is proven, which is exactly the precondition for replay, and it
        covers every entry path (hooks, workflow verbs, direct client calls) with a
        single hook. Set MCP_FAILSAFE_DRAIN_DISABLED=1 to opt out.
    #>
    [CmdletBinding()]
    param()

    if ($script:ReplFailsafeDrainCompleted) { return }
    if ($script:ReplFailsafeDraining) { return }
    if ($env:MCP_FAILSAFE_DRAIN_DISABLED -eq '1') { return }

    # Latch before draining so a failed pass cannot retrigger on every later call.
    $script:ReplFailsafeDrainCompleted = $true
    try {
        $summary = Invoke-ReplFailsafeDrain
        if ($summary.replayed -gt 0 -or $summary.quarantined -gt 0 -or $summary.failed -gt 0) {
            [Console]::Error.WriteLine(
                "Failsafe queue drain: replayed=$($summary.replayed) failed=$($summary.failed) quarantined=$($summary.quarantined) skipped=$($summary.skipped) dir='$($summary.failsafeDir)'.")
        }
    } catch {
        [Console]::Error.WriteLine("Failsafe queue drain failed: $($_.Exception.Message)")
    }
}

function Invoke-ReplTurnUpsertParams {
    # Build as object, will be serialized to JSON in the caller (no text YAML).
    # This eliminates indentation and block-scalar errors.
    param(
        [Parameter(Mandatory)][string]$SourceType,
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][string]$RequestId,
        # TR-MCP-REPL-018: '' is a deliberate value meaning "omit the title so
        # the server preserves it" (TR-MCP-REPL-015). Mandatory alone rejects an
        # empty string at bind time, which killed every title-omitting persist
        # (appendDialog/appendActions/completeTurn/supersede) with
        # ParameterBindingValidationException (BUG-TRIAGE-087/089/091/098).
        [Parameter(Mandatory)][AllowEmptyString()][string]$Title,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [string]$ActionsYaml = '',
        [object[]]$ProcessingDialog = @(),
        [string]$Interpretation = '',
        [int]$TokenCount = 0,
        [string[]]$Tags = @(),
        [string[]]$ContextList = @()
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
        -TokenCount $TokenCount `
        -Interpretation $Interpretation `
        -Tags $Tags `
        -ContextList $ContextList `
        -FilesModified $filePaths `
        -Actions $actions `
        -ProcessingDialog $ProcessingDialog

    return $request.ToParamsObject()
}

function Invoke-ReplPersistTurn {
    # Build the complete session payload once, save it before the remote call, and
    # remove the local copy only after MCP confirms durable persistence.
    param(
        [Parameter(Mandatory)][string]$RequestId,
        [AllowEmptyString()][string]$Title = '',
        [switch]$IncludeSessionTitle,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [string]$ActionsYaml = '',
        [object[]]$ProcessingDialog = @(),
        [string]$Interpretation = '',
        [int]$TokenCount = 0,
        [string[]]$Tags = @(),
        [string[]]$ContextList = @()
    )
    $script:LastReplPersistenceDetails = $null
    $meta = Get-ReplSessionMeta
    if (-not $meta) { throw 'Session log persistence failed because no session metadata is cached.' }

    $turnObj = Invoke-ReplTurnUpsertParams -SourceType $meta.SourceType -SessionId $meta.SessionId -RequestId $RequestId -Title $Title -Status $Status -ResponseText $ResponseText -ActionsYaml $ActionsYaml -ProcessingDialog $ProcessingDialog -Interpretation $Interpretation -TokenCount $TokenCount -Tags $Tags -ContextList $ContextList

    # TR-MCP-REPL-015: send the session title only when the caller explicitly seeds
    # or sets it (IncludeSessionTitle). Otherwise omit it so an incidental re-submit
    # never retitles the session; the server preserves the omitted field.
    $sessionTitle = Get-ReplSessionStateValue -Key 'title'
    $sessionStarted = Get-ReplSessionStateValue -Key 'started'
    if (-not $sessionStarted) { $sessionStarted = [string]$turnObj.turn.timestamp }
    $sessionLog = [ordered]@{
        sourceType = $meta.SourceType
        sessionId = $meta.SessionId
        model = [string]$turnObj.turn.model
        started = $sessionStarted
        lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        status = 'in_progress'
        turnCount = 1
        turns = @($turnObj.turn)
    }
    if ($IncludeSessionTitle -and -not [string]::IsNullOrWhiteSpace($sessionTitle)) {
        $sessionLog.title = $sessionTitle
    }

    $agentHeaderFields = [ordered]@{
        agentSessionId = $env:MCP_AGENT_SESSION_ID
        agentSessionTranscriptFile = $env:MCP_AGENT_SESSION_TRANSCRIPT_FILE
        agentExecutablePath = $env:MCP_AGENT_EXECUTABLE_PATH
        agentExecutableVersion = $env:MCP_AGENT_EXECUTABLE_VERSION
    }
    foreach ($entry in $agentHeaderFields.GetEnumerator()) {
        if (-not [string]::IsNullOrWhiteSpace([string]$entry.Value)) {
            $sessionLog[$entry.Key] = [string]$entry.Value
        }
    }

    $payloadObject = [ordered]@{
        sessionLog = $sessionLog
    }
    $paramsYaml = ConvertTo-Yaml -Data $payloadObject -Options WithIndentedSequences
    $failsafePath = Write-ReplFailsafe -Method 'client.SessionLog.SubmitAsync' -ParamsYaml $paramsYaml -Label 'session_submit'
    if (-not $failsafePath) {
        throw "Session log persistence failed because the failsafe payload could not be saved for request '$RequestId'."
    }

    $result = Invoke-ReplRaw -Method 'client.SessionLog.SubmitAsync' -ParamsYaml $paramsYaml
    if (-not $result.Success) {
        throw "Session log persistence failed for request '$RequestId'. FailsafePath='$failsafePath'. Output=$($result.Output) Error=$($result.Error)"
    }

    $response = Convert-ReplParamsYamlToObject -ParamsYaml $result.Output
    $payload = Get-ReplObjectValue -InputObject $response -Name 'payload'
    $details = Get-ReplObjectValue -InputObject $payload -Name 'result'
    if (-not $details) {
        $details = [ordered]@{
            persisted = $true
            degraded = $false
            persistenceStrategy = 'mcp-service'
            failsafePath = $null
            message = $null
        }
    }

    $persisted = Get-ReplObjectValue -InputObject $details -Name 'persisted'
    if ($null -eq $persisted) {
        $details = [ordered]@{
            persisted = $true
            degraded = $false
            persistenceStrategy = 'mcp-service'
            failsafePath = $null
            message = $null
        }
        $persisted = $true
    }
    if ($persisted -ne $true) {
        throw "Session log persistence did not confirm a durable write for request '$RequestId'. FailsafePath='$failsafePath'."
    }

    Clear-ReplFailsafe -Path $failsafePath
    $script:LastReplPersistenceDetails = $details
    return $true
}
function Update-ReplTurnCacheStatus {
    param([Parameter(Mandatory)][string]$NewStatus)
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $state['status'] = $NewStatus
    Write-ReplCurrentTurnState -State $state
    return $true
}

function Update-ReplTurnCacheEdits {
    param([Parameter(Mandatory)][int]$Increment)
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $current = if ($state.Contains('codeEdits')) { [int]$state['codeEdits'] } else { 0 }
    $state['codeEdits'] = $current + $Increment
    Write-ReplCurrentTurnState -State $state
    return $true
}

function Get-ReplTurnCacheField {
    param([Parameter(Mandatory)][string]$Field)
    $state = Read-ReplCurrentTurnState
    if (-not $state -or -not $state.Contains($Field) -or $null -eq $state[$Field]) { return '' }
    return [string]$state[$Field]
}

function Set-ReplTurnCacheField {
    param(
        [Parameter(Mandatory)][string]$Field,
        [Parameter(Mandatory)][string]$Value
    )
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $state[$Field] = $Value
    Write-ReplCurrentTurnState -State $state
    return $true
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
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $current = if ($state.Contains($Field)) { [int]$state[$Field] } else { 0 }
    $state[$Field] = $current + $Increment
    Write-ReplCurrentTurnState -State $state
    return $true
}

function Get-ReplObjectValue {
    param(
        $InputObject,
        [Parameter(Mandatory)][string]$Name
    )

    if ($null -eq $InputObject) { return $null }
    if ($InputObject -is [System.Collections.IDictionary]) {
        if ($InputObject.Contains($Name)) { return $InputObject[$Name] }
        return $null
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($property) { return $property.Value }
    return $null
}

function Get-ReplParamString {
    param(
        [string]$ParamsYaml,
        [Parameter(Mandatory)][string]$Name
    )
    if (-not $ParamsYaml) { return '' }
    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    if (-not $params) { return '' }
    $value = Get-ReplObjectValue -InputObject $params -Name $Name
    if ($null -eq $value) { return '' }
    return [string]$value
}

function Update-ReplTurnTitleFromParams {
    param([string]$ParamsYaml)
    $queryTitle = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'queryTitle'
    if ([string]::IsNullOrWhiteSpace($queryTitle)) { return $false }
    return Set-ReplTurnCacheField -Field 'queryTitle' -Value $queryTitle
}

function Assert-ReplCurrentTurnFresh {
    param([Parameter(Mandatory)][string]$Method)

    $turnFile = Get-ReplCurrentTurnFile
    $turnState = Read-ReplCurrentTurnState
    if (-not $turnState) { return $false }

    $sessionFile = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    $sessionState = Read-McpYamlObject -Path $sessionFile -Create
    $workspace = Resolve-ReplWorkspaceDirectory
    $snapshot = $null
    try {
        $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
    } catch {
        $snapshot = $null
    }

    $staleReasons = @()
    $turnSessionId = if ($turnState.Contains('sessionId')) { [string]$turnState['sessionId'] } else { '' }
    $activeSessionId = if ($sessionState.Contains('sessionId')) { [string]$sessionState['sessionId'] } else { '' }
    $turnMarkerPath = if ($turnState.Contains('markerFilePath')) { [string]$turnState['markerFilePath'] } else { '' }
    $turnMarkerWriteUtc = if ($turnState.Contains('markerLastWriteUtc')) { [string]$turnState['markerLastWriteUtc'] } else { '' }

    if ($turnSessionId -and $activeSessionId -and $turnSessionId -ne $activeSessionId) {
        $staleReasons += 'sessionId'
    }
    if ($snapshot -and $turnMarkerPath -and $turnMarkerPath -ne $snapshot.markerFilePath) {
        $staleReasons += 'markerFilePath'
    }
    if ($snapshot -and $turnMarkerWriteUtc -and $turnMarkerWriteUtc -ne $snapshot.markerLastWriteUtc) {
        $staleReasons += 'markerLastWriteUtc'
    }

    # TR-MCP-PLUGIN-012: a sessionId-only mismatch means the session rotated (Start-PluginSession
    # minted a new id) while the turn cache still carries the old one. Re-bind the turn to the active
    # session (below) instead of hard-rejecting every subsequent completeTurn (BUG-TRIAGE-071/075).
    # Marker drift (wrong-workspace) is still rejected separately.
    if ($staleReasons -contains 'sessionId') {
        [Console]::Error.WriteLine("$Method re-binding current-turn cache '$turnFile' from rotated sessionId '$turnSessionId' to active sessionId '$activeSessionId'.")
    }

    $markerDriftReasons = @($staleReasons | Where-Object { $_ -eq 'markerFilePath' -or $_ -eq 'markerLastWriteUtc' })
    if ($markerDriftReasons.Count -gt 0) {
        if (-not (Assert-ReplMarkerFresh)) {
            $markerPath = if ($snapshot) { $snapshot.markerFilePath } else { '' }
            $markerLastWriteUtc = if ($snapshot) { $snapshot.markerLastWriteUtc } else { '' }
            [Console]::Error.WriteLine("$Method rejected stale current-turn cache '$turnFile'. staleSessionId='$turnSessionId' activeSessionId='$activeSessionId' markerFilePath='$markerPath' markerLastWriteUtc='$markerLastWriteUtc' staleReasons='$($markerDriftReasons -join ',')'. $(Get-ReplRecoveryGuidance)")
            return $false
        }

        $sessionState = Read-McpYamlObject -Path $sessionFile -Create
        $activeSessionId = if ($sessionState.Contains('sessionId')) { [string]$sessionState['sessionId'] } else { '' }

        try {
            $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
        } catch {
            $snapshot = $null
        }
    }

    # TR-MCP-PLUGIN-012: adopt the active session id (rotation re-bind, or fill when the turn has none).
    if ($activeSessionId -and $turnSessionId -ne $activeSessionId) { $turnState['sessionId'] = $activeSessionId }
    if ($snapshot) {
        $turnState['markerFilePath'] = $snapshot.markerFilePath
        $turnState['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
    }
    Write-ReplCurrentTurnState -State $turnState

    return $true
}

function New-ReplBeginTurnRequestId {
    return ('req-{0}-turn-{1:x4}' -f (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'), (Get-Random -Maximum 0xffff))
}

function Get-ReplServerTurnTitle {
    # TR-MCP-REPL-019: read the server-side title of one turn through the
    # existing client passthrough (client.SessionLog.QueryAsync). Returns ''
    # when the session, the turn, or its title cannot be resolved; callers
    # treat '' as "omit the title" (TR-MCP-REPL-015).
    param([Parameter(Mandatory)][string]$RequestId)

    $meta = Get-ReplSessionMeta
    if (-not $meta) { return '' }

    try {
        $callParams = [ordered]@{
            agent = $meta.SourceType
            limit = 25
        }
        $callYaml = ConvertTo-Yaml -Data $callParams -Options WithIndentedSequences
        $result = Invoke-ReplRaw -Method 'client.SessionLog.QueryAsync' -ParamsYaml $callYaml
        if (-not $result.Success) { return '' }

        $response = Convert-ReplParamsYamlToObject -ParamsYaml $result.Output
        $payload = Get-ReplObjectValue -InputObject $response -Name 'payload'
        $details = Get-ReplObjectValue -InputObject $payload -Name 'result'
        if ($null -eq $details) { $details = $payload }
        $items = Get-ReplObjectValue -InputObject $details -Name 'items'
        foreach ($session in @($items)) {
            if ([string](Get-ReplObjectValue -InputObject $session -Name 'sessionId') -ne $meta.SessionId) { continue }
            foreach ($turn in @((Get-ReplObjectValue -InputObject $session -Name 'turns'))) {
                if ([string](Get-ReplObjectValue -InputObject $turn -Name 'requestId') -eq $RequestId) {
                    return [string](Get-ReplObjectValue -InputObject $turn -Name 'queryTitle')
                }
            }
        }
    } catch {
        # An unreadable query response must never block the supersede persist;
        # fall through to '' so the title is simply omitted.
    }
    return ''
}

function Resolve-ReplSupersedeTitle {
    # TR-MCP-REPL-019: choose the title persisted with a superseded turn.
    # Rules (BUG-TRIAGE-086):
    #   - A locally refined title (non-empty and different from the hook's raw
    #     default: the prompt first line or the literal 'User prompt') wins and
    #     costs no server round trip.
    #   - An empty or raw-default local title defers to the server-side title,
    #     fetched through the existing client passthrough.
    #   - Raw prompt text is NEVER re-sent as a title; when no refined title
    #     exists anywhere the title is omitted ('') per TR-MCP-REPL-015.
    param(
        [Parameter(Mandatory)]$State,
        [Parameter(Mandatory)][string]$RequestId
    )

    $localTitle = if ($State.Contains('queryTitle')) { [string]$State['queryTitle'] } else { '' }
    $queryText = if ($State.Contains('queryText')) { [string]$State['queryText'] } else { '' }
    $rawDefaults = @('User prompt')
    $promptFirstLine = @(($queryText -replace "`r`n", "`n" -replace "`r", "") -split "`n")[0].Trim()
    if ($promptFirstLine) { $rawDefaults += $promptFirstLine }

    $trimmedLocal = $localTitle.Trim()
    if ($trimmedLocal -and ($rawDefaults -notcontains $trimmedLocal)) {
        return $localTitle
    }

    $serverTitle = Get-ReplServerTurnTitle -RequestId $RequestId
    $trimmedServer = $serverTitle.Trim()
    if ($trimmedServer -and ($rawDefaults -notcontains $trimmedServer)) {
        return $serverTitle
    }
    return ''
}

function Invoke-ReplSupersedeCurrentTurnIfInProgress {
    param([Parameter(Mandatory)][string]$NextRequestId)

    $state = Read-ReplCurrentTurnState
    if (-not $state) { return }
    $status = if ($state.Contains('status')) { [string]$state['status'] } else { '' }
    if ($status -ne 'in_progress') { return }
    $oldRequestId = if ($state.Contains('turnRequestId')) { [string]$state['turnRequestId'] } else { '' }
    if (-not $oldRequestId -or $oldRequestId -eq $NextRequestId) { return }
    try {
        # TR-MCP-REPL-019: persist the superseded turn with a title that can
        # never clobber a refined title: a locally refined title is kept, a raw
        # or empty local title defers to the server-side title, and raw prompt
        # text is never re-sent (TR-MCP-REPL-015 omission is the fallback).
        $title = Resolve-ReplSupersedeTitle -State $state -RequestId $oldRequestId
        [void](Invoke-ReplPersistTurn -RequestId $oldRequestId -Title $title -Status 'canceled' -ResponseText "Superseded by $NextRequestId before it was completed.")
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.beginTurn could not persist superseded turn '$oldRequestId': $_")
    }
}

function Invoke-WorkflowBeginTurn {
    param([string]$ParamsYaml)

    $sessionId = Get-ReplSessionStateValue -Key 'sessionId'
    if ([string]::IsNullOrWhiteSpace($sessionId)) {
        [Console]::Error.WriteLine("workflow.sessionlog.beginTurn requires session-state.yaml with sessionId. $(Get-ReplRecoveryGuidance)")
        return $false
    }

    $requestId = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'requestId'
    if ([string]::IsNullOrWhiteSpace($requestId)) { $requestId = New-ReplBeginTurnRequestId }
    $queryTitle = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'queryTitle'
    if ([string]::IsNullOrWhiteSpace($queryTitle)) { $queryTitle = 'User prompt' }
    $queryText = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'queryText'
    if ([string]::IsNullOrWhiteSpace($queryText)) { $queryText = $queryTitle }

    $openedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    $turnState = [ordered]@{
        turnRequestId = $requestId
        queryTitle = $queryTitle
        openedAt = $openedAt
        status = 'in_progress'
        sessionId = $sessionId
        codeEdits = 0
        lastBuildStatus = 'unknown'
        auditActions = 0
        auditDialog = 0
        auditDecisions = 0
        auditFiles = 0
        auditCommits = 0
        queryText = $queryText
    }

    try {
        $snapshot = Get-MarkerFileSnapshot -StartDir (Resolve-ReplWorkspaceDirectory)
        $turnState['markerFilePath'] = $snapshot.markerFilePath
        $turnState['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
    } catch {
    }

    try {
        Invoke-ReplSupersedeCurrentTurnIfInProgress -NextRequestId $requestId
        Write-ReplCurrentTurnState -State $turnState
        # TR-MCP-REPL-015: seed the session title from the first turn only. Once
        # session-state has a title, later turns omit it (so the session is not
        # retitled to each new prompt); setSessionTitle changes it explicitly.
        $existingSessionTitle = Get-ReplSessionStateValue -Key 'title'
        $seedSessionTitle = ([string]::IsNullOrWhiteSpace($existingSessionTitle) -and -not [string]::IsNullOrWhiteSpace($queryTitle))
        if ($seedSessionTitle) {
            Set-ReplSessionStateValue -Key 'title' -Value $queryTitle | Out-Null
        }
        $persisted = [bool](Invoke-ReplPersistTurn -RequestId $requestId -Title $queryTitle -Status 'in_progress' -ResponseText '(turn opened)' -IncludeSessionTitle:$seedSessionTitle)
        if (-not $persisted) {
            [Console]::Error.WriteLine("workflow.sessionlog.beginTurn did not confirm durable persistence for '$requestId'.")
            return $false
        }
        return $true
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.beginTurn failed for '$requestId': $_")
        return $false
    }
}

function Invoke-WorkflowAppendActions {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.appendActions')
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.appendActions')) {
        return $false
    }

    $added = 0
    $actionsBlock = ''
    if ($ParamsYaml) {
        $p = $ParamsYaml -replace "`r`n", "`n" -replace "`r", ""
        $actionsBlock = Get-ReplNormalizedActionsBlock -ParamsYaml $p
        # Count only real filePath: fields (with value) for codeEdits. Substring matches in
        # descriptions must be ignored. Non-file actions (design_decision etc.) must persist.
        $added = ([regex]::Matches($p, '(?m)^\s*(?:-\s*)?filePath:\s*\S')).Count
    }

    if ($ParamsYaml -and $ParamsYaml.Trim()) {
        $explicitTitle = [bool](Update-ReplTurnTitleFromParams -ParamsYaml $ParamsYaml)
        if ($added -gt 0) {
            Update-ReplTurnCacheEdits -Increment $added | Out-Null
        }
        $actionC = ([regex]::Matches($ParamsYaml, '(?m)^\s*(?:-\s*)?type:')).Count
        $decC = ([regex]::Matches($ParamsYaml, '(?m)^\s*(?:-\s*)?type:\s*design_decision\b')).Count
        $comC = ([regex]::Matches($ParamsYaml, '(?m)^\s*(?:-\s*)?type:\s*commit\b')).Count
        Update-ReplTurnAudit -Field 'auditActions' -Increment $actionC | Out-Null
        Update-ReplTurnAudit -Field 'auditFiles' -Increment $added | Out-Null
        Update-ReplTurnAudit -Field 'auditDecisions' -Increment $decC | Out-Null
        Update-ReplTurnAudit -Field 'auditCommits' -Increment $comC | Out-Null

        $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
        # TR-MCP-REPL-015: send the turn title only when explicitly set this call;
        # otherwise omit so a stale cache title cannot clobber the server value.
        $title = if ($explicitTitle) { Get-ReplTurnCacheField -Field 'queryTitle' } else { '' }
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

    $rawItems = @()
    $rawValue = Get-ReplObjectValue -InputObject $params -Name 'dialogItems'
    if ($null -eq $rawValue) { $rawValue = Get-ReplObjectValue -InputObject $params -Name 'dialog' }
    if ($null -ne $rawValue) { $rawItems = @($rawValue) }

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
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.appendDialog')) {
        return $false
    }

    $dialogItems = @(Get-ReplDialogItemsFromParams -ParamsYaml $ParamsYaml)
    if ($dialogItems.Count -eq 0) {
        [Console]::Error.WriteLine('workflow.sessionlog.appendDialog requires at least one valid dialogItems or dialog entry.')
        return $false
    }

    $explicitTitle = [bool](Update-ReplTurnTitleFromParams -ParamsYaml $ParamsYaml)
    Update-ReplTurnAudit -Field 'auditDialog' -Increment $dialogItems.Count | Out-Null
    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    # TR-MCP-REPL-015: send the turn title only when explicitly set this call.
    $title = if ($explicitTitle) { Get-ReplTurnCacheField -Field 'queryTitle' } else { '' }
    return [bool](Invoke-ReplPersistTurn -RequestId $reqId -Title $title `
        -Status 'in_progress' -ResponseText 'Dialog appended.' `
        -ProcessingDialog $dialogItems)
}

function Invoke-WorkflowUpdateTurn {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.updateTurn')
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.updateTurn')) {
        return $false
    }

    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    $explicitTitle = [bool](Update-ReplTurnTitleFromParams -ParamsYaml $ParamsYaml)

    $state = Read-ReplCurrentTurnState
    $responseText = if ($state -and $state.Contains('response')) { [string]$state['response'] } else { '' }
    $interpretation = if ($state -and $state.Contains('interpretation')) { [string]$state['interpretation'] } else { '' }
    $tokenCount = if ($state -and $state.Contains('tokenCount')) { [int]$state['tokenCount'] } else { 0 }
    $tags = if ($state -and $state.Contains('tags')) { @($state['tags'] | ForEach-Object { [string]$_ }) } else { @() }
    $contextList = if ($state -and $state.Contains('contextList')) { @($state['contextList'] | ForEach-Object { [string]$_ }) } else { @() }

    if ($params) {
        $responseValue = Get-ReplObjectValue -InputObject $params -Name 'response'
        if ($null -ne $responseValue) { $responseText = [string]$responseValue }

        $interpretationValue = Get-ReplObjectValue -InputObject $params -Name 'interpretation'
        if ($null -ne $interpretationValue) { $interpretation = [string]$interpretationValue }

        $tokenValue = Get-ReplObjectValue -InputObject $params -Name 'tokenCount'
        if ($null -ne $tokenValue) { [void][int]::TryParse([string]$tokenValue, [ref]$tokenCount) }

        $tagValue = Get-ReplObjectValue -InputObject $params -Name 'tags'
        if ($null -ne $tagValue) { $tags = @($tagValue | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) }

        $contextValue = Get-ReplObjectValue -InputObject $params -Name 'contextList'
        if ($null -ne $contextValue) { $contextList = @($contextValue | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) }

        if ($state) {
            if (-not [string]::IsNullOrWhiteSpace($responseText)) { $state['response'] = $responseText }
            if (-not [string]::IsNullOrWhiteSpace($interpretation)) { $state['interpretation'] = $interpretation }
            if ($tokenCount -gt 0) { $state['tokenCount'] = $tokenCount }
            if ($tags.Count -gt 0) { $state['tags'] = @($tags) }
            if ($contextList.Count -gt 0) { $state['contextList'] = @($contextList) }
            Write-ReplCurrentTurnState -State $state
        }
    }

    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    # TR-MCP-REPL-015: send the turn title only when explicitly set this call.
    $title = if ($explicitTitle) { Get-ReplTurnCacheField -Field 'queryTitle' } else { '' }
    try {
        return [bool](Invoke-ReplPersistTurn -RequestId $reqId -Title $title `
            -Status 'in_progress' -ResponseText $responseText `
            -Interpretation $interpretation -TokenCount $tokenCount -Tags $tags -ContextList $contextList)
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.updateTurn failed for '$reqId': $_")
        return $false
    }
}

function Invoke-WorkflowCompleteTurn {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.completeTurn')
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.completeTurn')) {
        return $false
    }

    $responseText = '(no response provided)'
    if ($ParamsYaml) {
        $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
        $responseValue = Get-ReplObjectValue -InputObject $params -Name 'response'
        if ($null -ne $responseValue) {
            $responseText = [string]$responseValue
        }
    }
    $explicitTitle = [bool](Update-ReplTurnTitleFromParams -ParamsYaml $ParamsYaml)

    $actionsBlock = ''
    if ($ParamsYaml -and ($ParamsYaml -match '(?m)^\s*actions:' -or $ParamsYaml -match '(?m)^\s*actions:\s*\S')) {
        $actionsBlock = Get-ReplNormalizedActionsBlock -ParamsYaml ($ParamsYaml -replace "`r`n", "`n" -replace "`r", "")
    }

    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    # TR-MCP-REPL-015: send the turn title only when explicitly set this call.
    $title = if ($explicitTitle) { Get-ReplTurnCacheField -Field 'queryTitle' } else { '' }
    $persisted = $false
    try {
        $persisted = [bool](Invoke-ReplPersistTurn -RequestId $reqId -Title $title `
            -Status 'completed' -ResponseText $responseText -ActionsYaml $actionsBlock
        )
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.completeTurn failed for '$reqId': $_")
        return $false
    }
    if (-not $persisted) {
        return $false
    }

    Update-ReplTurnCacheStatus -NewStatus 'completed' | Out-Null

    if ($script:LastReplPersistenceDetails) {
        $degraded = Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'degraded'
        if ($degraded -eq $true) {
            $message = [string](Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'message')
            $failsafePath = [string](Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'failsafePath')
            if ([string]::IsNullOrWhiteSpace($message)) {
                $message = 'MCP Session Log persistence is degraded.'
            }
            [Console]::Error.WriteLine("$message FailsafePath='$failsafePath'.")
        }
    }
    return $true

}

function Invoke-WorkflowFailTurn {
    # TR-MCP-REPL-020: close the active turn as failed from plugin cache state
    # (BUG-TRIAGE-099). workflow.sessionlog.failTurn cannot be dispatched to the
    # REPL for plugin-shim turns: the in-process SessionLogWorkflow throws
    # 'No active session exists' because the PowerShell shim's beginTurn never
    # creates REPL-native state (that in-process contract is correct for
    # REPL-native sessions and stays untouched). Following the
    # appendDialog/appendActions pattern, the session and turn are resolved from
    # session-state.yaml + current-turn.yaml, the turn is persisted with status
    # 'failed' and the failure note, and current-turn.yaml is cleared so the
    # Stop hook sees a closed turn.
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.failTurn')
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.failTurn')) {
        return $false
    }

    # errorMessage is the canonical REPL contract parameter (IFailTurnParams);
    # failureNote is accepted as an alias for symmetry with the persisted field.
    $errorMessage = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'errorMessage'
    if ([string]::IsNullOrWhiteSpace($errorMessage)) {
        $errorMessage = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'failureNote'
    }
    if ([string]::IsNullOrWhiteSpace($errorMessage)) {
        [Console]::Error.WriteLine('workflow.sessionlog.failTurn requires a non-empty errorMessage (or failureNote).')
        return $false
    }
    $errorCode = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'errorCode'
    $failureNote = if ([string]::IsNullOrWhiteSpace($errorCode)) { $errorMessage } else { "$errorMessage (errorCode: $errorCode)" }

    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    $persisted = $false
    try {
        # TR-MCP-REPL-015: omit the title so failing a turn never retitles it.
        $persisted = [bool](Invoke-ReplPersistTurn -RequestId $reqId -Title '' `
            -Status 'failed' -ResponseText $failureNote)
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.failTurn failed for '$reqId': $_")
        return $false
    }
    if (-not $persisted) {
        return $false
    }

    Remove-Item -LiteralPath $turnFile -Force -ErrorAction SilentlyContinue
    return $true
}

function Invoke-WorkflowFailsafeDrain {
    # TR-MCP-REPL-016: operator-facing drain. Runs a full pass regardless of the
    # once-per-process latch used by the automatic trigger, and prints the summary
    # as YAML so a human or a script can read the outcome.
    param([string]$ParamsYaml)

    $maxRecords = 0
    $maxAttempts = 5
    $params = if ($ParamsYaml) { Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml } else { $null }
    if ($params -is [System.Collections.IDictionary]) {
        if ($params.Contains('maxRecords')) {
            try { $maxRecords = [int]$params['maxRecords'] } catch { $maxRecords = 0 }
        }
        if ($params.Contains('maxAttempts')) {
            try { $maxAttempts = [int]$params['maxAttempts'] } catch { $maxAttempts = 5 }
        }
    }

    $summary = Invoke-ReplFailsafeDrain -MaxRecords $maxRecords -MaxAttempts $maxAttempts
    $script:ReplFailsafeDrainCompleted = $true
    return [pscustomobject]@{
        Success = (-not $summary.aborted)
        Output = (ConvertTo-Yaml -Data $summary -Options WithIndentedSequences)
    }
}

function Invoke-WorkflowFailsafeStatus {
    # TR-MCP-REPL-017: read-only queue depth for operators, matching what
    # mcp-status.ps1 reports, without replaying anything.
    param([string]$ParamsYaml)

    $dir = Get-ReplFailsafeDir
    $quarantineDir = Get-ReplFailsafeQuarantineDir
    $status = [ordered]@{
        failsafeDir = $dir
        quarantineDir = $quarantineDir
        pendingCount = if (Test-Path -LiteralPath $dir -PathType Container) {
            @(Get-ChildItem -LiteralPath $dir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count
        } else { 0 }
        quarantineCount = if (Test-Path -LiteralPath $quarantineDir -PathType Container) {
            @(Get-ChildItem -LiteralPath $quarantineDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count
        } else { 0 }
    }
    return [pscustomobject]@{
        Success = $true
        Output = (ConvertTo-Yaml -Data $status -Options WithIndentedSequences)
    }
}

function Invoke-WorkflowSetTurnTitle {
    # TR-MCP-REPL-014: dedicated turn retitle. Updates the local cache queryTitle
    # and calls the server SetTurnTitle path, so the title is durable even though
    # incidental re-submits now omit the title (TR-MCP-REPL-015).
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.setTurnTitle')
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.setTurnTitle')) {
        return $false
    }

    $title = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'queryTitle'
    if ([string]::IsNullOrWhiteSpace($title)) {
        [Console]::Error.WriteLine('workflow.sessionlog.setTurnTitle requires a non-empty queryTitle.')
        return $false
    }

    $meta = Get-ReplSessionMeta
    if (-not $meta) {
        [Console]::Error.WriteLine("workflow.sessionlog.setTurnTitle requires cached session metadata. $(Get-ReplRecoveryGuidance)")
        return $false
    }
    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    if ([string]::IsNullOrWhiteSpace($reqId)) {
        [Console]::Error.WriteLine('workflow.sessionlog.setTurnTitle requires an active turn requestId.')
        return $false
    }

    Set-ReplTurnCacheField -Field 'queryTitle' -Value $title | Out-Null

    $callParams = [ordered]@{
        agent     = $meta.SourceType
        sessionId = $meta.SessionId
        requestId = $reqId
        title     = $title
    }
    $callYaml = ConvertTo-Yaml -Data $callParams -Options WithIndentedSequences
    $result = Invoke-ReplRaw -Method 'client.SessionLog.SetTurnTitleAsync' -ParamsYaml $callYaml
    if (-not $result.Success) {
        [Console]::Error.WriteLine("workflow.sessionlog.setTurnTitle server call failed: $($result.Error)$($result.Output)")
        return $false
    }
    return $true
}

function Invoke-WorkflowSetSessionTitle {
    # TR-MCP-REPL-014: dedicated session retitle. Writes the stable session-state
    # title and calls the server SetSessionTitle path.
    param([string]$ParamsYaml)
    $title = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'title'
    if ([string]::IsNullOrWhiteSpace($title)) {
        $title = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'sessionTitle'
    }
    if ([string]::IsNullOrWhiteSpace($title)) {
        [Console]::Error.WriteLine('workflow.sessionlog.setSessionTitle requires a non-empty title.')
        return $false
    }

    $meta = Get-ReplSessionMeta
    if (-not $meta) {
        [Console]::Error.WriteLine("workflow.sessionlog.setSessionTitle requires cached session metadata. $(Get-ReplRecoveryGuidance)")
        return $false
    }

    Set-ReplSessionStateValue -Key 'title' -Value $title | Out-Null

    $callParams = [ordered]@{
        agent     = $meta.SourceType
        sessionId = $meta.SessionId
        title     = $title
    }
    $callYaml = ConvertTo-Yaml -Data $callParams -Options WithIndentedSequences
    $result = Invoke-ReplRaw -Method 'client.SessionLog.SetSessionTitleAsync' -ParamsYaml $callYaml
    if (-not $result.Success) {
        [Console]::Error.WriteLine("workflow.sessionlog.setSessionTitle server call failed: $($result.Error)$($result.Output)")
        return $false
    }
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
        'workflow.sessionlog.beginTurn'       { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowBeginTurn -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.openSession'     { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowOpenSession -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.updateTurn'      { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowUpdateTurn -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.appendActions'   { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowAppendActions -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.appendDialog'    { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowAppendDialog -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.completeTurn'    { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowCompleteTurn -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.failTurn'        { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowFailTurn -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.setTurnTitle'    { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowSetTurnTitle -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.setSessionTitle' { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowSetSessionTitle -ParamsYaml $ParamsYaml); return }
        'workflow.failsafe.drain' {
            # These two verbs report a YAML document to stdout as well as a boolean
            # outcome, so they return a result object instead of a bare boolean.
            $drainResult = Invoke-WorkflowFailsafeDrain -ParamsYaml $ParamsYaml
            $script:LastInvokeReplMethodSuccess = [bool]$drainResult.Success
            $drainResult.Output
            return
        }
        'workflow.failsafe.status' {
            $failsafeStatusResult = Invoke-WorkflowFailsafeStatus -ParamsYaml $ParamsYaml
            $script:LastInvokeReplMethodSuccess = [bool]$failsafeStatusResult.Success
            $failsafeStatusResult.Output
            return
        }
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
