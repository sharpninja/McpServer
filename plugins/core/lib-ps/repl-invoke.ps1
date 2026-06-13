<#
.SYNOPSIS
    Sends a YAML request envelope to mcpserver-repl --agent-stdio.
.DESCRIPTION
    PowerShell parallel of lib/repl-invoke.sh. Constructs a YAML envelope
    and pipes it to the mcpserver-repl dotnet tool.

    Translation shim: workflow.sessionlog.* methods are not server routes
    — the dispatcher rejects them as method_not_found. They are plugin-
    local verbs that update cache/current-turn.yaml so the Stop hook can
    verify completion, and (best-effort) persist a session-log turn via
    the real client.SessionLog.SubmitAsync route.

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

$script:ReplInvokePluginRoot = if ($env:PLUGIN_ROOT_OVERRIDE) {
    $env:PLUGIN_ROOT_OVERRIDE
} else {
    Split-Path -Parent $PSScriptRoot
}

if (-not (Get-Command Resolve-McpCacheDir -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'resolve-cache-dir.ps1')
}

# Resolved lazily so per-call context (workspace / env) governs path.
function script:Get-ReplInvokeCacheDir { Resolve-McpCacheDir }

function Get-ReplSessionMeta {
    $f = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    if (-not (Test-Path $f)) { return $null }
    $line = Select-String -Path $f -Pattern '^sessionId:' -SimpleMatch:$false |
        Select-Object -First 1
    if (-not $line) { return $null }
    $sid = ($line.Line -replace '^sessionId:\s*', '').Trim()
    if (-not $sid) { return $null }
    $prefix = ($sid -split '-', 2)[0]
    [pscustomobject]@{ SourceType = $prefix; SessionId = $sid }
}

function Invoke-ReplRaw {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )
    if (-not (Get-Command mcpserver-repl -ErrorAction SilentlyContinue)) {
        Write-Error 'mcpserver-repl not found on PATH'
        return @{ Success = $false; Output = '' }
    }

    $requestId = "req-$(Get-Date -AsUTC -Format 'yyyyMMddTHHmmssZ')-$((Get-Random -Maximum 0xFFFF).ToString('x4'))"
    $timeout = if ($env:REPL_TIMEOUT) { [int]$env:REPL_TIMEOUT } else { 30 }

    $envelope = "type: request`npayload:`n  requestId: $requestId`n  method: $Method"
    if ($ParamsYaml) {
        $indented = ($ParamsYaml -split "`n" | ForEach-Object { "    $_" }) -join "`n"
        $envelope += "`n  params:`n$indented"
    }

    try {
        $psi = [System.Diagnostics.ProcessStartInfo]::new('mcpserver-repl', '--agent-stdio')
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        # Do NOT redirect stderr: mcpserver-repl logs verbose 'info:' lines
        # to stderr, and an unread redirected stream blocks the child once
        # its pipe buffer fills (Windows ~4 KB), causing WaitForExit to hang.
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        # mcpserver-repl writes UTF-8 (with BOM). Without explicit encoding,
        # PowerShell decodes as cp437 and BOM bytes (EF BB BF) become box-
        # drawing glyphs that break the '^type: error' regex anchor.
        $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8

        $proc = [System.Diagnostics.Process]::Start($psi)
        $proc.StandardInput.WriteLine($envelope)
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
            return @{ Success = $false; Output = '' }
        }
        $output = $readTask.Result
        $proc.WaitForExit()

        # mcpserver-repl writes a UTF-8 BOM before the YAML doc and may
        # interleave logger 'info:' lines on stdout — strip BOM and ignore
        # leading log noise so the regex anchor matches the real header.
        $output = $output -replace "[\uFEFF]", ''
        $isError = $output -match '(?m)^type:\s*error\b'
        if ($proc.ExitCode -ne 0 -or $isError) {
            return @{ Success = $false; Output = $output }
        }
        return @{ Success = $true; Output = $output }
    }
    catch {
        Write-Error "mcpserver-repl invocation failed for method ${Method}: $_"
        return @{ Success = $false; Output = '' }
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

function Get-ReplCurrentTurnQueryText {
    # Extract the queryText literal block from current-turn.yaml (twin of
    # _repl_yaml_block_get for the one block the upsert path needs).
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return '' }
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
        $doc = "method: $Method`nlabel: $Label`ntimestamp: $stamp`nparams:`n"
        $doc += (($ParamsYaml -split "`n" | ForEach-Object { "  $_" }) -join "`n") + "`n"
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
    # Twin of _repl_turn_upsert_params: agent/sessionId/turn document for
    # client.SessionLog.UpsertTurnAsync.
    param(
        [Parameter(Mandatory)][string]$SourceType,
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [string]$ActionsYaml = ''
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

    $queryLines = ($queryText -split "`n" | ForEach-Object { "    $_" }) -join "`n"
    $respLines = ($ResponseText -split "`n" | ForEach-Object { "    $_" }) -join "`n"

    $filesModifiedBlock = ''
    if ($ActionsYaml) {
        $filePaths = [regex]::Matches($ActionsYaml, '(?m)^\s*filePath:\s*(.+)$') |
            ForEach-Object { $_.Groups[1].Value.Trim() } | Where-Object { $_ }
        if ($filePaths) {
            $filesModifiedBlock = "  filesModified:`n" +
                (($filePaths | ForEach-Object { "    - $_" }) -join "`n")
        }
    }

    $actionsSection = ''
    if ($ActionsYaml) {
        $actionsSection = "  actions:`n" +
            (($ActionsYaml -split "`n" | ForEach-Object { "    $_" }) -join "`n")
    }

    $doc = @"
agent: $SourceType
sessionId: $SessionId
turn:
  requestId: $RequestId
  timestamp: $timestamp
  queryText: |
$queryLines
  queryTitle: $Title
  response: |
$respLines
  status: $Status
  model: $model
  tokenCount: !!int 0
"@
    if ($filesModifiedBlock) { $doc += "`n$filesModifiedBlock" }
    if ($actionsSection) { $doc += "`n$actionsSection" }
    return $doc
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
        [string]$ActionsYaml = ''
    )

    $respLines = ($ResponseText -split "`n" | ForEach-Object { "      $_" }) -join "`n"
    $params = @"
sessionLog:
  sourceType: $($Meta.SourceType)
  sessionId: $($Meta.SessionId)
  title: $Title
  status: in_progress
  turns:
    - requestId: $RequestId
      queryTitle: $Title
      status: $Status
      response: |
$respLines
"@
    if ($ActionsYaml) {
        $actLines = ($ActionsYaml -split "`n" | ForEach-Object { "      $_" }) -join "`n"
        $params += "`n      actions:`n$actLines"
    }

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
        [string]$ActionsYaml = ''
    )
    $meta = Get-ReplSessionMeta
    if (-not $meta) { return $false }

    $turnParams = Invoke-ReplTurnUpsertParams -SourceType $meta.SourceType `
        -SessionId $meta.SessionId -RequestId $RequestId -Title $Title `
        -Status $Status -ResponseText $ResponseText -ActionsYaml $ActionsYaml

    $failsafe = Write-ReplFailsafe -Method 'client.SessionLog.UpsertTurnAsync' `
        -ParamsYaml $turnParams -Label 'session_upsertTurn'

    $r = Invoke-ReplRaw -Method 'client.SessionLog.UpsertTurnAsync' -ParamsYaml $turnParams
    if ($r.Success) {
        Clear-ReplFailsafe -Path $failsafe
        return $true
    }

    if ($r.Output -match 'method_not_found') {
        Clear-ReplFailsafe -Path $failsafe
        return (Invoke-ReplSubmitSessionFallback -Meta $meta -RequestId $RequestId `
            -Title $Title -Status $Status -ResponseText $ResponseText -ActionsYaml $ActionsYaml)
    }

    return $false
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

