#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function ConvertTo-McpGovernedPayloadJson {
    <#
    .SYNOPSIS
    FR-QB-003/005, TR-QB-AUDIT-001: Normalize decoded audit text before capture.
    .DESCRIPTION
    Retains raw JSON numeric tokens, property identities and all other string
    content. The caller captures and hashes the returned bytes once, before
    enqueueing through the existing governed outbox. No lifecycle or transport.
    .PARAMETER Json
    The original JSON object, before any numeric coercion or payload capture.
    .OUTPUTS
    Normalized JSON text. Invalid JSON or a non-object root is rejected.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param([Parameter(Mandatory)][string]$Json)

    function Write-GovernedPayloadElement {
        param(
            [Parameter(Mandatory)][System.Text.Json.Utf8JsonWriter]$Writer,
            [Parameter(Mandatory)][System.Text.Json.JsonElement]$Element
        )
        switch ($Element.ValueKind.ToString()) {
            Object {
                $Writer.WriteStartObject()
                foreach ($property in $Element.EnumerateObject()) {
                    $Writer.WritePropertyName($property.Name)
                    Write-GovernedPayloadElement -Writer $Writer -Element $property.Value
                }
                $Writer.WriteEndObject()
            }
            Array {
                $Writer.WriteStartArray()
                foreach ($item in $Element.EnumerateArray()) {
                    Write-GovernedPayloadElement -Writer $Writer -Element $item
                }
                $Writer.WriteEndArray()
            }
            String {
                $text=$Element.GetString().Replace([char]0x2013,[char]0x002D).Replace([char]0x2014,[char]0x002D)
                $Writer.WriteStringValue($text)
            }
            Number { $Writer.WriteRawValue($Element.GetRawText()) }
            True { $Writer.WriteBooleanValue($true) }
            False { $Writer.WriteBooleanValue($false) }
            Null { $Writer.WriteNullValue() }
            default { throw 'Unsupported governed JSON value' }
        }
    }

    $document=[System.Text.Json.JsonDocument]::Parse($Json)
    $stream=[IO.MemoryStream]::new()
    $writer=[System.Text.Json.Utf8JsonWriter]::new($stream)
    try {
        if ($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            throw 'Governed payload root must be a JSON object'
        }
        Write-GovernedPayloadElement -Writer $writer -Element $document.RootElement
        $writer.Flush()
        return [Text.Encoding]::UTF8.GetString($stream.ToArray())
    } finally {
        $writer.Dispose()
        $stream.Dispose()
        $document.Dispose()
    }
}

function Test-McpGovernedReviewScore {
    <#
    .SYNOPSIS
    AC/TEST-QB-012/069: Test the unsigned, non-exponent numeric >=98 contract.
    .DESCRIPTION
    Inspects the original JSON token without binary floating point or decimal
    conversion. Trailing zeros and arbitrary precision remain untouched.
    .PARAMETER Value
    A score element from the preserved JSON document, never a coerced number.
    .OUTPUTS
    True only for a Number token with the accepted spelling and value >=98.
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    param([Parameter(Mandatory)][System.Text.Json.JsonElement]$Value)

    if ($Value.ValueKind -ne [System.Text.Json.JsonValueKind]::Number) { return $false }
    $lexeme=$Value.GetRawText()
    if ($lexeme -cnotmatch '^(0|[1-9][0-9]*)(?:\.[0-9]+)?$') { return $false }
    $integer=$lexeme.Split('.')[0]
    return ($integer.Length -gt 2 -or
        ($integer.Length -eq 2 -and [string]::CompareOrdinal($integer,'98') -ge 0))
}
