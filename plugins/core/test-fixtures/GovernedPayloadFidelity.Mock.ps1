#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
# Test-only independent C10/D12 model. No production helper or case-name outcomes.
function Write-FidelityModelValue {
    [CmdletBinding()]
    param([System.Text.Json.Utf8JsonWriter]$Writer,[System.Text.Json.JsonElement]$Element,[string]$Fault='')
    switch ($Element.ValueKind.ToString()) {
        Object {
            $Writer.WriteStartObject()
            foreach($property in $Element.EnumerateObject()){
                if($Fault -ceq 'drop-field' -and $property.Name -ceq 'text'){continue}
                $Writer.WritePropertyName($property.Name)
                Write-FidelityModelValue -Writer $Writer -Element $property.Value -Fault $Fault
            }
            $Writer.WriteEndObject()
        }
        Array {
            $Writer.WriteStartArray()
            foreach($elementItem in $Element.EnumerateArray()){Write-FidelityModelValue -Writer $Writer -Element $elementItem -Fault $Fault}
            $Writer.WriteEndArray()
        }
        String {
            $text=$Element.GetString()
            if($Fault -cne 'keep-decoded-dash'){$text=$text.Replace([char]0x2013,'-').Replace([char]0x2014,'-')}
            if($Fault -ceq 'normalize-literal-escape'){$text=$text.Replace('\u2014','-')}
            if($Fault -ceq 'trim-content'){$text=$text.Trim()}
            if($Fault -ceq 'lose-cr'){$text=$text.Replace("`r",'')}
            if($Fault -ceq 'change-unrelated-unicode'){$text=$text.Replace([char]0x03A9,'?')}
            if($Fault -ceq 'wrong-root' -and $text -ceq 'F:/fixture/qb'){$text='F:/fixture/other'}
            $Writer.WriteStringValue($text)
        }
        Number {
            $raw=$Element.GetRawText()
            if($Fault -ceq 'round-score'){$raw=([double]::Parse($raw,[cultureinfo]::InvariantCulture)).ToString('R',[cultureinfo]::InvariantCulture)}
            if($Fault -ceq 'drop-trailing-zero' -and $raw.Contains('.')){$raw=$raw.TrimEnd('0').TrimEnd('.')}
            $Writer.WriteRawValue($raw)
        }
        True {$Writer.WriteBooleanValue($true)}
        False {$Writer.WriteBooleanValue($false)}
        Null {$Writer.WriteNullValue()}
        default {throw 'Unexpected JSON kind'}
    }
}
function ConvertTo-FidelityModelJson {
    [CmdletBinding()]
    param([string]$Json,[string]$Fault='')
    $document=[System.Text.Json.JsonDocument]::Parse($Json)
    $stream=[IO.MemoryStream]::new()
    $writer=[System.Text.Json.Utf8JsonWriter]::new($stream)
    try {
        Write-FidelityModelValue -Writer $writer -Element $document.RootElement -Fault $Fault
        $writer.Flush()
        return [Text.Encoding]::UTF8.GetString($stream.ToArray())
    } finally {$writer.Dispose();$stream.Dispose();$document.Dispose()}
}
function Test-FidelityModelScore {
    [CmdletBinding()]
    param([System.Text.Json.JsonElement]$Element,[string]$Fault='')
    if($Element.ValueKind -ne [System.Text.Json.JsonValueKind]::Number){return $false}
    $token=$Element.GetRawText()
    if($Fault -cne 'accept-signed-exponent' -and $token -cnotmatch '^(?:0|[1-9][0-9]*)(?:\.[0-9]+)?$'){return $false}
    if($Fault -ceq 'round-threshold'){return [double]::Parse($token,[cultureinfo]::InvariantCulture) -ge 98}
    $number=[decimal]0
    if(-not [decimal]::TryParse($token,[Globalization.NumberStyles]::Float,[cultureinfo]::InvariantCulture,[ref]$number)){return $false}
    return $number -ge [decimal]98
}
function Get-FidelityModelProvider {
    [CmdletBinding()]
    param([string]$Fault='',[string]$FaultBoundary='transport')
    $store=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    $effect=[Collections.Generic.List[object]]::new()
    $modelFault=$Fault
    $modelBoundary=$FaultBoundary
    $modelLibraryPath=$PSCommandPath
    return {
        param([string]$Operation,[object]$InputObject)
        if($Operation -cne 'RoundTrip'){throw 'Unsupported fidelity model operation'}
        . $modelLibraryPath
        $store.Clear();$effect.Clear()
        $frames=[Collections.Generic.List[object]]::new()
        $normalized=ConvertTo-FidelityModelJson -Json $InputObject.json
        $captureHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normalized)))
        $current=$normalized
        foreach($boundary in @('core','host','transport','cache','outbox')){
            $faultAtBoundary=if($boundary -ceq $modelBoundary){$modelFault}else{''}
            $stageInput=if($faultAtBoundary -ceq 'keep-decoded-dash'){$InputObject.json}else{$current}
            $current=ConvertTo-FidelityModelJson -Json $stageInput -Fault $faultAtBoundary
            $store[$boundary]=$current
            $effect.Add(@{boundary=$boundary;operation='write';byteLength=[Text.Encoding]::UTF8.GetByteCount($current)})
            $published=$store[$boundary]
            $effect.Add(@{boundary=$boundary;operation='read';byteLength=[Text.Encoding]::UTF8.GetByteCount($published)})
            $parsed=[System.Text.Json.JsonDocument]::Parse($published)
            try {
                $frames.Add(@{boundary=$boundary;json=$published;accuracyAccepted=(Test-FidelityModelScore -Element $parsed.RootElement.GetProperty('accuracy') -Fault $faultAtBoundary);completenessAccepted=(Test-FidelityModelScore -Element $parsed.RootElement.GetProperty('completeness') -Fault $faultAtBoundary)})
            } finally {$parsed.Dispose()}
        }
        $faultIndex=[array]::IndexOf(@('core','host','transport','cache','outbox'),$modelBoundary)
        if($modelFault -ceq 'omit-frame'){$frames.RemoveAt($faultIndex)}
        if($modelFault -ceq 'reorder-effects'){$offset=2*$faultIndex;$temporary=$effect[$offset];$effect[$offset]=$effect[$offset+1];$effect[$offset+1]=$temporary}
        return @{frames=$frames.ToArray();effects=$effect.ToArray();capturedJson=$normalized;captureHash=$captureHash;inputJson=$InputObject.json}
    }.GetNewClosure()
}
