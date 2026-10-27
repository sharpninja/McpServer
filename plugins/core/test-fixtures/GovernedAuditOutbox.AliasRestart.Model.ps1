# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent, test-only executable outbox model for AC-QB-008/009/069.
.DESCRIPTION
Implements the frozen operation seam over disposable fixture documents.
It has no case names or expected summaries. The shared scenario driver observes
its records, actual injected transport calls, clock, sleeps and publications.
This deliberately simple model is not shipped, imported or called by production.
This derivative adds independent atomic fixture publication and receipt restart recovery.
The released sequential model stays unchanged. No production module is imported.
#>
function Invoke-MockAliasRestartAuditOutbox {
    [CmdletBinding()]
    param(
        [string]$RootDirectory,[hashtable]$Context,
        [ValidateSet('Enqueue','ApplyAlias','Resolve','Flush')][string]$Operation,
        [hashtable]$InputObject,[hashtable]$Transport,[scriptblock]$Clock,
        [scriptblock]$Sleep,[scriptblock]$PublicationObserver,
        [string]$Mutation=''
    )
    $publicationSource=$PublicationObserver
    function Get-ModelDigest {
        param([string]$Text)
        return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text))).ToLowerInvariant()
    }
    function Copy-ModelValue {
        param([object]$Value)
        return ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 50 -Compress) -AsHashtable -DateKind String -NoEnumerate
    }
    function Read-ModelDocument {
        param([string]$Path)
        return ConvertFrom-Yaml ([IO.File]::ReadAllText($Path))
    }
    function Save-ModelDocument {
        param([string]$Path,[object]$Value)
        # Independent test-only damaged storage effects for the common alias assertions.
        if($Value -is [Collections.IDictionary] -and $Value.Contains('event') -and $Value.event.requestId -ceq 'req-alias-minted'){
            if($Mutation -ceq 'damage-alias-payload'){$Value.event.payloadJson=(ConvertTo-Json -InputObject @{text='damaged alias payload'} -Compress);$Value.event.payloadHash=Get-ModelDigest $Value.event.payloadJson}
            if($Mutation -ceq 'reset-alias-order'){$Value.event.order=1L}
            if($Mutation -ceq 'change-alias-event'){$Value.event.eventId=[guid]::NewGuid().ToString('D')}
        }
        $scratch=$Path+'.fixture.tmp'
        $writer=[IO.StreamWriter]::new($scratch,$false,[Text.UTF8Encoding]::new($false))
        try{
            $writer.Write((ConvertTo-Json -InputObject $Value -Depth 60 -Compress))
            $writer.Flush()
            $writer.BaseStream.Flush($true)
        }finally{$writer.Dispose()}
        if($Mutation -cne 'omit-publication'){& $publicationSource 'staged' $Path}
        [IO.File]::Move($scratch,$Path,$true)
        if($Mutation -cne 'omit-publication'){& $publicationSource 'published' $Path}
    }
    function Get-ModelFile {
        param([string]$Folder)
        return @(Get-ChildItem (Join-Path $directory $Folder) -File | Sort-Object Name)
    }
    function Resolve-ModelId {
        param([string]$Id,[switch]$Logical)
        foreach($binding in @($aliases)){
            if($Logical -and $binding.requestId -ceq $Id){return $binding.pendingId}
            if(-not $Logical -and $binding.pendingId -ceq $Id){return $binding.requestId}
        }
        return $Id
    }
    function Get-ModelName {
        param([hashtable]$AuditRecord)
        return (Get-ModelDigest (Resolve-ModelId $AuditRecord.requestId -Logical))+'-'+([long]$AuditRecord.order).ToString('D16')+'-'+$AuditRecord.eventId+'.event.yaml'
    }
    function Test-ModelEvent {
        param([object]$AuditRecord)
        if($AuditRecord -isnot [Collections.IDictionary]){return $false}
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId')){
            if($AuditRecord[$field] -cne $Context[$field]){return $false}
        }
        $guidValue=[guid]::Empty
        if(-not [guid]::TryParseExact($AuditRecord.eventId,'D',[ref]$guidValue) -or $guidValue -eq [guid]::Empty){return $false}
        if($AuditRecord.order -isnot [ValueType] -or $AuditRecord.order -lt 1 -or $AuditRecord.order -gt 9007199254740991 -or [decimal]$AuditRecord.order % 1 -ne 0){return $false}
        if($AuditRecord.kind -cnotin @('action','decision','dialog','metadata') -or [string]::IsNullOrWhiteSpace($AuditRecord.requestId)){return $false}
        if($AuditRecord.payloadJson -isnot [string] -or $AuditRecord.payloadHash -cne (Get-ModelDigest $AuditRecord.payloadJson)){return $false}
        try{return (ConvertFrom-Json $AuditRecord.payloadJson -AsHashtable -NoEnumerate) -is [Collections.IDictionary]}catch{return $false}
    }
    function Test-ModelReceipt {
        param([object]$Reply,[hashtable]$AuditRecord,[switch]$Revision)
        if($Reply -isnot [Collections.IDictionary] -or $Reply.code -cnotin @('persisted','queued','unchanged','rejected','lost')){return $false}
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){
            if($Reply[$field] -cne $AuditRecord[$field]){return $false}
        }
        if($Revision -and ($Reply.revision -isnot [ValueType] -or $Reply.revision -lt 1 -or $Reply.revision -gt 9007199254740991 -or [decimal]$Reply.revision % 1 -ne 0)){return $false}
        return $true
    }
    function Get-ModelFailure {
        param([object]$Failure)
        $detail='unknown_error';$flag=$null;$reported=$null
        for($depth=0;$null -ne $Failure -and $depth -lt 8;$depth++){
            $data=if($Failure -is [Collections.IDictionary]){$Failure}else{$Failure.Data}
            if($data -and ($data.Contains('code') -or $data.Contains('detailCode'))){
                $detail=if($data.Contains('detailCode')){$data.detailCode}else{$data.code}
                $flag=$data.retryable;$reported=if($data.Contains('reportedRetryable')){$data.reportedRetryable}else{$flag}
                break
            }
            $Failure=$Failure.InnerException
        }
        if($detail -isnot [string] -or $detail -cnotmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$'){return @{detailCode='unknown_error';retryable=$false}}
        $transportCodes=@('transport_unavailable','transport_timeout','ECONNRESET','ECONNREFUSED','ETIMEDOUT','EPIPE','EHOSTUNREACH','ENETUNREACH','EAI_AGAIN')
        $persistenceCodes=@('backend_unavailable','persistence_error','timeout')
        $retry=($null -eq $flag -or $flag -is [bool]) -and ($null -eq $reported -or $reported -is [bool]) -and (($detail -cin $transportCodes -and $flag -ne $false) -or ($detail -cin $persistenceCodes -and $flag -eq $true))
        $result=@{detailCode=$detail;retryable=[bool]$retry}
        if($reported -is [bool]){$result.reportedRetryable=$reported}
        return $result
    }
    function Complete-ModelRecord {
        param([IO.FileInfo]$File,[string]$Folder)
        $target=Join-Path (Join-Path $directory $Folder) $File.Name
        & $publicationSource 'retiring' $target
        [IO.File]::Move($File.FullName,$target)
    }
    function Assert-ModelAlias {
        param([hashtable]$Binding)
        foreach($pair in @(@('workspacePath','workspacePath'),@('agent','agentIdentity'),@('mcpSessionId','mcpSessionId'),@('hostSessionKey','hostSessionKey'))){
            if($Binding[$pair[0]] -cne $Context[$pair[1]]){throw 'Foreign alias'}
        }
        $parsedDate=[DateTimeOffset]::MinValue
        if([string]::IsNullOrWhiteSpace($Binding.ingressNonce) -or $Binding.pendingId -cne ('pending-'+$Binding.ingressNonce) -or [string]::IsNullOrWhiteSpace($Binding.requestId) -or $Binding.requestId.StartsWith('pending-') -or $Binding.aliasedAtUtc -cnotmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z$' -or -not [DateTimeOffset]::TryParse($Binding.aliasedAtUtc,[ref]$parsedDate)){throw 'Invalid alias'}
    }
    try{
        $identity=@($Context.workspacePath,$Context.agentIdentity,$Context.mcpSessionId,$Context.hostSessionKey)
        $directory=Join-Path $RootDirectory (Get-ModelDigest (ConvertTo-Json -InputObject $identity -Compress))
        foreach($folder in @('','pending','archive','quarantine','rejected')){[void][IO.Directory]::CreateDirectory((Join-Path $directory $folder))}
        $aliasPath=Join-Path $directory 'applied-aliases.yaml'
        $aliases=if(Test-Path $aliasPath){@((Read-ModelDocument $aliasPath).aliases)}else{@()}
        foreach($binding in @($aliases)){Assert-ModelAlias $binding}
        # Replay a previously committed alias before any subsequent operation.
        foreach($folder in @('pending','archive','quarantine','rejected')){
            foreach($file in @(Get-ModelFile $folder)){
                if($file.Name -notlike '*.event.yaml'){continue}
                $binding=@($aliases|Where-Object {$file.Name.StartsWith((Get-ModelDigest $_.pendingId)+'-')})|Select-Object -First 1
                if($null -eq $binding){continue}
                $stored=Read-ModelDocument $file.FullName
                if(-not (Test-ModelEvent $stored.event)){throw 'Invalid aliased evidence'}
                if($stored.event.requestId -ceq $binding.pendingId){
                    if($folder -cne 'pending' -or $stored.attempts -ne 0 -or $stored.receipt -or $stored.failure){throw 'Attempted evidence cannot rebind'}
                    $stored.event.requestId=$binding.requestId
                    Save-ModelDocument $file.FullName $stored
                }
            }
        }
        $sequencePath=Join-Path $directory 'sequence.yaml'
        if($Operation -eq 'Resolve'){return @{code='resolved';requestId=(Resolve-ModelId $InputObject.requestId)}}
        if($Operation -eq 'ApplyAlias'){
            Assert-ModelAlias $InputObject
            if($InputObject.acks -isnot [array]){throw 'Invalid consumer acknowledgments'}
            $binding=@{};foreach($field in @('workspacePath','agent','mcpSessionId','hostSessionKey','ingressNonce','pendingId','requestId','aliasedAtUtc')){$binding[$field]=$InputObject[$field]}
            $existing=@($aliases|Where-Object {$_.pendingId -ceq $binding.pendingId -or $_.requestId -ceq $binding.requestId})
            if($existing.Count){
                foreach($field in $binding.Keys){if($existing[0][$field] -cne $binding[$field]){throw 'Conflicting alias'}}
                return @{code='unchanged'}
            }
            $canonicalKey=Get-ModelDigest $binding.requestId
            if((Test-Path $sequencePath) -and (Read-ModelDocument $sequencePath).orders.Contains($canonicalKey)){throw 'Independent canonical sequence'}
            foreach($folder in @('pending','archive','quarantine','rejected')){
                foreach($file in @(Get-ModelFile $folder)){
                    if($file.Name.StartsWith($canonicalKey+'-')){throw 'Independent canonical evidence'}
                    if($file.Name.StartsWith((Get-ModelDigest $binding.pendingId)+'-')){
                        $stored=Read-ModelDocument $file.FullName
                        if($folder -cne 'pending' -or $stored.attempts -ne 0 -or $stored.receipt -or $stored.failure){throw 'Attempted evidence'}
                    }
                }
            }
            if($Mutation -ceq 'omit-alias'){return @{code='applied'}}
            $aliases=@($aliases)+@($binding)
            Save-ModelDocument $aliasPath @{version=1;aliases=@($aliases)}
            foreach($file in @(Get-ModelFile 'pending')){
                $stored=Read-ModelDocument $file.FullName
                if($stored.event.requestId -ceq $binding.pendingId){$stored.event.requestId=$binding.requestId;Save-ModelDocument $file.FullName $stored}
            }
            return @{code='applied'}
        }
        if($Operation -eq 'Enqueue'){
            $request=Resolve-ModelId $InputObject.requestId
            $payload=ConvertFrom-Json $InputObject.payloadJson -AsHashtable -NoEnumerate
            if($payload -isnot [Collections.IDictionary]){throw 'Payload must be object'}
            $key=Get-ModelDigest (Resolve-ModelId $request -Logical)
            $sequence=if(Test-Path $sequencePath){Read-ModelDocument $sequencePath}else{@{version=1;orders=@{}}}
            $previous=if($sequence.orders.Contains($key)){[long]$sequence.orders[$key]}else{0L}
            foreach($folder in @('pending','archive','quarantine','rejected')){
                foreach($file in @(Get-ModelFile $folder)){
                    if($file.Name.StartsWith($key+'-') -and $file.Name.EndsWith('.event.yaml')){
                        $match=[regex]::Match($file.Name,'^[a-f0-9]{64}-(\d{16})-[a-f0-9-]{36}\.event\.yaml$')
                        if($Mutation -cne 'ignore-rollback' -and (-not $match.Success -or [long]$match.Groups[1].Value -gt $previous)){throw 'Sequence rollback'}
                    }
                }
            }
            $sequence.orders[$key]=$previous+1
            Save-ModelDocument $sequencePath $sequence
            $audit=@{workspacePath=$Context.workspacePath;agentIdentity=$Context.agentIdentity;mcpSessionId=$Context.mcpSessionId;requestId=$request;eventId=[guid]::NewGuid().ToString('D');order=($previous+1);kind=$InputObject.kind;payloadJson=$InputObject.payloadJson;payloadHash=(Get-ModelDigest $InputObject.payloadJson)}
            $target=Join-Path (Join-Path $directory 'pending') (Get-ModelName $audit)
            if($Mutation -cne 'omit-capture'){
                Save-ModelDocument $target @{version=1;event=$audit;attempts=0}
            }
            return @{code='queued';event=(Copy-ModelValue $audit)}
        }
        $receipts=[Collections.Generic.List[object]]::new();$persisted=0
        foreach($folder in @('','pending','archive','rejected')){
            foreach($file in @(Get-ModelFile $folder)){if($file.Name.EndsWith('.tmp')){Complete-ModelRecord $file 'quarantine'}}
        }
        if(@(Get-ModelFile 'quarantine').Count -eq 0){
            $pending=@(Get-ModelFile 'pending')
            if($Mutation -ceq 'reverse-replay'){[array]::Reverse($pending)}
            foreach($file in $pending){
                try{
                    $stored=Read-ModelDocument $file.FullName
                    if($stored.version -ne 1 -or -not (Test-ModelEvent $stored.event) -or (Get-ModelName $stored.event) -cne $file.Name -or $stored.attempts -lt 0 -or $stored.attempts -gt 10){throw 'Corrupt stored event'}
                }catch{Complete-ModelRecord $file 'quarantine';break}
                if($stored.event.requestId.StartsWith('pending-')){continue}
                if($stored.failure -and -not $stored.failure.retryable){Complete-ModelRecord $file 'quarantine';break}
                if($stored.receipt){
                    if($Mutation -ceq 'forget-receipt'){$stored.Remove('receipt')}
                    else{
                        if((Test-ModelReceipt $stored.receipt $stored.event -Revision) -and $stored.receipt.code -ceq 'persisted'){
                            Complete-ModelRecord $file 'archive';$receipts.Add($stored.receipt);$persisted++;continue
                        }
                        if((Test-ModelReceipt $stored.receipt $stored.event) -and $stored.receipt.code -ceq 'rejected' -and $stored.receipt.reason -ceq 'skipped_terminal'){
                            Complete-ModelRecord $file 'rejected';$receipts.Add($stored.receipt);continue
                        }
                        Complete-ModelRecord $file 'quarantine';break
                    }
                }
                if($stored.attempts -gt 0 -and $stored.attempts -lt 10 -and $stored.nextAttemptAt -gt (& $Clock)){& $Sleep ($stored.nextAttemptAt-(& $Clock))}
                $ack=$null
                while($stored.attempts -lt 10){
                    $stored.attempts++
                    $delay=[long]@(1000,2000,4000,8000,16000,30000)[[Math]::Min($stored.attempts-1,5)]
                    $stored.nextAttemptAt=if($Mutation -ceq 'omit-deadline'){0}else{(& $Clock)+$delay}
                    Save-ModelDocument $file.FullName $stored
                    try{
                        $reply=& $Transport.submit (Copy-ModelValue $stored.event)
                        if(-not (Test-ModelReceipt $reply $stored.event -Revision:($reply.code -cin @('persisted','unchanged')))){$ack=@{code='rejected';reason='receipt_mismatch'};break}
                        if($reply.code -ceq 'unchanged'){
                            $readback=& $Transport.read (Copy-ModelValue $stored.event)
                            if((Test-ModelReceipt $readback $stored.event -Revision) -and $readback.code -ceq 'persisted' -and $readback.revision -eq $reply.revision){$ack=$readback}else{$ack=@{code='rejected';reason='readback_mismatch'}}
                            break
                        }
                        if($reply.code -cne 'queued'){$ack=$reply;break}
                    }catch{
                        $stored.failure=Get-ModelFailure $_.Exception
                        Save-ModelDocument $file.FullName $stored
                        if(-not $stored.failure.retryable){$ack=@{code='rejected';reason=$stored.failure.detailCode};break}
                    }
                    if($stored.attempts -lt 10 -and $Mutation -cne 'omit-sleep'){& $Sleep $delay}
                }
                if($null -eq $ack){$ack=@{code='rejected';reason='retry_exhausted'}}
                $receipts.Add($ack)
                if($ack.code -ceq 'rejected' -and $ack.reason -ceq 'skipped_terminal' -and (Test-ModelReceipt $ack $stored.event) -and -not [string]::IsNullOrWhiteSpace($ack.storedStatus)){
                    $stored.receipt=$ack;Save-ModelDocument $file.FullName $stored;Complete-ModelRecord $file 'rejected';continue
                }
                if($ack.code -cne 'persisted'){Complete-ModelRecord $file 'quarantine';break}
                $stored.receipt=$ack;Save-ModelDocument $file.FullName $stored;Complete-ModelRecord $file 'archive';$persisted++
            }
        }
        return @{code='flushed';persisted=$persisted;pending=@(Get-ModelFile 'pending').Count;quarantined=@(Get-ModelFile 'quarantine').Count;rejected=@(Get-ModelFile 'rejected').Count;receipts=@($receipts)}
    }catch{
        return @{code='lost';reason='Model dependency operation interrupted or invalid'}
    }
}
