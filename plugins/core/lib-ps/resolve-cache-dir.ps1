<#
.SYNOPSIS
    Resolve the workspace-scoped PowerShell plugin cache directory.
.DESCRIPTION
    Runtime state belongs to the active workspace, never to the installed
    plugin checkout. The returned path is <workspace>/.mcpServer/<agent>.

    Precedence:
      1. MCP_CACHE_DIR_OVERRIDE for an explicit test or recovery override.
      2. The workspace marker found from the explicit or active start path.
      3. MCP/host workspace environment variables as a markerless fallback.
 #>

$script:ResolveCacheDirScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Get-McpCacheAgentKey {
    [CmdletBinding()]
    param()

    $agent = @(
        $env:MCP_AGENT_NAME,
        $env:PLUGIN_AGENT_NAME,
        $env:PLUGIN_AGENT_DEFAULT,
        $env:MCP_PLUGIN_HOST
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

    if (-not $agent) { return 'default' }

    switch ($agent.Trim().ToLowerInvariant()) {
        'claude' { return 'claude' }
        'claudecode' { return 'claude' }
        'claude-code' { return 'claude' }
        'claudecowork' { return 'cowork' }
        'claude-cowork' { return 'cowork' }
        'codex' { return 'codex' }
        'copilot' { return 'copilot' }
        'grok' { return 'grok' }
        'grokcode' { return 'grok' }
        'grok-code' { return 'grok' }
        'cline' { return 'cline' }
        'cline-v2' { return 'cline' }
        'opencode' { return 'opencode' }
        'open-code' { return 'opencode' }
    }

    $key = ($agent.Trim() -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
    if (-not $key) { return 'default' }
    return $key
}

function Get-McpPluginRoot {
    if ($env:MCP_PLUGIN_ROOT) { return $env:MCP_PLUGIN_ROOT }
    if ($env:CLAUDE_PLUGIN_ROOT) { return $env:CLAUDE_PLUGIN_ROOT }
    return (Split-Path -Parent $script:ResolveCacheDirScriptDir)
}

function Join-McpWorkspaceCachePath {
    param([Parameter(Mandatory)][string]$WorkspacePath)

    return (Join-Path (Join-Path $WorkspacePath '.mcpServer') (Get-McpCacheAgentKey))
}

function Get-McpWorkspaceFromEnvironment {
    $configured = @(
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:MCP_WORKSPACE_PATH,
        $env:CLAUDE_PROJECT_DIR,
        $env:CODEX_WORKSPACE_PATH,
        $env:CODEX_PROJECT_DIR,
        $env:COWORK_WORKSPACE_PATH,
        $env:CLINE_WORKSPACE_PATH,
        $env:OPENCODE_WORKSPACE_PATH
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

    if ($configured -and (Test-Path -LiteralPath $configured -PathType Container)) {
        return (Resolve-Path -LiteralPath $configured).ProviderPath
    }

    return $null
}

function Resolve-McpCacheDir {
    [CmdletBinding()]
    param([string]$StartPath)

    if ($env:MCP_CACHE_DIR_OVERRIDE) {
        return $env:MCP_CACHE_DIR_OVERRIDE
    }

    if (-not (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue)) {
        $resolver = Join-Path $script:ResolveCacheDirScriptDir 'marker-resolver.ps1'
        if (Test-Path -LiteralPath $resolver) {
            . $resolver
        }
    }

    $startCandidates = if (-not [string]::IsNullOrWhiteSpace($StartPath)) {
        @($StartPath)
    } else {
        @(
            $env:MCP_WORKSPACE_START_DIR,
            $env:MCP_WORKSPACE_PATH,
            $env:MCPSERVER_WORKSPACE_PATH,
            $env:CLAUDE_PROJECT_DIR,
            $env:CODEX_CWD,
            (Get-Location).Path
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
    }

    if (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue) {
        foreach ($startDir in $startCandidates) {
            try {
                $markerFile = Find-MarkerFile -StartDir $startDir
                if ($markerFile) {
                    return (Join-McpWorkspaceCachePath -WorkspacePath (Split-Path -Parent $markerFile))
                }
            } catch {
                # Try the next active start candidate, then configured env.
            }
        }
    }

    $configuredWorkspace = Get-McpWorkspaceFromEnvironment
    if ($configuredWorkspace) {
        return (Join-McpWorkspaceCachePath -WorkspacePath $configuredWorkspace)
    }

    throw "Unable to resolve the active workspace cache. Set MCP_WORKSPACE_PATH or MCP_CACHE_DIR_OVERRIDE; plugin install paths are not workspace caches."
}

function Get-McpFailsafeDir {
    <#
    .SYNOPSIS
        TR-MCP-REPL-016: resolves the failsafe queue directory.
    .DESCRIPTION
        Single source of truth for the queue location so the writer (repl-invoke),
        the drain, and the status reporter all agree. MCPSERVER_FAILSAFE_DIR and
        MCP_FAILSAFE_DIR override the workspace cache for tests and recovery.
    #>
    [CmdletBinding()]
    param([string]$StartPath)

    if ($env:MCPSERVER_FAILSAFE_DIR) { return $env:MCPSERVER_FAILSAFE_DIR }
    if ($env:MCP_FAILSAFE_DIR) { return $env:MCP_FAILSAFE_DIR }
    return (Join-Path (Resolve-McpCacheDir -StartPath $StartPath) 'failsafe')
}

function Get-McpFailsafeQuarantineDir {
    <#
    .SYNOPSIS
        TR-MCP-REPL-017: resolves the quarantine directory under the failsafe queue.
    .DESCRIPTION
        Records that cannot be replayed are moved here instead of being deleted or
        retried forever, so nothing is lost and the live queue keeps draining.
    #>
    [CmdletBinding()]
    param([string]$StartPath)

    return (Join-Path (Get-McpFailsafeDir -StartPath $StartPath) 'quarantine')
}

function Get-McpPathVolumeRoot {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001: returns the volume root of a filesystem path.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }

    try {
        $full = [System.IO.Path]::GetFullPath($Path)
        $root = [System.IO.Path]::GetPathRoot($full)
        if ([string]::IsNullOrWhiteSpace($root)) { return $null }
        return $root
    } catch {
        return $null
    }
}

function New-McpPluginTempAlignmentResult {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001: builds the TEMP alignment result object.
    #>
    [CmdletBinding()]
    param(
        [bool]$Succeeded,
        [bool]$Changed,
        [string]$TempPath,
        [string]$ErrorMessage
    )

    return [pscustomobject]@{
        Succeeded = [bool]$Succeeded
        Changed = [bool]$Changed
        TempPath = $TempPath
        Error = $ErrorMessage
    }
}

function Get-McpPluginWorkspaceTempDirectory {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001: prefers workspace .mcpServer/tmp on the target volume.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TargetPath)

    $dir = $TargetPath
    $isLeaf = $false
    try {
        $isLeaf = Test-Path -LiteralPath $TargetPath -PathType Leaf
    } catch {
        $isLeaf = $false
    }
    if ($isLeaf) {
        $dir = Split-Path -Parent $TargetPath
    }

    $cursor = $dir
    for ($i = 0; $i -lt 20 -and -not [string]::IsNullOrWhiteSpace($cursor); $i++) {
        $marker = Join-Path $cursor 'AGENTS-README-FIRST.yaml'
        $markerExists = $false
        try {
            $markerExists = Test-Path -LiteralPath $marker -PathType Leaf
        } catch {
            $markerExists = $false
        }
        if ($markerExists) {
            return (Join-Path (Join-Path $cursor '.mcpServer') 'tmp')
        }

        $parent = Split-Path -Parent $cursor
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $cursor) { break }
        $cursor = $parent
    }

    return (Join-Path (Join-Path $dir '.mcpServer') 'tmp')
}

function Set-McpPluginSameVolumeTemp {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001 / TR-MCP-TEMPVOL-001: align process TEMP and TMP to the target volume.
    .DESCRIPTION
        When TEMP/TMP are on a different volume than TargetPath, sets both to a writable
        directory on the target volume. Same-volume TEMP is left unchanged. A failed
        directory create does not mutate TEMP/TMP and returns Succeeded false with Error.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TargetPath)

    $currentTemp = if (-not [string]::IsNullOrWhiteSpace($env:TEMP)) {
        $env:TEMP
    } elseif (-not [string]::IsNullOrWhiteSpace($env:TMP)) {
        $env:TMP
    } else {
        [System.IO.Path]::GetTempPath()
    }

    $targetRoot = Get-McpPathVolumeRoot -Path $TargetPath
    $tempRoot = Get-McpPathVolumeRoot -Path $currentTemp
    if ($targetRoot -and $tempRoot -and $targetRoot.Equals($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return (New-McpPluginTempAlignmentResult -Succeeded $true -Changed $false -TempPath $currentTemp)
    }

    try {
        $alignedDir = Get-McpPluginWorkspaceTempDirectory -TargetPath $TargetPath
        [void][System.IO.Directory]::CreateDirectory($alignedDir)
        $probe = Join-Path $alignedDir ('.write-probe-' + [guid]::NewGuid().ToString('N'))
        [System.IO.File]::WriteAllText($probe, 'ok')
        [System.IO.File]::Delete($probe)
    } catch {
        return (New-McpPluginTempAlignmentResult -Succeeded $false -Changed $false -TempPath $currentTemp -ErrorMessage ('Unable to create writable TEMP on the workspace volume: ' + $_.Exception.Message))
    }

    $resolved = [System.IO.Path]::GetFullPath($alignedDir)
    $env:TEMP = $resolved
    $env:TMP = $resolved
    [Environment]::SetEnvironmentVariable('TEMP', $resolved, 'Process')
    [Environment]::SetEnvironmentVariable('TMP', $resolved, 'Process')
    return (New-McpPluginTempAlignmentResult -Succeeded $true -Changed $true -TempPath $resolved)
}

function Invoke-McpPluginReplacementMove {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001: replacement move that never reports success when the destination is unchanged.
    .DESCRIPTION
        Cross-volume File.Move is refused with a visible Error. Exceptions and missing
        destinations also return Succeeded false. Preview-without-apply cannot look like success.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$DestinationPath
    )

    $failed = {
        param([string]$Message)
        return [pscustomobject]@{
            Succeeded = $false
            Error = $Message
            DestinationUnchanged = $true
        }
    }

    $srcRoot = $null
    $dstRoot = $null
    try { $srcRoot = Get-McpPathVolumeRoot -Path $SourcePath } catch { $srcRoot = $null }
    try { $dstRoot = Get-McpPathVolumeRoot -Path $DestinationPath } catch { $dstRoot = $null }

    if ([string]::IsNullOrWhiteSpace($srcRoot) -or [string]::IsNullOrWhiteSpace($dstRoot)) {
        return (& $failed 'replacement move refused: source or destination volume could not be resolved')
    }

    if (-not $srcRoot.Equals($dstRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return (& $failed 'cross-volume replacement move refused: TEMP/TMP must be on the destination volume')
    }

    try {
        if (-not (Test-Path -LiteralPath $SourcePath -PathType Leaf)) {
            return (& $failed 'replacement move failed: source file is missing')
        }

        $destDir = Split-Path -Parent $DestinationPath
        if ($destDir -and -not (Test-Path -LiteralPath $destDir -PathType Container)) {
            return (& $failed 'replacement move failed: destination directory is missing')
        }

        [System.IO.File]::Move($SourcePath, $DestinationPath, $true)
    } catch {
        return (& $failed ('replacement move failed: ' + $_.Exception.Message))
    }

    if (-not (Test-Path -LiteralPath $DestinationPath -PathType Leaf)) {
        return (& $failed 'replacement move failed: destination is missing after move')
    }

    return [pscustomobject]@{
        Succeeded = $true
        Error = $null
        DestinationUnchanged = $false
    }
}
