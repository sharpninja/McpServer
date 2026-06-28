<#
.SYNOPSIS
    Workspace-aware cache path resolver for the PowerShell plugin runtime.
.DESCRIPTION
    Cache state belongs to the workspace the marker file lives in, not to
    the plugin install directory. This helper returns the correct cache dir.

    Precedence:
      1. $env:MCP_CACHE_DIR_OVERRIDE    explicit override.
      2. $env:PLUGIN_ROOT_OVERRIDE/cache legacy test hook.
      3. workspace env/cache            $env:MCPSERVER_WORKSPACE_PATH or
                                        $env:MCP_WORKSPACE_PATH (host-neutral).
      4. <markerDir>/cache              workspace resolved by walking up for
                                        AGENTS-README-FIRST.yaml.
      5. $env:MCP_PLUGIN_ROOT/cache     last-resort fallback (legacy
                                        $env:CLAUDE_PLUGIN_ROOT honored).
#>

$script:ResolveCacheDirScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Resolve-McpCacheDir {
    [CmdletBinding()]
    param()

    if ($env:MCP_CACHE_DIR_OVERRIDE) {
        return $env:MCP_CACHE_DIR_OVERRIDE
    }

    if ($env:PLUGIN_ROOT_OVERRIDE) {
        return (Join-Path $env:PLUGIN_ROOT_OVERRIDE 'cache')
    }

    $configuredWorkspace = if ($env:MCPSERVER_WORKSPACE_PATH) {
        $env:MCPSERVER_WORKSPACE_PATH
    } elseif ($env:MCP_WORKSPACE_PATH) {
        $env:MCP_WORKSPACE_PATH
    } else {
        $null
    }

    if ($configuredWorkspace -and (Test-Path -LiteralPath $configuredWorkspace -PathType Container)) {
        return (Join-Path $configuredWorkspace 'cache')
    }

    $startDir = if ($env:MCP_WORKSPACE_START_DIR) {
        $env:MCP_WORKSPACE_START_DIR
    } elseif ($env:CLAUDE_PROJECT_DIR) {
        $env:CLAUDE_PROJECT_DIR
    } else {
        (Get-Location).Path
    }

    if (-not (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue)) {
        $resolver = Join-Path $script:ResolveCacheDirScriptDir 'marker-resolver.ps1'
        if (Test-Path $resolver) {
            . $resolver
        }
    }

    if (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue) {
        try {
            $markerFile = Find-MarkerFile -StartDir $startDir
            if ($markerFile) {
                return (Join-Path (Split-Path -Parent $markerFile) 'cache')
            }
        } catch {
            # fall through to plugin-root fallback
        }
    }

    $pluginRoot = if ($env:MCP_PLUGIN_ROOT) {
        $env:MCP_PLUGIN_ROOT
    } elseif ($env:CLAUDE_PLUGIN_ROOT) {
        $env:CLAUDE_PLUGIN_ROOT
    } else {
        Split-Path -Parent $script:ResolveCacheDirScriptDir
    }
    return (Join-Path $pluginRoot 'cache')
}
