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
            (Get-Location).Path,
            $env:MCP_WORKSPACE_START_DIR,
            $env:CLAUDE_PROJECT_DIR,
            $env:CODEX_CWD
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
