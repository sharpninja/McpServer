$ErrorActionPreference = 'Stop'
try {
    $health = Invoke-RestMethod -Uri "http://localhost:7147/health" -TimeoutSec 5
    Write-Host "MCP Health: $($health.status)"
} catch {
    Write-Host "MCP not reachable: $_"
    exit 1
}
