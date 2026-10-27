#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<# .SYNOPSIS
 Nuke entry point for explicit pinned offline bundle inventory; no acquisition fallback.
 #>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$CatalogPath,[Parameter(Mandatory)][string]$RuntimeIdentifier)
$ErrorActionPreference='Stop'
Import-Module PowerShell.MCP -ErrorAction Stop
. (Join-Path $PSScriptRoot '../plugins/core/lib-ps/repl-runtime-bundle.ps1')
$catalog=Get-Content $CatalogPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
$entry=$catalog.rids[$RuntimeIdentifier]
if(-not $entry -or $entry.spec.rid -cne $RuntimeIdentifier){throw 'Exact platform catalog required; no runtime/module installation is permitted'}
if(-not $PSCmdlet.ShouldProcess($entry.outputDirectory,'Package exact pinned runtime/module/REPL inventory')){return}
$result=New-McpReplRuntimeBundle -Spec $entry.spec -PackageRequirements $entry.packageRequirements -SourceDirectory $entry.sourceDirectory -OutputDirectory $entry.outputDirectory
$result|ConvertTo-Json -Depth 20
if(-not $result.ok){throw ('Required offline bundle refused: '+$result.code)}

