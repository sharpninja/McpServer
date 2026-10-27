# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-069: complete session GetAsync graph and request interactions through one assertion runner.
.DESCRIPTION
Mock uses an independent JSON transport model. Production uses the actual typed client and
GenericClientPassthrough with an injected scripted HttpMessageHandler, never a real endpoint.
The same input documents, ordered graph assertions and HTTP observations apply in both modes.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $ReceiptPath){throw 'Receipt exists'}
Write-Output 'Counted effects: 6 full-graph cases; 60 rejecting response/effect controls; 66 scripted dependency invocations with 60 recorded requests; one receipt. No live server or product data writes.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,"Run $Mode session readback contract")){return}
Import-Module PowerShell.MCP -ErrorAction Stop
$script:readbackChecks=0
$script:readbackInteractions=0
$script:readbackControls=0
$session='Codex-20261022T040000Z-readback-fixture'
$workspace='F:/fixture/qb'
$route='/mcpserver/sessionlog/Codex/'+$session
$inputs=@("first`r`nsecond`rlast",'',"Unicode $([char]0x96ea)", 'literal \r\n and \u2014',('long line '*1000),"line`nline")
function Assert-ReadbackValue {
    param([AllowNull()][object]$Actual,[AllowNull()][object]$Expected,[string]$Path='$')
    $script:readbackChecks++
    if($null -eq $Expected){if($null -ne $Actual){throw "$Path null changed"};return}
    if($Expected -is [System.Collections.IDictionary]){
        if($Actual -isnot [System.Collections.IDictionary]){throw "$Path map missing"}
        foreach($key in $Expected.Keys){if(-not $Actual.Contains($key)){throw "$Path.$key missing"};Assert-ReadbackValue $Actual[$key] $Expected[$key] ($Path+'.'+$key)}
        return
    }
    if($Expected -is [array]){
        if($Actual -isnot [array] -or $Actual.Count -ne $Expected.Count){throw "$Path array changed"}
        for($i=0;$i -lt $Expected.Count;$i++){Assert-ReadbackValue $Actual[$i] $Expected[$i] ($Path+'['+$i+']')}
        return
    }
    if($Expected -is [string]){if($Actual -isnot [string] -or -not [string]::Equals($Actual,$Expected,[StringComparison]::Ordinal)){throw "$Path string changed"};return}
    if($Actual -cne $Expected){throw "$Path scalar changed"}
}
function Invoke-IndependentReadbackTransport {
    param([string]$Body,[string]$Fault='')
    # A dependency model decodes scripted server bytes and records the request it receives.
    $request=[ordered]@{method='GET';path=$route;workspace=$workspace}
    if($Fault -ceq 'wrongMethod'){$request.method='POST'}
    if($Fault -ceq 'wrongRoute'){$request.path='/wrong-route'}
    if($Fault -ceq 'wrongWorkspace'){$request.workspace='wrong-workspace'}
    $requests=if($Fault -ceq 'missingRequest'){@()}else{@($request)}
    $script:readbackInteractions+=@($requests).Count
    @{document=(ConvertFrom-Json $Body -AsHashtable -Depth 100);requests=@($requests)}
}
if($Mode -ceq 'Production'){
    $directory=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../src/QBrainAi.Repl.Host/bin/Debug/net10.0'))
    $resolver={param($LoadContext,$AssemblyName) [void]$LoadContext;$path=Join-Path $directory ($AssemblyName.Name+'.dll');if([IO.File]::Exists($path)){return [Reflection.Assembly]::LoadFrom($path)};return $null}.GetNewClosure()
    [Runtime.Loader.AssemblyLoadContext]::Default.add_Resolving($resolver)
    [void][Reflection.Assembly]::LoadFrom((Join-Path $directory 'QBrainAi.Client.dll'))
    [void][Reflection.Assembly]::LoadFrom((Join-Path $directory 'QBrainAi.Repl.Core.dll'))
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
public sealed class ScriptedSessionReadbackHandler : HttpMessageHandler
{
    public string Body { get; set; } = "";
    public string Fault { get; set; } = "";
    public List<string[]> Requests { get; } = new();
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var workspace = request.Headers.TryGetValues("X-Workspace-Path", out var values) ? string.Join(",", values) : "";
        if (Fault != "missingRequest") Requests.Add(new[] {
            Fault == "wrongMethod" ? "POST" : request.Method.Method,
            Fault == "wrongRoute" ? "/wrong-route" : request.RequestUri!.AbsolutePath,
            Fault == "wrongWorkspace" ? "wrong-workspace" : workspace });
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Body, Encoding.UTF8, "application/json") });
    }
}
'@ -ErrorAction Stop
}
function Invoke-ReadbackBoundary {
    param([string]$Body,[string]$Fault='')
    if($Mode -ceq 'Mock'){return Invoke-IndependentReadbackTransport $Body $Fault}
    $handler=[ScriptedSessionReadbackHandler]::new();$handler.Body=$Body;$handler.Fault=$Fault
    $http=[Net.Http.HttpClient]::new($handler)
    try{
        $options=[QBrainAi.Client.QBrainAiClientOptions]::new();$options.BaseUrl=[Uri]'http://fixture.invalid';$options.WorkspacePath=$workspace;$options.ApiKey='fixture-only-no-server'
        $client=[QBrainAi.Client.QBrainAiClient]::new($http,$options)
        $passthrough=[QBrainAi.Repl.Core.GenericClientPassthrough]::new($client)
        $arguments=[System.Collections.Generic.Dictionary[string,object]]::new();$arguments['agent']='Codex';$arguments['sessionId']=$session
        $actual=$passthrough.InvokeAsync('SessionLog','GetAsync',$arguments,[Threading.CancellationToken]::None).GetAwaiter().GetResult()
        $requests=@(foreach($request in $handler.Requests){$script:readbackInteractions++;[ordered]@{method=$request[0];path=$request[1];workspace=$request[2]}})
        # Assert the actual camelCase wire representation, not PowerShell's DTO reflection.
        $envelope=[QBrainAi.Repl.Core.YamlEnvelope]::new()
        $envelope.Type='result'
        $payload=[QBrainAi.Repl.Core.ResultPayload]::new()
        $payload.RequestId='fixture-readback'
        $payload.Result=$actual
        $envelope.Payload=$payload
        $wire=[QBrainAi.Repl.Core.YamlSerializer]::new().Serialize($envelope)
        $decoded=ConvertFrom-Yaml $wire
        # Materialize decoded YAML sequences as the JSON arrays used by the common graph model.
        @{document=($decoded.payload.result|ConvertTo-Json -Depth 100|ConvertFrom-Json -AsHashtable -Depth 100);requests=$requests}
    }finally{$http.Dispose()}
}
function Assert-ReadbackObservation {
    param($Observation,$Expected)
    Assert-ReadbackValue $Observation.document $Expected
    Assert-ReadbackValue $Observation.requests @([ordered]@{method='GET';path=$route;workspace=$workspace}) '$.requests'
}
try{
    foreach($text in $inputs){
        $expected=[ordered]@{sourceType='Codex';sessionId=$session;model='fixture-model';turns=@([ordered]@{requestId='req-fixture-1';queryText=$text;response='response';status='in_progress';designDecisions=@($text,'second decision');actions=@([ordered]@{order=1;type='design_decision';description=$text},[ordered]@{order=2;type='observation';description='second action'});processingDialog=@([ordered]@{role='model';category='decision';content=$text},[ordered]@{role='model';category='reasoning';content='second dialog'})},[ordered]@{requestId='req-fixture-2';queryText='second query';status='completed'})}
        $body=$expected|ConvertTo-Json -Depth 30 -Compress
        Assert-ReadbackObservation (Invoke-ReadbackBoundary $body) $expected
        foreach($fault in @('missingDecision','wrongAction','reorderedAction','missingDialog','wrongSession','missingTurn')){
            $damaged=ConvertFrom-Json $body -AsHashtable -Depth 100
            switch($fault){
                'missingDecision' {$damaged.turns[0].designDecisions=@('second decision')}
                'wrongAction' {$damaged.turns[0].actions[0].description='wrong dependency response'}
                'reorderedAction' {$damaged.turns[0].actions=@($damaged.turns[0].actions[1],$damaged.turns[0].actions[0])}
                'missingDialog' {$damaged.turns[0].Remove('processingDialog')}
                'wrongSession' {$damaged.sessionId='wrong-session'}
                'missingTurn' {$damaged.turns=@($damaged.turns[0])}
            }
            $observed=Invoke-ReadbackBoundary ($damaged|ConvertTo-Json -Depth 30 -Compress)
            $rejected=$false
            try{Assert-ReadbackObservation $observed $expected}catch{$rejected=$true;$script:readbackControls++}
            if(-not $rejected){throw "Wrong server response accepted: $fault"}
        }
        foreach($fault in @('wrongMethod','wrongRoute','wrongWorkspace','missingRequest')){
            $observed=Invoke-ReadbackBoundary $body $fault
            $rejected=$false
            try{Assert-ReadbackObservation $observed $expected}catch{$rejected=$true;$script:readbackControls++}
            if(-not $rejected){throw "Wrong transport effect accepted: $fault"}
        }
    }
    $receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;requirement='FR-QB-003/005 / TR-QB-AUDIT-001 / TEST-QB-069';cases=$inputs.Count;checks=$script:readbackChecks;negativeControls=$script:readbackControls;scriptedHttpInteractions=$script:readbackInteractions;failed=0;skipped=0;fixtureSha256=(Get-FileHash $PSCommandPath).Hash;scope='Typed client and generic REPL GetAsync with independent scripted HTTP dependency. No live server/storage acceptance.';productComplete=$false}
    $var1=$receipt|ConvertTo-Json -Depth 10
    Add-LinesToFile -Path $ReceiptPath -Content $var1 *> $null
    $receipt|ConvertTo-Json -Depth 10
}finally{if($Mode -ceq 'Production'){[Runtime.Loader.AssemblyLoadContext]::Default.remove_Resolving($resolver)}}
