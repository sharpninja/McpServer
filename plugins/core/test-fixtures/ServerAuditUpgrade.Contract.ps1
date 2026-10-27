#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode='Mock',
    [Parameter(Mandatory)][string]$ReceiptPath,
    [scriptblock]$ProviderFactory
)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $ReceiptPath){throw 'Chronological receipt exists'}
if($Mode -ceq 'Production' -and -not $ProviderFactory){throw 'Actual provider-owned migration adapter required'}
function Get-AuditUpgradeInput {
    $workspace='F:/fixture/qb'
    $stamp='2026-01-01 00:00:01+00:00'
    $text="legacy $([char]0x03A9)\\u2014`r`nkept"
    $tables=[ordered]@{
        SessionLogs=@(@{Id=1L;WorkspaceId=$workspace;SourceType='Codex';SessionId='Codex-20260101T000000Z-fixture';EntryCount=1;Title=$text;Status='in_progress';Started=$stamp})
        SessionLogTurns=@(@{Id=1L;WorkspaceId=$workspace;SessionLogId=1L;RequestId='req-20260101T000000Z-a';Status='in_progress';Timestamp=$stamp;QueryText=$text;Response=$text;PlanFile='None';TodoId='None'})
        SessionLogActions=@(@{Id=1L;WorkspaceId=$workspace;SessionLogTurnId=1L;Order=2147483647L;Description=$text;Type='edit';Status='completed';FilePath='F:/fixture/legacy.cs'})
        SessionLogProcessingDialogs=@(@{Id=1L;WorkspaceId=$workspace;SessionLogTurnId=1L;Ordinal=0;Timestamp=$stamp;Role='model';Content=$text;Category='decision'})
        SessionLogTurnStringLists=@(@{Id=1L;WorkspaceId=$workspace;SessionLogTurnId=1L;Ordinal=0;ListType='DesignDecision';Value=$text})
        SessionLogTags=@(@{Id=1L;WorkspaceId=$workspace;SessionLogId=1L;Tag='legacy-session-tag'})
        SessionLogTurnTags=@(@{Id=1L;WorkspaceId=$workspace;SessionLogTurnId=1L;Tag='legacy-turn-tag'})
        SessionLogTurnContexts=@(@{Id=1L;WorkspaceId=$workspace;SessionLogTurnId=1L;Ordinal=0;ContextItem='legacy-exact-context'})
        SessionLogCommits=@(@{Id=1L;WorkspaceId=$workspace;SessionLogTurnId=1L;Ordinal=0;Sha='abc123';Branch='codex/fixture';Message=$text;Author='Codex';CommitTimestamp=$stamp})
        SessionLogCommitFiles=@(@{Id=1L;WorkspaceId=$workspace;SessionLogCommitId=1L;Ordinal=0;Path='F:/fixture/legacy.cs'})
    }
    return @{tables=$tables}
}
function Get-AuditUpgradeModel {
    param([string]$Fault='')
    $modelFault=$Fault
    $state=@{tables=[ordered]@{};schema='old';history=@('legacy');revision=0L;events=@{};trace=[Collections.Generic.List[object]]::new()}
    $copy={param($Value) ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $Value -Depth 100 -Compress) -AsHashtable -NoEnumerate -DateKind String}
    $invoke={
        param([string]$Operation,[Collections.IDictionary]$InputObject)
        $state.trace.Add(@{operation=$Operation})
        switch($Operation){
            'SeedLegacy' {$state.tables=& $copy $InputObject.tables;return @{seeded=$true}}
            'ReadLegacy' {
                $rows=& $copy $state.tables
                if($modelFault -ceq 'missing-table'){$rows.Remove('SessionLogProcessingDialogs')}
                if($modelFault -ceq 'missing-row'){$rows.SessionLogActions=@()}
                return @{tables=$rows}
            }
            'EnsureReady' {
                if($state.schema -ceq 'old' -and -not $InputObject.autoMigrate){
                    if($modelFault -ceq 'bypass-schema-admin'){return @{ready=$true}}
                    throw 'Database migrations must be applied by the schema administrator before runtime startup. Pending: C7'
                }
                if($InputObject.autoMigrate -and $state.schema -ceq 'old'){
                    if($modelFault -cne 'skip-schema'){$state.schema='current';$state.history+=,'C7'}
                    if($modelFault -ceq 'change-timestamp'){$state.tables.SessionLogProcessingDialogs[0].Timestamp='changed'}
                    if($modelFault -ceq 'change-content'){$state.tables.SessionLogTurns[0].QueryText='changed'}
                    if($modelFault -ceq 'change-status'){$state.tables.SessionLogActions[0].Status='changed'}
                    if($modelFault -ceq 'change-order'){$state.tables.SessionLogActions[0].Order=1}
                    if($modelFault -ceq 'duplicate-row'){$state.tables.SessionLogActions+=,(& $copy $state.tables.SessionLogActions[0])}
                    if($modelFault -ceq 'invent-legacy-id'){$state.tables.SessionLogActions[0].AuditEventId='invented'}
                    if($modelFault -ceq 'rekey-workspace'){$state.tables.SessionLogs[0].WorkspaceId='F:/other'}
                }
                return @{ready=$true}
            }
            'SchemaAdminUpgrade' {$state.schema='current';if($state.history -cnotcontains 'C7'){$state.history+=,'C7'};return @{applied=$true}}
            'Reopen' {
                $restored=& $copy @{tables=$state.tables;schema=$state.schema;history=$state.history;revision=$state.revision;events=$state.events}
                foreach($key in $restored.Keys){$state[$key]=$restored[$key]}
                if($modelFault -ceq 'lose-receipt'){$state.events.Clear()}
                return @{reopened=$true}
            }
            'Submit' {
                if($state.schema -cne 'current'){throw 'Pending: C7'}
                $state.revision++
                $state.events[$InputObject.eventId]=@{event=(& $copy $InputObject);revision=$state.revision}
                $reply=@{code='persisted';revision=$state.revision}
                foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){$reply[$field]=$InputObject[$field]}
                if($modelFault -ceq 'wrong-root'){$reply.requestId='foreign'}
                return $reply
            }
            'Read' {
                $row=$state.events[$InputObject.eventId]
                if($null -eq $row){return $null}
                $reply=@{code='persisted';revision=$row.revision}
                foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){$reply[$field]=$row.event[$field]}
                return $reply
            }
            'Snapshot' {
                $snapshot=& $copy $state
                $snapshot.legacyMetadataClear=$true
                foreach($table in @('SessionLogActions','SessionLogProcessingDialogs','SessionLogTurnStringLists')){foreach($row in $state.tables[$table]){foreach($field in @('AuditEventId','AuditEventOrder','OriginalTimestamp')){if($null -ne $row[$field]){$snapshot.legacyMetadataClear=$false}}}}
                return $snapshot
            }
            default {throw 'Unknown upgrade dependency operation'}
        }
    }.GetNewClosure()
    return @{invoke=$invoke}
}
function Assert-AuditUpgradeValue {
    param([bool]$Condition,[string]$Message)
    $script:auditUpgradeChecks++
    if(-not $Condition){throw ('upgrade_assert:'+$Message)}
}
function Assert-AuditUpgradeRowSet {
    param($Actual,$Expected)
    Assert-AuditUpgradeValue ($Actual.tables.Count -eq $Expected.tables.Count) 'complete legacy table set'
    foreach($table in $Expected.tables.Keys){
        Assert-AuditUpgradeValue ($Actual.tables.Contains($table)) ('legacy table:'+ $table)
        $rows=@($Actual.tables[$table]);$expectedRows=@($Expected.tables[$table])
        Assert-AuditUpgradeValue ($rows.Count -eq $expectedRows.Count) ('legacy row count:'+ $table)
        for($index=0;$index -lt $expectedRows.Count;$index++){
            foreach($column in $expectedRows[$index].Keys){
                Assert-AuditUpgradeValue ($rows[$index][$column] -ceq $expectedRows[$index][$column]) ('exact old field:'+ $table+'.'+$column)
            }
        }
    }
}
function Invoke-AuditUpgradeCase {
    param([string]$Name,[string]$Fault='')
    $provider=if($Mode -ceq 'Mock'){Get-AuditUpgradeModel $Fault}else{& $ProviderFactory $Fault}
    $invoke=$provider.invoke
    $seed=Get-AuditUpgradeInput
    $null=& $invoke SeedLegacy $seed
    $before=& $invoke ReadLegacy @{}
    Assert-AuditUpgradeRowSet $before $seed
    if($Name -ceq 'schema-admin'){
        $message=''
        try{$null=& $invoke EnsureReady @{autoMigrate=$false}}catch{$message=$_.Exception.GetBaseException().Message}
        Assert-AuditUpgradeValue ($message.Contains('Pending:',[StringComparison]::Ordinal)) 'schema admin pending fails closed'
        $still=& $invoke ReadLegacy @{}
        Assert-AuditUpgradeRowSet $still $before
        $null=& $invoke SchemaAdminUpgrade @{}
        $null=& $invoke EnsureReady @{autoMigrate=$false}
    }else{$null=& $invoke EnsureReady @{autoMigrate=$true}}
    $after=& $invoke ReadLegacy @{}
    Assert-AuditUpgradeRowSet $after $before
    $schemaProbe=& $invoke Snapshot @{}
    Assert-AuditUpgradeValue ($schemaProbe.schema -ceq 'current') 'migration actually made schema ready'
    $body=@{type='tool_execution';description='new after upgrade';status='completed';order=9007199254740991L}|ConvertTo-Json -Compress
    $captured=@{workspacePath='F:/fixture/qb';agentIdentity='Codex';mcpSessionId='Codex-20260101T000000Z-fixture';requestId='req-20260101T000000Z-a';eventId='33333333-3333-4333-8333-333333333333';kind='action';order=9007199254740991L;payloadJson=$body;payloadHash=[Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($body)))}
    $receipt=& $invoke Submit $captured
    Assert-AuditUpgradeValue ($receipt.code -ceq 'persisted' -and $receipt.revision -isnot [bool] -and $receipt.revision -is [ValueType] -and $receipt.revision -gt 0 -and [decimal]$receipt.revision % 1 -eq 0) 'new event commits with positive integral revision'
    foreach($key in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){Assert-AuditUpgradeValue ($receipt[$key] -ceq $captured[$key]) ('submitted receipt:'+ $key)}
    if($Name -ceq 'idempotent-reopen'){
        $null=& $invoke EnsureReady @{autoMigrate=$true}
        $null=& $invoke Reopen @{}
    }else{$null=& $invoke Reopen @{}}
    $read=& $invoke Read $captured
    Assert-AuditUpgradeValue ($null -ne $read -and $read.revision -eq $receipt.revision -and $read.code -ceq 'persisted') 'reopen independently retains same committed revision'
    foreach($key in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){Assert-AuditUpgradeValue ($read[$key] -ceq $captured[$key]) ('upgraded receipt:'+ $key)}
    Assert-AuditUpgradeRowSet (& $invoke ReadLegacy @{}) $before
    $snapshot=& $invoke Snapshot @{}
    Assert-AuditUpgradeValue $snapshot.legacyMetadataClear 'legacy projection identity/order/timestamp remain null'
    Assert-AuditUpgradeValue ($snapshot.schema -ceq 'current') 'actual current schema readiness'
    Assert-AuditUpgradeValue (@($snapshot.history|Where-Object {$_ -ceq 'C7'}).Count -eq 1) 'one provider migration history entry'
    Assert-AuditUpgradeValue (@($snapshot.trace|Where-Object operation -CEQ 'SeedLegacy').Count -eq 1 -and @($snapshot.trace|Where-Object operation -CEQ 'EnsureReady').Count -ge 1 -and @($snapshot.trace|Where-Object operation -CEQ 'Reopen').Count -eq 1) 'actual seeded migration/readiness/reopen interactions'
    return @{name=$Name;trace=$snapshot.trace;before=$before;after=(& $invoke ReadLegacy @{});receipt=$receipt;read=$read;history=$snapshot.history;schema=$snapshot.schema}
}
Write-Output 'Counted effects: three SAME old-schema seeded upgrade/readiness/reopen scenarios, thirteen corrupt dependency/effect controls, one retained native receipt; actual migration only via explicit Production provider.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Run requirement-derived legacy upgrade assertions')){return}
$script:auditUpgradeChecks=0
$observations=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new();$rejects=[Collections.Generic.List[object]]::new()
foreach($case in @('auto-upgrade','schema-admin','idempotent-reopen')){
    try{$observations.Add((Invoke-AuditUpgradeCase $case))}catch{$failures.Add(@{name=$case;error=$_.Exception.ToString()})}
}
if($Mode -ceq 'Mock'){
    foreach($fault in @('missing-table','missing-row','bypass-schema-admin','skip-schema','change-timestamp','change-content','change-status','change-order','duplicate-row','rekey-workspace','lose-receipt','wrong-root','invent-legacy-id')){
        $case=if($fault -ceq 'bypass-schema-admin'){'schema-admin'}else{'idempotent-reopen'}
        try{$null=Invoke-AuditUpgradeCase $case $fault;$failures.Add(@{fault=$fault;error='Corrupted effect was not rejected'})}
        catch{if($_.Exception.GetBaseException().Message.StartsWith('upgrade_assert:',[StringComparison]::Ordinal)){$rejects.Add(@{fault=$fault;assertion=$_.Exception.GetBaseException().Message})}else{$failures.Add(@{fault=$fault;error=$_.Exception.ToString()})}}
    }
}
$receipt=[ordered]@{timestamp=(Get-Date).ToString('o');mode=$Mode;requirements=@('FR-QB-003','FR-QB-005','TR-QB-AUDIT-001','AC-QB-009','AC-QB-069','TEST-QB-009','TEST-QB-069','FR-SUPPORT-010');cases=3;assertions=$script:auditUpgradeChecks;negativeControls=$rejects.Count;failed=$failures.Count;skipped=0;observations=@($observations);rejectingControls=@($rejects);failures=@($failures);sourceSha256=(Get-FileHash $PSCommandPath).Hash;productComplete=$false;scope='Upgrade assertions only; Mock is not proof of inherited provider migration, actual old-schema SQL or runtime.'}
Import-Module PowerShell.MCP -ErrorAction Stop
$var1=$receipt|ConvertTo-Json -Depth 100
Add-LinesToFile $ReceiptPath -Content $var1 | Out-Null
[pscustomobject]@{cases=3;assertions=$receipt.assertions;controls=$receipt.negativeControls;failed=$receipt.failed;skipped=0;receipt=$ReceiptPath}
if($failures.Count){throw 'Upgrade assertion gate failed; full chronological receipt retained'}
