#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$deadline = [DateTime]::UtcNow.AddSeconds(90)
do {
    Start-Sleep -Seconds 2
    $svc = Get-Service -Name McpServer
    $proc = Get-Process -Name 'McpServer.Support.Mcp' -ErrorAction SilentlyContinue
} while ($svc.Status -ne 'Running' -and [DateTime]::UtcNow -lt $deadline)

Write-Output ('Status=' + $svc.Status)
if ($proc) {
    foreach ($item in @($proc)) {
        Write-Output ('Pid=' + $item.Id)
        Write-Output ('StartTimeUtc=' + $item.StartTime.ToUniversalTime().ToString('o'))
    }
} else {
    Write-Output 'Pid=NONE'
}

if ($svc.Status -ne 'Running') {
    throw 'McpServer service is not Running.'
}
