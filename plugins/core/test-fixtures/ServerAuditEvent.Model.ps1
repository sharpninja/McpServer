# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent test-only transactional event model for FR-QB-003/005, TEST-QB-008/009/069.
#>
function Get-ServerAuditModel {
    [CmdletBinding()]
    param([string]$Fault = '')
    $modelFault = $Fault
    $corruptReceipt = {
        param($Receipt,[string]$Phase)
        $prefix = $Phase + '-revision-'
        if ($modelFault.StartsWith($prefix,[StringComparison]::Ordinal)) {
            switch ($modelFault.Substring($prefix.Length)) {
                'boolean-true' { $Receipt.revision = $true }
                'boolean-false' { $Receipt.revision = $false }
                'fraction' { $Receipt.revision = 1.5 }
                'negative-fraction' { $Receipt.revision = -1.5 }
                'decimal-fraction' { $Receipt.revision = [decimal]::Parse('1.0000000000000001',[Globalization.CultureInfo]::InvariantCulture) }
                'infinity' { $Receipt.revision = [double]::PositiveInfinity }
                'negative-infinity' { $Receipt.revision = [double]::NegativeInfinity }
                'nan' { $Receipt.revision = [double]::NaN }
                'string' { $Receipt.revision = '9' }
                'missing' { $null = $Receipt.Remove('revision') }
                'object' { $Receipt.revision = @{ value = 9 } }
                'date' { $Receipt.revision = [datetime]::UnixEpoch }
                'character' { $Receipt.revision = [char]57 }
                'zero' { $Receipt.revision = 0L }
                'negative' { $Receipt.revision = -9L }
            }
        }
        return $Receipt
    }.GetNewClosure()
    $state = @{ revision = 0L; events = @{}; rejected = @{}; turns = @{}; nextFailure = ''; trace = [Collections.Generic.List[object]]::new() }
    $copy = { param($Value) ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 100 -Compress) -AsHashtable -NoEnumerate -DateKind String }
    $dispatch = @{ invoke = $null }
    $invoke = {
        param([string]$Operation, [Collections.IDictionary]$InputObject)
        $state.trace.Add(@{ operation = $Operation; input = (& $copy $InputObject) })
        if ($Operation -ceq 'Configure') { $state.nextFailure = $InputObject.failAt; return @{ code = 'configured' } }
        if ($Operation -ceq 'ConcurrentSubmit') {
            $replies = [Collections.Generic.List[object]]::new()
            for ($contextIndex = 0; $contextIndex -lt $InputObject.events.Count; $contextIndex++) {
                $contextId = if ($modelFault -ceq 'one-context') { 'context-one' } else { 'context-' + $contextIndex }
                $state.trace.Add(@{ operation = 'OpenContext'; contextId = $contextId })
                $replies.Add((& $dispatch.invoke Submit $InputObject.events[$contextIndex]))
            }
            return @{ replies = $replies.ToArray() }
        }
        if ($Operation -ceq 'Reopen') {
            # An independent serialized store model simulates context disposal/reopen.
            # Actual Production adapters must reopen real storage, not use this model.
            $restored = & $copy $state
            $state.Clear()
            foreach ($field in $restored.Keys) { $state[$field] = $restored[$field] }
            $restoredTrace = [Collections.Generic.List[object]]::new()
            foreach ($entry in $state.trace) { $restoredTrace.Add($entry) }
            $state.trace = $restoredTrace
            if ($modelFault -ceq 'lose-receipt-on-reopen') { $state.events.Clear() }
            if ($modelFault -ceq 'lose-rejection-on-reopen') { $state.rejected.Clear() }
            return @{ code = 'reopened' }
        }
        $root = $InputObject.workspacePath + '|' + $InputObject.agentIdentity + '|' + $InputObject.mcpSessionId + '|' + $InputObject.requestId
        if ($Operation -ceq 'Seed') {
            $state.turns[$root] = @{ status = $InputObject.status; actions = @(); designDecisions = @(); processingDialog = @() }
            return @{ code = 'seeded' }
        }
        if ($Operation -ceq 'Snapshot') {
            $visible = & $copy $state
            foreach ($collection in @('events','rejected')) {
                foreach ($itemKey in @($visible[$collection].Keys)) {
                    if ($visible[$collection][$itemKey].event.workspacePath -cne $InputObject.workspacePath) { $null = $visible[$collection].Remove($itemKey) }
                }
            }
            foreach ($turnKey in @($visible.turns.Keys)) {
                if (-not $turnKey.StartsWith($InputObject.workspacePath + '|',[StringComparison]::Ordinal)) { $null = $visible.turns.Remove($turnKey) }
            }
            $visible.revision = if ($visible.events.Count) { ($visible.events.Values.receipt.revision | Measure-Object -Maximum).Maximum } else { 0L }
            return $visible
        }
        if ($Operation -ceq 'Complete') {
            return @{ allowed = (-not @($state.rejected.Values | Where-Object root -CEQ $root).Count) -or $modelFault -ceq 'ignore-rejection' }
        }
        $key = $root + '|' + $InputObject.eventId
        if ($Operation -ceq 'Read') {
            if (-not $state.events.ContainsKey($key)) {
                if ($modelFault -ceq 'leak-foreign-receipt') {
                    $foreignRead = @($state.events.Values | Where-Object { $_.event.eventId -ceq $InputObject.eventId })
                    if ($foreignRead.Count) { return & $copy $foreignRead[0].receipt }
                }
                return $null
            }
            $receipt = & $copy $state.events[$key].receipt
            $receipt.code = 'persisted'
            return & $corruptReceipt $receipt 'read'
        }
        if ($Operation -ceq 'Project') {
            $turn = $state.turns[$root]
            $rows = @($state.events.Values | Where-Object root -CEQ $root | Sort-Object { $_.event.order })
            $actions = @($turn.actions | Sort-Object order | ForEach-Object { $_.value })
            $decisions = @($turn.designDecisions | Sort-Object order | ForEach-Object { $_.value })
            $dialog = @($turn.processingDialog | Sort-Object order | ForEach-Object { $_.value })
            if ($modelFault -ceq 'reverse-projection') { [array]::Reverse($rows); [array]::Reverse($actions); [array]::Reverse($dialog) }
            return & $copy @{ status = $turn.status; actions = $actions; designDecisions = $decisions; processingDialog = $dialog; eventIds = @($rows | ForEach-Object { $_.event.eventId }) }
        }
        if ($Operation -cne 'Submit') { throw 'Unsupported test-model operation' }
        if (-not $state.turns.ContainsKey($root)) { return @{ code = 'rejected'; reason = 'request_unknown' } }
        if ($modelFault -cne 'accept-invalid') {
            $valid = $true
            foreach ($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','kind','payloadJson','payloadHash')) { if ($InputObject[$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($InputObject[$field])) { $valid = $false } }
            $parsedId = [guid]::Empty
            $valid = $valid -and [guid]::TryParseExact($InputObject.eventId,'D',[ref]$parsedId) -and $parsedId -ne [guid]::Empty
            $valid = $valid -and $InputObject.order -is [ValueType] -and $InputObject.order -isnot [bool] -and $InputObject.order -ge 1 -and $InputObject.order -le 9007199254740991L -and [decimal]$InputObject.order % 1 -eq 0
            $valid = $valid -and $InputObject.kind -cin @('action','decision','dialog','metadata')
            try { $body = ConvertFrom-Json $InputObject.payloadJson -AsHashtable -NoEnumerate -DateKind String; $valid = $valid -and $body -is [Collections.IDictionary] } catch { $valid = $false }
            if ($valid -and $InputObject.kind -ceq 'decision') { $valid = $body.conclusion -is [string] -and -not [string]::IsNullOrWhiteSpace($body.conclusion) -and $body.timestamp -is [string] -and -not [string]::IsNullOrWhiteSpace($body.timestamp) }
            if (-not $valid) { return @{ code = 'rejected'; reason = 'invalid_audit_event' } }
        }
        $failurePoint = $state.nextFailure; $state.nextFailure = ''
        $auditRecord = & $copy $InputObject
        if ($modelFault -ceq 'clip-order') { $auditRecord.order = [long][int]::MaxValue }
        $receipt = @{ code = 'persisted'; revision = 0L }
        foreach ($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')) { $receipt[$field] = $auditRecord[$field] }
        $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($auditRecord.payloadJson))).ToLowerInvariant()
        if ($auditRecord.payloadHash -cne $digest -and $modelFault -cne 'accept-hash-conflict') { $receipt.code = 'rejected'; $receipt.reason = 'payload_conflict'; return $receipt }
        $foreignOwners = @($state.events.GetEnumerator() | Where-Object { $_.Value.event.eventId -ceq $auditRecord.eventId -and $_.Value.root -cne $root })
        if ($modelFault -cne 'allow-rebind' -and $foreignOwners.Count) {
            if ($modelFault -ceq 'foreign-collision-storage-error') { throw 'scripted_cross_tenant_unique_constraint' }
            if ($modelFault -ceq 'erase-original-on-foreign-conflict') {
                foreach ($owner in $foreignOwners) {
                    $null = $state.events.Remove($owner.Key)
                    foreach ($channel in @('actions','designDecisions','processingDialog')) { $state.turns[$owner.Value.root][$channel] = @() }
                }
            }
            if ($modelFault -ceq 'foreign-projection-on-conflict') { $state.turns[$root].actions = & $copy $state.turns[$foreignOwners[0].Value.root].actions }
            if ($modelFault -ceq 'foreign-revision-on-conflict') { $receipt.revision = $foreignOwners[0].Value.receipt.revision }
            $receipt.code = 'rejected'; $receipt.reason = 'payload_conflict'; return $receipt
        }
        if ($modelFault -cne 'allow-order-collision' -and @($state.events.Values | Where-Object { $_.root -ceq $root -and $_.event.order -eq $auditRecord.order -and $_.event.eventId -cne $auditRecord.eventId }).Count) { $receipt.code = 'rejected'; $receipt.reason = 'payload_conflict'; return $receipt }
        if ($state.events.ContainsKey($key) -and $modelFault -cnotin @('duplicate-application','racy-duplicate')) {
            $stored = $state.events[$key]
            $same = $true
            foreach ($field in @('payloadHash','payloadJson','order','kind')) { if ($stored.event[$field] -cne $auditRecord[$field]) { $same = $false } }
            if (-not $same -and $modelFault -cne 'overwrite-conflict') { $receipt.code = 'rejected'; $receipt.reason = 'payload_conflict'; return $receipt }
            if ($same) { $receipt = & $copy $stored.receipt; $receipt.code = 'unchanged'; return & $corruptReceipt $receipt 'replay' }
        }
        if ($state.turns[$root].status -cin @('completed','canceled','failed') -and $modelFault -cne 'accept-terminal') {
            $receipt.code = 'rejected'; $receipt.reason = 'skipped_terminal'; $receipt.storedStatus = $state.turns[$root].status
            $state.rejected[$key] = @{ root = $root; event = $auditRecord; receipt = $receipt }
            return & $copy $receipt
        }
        if ($failurePoint -ceq 'before-commit') {
            if ($modelFault -ceq 'leak-before-commit') { $state.events[$key] = @{ root = $root; event = $auditRecord; receipt = $receipt } }
            throw 'scripted_transport_timeout_before_commit'
        }
        if ($modelFault -ceq 'content-dedup' -and @($state.events.Values | Where-Object { $_.root -ceq $root -and $_.event.payloadHash -ceq $auditRecord.payloadHash }).Count) {
            $receipt.code = 'unchanged'; $receipt.revision = $state.revision
            return $receipt
        }
        $pendingTurn = & $copy $state.turns[$root]
        $payload = ConvertFrom-Json $auditRecord.payloadJson -AsHashtable -DateKind String
        if ($auditRecord.kind -ceq 'action') {
            $pendingTurn.actions += @{ order = $auditRecord.order; eventId = $auditRecord.eventId; value = $payload }
        } elseif ($auditRecord.kind -ceq 'dialog') {
            $pendingTurn.processingDialog += @{ order = $auditRecord.order; eventId = $auditRecord.eventId; value = $payload }
        } elseif ($auditRecord.kind -ceq 'decision') {
            $stages = @(
                @{ channel = 'designDecisions'; point = 'after-decision-field'; fault = 'drop-decision-field'; value = $payload.conclusion },
                @{ channel = 'processingDialog'; point = 'after-decision-dialog'; fault = 'drop-decision-dialog'; value = @{ timestamp = $payload.timestamp; role = 'model'; category = 'decision'; content = $payload.conclusion; eventId = $auditRecord.eventId } },
                @{ channel = 'actions'; point = 'after-decision-action'; fault = 'drop-decision-action'; value = @{ order = $auditRecord.order; type = 'design_decision'; description = $payload.conclusion; status = 'completed'; eventId = $auditRecord.eventId } }
            )
            foreach ($stage in $stages) {
                if ($modelFault -cne $stage.fault) {
                    $pendingTurn[$stage.channel] += @{ order = $auditRecord.order; eventId = $auditRecord.eventId; value = $stage.value }
                }
                $state.trace.Add(@{ operation = 'Stage'; channel = $stage.channel; eventId = $auditRecord.eventId })
                if ($failurePoint -ceq $stage.point) {
                    if ($modelFault -ceq 'leak-decision-stage') { $state.turns[$root] = $pendingTurn }
                    $state.trace.Add(@{ operation = 'Rollback'; point = $stage.point; eventId = $auditRecord.eventId })
                    throw ('scripted_storage_failure_' + $stage.point.Replace('-','_'))
                }
            }
        }
        $state.revision++
        $receipt.revision = $state.revision
        if ($modelFault -cin @('duplicate-application','racy-duplicate') -and $state.events.ContainsKey($key)) { $key += '|duplicate' }
        $state.turns[$root] = $pendingTurn
        if ($modelFault -ceq 'normalize-captured-root') { $auditRecord.workspacePath = $auditRecord.workspacePath.Replace('\','/'); $receipt.workspacePath = $auditRecord.workspacePath }
        if ($modelFault -ceq 'reformat-captured-payload') { $auditRecord.payloadJson = ConvertTo-Json -InputObject (ConvertFrom-Json $auditRecord.payloadJson -AsHashtable -DateKind String) -Depth 30 -Compress }
        $state.events[$key] = @{ root = $root; event = $auditRecord; receipt = (& $copy $receipt) }
        $state.trace.Add(@{ operation = 'Commit'; eventId = $auditRecord.eventId; revision = $state.revision })
        if ($failurePoint -ceq 'after-commit') { throw 'scripted_transport_timeout_after_commit' }
        if ($modelFault -ceq 'wrong-revision') { $receipt.revision = 0L }
        if ($modelFault -ceq 'wrong-root') { $receipt.requestId = 'foreign-request' }
        if ($modelFault -ceq 'wrong-hash') { $receipt.payloadHash = 'wrong' }
        return & $corruptReceipt (& $copy $receipt) 'submit'
    }.GetNewClosure()
    $dispatch.invoke = $invoke
    return @{ invoke = $invoke; state = $state }
}



