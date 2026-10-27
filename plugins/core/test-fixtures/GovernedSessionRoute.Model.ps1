# SPDX-License-Identifier: Apache-2.0
function Get-MockSessionRoute {
 <# Independent routing dependency model. No production parser/dispatcher imports or scenario-name outcomes. #>
 [CmdletBinding()]
 param([Collections.IDictionary]$TrustedCapture,[bool]$Governed,[string]$Mutation='')
 $captured=ConvertFrom-Json (ConvertTo-Json $TrustedCapture -Depth 30 -Compress) -AsHashtable -DateKind String
 $routing=@{governed=$Governed;mutation=$Mutation;capture=$captured;effects=[Collections.Generic.List[object]]::new()}
 $invoke={param($Method,$ParamsJson)
  $route=if($routing.governed){'governed'}else{'legacy'}
  if($routing.mutation -ceq 'legacy-in-governed'){$route='legacy'}
  if($routing.mutation -ceq 'governed-in-generic'){$route='governed'}
  $root=ConvertFrom-Json (ConvertTo-Json $routing.capture -Depth 30 -Compress) -AsHashtable -DateKind String
  $forwarded=$ParamsJson
  if($routing.mutation -ceq 'tool-root'){$body=ConvertFrom-Json $ParamsJson -AsHashtable -DateKind String;$root.workspacePath=$body.workspacePath}
  if($routing.mutation -cin @('omit-tail','reverse','coerce-review')){
   $body=ConvertFrom-Json $ParamsJson -AsHashtable -DateKind String
   foreach($field in @('actions','dialogItems')){if($body.ContainsKey($field)){
    $items=@($body[$field]);if($routing.mutation -ceq 'omit-tail'){$body[$field]=@($items[0..($items.Count-2)])}
    if($routing.mutation -ceq 'reverse'){[array]::Reverse($items);$body[$field]=$items}
   }}
   if($routing.mutation -ceq 'coerce-review'){$body.reviewJson=ConvertTo-Json (ConvertFrom-Json $body.reviewJson -AsHashtable -DateKind String) -Depth 90 -Compress}
   $forwarded=ConvertTo-Json $body -Depth 100 -Compress
  }
  $typedBatch=@()
  $body=ConvertFrom-Json $forwarded -AsHashtable -DateKind String
  foreach($field in @('actions','dialogItems')){if($body.ContainsKey($field)){$typedBatch=@($body[$field])}}
  if($routing.mutation -ceq 'omit-typed-tail'){$typedBatch=@($typedBatch[0..($typedBatch.Count-2)])}
  if($routing.mutation -ceq 'reverse-typed'){[array]::Reverse($typedBatch)}
  $fieldMutation=[regex]::Match($routing.mutation,'^typed-(actions|dialogItems)-(order|type|status|filePath|role|category|timestamp)-(wrong|missing)$')
  if($fieldMutation.Success){
   $field=$fieldMutation.Groups[2].Value
   if($fieldMutation.Groups[3].Value -ceq 'missing'){$null=$typedBatch[0].Remove($field)}
   else{$typedBatch[0][$field]=if($field -ceq 'order'){-99}elseif($field -ceq 'timestamp'){'2001-01-01T00:00:00.0000000+00:00'}else{'WRONG'}}
  }
  $routing.effects.Add(@{route=$route;captured=$root;method=$Method;paramsJson=$forwarded;typedBatch=$typedBatch})
  return @{accepted=$true}
 }.GetNewClosure()
 $inspect={return $routing.effects.ToArray()}.GetNewClosure()
 return @{Invoke=$invoke;Inspect=$inspect}
}
