#Requires -Version 7.0
$ErrorActionPreference = 'Continue'

$health = Invoke-WebRequest -Uri 'http://127.0.0.1:7147/health?nonce=postrestart3' -UseBasicParsing -TimeoutSec 20
Write-Output ('HealthStatus=' + [int]$health.StatusCode)
Write-Output $health.Content

Write-Output '--- ready ---'
try {
    $ready = Invoke-WebRequest -Uri 'http://127.0.0.1:7147/ready' -UseBasicParsing -TimeoutSec 20
    Write-Output ('ReadyStatus=' + [int]$ready.StatusCode)
    Write-Output $ready.Content
} catch {
    Write-Output ('ReadyError=' + $_.Exception.Message)
    if ($_.Exception.Response) {
        Write-Output ('ReadyHttp=' + [int]$_.Exception.Response.StatusCode)
    }
}
