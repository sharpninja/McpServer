#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function Invoke-MockRecoveryBinding {
 [CmdletBinding()]
 param([Collections.IDictionary]$Snapshot,[AllowNull()][Collections.IDictionary]$TrustedPair)
 if(-not $TrustedPair -or $TrustedPair.mcpSessionId -isnot [string] -or [string]::IsNullOrWhiteSpace($TrustedPair.mcpSessionId) -or $TrustedPair.requestId -isnot [string] -or [string]::IsNullOrWhiteSpace($TrustedPair.requestId)){return @{ok=$false;code='request_unknown';binding=$null}}
 $target=$TrustedPair.requestId
 $aliases=@($Snapshot.aliases|Where-Object {$_.mcpSessionId -ceq $TrustedPair.mcpSessionId -and $_.pendingId -ceq $target})
 if($aliases.Count -gt 1){return @{ok=$false;code='request_ambiguous';binding=$null}}
 if($aliases.Count -eq 1){$target=$aliases[0].requestId}
 $found=[Collections.Generic.List[object]]::new()
 foreach($hostRecord in $Snapshot.hosts){foreach($turn in $hostRecord.turns){
  if($turn.workspacePath -cne $Snapshot.workspacePath -or $turn.agentIdentity -cne $Snapshot.agentIdentity){continue}
  if($turn.mcpSessionId -ceq $TrustedPair.mcpSessionId -and $turn.requestId -ceq $target){$found.Add($turn)}
 }}
 if($found.Count -ne 1){return @{ok=$false;code=$(if($found.Count){'request_ambiguous'}else{'request_unknown'});binding=$null}}
 if($aliases.Count -eq 1 -and ($aliases[0].hostSessionKey -cne $found[0].hostSessionKey -or $aliases[0].ingressNonce -cne $found[0].ingressNonce -or $aliases[0].pendingId -cne ('pending-'+$found[0].ingressNonce))){return @{ok=$false;code='request_unknown';binding=$null}}
 $copy=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
 foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')){
  if($found[0][$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($found[0][$field])){return @{ok=$false;code='request_unknown';binding=$null}}
  $copy.Add($field,$found[0][$field])
 }
 return @{ok=$true;code='resolved';binding=[Collections.ObjectModel.ReadOnlyDictionary[string,string]]::new($copy)}
}