function Invoke-WorkflowAppendActions {
    param([string]$ParamsYaml)
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return $true }

    $added = 0
    if ($ParamsYaml) {
        $added = ([regex]::Matches($ParamsYaml, '(?m)^\s*filePath:\s*\S')).Count
    }
    if ($added -le 0) { return $true }

    Update-ReplTurnCacheEdits -Increment $added | Out-Null

    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    $title = Get-ReplTurnCacheField -Field 'queryTitle'
    Invoke-ReplPersistTurn -RequestId $reqId -Title $title `
        -Status 'in_progress' -ResponseText 'Actions appended.' `
        -ActionsYaml $ParamsYaml | Out-Null
    return $true
}

function Invoke-WorkflowCompleteTurn {
    param([string]$ParamsYaml)
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return $true }

    $responseText = '(no response provided)'
    if ($ParamsYaml -match '(?ms)^\s*response:\s*\|\s*\r?\n(.*)$') {
        $block = $Matches[1]
        $responseText = ($block -split "`n" | ForEach-Object {
            $_ -replace '^\s{0,8}', ''
        }) -join "`n"
        $responseText = $responseText.TrimEnd()
    } elseif ($ParamsYaml -match '(?m)^\s*response:\s*(.+)$') {
        $responseText = $Matches[1].Trim()
    }

    Update-ReplTurnCacheStatus -NewStatus 'completed' | Out-Null

    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    $title = Get-ReplTurnCacheField -Field 'queryTitle'
    Invoke-ReplPersistTurn -RequestId $reqId -Title $title `
        -Status 'completed' -ResponseText $responseText | Out-Null
    return $true
}

function Invoke-ReplMethod {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )

    switch -Wildcard ($Method) {
        'workflow.sessionlog.beginTurn'       { return $true }
        'workflow.sessionlog.openSession'     { return $true }
        'workflow.sessionlog.appendActions'   { return Invoke-WorkflowAppendActions -ParamsYaml $ParamsYaml }
        'workflow.sessionlog.completeTurn'    { return Invoke-WorkflowCompleteTurn -ParamsYaml $ParamsYaml }
    }

    $r = Invoke-ReplRaw -Method $Method -ParamsYaml $ParamsYaml
    if ($r.Output) { Write-Host $r.Output }
    return [bool]$r.Success
}

# Script-entry: only when invoked directly with -Method (not when dot-sourced).
if ($Method -and $MyInvocation.InvocationName -ne '.') {
    $ok = Invoke-ReplMethod -Method $Method -ParamsYaml $ParamsYaml
    if (-not $ok) { exit 1 }
    exit 0
}
