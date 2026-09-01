#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'
$doc = Read-McpYamlObject -Path 'C:\ProgramData\McpServer\appsettings.yaml'

function Show-Map {
    param($Map, $Prefix)
    if ($Map -isnot [System.Collections.IDictionary]) { return }
    foreach ($key in @($Map.Keys)) {
        $value = $Map[$key]
        $path = if ($Prefix) { "$Prefix.$key" } else { "$key" }
        if ($value -is [System.Collections.IDictionary]) {
            Show-Map -Map $value -Prefix $path
        } else {
            $text = [string]$value
            if ($path -match 'Password|ConnectionString|ApiKey') {
                if ([string]::IsNullOrWhiteSpace($text)) {
                    Write-Output ($path + '=<empty>')
                } else {
                    Write-Output ($path + '=<redacted len=' + $text.Length + '>')
                }
            } else {
                Write-Output ($path + '=' + $text)
            }
        }
    }
}

Write-Output '--- Mcp storage-related ---'
if ($doc.Contains('Mcp')) {
    $mcp = $doc['Mcp']
    foreach ($key in @('TodoStorage','Database','DataSource','DataDirectory','DataFolder')) {
        if ($mcp.Contains($key)) {
            if ($mcp[$key] -is [System.Collections.IDictionary]) {
                Show-Map -Map $mcp[$key] -Prefix ('Mcp.' + $key)
            } else {
                Write-Output ('Mcp.' + $key + '=' + $mcp[$key])
            }
        }
    }
}
if ($doc.Contains('ConnectionStrings')) {
    Write-Output '--- ConnectionStrings keys ---'
    foreach ($key in @($doc['ConnectionStrings'].Keys)) {
        $text = [string]$doc['ConnectionStrings'][$key]
        Write-Output ($key + '=<redacted len=' + $text.Length + '>')
    }
}
if ($doc.Contains('DataFolder')) {
    Write-Output ('DataFolder=' + $doc['DataFolder'])
}
