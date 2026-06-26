<#
.SYNOPSIS
    plugin-env.ps1 - host knob defaults for the shared PowerShell runtime.
.DESCRIPTION
    Maps host-specific environment variables onto the neutral knob surface
    consumed by the core libraries. Dot-source after setting
    $env:MCP_PLUGIN_HOST (claude-code | cowork | codex | copilot | grok).
    Keep host defaults in this file so generated wrappers stay minimal.
#>

if ($env:MCP_PLUGIN_ENV_LOADED) { return }
$env:MCP_PLUGIN_ENV_LOADED = '1'

$script:PluginEnvScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$host_ = if ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST } else { 'claude-code' }
$env:MCP_PLUGIN_HOST = $host_

switch ($host_) {
    'claude-code' {
        $agent = 'ClaudeCode'; $model = 'claude'; $tag = 'claude-code'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:CLAUDE_PLUGIN_ROOT)
        $startChain = @($env:CLAUDE_PROJECT_DIR)
    }
    { $_ -in 'cowork', 'claude-cowork' } {
        $agent = 'ClaudeCowork'; $model = 'claude'; $tag = 'claude-cowork'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:CLAUDE_PLUGIN_ROOT)
        $startChain = @($env:COWORK_WORKSPACE_PATH, $env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH, $env:CLAUDE_COWORK_WORKSPACE_PATH, $env:CLAUDE_PROJECT_DIR)
    }
    'codex' {
        $agent = 'Codex'; $model = 'codex'; $tag = 'codex'; $outputMode = 'cli'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:CODEX_PLUGIN_ROOT)
        $startChain = @()
    }
    'copilot' {
        $agent = 'Copilot'; $model = 'copilot'; $tag = 'copilot'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:PLUGIN_ROOT, $env:CLAUDE_PLUGIN_ROOT)
        $startChain = @()
    }
    'grok' {
        $agent = 'GrokCode'; $model = 'grok'; $tag = 'grok'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:GROK_PLUGIN_ROOT, $env:PLUGIN_ROOT, $env:CLAUDE_PLUGIN_ROOT)
        $startChain = @($env:CLAUDE_PROJECT_DIR)
    }
    default {
        $agent = 'Codex'; $model = 'codex'; $tag = 'mcpserver'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT)
        $startChain = @()
    }
}

if (-not $env:PLUGIN_AGENT_DEFAULT) { $env:PLUGIN_AGENT_DEFAULT = $agent }
if (-not $env:PLUGIN_MODEL_DEFAULT) { $env:PLUGIN_MODEL_DEFAULT = $model }
if (-not $env:PLUGIN_TAG) { $env:PLUGIN_TAG = $tag }
if (-not $env:MCP_HOOK_OUTPUT_MODE) { $env:MCP_HOOK_OUTPUT_MODE = $outputMode }

if (-not $env:MCP_PLUGIN_ROOT) {
    $resolvedRoot = $rootChain | Where-Object { $_ } | Select-Object -First 1
    if (-not $resolvedRoot) { $resolvedRoot = Split-Path -Parent $script:PluginEnvScriptDir }
    $env:MCP_PLUGIN_ROOT = $resolvedRoot
}

if (-not $env:MCP_WORKSPACE_START_DIR) {
    $resolvedStart = $startChain | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Container) } | Select-Object -First 1
    if ($resolvedStart) { $env:MCP_WORKSPACE_START_DIR = $resolvedStart }
}

# Unified identity trio + model/title defaults.
if (-not $env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME = $env:PLUGIN_AGENT_DEFAULT }
if (-not $env:MCP_AGENT_ID) { $env:MCP_AGENT_ID = $env:PLUGIN_AGENT_DEFAULT }
if (-not $env:MCP_SESSION_AGENT) { $env:MCP_SESSION_AGENT = $env:PLUGIN_AGENT_DEFAULT }
if (-not $env:MCP_SESSION_MODEL) { $env:MCP_SESSION_MODEL = $env:PLUGIN_MODEL_DEFAULT }
if (-not $env:MCP_SESSION_TITLE) { $env:MCP_SESSION_TITLE = "$($env:PLUGIN_AGENT_DEFAULT) plugin session" }

# Turn recovery host identity.
if (-not $env:CT2R_SOURCE_TYPE) { $env:CT2R_SOURCE_TYPE = $env:PLUGIN_AGENT_DEFAULT }
if (-not $env:CT2R_MODEL) { $env:CT2R_MODEL = $env:PLUGIN_MODEL_DEFAULT }
if (-not $env:CT2R_TITLE) { $env:CT2R_TITLE = "$($env:PLUGIN_AGENT_DEFAULT) turn" }
if (-not $env:CT2R_TAGS) { $env:CT2R_TAGS = $env:PLUGIN_TAG }
