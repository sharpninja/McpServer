# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
FR-QB-002/003/005: Context-owned A8 multi-open turn and alias persistence.
.DESCRIPTION
See ../contracts/governed-turn-store.md. No transport or completion grant is
implemented here. All caller context and operations belong to the trusted host.
#>
function Invoke-McpGovernedTurnStore {
    <#
    .SYNOPSIS
    TR-QB-AUDIT-001: Serializes a lifecycle operation with durable alias lookup.
    .PARAMETER Directory
    Trusted private context directory. Never resolved from environment state.
    .PARAMETER Context
    Validated workspacePath, agentIdentity and fixed aliasConsumers.
    .PARAMETER Operation
    Internal lifecycle operation; Terminal requires the caller's completion gate.
    .PARAMETER InputObject
    Captured host input, independent of model-authored tool arguments.
    .PARAMETER PublicationObserver
    Trusted fixture seam called at staged/published boundaries. No environment
    or model-facing configuration can enable it. Production callers omit it.
    .OUTPUTS
    Typed result and independent snapshot. Success is local durability only.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Directory,
        [Parameter(Mandatory)][System.Collections.IDictionary]$Context,
        [Parameter(Mandatory)][ValidateSet('Register','Deliver','Resolve','ResolveOriginal','Alias','ListAliases','Acknowledge','Terminal')][string]$Operation,
        [Parameter(Mandatory)][System.Collections.IDictionary]$InputObject,
        [scriptblock]$PublicationObserver
    )
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'governed-turn-binding.ps1')
    $fields=@('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')
    $state=$null
    $lockStream=$null
    $observer=$PublicationObserver
    function Get-StoreResult {
        param([string]$Code,[bool]$Ok=$false,[object]$Snapshot=$state,[object]$Binding=$null,[object[]]$Aliases=@(),[bool]$Retryable=$false)
        return [ordered]@{ok=$Ok;code=$Code;retryable=$Retryable;binding=$Binding;aliases=@($Aliases);state=$Snapshot}
    }
    function Test-StoreText {
        param([AllowNull()][object]$Value)
        return ($Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value))
    }
    function Test-StoreKeySet {
        param([object]$Value,[string[]]$Keys)
        return ($Value -is [System.Collections.IDictionary] -and
            (@($Value.Keys|Sort-Object -CaseSensitive)-join '|') -ceq (@($Keys|Sort-Object -CaseSensitive)-join '|'))
    }
    function Test-StoreUtc {
        param([object]$Value)
        if($Value -isnot [string] -or $Value -cnotmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?Z$'){return $false}
        $parsed=[DateTimeOffset]::MinValue
        return [DateTimeOffset]::TryParse($Value,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::AssumeUniversal,[ref]$parsed)
    }
    function Test-StoreBinding {
        param([object]$Value)
        if($Value -isnot [System.Collections.IDictionary]){return $false}
        foreach($field in $fields){if(-not $Value.Contains($field) -or -not (Test-StoreText $Value[$field])){return $false}}
        return ($Value.workspacePath -ceq $Context.workspacePath -and $Value.agentIdentity -ceq $Context.agentIdentity -and
            (Test-StoreUtc $Value.boundAtUtc) -and
            (-not $Value.requestId.StartsWith('pending-',[StringComparison]::Ordinal) -or $Value.requestId -ceq "pending-$($Value.ingressNonce)"))
    }
    function Test-StoreStringSet {
        param([object]$Values)
        if($Values -isnot [array]){return $false}
        $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($value in $Values){if(-not (Test-StoreText $value) -or -not $seen.Add($value)){return $false}}
        return $true
    }
    function Test-StoreSnapshot {
        param([object]$Value)
        if(-not (Test-StoreKeySet $Value @('schemaVersion','workspacePath','agentIdentity','aliasConsumers','revision','hosts','aliases'))){return $false}
        if($Value.schemaVersion -isnot [long] -and $Value.schemaVersion -isnot [int]){return $false}
        if($Value.schemaVersion -ne 1 -or ($Value.revision -isnot [long] -and $Value.revision -isnot [int]) -or $Value.revision -lt 0 -or
            $Value.hosts -isnot [array] -or $Value.aliases -isnot [array] -or -not (Test-StoreStringSet $Value.aliasConsumers) -or
            $Value.aliasConsumers.Count -eq 0){return $false}
        $hostKeys=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($entry in $Value.hosts){
            if(-not (Test-StoreKeySet $entry @('hostSessionKey','turns')) -or -not (Test-StoreText $entry.hostSessionKey) -or
                -not $hostKeys.Add($entry.hostSessionKey) -or $entry.turns -isnot [array]){return $false}
            $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            $nonces=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            foreach($turn in $entry.turns){
                if(-not (Test-StoreKeySet $turn ($fields+@('delivered','status'))) -or -not (Test-StoreBinding $turn) -or
                    $turn.hostSessionKey -cne $entry.hostSessionKey -or $turn.delivered -isnot [bool] -or
                    $turn.status -cnotin @('open','terminal') -or -not $ids.Add($turn.requestId) -or -not $nonces.Add($turn.ingressNonce)){return $false}
            }
        }
        $pendingKeys=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $mintedKeys=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($alias in $Value.aliases){
            if(-not (Test-StoreKeySet $alias ($fields+@('pendingId','aliasedAtUtc','acks'))) -or -not (Test-StoreBinding $alias) -or
                $alias.pendingId -cne "pending-$($alias.ingressNonce)" -or $alias.requestId.StartsWith('pending-',[StringComparison]::Ordinal) -or
                -not (Test-StoreUtc $alias.aliasedAtUtc) -or -not (Test-StoreStringSet $alias.acks)){return $false}
            foreach($ack in $alias.acks){if($ack -cnotin $Value.aliasConsumers){return $false}}
            $pendingKey=ConvertTo-Json -InputObject @($alias.hostSessionKey,$alias.pendingId) -Compress
            $mintedKey=ConvertTo-Json -InputObject @($alias.hostSessionKey,$alias.requestId) -Compress
            if(-not $pendingKeys.Add($pendingKey) -or -not $mintedKeys.Add($mintedKey)){return $false}
            $owners=@($Value.hosts|Where-Object hostSessionKey -CEQ $alias.hostSessionKey|ForEach-Object {$_.turns}|Where-Object requestId -CEQ $alias.requestId)
            if($owners.Count -ne 1){return $false}
            foreach($field in $fields){if($owners[0][$field] -cne $alias[$field]){return $false}}
        }
        return $true
    }
    function Assert-StorePath {
        param([string]$Path)
        $cursor=$Path
        while($cursor){
            if([IO.Path]::Exists($cursor) -and ([IO.File]::GetAttributes($cursor) -band [IO.FileAttributes]::ReparsePoint)){throw [IO.InvalidDataException]::new('Symbolic link in lifecycle storage path.')}
            $parent=[IO.Path]::GetDirectoryName($cursor)
            if($parent -ceq $cursor){break};$cursor=$parent
        }
    }
    function Get-StoreCanonicalId {
        param([object]$Id,[object]$HostKey)
        if($Id -isnot [string]){return $Id}
        $alias=@($state.aliases|Where-Object {$_.hostSessionKey -ceq $HostKey -and $_.pendingId -ceq $Id})
        if($alias.Count -eq 1){return $alias[0].requestId}
        return $Id
    }
    function Publish-StoreSnapshot {
        param([string]$Path)
        $state.revision=[long]$state.revision+1
        if(-not (Test-StoreSnapshot $state)){throw [IO.InvalidDataException]::new('Lifecycle mutation violated snapshot invariants.')}
        $bytes=[Text.UTF8Encoding]::new($false).GetBytes((ConvertTo-Json -InputObject $state -Depth 60 -Compress))
        $temporary=Join-Path $Directory ('turn-store.'+[guid]::NewGuid().ToString('N')+'.pending')
        $stream=[IO.FileStream]::new($temporary,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None,4096,[IO.FileOptions]::WriteThrough)
        try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
        if($observer){& $observer 'staged'|Out-Null}
        # Atomic publication is the approved write-ahead snapshot boundary. No
        # user file is relocated; failed staging files remain recovery evidence.
        [IO.File]::Move($temporary,$Path,$true)
        if($observer){& $observer 'published'|Out-Null}
    }
    try{
        if(-not $Context.Contains('workspacePath') -or -not $Context.Contains('agentIdentity') -or -not $Context.Contains('aliasConsumers') -or
            -not (Test-StoreText $Context.workspacePath) -or -not (Test-StoreText $Context.agentIdentity) -or
            -not (Test-StoreStringSet $Context.aliasConsumers) -or 'qb-audit-outbox-v1' -cnotin $Context.aliasConsumers -or
            -not [IO.Path]::IsPathFullyQualified($Directory)){
            return (Get-StoreResult context_mismatch -Snapshot $null)
        }
        $Directory=[IO.Path]::GetFullPath($Directory)
        Assert-StorePath $Directory
        [void][IO.Directory]::CreateDirectory($Directory)
        $path=Join-Path $Directory 'turn-store.json';$lockPath=Join-Path $Directory 'turn-store.lock'
        Assert-StorePath $path;Assert-StorePath $lockPath
        $timer=[Diagnostics.Stopwatch]::StartNew()
        while($null -eq $lockStream){
            try{$lockStream=[IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)}
            catch [IO.IOException]{if($timer.ElapsedMilliseconds -ge 2500){return (Get-StoreResult store_unavailable -Snapshot $null -Retryable $true)};Start-Sleep -Milliseconds 25}
        }
        Assert-StorePath $path
        if([IO.File]::Exists($path)){
            try{$state=ConvertFrom-Json ([IO.File]::ReadAllText($path)) -AsHashtable -DateKind String -NoEnumerate}catch{
                return (Get-StoreResult store_corrupt -Snapshot $null)
            }
            if($state -isnot [System.Collections.IDictionary] -or -not $state.Contains('workspacePath') -or -not $state.Contains('agentIdentity') -or -not $state.Contains('aliasConsumers')){
                return (Get-StoreResult store_corrupt -Snapshot $null)
            }
            if($state.workspacePath -cne $Context.workspacePath -or $state.agentIdentity -cne $Context.agentIdentity -or
                (ConvertTo-Json -InputObject $state.aliasConsumers -Compress) -cne (ConvertTo-Json -InputObject @($Context.aliasConsumers) -Compress)){
                return (Get-StoreResult context_mismatch -Snapshot $null)
            }
            if(-not (Test-StoreSnapshot $state)){return (Get-StoreResult store_corrupt -Snapshot $null)}
        }else{
            $state=[ordered]@{schemaVersion=1;workspacePath=$Context.workspacePath;agentIdentity=$Context.agentIdentity;aliasConsumers=@($Context.aliasConsumers);revision=0;hosts=@();aliases=@()}
        }
        if($Operation -ceq 'ResolveOriginal'){
            $recovery=Resolve-McpGovernedRecoveryBinding -Context $state -TrustedPair $InputObject
            return (Get-StoreResult $recovery.code -Ok $recovery.ok -Binding $recovery.binding)
        }
        if($Operation -ceq 'Register'){
            if(-not (Test-StoreKeySet $InputObject $fields) -or -not (Test-StoreBinding $InputObject)){return (Get-StoreResult ingress_nonce_invalid)}
            $hosts=@($state.hosts|Where-Object hostSessionKey -CEQ $InputObject.hostSessionKey)
            $existing=@(if($hosts.Count){$hosts[0].turns|Where-Object ingressNonce -CEQ $InputObject.ingressNonce})
            if($existing.Count){
                foreach($field in $fields){
                    $expected=if($field -ceq 'requestId'){Get-StoreCanonicalId $InputObject.requestId $InputObject.hostSessionKey}else{$InputObject[$field]}
                    if($existing[0][$field] -cne $expected){return (Get-StoreResult ingress_nonce_invalid)}
                }
                return (Get-StoreResult unchanged -Ok $true)
            }
            if($hosts.Count -and @($hosts[0].turns|Where-Object requestId -CEQ $InputObject.requestId).Count){return (Get-StoreResult ingress_nonce_invalid)}
            if(-not $hosts.Count){$entry=[ordered]@{hostSessionKey=$InputObject.hostSessionKey;turns=@()};$state.hosts+=@($entry)}else{$entry=$hosts[0]}
            $turn=[ordered]@{};foreach($field in $fields){$turn[$field]=$InputObject[$field]};$turn.delivered=$false;$turn.status='open';$entry.turns+=@($turn)
            Publish-StoreSnapshot $path
            return (Get-StoreResult registered -Ok $true)
        }
        if($Operation -ceq 'Alias'){
            if(-not (Test-StoreKeySet $InputObject ($fields+@('pendingId','aliasedAtUtc'))) -or -not (Test-StoreBinding $InputObject) -or
                $InputObject.pendingId -cne "pending-$($InputObject.ingressNonce)" -or $InputObject.requestId.StartsWith('pending-',[StringComparison]::Ordinal) -or
                -not (Test-StoreUtc $InputObject.aliasedAtUtc)){return (Get-StoreResult alias_conflict)}
            $aliases=@($state.aliases|Where-Object {$_.hostSessionKey -ceq $InputObject.hostSessionKey -and ($_.pendingId -ceq $InputObject.pendingId -or $_.requestId -ceq $InputObject.requestId)})
            if($aliases.Count){
                if($aliases.Count -ne 1){return (Get-StoreResult alias_conflict)}
                foreach($field in ($fields+@('pendingId'))){if($aliases[0][$field] -cne $InputObject[$field]){return (Get-StoreResult alias_conflict)}}
                return (Get-StoreResult unchanged -Ok $true)
            }
            $hostTurns=@($state.hosts|Where-Object hostSessionKey -CEQ $InputObject.hostSessionKey|ForEach-Object {$_.turns})
            $owners=@($hostTurns|Where-Object requestId -CEQ $InputObject.pendingId)
            if($owners.Count -ne 1 -or @($hostTurns|Where-Object requestId -CEQ $InputObject.requestId).Count){return (Get-StoreResult alias_conflict)}
            foreach($field in $fields){if($field -cne 'requestId' -and $owners[0][$field] -cne $InputObject[$field]){return (Get-StoreResult alias_conflict)}}
            if($owners[0].status -ceq 'terminal'){return (Get-StoreResult turn_immutable)}
            $alias=[ordered]@{};foreach($field in ($fields+@('pendingId','aliasedAtUtc'))){$alias[$field]=$InputObject[$field]};$alias.acks=@()
            $state.aliases+=@($alias);$owners[0].requestId=$InputObject.requestId
            Publish-StoreSnapshot $path
            return (Get-StoreResult aliased -Ok $true)
        }
        if($Operation -ceq 'Acknowledge'){
            if(-not (Test-StoreKeySet $InputObject @('hostSessionKey','mcpSessionId','pendingId','requestId','consumerId'))){return (Get-StoreResult alias_conflict)}
            if($InputObject.consumerId -cnotin $state.aliasConsumers){return (Get-StoreResult consumer_unknown)}
            $matchingAliases=@($state.aliases|Where-Object {$_.hostSessionKey -ceq $InputObject.hostSessionKey -and $_.mcpSessionId -ceq $InputObject.mcpSessionId -and $_.pendingId -ceq $InputObject.pendingId -and $_.requestId -ceq $InputObject.requestId})
            if($matchingAliases.Count -ne 1){return (Get-StoreResult alias_conflict)}
            if($InputObject.consumerId -cin $matchingAliases[0].acks){return (Get-StoreResult unchanged -Ok $true)}
            $matchingAliases[0].acks+=@($InputObject.consumerId)
            Publish-StoreSnapshot $path
            return (Get-StoreResult acknowledged -Ok $true)
        }
        $hostKey=if($InputObject.Contains('hostSessionKey')){$InputObject.hostSessionKey}else{$null}
        if($Operation -ceq 'ListAliases'){
            if($InputObject.Contains('consumerId') -and $InputObject.consumerId -cnotin $state.aliasConsumers){return (Get-StoreResult consumer_unknown)}
            if(-not (Test-StoreText $hostKey) -or @($state.hosts|Where-Object hostSessionKey -CEQ $hostKey).Count -ne 1){return (Get-StoreResult host_session_unknown)}
            $matchingAliases=@($state.aliases|Where-Object {$_.hostSessionKey -ceq $hostKey -and (-not $InputObject.Contains('consumerId') -or $InputObject.consumerId -cnotin $_.acks)})
            return (Get-StoreResult listed -Ok $true -Aliases $matchingAliases)
        }
        $selection=@{Context=$state;HostSessionKey=$hostKey}
        if($InputObject.Contains('requestId')){$selection.RequestId=Get-StoreCanonicalId $InputObject.requestId $hostKey}
        elseif($Operation -cne 'Resolve'){return (Get-StoreResult request_unknown)}
        $resolved=Resolve-McpGovernedTurnBinding @selection
        if($Operation -ceq 'Resolve'){return (Get-StoreResult $resolved.code -Ok $resolved.ok -Binding $resolved.binding)}
        if(-not $resolved.ok){
            if($Operation -ceq 'Terminal' -and $resolved.code -ceq 'turn_immutable'){return (Get-StoreResult unchanged -Ok $true)}
            return (Get-StoreResult $resolved.code)
        }
        $selected=@($state.hosts|Where-Object hostSessionKey -CEQ $hostKey|ForEach-Object {$_.turns}|Where-Object requestId -CEQ $resolved.binding.requestId)[0]
        if($Operation -ceq 'Deliver'){
            if($selected.delivered){return (Get-StoreResult unchanged -Ok $true)}
            $selected.delivered=$true;Publish-StoreSnapshot $path
            return (Get-StoreResult delivered -Ok $true)
        }
        $selected.status='terminal';Publish-StoreSnapshot $path
        return (Get-StoreResult terminal -Ok $true)
    }catch [IO.InvalidDataException]{
        return (Get-StoreResult store_corrupt -Snapshot $null)
    }catch [UnauthorizedAccessException]{
        return (Get-StoreResult store_unavailable -Snapshot $null -Retryable $true)
    }catch [IO.IOException]{
        return (Get-StoreResult store_unavailable -Snapshot $null -Retryable $true)
    }finally{
        if($null -ne $lockStream){$lockStream.Dispose()}
    }
}

