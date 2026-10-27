# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-008/009/069: Same outbox lock/link scenarios for Mock and Production.
.DESCRIPTION
Real disposable storage effects are observed. The independent test-only guard
provider wraps the frozen independent state model; production uses the same seam.
No hard-coded provider summaries, production mock imports or network calls.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,"Run $Mode outbox lock/link scenarios and write one receipt")){return}
Import-Module powershell-yaml -ErrorAction Stop
. (Join-Path $PSScriptRoot 'GovernedAuditOutbox.Model.ps1')
. (Join-Path $PSScriptRoot 'GovernedAuditOutbox.Guard.Model.ps1')
$production=Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1'
if($Mode -ceq 'Production'){. $production}
$base=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../.mcpServer'))) ('g-'+[guid]::NewGuid().ToString('N').Substring(0,8))
$context=@{workspacePath='/fixture/guards';agentIdentity='Codex';mcpSessionId='session-fixture';hostSessionKey='host-fixture'}
$inputData=@{requestId='req-one';kind='dialog';payloadJson=(ConvertTo-Json -InputObject @{text='immutable fixture payload'} -Compress)}
$identity=ConvertTo-Json -InputObject @($context.workspacePath,$context.agentIdentity,$context.mcpSessionId,$context.hostSessionKey) -Compress
$key=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($identity))).ToLowerInvariant()
function Invoke-GuardProvider {
    param([string]$Root,[string]$Mutation='')
    $arguments=@{RootDirectory=$Root;Context=$context;Operation='Enqueue';InputObject=$inputData;PublicationObserver={param($Phase,$Path) [void]$Phase;[void]$Path}}
    if($Mode -ceq 'Mock' -or $Mutation){return Invoke-MockGuardedOutbox @arguments -Mutation $Mutation}
    return Invoke-McpGovernedAuditOutbox @arguments
}
function Get-GuardSnapshot {
    param([string]$Path)
    return @(Get-ChildItem -LiteralPath $Path -Recurse -Force -File | Sort-Object FullName | ForEach-Object {
        if($_.LinkTarget){@{path=[IO.Path]::GetRelativePath($Path,$_.FullName);link=$_.LinkTarget}}
        else{@{path=[IO.Path]::GetRelativePath($Path,$_.FullName);hash=(Get-FileHash $_.FullName).Hash}}
    })|ConvertTo-Json -Depth 20 -Compress
}
function Invoke-GuardCase {
    param([string]$Name,[string]$Mutation='')
    $directory=Join-Path $base ($Name+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
    $retained=Join-Path $directory 'retained'
    $root=Join-Path $directory 'entry'
    $seed=Invoke-GuardProvider $retained
    if($seed.code -cne 'queued'){throw 'Independent scenario setup failed'}
    $targetContext=Join-Path $retained $key
    $contextDir=Join-Path $root $key
    [void][IO.Directory]::CreateDirectory($directory)
    $held=$null
    if($Name -ceq 'root-link'){
        $null=New-Item -ItemType SymbolicLink -Path $root -Target $retained
    }elseif($Name -ceq 'context-link'){
        [void][IO.Directory]::CreateDirectory($root)
        $null=New-Item -ItemType SymbolicLink -Path $contextDir -Target $targetContext
    }else{
        [void][IO.Directory]::CreateDirectory($contextDir)
        foreach($folder in @('pending','archive','rejected','quarantine')){[void][IO.Directory]::CreateDirectory((Join-Path $contextDir $folder))}
        switch($Name){
            'legacy-lock' {[IO.File]::WriteAllText((Join-Path $contextDir '.lock'),'retained legacy owner')}
            'held-lock' {$held=[IO.File]::Open((Join-Path $contextDir '.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)}
            'sequence-link' {$null=New-Item -ItemType SymbolicLink -Path (Join-Path $contextDir 'sequence.yaml') -Target (Join-Path $targetContext 'sequence.yaml')}
            'sequence-dangling' {$null=New-Item -ItemType SymbolicLink -Path (Join-Path $contextDir 'sequence.yaml') -Target (Join-Path $directory 'absent.yaml')}
            'alias-link' {
                $target=Join-Path $targetContext 'applied-aliases.yaml'
                [IO.File]::WriteAllText($target,(ConvertTo-Yaml -Data @{version=1;aliases=@()}))
                $null=New-Item -ItemType SymbolicLink -Path (Join-Path $contextDir 'applied-aliases.yaml') -Target $target
            }
            'alias-dangling' {$null=New-Item -ItemType SymbolicLink -Path (Join-Path $contextDir 'applied-aliases.yaml') -Target (Join-Path $directory 'absent.yaml')}
            'event-link' {
                Copy-Item (Join-Path $targetContext 'sequence.yaml') (Join-Path $contextDir 'sequence.yaml')
                $file=@(Get-ChildItem (Join-Path $targetContext 'pending') -File)[0]
                $null=[IO.File]::CreateSymbolicLink((Join-Path (Join-Path $contextDir 'pending') $file.Name),$file.FullName)
            }
            'empty-lock' {[IO.File]::WriteAllText((Join-Path $contextDir '.lock'),'')}
            default {throw 'Unknown fixture scenario'}
        }
    }
    $before=Get-GuardSnapshot $retained
    $lockBefore=if($Name -ceq 'legacy-lock'){[IO.File]::ReadAllText((Join-Path $contextDir '.lock'))}else{''}
    try{
        $result=Invoke-GuardProvider $root $Mutation
        if($Name -ceq 'empty-lock'){
            if($result.code -cne 'queued' -or $result.event.order -ne 1){throw 'Empty lock was not reusable'}
            $second=Invoke-GuardProvider $root $Mutation
            if($second.code -cne 'queued' -or $second.event.order -ne 2){throw 'Released lock lost sequencing'}
        }else{
            if($result.code -cne 'lost'){throw 'Unsafe dependency did not refuse capture'}
            if((Get-GuardSnapshot $retained) -cne $before){throw 'Refusal changed retained target bytes'}
            if($Name -ceq 'legacy-lock' -and [IO.File]::ReadAllText((Join-Path $contextDir '.lock')) -cne $lockBefore){throw 'Legacy lock evidence changed'}
        }
        return @{case=$Name;result=$result;retainedBefore=$before;retainedAfter=(Get-GuardSnapshot $retained);root=$root}
    }finally{if($held){$held.Dispose()}}
}
$cases=@('legacy-lock','held-lock','root-link','context-link','sequence-link','sequence-dangling','alias-link','alias-dangling','event-link','empty-lock')
$results=@(foreach($case in $cases){Invoke-GuardCase $case})
$controls=@(foreach($control in @(@{case='legacy-lock';mutation='omit-legacy'},@{case='held-lock';mutation='omit-lock'},@{case='root-link';mutation='omit-links'})){
    $rejection=''
    try{$null=Invoke-GuardCase $control.case $control.mutation}catch{$rejection=$_.Exception.Message}
    if(-not $rejection){throw 'Wrong guard effect escaped actual scenario assertions'}
    @{case=$control.case;mutation=$control.mutation;rejected=$true;assertion=$rejection}
})
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;cases=$results.Count;negativeControls=$controls.Count;failed=0;skipped=0;results=$results;controls=$controls;fixtureSha256=(Get-FileHash $PSCommandPath).Hash;modelSha256=(Get-FileHash (Join-Path $PSScriptRoot 'GovernedAuditOutbox.Guard.Model.ps1')).Hash;stateModelSha256=(Get-FileHash (Join-Path $PSScriptRoot 'GovernedAuditOutbox.Model.ps1')).Hash;productionSha256=$(if($Mode -ceq 'Production'){(Get-FileHash $production).Hash}else{$null});scope='Lock/link refusal only; actual crash/restart and full cumulative gates remain required';productComplete=$false}
$receiptJson=$receipt|ConvertTo-Json -Depth 40
Add-LinesToFile -Path $ReceiptPath -Content $receiptJson | Out-Null
[pscustomobject]$receipt|Select-Object mode,cases,negativeControls,failed,skipped|ConvertTo-Json
