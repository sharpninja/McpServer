#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$svc = Get-CimInstance -ClassName Win32_Service -Filter "Name = 'McpServer'"
Write-Output ('Name=' + $svc.Name)
Write-Output ('State=' + $svc.State)
Write-Output ('StartMode=' + $svc.StartMode)
Write-Output ('StartName=' + $svc.StartName)
Write-Output ('PathName=' + $svc.PathName)

Write-Output '--- appsettings files ---'
Get-ChildItem -LiteralPath 'C:\ProgramData\McpServer' -Filter 'appsettings*' -Force |
    ForEach-Object { Write-Output ($_.FullName + ' len=' + $_.Length + ' utc=' + $_.LastWriteTimeUtc.ToString('o')) }

Write-Output '--- AgentHelp live ---'
. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'
$doc = Read-McpYamlObject -Path 'C:\ProgramData\McpServer\appsettings.yaml'
if ($doc.Contains('AgentHelp') -and $doc['AgentHelp'] -is [System.Collections.IDictionary]) {
    foreach ($key in @($doc['AgentHelp'].Keys)) {
        $value = $doc['AgentHelp'][$key]
        if ($value -is [System.Collections.IEnumerable] -and $value -isnot [string]) {
            Write-Output ('AgentHelp.' + $key + '=[' + (@($value) -join ',') + ']')
        } else {
            Write-Output ('AgentHelp.' + $key + '=' + $value)
        }
    }
} else {
    Write-Output 'AgentHelp=MISSING'
}
