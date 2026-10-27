# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
FR-QB-003/005 TR-QB-AUDIT-001 AC/TEST-QB-008/009/069: actual outbox process restart contract.
.DESCRIPTION
One common driver starts fresh processes using identical inputs, storage snapshots,
transport effects and assertions in both modes. Mock loads an independent test-only
state model. Production substitutes only the frozen outbox operation boundary.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Execute8restartcases and3rejectingcontrols with retained child streams')){return}
Import-Module powershell-yaml -ErrorAction Stop
Import-Module PowerShell.MCP -ErrorAction Stop
$base=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../.mcpServer'))) ('r-'+[guid]::NewGuid().ToString('N').Substring(0,8))
$worker=Join-Path $PSScriptRoot 'GovernedAuditOutbox.Restart.Worker.ps1'
$processes=[Collections.Generic.List[object]]::new()
function Invoke-RestartWorker {
    param([string]$Directory,[string]$Operation,[string]$Phase='',[string]$Kind='',[string]$Mutation='')
    [void][IO.Directory]::CreateDirectory($Directory)
    $run=[guid]::NewGuid().ToString('N').Substring(0,8)
    $resultPath=Join-Path $Directory ($run+'.result.json')
    $trace=Join-Path $Directory 'effects.jsonl'
    $provider=if($Mutation){'Mock'}else{$Mode}
    $arguments=@('-NoProfile','-NonInteractive','-File',$worker,'-Mode',$provider,'-RootDirectory',($Directory+'/data'),'-TracePath',$trace,'-ResultPath',$resultPath,'-Operation',$Operation)
    if($Phase){$arguments+=@('-CrashPhase',$Phase,'-CrashKind',$Kind)}
    if($Mutation){$arguments+=@('-Mutation',$Mutation)}
    $stdout=Join-Path $Directory ($run+'.stdout.log')
    $stderr=Join-Path $Directory ($run+'.stderr.log')
    $process=Start-Process -FilePath (Get-Process -Id $PID).Path -ArgumentList $arguments -WorkingDirectory ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))) -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
    try{
        if(-not $process.WaitForExit(30000)){throw 'Fixture child exceeded30seconds; process retained for diagnosis'}
        $process.Refresh()
        $result=if(Test-Path $resultPath){ConvertFrom-Json ([IO.File]::ReadAllText($resultPath)) -AsHashtable}else{$null}
        $record=@{pid=$process.Id;exitCode=$process.ExitCode;operation=$Operation;phase=$Phase;kind=$Kind;mode=$provider;mutation=$Mutation;result=$result;stdout=$stdout;stderr=$stderr;trace=$trace}
        $processes.Add($record)
        return $record
    }finally{$process.Dispose()}
}
function Get-RestartDocument {
    param([string]$Directory,[string]$Folder)
    $context=@(Get-ChildItem -LiteralPath ($Directory+'/data') -Directory)[0].FullName
    return @(Get-ChildItem -LiteralPath (Join-Path $context $Folder) -File -Force|ForEach-Object {
        @{name=$_.Name;hash=(Get-FileHash -LiteralPath $_.FullName).Hash;value=(ConvertFrom-Yaml ([IO.File]::ReadAllText($_.FullName)))}
    })
}
function Get-RestartEffect {
    param([string]$Directory)
    return @([IO.File]::ReadAllLines((Join-Path $Directory 'effects.jsonl'))|ForEach-Object {ConvertFrom-Json $_ -AsHashtable})
}
function Invoke-RestartCase {
    param([hashtable]$Case,[string]$Mutation='')
    $directory=Join-Path $base ($Case.name+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
    $seed=$null
    if($Case.operation -ceq 'Flush'){
        $seed=Invoke-RestartWorker $directory 'Enqueue'
        if($seed.exitCode -ne 0 -or $seed.result.code -cne 'queued'){throw 'Fixture seed failed'}
    }
    $crash=Invoke-RestartWorker $directory $Case.operation $Case.phase $Case.kind $Mutation
    if($crash.exitCode -ne 75){throw ('Abrupt boundary not executed:'+ $Case.name)}
    $effects=Get-RestartEffect $directory
    $boundary=@($effects|Where-Object {$_.effect -ceq 'publication' -and $_.phase -ceq $Case.phase -and $_.kind -ceq $Case.kind})[-1]
    if($Case.kind -ceq 'attempt' -and ($boundary.document.attempts -ne 1 -or $boundary.document.nextAttemptAt -ne 2000)){throw 'Attempt/deadline was not durably captured before termination'}
    $temporary=@(foreach($folder in @('','pending')){Get-RestartDocument $directory $folder|Where-Object name -Like '*.tmp'})
    $capture=if($seed){$seed.result.event}elseif($boundary.document){$boundary.document.event}else{$null}
    if($Case.name -ceq 'sequence-published'){
        $next=Invoke-RestartWorker $directory 'Enqueue'
        if($next.exitCode -ne 0 -or $next.result.event.order -ne 2){throw 'Published sequence reservation was reused'}
        $capture=$next.result.event
    }
    $replay=Invoke-RestartWorker $directory 'Flush' '' '' $Mutation
    if($replay.exitCode -ne 0 -or $replay.result.code -cne 'flushed'){throw 'Fresh process did not recover outbox state'}
    $effects=Get-RestartEffect $directory
    $submitted=@($effects|Where-Object effect -CEQ 'submit')
    $reads=@($effects|Where-Object effect -CEQ 'read')
    $sleeps=@($effects|Where-Object effect -CEQ 'sleep')
    $archive=@(Get-RestartDocument $directory 'archive')
    $pending=@(Get-RestartDocument $directory 'pending')
    $quarantine=@(Get-RestartDocument $directory 'quarantine')
    if($submitted.Count -ne $Case.calls -or $reads.Count -ne 0){throw 'Restart transport interaction count differs'}
    if($archive.Count -ne $Case.archive -or $pending.Count -ne $Case.pending -or $quarantine.Count -ne $Case.quarantine){throw 'Restart retained storage disposition differs'}
    foreach($staged in $temporary){
        if(@($quarantine|Where-Object {$_.name -ceq $staged.name -and $_.hash -ceq $staged.hash}).Count -ne 1){throw 'Abandoned staged bytes were discarded or changed'}
    }
    if($Case.name -ceq 'attempt-published'){
        if($sleeps.Count -ne 1 -or $sleeps[0].milliseconds -ne 1000){throw 'Persisted retry deadline was not observed on restart'}
    }elseif($sleeps.Count){throw 'Unexpected restart sleep effect'}
    foreach($entry in $archive){
        $record=$entry.value
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','kind','payloadJson','payloadHash')){
            if($record.event[$field] -cne $capture[$field]){throw ('Archived captured identity changed:'+ $field)}
        }
        if($record.attempts -ne $Case.attempts -or $record.receipt.code -cne 'persisted' -or $record.receipt.revision -ne 7){throw 'Archived receipt/attempt evidence differs'}
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){
            if($record.receipt[$field] -cne $capture[$field]){throw 'Receipt identity changed across restart'}
        }
    }
    foreach($call in $submitted){
        foreach($field in @('eventId','order','requestId','payloadJson','payloadHash')){
            if($call.event[$field] -cne $capture[$field]){throw 'Replay changed captured submission'}
        }
    }
    return @{case=$Case.name;directory=$directory;crash=$crash;replay=$replay;effects=$effects;archive=$archive;pending=$pending;quarantine=$quarantine}
}
$cases=@(
    @{name='sequence-staged';operation='Enqueue';phase='staged';kind='sequence';calls=0;archive=0;pending=0;quarantine=1;attempts=0},
    @{name='sequence-published';operation='Enqueue';phase='published';kind='sequence';calls=1;archive=1;pending=0;quarantine=0;attempts=1},
    @{name='event-staged';operation='Enqueue';phase='staged';kind='capture';calls=0;archive=0;pending=0;quarantine=1;attempts=0},
    @{name='event-published';operation='Enqueue';phase='published';kind='capture';calls=1;archive=1;pending=0;quarantine=0;attempts=1},
    @{name='attempt-staged';operation='Flush';phase='staged';kind='attempt';calls=0;archive=0;pending=1;quarantine=1;attempts=0},
    @{name='attempt-published';operation='Flush';phase='published';kind='attempt';calls=1;archive=1;pending=0;quarantine=0;attempts=2},
    @{name='receipt-published';operation='Flush';phase='published';kind='receipt';calls=1;archive=1;pending=0;quarantine=0;attempts=1},
    @{name='receipt-retiring';operation='Flush';phase='retiring';kind='receipt';calls=1;archive=1;pending=0;quarantine=0;attempts=1}
)
$results=[Collections.Generic.List[object]]::new()
$controls=[Collections.Generic.List[object]]::new()
$failure=$null
try{
    foreach($case in $cases){$results.Add((Invoke-RestartCase $case))}
    foreach($control in @(@{name='event-published';mutation='omit-publication'},@{name='attempt-published';mutation='omit-deadline'},@{name='receipt-published';mutation='forget-receipt'})){
        $case=@($cases|Where-Object name -CEQ $control.name)[0]
        $rejection=''
        try{$null=Invoke-RestartCase $case $control.mutation}catch{$rejection=$_.Exception.Message}
        if(-not $rejection){throw 'Incorrect durability dependency effect escaped common scenario assertions'}
        $controls.Add(@{case=$control.name;mutation=$control.mutation;assertion=$rejection;rejected=$true})
    }
}catch{$failure=@{message=$_.Exception.Message;stack=$_.ScriptStackTrace}}
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;cases=$results.Count;plannedCases=$cases.Count;negativeControls=$controls.Count;failed=$(if($failure){1}else{0});skipped=0;failure=$failure;results=@($results);controls=@($controls);processes=@($processes);fixtureSha256=(Get-FileHash $PSCommandPath).Hash;workerSha256=(Get-FileHash $worker).Hash;modelSha256=(Get-FileHash (Join-Path $PSScriptRoot 'GovernedAuditOutbox.Durable.Model.ps1')).Hash;productionSha256=$(if($Mode -ceq 'Production'){(Get-FileHash (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')).Hash}else{$null});scope='Real process termination/restart, not machine power-loss durability; alias crash windows/concurrent writers/integration remain separate';productComplete=$false}
Add-LinesToFile -Path $ReceiptPath -Content ($receipt|ConvertTo-Json -Depth 70)|Out-Null
[pscustomobject]$receipt|Select-Object mode,cases,plannedCases,negativeControls,failed,skipped,failure|ConvertTo-Json -Depth 8
if($failure){throw $failure.message}
