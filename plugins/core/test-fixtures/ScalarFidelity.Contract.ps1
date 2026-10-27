# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-069: Tests scalar fidelity at canonical PowerShell YAML boundaries.
.DESCRIPTION
The same inputs and ordinal assertions exercise an independent JSON codec model
and each actual production YAML serialization expression. This is a serialization
unit gate, not native transfer, server persistence, or complete AC acceptance.
Production expressions come from the parser AST and execute unchanged with their
native Data variable bound to the scenario document. No host entrypoint, marker,
environment, session, transport, or production lifecycle is executed.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode='Mock',
    [ValidateSet('PowerShell','DotNetWire')][string]$BoundarySet='PowerShell',
    [Parameter(Mandatory)][string]$ReceiptPath
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$caseTotal=if($BoundarySet -ceq 'DotNetWire'){12}else{144}
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,"Run $Mode $caseTotal scalar cases,5phases,6negativecontrols;write1receipt")){return}
Import-Module powershell-yaml -ErrorAction Stop
Import-Module PowerShell.MCP -ErrorAction Stop
$started=[DateTimeOffset]::Now.ToString('o')
$lib=Join-Path $PSScriptRoot '../lib-ps'
$script:fidelityChecks=0
$script:fidelityControls=0
$cr=[string][char]13;$lf=[string][char]10;$tab=[string][char]9
function Assert-FidelityValue {
    param([AllowNull()][object]$Actual,[AllowNull()][object]$Expected,[string]$Path='$')
    $script:fidelityChecks++
    if($null -eq $Expected){if($null -ne $Actual){throw "$Path null changed"};return}
    if($Expected -is [string]){
        if($Actual -isnot [string] -or -not [string]::Equals($Actual,$Expected,[StringComparison]::Ordinal)){throw "$Path string changed"}
        return
    }
    if($Expected -is [Collections.IDictionary]){
        if($Actual -isnot [Collections.IDictionary]){throw "$Path map changed"}
        if((@($Actual.Keys|Sort-Object -CaseSensitive)-join '|') -cne (@($Expected.Keys|Sort-Object -CaseSensitive)-join '|')){throw "$Path map keys changed"}
        foreach($key in $Expected.Keys){Assert-FidelityValue $Actual[$key] $Expected[$key] "$Path.$key"}
        return
    }
    if($Expected -is [Collections.IList]){
        if($Actual -isnot [Collections.IList] -or $Actual.Count -ne $Expected.Count){throw "$Path array changed"}
        for($i=0;$i -lt $Expected.Count;$i++){Assert-FidelityValue $Actual[$i] $Expected[$i] "$Path[$i]"}
        return
    }
    if($Actual.GetType() -ne $Expected.GetType() -or $Actual -cne $Expected){throw "$Path scalar changed"}
}
function Invoke-FidelityCodec {
    param([Collections.IDictionary]$Document,[object]$Boundary,[string]$Mutation='')
    if($Mode -ceq 'Mock' -or $Mutation){
        # Independent test-only codec: native inputs, no expected/case-name input.
        $wire=ConvertTo-Json -InputObject $Document -Depth 60 -Compress
        $copy=ConvertFrom-Json $wire -AsHashtable -DateKind String -NoEnumerate
        switch($Mutation){
            'drop-cr' {$copy.queryText=$copy.queryText.Replace([string][char]13,'')}
            'trim' {$copy.queryText=$copy.queryText.Trim()}
            'omit' {$copy.Remove('queryText')}
            'swap' {$saved=$copy.queryText;$copy.queryText=$copy.response;$copy.response=$saved}
            'round-score' {$copy.scores[0]='98'}
            'unescape-literal' {$copy.queryText=$copy.queryText.Replace('\r','').Replace('\n',[string][char]10)}
        }
        $wire=ConvertTo-Json -InputObject $copy -Depth 60 -Compress
        return @{wire=$wire;value=(ConvertFrom-Json $wire -AsHashtable -DateKind String -NoEnumerate)}
    }
    if($BoundarySet -ceq 'DotNetWire'){
        $envelope=[Activator]::CreateInstance($script:wireAssembly.GetType('QBrainAi.Repl.Core.YamlEnvelope'))
        $result=[Activator]::CreateInstance($script:wireAssembly.GetType('QBrainAi.Repl.Core.ResultPayload'))
        $result.RequestId='scalar-fixture';$result.Result=$Document
        $envelope.Type='result';$envelope.Payload=$result
        $wire=$script:wireSerializer.Serialize($envelope)
        $decoded=ConvertFrom-Yaml -Yaml $wire -Ordered
        return @{wire=$wire;value=$decoded.payload.result}
    }
    Set-Variable -Name $Boundary.dataVariable -Value $Document -Scope Local
    $wire=& ([scriptblock]::Create($Boundary.expression))
    return @{wire=($wire -join [string][char]10);value=(ConvertFrom-Yaml -Yaml ($wire -join [string][char]10) -Ordered)}
}
function Invoke-FidelityScenario {
    param([object]$Case,[object]$Boundary,[string]$Mutation='')
    $document=[ordered]@{
        queryText=$Case.text;response=('response:'+$tab+$cr+$lf+'  * exact final '+$cr+$lf)
        nested=@{dialog=@(@{content=$Case.text});empty=@();nothing=$null;flag=$true}
        scores=@('98.1234567890123456789','98.1234567890123456789012345678','98.0000')
    }
    $original=ConvertFrom-Json (ConvertTo-Json -InputObject $document -Depth 60 -Compress) -AsHashtable -DateKind String -NoEnumerate
    $trace=[Collections.Generic.List[object]]::new()
    foreach($phase in @('ingress','cache','submit','replay','readback')){
        $observation=Invoke-FidelityCodec -Document $document -Boundary $Boundary -Mutation $Mutation
        Assert-FidelityValue $observation.value $original
        $trace.Add(@{phase=$phase;wire=$observation.wire;sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($observation.wire)));observed=$observation.value})
        $document=$observation.value
    }
    return @{case=$Case.name;boundary=$Boundary.name;phases=@($trace)}
}
$cases=@(
    @{name='crlf';text=('one'+$cr+$lf+'two'+$cr+$lf)},
    @{name='cr';text=('one'+$cr+'two'+$cr)},
    @{name='lf';text=('one'+$lf+'two'+$lf)},
    @{name='mixed';text=('one'+$cr+$lf+'two'+$cr+'three'+$lf+'four'+$cr+$lf)},
    @{name='spaces';text=(' '+$tab+' leading  '+$cr+$lf+$tab+'trailing  '+$cr+$lf+$cr+$lf)},
    @{name='literal-escapes';text='literal \r\n and \u2013 and backslash \\'},
    @{name='markdown';text=('# Title'+$cr+$lf+$cr+$lf+'> quote'+$cr+$lf+$cr+$lf+'    code  '+$cr+$lf)},
    @{name='unicode';text=([string][char]0x96EA+[char]0x0301+[char]0x00E9+[char]0x2028+[char]0x2029+[char]0x2013+[char]0x2014+[char]0x2011+[char]0x2212)},
    @{name='punctuation';text=("{a: [true, null, 'quoted']} # text"+$cr+$lf+'---'+$cr+$lf)},
    @{name='empty';text=''},
    @{name='blank';text=($cr+$lf+$tab+' '+$cr+$lf)},
    @{name='long';text=(('0123456789'*1000)+$cr+$lf+('z'*1000))}
)
$boundaries=[Collections.Generic.List[object]]::new()
if($BoundarySet -ceq 'DotNetWire'){
    $assemblyPath=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../tests/QBrainAi.Repl.Core.Tests/bin/Debug/net10.0/QBrainAi.Repl.Core.dll'))
    $boundaries.Add(@{name='DotNetWire';sourceSha256=$(if($Mode -ceq 'Production'){(Get-FileHash $assemblyPath).Hash}else{'independent-json-model'})})
    if($Mode -ceq 'Production'){
        # Byte load avoids locking build outputs or silently reusing an old DLL.
        $script:wireLoadContext=[Runtime.Loader.AssemblyLoadContext]::new(('wire-fixture-'+[guid]::NewGuid().ToString('N')),$true)
        foreach($dependency in @((Join-Path ([IO.Path]::GetDirectoryName($assemblyPath)) 'YamlDotNet.dll'),$assemblyPath)){
            $stream=[IO.MemoryStream]::new([IO.File]::ReadAllBytes($dependency))
            try{$loaded=$script:wireLoadContext.LoadFromStream($stream)}finally{$stream.Dispose()}
            if($dependency -ceq $assemblyPath){$script:wireAssembly=$loaded}
        }
        $script:wireSerializer=[Activator]::CreateInstance($script:wireAssembly.GetType('QBrainAi.Repl.Core.YamlSerializer'))
    }
}else{foreach($file in @('Invoke-McpPlugin.ps1','yaml-object-mutation.ps1','repl-invoke.ps1')){
    $tokens=$null;$errors=$null
    $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $lib $file),[ref]$tokens,[ref]$errors)
    if($errors.Count){throw "Production parse errors in $file"}
    $commands=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.CommandAst] -and $node.Extent.Text -match '^ConvertTo-(Yaml -Data|Json -InputObject) \$(ParamsObject|Document|recordParams|payloadObject|callParams|summary|status|envelope)\b'},$true))
    # The legacy configuration branch is covered by TEST-MCP-YAML-MUTATION-002.
    # Governed cache callers explicitly select ExactScalars; retain its same boundary.
    if($file -ceq 'yaml-object-mutation.ps1'){$commands=@($commands|Where-Object {$_.GetCommandName() -ceq 'ConvertTo-Json'})}
    foreach($command in $commands){
        $elements=@($command.CommandElements);$dataIndex=-1
        for($i=0;$i -lt $elements.Count;$i++){if($elements[$i] -is [Management.Automation.Language.CommandParameterAst] -and $elements[$i].ParameterName -cin @('Data','InputObject')){$dataIndex=$i+1;break}}
        if($dataIndex -lt 0 -or $elements[$dataIndex] -isnot [Management.Automation.Language.VariableExpressionAst]){throw 'Unrecognized Data seam'}
        $boundaries.Add(@{name=($file+':'+$command.Extent.StartLineNumber);expression=$command.Extent.Text;dataVariable=$elements[$dataIndex].VariablePath.UserPath;sourceSha256=(Get-FileHash (Join-Path $lib $file)).Hash})
    }
}
if($boundaries.Count -ne 12){throw "Boundary inventory drift: $($boundaries.Count)"}
}
$results=@(foreach($boundary in $boundaries){foreach($case in $cases){Invoke-FidelityScenario -Case $case -Boundary $boundary}})
$controls=@(foreach($mutation in @('drop-cr','trim','omit','swap','round-score','unescape-literal')){
    $case=if($mutation -ceq 'unescape-literal'){@($cases|Where-Object name -CEQ 'literal-escapes')[0]}else{@($cases|Where-Object name -CEQ 'spaces')[0]}
    $rejection=''
    try{$null=Invoke-FidelityScenario -Case $case -Boundary $boundaries[0] -Mutation $mutation}catch{$rejection=$_.Exception.Message}
    if(-not $rejection){throw "Codec mutation escaped assertions: $mutation"}
    $script:fidelityControls++
    @{mutation=$mutation;case=$case.name;rejected=$true;assertion=$rejection}
})
$receipt=[ordered]@{
    started=$started;finished=[DateTimeOffset]::Now.ToString('o');mode=$Mode
    cases=$results.Count;inputCases=$cases.Count;boundaries=@($boundaries);checks=$script:fidelityChecks
    negativeControls=$script:fidelityControls;failed=0;skipped=0;results=$results;controls=$controls
    fixtureSha256=(Get-FileHash $PSCommandPath).Hash
    scope='Serialization unit assertions only. Dash storage normalization, native ingress, complete workflows and actual server JSON readback remain required.'
    productComplete=$false
}
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $ReceiptPath))
Add-LinesToFile -Path $ReceiptPath -Content ($receipt|ConvertTo-Json -Depth 80)|Out-Null
[pscustomobject]$receipt|Select-Object mode,cases,inputCases,checks,negativeControls,failed,skipped,fixtureSha256|ConvertTo-Json
