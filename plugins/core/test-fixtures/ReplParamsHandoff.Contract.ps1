#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode='Mock',
    [Parameter(Mandatory)][string]$ReceiptPath
)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(Test-Path -LiteralPath $ReceiptPath){throw 'Chronological receipt exists'}
$script:handoffAssertions=0
function Assert-HandoffValue {
    param([bool]$Condition,[string]$Message)
    $script:handoffAssertions++
    if(-not $Condition){throw "handoff_assert:$Message"}
}
function Invoke-HandoffModel {
    param([string]$Text,[bool]$ThrowInvoke,[string]$Fault='')
    $files=@{};$arguments=@('-Method','fixture.echo');$path=$null
    if($Text.Length){
        $path='model/private-'+[guid]::NewGuid().ToString('N')+'.yaml'
        $bytes=[Text.Encoding]::UTF8.GetBytes($Text)
        if($Fault -ceq 'lose-cr'){$bytes=[Text.Encoding]::UTF8.GetBytes($Text.Replace("`r",''))}
        if($Fault -ceq 'trim'){$bytes=[Text.Encoding]::UTF8.GetBytes($Text.Trim())}
        if($Fault -ceq 'wrong-encoding'){$bytes=[Text.Encoding]::ASCII.GetBytes($Text)}
        $files[$path]=$bytes
        $arguments+=@('-ParamsPath',$path)
    }
    if($Fault -ceq 'inline'){$arguments=@('-Method','fixture.echo','-ParamsYaml',$Text)}
    $captured='';$presentDuring=$false;$errorText='';$calls=0
    try{
        $calls++
        if($path){
            if($Fault -ceq 'missing-file'){$files.Remove($path)}
            $presentDuring=$files.ContainsKey($path)
            if(-not $presentDuring){throw 'Missing dependency file'}
            $captured=[Text.Encoding]::UTF8.GetString($files[$path])
        }
        if($ThrowInvoke){throw 'scripted_dependency_failure'}
    }catch{$errorText=$_.Exception.Message}
    finally{
        if($path -and $Fault -cnotin @('omit-cleanup','skip-finally')){$files.Remove($path)}
    }
    return @{arguments=$arguments;text=$captured;error=$errorText;calls=$calls;path=$path;presentDuring=$presentDuring;presentAfter=($path -and $files.ContainsKey($path))}
}
function Invoke-HandoffProduction {
    param([string]$Text,[bool]$ThrowInvoke)
    $wrapper=Join-Path $workspace 'plugins/core/lib-ps/Invoke-McpPlugin.ps1'
    $helper=Join-Path $workspace 'plugins/core/lib-ps/repl-params-transfer.ps1'
    . $helper
    $tokens=$null;$parseErrors=$null
    $ast=[Management.Automation.Language.Parser]::ParseFile($wrapper,[ref]$tokens,[ref]$parseErrors)
    if($parseErrors.Count){throw 'Actual wrapper parse failure'}
    $switches=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.SwitchStatementAst] -and $node.Condition.Extent.Text -ceq '$script:McpPluginInvocation.Command'},$true))
    if($switches.Count -ne 1){throw 'Actual invocation switch not unique'}
    $branches=@($switches[0].Clauses | Where-Object {$_.Item1.Value -ceq 'Invoke'})
    if($branches.Count -ne 1){throw 'Actual Invoke branch not unique'}
    $body=$branches[0].Item2.Extent.Text
    if($body -notmatch 'New-McpReplParamsTransfer' -or $body -notmatch 'Remove-McpReplParamsTransfer'){throw 'Actual file-transfer wiring absent'}
    $script:McpPluginParamsObjectBound=$true
    $script:McpPluginInvocation=@{Command='Invoke';Method='fixture.echo';Params=$Text}
    $pluginRootFull='test-only-unused-root'
    [void]$pluginRootFull # Read by the exact wrapper branch executed below.
    $shouldThrow=$ThrowInvoke
    $observation=@{arguments=@();text='';error='';calls=0;path=$null;presentDuring=$false;presentAfter=$false}
    function Resolve-PluginPowerShellScript {param([string]$Root,[string]$Name) [void]$Root;return $Name}
    function Invoke-PluginPowerShellScript {
        param([string]$ScriptPath,[string[]]$Arguments)
        [void]$ScriptPath
        $observation.calls++
        $observation.arguments=@($Arguments)
        if($Arguments.Count -eq 4){
            $observation.path=$Arguments[3]
            $observation.presentDuring=Test-Path -LiteralPath $Arguments[3] -PathType Leaf
            $observation.text=Read-McpReplParamsFile -Path $Arguments[3]
        }
        if($shouldThrow){throw 'scripted_dependency_failure'}
    }
    try{. ([scriptblock]::Create($body.Substring(1,$body.Length-2)))}
    catch{$observation.error=$_.Exception.Message}
    $observation.presentAfter=($observation.path -and (Test-Path -LiteralPath $observation.path))
    return $observation
}
function Invoke-HandoffScenario {
    param([Collections.IDictionary]$Case,[string]$Fault='')
    $actual=if($Mode -ceq 'Mock' -or $Fault){Invoke-HandoffModel -Text $Case.text -ThrowInvoke ([bool]$Case.throwInvoke) -Fault $Fault}else{Invoke-HandoffProduction -Text $Case.text -ThrowInvoke ([bool]$Case.throwInvoke)}
    Assert-HandoffValue ($actual.calls -eq 1) 'exactly one dependency invocation'
    Assert-HandoffValue ($actual.arguments.Count -eq $(if($Case.text.Length){4}else{2})) 'argument shape'
    Assert-HandoffValue ($actual.arguments[0] -ceq '-Method' -and $actual.arguments[1] -ceq 'fixture.echo') 'method retained'
    if($Case.text.Length){
        Assert-HandoffValue ($actual.arguments[2] -ceq '-ParamsPath') 'file seam instead of inline payload'
        Assert-HandoffValue ($actual.arguments[3] -ceq $actual.path -and $actual.presentDuring) 'actual dependency file available'
        Assert-HandoffValue ($actual.path.Length -lt 4096) 'bounded command-line path'
    }
    Assert-HandoffValue ($actual.text -ceq $Case.text) 'exact child-read scalar'
    Assert-HandoffValue (-not $actual.presentAfter) 'private transfer cleaned after dependency'
    Assert-HandoffValue ($actual.error -ceq $(if($Case.throwInvoke){'scripted_dependency_failure'}else{''})) 'dependency outcome retained'
    return @{name=$Case.name;characters=$actual.text.Length;arguments=$actual.arguments;path=$actual.path;presentDuring=$actual.presentDuring;presentAfter=$actual.presentAfter;error=$actual.error}
}
$numericStream=[IO.MemoryStream]::new()
$numericWriter=[System.Text.Json.Utf8JsonWriter]::new($numericStream)
try{
    $numericWriter.WriteStartObject()
    $numericWriter.WritePropertyName('accuracy');$numericWriter.WriteRawValue('98.12345678901234567890123456')
    $numericWriter.WritePropertyName('completeness');$numericWriter.WriteRawValue('98.0000')
    $numericWriter.WriteEndObject();$numericWriter.Flush()
    $numericJson=[Text.Encoding]::UTF8.GetString($numericStream.ToArray())
}finally{$numericWriter.Dispose();$numericStream.Dispose()}
$cases=@(
    @{name='empty';text=''},
    @{name='short';text='short body'},
    @{name='crlf';text="  first`r`nsecond`rlast  "},
    @{name='unicode';text=("Unicode "+[char]0x96EA+[char]0x03A9)},
    @{name='literal';text='literal \\r\\n and \\u2014'},
    @{name='large';text=('large payload '*16000)},
    @{name='numeric-json';text=$numericJson},
    @{name='failure-finally';text=" failed dependency `r`n retained ";throwInvoke=$true}
)
$controls=@(
    @{fault='inline';case='large'},@{fault='lose-cr';case='crlf'},
    @{fault='trim';case='crlf'},@{fault='wrong-encoding';case='unicode'},
    @{fault='missing-file';case='short'},@{fault='omit-cleanup';case='short'},
    @{fault='skip-finally';case='failure-finally'}
)
Write-Output 'Counted effects:8 shared wrapper transfer scenarios,7 Mock rejecting dependency controls;Production uses unchanged actual Invoke branch/helper against an injected child invocation only. One receipt; no server/native REPL or product acceptance.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Run file-handoff contract and retain chronological evidence')){return}
$observations=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new();$rejections=[Collections.Generic.List[object]]::new()
foreach($case in $cases){try{$observations.Add((Invoke-HandoffScenario -Case $case))}catch{$failures.Add(@{case=$case.name;message=$_.Exception.Message})}}
if($Mode -ceq 'Mock'){
    foreach($control in $controls){
        $case=@($cases|Where-Object name -CEQ $control.case)[0]
        try{$null=Invoke-HandoffScenario -Case $case -Fault $control.fault;$failures.Add(@{control=$control.fault;message='bad dependency accepted'})}
        catch{if($_.Exception.Message -notlike 'handoff_assert:*'){$failures.Add(@{control=$control.fault;message=$_.Exception.Message})}else{$rejections.Add(@{control=$control.fault;case=$control.case;assertion=$_.Exception.Message})}}
    }
}
$result=[ordered]@{timestamp=(Get-Date).ToString('o');mode=$Mode;cases=$observations.Count;assertions=$script:handoffAssertions;controls=$rejections.Count;failed=$failures.Count;skipped=0;observations=$observations.ToArray();rejections=$rejections.ToArray();failures=$failures.ToArray();contractSha256=(Get-FileHash -LiteralPath $PSCommandPath).Hash;requirements=@('FR-QB-003','FR-QB-005','AC-QB-069','TEST-QB-069');scope='Actual wrapper branch and private file handoff only; child invocation injected, not native REPL/server acceptance';productComplete=$false}
Add-LinesToFile -Path $ReceiptPath -Content ($result|ConvertTo-Json -Depth 30)|Out-Null
if($failures.Count -or $observations.Count -ne 8 -or ($Mode -ceq 'Mock' -and $rejections.Count -ne 7)){throw 'Handoff contract failed; receipt retained'}
[pscustomobject]$result|Select-Object mode,cases,assertions,controls,failed,skipped
