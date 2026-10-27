# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-008/009/069: independent process-lock composition over the retained durable test model.
.DESCRIPTION
Uses fixture OS handles and the independently authored durable model. No production
module is imported. Faults change actual lock acquisition or capture dependency effects.
#>
function Invoke-MockConcurrentOutbox {
    [CmdletBinding()]
    param([string]$RootDirectory,[hashtable]$Context,[string]$Operation,
        [hashtable]$InputObject,[hashtable]$Transport,[scriptblock]$Clock,
        [scriptblock]$Sleep,[scriptblock]$PublicationObserver,[string]$Mutation='')
    $handle=$null
    try {
        $identity=ConvertTo-Json -InputObject @($Context.workspacePath,$Context.agentIdentity,$Context.mcpSessionId,$Context.hostSessionKey) -Compress
        $digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($identity))).ToLowerInvariant()
        $directory=Join-Path $RootDirectory $digest
        [void][IO.Directory]::CreateDirectory($directory)
        if($Mutation -cne 'omit-lock') {
            $handle=[IO.FileStream]::new((Join-Path $directory '.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
            if($handle.Length -ne 0){throw 'Retained legacy lock is not reusable'}
        }
        $arguments=@{RootDirectory=$RootDirectory;Context=$Context;Operation=$Operation;InputObject=$InputObject;Transport=$Transport;Clock=$Clock;Sleep=$Sleep;PublicationObserver=$PublicationObserver}
        if($Mutation -ceq 'omit-capture'){$arguments.Mutation='omit-capture'}
        return Invoke-MockDurableAuditOutbox @arguments
    } catch {
        return @{code='lost';reason='Independent fixture lock/dependency refused operation'}
    } finally {
        if($handle){$handle.Dispose()}
    }
}
