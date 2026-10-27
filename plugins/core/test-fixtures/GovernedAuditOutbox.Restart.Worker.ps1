# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-008/009/069: disposable process worker for real abrupt outbox restarts.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode,
    [Parameter(Mandatory)][string]$RootDirectory,
    [Parameter(Mandatory)][string]$TracePath,
    [Parameter(Mandatory)][string]$ResultPath,
    [ValidateSet('Enqueue','Flush')][string]$Operation,
    [string]$CrashPhase='',[string]$CrashKind='',[string]$Mutation=''
)
$ErrorActionPreference='Stop'
if(-not $PSCmdlet.ShouldProcess($RootDirectory,'Run one fixture operation, emit dependency effects, optionally terminate process75')){return}
Import-Module powershell-yaml -ErrorAction Stop
Import-Module PowerShell.MCP -ErrorAction Stop
if($Mode -ceq 'Mock'){. (Join-Path $PSScriptRoot 'GovernedAuditOutbox.Durable.Model.ps1')}
else{. (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')}
$context=@{workspacePath='/fixture/restart';agentIdentity='Codex';mcpSessionId='session-restart';hostSessionKey='host-restart'}
$inputData=@{requestId='req-restart';kind='dialog';payloadJson=(ConvertTo-Json -InputObject @{text="retained `r`n payload";unicode=([string][char]0x96EA)} -Compress)}
$fixtureClock=@{now=1000L}
$fixtureTrace=$TracePath;$terminationPhase=$CrashPhase;$terminationKind=$CrashKind
function Add-WorkerEffect {
    param([hashtable]$Value)
    Add-LinesToFile -Path $fixtureTrace -Content (ConvertTo-Json -InputObject $Value -Depth 30 -Compress)|Out-Null
}
$transport=@{
    submit={
        param($AuditRecord)
        Add-WorkerEffect @{effect='submit';event=$AuditRecord}
        $reply=@{code='persisted';revision=7}
        foreach($key in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){$reply[$key]=$AuditRecord[$key]}
        return $reply
    }
    read={param($AuditRecord) Add-WorkerEffect @{effect='read';event=$AuditRecord};return @{code='lost'}}
}
$observer={
    param($Phase,$Path)
    $kind=if([IO.Path]::GetFileName($Path) -ceq 'sequence.yaml'){'sequence'}else{'capture'}
    $document=$null
    if($Path.EndsWith('.event.yaml')){
        $readPath=$Path
        if($Phase -ceq 'staged'){
            $readPath=@([IO.Directory]::EnumerateFiles([IO.Path]::GetDirectoryName($Path),([IO.Path]::GetFileName($Path)+'.*.tmp')))[0]
        }elseif($Phase -ceq 'retiring'){
            $readPath=Join-Path (Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Path))) 'pending') ([IO.Path]::GetFileName($Path))
        }
        $document=ConvertFrom-Yaml ([IO.File]::ReadAllText($readPath))
        if($document.receipt){$kind='receipt'}elseif($document.attempts -gt 0){$kind='attempt'}
    }
    Add-WorkerEffect @{effect='publication';phase=$Phase;kind=$kind;path=$Path;document=$document}
    if($Phase -ceq $terminationPhase -and $kind -ceq $terminationKind){
        Add-WorkerEffect @{effect='termination';phase=$Phase;kind=$kind}
        [Environment]::Exit(75)
    }
}
$arguments=@{RootDirectory=$RootDirectory;Context=$context;Operation=$Operation;InputObject=$inputData;Transport=$transport;Clock={$fixtureClock.now};Sleep={param($Milliseconds) Add-WorkerEffect @{effect='sleep';milliseconds=$Milliseconds};$fixtureClock.now+=$Milliseconds};PublicationObserver=$observer}
$result=if($Mode -ceq 'Mock'){Invoke-MockDurableAuditOutbox @arguments -Mutation $Mutation}else{Invoke-McpGovernedAuditOutbox @arguments}
Add-LinesToFile -Path $ResultPath -Content (ConvertTo-Json -InputObject $result -Depth 40)|Out-Null
if($result.code -ceq 'lost'){exit 1}
