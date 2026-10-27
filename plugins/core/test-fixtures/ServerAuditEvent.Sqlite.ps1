#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode='Mock',
    [ValidateSet('Event','Upgrade')][string]$ContractSet='Event',
    [Parameter(Mandatory)][string]$ReceiptPath,
    [Parameter(Mandatory)][string]$ArtifactRoot,
    [string]$AssemblyDirectory,
    [string]$MockReceiptPath
)
$ErrorActionPreference='Stop'
$artifactDirectory=[IO.Path]::GetFullPath($ArtifactRoot)
$isUpgrade=$ContractSet -ceq 'Upgrade'
$runner=Join-Path $PSScriptRoot $(if($isUpgrade){'ServerAuditUpgrade.Contract.ps1'}else{'ServerAuditEvent.Contract.ps1'})
if(Test-Path -LiteralPath $ReceiptPath){throw 'Chronological receipt already exists'}
if($Mode -ceq 'Production'){
    if(-not $AssemblyDirectory -or -not $MockReceiptPath){throw 'Compiled real adapter and independent mock gate required'}
    $gate=Get-Content -LiteralPath $MockReceiptPath -Raw | ConvertFrom-Json -DateKind String
    if($gate.mode -cne 'Mock' -or $gate.failed -ne 0 -or $gate.skipped -ne 0){throw 'Current independent mock gate required'}
    if($isUpgrade){
        if($gate.cases -ne 3 -or $gate.negativeControls -ne 13 -or $gate.sourceSha256 -cne (Get-FileHash $runner).Hash){throw 'Exact3/13 upgrade gate required'}
    }else{
        if($gate.cases -ne 28 -or $gate.controls -ne 81){throw 'Exact28/81 event gate required'}
        foreach($pin in $gate.sourceHashes){if((Get-FileHash -LiteralPath $pin.Path).Hash -cne $pin.Hash){throw 'Frozen mock assertion/model source changed'}}
    }
}
Write-Output ('Counted effects: SAME '+$ContractSet+' scenario inputs/assertions; retained fixture databases and one complete chronological receipt. No service/network/operator data or build.')
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,('Run '+$Mode+' frozen audit scenario interactions'))){return}
if($Mode -ceq 'Mock'){
    & $runner -Mode Mock -ReceiptPath $ReceiptPath
    return
}
. (Join-Path $PSScriptRoot 'ServerAuditFramework.ps1')
$assembly=Get-ServerAuditFixtureAssembly -AssemblyDirectory $AssemblyDirectory
$type=$assembly.GetType('QBrainAi.Support.Mcp.Tests.Services.ServerAuditSqliteFixture',$true)
$create=$type.GetMethod($(if($isUpgrade){'CreateForUpgrade'}else{'Create'}))
$factory={
    param([string]$Fault)
    if($Fault){throw 'Mock-only corruption is not a real adapter'}
    $instance=$create.Invoke($null,[object[]]@([string]$artifactDirectory))
    $invoke={
        param([string]$Operation,[Collections.IDictionary]$InputObject)
        $inputJson=ConvertTo-Json -InputObject $InputObject -Depth 100 -Compress
        $nativeJson=if($isUpgrade){$instance.InvokeUpgrade($Operation,$inputJson)}else{$instance.Invoke($Operation,$inputJson)}
        return ConvertFrom-Json -InputObject $nativeJson -AsHashtable -NoEnumerate -DateKind String
    }.GetNewClosure()
    return @{invoke=$invoke}
}.GetNewClosure()
& $runner -Mode Production -ReceiptPath $ReceiptPath -ProviderFactory $factory
