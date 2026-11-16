#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Arm', 'Check')]
    [string]$Mode,

    [string]$HostName = $(if ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST } else { 'codex' }),

    [string]$WorkspacePath = $(if ($env:MCP_WORKSPACE_PATH) { $env:MCP_WORKSPACE_PATH } else { (Get-Location).ProviderPath }),

    [object]$InputObject
)

<#
.SYNOPSIS
    Standalone hook entrypoint for hosts whose plugin manifest cannot register hooks.
.DESCRIPTION
    FR-MCP-190 and TR-MCP-PROFILE-001 keep host-specific profile reload behavior in
    the plugin even when the host requires a user hook registration. Arm handles a
    compact SessionStart and Check handles UserPromptSubmit. The wrapper starts no
    MCP session and mutates no session-log state.

    Change log:
    2026-11-14 - Added Codex-compatible compact and threshold entrypoints for TEST-MCP-238.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir 'profile-reload.ps1')
. (Join-Path $scriptDir 'resolve-cache-dir.ps1')

$payload = if ($null -ne $InputObject) {
    if ($InputObject -is [string]) { [string]$InputObject } else { $InputObject | ConvertTo-Json -Depth 30 -Compress }
} elseif ([Console]::IsInputRedirected) {
    [Console]::In.ReadToEnd()
} else {
    ''
}

$startPath = $WorkspacePath
if (-not (Test-Path -LiteralPath $startPath -PathType Container)) {
    $payloadObject = Get-McpProfileReloadPayloadObject -Payload $payload
    $payloadPath = Get-McpProfileReloadPropertyValue -InputObject $payloadObject -Names @('cwd', 'workspace_path', 'workspacePath')
    if ($payloadPath -and (Test-Path -LiteralPath ([string]$payloadPath) -PathType Container)) {
        $startPath = [string]$payloadPath
    }
}
$cacheDir = Resolve-McpCacheDir -StartPath $startPath

if ($Mode -eq 'Arm') {
    $result = Initialize-McpProfileReloadCompaction -CacheDir $cacheDir -HostName $HostName -InjectionAvailable -Source 'session-start-compact'
    [ordered]@{
        hookSpecificOutput = [ordered]@{
            hookEventName = 'SessionStart'
            additionalContext = [string]$result.AdditionalContext
        }
    } | ConvertTo-Json -Depth 10 -Compress
    return
}

$check = Invoke-McpProfileReloadCheck -CacheDir $cacheDir -HostName $HostName -Payload $payload
if ([string]::IsNullOrWhiteSpace([string]$check.AdditionalContext)) {
    [ordered]@{} | ConvertTo-Json -Compress
    return
}
[ordered]@{
    hookSpecificOutput = [ordered]@{
        hookEventName = 'UserPromptSubmit'
        additionalContext = [string]$check.AdditionalContext
    }
} | ConvertTo-Json -Depth 10 -Compress
