# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-005/009: Isolated child process for lifecycle crash and writer tests.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$Directory,
    [ValidateSet('AliasCrash','Writer')][string]$Action='Writer',
    [ValidateSet('none','staged','published')][string]$FaultAt='none',
    [string]$Nonce='one'
)
$ErrorActionPreference='Stop'
if(-not $PSCmdlet.ShouldProcess($Directory,"Fixture $Action $FaultAt")){return}
. (Join-Path $PSScriptRoot '../lib-ps/governed-turn-store.ps1')
$context=@{workspacePath='/fixture/workspace';agentIdentity='ClineQb';aliasConsumers=@('qb-audit-outbox-v1','host-cache-v1')}
$binding=@{workspacePath='/fixture/workspace';agentIdentity='ClineQb';mcpSessionId='session-one';hostSessionKey='host-one';keySource='native';ingressNonce=$Nonce;requestId="pending-$Nonce";boundAtUtc='2026-10-14T23:00:00.0000000Z'}
if($Action -eq 'AliasCrash'){
    $binding.requestId='req-one';$binding.pendingId='pending-one';$binding.aliasedAtUtc='2026-10-14T23:01:00.0000000Z'
    $fault={param($Phase) if($Phase -ceq $FaultAt){[Environment]::Exit(75)}}.GetNewClosure()
    $result=Invoke-McpGovernedTurnStore -Directory $Directory -Context $context -Operation Alias -InputObject $binding -PublicationObserver $fault
    if(-not $result.ok){throw "Alias failed: $($result.code)"}
}else{
    $result=Invoke-McpGovernedTurnStore -Directory $Directory -Context $context -Operation Register -InputObject $binding
    if(-not $result.ok){throw "Registration failed: $($result.code)"}
    $result=Invoke-McpGovernedTurnStore -Directory $Directory -Context $context -Operation Deliver -InputObject @{hostSessionKey='host-one';requestId="pending-$Nonce"}
    if(-not $result.ok){throw "Delivery failed: $($result.code)"}
}
