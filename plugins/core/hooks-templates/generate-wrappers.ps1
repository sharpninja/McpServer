#Requires -Version 7.0
<#
.SYNOPSIS
    Generates host hook wrappers for the shared PowerShell plugin runtime.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('claude-code', 'cowork', 'claude-cowork', 'codex', 'copilot', 'grok')]
    [string]$HostName,

    [Parameter(Mandatory)][string]$PluginRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$templateDir = $PSScriptRoot
$templatePath = Join-Path $templateDir 'wrapper.ps1.template'
if (-not (Test-Path -LiteralPath $templatePath)) {
    throw "Wrapper template was not found: $templatePath"
}

switch ($HostName) {
    'codex' {
        $depth = '..'
        $wrapperDir = Join-Path $PluginRoot 'lib'
        $hooks = @(
            @{ Name = 'session-start'; Mode = 'flat' },
            @{ Name = 'user-prompt-submit'; Mode = 'scoped' },
            @{ Name = 'stop-gate'; Mode = 'scoped' },
            @{ Name = 'code-verify'; Mode = 'scoped' },
            @{ Name = 'subagent-import'; Mode = 'flat' }
        )
    }
    default {
        $depth = '../..'
        $wrapperDir = Join-Path $PluginRoot 'hooks\scripts'
        $hooks = @(
            @{ Name = 'session-start'; Mode = 'flat' },
            @{ Name = 'session-end'; Mode = 'flat' },
            @{ Name = 'pre-compact'; Mode = 'flat' },
            @{ Name = 'post-compact'; Mode = 'flat' },
            @{ Name = 'user-prompt-submit'; Mode = 'scoped' },
            @{ Name = 'stop-gate'; Mode = 'scoped' },
            @{ Name = 'code-verify'; Mode = 'scoped' },
            @{ Name = 'plan-approved'; Mode = 'flat' },
            @{ Name = 'plan-modified'; Mode = 'flat' },
            @{ Name = 'cache-flush'; Mode = 'flat' },
            @{ Name = 'health-check'; Mode = 'flat' },
            @{ Name = 'subagent-import'; Mode = 'flat' }
        )
    }
}

if (-not (Test-Path -LiteralPath $wrapperDir)) {
    [void][System.IO.Directory]::CreateDirectory($wrapperDir)
}

Get-ChildItem -LiteralPath $wrapperDir -Filter '*.sh' -File -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $wrapperDir -Filter '*.bash' -File -ErrorAction SilentlyContinue | Remove-Item -Force

$template = [System.IO.File]::ReadAllText($templatePath)
foreach ($hook in $hooks) {
    $content = $template.
        Replace('__HOOK_NAME__', $hook.Name).
        Replace('__HOST__', $HostName).
        Replace('__DEPTH__', $depth).
        Replace('__CACHE_MODE__', $hook.Mode)

    $out = Join-Path $wrapperDir "$($hook.Name).ps1"
    [System.IO.File]::WriteAllText($out, $content, [System.Text.UTF8Encoding]::new($false))
}

$oldEnv = Join-Path $PluginRoot 'lib\plugin-env.sh'
if (Test-Path -LiteralPath $oldEnv) {
    Remove-Item -LiteralPath $oldEnv -Force
}

if ($HostName -ne 'codex') {
    $hooksRoot = Join-Path $PluginRoot 'hooks'
    if (-not (Test-Path -LiteralPath $hooksRoot)) {
        [void][System.IO.Directory]::CreateDirectory($hooksRoot)
    }

    $rootExpression = switch ($HostName) {
        'grok' { '${GROK_PLUGIN_ROOT:-${PLUGIN_ROOT:-$CLAUDE_PLUGIN_ROOT}}' }
        'copilot' { '${COPILOT_PLUGIN_ROOT:-${PLUGIN_ROOT:-$CLAUDE_PLUGIN_ROOT}}' }
        'cowork' { '${COWORK_PLUGIN_ROOT:-${CLAUDE_PLUGIN_ROOT}}' }
        'claude-cowork' { '${COWORK_PLUGIN_ROOT:-${CLAUDE_PLUGIN_ROOT}}' }
        default { '${CLAUDE_PLUGIN_ROOT}' }
    }

    $hooksJson = [System.IO.File]::ReadAllText((Join-Path $templateDir 'hooks.claude-code.json')).
        Replace('${CLAUDE_PLUGIN_ROOT}', $rootExpression)
    $hooksPath = Join-Path $hooksRoot 'hooks.json'
    [System.IO.File]::WriteAllText($hooksPath, $hooksJson, [System.Text.UTF8Encoding]::new($false))
    [void]($hooksJson | ConvertFrom-Json -Depth 20 -ErrorAction Stop)
}

Write-Output "generated $HostName PowerShell wrappers in $wrapperDir"
