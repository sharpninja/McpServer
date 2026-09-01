#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'

$path = 'C:\ProgramData\McpServer\appsettings.yaml'
$before = Read-McpYamlObject -Path $path

Write-Output '--- BEFORE ---'
if ($before.Contains('AgentHelp') -and $before['AgentHelp'] -is [System.Collections.IDictionary]) {
    foreach ($key in @($before['AgentHelp'].Keys)) {
        Write-Output ("AgentHelp." + $key + "=" + $before['AgentHelp'][$key])
    }
} else {
    Write-Output 'AgentHelp=MISSING'
}

Update-McpYamlObject -Path $path -Mutation {
    param($document)
    if ($document['AgentHelp'] -isnot [System.Collections.IDictionary]) {
        $document['AgentHelp'] = [ordered]@{}
    }
    $document['AgentHelp']['DefaultExecutionStrategy'] = 'grok-cli'
    $document['AgentHelp']['HelperModel'] = 'grok-4.5'
} | Out-Null

$after = Read-McpYamlObject -Path $path
Write-Output '--- AFTER ---'
if ($after.Contains('AgentHelp') -and $after['AgentHelp'] -is [System.Collections.IDictionary]) {
    foreach ($key in @($after['AgentHelp'].Keys)) {
        Write-Output ("AgentHelp." + $key + "=" + $after['AgentHelp'][$key])
    }
}
