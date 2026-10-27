#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode='Mock',
    [Parameter(Mandatory)][string]$ReceiptPath,
    [scriptblock]$ProviderFactory
)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $ReceiptPath){throw 'Chronological receipt exists'}
if($Mode -ceq 'Production' -and -not $ProviderFactory){throw 'Actual supported fidelity boundary provider required'}
. (Join-Path $PSScriptRoot 'GovernedPayloadFidelity.Mock.ps1')
$assertionCount=0
function Assert-FidelityCondition {
    [CmdletBinding()]
    param([bool]$Condition,[string]$Message)
    $script:assertionCount++
    if(-not $Condition){throw "fidelity_assert:$Message"}
}
function Get-FidelityInput {
    [CmdletBinding()]
    param([string]$Lexeme,[string]$Kind,[bool]$LiteralUnicode)
    $text="  # Markdown **kept** $([char]0x03A9) $([char]0x2013) $([char]0x2014) literal \u2014`r`ntrail  "
    $expectedText="  # Markdown **kept** $([char]0x03A9) - - literal \u2014`r`ntrail  "
    $stream=[IO.MemoryStream]::new()
    $options=[System.Text.Json.JsonWriterOptions]::new()
    if($LiteralUnicode){$options.Encoder=[Text.Encodings.Web.JavaScriptEncoder]::UnsafeRelaxedJsonEscaping}
    $writer=[System.Text.Json.Utf8JsonWriter]::new($stream,$options)
    try{
        $writer.WriteStartObject()
        $writer.WriteString('workspacePath','F:/fixture/qb')
        $writer.WriteString('text',$text)
        $writer.WriteStartObject('nested')
        $writer.WriteStartArray('items');$writer.WriteStringValue($text);$writer.WriteEndArray();$writer.WriteEndObject()
        foreach($name in @('accuracy','completeness')){
            $writer.WritePropertyName($name)
            switch($Kind){
                Number {$writer.WriteRawValue($Lexeme)}
                String {$writer.WriteStringValue($Lexeme)}
                Null {$writer.WriteNullValue()}
                Boolean {$writer.WriteBooleanValue($true)}
            }
        }
        $writer.WriteEndObject();$writer.Flush()
        return @{json=[Text.Encoding]::UTF8.GetString($stream.ToArray());expectedText=$expectedText;lexeme=$Lexeme;kind=$Kind}
    }finally{$writer.Dispose();$stream.Dispose()}
}
function Assert-FidelityPublishedValue {
    [CmdletBinding()]
    param([string]$Json,[object]$Vector,[string]$Label)
    $document=[System.Text.Json.JsonDocument]::Parse($Json)
    try{
        $root=$document.RootElement
        Assert-FidelityCondition (@($root.EnumerateObject()).Count -eq 5) "$Label field count"
        foreach($required in @('workspacePath','text','nested','accuracy','completeness')){
            $property=[System.Text.Json.JsonElement]::new()
            Assert-FidelityCondition ($root.TryGetProperty($required,[ref]$property)) "$Label missing $required"
        }
        Assert-FidelityCondition ($root.GetProperty('workspacePath').GetString() -ceq 'F:/fixture/qb') "$Label root"
        Assert-FidelityCondition ($root.GetProperty('text').GetString() -ceq $Vector.expectedText) "$Label decoded text"
        Assert-FidelityCondition ($root.GetProperty('nested').GetProperty('items')[0].GetString() -ceq $Vector.expectedText) "$Label nested decoded text"
        foreach($score in @('accuracy','completeness')){
            $element=$root.GetProperty($score)
            $expectedKind=switch($Vector.kind){Number{'Number'}String{'String'}Null{'Null'}Boolean{'True'}}
            Assert-FidelityCondition ($element.ValueKind.ToString() -ceq $expectedKind) "$Label $score kind"
            if($Vector.kind -ceq 'Number'){Assert-FidelityCondition ($element.GetRawText() -ceq $Vector.lexeme) "$Label $score lexical bytes"}
            if($Vector.kind -ceq 'String'){Assert-FidelityCondition ($element.GetString() -ceq $Vector.lexeme) "$Label $score string content"}
        }
    }finally{$document.Dispose()}
}
function Invoke-FidelityScenario {
    [CmdletBinding()]
    param([scriptblock]$Provider,[object]$Case)
    $response=& $Provider 'RoundTrip' $Case.input
    Assert-FidelityCondition ($response.inputJson -ceq $Case.input.json) 'source input retained'
    Assert-FidelityPublishedValue -Json $response.capturedJson -Vector $Case.input -Label capture
    $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($response.capturedJson)))
    Assert-FidelityCondition ($response.captureHash -ceq $hash) 'capture hash matches normalized captured bytes'
    Assert-FidelityCondition (@($response.frames).Count -eq 5) 'five actual published boundary frames'
    Assert-FidelityCondition (@($response.effects).Count -eq 10) 'five writes and reads'
    $boundaries=@('core','host','transport','cache','outbox')
    for($index=0;$index -lt $boundaries.Count;$index++){
        $boundary=$boundaries[$index];$frame=$response.frames[$index]
        Assert-FidelityCondition ($frame.boundary -ceq $boundary) 'boundary publication order'
        Assert-FidelityPublishedValue -Json $frame.json -Vector $Case.input -Label $boundary
        foreach($score in @('accuracyAccepted','completenessAccepted')){
            Assert-FidelityCondition ($frame[$score] -is [bool] -and $frame[$score] -eq $Case.accepted) "$boundary $score exact threshold result"
        }
        for($effectIndex=0;$effectIndex -lt 2;$effectIndex++){
            $effect=$response.effects[2*$index+$effectIndex]
            $operation=if($effectIndex -eq 0){'write'}else{'read'}
            Assert-FidelityCondition ($effect.boundary -ceq $boundary -and $effect.operation -ceq $operation) 'actual storage interaction order'
            Assert-FidelityCondition ($effect.byteLength -eq [Text.Encoding]::UTF8.GetByteCount($frame.json)) 'actual byte effect'
        }
    }
    return @{name=$Case.name;frames=$response.frames;effects=$response.effects;captureHash=$response.captureHash}
}
$cases=[Collections.Generic.List[object]]::new()
$specifications=@(
    @{name='precision19';lexeme='98.12345678901234567';kind='Number';accepted=$true},
    @{name='precision28';lexeme='98.12345678901234567890123456';kind='Number';accepted=$true},
    @{name='trailing-zero';lexeme='98.00000000000000000000000000';kind='Number';accepted=$true},
    @{name='below98';lexeme='97.99999999999999999999999999';kind='Number';accepted=$false},
    @{name='threshold98';lexeme='98';kind='Number';accepted=$true},
    @{name='no-upper-bound';lexeme='101';kind='Number';accepted=$true},
    @{name='exponent';lexeme='9.8e1';kind='Number';accepted=$false},
    @{name='signed';lexeme='-98';kind='Number';accepted=$false},
    @{name='numeric-string';lexeme='98';kind='String';accepted=$false},
    @{name='missing-score';lexeme='';kind='Null';accepted=$false},
    @{name='boolean';lexeme='';kind='Boolean';accepted=$false}
)
foreach($specification in $specifications){
    foreach($literal in @($false,$true)){
        $cases.Add(@{name=$specification.name+$(if($literal){'-literal'}else{'-escaped'});input=(Get-FidelityInput -Lexeme $specification.lexeme -Kind $specification.kind -LiteralUnicode $literal);accepted=$specification.accepted})
    }
}
$controls=@(
    @{fault='round-score';case='precision28-literal'},
    @{fault='drop-trailing-zero';case='trailing-zero-escaped'},
    @{fault='keep-decoded-dash';case='precision19-escaped'},
    @{fault='normalize-literal-escape';case='precision19-literal'},
    @{fault='trim-content';case='precision19-escaped'},
    @{fault='lose-cr';case='precision19-literal'},
    @{fault='change-unrelated-unicode';case='precision19-escaped'},
    @{fault='drop-field';case='precision19-literal'},
    @{fault='wrong-root';case='precision19-escaped'},
    @{fault='round-threshold';case='below98-literal'},
    @{fault='accept-signed-exponent';case='exponent-literal'},
    @{fault='omit-frame';case='precision19-escaped'},
    @{fault='reorder-effects';case='precision19-literal'}
)
Write-Output ("Counted effects: $($cases.Count) SAME fidelity scenarios; Mock $($controls.Count*5) rejecting controls; one chronological native receipt. Production requires explicit actual adapter; no fallback.")
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Execute shared fidelity scenarios and retain complete result')){return}
$observations=[Collections.Generic.List[object]]::new()
$failures=[Collections.Generic.List[object]]::new()
$rejections=[Collections.Generic.List[object]]::new()
foreach($case in $cases){
    try{
        $provider=if($Mode -ceq 'Mock'){Get-FidelityModelProvider}else{& $ProviderFactory}
        $observations.Add((Invoke-FidelityScenario -Provider $provider -Case $case))
    }catch{$failures.Add(@{case=$case.name;message=$_.Exception.Message})}
}
if($Mode -ceq 'Mock'){
    foreach($control in $controls){
        foreach($boundary in @('core','host','transport','cache','outbox')){
            $case=@($cases|Where-Object name -CEQ $control.case)[0]
            try{
                $null=Invoke-FidelityScenario -Provider (Get-FidelityModelProvider -Fault $control.fault -FaultBoundary $boundary) -Case $case
                $failures.Add(@{control=$control.fault;boundary=$boundary;message='corruption accepted'})
            }catch{
                if($_.Exception.Message.StartsWith('fidelity_assert:',[StringComparison]::Ordinal)){$rejections.Add(@{control=$control.fault;boundary=$boundary;assertion=$_.Exception.Message})}
                else{$failures.Add(@{control=$control.fault;boundary=$boundary;message=$_.Exception.Message;unexpected=$true})}
            }
        }
    }
}
$result=[ordered]@{timestamp=(Get-Date).ToString('o');mode=$Mode;requirements=@('FR-QB-003','FR-QB-005','TR-QB-AUDIT-001','AC-QB-012','TEST-QB-012','AC-QB-069','TEST-QB-069','C10','D12');cases=$observations.Count;assertions=$assertionCount;rejectingControls=$rejections.Count;failed=$failures.Count;skipped=0;observations=$observations.ToArray();rejections=$rejections.ToArray();failures=$failures.ToArray();sourceHashes=@(Get-FileHash -LiteralPath $PSCommandPath,(Join-Path $PSScriptRoot 'GovernedPayloadFidelity.Mock.ps1')|Select-Object Path,Hash);productComplete=$false}
Add-LinesToFile -Path $ReceiptPath -Content ($result|ConvertTo-Json -Depth 60) | Out-Null
[pscustomobject]$result|Select-Object mode,cases,assertions,rejectingControls,failed,skipped|ConvertTo-Json
if($failures.Count){throw "Fidelity gate failed: $($failures.Count)"}
