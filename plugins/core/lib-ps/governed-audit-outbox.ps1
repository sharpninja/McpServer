# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Persist and replay context-bound governed audit events through caller transports.
.DESCRIPTION
FR-QB-003/005; TR-QB-AUDIT-001; AC/TEST-QB-008/009/069.
The operation seam accepts captured immutable context and JSON text, never an
ambient current turn, environment-selected root, HTTP client or completion grant.
This increment covers the released sequential contract. Host integration and
process/locking/link acceptance remain separate P1 gates.
#>
function Invoke-McpGovernedAuditOutbox {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RootDirectory,
        [Parameter(Mandatory)][Collections.IDictionary]$Context,
        [Parameter(Mandatory)][ValidateSet('Enqueue','ApplyAlias','Resolve','Flush','CheckCompletion','ReadManifest','CheckCapturedCompletion')][string]$Operation,
        [Collections.IDictionary]$InputObject=@{},[Collections.IDictionary]$Transport=@{},
        [scriptblock]$Clock={[DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()},
        [scriptblock]$Sleep={param($Milliseconds) Start-Sleep -Milliseconds $Milliseconds},
        [scriptblock]$PublicationObserver={param($Stage,$Path) [void]$Stage;[void]$Path}
    )
    $clockSource=$Clock;$delaySource=$Sleep;$publicationSource=$PublicationObserver
    $heldLock=$null
    function Assert-OutboxStoragePath {
        param([string]$Path)
        for($cursor=[IO.Path]::GetFullPath($Path);$cursor;$cursor=[IO.Path]::GetDirectoryName($cursor)){
            $info=[IO.FileInfo]::new($cursor)
            if($info.LinkTarget -or ([IO.Path]::Exists($cursor) -and ([IO.File]::GetAttributes($cursor) -band [IO.FileAttributes]::ReparsePoint))){throw 'Linked audit storage is refused'}
        }
    }
    function Assert-OutboxStorageTree {
        param([string]$Path)
        Assert-OutboxStoragePath $Path
        foreach($entry in @(Get-ChildItem -LiteralPath $Path -Force)){
            Assert-OutboxStoragePath $entry.FullName
            if($entry.PSIsContainer){Assert-OutboxStorageTree $entry.FullName}
        }
    }
    function Get-OutboxDigest {
        param([string]$Text)
        return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text))).ToLowerInvariant()
    }
    function Copy-OutboxValue {
        param([object]$Value)
        return ,(ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 60 -Compress) -AsHashtable -DateKind String -NoEnumerate)
    }
    function Read-OutboxDocument {
        param([string]$Path)
        return ConvertFrom-Yaml ([IO.File]::ReadAllText($Path))
    }
    function Write-OutboxDocument {
        param([string]$Path,[object]$Value)
        $temporary=$Path+'.'+[guid]::NewGuid().ToString('N')+'.tmp'
        $bytes=[Text.UTF8Encoding]::new($false).GetBytes((ConvertTo-Yaml $Value))
        $stream=[IO.FileStream]::new($temporary,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None,4096,[IO.FileOptions]::WriteThrough)
        try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
        & $publicationSource 'staged' $Path
        [IO.File]::Move($temporary,$Path,$true)
        & $publicationSource 'published' $Path
    }
    function Get-OutboxFile {
        param([string]$Folder='')
        return @(Get-ChildItem -LiteralPath (Join-Path $directory $Folder) -File -Force|Sort-Object Name -CaseSensitive)
    }
    function Resolve-OutboxRequest {
        param([string]$Id,[switch]$Logical)
        foreach($binding in @($aliases)){
            if($Logical -and $Id -ceq $binding.requestId){return $binding.pendingId}
            if(-not $Logical -and $Id -ceq $binding.pendingId){return $binding.requestId}
        }
        return $Id
    }
    function Get-OutboxFilename {
        param([Collections.IDictionary]$AuditRecord)
        $requestKey=Get-OutboxDigest (Resolve-OutboxRequest $AuditRecord.requestId -Logical)
        return $requestKey+'-'+([long]$AuditRecord.order).ToString('D16')+'-'+$AuditRecord.eventId+'.event.yaml'
    }
    function Test-OutboxInteger {
        param([object]$Value,[long]$Minimum=0,[long]$Maximum=9007199254740991)
        return $Value -is [ValueType] -and $Value -isnot [bool] -and $Value -ge $Minimum -and $Value -le $Maximum -and [decimal]$Value % 1 -eq 0
    }
    function Test-OutboxEvent {
        param([object]$AuditRecord)
        if($AuditRecord -isnot [Collections.IDictionary]){return $false}
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId')){
            if($AuditRecord[$field] -isnot [string] -or -not [string]::Equals($AuditRecord[$field],$captured[$field],[StringComparison]::Ordinal)){return $false}
        }
        $parsedGuid=[guid]::Empty
        if(-not [guid]::TryParseExact($AuditRecord.eventId,'D',[ref]$parsedGuid) -or $parsedGuid -eq [guid]::Empty -or -not (Test-OutboxInteger $AuditRecord.order 1)){return $false}
        if($AuditRecord.requestId -isnot [string] -or [string]::IsNullOrWhiteSpace($AuditRecord.requestId) -or $AuditRecord.kind -cnotin @('action','decision','dialog','metadata')){return $false}
        if($AuditRecord.payloadJson -isnot [string] -or $AuditRecord.payloadHash -cne (Get-OutboxDigest $AuditRecord.payloadJson)){return $false}
        try{return (ConvertFrom-Json $AuditRecord.payloadJson -AsHashtable -NoEnumerate) -is [Collections.IDictionary]}catch{return $false}
    }
    function Test-OutboxReceipt {
        param([object]$Receipt,[Collections.IDictionary]$AuditRecord,[switch]$Revision)
        if($Receipt -isnot [Collections.IDictionary] -or $Receipt.code -cnotin @('persisted','queued','unchanged','rejected','lost')){return $false}
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','payloadHash')){
            if($Receipt[$field] -isnot [string] -or -not [string]::Equals($Receipt[$field],$AuditRecord[$field],[StringComparison]::Ordinal)){return $false}
        }
        return (Test-OutboxInteger $Receipt.order 1) -and $Receipt.order -eq $AuditRecord.order -and (-not $Revision -or (Test-OutboxInteger $Receipt.revision 1))
    }
    function Test-OutboxTerminalReceipt {
        param([object]$Receipt,[Collections.IDictionary]$AuditRecord)
        return (Test-OutboxReceipt $Receipt $AuditRecord) -and $Receipt.code -ceq 'rejected' -and $Receipt.reason -ceq 'skipped_terminal' -and $Receipt.storedStatus -is [string] -and -not [string]::IsNullOrWhiteSpace($Receipt.storedStatus)
    }
    function Get-OutboxFailure {
        param([object]$Failure)
        for($depth=0;$null -ne $Failure -and $depth -lt 8;$depth++){
            $structured=if($Failure -is [Collections.IDictionary]){$Failure}else{$Failure.Data}
            if($structured -and ($structured.Contains('detailCode') -or $structured.Contains('code'))){
                $code=if($structured.Contains('detailCode')){$structured.detailCode}else{$structured.code}
                if($code -isnot [string] -or $code -cnotmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$'){break}
                $flag=$structured.retryable
                $reported=if($structured.Contains('reportedRetryable')){$structured.reportedRetryable}else{$flag}
                $flagsValid=($null -eq $flag -or $flag -is [bool]) -and ($null -eq $reported -or $reported -is [bool])
                $transient=$code -cin @('transport_unavailable','transport_timeout','ECONNRESET','ECONNREFUSED','ETIMEDOUT','EPIPE','EHOSTUNREACH','ENETUNREACH','EAI_AGAIN')
                $degraded=$code -cin @('backend_unavailable','persistence_error','timeout')
                $classified=@{detailCode=$code;retryable=[bool]($flagsValid -and (($transient -and $flag -ne $false) -or ($degraded -and $flag -eq $true)))}
                if($reported -is [bool]){$classified.reportedRetryable=$reported}
                return $classified
            }
            $Failure=$Failure.InnerException
        }
        return @{detailCode='unknown_error';retryable=$false}
    }
    function Complete-OutboxRecord {
        param([IO.FileInfo]$File,[string]$Folder)
        $target=Join-Path (Join-Path $directory $Folder) $File.Name
        & $publicationSource 'retiring' $target
        [IO.File]::Move($File.FullName,$target)
        & $publicationSource 'retired' $target
    }
    function Get-OutboxCheckedAlias {
        param([Collections.IDictionary]$Value)
        foreach($pair in @(@('workspacePath','workspacePath'),@('agent','agentIdentity'),@('mcpSessionId','mcpSessionId'),@('hostSessionKey','hostSessionKey'))){
            if($Value[$pair[0]] -isnot [string] -or $Value[$pair[0]] -cne $captured[$pair[1]]){throw 'Alias context mismatch'}
        }
        $instant=[DateTimeOffset]::MinValue
        if($Value.ingressNonce -isnot [string] -or [string]::IsNullOrWhiteSpace($Value.ingressNonce) -or $Value.pendingId -cne ('pending-'+$Value.ingressNonce) -or $Value.requestId -isnot [string] -or [string]::IsNullOrWhiteSpace($Value.requestId) -or $Value.requestId.StartsWith('pending-') -or $Value.aliasedAtUtc -isnot [string] -or $Value.aliasedAtUtc -cnotmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z$' -or -not [DateTimeOffset]::TryParse($Value.aliasedAtUtc,[ref]$instant)){throw 'Invalid alias binding'}
        $checked=@{}
        foreach($field in @('workspacePath','agent','mcpSessionId','hostSessionKey','ingressNonce','pendingId','requestId','aliasedAtUtc')){$checked[$field]=$Value[$field]}
        return $checked
    }
    function Sync-OutboxAlias {
        $journalPath=Join-Path $directory 'sequence.yaml'
        if([IO.File]::Exists($journalPath)){
            $journal=Read-OutboxDocument $journalPath
            if($journal.Contains('evidence')){
                if($journal.evidence -isnot [Collections.IDictionary]){throw 'Invalid captured evidence journal'}
                $changed=$false
                foreach($binding in @($aliases)){
                    $logicalKey=Get-OutboxDigest $binding.pendingId
                    if(-not $journal.evidence.Contains($logicalKey)){continue}
                    foreach($audit in $journal.evidence[$logicalKey].expectedEvents){
                        if($audit.requestId -ceq $binding.pendingId){$audit.requestId=$binding.requestId;$changed=$true}
                        elseif($audit.requestId -cne $binding.requestId){throw 'Manifest alias conflicts with captured ownership'}
                    }
                }
                if($changed){Write-OutboxDocument $journalPath $journal}
            }
        }

        foreach($folder in @('pending','archive','quarantine','rejected')){
            foreach($file in @(Get-OutboxFile $folder)){
                if(-not $file.Name.EndsWith('.event.yaml')){continue}
                $matching=@($aliases|Where-Object {$file.Name.StartsWith((Get-OutboxDigest $_.pendingId)+'-')})
                if($matching.Count -eq 0){continue}
                $stored=Read-OutboxDocument $file.FullName
                if($stored.version -ne 1 -or -not (Test-OutboxEvent $stored.event) -or (Get-OutboxFilename $stored.event) -cne $file.Name){throw 'Invalid aliased record'}
                $request=Resolve-OutboxRequest $stored.event.requestId
                if($request -ceq $stored.event.requestId){continue}
                if($folder -cne 'pending' -or $stored.attempts -ne 0 -or $stored.receipt -or $stored.failure){throw 'Submitted evidence cannot rebind'}
                $stored.event.requestId=$request
                Write-OutboxDocument $file.FullName $stored
            }
        }
    }
    function Read-OutboxManifest {
        param([string]$RequestId)
        if([string]::IsNullOrWhiteSpace($RequestId)){throw 'Captured manifest request is required'}
        $journalPath=Join-Path $directory 'sequence.yaml'
        if(-not [IO.File]::Exists($journalPath)){return @{code='manifest_missing';manifest=$null}}
        $journal=Read-OutboxDocument $journalPath
        if($journal.version -ne 1 -or $journal.orders -isnot [Collections.IDictionary]){throw 'Invalid captured sequence'}
        $logicalKey=Get-OutboxDigest (Resolve-OutboxRequest $RequestId -Logical)
        if(-not $journal.Contains('evidence') -or -not $journal.evidence.Contains($logicalKey)){return @{code='manifest_missing';manifest=$null}}
        $manifest=$journal.evidence[$logicalKey]
        if($manifest -isnot [Collections.IDictionary] -or $manifest.expectedEvents -isnot [Collections.IList] -or
            $manifest.captureKeys -isnot [Collections.IDictionary] -or $manifest.legacyIncomplete -isnot [bool] -or
            $manifest.ingressEventId -isnot [string] -or $manifest.resultEventId -isnot [string] -or
            -not $journal.orders.Contains($logicalKey) -or -not (Test-OutboxInteger $journal.orders[$logicalKey] 1)){throw 'Invalid captured manifest'}
        foreach($audit in $manifest.expectedEvents){if(-not (Test-OutboxEvent $audit)){throw 'Invalid captured manifest event'}}
        return @{code='manifest';manifest=(Copy-OutboxValue $manifest);reservedOrder=$journal.orders[$logicalKey]}
    }

    try{
        if(-not [IO.Path]::IsPathRooted($RootDirectory)){throw 'Root must be absolute'}
        $captured=Copy-OutboxValue $Context
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey')){
            if($captured[$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($captured[$field])){throw 'Captured context is required'}
        }
        if(-not [IO.Path]::IsPathRooted($captured.workspacePath)){throw 'Workspace identity must be absolute'}
        $key=Get-OutboxDigest (ConvertTo-Json -InputObject @($captured.workspacePath,$captured.agentIdentity,$captured.mcpSessionId,$captured.hostSessionKey) -Compress)
        $directory=Join-Path $RootDirectory $key
        Assert-OutboxStoragePath $RootDirectory
        Assert-OutboxStoragePath $directory
        [void][IO.Directory]::CreateDirectory($directory)
        $lockPath=Join-Path $directory '.lock'
        Assert-OutboxStoragePath $lockPath
        $heldLock=[IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
        if($heldLock.Length -ne 0){throw 'Nonempty legacy lock requires explicit migration'}
        Assert-OutboxStorageTree $directory
        foreach($folder in @('','pending','archive','quarantine','rejected')){[void][IO.Directory]::CreateDirectory((Join-Path $directory $folder))}
        $aliasPath=Join-Path $directory 'applied-aliases.yaml'
        $aliases=@()
        if([IO.File]::Exists($aliasPath)){
            $journal=Read-OutboxDocument $aliasPath
            if($journal.version -ne 1 -or $journal.aliases -isnot [Collections.IList]){throw 'Invalid alias journal'}
            $aliases=@(foreach($item in $journal.aliases){Get-OutboxCheckedAlias $item})
            if(@($aliases.pendingId|Select-Object -Unique).Count -ne $aliases.Count -or @($aliases.requestId|Select-Object -Unique).Count -ne $aliases.Count){throw 'Ambiguous alias journal'}
        }
        Sync-OutboxAlias
        $sequencePath=Join-Path $directory 'sequence.yaml'
        if($Operation -ceq 'Resolve'){
            if([string]::IsNullOrWhiteSpace($InputObject.requestId)){throw 'Request id is required'}
            return @{code='resolved';requestId=(Resolve-OutboxRequest $InputObject.requestId)}
        }
        if($Operation -ceq 'ApplyAlias'){
            $binding=Get-OutboxCheckedAlias $InputObject
            if($InputObject.acks -isnot [array] -or @($InputObject.acks|Where-Object {$_ -isnot [string] -or [string]::IsNullOrWhiteSpace($_)}).Count){throw 'Invalid alias acknowledgments'}
            $previous=@($aliases|Where-Object {$_.pendingId -ceq $binding.pendingId -or $_.requestId -ceq $binding.requestId})
            if($previous.Count){
                foreach($field in $binding.Keys){if($previous[0][$field] -cne $binding[$field]){throw 'Contradictory alias'}}
                return @{code='unchanged'}
            }
            $canonicalKey=Get-OutboxDigest $binding.requestId
            if([IO.File]::Exists($sequencePath) -and (Read-OutboxDocument $sequencePath).orders.Contains($canonicalKey)){throw 'Independent canonical sequence'}
            foreach($folder in @('pending','archive','quarantine','rejected')){
                foreach($file in @(Get-OutboxFile $folder)){
                    if($file.Name.StartsWith($canonicalKey+'-')){throw 'Independent canonical evidence'}
                    if($file.Name.StartsWith((Get-OutboxDigest $binding.pendingId)+'-') -and $file.Name.EndsWith('.event.yaml')){
                        $stored=Read-OutboxDocument $file.FullName
                        if($folder -cne 'pending' -or -not (Test-OutboxEvent $stored.event) -or $stored.attempts -ne 0 -or $stored.receipt -or $stored.failure){throw 'Attempted evidence cannot alias'}
                    }
                }
            }
            $aliases=@($aliases)+@($binding)
            Write-OutboxDocument $aliasPath @{version=1;aliases=@($aliases)}
            Sync-OutboxAlias
            return @{code='applied'}
        }
        if($Operation -ceq 'ReadManifest'){return Read-OutboxManifest $InputObject.requestId}
        if($Operation -ceq 'CheckCapturedCompletion'){
            $authority=Read-OutboxManifest $InputObject.requestId
            if($authority.code -cne 'manifest' -or $authority.manifest.legacyIncomplete -or
                @($authority.manifest.expectedEvents).Count -ne $authority.reservedOrder){return @{allowed=$false;code='session_turn_completion_gate';detailCode='expected_set_missing';checkedEventIds=@()}}
            $manifest=$authority.manifest
            $InputObject=@{requestId=(Resolve-OutboxRequest $InputObject.requestId);expectedEvents=$manifest.expectedEvents;ingressEventId=$manifest.ingressEventId;resultEventId=$manifest.resultEventId;requiresResult=$InputObject.requiresResult}
        }

        if($Operation -cin @('CheckCompletion','CheckCapturedCompletion')){
            $checked=[Collections.Generic.List[string]]::new()
            function Get-OutboxCompletionRefusal {
                param([string]$Reason)
                return @{allowed=$false;code='session_turn_completion_gate';detailCode=$Reason;checkedEventIds=$checked.ToArray()}
            }
            $expected=@($InputObject.expectedEvents)
            if(-not $expected.Count -or [string]::IsNullOrWhiteSpace($InputObject.ingressEventId) -or
                $InputObject.requiresResult -isnot [bool] -or ($InputObject.requiresResult -and [string]::IsNullOrWhiteSpace($InputObject.resultEventId))){return Get-OutboxCompletionRefusal 'expected_set_missing'}
            $index=@{};$ordinal=1L
            foreach($audit in $expected){
                $uuid=[guid]::Empty
                if($audit -isnot [Collections.IDictionary] -or -not [guid]::TryParseExact($audit.eventId,'D',[ref]$uuid) -or $uuid -eq [guid]::Empty -or
                    $index.ContainsKey($audit.eventId) -or -not (Test-OutboxInteger $audit.order 1) -or $audit.order -ne $ordinal -or
                    $audit.workspacePath -cne $captured.workspacePath -or $audit.agentIdentity -cne $captured.agentIdentity -or
                    $audit.mcpSessionId -cne $captured.mcpSessionId -or $audit.requestId -cne $InputObject.requestId){return Get-OutboxCompletionRefusal 'expected_set_invalid'}
                $index[$audit.eventId]=$audit;$ordinal++
            }
            if(-not $index.ContainsKey($InputObject.ingressEventId) -or ($InputObject.requiresResult -and
                (-not $index.ContainsKey($InputObject.resultEventId) -or $InputObject.resultEventId -ceq $InputObject.ingressEventId))){return Get-OutboxCompletionRefusal 'expected_set_invalid'}
            if($Transport.read -isnot [scriptblock]){return Get-OutboxCompletionRefusal 'evidence_unavailable'}
            $prefix=(Get-OutboxDigest (Resolve-OutboxRequest $InputObject.requestId -Logical))+'-'
            & $publicationSource 'completion-enumerated' $prefix
            $files=@(foreach($folder in @('pending','archive','quarantine','rejected')){
                foreach($file in @(Get-OutboxFile $folder)){if($file.Name.StartsWith($prefix,[StringComparison]::Ordinal)){@{folder=$folder;file=$file}}}
            })
            if($files.Count -ne $expected.Count){return Get-OutboxCompletionRefusal 'retained_set_mismatch'}
            $retained=@{}
            foreach($entry in $files){
                & $publicationSource 'completion-read' $entry.file.FullName
                $stored=Read-OutboxDocument $entry.file.FullName
                $audit=$stored.event
                if($audit -isnot [Collections.IDictionary] -or -not $index.ContainsKey($audit.eventId) -or $retained.ContainsKey($audit.eventId)){return Get-OutboxCompletionRefusal 'retained_set_mismatch'}
                $original=$index[$audit.eventId]
                if(-not (Test-OutboxEvent $audit) -or $audit.order -ne $original.order -or $audit.kind -cne $original.kind -or
                    $audit.payloadJson -cne $original.payloadJson -or $audit.payloadHash -cne $original.payloadHash -or
                    $audit.requestId -cne $original.requestId){return Get-OutboxCompletionRefusal 'retained_event_mismatch'}
                if($entry.folder -cne 'archive'){return Get-OutboxCompletionRefusal 'mandatory_evidence_blocked'}
                if(-not (Test-OutboxReceipt $stored.receipt $original -Revision) -or $stored.receipt.code -cne 'persisted'){return Get-OutboxCompletionRefusal 'retained_receipt_mismatch'}
                $retained[$audit.eventId]=$stored
            }
            foreach($audit in $expected){
                $reply=& $Transport.read (Copy-OutboxValue $audit)
                if(-not (Test-OutboxReceipt $reply $audit -Revision) -or $reply.code -cne 'persisted' -or
                    $reply.revision -ne $retained[$audit.eventId].receipt.revision){return Get-OutboxCompletionRefusal 'server_readback_mismatch'}
                $checked.Add($audit.eventId)
            }
            return @{allowed=$true;code='verified';detailCode='';checkedEventIds=$checked.ToArray()}
        }
        if($Operation -ceq 'Enqueue'){
            if($InputObject.requestId -isnot [string] -or [string]::IsNullOrWhiteSpace($InputObject.requestId) -or $InputObject.kind -cnotin @('action','decision','dialog','metadata') -or $InputObject.payloadJson -isnot [string] -or (ConvertFrom-Json $InputObject.payloadJson -AsHashtable -NoEnumerate) -isnot [Collections.IDictionary]){throw 'Invalid audit capture'}
            $request=Resolve-OutboxRequest $InputObject.requestId
            $requestKey=Get-OutboxDigest (Resolve-OutboxRequest $request -Logical)
            $sequence=@{version=1;orders=@{}}
            if([IO.File]::Exists($sequencePath)){
                $sequence=Read-OutboxDocument $sequencePath
                if($sequence.version -ne 1 -or $sequence.orders -isnot [Collections.IDictionary]){throw 'Invalid durable sequence'}
                foreach($entry in $sequence.orders.Keys){if($entry -cnotmatch '^[a-f0-9]{64}$' -or -not (Test-OutboxInteger $sequence.orders[$entry])){throw 'Corrupt sequence entry'}}
            }elseif(@(foreach($folder in @('pending','archive','quarantine','rejected')){Get-OutboxFile $folder}).Count){throw 'Sequence missing beside evidence'}
            $next=if($sequence.orders.Contains($requestKey)){[long]$sequence.orders[$requestKey]+1}else{1L}
            if(-not (Test-OutboxInteger $next 1)){throw 'Sequence exhausted'}
            foreach($folder in @('pending','archive','quarantine','rejected')){
                foreach($file in @(Get-OutboxFile $folder)){
                    if(-not $file.Name.StartsWith($requestKey+'-') -or -not $file.Name.EndsWith('.event.yaml')){continue}
                    $match=[regex]::Match($file.Name,'^[a-f0-9]{64}-(\d{16})-[a-f0-9-]{36}\.event\.yaml$')
                    if(-not $match.Success -or [long]$match.Groups[1].Value -lt 1 -or [long]$match.Groups[1].Value -ge $next){throw 'Sequence rollback conflicts with retained evidence'}
                }
            }
            $role=if($InputObject.Contains('evidenceRole')){$InputObject.evidenceRole}else{''}
            $captureKey=if($InputObject.Contains('captureKey')){$InputObject.captureKey}else{''}
            if($role -isnot [string] -or $role -cnotin @('','ingress','result') -or $captureKey -isnot [string] -or
                ($InputObject.Contains('captureKey') -and [string]::IsNullOrWhiteSpace($captureKey))){throw 'Invalid trusted capture identity or role'}
            if(-not $sequence.Contains('evidence')){$sequence.evidence=@{}}
            if($sequence.evidence -isnot [Collections.IDictionary]){throw 'Invalid captured evidence journal'}
            if(-not $sequence.evidence.Contains($requestKey)){
                $sequence.evidence[$requestKey]=@{expectedEvents=@();ingressEventId='';resultEventId='';captureKeys=@{};legacyIncomplete=$sequence.orders.Contains($requestKey)}
            }else{
                $validation=Read-OutboxManifest $request
                if($validation.code -cne 'manifest'){throw 'Existing captured manifest is invalid'}
            }
            $manifest=$sequence.evidence[$requestKey]
            if($captureKey -and $manifest.captureKeys.Contains($captureKey)){
                $prior=$manifest.captureKeys[$captureKey]
                $original=@($manifest.expectedEvents|Where-Object eventId -CEQ $prior.eventId)
                if($original.Count -ne 1 -or $original[0].kind -cne $InputObject.kind -or $original[0].payloadJson -cne $InputObject.payloadJson -or $prior.role -cne $role){throw 'Capture identity conflicts with original evidence'}
                $existing=@(foreach($folder in @('pending','archive','quarantine','rejected')){
                    $candidate=Join-Path (Join-Path $directory $folder) (Get-OutboxFilename $original[0])
                    if([IO.File]::Exists($candidate)){$candidate}
                })
                if($existing.Count -gt 1){throw 'Duplicate retained capture identity'}
                if(-not $existing.Count){
                    Write-OutboxDocument (Join-Path (Join-Path $directory 'pending') (Get-OutboxFilename $original[0])) @{version=1;event=(Copy-OutboxValue $original[0]);attempts=0}
                }
                return @{code='queued';event=(Copy-OutboxValue $original[0])}
            }
            if(($role -ceq 'ingress' -and $manifest.ingressEventId) -or ($role -ceq 'result' -and $manifest.resultEventId)){throw 'Distinct capture cannot replace ingress or final result'}
            $audit=@{workspacePath=$captured.workspacePath;agentIdentity=$captured.agentIdentity;mcpSessionId=$captured.mcpSessionId;requestId=$request;eventId=[guid]::NewGuid().ToString('D');order=$next;kind=$InputObject.kind;payloadJson=$InputObject.payloadJson;payloadHash=(Get-OutboxDigest $InputObject.payloadJson)}
            $manifest.expectedEvents=@($manifest.expectedEvents)+@(Copy-OutboxValue $audit)
            if($role){$manifest[$role+'EventId']=$audit.eventId}
            if($captureKey){$manifest.captureKeys[$captureKey]=@{eventId=$audit.eventId;role=$role}}
            $sequence.orders[$requestKey]=$next
            Write-OutboxDocument $sequencePath $sequence

            Write-OutboxDocument (Join-Path (Join-Path $directory 'pending') (Get-OutboxFilename $audit)) @{version=1;event=$audit;attempts=0}
            return @{code='queued';event=(Copy-OutboxValue $audit)}
        }
        if($Transport.submit -isnot [scriptblock] -or $Transport.read -isnot [scriptblock]){throw 'Supported submit/read dependencies are required'}
        $receipts=[Collections.Generic.List[object]]::new();$persisted=0
        foreach($folder in @('','pending','archive','rejected')){
            foreach($file in @(Get-OutboxFile $folder)){if($file.Name.EndsWith('.tmp')){Complete-OutboxRecord $file 'quarantine'}}
        }
        if(@(Get-OutboxFile 'quarantine').Count -eq 0){
            foreach($file in @(Get-OutboxFile 'pending')){
                try{
                    $record=Read-OutboxDocument $file.FullName
                    if($record.version -ne 1 -or -not (Test-OutboxEvent $record.event) -or (Get-OutboxFilename $record.event) -cne $file.Name -or -not (Test-OutboxInteger $record.attempts 0 10) -or ($record.attempts -gt 0 -and -not (Test-OutboxInteger $record.nextAttemptAt))){throw 'Invalid stored event'}
                    if($record.failure){
                        $classified=Get-OutboxFailure $record.failure
                        if($record.failure.retryable -isnot [bool] -or $classified.detailCode -cne $record.failure.detailCode -or $classified.retryable -cne $record.failure.retryable){throw 'Invalid retained failure'}
                    }
                    if($record.receipt -and -not ((Test-OutboxTerminalReceipt $record.receipt $record.event) -or ((Test-OutboxReceipt $record.receipt $record.event -Revision) -and $record.receipt.code -ceq 'persisted'))){throw 'Invalid retained receipt'}
                }catch{Complete-OutboxRecord $file 'quarantine';break}
                if($record.event.requestId.StartsWith('pending-')){continue}
                if($record.failure -and -not $record.failure.retryable){Complete-OutboxRecord $file 'quarantine';break}
                if($record.receipt){
                    if($record.receipt.code -ceq 'persisted'){Complete-OutboxRecord $file 'archive';$persisted++}
                    else{Complete-OutboxRecord $file 'rejected'}
                    $receipts.Add($record.receipt);continue
                }
                $deadline=[long](& $clockSource)
                if($record.attempts -gt 0 -and $record.attempts -lt 10 -and $record.nextAttemptAt -gt $deadline){& $delaySource ($record.nextAttemptAt-$deadline)}
                $ack=$null
                while($record.attempts -lt 10){
                    $record.attempts++
                    $backoff=[long]@(1000,2000,4000,8000,16000,30000)[[Math]::Min($record.attempts-1,5)]
                    $record.nextAttemptAt=([long](& $clockSource))+$backoff
                    Write-OutboxDocument $file.FullName $record
                    try{
                        $reply=& $Transport.submit (Copy-OutboxValue $record.event)
                        if(-not (Test-OutboxReceipt $reply $record.event -Revision:($reply.code -cin @('persisted','unchanged')))){$ack=@{code='rejected';reason='receipt_mismatch'};break}
                        if($reply.code -ceq 'unchanged'){
                            $readback=& $Transport.read (Copy-OutboxValue $record.event)
                            if((Test-OutboxReceipt $readback $record.event -Revision) -and $readback.code -ceq 'persisted' -and $readback.revision -eq $reply.revision){$ack=$readback}else{$ack=@{code='rejected';reason='readback_mismatch'}}
                            break
                        }
                        if($reply.code -cne 'queued'){$ack=$reply;break}
                    }catch{
                        $record.failure=Get-OutboxFailure $_.Exception
                        Write-OutboxDocument $file.FullName $record
                        if(-not $record.failure.retryable){$ack=@{code='rejected';reason=$record.failure.detailCode};break}
                    }
                    if($record.attempts -lt 10){& $delaySource $backoff}
                }
                if($null -eq $ack){$ack=@{code='rejected';reason='retry_budget_exhausted'}}
                $receipts.Add($ack)
                if(Test-OutboxTerminalReceipt $ack $record.event){
                    $record.receipt=$ack;Write-OutboxDocument $file.FullName $record;Complete-OutboxRecord $file 'rejected';continue
                }
                if($ack.code -cne 'persisted'){Complete-OutboxRecord $file 'quarantine';break}
                $record.receipt=$ack;Write-OutboxDocument $file.FullName $record
                Complete-OutboxRecord $file 'archive';$persisted++
            }
        }
        return @{code='flushed';persisted=$persisted;pending=@(Get-OutboxFile 'pending').Count;quarantined=@(Get-OutboxFile 'quarantine').Count;rejected=@(Get-OutboxFile 'rejected').Count;receipts=@($receipts)}
    }catch{
        if($Operation -cin @('CheckCompletion','CheckCapturedCompletion')){return @{allowed=$false;code='session_turn_completion_gate';detailCode='evidence_unavailable';checkedEventIds=@();diagnostic=$_.Exception.ToString()}}
        return @{code='lost';reason='Audit persistence or replay requires explicit recovery'}
    }finally{
        if($heldLock){$heldLock.Dispose()}
    }
}
