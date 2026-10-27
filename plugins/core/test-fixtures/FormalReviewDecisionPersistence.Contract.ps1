# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-069-FormalReviewDecisionPersistence: explicit lifecycle fields, never action promotion.
.DESCRIPTION
One runner binds independent test-only model or actual appendActions and request-builder
functions. Transport captures native submitted fields and applies additive storage.
This isolated gate does not establish live server persistence or full C7 acceptance.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Execute8explicit lifecycle scenarios and7wrong-effect controls; retain1receipt')){return}
Import-Module powershell-yaml
Import-Module PowerShell.MCP
$script:decisionEffects=[Collections.Generic.List[object]]::new()
function Copy-DecisionValue {
    param($Value)
    return ,(ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 50 -Compress) -AsHashtable -NoEnumerate -DateKind String)
}
function Submit-DecisionFixture {
    param([Collections.IDictionary]$Turn)
    $script:decisionEffects.Add((Copy-DecisionValue $Turn))
    foreach($field in @('actions','designDecisions','processingDialog')){
        foreach($item in @($Turn[$field])){
            if($null -eq $item){continue}
            $encoded=ConvertTo-Json -InputObject $item -Depth 40 -Compress
            if(-not $script:decisionStored[$field].Contains($encoded)){$script:decisionStored[$field].Add($encoded)}
        }
    }
    return $true
}
function Invoke-MockFormalDecisionAppend {
    param([string]$ParamsYaml)
    $inputDocument=ConvertFrom-Json $ParamsYaml -AsHashtable -DateKind String
    $submission=@{requestId='req-decision';actions=@();designDecisions=@();processingDialog=@()}
    foreach($item in @($inputDocument.actions)){if($null -ne $item){$submission.actions+=Copy-DecisionValue $item}}
    foreach($item in @($inputDocument.designDecisions)){if($null -ne $item){$submission.designDecisions+=[string]$item}}
    foreach($item in @($inputDocument.processingDialog)){if($null -ne $item){$submission.processingDialog+=Copy-DecisionValue $item}}
    return Submit-DecisionFixture $submission
}
if($Mode -ceq 'Production'){
    $sourcePath=Join-Path $PSScriptRoot '../lib-ps/repl-invoke.ps1'
    $parseTokens=$null;$parseErrors=$null
    $tree=[Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath($sourcePath),[ref]$parseTokens,[ref]$parseErrors)
    if($parseErrors){throw 'Actual workflow parser failure'}
    foreach($name in @('Invoke-WorkflowAppendActions','Invoke-ReplTurnUpsertParams','Convert-ReplParamsYamlToObject','Get-ReplObjectValue')){
        $definition=$tree.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $name},$true)
        if(-not $definition){throw ('Missing actual workflow function '+$name)}
        . ([scriptblock]::Create($definition.Extent.Text))
    }
    Import-Module (Join-Path $PSScriptRoot '../lib-ps/McpPluginShim.psm1') -Force
    function Get-ReplCurrentTurnFile {return $PSCommandPath}
    # Read-only fixture dependencies are named for their actual behavior. Aliases
    # bind the unchanged upstream API names without renaming production functions.
    function Test-DecisionFixtureIdentity {return $true}
    function Read-DecisionFixtureFalse {return $false}
    function Read-DecisionFixtureNoEffect {}
    Set-Alias -Name Assert-ReplCallerRequestMatches -Value Test-DecisionFixtureIdentity
    Set-Alias -Name Assert-ReplCurrentTurnFresh -Value Test-DecisionFixtureIdentity
    Set-Alias -Name Update-ReplTurnTitleFromParams -Value Read-DecisionFixtureFalse
    Set-Alias -Name Update-ReplTurnCacheEdits -Value Read-DecisionFixtureNoEffect
    Set-Alias -Name Update-ReplTurnAudit -Value Read-DecisionFixtureNoEffect
    Set-Alias -Name Set-ReplPersistPlanTodoArgs -Value Read-DecisionFixtureNoEffect
    function Get-ReplTurnCacheField {param($Field) if($Field -ceq 'turnRequestId'){return 'req-decision'};return ''}
    function Resolve-ReplPersistPlanTodo {return @{}}
    function Get-ReplCurrentTurnQueryText {return 'Original request'}
    function Get-ReplCurrentTurnValue {param($Key) if($Key -ceq 'openedAt'){return '2026-10-22T00:00:00Z'};return ''}
    function Get-ReplSessionStateValue {return 'fixture-model'}
    function Invoke-ReplPersistTurn {
        param($RequestId,$Title,$Status,$ResponseText,$ActionsYaml,$Actions,$ProcessingDialog,$DesignDecisions)
        $arguments=@{SourceType='Codex';SessionId='session-decision';RequestId=$RequestId;Title=$Title;Status=$Status;ResponseText=$ResponseText;ActionsYaml=$ActionsYaml;ProcessingDialog=$ProcessingDialog;DesignDecisions=$DesignDecisions}
        if($PSBoundParameters.ContainsKey('Actions')){$arguments.Actions=$Actions}
        $built=Invoke-ReplTurnUpsertParams @arguments
        return Submit-DecisionFixture $built.turn
    }
}
function Invoke-FormalDecisionScenario {
    param([string]$Name,[string]$Mutation='')
    $text=if($Name -ceq 'unicode'){'Decision '+[char]0x96EA}
          elseif($Name -ceq 'line-endings'){"Decision one`r`nsecond`rthird`nlast"}
          elseif($Name -ceq 'literal-escapes'){'literal \r\n and \u2014'}
          else{'Choose existing supported lifecycle; reject replacement transport'}
    $count=if($Name -ceq 'two-decisions'){2}else{1}
    $inputDocument=@{actions=@();designDecisions=@();processingDialog=@()}
    foreach($number in 1..$count){
        $value=$text+':'+$number
        $inputDocument.actions+=@{order=$number;type='design_decision';status='completed';description=$value}
        if($Name -cne 'action-only'){$inputDocument.designDecisions+=$value}
        if($Name -cne 'action-only'){$inputDocument.processingDialog+=@{timestamp=('2026-10-22T00:00:0'+$number+'Z');role='model';category='decision';content=$value}}
    }
    if($Name -ceq 'ordinary-action'){$inputDocument.actions[0].type='observation';$inputDocument.designDecisions=@();$inputDocument.processingDialog=@()}
    $expected=Copy-DecisionValue $inputDocument
    $script:decisionEffects.Clear()
    $script:decisionStored=@{}
    foreach($field in @('actions','designDecisions','processingDialog')){$script:decisionStored[$field]=[Collections.Generic.List[string]]::new()}
    $submitted=Copy-DecisionValue $inputDocument
    switch($Mutation){
        'omit-decisions' {$submitted.Remove('designDecisions')}
        'reorder-decisions' {[array]::Reverse($submitted.designDecisions)}
        'duplicate-decisions' {$submitted.designDecisions+=@($submitted.designDecisions[0])}
        'omit-dialog' {$submitted.Remove('processingDialog')}
        'reorder-actions' {[array]::Reverse($submitted.actions)}
        'drop-cr' {$submitted.designDecisions=@($submitted.designDecisions|ForEach-Object {$_.Replace("`r",'')})}
        'automatic-promotion' {$submitted.designDecisions=@($submitted.actions[0].description)}
    }
    $runs=if($Name -ceq 'replay'){2}else{1}
    foreach($run in 1..$runs){
        [void]$run
        $json=ConvertTo-Json -InputObject $submitted -Depth 50 -Compress
        $ok=if($Mutation -or $Mode -ceq 'Mock'){Invoke-MockFormalDecisionAppend $json}else{Invoke-WorkflowAppendActions -ParamsYaml $json}
        if(-not $ok){throw 'Actual lifecycle append refused'}
    }
    if($script:decisionEffects.Count -ne $runs){throw 'Transport call count mismatch'}
    foreach($effect in $script:decisionEffects){
        if($effect.requestId -cne 'req-decision'){throw 'Request identity changed'}
        foreach($field in @('actions','designDecisions','processingDialog')){
            $actual=@($effect[$field]|Where-Object {$null -ne $_})
            if((ConvertTo-Json -InputObject $actual -Depth 40 -Compress) -cne (ConvertTo-Json -InputObject @($expected[$field]) -Depth 40 -Compress)){throw ('Explicit lifecycle field differs: '+$field)}
        }
    }
    foreach($field in @('actions','designDecisions','processingDialog')){
        if($script:decisionStored[$field].Count -ne @($expected[$field]).Count){throw ('Stored replay cardinality differs: '+$field)}
    }
    return @{case=$Name;transportCalls=$script:decisionEffects.Count;storedDecisions=$script:decisionStored.designDecisions.Count;effects=@($script:decisionEffects)}
}
$cases=@('one-decision','two-decisions','unicode','line-endings','literal-escapes','replay','action-only','ordinary-action')
$results=@(foreach($name in $cases){Invoke-FormalDecisionScenario $name})
$negative=@(foreach($control in @(
    @('one-decision','omit-decisions'),@('two-decisions','reorder-decisions'),@('one-decision','duplicate-decisions'),
    @('one-decision','omit-dialog'),@('two-decisions','reorder-actions'),@('line-endings','drop-cr'),@('action-only','automatic-promotion')
)){
    $rejected=$false;$reason=''
    try{$null=Invoke-FormalDecisionScenario $control[0] $control[1]}catch{$rejected=$true;$reason=$_.Exception.Message}
    if(-not $rejected){throw ('Negative lifecycle effect escaped: '+$control[1])}
    @{case=$control[0];mutation=$control[1];rejected=$true;reason=$reason}
})
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;test='TEST-QB-069-FormalReviewDecisionPersistence';requirements=@('FR-QB-003','FR-QB-005','TR-QB-AUDIT-001','AC-QB-069','TEST-QB-069');passed=$results.Count;failed=0;skipped=0;negativeControls=$negative;cases=$results;fixtureHash=(Get-FileHash $PSCommandPath).Hash;scope='Actual isolated allowed appendActions and turn-builder functions; injected cache/transport. Live full server readback and full C7 atomic event replay remain required.';productComplete=$false}
Add-LinesToFile -Path $ReceiptPath -Content ($receipt|ConvertTo-Json -Depth 60)|Out-Null
[pscustomobject]$receipt|Select-Object mode,passed,failed,skipped,@{n='controls';e={$_.negativeControls.Count}},productComplete|ConvertTo-Json
