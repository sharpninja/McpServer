#Requires -Version 7.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$HooksPath,
    [Parameter(Mandatory)][string]$PluginHookPath,
    [Parameter(Mandatory)][string]$PowerShellExecutable
)

<#
.SYNOPSIS
    Configures Codex to use the plugin-owned profile reload hook.
.DESCRIPTION
    Replaces only profile reload registrations in the complete hooks document.
    All unrelated hook registrations are preserved. The original document is
    copied to a timestamped backup and hash-verified before the mutation is
    written.

    Change log:
    2026-11-14 - Added object-first Codex hook registration for FR-MCP-190,
    TR-MCP-PROFILE-001, and TEST-MCP-238.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Select-NonProfileReloadRegistration {
    param([Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Registrations)

    $preserved = @()
    foreach ($registration in $Registrations) {
        $remainingHooks = @($registration.hooks | Where-Object {
                -not ($_.type -eq 'command' -and $_.command -match 'profile-reload(?:-hook)?\.ps1')
            })
        if ($remainingHooks.Count -eq 0) {
            continue
        }

        $registration.hooks = $remainingHooks
        $preserved += $registration
    }

    return $preserved
}

$resolvedHooksPath = [System.IO.Path]::GetFullPath($HooksPath)
$resolvedPluginHookPath = [System.IO.Path]::GetFullPath($PluginHookPath)
if (-not (Test-Path -LiteralPath $resolvedHooksPath -PathType Leaf)) {
    throw "Codex hooks file was not found: $resolvedHooksPath"
}
if (-not (Test-Path -LiteralPath $resolvedPluginHookPath -PathType Leaf)) {
    throw "Plugin profile reload hook was not found: $resolvedPluginHookPath"
}

$document = Get-Content -Raw -LiteralPath $resolvedHooksPath | ConvertFrom-Json -Depth 100
if (-not $document.PSObject.Properties['hooks']) {
    $document | Add-Member -NotePropertyName hooks -NotePropertyValue ([pscustomobject]@{})
}
if (-not $document.hooks.PSObject.Properties['SessionStart']) {
    $document.hooks | Add-Member -NotePropertyName SessionStart -NotePropertyValue @()
}
if (-not $document.hooks.PSObject.Properties['UserPromptSubmit']) {
    $document.hooks | Add-Member -NotePropertyName UserPromptSubmit -NotePropertyValue @()
}

$commandPluginHookPath = $resolvedPluginHookPath.Replace('\', '/')
$armCommand = "$PowerShellExecutable -NoLogo -NoProfile -NonInteractive -File '$commandPluginHookPath' -Mode Arm -HostName codex"
$checkCommand = "$PowerShellExecutable -NoLogo -NoProfile -NonInteractive -File '$commandPluginHookPath' -Mode Check -HostName codex"
$sessionStart = @(Select-NonProfileReloadRegistration -Registrations @($document.hooks.SessionStart))
$userPromptSubmit = @(Select-NonProfileReloadRegistration -Registrations @($document.hooks.UserPromptSubmit))
$sessionStart = @(
    [pscustomobject]@{
        matcher = 'compact'
        hooks = @([pscustomobject]@{ type = 'command'; command = $armCommand; timeout = 15 })
    }
) + $sessionStart
$userPromptSubmit = @(
    [pscustomobject]@{
        hooks = @([pscustomobject]@{ type = 'command'; command = $checkCommand; timeout = 30 })
    }
) + $userPromptSubmit
$document.hooks.SessionStart = $sessionStart
$document.hooks.UserPromptSubmit = $userPromptSubmit

$backupPath = "$resolvedHooksPath.pre-plugin-profile-reload.$([DateTimeOffset]::Now.ToString('yyyyMMddTHHmmsszzz').Replace(':', '')).bak"
if ($PSCmdlet.ShouldProcess($resolvedHooksPath, 'Configure plugin-owned Codex profile reload hooks')) {
    Copy-Item -LiteralPath $resolvedHooksPath -Destination $backupPath
    $sourceHash = (Get-FileHash -LiteralPath $resolvedHooksPath -Algorithm SHA256).Hash
    $backupHash = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash
    if ($sourceHash -ne $backupHash) {
        throw "Codex hooks backup hash verification failed: $backupPath"
    }

    $json = $document | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($resolvedHooksPath, "$json$([Environment]::NewLine)", [System.Text.UTF8Encoding]::new($false))

    [pscustomobject]@{
        HooksPath = $resolvedHooksPath
        BackupPath = $backupPath
        ArmCommand = $armCommand
        CheckCommand = $checkCommand
        BackupHashVerified = $true
    }
}
