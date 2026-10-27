# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-008/009/069: one disposable outbox operation with explicit process gates.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode,
    [Parameter(Mandatory)][string]$RootDirectory,[Parameter(Mandatory)][string]$TracePath,
    [Parameter(Mandatory)][string]$ResultPath,[ValidateSet('Enqueue','Flush')][string]$Operation,
    [string]$RequestId='req-concurrent',[string]$HostSessionKey='host-concurrent',
    [ValidateSet('None','Publication','Submit')][string]$Hold='None',[string]$GatePath='',
    [string]$Mutation='')
$ErrorActionPreference='Stop'
Write-Output 'Counted effects:1 fixture operation;actual storage/transport/publication trace;1result;optional explicit ready/release gate. No live server.'
if(-not $PSCmdlet.ShouldProcess($RootDirectory,'Execute one context-bound contention fixture operation')){return}
Import-Module powershell-yaml -ErrorAction Stop
Import-Module PowerShell.MCP -ErrorAction Stop
if($Mode -ceq 'Mock') {
    . (Join-Path $PSScriptRoot 'GovernedAuditOutbox.Durable.Model.ps1')
    . (Join-Path $PSScriptRoot 'GovernedAuditOutbox.Concurrent.Model.ps1')
} else {. (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')}
$context=@{workspacePath='/fixture/concurrent';agentIdentity='Codex';mcpSessionId='session-concurrent';hostSessionKey=$HostSessionKey}
$payload=ConvertTo-Json -InputObject ([ordered]@{text="equal captured `r`n payload";unicode=([string][char]0x96EA)}) -Compress
$inputData=@{requestId=$RequestId;kind='dialog';payloadJson=$payload}
$workerState=@{held=$false;now=1000L;trace=$TracePath;gate=$GatePath;hold=$Hold;mutation=$Mutation}
function Add-ConcurrentEffect {
    param([hashtable]$Value)
    $text=ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    Add-LinesToFile -Path $workerState.trace -Content $text *> $null
}
function Wait-ConcurrentRelease {
    param([string]$Boundary)
    if($workerState.held){return}
    $workerState.held=$true
    Add-ConcurrentEffect @{effect='gate_ready';boundary=$Boundary;pid=$PID}
    Add-LinesToFile -Path ($workerState.gate+'.ready') -Content $Boundary *> $null
    $deadline=[DateTimeOffset]::Now.AddSeconds(45)
    while(-not [IO.File]::Exists($workerState.gate+'.release')) {
        if([DateTimeOffset]::Now -gt $deadline){throw 'Fixture release gate not delivered'}
        Start-Sleep -Milliseconds 40
    }
    Add-ConcurrentEffect @{effect='gate_released';boundary=$Boundary;pid=$PID}
}
$observer={
    param($Phase,$Path)
    Add-ConcurrentEffect @{effect='publication';phase=$Phase;path=$Path}
    if($workerState.hold -ceq 'Publication' -and $Phase -ceq 'published' -and [IO.Path]::GetFileName($Path) -ceq 'sequence.yaml'){Wait-ConcurrentRelease 'sequence-published'}
}
$transport=@{
    submit={
        param($AuditRecord)
        Add-ConcurrentEffect @{effect='submit';event=$AuditRecord}
        if($workerState.hold -ceq 'Submit'){Wait-ConcurrentRelease 'submit'}
        $reply=@{code='persisted';revision=7}
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){$reply[$field]=$AuditRecord[$field]}
        if($workerState.mutation -ceq 'missing-receipt-identity'){[void]$reply.Remove('eventId')}
        Add-ConcurrentEffect @{effect='reply';receipt=$reply}
        return $reply
    }
    read={param($AuditRecord) Add-ConcurrentEffect @{effect='read';event=$AuditRecord};return @{code='lost'}}
}
$arguments=@{RootDirectory=$RootDirectory;Context=$context;Operation=$Operation;InputObject=$inputData;Transport=$transport;Clock={$workerState.now};Sleep={param($Milliseconds) Add-ConcurrentEffect @{effect='sleep';milliseconds=$Milliseconds};$workerState.now+=$Milliseconds};PublicationObserver=$observer}
$result=if($Mode -ceq 'Mock'){Invoke-MockConcurrentOutbox @arguments -Mutation $Mutation}else{Invoke-McpGovernedAuditOutbox @arguments}
$resultText=ConvertTo-Json -InputObject $result -Depth 50
Add-LinesToFile -Path $ResultPath -Content $resultText *> $null
