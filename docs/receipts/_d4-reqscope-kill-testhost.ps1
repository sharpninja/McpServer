$ErrorActionPreference = 'Continue'
Get-Process testhost, vstest.console, 'McpServer.Support.Mcp.Tests' -ErrorAction SilentlyContinue |
    ForEach-Object {
        Write-Output ("Killing {0} pid={1}" -f $_.ProcessName, $_.Id)
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
Start-Sleep -Seconds 2
$left = @(Get-Process testhost, vstest.console, 'McpServer.Support.Mcp.Tests' -ErrorAction SilentlyContinue)
Write-Output ("Remaining testhost/vstest count={0}" -f $left.Count)
