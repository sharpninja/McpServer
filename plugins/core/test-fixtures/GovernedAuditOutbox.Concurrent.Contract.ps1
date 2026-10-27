# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-008/009/069: same multi-process contention and receipt assertions for Mock and Production.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$RootDirectory,[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $RootDirectory){throw 'Fixture root must be new'}
if(Test-Path -LiteralPath $ReceiptPath){throw 'Receipt exists'}
Write-Output 'Counted effects:2 positive contention scenarios;3 rejecting model dependency controls;at most45 short-lived processes;retained per-operation streams/trace/result and explicit ready/release gates;1 aggregate receipt. No live server.'
if(-not $PSCmdlet.ShouldProcess($RootDirectory,'Run deterministic isolated process contention assertions')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
$worker=Join-Path $PSScriptRoot 'GovernedAuditOutbox.Concurrent.Worker.ps1'
$pwsh=(Get-Process -Id $PID).Path
$script:concurrentChecks=0
$script:concurrentProcesses=[Collections.Generic.List[object]]::new()
function Assert-ConcurrentValue {
    param([bool]$Condition,[string]$Message)
    $script:concurrentChecks++
    if(-not $Condition){throw ('Concurrent assertion: '+$Message)}
}
function Start-ConcurrentWorker {
    [CmdletBinding(SupportsShouldProcess)]
    param([hashtable]$State,[string]$Operation,[string]$Request='req-concurrent',[string]$HostKey='host-concurrent',[string]$Hold='None',[string]$Mutation='')
    if(-not $PSCmdlet.ShouldProcess($State.folder,'Launch one isolated contention worker process')){return}
    $State.step++
    $stem=Join-Path $State.folder ($State.step.ToString('D2')+'-'+$Operation)
    $arguments=@('-NoProfile','-NonInteractive','-File',$worker,'-Mode',$Mode,'-RootDirectory',$State.outbox,'-TracePath',($stem+'.trace.jsonl'),'-ResultPath',($stem+'.result.json'),'-Operation',$Operation,'-RequestId',$Request,'-HostSessionKey',$HostKey,'-Hold',$Hold,'-GatePath',($stem+'.gate'))
    if($Mutation){$arguments+=@('-Mutation',$Mutation)}
    $process=Start-Process -FilePath $pwsh -ArgumentList $arguments -WorkingDirectory $State.folder -WindowStyle Hidden -RedirectStandardOutput ($stem+'.stdout.log') -RedirectStandardError ($stem+'.stderr.log') -PassThru
    return @{process=$process;stem=$stem;operation=$Operation;finished=$false}
}
function Complete-ConcurrentWorker {
    param([hashtable]$Job)
    if($Job.finished){return $Job.result}
    if(-not $Job.process.WaitForExit(55000)){throw 'Fixture worker did not exit after released gate'}
    $Job.process.Refresh()
    $exit=$Job.process.ExitCode
    $trace=if([IO.File]::Exists($Job.stem+'.trace.jsonl')){@([IO.File]::ReadAllLines($Job.stem+'.trace.jsonl')|Where-Object{$_}|ForEach-Object{ConvertFrom-Json $_ -AsHashtable})}else{@()}
    $script:concurrentProcesses.Add(@{pid=$Job.process.Id;operation=$Job.operation;exit=$exit;tracePath=$Job.stem+'.trace.jsonl';stdout=$Job.stem+'.stdout.log';stderr=$Job.stem+'.stderr.log';effects=$trace})
    $Job.finished=$true
    $Job.process.Dispose()
    if($exit -ne 0){throw ('Fixture worker exit '+$exit+'; see '+$Job.stem+'.stderr.log')}
    $Job.result=ConvertFrom-Json ([IO.File]::ReadAllText($Job.stem+'.result.json')) -AsHashtable -Depth 60
    $Job.trace=@($trace | Where-Object { $null -ne $_ })
    return $Job.result
}
function Wait-ConcurrentReady {
    param([hashtable]$Job)
    $deadline=[DateTimeOffset]::Now.AddSeconds(30)
    while(-not [IO.File]::Exists($Job.stem+'.gate.ready')) {
        if($Job.process.HasExited -or [DateTimeOffset]::Now -gt $deadline){throw 'Fixture worker never reached required interaction gate'}
        Start-Sleep -Milliseconds 40
    }
}
function Unblock-ConcurrentWorker {
    [CmdletBinding(SupportsShouldProcess)]
    param([hashtable]$Job)
    if(-not $PSCmdlet.ShouldProcess($Job.stem,'Publish one explicit fixture release gate')){return}
    if(-not [IO.File]::Exists($Job.stem+'.gate.release')){Add-LinesToFile -Path ($Job.stem+'.gate.release') -Content 'release' *> $null}
}
function Get-ConcurrentCapture {
    param([string]$Directory,[string]$Folder)
    return @(Get-ChildItem -LiteralPath (Join-Path $Directory $Folder) -File -Filter '*.event.yaml'|Sort-Object Name|ForEach-Object{@{name=$_.Name;document=(ConvertFrom-Yaml ([IO.File]::ReadAllText($_.FullName)))}})
}
function Invoke-ConcurrentScenario {
    param([string]$Name,[bool]$SameRequest,[string]$Mutation='')
    $folder=Join-Path $RootDirectory ($Name+$(if($Mutation){'-'+$Mutation}else{''}))
    [void][IO.Directory]::CreateDirectory($folder)
    $state=@{folder=$folder;outbox=(Join-Path $folder 'outbox');step=0}
    $request=if($SameRequest){'req-concurrent'}else{'req-other'}
    $contextJson=ConvertTo-Json -InputObject @('/fixture/concurrent','Codex','session-concurrent','host-concurrent') -Compress
    $digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($contextJson))).ToLowerInvariant()
    $directory=Join-Path $state.outbox $digest
    $holder=Start-ConcurrentWorker $state 'Enqueue' -Hold 'Publication' -Mutation $Mutation
    try {
        Wait-ConcurrentReady $holder
        $contender=Start-ConcurrentWorker $state 'Enqueue' -Request $request -Mutation $Mutation
        $blocked=Complete-ConcurrentWorker $contender
        Assert-ConcurrentValue ($blocked.code -ceq 'lost') 'contending capture refused while writer holds lock'
        Assert-ConcurrentValue (@($contender.trace|Where-Object{$_.effect -in @('publication','submit','read')}).Count -eq 0) 'blocked writer has no storage publication or transport effects'
        $foreign=Start-ConcurrentWorker $state 'Enqueue' -HostKey 'other-host' -Mutation $Mutation
        $isolated=Complete-ConcurrentWorker $foreign
        Assert-ConcurrentValue ($isolated.code -ceq 'queued' -and $isolated.event.order -eq 1) 'separate host context proceeds with independent sequence'
    } finally {
        Unblock-ConcurrentWorker $holder
        $first=Complete-ConcurrentWorker $holder
    }
    Assert-ConcurrentValue ($first.code -ceq 'queued' -and $first.event.order -eq 1) 'first writer capture survives contention'
    $retry=Start-ConcurrentWorker $state 'Enqueue' -Request $request -Mutation $Mutation
    $second=Complete-ConcurrentWorker $retry
    Assert-ConcurrentValue ($second.code -ceq 'queued' -and $second.event.order -eq $(if($SameRequest){2}else{1})) 'retry after release reserves correct per-request order'
    Assert-ConcurrentValue ($first.event.eventId -cne $second.event.eventId) 'equal-content distinct captures have separate event IDs'
    Assert-ConcurrentValue ([string]::Equals($first.event.payloadJson,$second.event.payloadJson,[StringComparison]::Ordinal)) 'equal-content scenario retains identical payload bytes'
    $pending=@(Get-ConcurrentCapture $directory 'pending')
    Assert-ConcurrentValue ($pending.Count -eq 2) 'exactly two owned-context captures durably present'
    foreach($capture in @($first.event,$second.event)) {
        $matching=@($pending|Where-Object{$_.document.event.eventId -ceq $capture.eventId})
        Assert-ConcurrentValue ($matching.Count -eq 1) 'each captured GUID has exactly one retained file'
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','kind','payloadJson','payloadHash')) {
            Assert-ConcurrentValue ([string]::Equals([string]$matching[0].document.event[$field],[string]$capture[$field],[StringComparison]::Ordinal)) ('stored capture immutable '+$field)
        }
    }
    $flusher=Start-ConcurrentWorker $state 'Flush' -Hold 'Submit' -Mutation $Mutation
    try {
        Wait-ConcurrentReady $flusher
        $otherFlush=Start-ConcurrentWorker $state 'Flush' -Mutation $Mutation
        $blockedFlush=Complete-ConcurrentWorker $otherFlush
        Assert-ConcurrentValue ($blockedFlush.code -ceq 'lost') 'concurrent flush refused while submit is in flight'
        Assert-ConcurrentValue (@($otherFlush.trace|Where-Object{$_.effect -in @('submit','read','publication')}).Count -eq 0) 'blocked flush never submits or alters captured evidence'
    } finally {
        Unblock-ConcurrentWorker $flusher
        $flushed=Complete-ConcurrentWorker $flusher
    }
    Assert-ConcurrentValue ($flushed.code -ceq 'flushed' -and $flushed.persisted -eq 2 -and $flushed.pending -eq 0 -and $flushed.quarantined -eq 0 -and $flushed.rejected -eq 0) 'only lock owner persists both complete receipts'
    $submitted=@($flusher.trace|Where-Object{$_.effect -ceq 'submit'})
    Assert-ConcurrentValue ($submitted.Count -eq 2 -and @($flushed.receipts).Count -eq 2) 'one submit and one receipt per captured event'
    for($index=0;$index -lt $pending.Count;$index++) {
        $capture=$pending[$index].document.event
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')) {
            Assert-ConcurrentValue ([string]::Equals([string]$submitted[$index].event[$field],[string]$capture[$field],[StringComparison]::Ordinal)) ('ordered transport retains '+$field)
            Assert-ConcurrentValue ([string]::Equals([string]$flushed.receipts[$index][$field],[string]$capture[$field],[StringComparison]::Ordinal)) ('receipt matches '+$field)
        }
        Assert-ConcurrentValue ($flushed.receipts[$index].code -ceq 'persisted' -and $flushed.receipts[$index].revision -eq 7) 'receipt has positive matching revision'
    }
    $archive=@(Get-ConcurrentCapture $directory 'archive')
    Assert-ConcurrentValue ($archive.Count -eq 2) 'both acknowledged captures retained in archive'
    foreach($prior in $pending) {
        $stored=@($archive|Where-Object{$_.name -ceq $prior.name})
        Assert-ConcurrentValue ($stored.Count -eq 1) 'retirement preserves immutable filename'
        Assert-ConcurrentValue ([string]::Equals($stored[0].document.event.payloadJson,$prior.document.event.payloadJson,[StringComparison]::Ordinal)) 'retirement preserves original payload'
    }
    $last=Start-ConcurrentWorker $state 'Flush' -Mutation $Mutation
    $replay=Complete-ConcurrentWorker $last
    Assert-ConcurrentValue ($replay.persisted -eq 0 -and @($last.trace|Where-Object{$_.effect -ceq 'submit'}).Count -eq 0) 'new process never resubmits archived events'
    return @{case=$Name;mutation=$Mutation;directory=$directory;first=$first;second=$second;isolated=$isolated;pending=$pending;flush=$flushed;archive=$archive;replay=$replay}
}
$results=[Collections.Generic.List[object]]::new()
$controls=[Collections.Generic.List[object]]::new()
$failure=$null
try {
    $results.Add((Invoke-ConcurrentScenario 'same-request' $true))
    $results.Add((Invoke-ConcurrentScenario 'different-request' $false))
    if($Mode -ceq 'Mock') {
        foreach($fault in @('omit-lock','omit-capture','missing-receipt-identity')) {
            $rejection=''
            try{$null=Invoke-ConcurrentScenario 'same-request' $true -Mutation $fault}catch{if(-not $_.Exception.Message.StartsWith('Concurrent assertion:')){throw};$rejection=$_.Exception.Message}
            if(-not $rejection){throw ('Wrong dependency escaped common interaction assertions: '+$fault)}
            $controls.Add(@{mutation=$fault;assertion=$rejection;rejected=$true})
        }
    }
} catch {$failure=@{message=$_.Exception.Message;stack=$_.ScriptStackTrace}}
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;requirement='FR-QB-003/005 / TR-QB-AUDIT-001 / AC/TEST-QB-008/009/069';cases=$results.Count;plannedCases=2;checks=$script:concurrentChecks;negativeControls=$controls.Count;nativeProcesses=$script:concurrentProcesses.Count;failed=[int]($null -ne $failure);skipped=0;failure=$failure;results=@($results);controls=@($controls);processes=@($script:concurrentProcesses);sourceHashes=@(foreach($name in @('GovernedAuditOutbox.Concurrent.Contract.ps1','GovernedAuditOutbox.Concurrent.Worker.ps1','GovernedAuditOutbox.Concurrent.Model.ps1','GovernedAuditOutbox.Durable.Model.ps1')){@{path=$name;sha256=(Get-FileHash (Join-Path $PSScriptRoot $name)).Hash}});productionSha256=if($Mode -ceq 'Production'){(Get-FileHash (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')).Hash}else{$null};scope='Actual process-held capture/replay contention, host isolation and immutable receipt replay only; no machine-powerloss or runtime integration acceptance';productComplete=$false}
$receiptText=$receipt|ConvertTo-Json -Depth 80
Add-LinesToFile -Path $ReceiptPath -Content $receiptText *> $null
[pscustomobject]@{cases=$receipt.cases;checks=$receipt.checks;controls=$receipt.negativeControls;processes=$receipt.nativeProcesses;failed=$receipt.failed;skipped=0;failure=$failure}|ConvertTo-Json -Depth 8
if($failure){throw $failure.message}
