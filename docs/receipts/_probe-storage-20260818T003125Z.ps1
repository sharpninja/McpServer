#Requires -Version 7.0
$ErrorActionPreference = 'Continue'

$nonce = [Guid]::NewGuid().ToString('N')
$health = Invoke-WebRequest -Uri ('http://127.0.0.1:7147/health?nonce=' + $nonce) -UseBasicParsing -TimeoutSec 20
Write-Output ('HealthStatus=' + [int]$health.StatusCode)
Write-Output $health.Content
Write-Output ('NonceSent=' + $nonce)

Write-Output '--- ready ---'
try {
    $ready = Invoke-WebRequest -Uri 'http://127.0.0.1:7147/ready' -UseBasicParsing -TimeoutSec 30
    Write-Output ('ReadyStatus=' + [int]$ready.StatusCode)
    Write-Output $ready.Content
} catch {
    Write-Output ('ReadyError=' + $_.Exception.Message)
    if ($_.Exception.Response) {
        Write-Output ('ReadyHttp=' + [int]$_.Exception.Response.StatusCode)
    }
}

$svc = Get-CimInstance -ClassName Win32_Service -Filter "Name = 'McpServer'"
Write-Output ('ServiceState=' + $svc.State)
Write-Output ('ServicePid=' + $svc.ProcessId)
