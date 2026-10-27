# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-008/009/069: one isolated alias-journal operation with real process termination.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode,
    [Parameter(Mandatory)][string]$RootDirectory,
    [Parameter(Mandatory)][string]$TracePath,
    [Parameter(Mandatory)][string]$ResultPath,
    [ValidateSet('Enqueue','ApplyAlias','Resolve')][string]$Operation,
    [string]$RequestId='pending-alias-nonce',
    [string]$CrashPhase='', [string]$CrashKind='', [int]$CrashAfter=1,
    [string]$Mutation=''
)
$ErrorActionPreference='Stop'
Write-Output 'Counted effects: one fixture outbox operation, publication trace and result receipt; optional own-process exit75. No live transport.'
if(-not $PSCmdlet.ShouldProcess($RootDirectory,'Execute isolated alias recovery fixture operation')){return}
Import-Module powershell-yaml -ErrorAction Stop
Import-Module PowerShell.MCP -ErrorAction Stop
if($Mode -ceq 'Mock'){. (Join-Path $PSScriptRoot 'GovernedAuditOutbox.AliasRestart.Model.ps1')}
else{. (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')}
$context=@{workspacePath='/fixture/alias-restart';agentIdentity='Codex';mcpSessionId='session-alias-restart';hostSessionKey='host-alias-restart'}
$inputData=if($Operation -ceq 'ApplyAlias'){
    @{workspacePath=$context.workspacePath;agent='Codex';mcpSessionId=$context.mcpSessionId;hostSessionKey=$context.hostSessionKey;ingressNonce='alias-nonce';pendingId='pending-alias-nonce';requestId='req-alias-minted';aliasedAtUtc='2026-10-22T01:00:00Z';acks=@()}
}else{@{requestId=$RequestId;kind='dialog';payloadJson=(ConvertTo-Json -InputObject @{text="retained `r`n alias payload";unicode=([string][char]0x96EA)} -Compress)}}
$workerTrace=$TracePath;$terminationPhase=$CrashPhase;$terminationKind=$CrashKind;$terminationAfter=$CrashAfter
$script:aliasCrashCount=0
$observer={
    param($Phase,$Path)
    $kind=if([IO.Path]::GetFileName($Path) -ceq 'applied-aliases.yaml'){'aliasJournal'}elseif($Operation -cin @('ApplyAlias','Resolve') -and $Path.EndsWith('.event.yaml')){'aliasEvent'}else{'other'}
    $effect=[ordered]@{phase=$Phase;kind=$kind;path=$Path}
    Add-LinesToFile -Path $workerTrace -Content (ConvertTo-Json -InputObject $effect -Compress) *> $null
    if($Phase -ceq $terminationPhase -and $kind -ceq $terminationKind){
        $script:aliasCrashCount++
        if($script:aliasCrashCount -eq $terminationAfter){[Environment]::Exit(75)}
    }
}
$arguments=@{RootDirectory=$RootDirectory;Context=$context;Operation=$Operation;InputObject=$inputData;Transport=@{};Clock={1000L};Sleep={param($Milliseconds) [void]$Milliseconds};PublicationObserver=$observer}
$result=if($Mode -ceq 'Mock'){Invoke-MockAliasRestartAuditOutbox @arguments -Mutation $Mutation}else{Invoke-McpGovernedAuditOutbox @arguments}
$var1=$result|ConvertTo-Json -Depth 40
Add-LinesToFile -Path $ResultPath -Content $var1 *> $null
if($result.code -ceq 'lost'){exit 1}
