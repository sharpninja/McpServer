$ErrorActionPreference = 'Continue'
Get-Process testhost, vstest.console -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$ErrorActionPreference = 'Stop'
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$outDir = Join-Path 'docs/receipts' "_d4-gate-$stamp"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$summary = Join-Path $outDir 'summary.txt'
function Invoke-Step([string]$Name, [scriptblock]$Body) {
    Write-Output "=== $Name ==="
    $sw = [Diagnostics.Stopwatch]::StartNew()
    & $Body
    $code = $LASTEXITCODE
    $sw.Stop()
    $line = "{0} exit={1} elapsedSec={2}" -f $Name, $code, [int]$sw.Elapsed.TotalSeconds
    Add-Content -Path $summary -Value $line
    Write-Output $line
    if ($code -ne 0) { exit $code }
}

Invoke-Step 'Client.Tests' {
    dotnet test tests/McpServer.Client.Tests/McpServer.Client.Tests.csproj -c Debug --logger "trx;LogFileName=d4-client.trx"
}
Invoke-Step 'Support.Mcp.Tests' {
    dotnet test tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj -c Debug --logger "trx;LogFileName=d4-support-unit.trx"
}
Invoke-Step 'Repl.Core.Tests' {
    dotnet test tests/McpServer.Repl.Core.Tests/McpServer.Repl.Core.Tests.csproj -c Debug --logger "trx;LogFileName=d4-repl-core.trx"
}
Invoke-Step 'Repl.IntegrationTests' {
    dotnet test tests/McpServer.Repl.IntegrationTests/McpServer.Repl.IntegrationTests.csproj -c Debug --logger "trx;LogFileName=d4-repl-int.trx"
}
Invoke-Step 'Compile' {
    pwsh.exe -NoProfile -NonInteractive -File ./build.ps1 Compile
}
Invoke-Step 'Nuke.Test' {
    pwsh.exe -NoProfile -NonInteractive -File ./build.ps1 Test
}
Invoke-Step 'ValidateTraceability' {
    pwsh.exe -NoProfile -NonInteractive -File ./build.ps1 ValidateTraceability
}
Invoke-Step 'SyncAgentPlugins' {
    pwsh.exe -NoProfile -NonInteractive -File ./build.ps1 SyncAgentPlugins
}
Write-Output "D4 remainder complete. Summary $summary"
Get-Content $summary
exit 0
