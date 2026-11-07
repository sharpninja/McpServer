#Requires -Version 7.0
<#
.SYNOPSIS
Checks generated process-test fixtures before either script is executed.
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$ProducerPath,[Parameter(Mandatory)][string]$ServerPath)
$ErrorActionPreference='Stop'
foreach($path in @($ProducerPath,$ServerPath)) {
    $parseErrors=$null
    $null=[System.Management.Automation.Language.Parser]::ParseFile($path,[ref]$null,[ref]$parseErrors)
    if($parseErrors) { throw $parseErrors }
    $diagnostics=@(Invoke-ScriptAnalyzer -Path $path -Severity Warning,Error)
    if($diagnostics.Count) { throw ($diagnostics | Out-String) }
}
Write-Output 'PowerShell fixtures: parser and PSScriptAnalyzer passed.'
