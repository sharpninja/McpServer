# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent test-only outbox storage guard model for TEST-QB-008/009/069.
.DESCRIPTION
Wraps the independently authored sequential state model with real fixture locks
and link checks. It has no case-name or expected-outcome inputs and never imports
production. Mutations omit dependency protections for rejecting control runs.
#>
function Invoke-MockGuardedOutbox {
    [CmdletBinding()]
    param([string]$RootDirectory,[hashtable]$Context,[string]$Operation,
        [hashtable]$InputObject,[hashtable]$Transport,[scriptblock]$Clock,
        [scriptblock]$Sleep,[scriptblock]$PublicationObserver,[string]$Mutation='')
    function Assert-ModelStoragePath {
        param([string]$Path)
        if($Mutation -ceq 'omit-links'){return}
        $cursor=[IO.Path]::GetFullPath($Path)
        while($cursor){
            $info=[IO.FileInfo]::new($cursor)
            if($info.LinkTarget -or ([IO.Path]::Exists($cursor) -and ([IO.File]::GetAttributes($cursor) -band [IO.FileAttributes]::ReparsePoint))){throw 'Linked storage refused'}
            $parent=[IO.Path]::GetDirectoryName($cursor)
            if($parent -ceq $cursor){break}
            $cursor=$parent
        }
    }
    $lock=$null
    try{
        Assert-ModelStoragePath $RootDirectory
        $identity=ConvertTo-Json -InputObject @($Context.workspacePath,$Context.agentIdentity,$Context.mcpSessionId,$Context.hostSessionKey) -Compress
        $digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($identity))).ToLowerInvariant()
        $directory=Join-Path $RootDirectory $digest
        Assert-ModelStoragePath $directory
        [void][IO.Directory]::CreateDirectory($directory)
        $scan=[Collections.Generic.Queue[string]]::new()
        $scan.Enqueue($directory)
        while($scan.Count){
            foreach($entry in @(Get-ChildItem -LiteralPath $scan.Dequeue() -Force)){
                Assert-ModelStoragePath $entry.FullName
                if($entry.PSIsContainer){$scan.Enqueue($entry.FullName)}
            }
        }
        $lockPath=Join-Path $directory '.lock'
        Assert-ModelStoragePath $lockPath
        if($Mutation -cne 'omit-lock'){
            $lock=[IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
            if($lock.Length -gt 0 -and $Mutation -cne 'omit-legacy'){throw 'Legacy owner evidence refused'}
        }
        $arguments=@{RootDirectory=$RootDirectory;Context=$Context;Operation=$Operation;InputObject=$InputObject}
        foreach($name in @('Transport','Clock','Sleep','PublicationObserver')){
            if($PSBoundParameters.ContainsKey($name)){$arguments[$name]=$PSBoundParameters[$name]}
        }
        return Invoke-MockAuditOutbox @arguments
    }catch{
        return @{code='lost';reason='Fixture guard refused dependency'}
    }finally{
        if($lock){$lock.Dispose()}
    }
}
