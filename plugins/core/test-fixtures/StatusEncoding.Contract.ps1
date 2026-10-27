# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-069/008: Status verification accepts native YAML/JSON cache documents.
.DESCRIPTION
Independent native-object model and isolated actual status function use the same
cache inputs and boolean/no-mutation assertions. No host entrypoint runs.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,"Run $Mode cache status assertions and write one receipt")){return}
Import-Module powershell-yaml -ErrorAction Stop
$directory=Join-Path (Split-Path -Parent $ReceiptPath) ('status-'+[guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($directory)
function Read-McpYamlObject {param([string]$Path) return ConvertFrom-Yaml ([IO.File]::ReadAllText($Path)) -Ordered}
function Invoke-MockStatus {
    param([string]$Path,[string]$Mutation='')
    try{
        $state=Read-McpYamlObject $Path
        if($Mutation -ceq 'ignore-status'){$state.status='verified'}
        if($Mutation -ceq 'invent-session'){$state.sessionId='invented'}
        return ($state -is [Collections.IDictionary] -and $state.Contains('status') -and $state.status -is [string] -and $state.status -ceq 'verified' -and $state.Contains('sessionId') -and $state.sessionId -is [string] -and -not [string]::IsNullOrWhiteSpace($state.sessionId))
    }catch{return $false}
}
$source=Join-Path $PSScriptRoot '../lib-ps/mcp-status.ps1'
if($Mode -ceq 'Production'){
    $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($source,[ref]$tokens,[ref]$errors)
    if($errors.Count){throw 'Parse failed'}
    $function=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Test-McpStatusSessionState'},$true))[0]
    if(@(Invoke-ScriptAnalyzer -ScriptDefinition $function.Extent.Text -Severity Warning,Error).Count){throw 'Function analyzer failed'}
    . ([scriptblock]::Create($function.Extent.Text))
}
$cases=@(
    @{name='valid';state=@{status='verified';sessionId='Codex-20261022T000000Z-plugin-session'};expected=$true},
    @{name='degraded';state=@{status='degraded';sessionId='Codex-20261022T000000Z-plugin-session'};expected=$false},
    @{name='empty';state=@{status='verified';sessionId=' '};expected=$false},
    @{name='null';state=@{status='verified';sessionId=$null};expected=$false},
    @{name='numeric';state=@{status='verified';sessionId=42};expected=$false},
    @{name='missing';state=@{status='verified'};expected=$false}
)
function Invoke-StatusCase {
    param([object]$Case,[string]$Format,[string]$Mutation='')
    $path=Join-Path $directory ($Case.name+'-'+$Format+'.yaml')
    $wire=if($Format -ceq 'Json'){ConvertTo-Json -InputObject $Case.state -Depth 30 -Compress}else{ConvertTo-Yaml -Data $Case.state -Options WithIndentedSequences}
    [IO.File]::WriteAllText($path,$wire,[Text.UTF8Encoding]::new($false))
    $before=(Get-FileHash $path).Hash
    $actual=if($Mode -ceq 'Mock' -or $Mutation){Invoke-MockStatus $path $Mutation}else{Test-McpStatusSessionState $path}
    if($actual -isnot [bool] -or $actual -cne $Case.expected){throw 'Verification classification changed'}
    if((Get-FileHash $path).Hash -cne $before){throw 'Status read rewrote cache'}
    return @{case=$Case.name;format=$Format;actual=$actual;sha256=$before;wire=$wire}
}
$results=@(foreach($case in $cases){foreach($format in @('Yaml','Json')){Invoke-StatusCase $case $format}})
$controls=@(foreach($control in @(@{index=1;mutation='ignore-status'},@{index=2;mutation='invent-session'})){
    $rejected=$false;try{$null=Invoke-StatusCase $cases[$control.index] Json $control.mutation}catch{$rejected=$true}
    if(-not $rejected){throw 'Wrong provider effect escaped'}
    @{mutation=$control.mutation;rejected=$rejected}
})
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;cases=$results.Count;negativeControls=$controls.Count;failed=0;skipped=0;results=$results;controls=$controls;fixtureSha256=(Get-FileHash $PSCommandPath).Hash;sourceSha256=(Get-FileHash $source).Hash;scope='Isolated status function, no lifecycle/live acceptance';productComplete=$false}
$receipt|ConvertTo-Json -Depth 30|Set-Content -LiteralPath $ReceiptPath -Encoding utf8NoBOM
[pscustomobject]$receipt|Select-Object mode,cases,negativeControls,failed,skipped|ConvertTo-Json
