# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
FR-QB-003/005 TEST-QB-069: existing installed wrapper ParamsPath parity.
.DESCRIPTION
An independent UTF8-byte model and actual script entry parameter resolver share
the same inputs and ordinal assertions. No runtime, server or installed writes.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Create12owned text fixtures,run12transfer cases and5negativecontrols;retainreceipt')){return}
Import-Module PowerShell.MCP
if($Mode -ceq 'Production'){
    $tokens=$null;$errors=$null
    $ast=[Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../lib-ps/repl-invoke.ps1')),[ref]$tokens,[ref]$errors)
    if($errors){throw 'Production parse failure'}
    $definition=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Read-ReplInvocationParam'},$true)
    if(-not $definition){throw 'Missing actual entry parameter resolver'}
    . ([scriptblock]::Create($definition.Extent.Text))
}
function Read-MockInvocationParam {
    param([string]$ParamsYaml='',[string]$ParamsPath='')
    if($PSBoundParameters.ContainsKey('ParamsPath') -and $PSBoundParameters.ContainsKey('ParamsYaml')){throw 'pass -ParamsPath or -ParamsYaml, not both'}
    if(-not $PSBoundParameters.ContainsKey('ParamsPath')){return $ParamsYaml}
    if(-not [IO.File]::Exists($ParamsPath)){throw 'ParamsPath file not found'}
    $text=[Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($ParamsPath))
    if($text.Length -and $text[0] -eq [char]0xFEFF){$text=$text.Substring(1)}
    return $text
}
$root=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../.mcpServer'))) ('params-'+[guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
function Invoke-TransferScenario {
    param([Collections.IDictionary]$Case,[string]$Mutation='')
    $arguments=@{};$expected=[string]$Case.text
    if($Case.file){
        $path=Join-Path $root ($Case.name+'-'+[guid]::NewGuid().ToString('N')+'.txt')
        if(-not $Case.missing){
            Add-LinesToFile -Path $path -Content ([string]$Case.text)|Out-Null
            # Fixture editor uses platform newlines for new files, independently
            # pinned by retained byte probes; this is setup, not target decoding.
            $expected=$expected.Replace("`r`n","`n").Replace("`n",[Environment]::NewLine)
            if($Case.bom){$expected=$expected.Substring(1)}
        }
        $arguments.ParamsPath=$path
    }elseif(-not $Case.omitted){$arguments.ParamsYaml=[string]$Case.text}
    if($Case.both){$arguments.ParamsYaml='conflicting inline'}
    $actual='';$errorText=''
    try{$actual=if($Mode -ceq 'Mock' -or $Mutation){Read-MockInvocationParam @arguments}else{Read-ReplInvocationParam @arguments}}catch{$errorText=$_.Exception.Message}
    switch($Mutation){
        'drop-cr' {$actual=$actual.Replace("`r",'')}
        'trim' {$actual=$actual.TrimEnd()}
        'literal-unescape' {$actual=$actual.Replace('\r\n',"`r`n")}
        'accept-conflict' {$errorText='';$actual='conflicting inline'}
        'accept-missing' {$errorText='';$actual=''}
    }
    if($Case.error){
        if($errorText -notmatch $Case.error){throw ('Wrong transfer refusal '+$Case.name)}
    }else{
        if($errorText){throw $errorText}
        if(-not [string]::Equals($actual,$expected,[StringComparison]::Ordinal)){throw ('Scalar changed '+$Case.name)}
    }
    return @{case=$Case.name;characters=$actual.Length;refusal=$errorText}
}
$cases=@(
    @{name='omitted';omitted=$true;text=''},
    @{name='inline-empty';text=''},
    @{name='inline-crlf';text="first`r`nsecond`rlast"},
    @{name='file-empty';file=$true;text=''},
    @{name='file-crlf';file=$true;text="first`r`nsecond`rlast"},
    @{name='file-unicode';file=$true;text=('Unicode '+[char]0x96EA)},
    @{name='file-literal';file=$true;text='literal \r\n and \u2014'},
    @{name='file-large';file=$true;text=('large payload ' * 16000)},
    @{name='file-bom';file=$true;bom=$true;text=([string][char]0xFEFF+'BOM body')},
    @{name='file-trailing';file=$true;text="keep trailing  `n"},
    @{name='missing';file=$true;missing=$true;error='file not found'},
    @{name='conflict';file=$true;text='file body';both=$true;error='not both'}
)
$results=@(foreach($case in $cases){Invoke-TransferScenario $case})
$controls=@(foreach($pair in @(@('file-crlf','drop-cr'),@('file-trailing','trim'),@('file-literal','literal-unescape'),@('conflict','accept-conflict'),@('missing','accept-missing'))){
    $case=@($cases|Where-Object name -CEQ $pair[0])[0];$rejected=$false;$reason=''
    try{$null=Invoke-TransferScenario $case $pair[1]}catch{$rejected=$true;$reason=$_.Exception.Message}
    if(-not $rejected){throw 'Wrong transfer dependency accepted'}
    @{case=$pair[0];mutation=$pair[1];rejected=$true;reason=$reason}
})
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;passed=$results.Count;failed=0;skipped=0;cases=$results;negativeControls=$controls;fixtureHash=(Get-FileHash $PSCommandPath).Hash;root=$root;requirements=@('FR-QB-003','FR-QB-005','AC-QB-069','TEST-QB-069');scope='Existing installed wrapper file-to-script entry compatibility; not live server acceptance';productComplete=$false}
Add-LinesToFile -Path $ReceiptPath -Content ($receipt|ConvertTo-Json -Depth 20)|Out-Null
[pscustomobject]$receipt|Select-Object mode,passed,failed,skipped,@{n='controls';e={$_.negativeControls.Count}}|ConvertTo-Json
