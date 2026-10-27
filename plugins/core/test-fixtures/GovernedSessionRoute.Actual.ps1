#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-069: binds the same27route inputs/assertions to actual parser/dispatcher callbacks.
.DESCRIPTION
Test-only compiled fixture, separate from native composition and storage acceptance.
Uses the existing eager isolated assembly-loading seam with no PowerShell worker callbacks.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
 [Parameter(Mandatory)][string]$ReceiptPath,
 [Parameter(Mandatory)][string]$MockReceiptPath,
 [string]$AssemblyDirectory=(Join-Path $PSScriptRoot '../../../tests/QBrainAi.Repl.Core.Tests/bin/Debug/net10.0')
)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
$mock=Get-Content $MockReceiptPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
if($mock.mode -cne 'Mock' -or $mock.cases -ne 31 -or $mock.rejectingControls -ne 38 -or $mock.failed -or $mock.skipped){throw 'Exact independent31/38full-field mock prerequisite required'}
foreach($source in $mock.sourceHashes){if((Get-FileHash $source.Path).Hash -cne $source.Hash){throw 'Mock source pin changed'}}
Write-Output 'Counted effects:31same cases against actual compiled parser/dispatcher with dependency captures and one receipt. No native/SQLite/fullscope acceptance.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Execute common route assertions against actual compiled routing')){return}
. (Join-Path $PSScriptRoot 'ServerAuditFramework.ps1')
$directory=[IO.Path]::GetFullPath($AssemblyDirectory)
$assemblyPath=Join-Path $directory 'QBrainAi.Repl.Core.Tests.dll'
if(-not (Test-Path $assemblyPath)){throw 'Compiled actual route fixture missing'}
$runtimeLines=@(& dotnet --list-runtimes)
if($LASTEXITCODE){throw 'Runtime inventory failed'}
$framework=Get-ServerAuditFramework -RuntimeLine $runtimeLines -RuntimeMajor ([Environment]::Version.Major)
$loadContext=[Runtime.Loader.AssemblyLoadContext]::new(('actual-route-fixture-'+[guid]::NewGuid().ToString('N')),$false)
$dependencies=Get-ServerAuditAssemblyFile -FrameworkFile @(Get-ChildItem $framework.FullName -Filter '*.dll') -ApplicationFile @(Get-ChildItem $directory -Filter '*.dll')
foreach($path in $dependencies){try{$null=$loadContext.LoadFromAssemblyPath($path)}catch [BadImageFormatException]{Write-Verbose ('Native dependency deferred: '+[IO.Path]::GetFileName($path))}}
$assembly=$loadContext.LoadFromAssemblyPath($assemblyPath)
$fixtureType=$assembly.GetType('QBrainAi.Repl.Core.Tests.GovernedSessionRouteFixture',$true)
$factory={
 param($capture,$governed)
 $constructor=$fixtureType.GetConstructor([type[]]@([string],[bool]))
 if(-not $constructor){throw 'Compiled fixture String/Boolean constructor missing'}
 $arguments=[object[]]::new(2)
 $arguments[0]=[string](ConvertTo-Json $capture -Depth 30 -Compress)
 $arguments[1]=[bool]$governed
 $instance=$constructor.Invoke($arguments)
 $invoke={param($method,$paramsJson) $task=$instance.InvokeAsync($method,$paramsJson);$task.GetAwaiter().GetResult()}.GetNewClosure()
 $inspect={ConvertFrom-Json $instance.InspectJson() -AsHashtable -DateKind String}.GetNewClosure()
 return @{Invoke=$invoke;Inspect=$inspect}
}.GetNewClosure()
& (Join-Path $PSScriptRoot 'GovernedSessionRoute.Contract.ps1') -Mode Production -ReceiptPath $ReceiptPath -ProviderFactory $factory
