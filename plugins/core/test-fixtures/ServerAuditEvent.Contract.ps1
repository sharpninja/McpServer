# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Same requirement-derived event interactions/assertions for independent model and real service adapters.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode = 'Mock',
    [Parameter(Mandatory)][string]$ReceiptPath,
    [scriptblock]$ProviderFactory
)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $ReceiptPath) { throw 'Chronological receipt already exists' }
. (Join-Path $PSScriptRoot 'ServerAuditEvent.Model.ps1')
if ($Mode -ceq 'Production' -and -not $ProviderFactory) { throw 'Real service adapter required; no mock fallback' }
function Assert-ServerAuditValue {
    param([bool]$Condition,[string]$Message)
    $script:serverAuditChecks++
    if (-not $Condition) { throw ('audit_assert:' + $Message) }
}
function Get-ServerAuditInput {
    param([string]$Id,[long]$Order = 1,[string]$Kind = 'action',[string]$Request = 'req-20260101T000000Z-a')
    $payload = if ($Kind -ceq 'decision') { [ordered]@{ conclusion = 'Preserve distinct events'; timestamp = '2026-01-01T00:00:01.0000000Z' } }
        elseif ($Kind -ceq 'dialog') { [ordered]@{ category = 'tool_result'; role = 'tool'; content = "exact Unicode $([char]0x03A9)\u2014`r`n"; timestamp = '2026-01-01T00:00:01.0000000Z' } }
        else { [ordered]@{ type = 'tool_execution'; description = 'same action'; status = 'completed'; order = $Order; filePath = 'F:/fixture/a.cs' } }
    $json = ConvertTo-Json -InputObject $payload -Depth 20 -Compress
    return @{ workspacePath = 'F:/fixture/qb'; agentIdentity = 'Codex'; mcpSessionId = 'Codex-20260101T000000Z-fixture'; requestId = $Request; eventId = $Id; order = $Order; kind = $Kind; payloadJson = $json; payloadHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($json))).ToLowerInvariant() }
}
function Assert-ServerAuditReceipt {
    param($Receipt,$AuditInput,[string]$Code)
    Assert-ServerAuditValue ($null -ne $Receipt -and $Receipt.code -ceq $Code) ('receipt code:' + $Code)
    foreach ($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')) {
        Assert-ServerAuditValue ($Receipt[$field] -ceq $AuditInput[$field]) ('receipt root/effect:' + $field)
    }
    if ($Code -cin @('persisted','unchanged')) {
        $revision = $Receipt.revision
        $numeric = $null -ne $revision -and [Type]::GetTypeCode($revision.GetType()) -in @([TypeCode]::Byte,[TypeCode]::SByte,[TypeCode]::Int16,[TypeCode]::UInt16,[TypeCode]::Int32,[TypeCode]::UInt32,[TypeCode]::Int64,[TypeCode]::UInt64,[TypeCode]::Single,[TypeCode]::Double,[TypeCode]::Decimal)
        $whole = $false
        if ($numeric) {
            $whole = if ($revision -is [double] -or $revision -is [single]) { [double]::IsFinite([double]$revision) -and [Math]::Truncate([double]$revision) -eq [double]$revision } else { [decimal]$revision % 1 -eq 0 }
        }
        Assert-ServerAuditValue ($numeric -and $whole -and $revision -gt 0) 'positive finite integral committed revision'
    }
}
function Invoke-ServerAuditCase {
    param([string]$Name,[string]$Fault = '')
    $provider = if ($Mode -ceq 'Mock') { Get-ServerAuditModel -Fault $Fault } else { & $ProviderFactory $Fault }
    $invoke = $provider.invoke
    $a = Get-ServerAuditInput '11111111-1111-4111-8111-111111111111'
    $b = Get-ServerAuditInput '22222222-2222-4222-8222-222222222222' 2
    if ($Name -ceq 'exact-captured-envelope') {
        $a.workspacePath = 'F:\fixture\qb'
        $capturedBody = ConvertFrom-Json $a.payloadJson -AsHashtable -DateKind String
        $capturedBody.description = "exact $([char]0x03A9)\u2014`r`n"
        $a.payloadJson = "`r`n " + (ConvertTo-Json -InputObject $capturedBody -Compress) + "`r`n"
        $a.payloadHash = [Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($a.payloadJson)))
    }
    $root = @{ workspacePath = $a.workspacePath; agentIdentity = $a.agentIdentity; mcpSessionId = $a.mcpSessionId; requestId = $a.requestId; status = 'in_progress' }
    if ($Name -cin @('terminal-retention','rejection-reopen')) { $root.status = 'completed' }
    $null = & $invoke Seed $root
    switch ($Name) {
        'exact-captured-envelope' {
            $first = & $invoke Submit $a
            Assert-ServerAuditReceipt $first $a 'persisted'
            $null = & $invoke Reopen $root
            $read = & $invoke Read $a
            Assert-ServerAuditReceipt $read $a 'persisted'
            Assert-ServerAuditValue ($read.revision -eq $first.revision) 'raw captured root retains original revision after reopen'
            $stored = & $invoke Snapshot $root
            $original = @($stored.events.Values | Where-Object { $_.event.eventId -ceq $a.eventId })
            Assert-ServerAuditValue ($original.Count -eq 1) 'one exact immutable captured envelope'
            Assert-ServerAuditValue ($original[0].event.workspacePath -ceq $a.workspacePath) 'captured spelling independent of canonical tenancy'
            Assert-ServerAuditValue ($original[0].event.payloadJson -ceq $a.payloadJson) 'captured JSON whitespace and Unicode bytes preserved'
            Assert-ServerAuditReceipt (& $invoke Submit $a) $a 'unchanged'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1) 'exact captured root replay has one projection'
        }
        'safe-order-boundary' {
            $a = Get-ServerAuditInput $a.eventId 9007199254740991L
            Assert-ServerAuditReceipt (& $invoke Submit $a) $a 'persisted'
            Assert-ServerAuditReceipt (& $invoke Read $a) $a 'persisted'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1 -and $view.actions[0].order -eq 9007199254740991L) 'maximum captured order survives stored projection'
        }
        'roundtrip' {
            $receipt = & $invoke Submit $a
            Assert-ServerAuditReceipt $receipt $a 'persisted'
            $read = & $invoke Read $a
            Assert-ServerAuditReceipt $read $a 'persisted'
            Assert-ServerAuditValue ($read.revision -eq $receipt.revision) 'same committed revision'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1 -and $view.actions[0].description -ceq 'same action') 'action stored'
            Assert-ServerAuditValue ($view.designDecisions.Count -eq 0 -and $view.processingDialog.Count -eq 0) 'action never implies decision'
        }
        'redelivery' {
            $first = & $invoke Submit $a
            $again = & $invoke Submit $a
            Assert-ServerAuditReceipt $first $a 'persisted'
            Assert-ServerAuditReceipt $again $a 'unchanged'
            Assert-ServerAuditValue ($again.revision -eq $first.revision) 'redelivery reuses committed revision'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1) 'one logical event'
        }
        'equal-content-distinct' {
            $b.payloadJson = $a.payloadJson; $b.payloadHash = $a.payloadHash
            $first = & $invoke Submit $a
            $second = & $invoke Submit $b
            Assert-ServerAuditReceipt $first $a 'persisted'
            Assert-ServerAuditReceipt $second $b 'persisted'
            Assert-ServerAuditValue ($second.revision -gt $first.revision) 'distinct committed revision'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 2 -and $view.eventIds[0] -ceq $a.eventId -and $view.eventIds[1] -ceq $b.eventId) 'equal content has distinct event identity'
        }
        'payload-conflict' {
            $first = & $invoke Submit $a
            $changed = Get-ServerAuditInput $a.eventId 1 'decision'
            $conflict = & $invoke Submit $changed
            Assert-ServerAuditReceipt $conflict $changed 'rejected'
            Assert-ServerAuditValue ($conflict.reason -ceq 'payload_conflict') 'conflict classification'
            $read = & $invoke Read $a
            Assert-ServerAuditValue ($read.payloadHash -ceq $a.payloadHash -and $read.revision -eq $first.revision) 'conflict preserves original'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1 -and $view.designDecisions.Count -eq 0) 'conflict has no side effects'
        }
        'out-of-order' {
            $null = & $invoke Submit $b
            $null = & $invoke Submit $a
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.eventIds.Count -eq 2 -and $view.eventIds[0] -ceq $a.eventId -and $view.eventIds[1] -ceq $b.eventId) 'arrival cannot reorder captured events'
            Assert-ServerAuditValue ($view.actions[0].order -eq 1 -and $view.actions[1].order -eq 2) 'action order is captured order'
        }
        'decision-triple' {
            $a = Get-ServerAuditInput $a.eventId 1 'decision'
            $first = & $invoke Submit $a
            $again = & $invoke Submit $a
            Assert-ServerAuditReceipt $first $a 'persisted'
            Assert-ServerAuditReceipt $again $a 'unchanged'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1 -and $view.designDecisions.Count -eq 1 -and $view.processingDialog.Count -eq 1) 'all three representations once'
            Assert-ServerAuditValue ($view.actions[0].type -ceq 'design_decision' -and $view.processingDialog[0].category -ceq 'decision') 'explicit classification'
            Assert-ServerAuditValue ($view.actions[0].description -ceq $view.designDecisions[0] -and $view.processingDialog[0].content -ceq $view.designDecisions[0]) 'same conclusion'
            Assert-ServerAuditValue ($view.actions[0].eventId -ceq $a.eventId -and $view.processingDialog[0].eventId -ceq $a.eventId) 'same decision identity'
        }
        'decision-distinct' {
            $a = Get-ServerAuditInput $a.eventId 1 'decision'
            $b = Get-ServerAuditInput $b.eventId 2 'decision'
            $null = & $invoke Submit $a
            $null = & $invoke Submit $b
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 2 -and $view.designDecisions.Count -eq 2 -and $view.processingDialog.Count -eq 2) 'distinct equal decisions retained'
        }
        'timeout-before-commit' {
            $null = & $invoke Configure @{ failAt = 'before-commit' }
            $caught = ''
            try { $null = & $invoke Submit $a } catch { $caught = $_.Exception.GetBaseException().Message }
            Assert-ServerAuditValue ($caught -ceq 'scripted_transport_timeout_before_commit') 'before-commit dependency timeout observed'
            Assert-ServerAuditValue ($null -eq (& $invoke Read $a)) 'no precommit receipt'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 0) 'no partial precommit application'
            Assert-ServerAuditReceipt (& $invoke Submit $a) $a 'persisted'
        }
        'timeout-after-commit' {
            $null = & $invoke Configure @{ failAt = 'after-commit' }
            $caught = ''
            try { $null = & $invoke Submit $a } catch { $caught = $_.Exception.GetBaseException().Message }
            Assert-ServerAuditValue ($caught -ceq 'scripted_transport_timeout_after_commit') 'after-commit dependency timeout observed'
            $read = & $invoke Read $a
            Assert-ServerAuditReceipt $read $a 'persisted'
            $retry = & $invoke Submit $a
            Assert-ServerAuditReceipt $retry $a 'unchanged'
            Assert-ServerAuditValue ($retry.revision -eq $read.revision) 'ambiguous timeout replay preserves revision'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1) 'after-commit replay no duplicate'
        }
        'terminal-retention' {
            $receipt = & $invoke Submit $a
            Assert-ServerAuditReceipt $receipt $a 'rejected'
            Assert-ServerAuditValue ($receipt.reason -ceq 'skipped_terminal' -and $receipt.storedStatus -ceq 'completed') 'terminal evidence'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 0 -and $view.status -ceq 'completed') 'terminal unchanged'
            $snapshot = & $invoke Snapshot $root
            Assert-ServerAuditValue ($snapshot.rejected.Count -eq 1) 'retained terminal rejection'
            Assert-ServerAuditValue (-not (& $invoke Complete $root).allowed) 'retained rejection blocks completion'
        }
        'cross-workspace-uuid-collision' {
            # FR-QB-003/005, AC/TEST-QB-009/069: two valid tenants, same UUID and turn strings.
            $first = & $invoke Submit $a
            Assert-ServerAuditReceipt $first $a 'persisted'
            $secondRoot = $root.Clone(); $secondRoot.workspacePath = 'F:/fixture/qb-second'
            $null = & $invoke Seed $secondRoot
            $changed = $a.Clone(); $changed.workspacePath = $secondRoot.workspacePath
            $secondView = & $invoke Project $secondRoot
            Assert-ServerAuditValue ($secondView.status -ceq 'in_progress' -and $secondView.actions.Count -eq 0) 'second tenant is a valid independent empty turn before collision'
            Assert-ServerAuditValue ($null -eq (& $invoke Read $changed)) 'foreign UUID read has no receipt before collision'
            $collisionError = $null; $reply = $null
            try { $reply = & $invoke Submit $changed } catch { $collisionError = $_.Exception.GetBaseException().Message }
            Assert-ServerAuditValue ($null -eq $collisionError) 'cross-tenant UUID collision returns classified receipt rather than storage exception'
            Assert-ServerAuditReceipt $reply $changed 'rejected'
            Assert-ServerAuditValue ($reply.reason -ceq 'payload_conflict') 'UUID remains globally immutable across valid tenants'
            Assert-ServerAuditValue ($null -eq $reply.revision -or $reply.revision -eq 0) 'rejection exposes no foreign committed revision'
            Assert-ServerAuditValue ($null -eq $reply.payloadJson -and $null -eq $reply.storedStatus) 'rejection exposes no foreign payload or turn status'
            Assert-ServerAuditValue ($null -eq (& $invoke Read $changed)) 'foreign UUID read still has no receipt after rejection'
            $secondView = & $invoke Project $secondRoot
            Assert-ServerAuditValue ($secondView.status -ceq 'in_progress' -and $secondView.actions.Count -eq 0 -and $secondView.designDecisions.Count -eq 0 -and $secondView.processingDialog.Count -eq 0 -and $secondView.eventIds.Count -eq 0) 'rejected foreign UUID changes no second tenant representation'
            $read = & $invoke Read $a
            Assert-ServerAuditReceipt $read $a 'persisted'
            Assert-ServerAuditValue ($read.revision -eq $first.revision) 'original tenant receipt revision unchanged'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.status -ceq 'in_progress' -and $view.actions.Count -eq 1 -and $view.actions[0].description -ceq 'same action' -and $view.eventIds.Count -eq 1 -and $view.eventIds[0] -ceq $a.eventId) 'original tenant projection remains exact'
            $firstSnapshot = & $invoke Snapshot $root
            $secondSnapshot = & $invoke Snapshot $secondRoot
            Assert-ServerAuditValue ($firstSnapshot.events.Count -eq 1 -and $firstSnapshot.turns.Count -eq 1) 'original tenant snapshot retains one event and one valid turn'
            Assert-ServerAuditValue ($secondSnapshot.events.Count -eq 0 -and $secondSnapshot.rejected.Count -eq 0 -and $secondSnapshot.turns.Count -eq 1) 'second tenant snapshot exposes no foreign evidence and retains its valid turn'
            $original = @($firstSnapshot.events.Values)[0].event
            foreach ($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','kind','payloadJson','payloadHash')) {
                Assert-ServerAuditValue ($original[$field] -ceq $a[$field]) ('original immutable field retained after cross-tenant collision:' + $field)
            }
            $null = & $invoke Reopen $secondRoot
            Assert-ServerAuditValue ($null -eq (& $invoke Read $changed)) 'reopened second tenant cannot read original UUID'
            $collisionError = $null; $retry = $null
            try { $retry = & $invoke Submit $changed } catch { $collisionError = $_.Exception.GetBaseException().Message }
            Assert-ServerAuditValue ($null -eq $collisionError) 'reopened cross-tenant collision remains classified'
            Assert-ServerAuditReceipt $retry $changed 'rejected'
            Assert-ServerAuditValue ($retry.reason -ceq 'payload_conflict' -and ($null -eq $retry.revision -or $retry.revision -eq 0)) 'reopened foreign UUID stays rejected without revision exposure'
            Assert-ServerAuditReceipt (& $invoke Submit $a) $a 'unchanged'
            $read = & $invoke Read $a
            Assert-ServerAuditReceipt $read $a 'persisted'
            Assert-ServerAuditValue ($read.revision -eq $first.revision) 'original revision remains stable after reopen and both retries'
            $secondView = & $invoke Project $secondRoot
            Assert-ServerAuditValue ($secondView.actions.Count -eq 0 -and $secondView.eventIds.Count -eq 0) 'second tenant retry still applies no event'
        }

        'cross-request-rebind' {
            $null = & $invoke Submit $a
            $secondRoot = $root.Clone(); $secondRoot.requestId = 'req-20260101T000000Z-b'
            $null = & $invoke Seed $secondRoot
            $changed = $a.Clone(); $changed.requestId = $secondRoot.requestId
            $reply = & $invoke Submit $changed
            Assert-ServerAuditValue ($reply.code -ceq 'rejected' -and $reply.reason -ceq 'payload_conflict') 'event cannot change owning request'
            $view = & $invoke Project $secondRoot
            Assert-ServerAuditValue ($view.actions.Count -eq 0) 'foreign request receives no action'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1) 'original request retained'
        }
        'order-collision' {
            $null = & $invoke Submit $a
            $b.order = $a.order
            $reply = & $invoke Submit $b
            Assert-ServerAuditValue ($reply.code -ceq 'rejected' -and $reply.reason -ceq 'payload_conflict') 'different events cannot own same captured ordinal'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1) 'collision preserves first committed event'
        }
        'invalid-envelope' {
            $mutations = @(
                @{ key = 'eventId'; value = 'not-a-uuid' }, @{ key = 'eventId'; value = [guid]::Empty.ToString() },
                @{ key = 'order'; value = -1L }, @{ key = 'order'; value = 0L },
                @{ key = 'order'; value = 1.5 }, @{ key = 'order'; value = $true },
                @{ key = 'order'; value = 9007199254740992L }, @{ key = 'kind'; value = 'invented' },
                @{ key = 'payloadJson'; value = 'invalid' }, @{ key = 'payloadJson'; value = '[]' }
            )
            foreach ($mutation in $mutations) {
                $changed = $a.Clone(); $changed[$mutation.key] = $mutation.value
                if ($mutation.key -ceq 'payloadJson') { $changed.payloadHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($changed.payloadJson))).ToLowerInvariant() }
                $reply = & $invoke Submit $changed
                Assert-ServerAuditValue ($reply.code -ceq 'rejected' -and $reply.reason -ceq 'invalid_audit_event') ('malformed envelope rejected:' + $mutation.key)
            }
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 0) 'invalid envelopes leave no action'
        }
        'hash-conflict' {
            $a.payloadHash = '0000000000000000000000000000000000000000000000000000000000000000'
            $reply = & $invoke Submit $a
            Assert-ServerAuditValue ($reply.code -ceq 'rejected' -and $reply.reason -ceq 'payload_conflict') 'incorrect payload digest refused'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 0) 'hash conflict leaves no action'
        }
        'unknown-request' {
            $a.requestId = 'req-20260101T000000Z-unknown'
            $reply = & $invoke Submit $a
            Assert-ServerAuditValue ($reply.code -ceq 'rejected' -and $reply.reason -ceq 'request_unknown') 'missing target never creates another turn'
            $snapshot = & $invoke Snapshot $root
            Assert-ServerAuditValue ($snapshot.turns.Count -eq 1 -and $snapshot.events.Count -eq 0) 'unknown target leaves graph unchanged'
        }
        { $_ -cin @('decision-failure-field','decision-failure-dialog','decision-failure-action') } {
            $a = Get-ServerAuditInput $a.eventId 1 'decision'
            $point = $Name.Replace('decision-failure-','after-decision-')
            $null = & $invoke Configure @{ failAt = $point }
            $caught = ''
            try { $null = & $invoke Submit $a } catch { $caught = $_.Exception.GetBaseException().Message }
            Assert-ServerAuditValue ($caught -ceq ('scripted_storage_failure_' + $point.Replace('-','_'))) 'decision dependency failure observed'
            Assert-ServerAuditValue ($null -eq (& $invoke Read $a)) 'failed decision has no committed receipt'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 0 -and $view.designDecisions.Count -eq 0 -and $view.processingDialog.Count -eq 0) 'failed decision leaks no representation'
            $beforeRetry = & $invoke Snapshot $root
            Assert-ServerAuditValue ($beforeRetry.events.Count -eq 0 -and $beforeRetry.revision -eq 0) 'failed triple has no ledger commit or revision'
            Assert-ServerAuditValue (@($beforeRetry.trace | Where-Object operation -CEQ 'Stage').Count -gt 0 -and @($beforeRetry.trace | Where-Object operation -CEQ 'Rollback').Count -eq 1) 'actual staged writes rolled back'
            $receipt = & $invoke Submit $a
            Assert-ServerAuditReceipt $receipt $a 'persisted'
            $view = & $invoke Project $root
            $body = ConvertFrom-Json $a.payloadJson -AsHashtable -DateKind String
            Assert-ServerAuditValue ($view.actions.Count -eq 1 -and $view.designDecisions.Count -eq 1 -and $view.processingDialog.Count -eq 1) 'retry commits complete triple once'
            Assert-ServerAuditValue ($view.actions[0].description -ceq $body.conclusion -and $view.designDecisions[0] -ceq $body.conclusion -and $view.processingDialog[0].content -ceq $body.conclusion) 'retry preserves original conclusion'
            Assert-ServerAuditValue ($view.actions[0].eventId -ceq $a.eventId -and $view.processingDialog[0].eventId -ceq $a.eventId -and $view.processingDialog[0].timestamp -ceq $body.timestamp) 'retry preserves decision identity and original timestamp'
        }
        'concurrent-redelivery' {
            $batch = & $invoke ConcurrentSubmit @{ events = @($a,$a) }
            Assert-ServerAuditValue ($batch.replies.Count -eq 2) 'both context submissions return'
            Assert-ServerAuditValue (@($batch.replies | Where-Object code -CEQ 'persisted').Count -eq 1 -and @($batch.replies | Where-Object code -CEQ 'unchanged').Count -eq 1) 'one commit and one reconciled replay across contexts'
            foreach ($reply in $batch.replies) { Assert-ServerAuditReceipt $reply $a $reply.code }
            Assert-ServerAuditValue ($batch.replies[0].revision -eq $batch.replies[1].revision) 'concurrent replay shares original committed revision'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1 -and $view.eventIds.Count -eq 1) 'concurrent replay applies once'
            $snapshot = & $invoke Snapshot $root
            Assert-ServerAuditValue (@($snapshot.trace | Where-Object operation -CEQ 'OpenContext' | Select-Object -ExpandProperty contextId -Unique).Count -eq 2) 'two independent storage contexts'
        }
        'concurrent-distinct' {
            $b.payloadJson = $a.payloadJson; $b.payloadHash = $a.payloadHash
            $batch = & $invoke ConcurrentSubmit @{ events = @($b,$a) }
            Assert-ServerAuditValue ($batch.replies.Count -eq 2) 'both distinct context submissions return'
            Assert-ServerAuditReceipt $batch.replies[0] $b 'persisted'
            Assert-ServerAuditReceipt $batch.replies[1] $a 'persisted'
            Assert-ServerAuditValue ($batch.replies[0].revision -ne $batch.replies[1].revision) 'distinct events have distinct committed revisions'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 2 -and $view.eventIds[0] -ceq $a.eventId -and $view.eventIds[1] -ceq $b.eventId) 'concurrent arrival preserves both captured positions'
            $snapshot = & $invoke Snapshot $root
            Assert-ServerAuditValue (@($snapshot.trace | Where-Object operation -CEQ 'OpenContext' | Select-Object -ExpandProperty contextId -Unique).Count -eq 2) 'distinct submissions use independent contexts'
        }
        'concurrent-conflict' {
            $changed = Get-ServerAuditInput $a.eventId 1 'decision'
            $batch = & $invoke ConcurrentSubmit @{ events = @($a,$changed) }
            Assert-ServerAuditValue ($batch.replies.Count -eq 2) 'both conflicting context submissions return'
            Assert-ServerAuditReceipt $batch.replies[0] $a $batch.replies[0].code
            Assert-ServerAuditReceipt $batch.replies[1] $changed $batch.replies[1].code
            $winner = if ($batch.replies[0].code -ceq 'persisted') { $a } else { $changed }
            Assert-ServerAuditReceipt (& $invoke Read $winner) $winner 'persisted'
            Assert-ServerAuditValue (@($batch.replies | Where-Object code -CEQ 'persisted').Count -eq 1 -and @($batch.replies | Where-Object { $_.code -ceq 'rejected' -and $_.reason -ceq 'payload_conflict' }).Count -eq 1) 'one immutable winning payload'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.eventIds.Count -eq 1 -and $view.actions.Count -eq 1 -and $view.designDecisions.Count -eq $view.processingDialog.Count) 'conflicting payload cannot fork the event'
        }
        'event-reopen' {
            $a = Get-ServerAuditInput $a.eventId 1 'decision'
            $first = & $invoke Submit $a
            Assert-ServerAuditReceipt $first $a 'persisted'
            $null = & $invoke Reopen $root
            $read = & $invoke Read $a
            Assert-ServerAuditReceipt $read $a 'persisted'
            Assert-ServerAuditValue ($first.revision -eq $read.revision) 'reopen retains committed receipt revision'
            Assert-ServerAuditReceipt (& $invoke Submit $a) $a 'unchanged'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1 -and $view.designDecisions.Count -eq 1 -and $view.processingDialog.Count -eq 1) 'reopened triple is complete and replay-safe'
            $snapshot = & $invoke Snapshot $root
            $stored = @($snapshot.events.Values)[0].event
            Assert-ServerAuditValue ($stored.payloadJson -ceq $a.payloadJson -and $stored.payloadHash -ceq $a.payloadHash) 'reopen preserves exact immutable payload bytes and digest'
        }
        'ambiguous-timeout-reopen' {
            $null = & $invoke Configure @{ failAt = 'after-commit' }
            $caught = ''
            try { $null = & $invoke Submit $a } catch { $caught = $_.Exception.GetBaseException().Message }
            Assert-ServerAuditValue ($caught -ceq 'scripted_transport_timeout_after_commit') 'ambiguous commit timeout observed before reopen'
            $null = & $invoke Reopen $root
            $read = & $invoke Read $a
            Assert-ServerAuditReceipt $read $a 'persisted'
            $retry = & $invoke Submit $a
            Assert-ServerAuditReceipt $retry $a 'unchanged'
            Assert-ServerAuditValue ($retry.revision -eq $read.revision) 'reopened reconciliation uses same committed revision'
            $view = & $invoke Project $root
            Assert-ServerAuditValue ($view.actions.Count -eq 1) 'reopened ambiguous replay adds no duplicate'
        }
        'rejection-reopen' {
            $reply = & $invoke Submit $a
            Assert-ServerAuditReceipt $reply $a 'rejected'
            Assert-ServerAuditValue ($reply.reason -ceq 'skipped_terminal') 'reopen target retains terminal rejection reason'
            $null = & $invoke Reopen $root
            $snapshot = & $invoke Snapshot $root
            Assert-ServerAuditValue ($snapshot.rejected.Count -eq 1 -and $snapshot.events.Count -eq 0) 'reopen retains rejected evidence without accepted event'
            Assert-ServerAuditValue (-not (& $invoke Complete $root).allowed) 'reopened retained rejection blocks successful completion'
            Assert-ServerAuditValue (@($snapshot.trace | Where-Object operation -CEQ 'Submit').Count -eq 1) 'terminal rejection is not blindly retried after reopen'
        }
        'exact-dialog' {
            $a = Get-ServerAuditInput $a.eventId 1 'dialog'
            Assert-ServerAuditReceipt (& $invoke Submit $a) $a 'persisted'
            $view = & $invoke Project $root
            $payload = ConvertFrom-Json $a.payloadJson -AsHashtable
            Assert-ServerAuditValue ($view.processingDialog.Count -eq 1 -and $view.processingDialog[0].content -ceq $payload.content) 'exact dialog content'
            Assert-ServerAuditValue ($view.actions.Count -eq 0 -and $view.designDecisions.Count -eq 0) 'dialog classification preserved'
        }
    }
    $snapshot = & $invoke Snapshot $root
    Assert-ServerAuditValue (@($snapshot.trace | Where-Object operation -CEQ 'Seed').Count -eq $(if ($Name -cin @('cross-request-rebind','cross-workspace-uuid-collision')) { 2 } else { 1 })) 'provider seed interaction'
    Assert-ServerAuditValue (@($snapshot.trace | Where-Object operation -CEQ 'Submit').Count -gt 0) 'provider submit interaction'
    return @{ name = $Name; fault = $Fault; trace = $snapshot.trace; eventCount = $snapshot.events.Count; rejectedCount = $snapshot.rejected.Count; revision = $snapshot.revision }
}
Write-Output 'Counted effects: twenty-eight shared scenarios; original twenty-seven/seventy-five retained plus one cross-workspace UUID scenario and six interaction/storage controls; positive revision probes; one retained native receipt. Production requires explicit real-service adapter.'
if (-not $PSCmdlet.ShouldProcess($ReceiptPath,'Run C7 requirement assertions and retain complete interaction evidence')) { return }
$script:serverAuditChecks = 0
$observations = [Collections.Generic.List[object]]::new()
$failures = [Collections.Generic.List[object]]::new()
$cases = @('roundtrip','redelivery','equal-content-distinct','payload-conflict','out-of-order','decision-triple','decision-distinct','timeout-before-commit','timeout-after-commit','terminal-retention','exact-dialog','cross-request-rebind','order-collision','invalid-envelope','hash-conflict','unknown-request','decision-failure-field','decision-failure-dialog','decision-failure-action','concurrent-redelivery','concurrent-distinct','concurrent-conflict','event-reopen','ambiguous-timeout-reopen','rejection-reopen','safe-order-boundary','exact-captured-envelope','cross-workspace-uuid-collision')
foreach ($name in $cases) {
    try { $observations.Add((Invoke-ServerAuditCase $name)) } catch { $failures.Add(@{ name = $name; error = $_.Exception.ToString() }) }
}
$controls = @(
    @('cross-workspace-uuid-collision','allow-rebind'), @('cross-workspace-uuid-collision','leak-foreign-receipt'),
    @('cross-workspace-uuid-collision','erase-original-on-foreign-conflict'), @('cross-workspace-uuid-collision','foreign-projection-on-conflict'),
    @('cross-workspace-uuid-collision','foreign-revision-on-conflict'), @('cross-workspace-uuid-collision','foreign-collision-storage-error'),
    @('redelivery','duplicate-application'), @('equal-content-distinct','content-dedup'),
    @('payload-conflict','overwrite-conflict'), @('out-of-order','reverse-projection'),
    @('decision-triple','drop-decision-field'), @('decision-triple','drop-decision-dialog'),
    @('decision-triple','drop-decision-action'), @('timeout-before-commit','leak-before-commit'),
    @('timeout-after-commit','duplicate-application'), @('terminal-retention','accept-terminal'),
    @('terminal-retention','ignore-rejection'), @('roundtrip','wrong-revision'),
    @('roundtrip','wrong-root'), @('roundtrip','wrong-hash'),
    @('cross-request-rebind','allow-rebind'), @('order-collision','allow-order-collision'),
    @('invalid-envelope','accept-invalid'), @('hash-conflict','accept-hash-conflict'),
    @('decision-failure-field','leak-decision-stage'), @('decision-failure-dialog','leak-decision-stage'), @('decision-failure-action','leak-decision-stage'),
    @('safe-order-boundary','clip-order'),
    @('exact-captured-envelope','normalize-captured-root'), @('exact-captured-envelope','reformat-captured-payload'),
    @('concurrent-redelivery','racy-duplicate'), @('concurrent-redelivery','one-context'), @('concurrent-distinct','content-dedup'), @('concurrent-conflict','overwrite-conflict'), @('event-reopen','lose-receipt-on-reopen'), @('rejection-reopen','lose-rejection-on-reopen')
)
$revisionPositiveChecks = [Collections.Generic.List[object]]::new()
if ($Mode -ceq 'Mock') {
    foreach ($phase in @('submit','read','replay')) {
        $scenario = if ($phase -ceq 'replay') { 'redelivery' } else { 'roundtrip' }
        foreach ($kind in @('boolean-true','boolean-false','fraction','negative-fraction','decimal-fraction','infinity','negative-infinity','nan','string','missing','object','date','character','zero','negative')) {
            $controls += ,@($scenario,($phase + '-revision-' + $kind))
        }
    }
    $revisionInput = Get-ServerAuditInput '11111111-1111-4111-8111-111111111111'
    foreach ($code in @('persisted','unchanged')) {
        foreach ($validRevision in @([long]1,[long]9,[double]9,[decimal]9,[long]9007199254740992)) {
            $validReceipt = @{ code = $code; revision = $validRevision }
            foreach ($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')) { $validReceipt[$field] = $revisionInput[$field] }
            Assert-ServerAuditReceipt $validReceipt $revisionInput $code
            $revisionPositiveChecks.Add(@{ code = $code; type = $validRevision.GetType().FullName; value = [string]$validRevision; accepted = $true })
        }
    }
}
$rejections = [Collections.Generic.List[object]]::new()
if ($Mode -ceq 'Mock') {
    foreach ($control in $controls) {
        try { $null = Invoke-ServerAuditCase $control[0] $control[1]; $failures.Add(@{ name = $control[0]; fault = $control[1]; error = 'Wrong provider behavior was not rejected' }) }
        catch {
            $message = $_.Exception.GetBaseException().Message
            if ($message.StartsWith('audit_assert:',[StringComparison]::Ordinal)) { $rejections.Add(@{ name = $control[0]; fault = $control[1]; assertion = $message }) }
            else { $failures.Add(@{ name = $control[0]; fault = $control[1]; error = $_.Exception.ToString() }) }
        }
    }
}
$receipt = [ordered]@{ timestamp = Get-Date -Format o; mode = $Mode; requirements = @('FR-QB-003','FR-QB-005','TR-QB-AUDIT-001','AC-QB-008','AC-QB-009','AC-QB-069','TEST-QB-008','TEST-QB-009','TEST-QB-069'); cases = $cases.Count; assertions = $script:serverAuditChecks; controls = $rejections.Count; positiveRevisionChecks = @($revisionPositiveChecks); failed = $failures.Count; skipped = 0; observations = @($observations); rejectingControls = @($rejections); failures = @($failures); sourceHashes = @(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot 'ServerAuditEvent.Model.ps1') | Select-Object Path,Hash); scope = 'C7 first event/receipt/triple assertion increment. Actual concurrency, restart, all boundary validation, host integration and cumulative gates remain required. No product acceptance.'; productComplete = $false }
Import-Module PowerShell.MCP -ErrorAction Stop
$var1 = $receipt | ConvertTo-Json -Depth 100
Add-LinesToFile -Path $ReceiptPath -Content $var1 *> $null
[pscustomobject]@{ cases = $receipt.cases; checks = $receipt.assertions; controls = $receipt.controls; failed = $receipt.failed; skipped = 0; receipt = $ReceiptPath }
if ($failures.Count) { throw 'C7 assertion increment failed; full chronological receipt retained' }



