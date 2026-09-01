#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'

$path = 'C:\ProgramData\McpServer\appsettings.yaml'

$before = Read-McpYamlObject -Path $path
Write-Output '--- BEFORE ---'
if ($before.Contains('AgentHelp') -and $before['AgentHelp'] -is [System.Collections.IDictionary]) {
    foreach ($key in @($before['AgentHelp'].Keys)) {
        Write-Output ('AgentHelp.' + $key + '=' + $before['AgentHelp'][$key])
    }
} else {
    Write-Output 'AgentHelp=MISSING'
}

Update-McpYamlObject -Path $path -Mutation {
    param($document)
    $existing = @{}
    if ($document['AgentHelp'] -is [System.Collections.IDictionary]) {
        foreach ($key in @($document['AgentHelp'].Keys)) {
            $existing[$key] = $document['AgentHelp'][$key]
        }
    }

    $agentHelp = [ordered]@{}
    foreach ($key in @($existing.Keys)) {
        $agentHelp[$key] = $existing[$key]
    }

    $agentHelp['Enabled'] = $true
    $agentHelp['DefaultExecutionStrategy'] = 'grok-cli'
    $agentHelp['HelperModel'] = 'grok-4.5'
    $document['AgentHelp'] = $agentHelp
} | Out-Null

$after = Read-McpYamlObject -Path $path
Write-Output '--- AFTER ---'
if ($after.Contains('AgentHelp') -and $after['AgentHelp'] -is [System.Collections.IDictionary]) {
    foreach ($key in @($after['AgentHelp'].Keys)) {
        Write-Output ('AgentHelp.' + $key + '=' + $after['AgentHelp'][$key])
    }
}

$item = Get-Item -LiteralPath $path
Write-Output ('LastWriteTimeUtc=' + $item.LastWriteTimeUtc.ToString('o'))
Write-Output ('Length=' + $item.Length)
