# Claude MCP Hook Validation Skill Export

Source skill directory:
`F:\GitHub\mcpserver-claude-code-plugin\skills\claude-hook-validation`

This export contains the skill entrypoint and its supporting validation script.

## `SKILL.md`

````markdown
---
name: Claude MCP Hook Validation
description: Use when an MCP workspace marker tells Claude Code to validate active MCP hook enforcement. Verifies active Claude settings and installs missing MCP hooks through the claude-hook-wiring skill.
---

## Purpose

This skill is triggered by the workspace marker file during Claude Code plugin bootstrap. It verifies that the active Claude settings include the MCP hook chain required for per-request enforcement.

Use this skill when:

- `AGENTS-README-FIRST.yaml` or another marker file says to validate Claude hook wiring.
- Claude Code has the McpServer plugin installed but does not reliably open MCP turns or run stop gates.
- Active Claude settings may not include MCP `UserPromptSubmit`, `Stop`, or `PostToolUse` hooks.

## Action

Run:

```powershell
pwsh -NoLogo -NoProfile -NonInteractive -File "$env:CLAUDE_PLUGIN_ROOT/skills/claude-hook-validation/scripts/validate-claude-mcp-hooks.ps1"
```

If `CLAUDE_PLUGIN_ROOT` is unavailable, pass the plugin root explicitly:

```powershell
pwsh -NoLogo -NoProfile -NonInteractive -File "F:/GitHub/mcpserver-claude-code-plugin/skills/claude-hook-validation/scripts/validate-claude-mcp-hooks.ps1" -PluginRoot "F:/GitHub/mcpserver-claude-code-plugin"
```

The validation script checks active Claude `settings.json` for:

- `UserPromptSubmit` with `user-prompt-submit.ps1`
- `Stop` with `stop-gate.ps1`
- `PostToolUse` with `code-verify.ps1`

When any required hook is missing, the script invokes the `claude-hook-wiring` skill installer script, `install-claude-mcp-hooks.ps1`, to merge MCP hooks from `hooks/hooks.json` into active Claude settings. Existing hooks must remain intact.

## Result Handling

- `status: valid`: hooks are already active; continue normal MCP work.
- `status: installed`: hooks were merged into active Claude settings; restart Claude Code before relying on automatic hook enforcement.
- `status: missing`: hooks are absent and installation was disabled; report the missing hooks and do not claim enforcement is active.

After `status: installed`, restart Claude Code. If the current task must continue before restart, manually follow the MCP session-log workflow for the current request and treat automatic enforcement as pending until the restart.
````

## `scripts/validate-claude-mcp-hooks.ps1`

```powershell
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$SettingsPath = (Join-Path $HOME '.claude/settings.json'),
    [string]$PluginRoot = $(if ($env:CLAUDE_PLUGIN_ROOT) { $env:CLAUDE_PLUGIN_ROOT } else { (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).ProviderPath }),
    [switch]$CheckOnly,
    [switch]$NoBackup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-JsonObject {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{}
    }

    $content = [System.IO.File]::ReadAllText($Path)
    if ([string]::IsNullOrWhiteSpace($content)) {
        return [ordered]@{}
    }

    return $content | ConvertFrom-Json -AsHashtable -Depth 100
}

function Test-HookCommand {
    param(
        [Parameter(Mandatory)][System.Collections.IDictionary]$Settings,
        [Parameter(Mandatory)][string]$EventName,
        [Parameter(Mandatory)][string]$CommandFragment
    )

    if (-not $Settings.Contains('hooks') -or $Settings['hooks'] -isnot [System.Collections.IDictionary]) {
        return $false
    }

    $hooks = $Settings['hooks']
    if (-not $hooks.Contains($EventName)) {
        return $false
    }

    foreach ($group in @($hooks[$EventName])) {
        if ($group -isnot [System.Collections.IDictionary] -or -not $group.Contains('hooks')) {
            continue
        }

        foreach ($hook in @($group['hooks'])) {
            if ($hook -isnot [System.Collections.IDictionary] -or -not $hook.Contains('command')) {
                continue
            }

            if ([string]$hook['command'] -like "*$CommandFragment*") {
                return $true
            }
        }
    }

    return $false
}

function Get-MissingHookCommands {
    param([Parameter(Mandatory)][System.Collections.IDictionary]$Settings)

    $required = @(
        @{ EventName = 'UserPromptSubmit'; Fragment = 'user-prompt-submit.ps1' },
        @{ EventName = 'Stop'; Fragment = 'stop-gate.ps1' },
        @{ EventName = 'PostToolUse'; Fragment = 'code-verify.ps1' }
    )

    $missing = @()
    foreach ($requirement in $required) {
        if (-not (Test-HookCommand -Settings $Settings -EventName $requirement.EventName -CommandFragment $requirement.Fragment)) {
            $missing += "$($requirement.EventName):$($requirement.Fragment)"
        }
    }

    return $missing
}

$pluginRootFull = (Resolve-Path -LiteralPath $PluginRoot).ProviderPath
$installerPath = Join-Path $pluginRootFull 'skills/claude-hook-wiring/scripts/install-claude-mcp-hooks.ps1'
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "Claude hook wiring installer not found: $installerPath"
}

$settingsBefore = Read-JsonObject -Path $SettingsPath
$missingBefore = @(Get-MissingHookCommands -Settings $settingsBefore)
if ($missingBefore.Count -eq 0) {
    [ordered]@{
        status = 'valid'
        settingsPath = if (Test-Path -LiteralPath $SettingsPath -PathType Leaf) { (Resolve-Path -LiteralPath $SettingsPath).ProviderPath } else { $SettingsPath }
        missingHooks = @()
        restartRequired = $false
    } | ConvertTo-Json -Depth 10 -Compress
    return
}

if ($CheckOnly) {
    [ordered]@{
        status = 'missing'
        settingsPath = $SettingsPath
        missingHooks = $missingBefore
        restartRequired = $false
    } | ConvertTo-Json -Depth 10 -Compress
    return
}

$installerArgs = @(
    '-NoLogo',
    '-NoProfile',
    '-NonInteractive',
    '-File',
    $installerPath,
    '-SettingsPath',
    $SettingsPath,
    '-PluginRoot',
    $pluginRootFull
)
if ($NoBackup) {
    $installerArgs += '-NoBackup'
}

& pwsh @installerArgs | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Claude hook wiring installer failed with exit code $LASTEXITCODE."
}

$settingsAfter = Read-JsonObject -Path $SettingsPath
$missingAfter = @(Get-MissingHookCommands -Settings $settingsAfter)
$status = if ($missingAfter.Count -eq 0) { 'installed' } else { 'missing' }

[ordered]@{
    status = $status
    settingsPath = (Resolve-Path -LiteralPath $SettingsPath).ProviderPath
    missingHooks = $missingAfter
    restartRequired = ($status -eq 'installed')
} | ConvertTo-Json -Depth 10 -Compress
```
