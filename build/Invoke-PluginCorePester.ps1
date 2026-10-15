# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TR-MCP-NUKE-012: runs Pester 5 on the given plugin core test paths with the configuration Invoke-Pester -CI applies (TestResult.Enabled and
Run.Exit), writing NUnit XML to -ResultPath. Started only by the Nuke target TestPluginCore, which resolves the paths first and then fails
on any failure or skip recorded in the result file.

.PARAMETER ResultPath
Where the NUnit XML result is written; its folder is created when missing.

.PARAMETER Path
The Pester test files or folders to run, already resolved by the target.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ResultPath,
    [Parameter(Mandatory, ValueFromRemainingArguments)][string[]]$Path
)
$ErrorActionPreference = 'Stop'
Import-Module Pester -MinimumVersion 5.0
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ResultPath) | Out-Null
$config = New-PesterConfiguration
$config.Run.Path = $Path
# -CI equals TestResult.Enabled plus Run.Exit; the configuration form also sets the result path the target reads.
$config.Run.Exit = $true
$config.TestResult.Enabled = $true
$config.TestResult.OutputFormat = 'NUnitXml'
$config.TestResult.OutputPath = $ResultPath
$config.Output.Verbosity = 'Detailed'
Invoke-Pester -Configuration $config
