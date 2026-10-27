#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function Invoke-McpGovernedSessionDispatch {
    <#
    .SYNOPSIS
    FR-QB-003/005 TEST-QB-069: dispatch S6/legacy names to the canonical A8 host.
    .DESCRIPTION
    The validated host supplies captured context, private storage and existing
    typed client/policy callbacks separately from untrusted method parameters.
    This adapter owns no lifecycle state, latest-turn fallback or transport.
    .PARAMETER Envelope
    Existing method/params envelope. Root claims in params are never host context.
    .PARAMETER HostOptions
    Trusted constructor options for Invoke-McpGovernedSessionHost. Must not come
    from model/tool parameters. The caller preserves this host-owned boundary.
    .OUTPUTS
    The canonical host result, or an explicit unsupported/malformed method result.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][Collections.IDictionary]$Envelope,
          [Parameter(Mandatory)][Collections.IDictionary]$HostOptions)
    . (Join-Path $PSScriptRoot 'governed-session-host.ps1')
    $operations=@{
        'workflow.sessionlog.mintTurn'='mintTurn';'workflow.sessionlog.beginTurn'='beginTurn'
        'workflow.sessionlog.currentSession'='resolve';'workflow.sessionlog.appendActions'='appendAction'
        'workflow.sessionlog.appendDialog'='appendDialog';'workflow.sessionlog.updateTurn'='decision'
        'workflow.sessionlog.recordReview'='recordReview';'workflow.sessionlog.completeTurn'='completeTurn'
        'workflow.sessionlog.failTurn'='failTurn'
    }
    $method=$Envelope.method
    if($method -isnot [string] -or -not $operations.ContainsKey($method)){return @{ok=$false;code='method_not_found'}}
    $parameters=$Envelope.params
    if($parameters -isnot [Collections.IDictionary]){return @{ok=$false;code='invalid_params'}}
    $content=@{}
    if($parameters.Contains('requestId')){$content.requestId=$parameters.requestId}
    $operation=$operations[$method]
    if($operation -cin @('appendAction','appendDialog')){
        $field=if($operation -ceq 'appendAction'){'actions'}else{'dialogItems'}
        $items=$parameters[$field]
        if($items -isnot [Collections.IList] -or $items.Count -eq 0){return @{ok=$false;code='invalid_params'}}
        foreach($item in $items){if($item -isnot [Collections.IDictionary]){return @{ok=$false;code='invalid_params'}}}
        $result=$null
        for($index=0;$index -lt $items.Count;$index++){
            $data=@{payloadJson=(ConvertTo-Json $items[$index] -Depth 100 -Compress)}
            if($content.Contains('requestId')){$data.requestId=$content.requestId}
            if($items.Count -gt 1){$data.batchIndex=$index}
            $result=Invoke-McpGovernedSessionHost @HostOptions -Operation $operation -InputObject $data
            if(-not $result.ok){return $result}
        }
        return $result
    }
    switch -CaseSensitive ($operation){
        {$_ -cin @('mintTurn','beginTurn')} {$content.queryTitle=$parameters.queryTitle}
        'decision' {$content.payloadJson=$parameters.decisionJson}
        'recordReview' {$content.payloadJson=$parameters.reviewJson}
        'completeTurn' {$content.response=$parameters.response}
        'failTurn' {$content.failureNote=$parameters.errorMessage}
    }
    return Invoke-McpGovernedSessionHost @HostOptions -Operation $operation -InputObject $content
}
