#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$MockReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot,[string]$WorkspacePath=([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))))
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath($WorkspacePath)
$root=[IO.Path]::GetFullPath($ArtifactRoot)
$allowed=[IO.Path]::GetFullPath((Join-Path $workspace '.mcpServer'))+[IO.Path]::DirectorySeparatorChar
if(-not $root.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Own synthetic artifact root required'}
if(Test-Path -LiteralPath $ReceiptPath){throw 'Chronological receipt exists'}
$gate=[IO.File]::ReadAllText($MockReceiptPath)|ConvertFrom-Json -DateKind String
if($gate.mode -cne 'Mock' -or $gate.cases -ne 21 -or $gate.assertions -ne 256 -or $gate.negativeControls -ne 11 -or $gate.failed -ne 0 -or $gate.skipped -ne 0){throw 'Exact independent 21/256/11 prerequisite required'}
foreach($pin in $gate.sourcePins){if((Get-FileHash -LiteralPath $pin.Path).Hash -cne $pin.Hash){throw 'Frozen common assertions/model/original inputs changed'}}
Write-Output 'Counted effects:21 private storage fixtures, up to21 exact seed transfers and retained copies, one held-lock refusal, SAME original inputs/assertions and11 model controls, one chronological receipt. No real workspace turns, server writes, native host acceptance or new lifecycle.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Bind common serialized recovery assertions to existing durable store')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
. (Join-Path $workspace 'plugins/core/lib-ps/repl-params-transfer.ps1')
. (Join-Path $workspace 'plugins/core/lib-ps/governed-turn-store.ps1')
$transferCall=(Get-Command New-McpReplParamsTransfer).ScriptBlock
$storeCall=(Get-Command Invoke-McpGovernedTurnStore).ScriptBlock
$provider={
    param([hashtable]$Scenario)
    $directory=Join-Path $root ([guid]::NewGuid().ToString('N'))
    $null=New-Item -ItemType Directory -Path $directory -Force
    $path=Join-Path $directory 'turn-store.json'
    $lockPath=Join-Path $directory 'turn-store.lock'
    $seed=$null
    if($null -ne $Scenario.serialized){
        # The existing exact private UTF-8 writer preserves compact fixture bytes.
        # Copy, hash-verify and retain the original; no queued user record is used.
        $seed=& $transferCall -ParamsText $Scenario.serialized
        Copy-Item -LiteralPath $seed.Path -Destination $path -ErrorAction Stop
        if((Get-FileHash -LiteralPath $seed.Path).Hash -cne (Get-FileHash -LiteralPath $path).Hash -or [IO.File]::ReadAllText($path) -cne $Scenario.serialized){throw 'Exact fixture seed differs'}
    }
    $before=if([IO.File]::Exists($path)){[IO.File]::ReadAllText($path)}else{$null}
    $publications=[Collections.Generic.List[string]]::new()
    $observer={param([string]$Stage)$publications.Add($Stage)}.GetNewClosure()
    $held=$null
    try{
        if(-not $Scenario.lockAvailable){$held=[IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)}
        $pair=if($null -eq $Scenario.trustedPair){@{}}else{$Scenario.trustedPair}
        $result=& $storeCall -Directory $directory -Context $Scenario.context -Operation ResolveOriginal -InputObject $pair -PublicationObserver $observer
    }finally{if($null -ne $held){$held.Dispose()}}
    $after=if([IO.File]::Exists($path)){[IO.File]::ReadAllText($path)}else{$null}
    return @{result=$result;before=$before;after=$after;writes=$publications.Count;effects=@($publications);fixtureDirectory=$directory;retainedSeedPath=if($seed){$seed.Path}else{$null};storePath=$path}
}.GetNewClosure()
& (Join-Path $PSScriptRoot 'SerializedRecovery.Contract.ps1') -Mode Production -ProductionInvoke $provider -ReceiptPath $ReceiptPath


