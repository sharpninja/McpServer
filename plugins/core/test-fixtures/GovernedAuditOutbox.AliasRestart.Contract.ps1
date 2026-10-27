# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-008/009/069: one alias restart scenario runner for independent Model and real outbox.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$RootDirectory,[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $RootDirectory){throw 'Fixture root must be new'}
if(Test-Path -LiteralPath $ReceiptPath){throw 'Receipt exists'}
Write-Output 'Counted effects:6 recovery scenarios,4 rejecting model-effect controls, at most80 short-lived fixture processes; per-operation retained trace/result/streams;1 aggregate receipt. No live server.'
if(-not $PSCmdlet.ShouldProcess($RootDirectory,'Run isolated alias journal process recovery scenarios')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
$script:aliasChecks=0;$script:aliasProcesses=0;$script:aliasNegative=0
$worker=Join-Path $PSScriptRoot 'GovernedAuditOutbox.AliasRestart.Worker.ps1'
$pwsh=(Get-Process -Id $PID).Path
$cases=@(
    @{name='no-crash';phase='';kind='';after=1},
    @{name='before-journal-commit';phase='staged';kind='aliasJournal';after=1},
    @{name='after-journal-commit';phase='published';kind='aliasJournal';after=1},
    @{name='before-first-rewrite';phase='staged';kind='aliasEvent';after=1},
    @{name='between-rewrites';phase='published';kind='aliasEvent';after=1},
    @{name='after-second-rewrite';phase='published';kind='aliasEvent';after=2}
)
function Assert-AliasValue {
    param([bool]$Condition,[string]$Message)
    $script:aliasChecks++
    if(-not $Condition){throw ('Alias assertion: '+$Message)}
}
function Get-AliasEvent {
    param([string]$Root)
    return @(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.event.yaml'|Sort-Object Name|ForEach-Object{@{name=$_.Name;document=(ConvertFrom-Yaml ([IO.File]::ReadAllText($_.FullName)))}})
}
function Invoke-AliasWorker {
    param([hashtable]$State,[string]$Operation,[string]$Request='pending-alias-nonce',[string]$Phase='',[string]$Kind='',[int]$After=1,[string]$Mutation='')
    $State.step++
    $stem=Join-Path $State.folder ($State.step.ToString('D2')+'-'+$Operation)
    $workerArguments=@('-NoProfile','-NonInteractive','-File',$worker,'-Mode',$Mode,'-RootDirectory',$State.outbox,'-TracePath',($stem+'.trace.jsonl'),'-ResultPath',($stem+'.result.json'),'-Operation',$Operation,'-RequestId',$Request,'-CrashAfter',$After)
    if($Phase){$workerArguments+=@('-CrashPhase',$Phase,'-CrashKind',$Kind)}
    if($Mutation){$workerArguments+=@('-Mutation',$Mutation)}
    $process=Start-Process -FilePath $pwsh -ArgumentList $workerArguments -WindowStyle Hidden -WorkingDirectory $State.folder -RedirectStandardOutput ($stem+'.stdout.log') -RedirectStandardError ($stem+'.stderr.log') -PassThru
    $process.WaitForExit();$process.Refresh();$script:aliasProcesses++
    $exit=$process.ExitCode;$pidValue=$process.Id;$process.Dispose()
    $trace=if([IO.File]::Exists($stem+'.trace.jsonl')){@([IO.File]::ReadAllLines($stem+'.trace.jsonl')|Where-Object{$_}|ForEach-Object{ConvertFrom-Json $_ -AsHashtable})}else{@()}
    foreach($effect in $trace){$State.effects.Add($effect)}
    $State.processes.Add(@{pid=$pidValue;operation=$Operation;exit=$exit;trace=$stem+'.trace.jsonl';stdout=$stem+'.stdout.log';stderr=$stem+'.stderr.log'})
    Assert-AliasValue ($exit -eq $(if($Phase){75}else{0})) ('worker exit '+$Operation+' expected crash='+[bool]$Phase+' observed='+$exit)
    if($Phase){return $null}
    return ConvertFrom-Json ([IO.File]::ReadAllText($stem+'.result.json')) -AsHashtable -Depth 60
}
function Invoke-AliasScenario {
    param([hashtable]$Case,[string]$Mutation='')
    $folder=Join-Path $RootDirectory ($Case.name+$(if($Mutation){'-'+$Mutation}else{''}))
    [void][IO.Directory]::CreateDirectory($folder)
    $state=@{folder=$folder;outbox=(Join-Path $folder 'outbox');step=0;effects=[Collections.Generic.List[object]]::new();processes=[Collections.Generic.List[object]]::new()}
    $first=Invoke-AliasWorker $state 'Enqueue'
    $second=Invoke-AliasWorker $state 'Enqueue'
    Assert-AliasValue ($first.code -ceq 'queued' -and $second.code -ceq 'queued') 'both captures queued'
    $before=Get-AliasEvent $state.outbox
    Assert-AliasValue ($before.Count -eq 2) 'two immutable captures before alias'
    $null=Invoke-AliasWorker $state 'ApplyAlias' -Phase $Case.phase -Kind $Case.kind -After $Case.after -Mutation $Mutation
    $resolved=Invoke-AliasWorker $state 'Resolve' -Mutation $Mutation
    if($Case.name -ceq 'before-journal-commit'){
        Assert-AliasValue ($resolved.requestId -ceq 'pending-alias-nonce') 'uncommitted alias did not publish'
        $null=Invoke-AliasWorker $state 'ApplyAlias' -Mutation $Mutation
        $resolved=Invoke-AliasWorker $state 'Resolve' -Mutation $Mutation
    }
    Assert-AliasValue ($resolved.requestId -ceq 'req-alias-minted') 'pending resolves committed minted ID after restart'
    $after=Get-AliasEvent $state.outbox
    Assert-AliasValue ($after.Count -eq 2) 'both captures survive every alias crash'
    foreach($original in $before){
        $matchingRecords=@($after|Where-Object{$_.name -ceq $original.name})
        Assert-AliasValue ($matchingRecords.Count -eq 1) 'stable immutable filename'
        $actual=$matchingRecords[0].document.event;$expected=$original.document.event
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','eventId','order','kind','payloadJson','payloadHash')){
            Assert-AliasValue ([string]::Equals([string]$actual[$field],[string]$expected[$field],[StringComparison]::Ordinal)) ('immutable '+$field)
        }
        Assert-AliasValue ($actual.requestId -ceq 'req-alias-minted') 'only request ID rebound'
    }
    $journals=@(Get-ChildItem -LiteralPath $state.outbox -Recurse -File -Filter 'applied-aliases.yaml')
    Assert-AliasValue ($journals.Count -eq 1) 'exactly one alias journal'
    $journal=ConvertFrom-Yaml ([IO.File]::ReadAllText($journals[0].FullName))
    Assert-AliasValue (@($journal.aliases).Count -eq 1) 'exactly one committed logical alias'
    $journalHash=(Get-FileHash $journals[0].FullName).Hash
    $duplicate=Invoke-AliasWorker $state 'ApplyAlias' -Mutation $Mutation
    Assert-AliasValue ($duplicate.code -ceq 'unchanged') 'duplicate mint reply does not publish second alias'
    Assert-AliasValue ((Get-FileHash $journals[0].FullName).Hash -ceq $journalHash) 'duplicate alias leaves journal bytes unchanged'
    $canonical=Invoke-AliasWorker $state 'Resolve' -Request 'req-alias-minted' -Mutation $Mutation
    Assert-AliasValue ($canonical.requestId -ceq 'req-alias-minted') 'both IDs resolve same logical turn'
    $third=Invoke-AliasWorker $state 'Enqueue' -Request 'req-alias-minted' -Mutation $Mutation
    Assert-AliasValue ($third.code -ceq 'queued' -and $third.event.order -eq 3) 'rebind cannot reset sequence counter'
    $all=Get-AliasEvent $state.outbox
    Assert-AliasValue ($all.Count -eq 3) 'new captured event adds one record'
    Assert-AliasValue (@($all|ForEach-Object{$_.document.event.eventId}|Select-Object -Unique).Count -eq 3) 'three distinct event identities'
    $committed=$false;$rewriteEffects=0
    foreach($effect in $state.effects){
        if($effect.kind -ceq 'aliasJournal' -and $effect.phase -ceq 'published'){$committed=$true}
        if($effect.kind -ceq 'aliasEvent'){$rewriteEffects++;Assert-AliasValue $committed 'journal publication precedes every event rewrite'}
    }
    Assert-AliasValue ($committed -and $rewriteEffects -ge 4) 'observed actual journal and both event rewrite publications'
    return @{name=$Case.name;mutation=$Mutation;processes=@($state.processes);effects=@($state.effects);before=$before;after=$after;third=$third.event;temporaryFiles=@(Get-ChildItem -LiteralPath $state.outbox -Recurse -File|Where-Object Name -Like '*.tmp'|ForEach-Object FullName)}
}
$observations=[Collections.Generic.List[object]]::new()
foreach($case in $cases){$observations.Add((Invoke-AliasScenario $case))}
if($Mode -ceq 'Mock'){
    foreach($mutation in @('omit-alias','damage-alias-payload','reset-alias-order','change-alias-event')){
        $rejected=$false;$reason=''
        try{$null=Invoke-AliasScenario $cases[0] $mutation}catch{$rejected=$true;$reason=$_.Exception.Message}
        Assert-AliasValue $rejected ('wrong dependency effect rejected '+$mutation)
        $script:aliasNegative++
        $observations.Add(@{mutation=$mutation;rejected=$rejected;reason=$reason})
    }
}
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;requirement='FR-QB-003/005 / TR-QB-AUDIT-001 / AC/TEST-QB-008/009/069';cases=$cases.Count;checks=$script:aliasChecks;negativeControls=$script:aliasNegative;nativeProcesses=$script:aliasProcesses;failed=0;skipped=0;observations=@($observations);fixtureSha256=(Get-FileHash $PSCommandPath).Hash;workerSha256=(Get-FileHash $worker).Hash;modelSha256=(Get-FileHash (Join-Path $PSScriptRoot 'GovernedAuditOutbox.AliasRestart.Model.ps1')).Hash;scope='Real process exit/restart at alias journal commit and event rewrite boundaries, immutable captures and sequence recovery only. No power-loss, concurrentwriter, host integration, or full AC acceptance; abandoned temporary files retained.';productComplete=$false}
$var1=$receipt|ConvertTo-Json -Depth 80
Add-LinesToFile -Path $ReceiptPath -Content $var1 *> $null
[pscustomobject]@{cases=$receipt.cases;checks=$receipt.checks;negativeControls=$receipt.negativeControls;nativeProcesses=$receipt.nativeProcesses;failed=0;skipped=0}|ConvertTo-Json
