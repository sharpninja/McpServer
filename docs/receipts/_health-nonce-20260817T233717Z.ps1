#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'

$nonce = [Guid]::NewGuid().ToString('N')
$uri = 'http://127.0.0.1:7147/health?nonce=' + $nonce
$response = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 20
Write-Output ('HealthStatusCode=' + [int]$response.StatusCode)
Write-Output ('NonceSent=' + $nonce)
Write-Output ('Body=' + $response.Content)

$doc = Read-McpYamlObject -Path 'C:\ProgramData\McpServer\appsettings.yaml'
Write-Output '--- AgentHelp after restart ---'
if ($doc.Contains('AgentHelp') -and $doc['AgentHelp'] -is [System.Collections.IDictionary]) {
    foreach ($key in @($doc['AgentHelp'].Keys)) {
        Write-Output ('AgentHelp.' + $key + '=' + $doc['AgentHelp'][$key])
    }
} else {
    Write-Output 'AgentHelp=MISSING'
}
