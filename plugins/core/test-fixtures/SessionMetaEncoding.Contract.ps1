# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-069/008: Session metadata survives native YAML or JSON cache encoding.
.DESCRIPTION
One case runner uses the independent native-object reader model or the isolated
actual Get-ReplSessionMeta function with scripted cache/context dependencies.
No production entrypoint, marker, ambient cache or network is invoked.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,"Run $Mode metadata scenarios and write receipt")){return}
Import-Module powershell-yaml -ErrorAction Stop
$root=Join-Path (Split-Path -Parent $ReceiptPath) ('meta-'+[guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
$script:metadataRoot=$root
$script:metadataReads=[Collections.Generic.List[object]]::new()
function Get-ReplInvokeCacheDir {return $script:metadataRoot}
function Get-ReplCurrentTurnValue {param([string]$Key) $script:metadataReads.Add(@{operation='current';key=$Key});return ''}
function Get-ReplCompleteTurnPersistSessionId {param([string]$CurrentTurnSessionId,[string]$ActiveSessionId) [void]$CurrentTurnSessionId;return $ActiveSessionId}
function ConvertTo-ReplCanonicalSessionId {param([string]$SessionId) return $SessionId}
function New-McpPluginSessionMeta {[CmdletBinding(SupportsShouldProcess)]param([string]$SourceType,[string]$SessionId) if($PSCmdlet.ShouldProcess($SessionId,'Return fixture metadata')){return @{SourceType=$SourceType;SessionId=$SessionId}}}
function Read-McpYamlObject {
    param([string]$Path)
    $script:metadataReads.Add(@{operation='read';path=$Path})
    return ConvertFrom-Yaml ([IO.File]::ReadAllText($Path)) -Ordered
}
function Invoke-MockSessionMeta {
    param([string]$Mutation='')
    $path=Join-Path $script:metadataRoot 'session-state.yaml'
    if(-not [IO.File]::Exists($path)){return $null}
    $state=Read-McpYamlObject $path
    if(-not $state.Contains('sessionId') -or $state.sessionId -isnot [string] -or [string]::IsNullOrWhiteSpace($state.sessionId)){return $null}
    $id=$state.sessionId
    if($Mutation -ceq 'case-fold'){$id=$id.ToLowerInvariant()}
    if($Mutation -ceq 'wrong-session'){$id='Wrong-20261022T000000Z-session'}
    $prefix=$id.Split('-')[0]
    if($Mutation -ceq 'wrong-agent'){$prefix='Other'}
    return New-McpPluginSessionMeta -SourceType $prefix -SessionId $id
}
function Assert-MetaValue {
    param([AllowNull()][object]$Actual,[AllowNull()][object]$Expected)
    if($null -eq $Expected){if($null -ne $Actual){throw 'Unexpected session'};return}
    if($null -eq $Actual -or $Actual.SourceType -cne $Expected.SourceType -or $Actual.SessionId -cne $Expected.SessionId){throw 'Captured session identity changed'}
}
$source=Join-Path $PSScriptRoot '../lib-ps/repl-invoke.ps1'
if($Mode -ceq 'Production'){
    $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($source,[ref]$tokens,[ref]$errors)
    if($errors.Count){throw 'Source parse failed'}
    $function=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-ReplSessionMeta'},$true))[0]
    $findings=@(Invoke-ScriptAnalyzer -ScriptDefinition $function.Extent.Text -Severity Warning,Error)
    if($findings.Count){throw 'Isolated function analyzer failed'}
    . ([scriptblock]::Create($function.Extent.Text))
}
$cases=@(
    @{name='yaml';format='Yaml';state=@{sessionId='Codex-20261022T000000Z-plugin-session'};expected=@{SourceType='Codex';SessionId='Codex-20261022T000000Z-plugin-session'}},
    @{name='json';format='Json';state=@{sessionId='Codex-20261022T000000Z-plugin-session'};expected=@{SourceType='Codex';SessionId='Codex-20261022T000000Z-plugin-session'}},
    @{name='json-order';format='Json';state=[ordered]@{title="one$([char]13)$([char]10)two";sessionId='Codex-20261022T000000Z-plugin-session'};expected=@{SourceType='Codex';SessionId='Codex-20261022T000000Z-plugin-session'}},
    @{name='yaml-empty';format='Yaml';state=@{sessionId=''};expected=$null},
    @{name='json-empty';format='Json';state=@{sessionId=''};expected=$null},
    @{name='json-null';format='Json';state=@{sessionId=$null};expected=$null},
    @{name='json-nonstring';format='Json';state=@{sessionId=42};expected=$null},
    @{name='json-missing';format='Json';state=@{title='fixture'};expected=$null}
)
function Invoke-MetadataCase {
    param([object]$Case,[string]$Mutation='')
    $path=Join-Path $script:metadataRoot 'session-state.yaml'
    $wire=if($Case.format -ceq 'Json'){ConvertTo-Json -InputObject $Case.state -Depth 20 -Compress}else{ConvertTo-Yaml -Data $Case.state -Options WithIndentedSequences}
    [IO.File]::WriteAllText($path,$wire,[Text.UTF8Encoding]::new($false))
    $before=(Get-FileHash $path).Hash
    $actual=if($Mode -ceq 'Mock' -or $Mutation){Invoke-MockSessionMeta -Mutation $Mutation}else{Get-ReplSessionMeta}
    Assert-MetaValue $actual $Case.expected
    if((Get-FileHash $path).Hash -cne $before){throw 'Read rewrote captured cache'}
    return @{case=$Case.name;format=$Case.format;wire=$wire;actual=$actual;expected=$Case.expected;sha256=$before}
}
$results=@(foreach($case in $cases){Invoke-MetadataCase $case})
$controls=@(foreach($mutation in @('case-fold','wrong-session','wrong-agent')){
    $rejection=''
    try{$null=Invoke-MetadataCase $cases[1] $mutation}catch{$rejection=$_.Exception.Message}
    if(-not $rejection){throw 'Incorrect identity escaped assertions'}
    @{mutation=$mutation;rejected=$true;assertion=$rejection}
})
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;cases=$results.Count;negativeControls=$controls.Count;failed=0;skipped=0;results=$results;controls=$controls;reads=@($script:metadataReads);fixtureSha256=(Get-FileHash $PSCommandPath).Hash;sourceSha256=(Get-FileHash $source).Hash;scope='Isolated metadata reader only, no full lifecycle or live persistence';productComplete=$false}
$receipt|ConvertTo-Json -Depth 30|Set-Content -LiteralPath $ReceiptPath -Encoding utf8NoBOM
[pscustomobject]$receipt|Select-Object mode,cases,negativeControls,failed,skipped|ConvertTo-Json
