#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-Prop {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    $p = $Object.PSObject.Properties[$Name]
    if ($null -eq $p) { return $null }
    return $p.Value
}

$events = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\events.jsonl'
$hits = Select-String -LiteralPath $events -Pattern '01f6458b'
Write-Output ('HIT_COUNT=' + @($hits).Count)

foreach ($h in $hits) {
    Write-Output ('--- line ' + $h.LineNumber + ' ---')
    $obj = $h.Line | ConvertFrom-Json
    $props = @($obj.PSObject.Properties.Name) -join ','
    Write-Output ('props=' + $props)
    Write-Output ('ts=' + (Get-Prop $obj 'ts'))
    Write-Output ('type=' + (Get-Prop $obj 'type'))
    Write-Output ('tool_name=' + (Get-Prop $obj 'tool_name'))
    Write-Output ('duration_ms=' + (Get-Prop $obj 'duration_ms'))
    Write-Output ('outcome=' + (Get-Prop $obj 'outcome'))
    $input = Get-Prop $obj 'input'
    if ($null -ne $input) {
        Write-Output ('input_type=' + $input.GetType().FullName)
        Write-Output ('input_json=' + ($input | ConvertTo-Json -Compress -Depth 6))
    }
    $cmd = Get-Prop $obj 'command'
    if ($null -ne $cmd) { Write-Output ('command=' + $cmd) }
    $args = Get-Prop $obj 'args'
    if ($null -ne $args) { Write-Output ('args_json=' + ($args | ConvertTo-Json -Compress -Depth 6)) }
    $previewLen = [Math]::Min(1500, $h.Line.Length)
    Write-Output ('preview=' + $h.Line.Substring(0, $previewLen))
}
