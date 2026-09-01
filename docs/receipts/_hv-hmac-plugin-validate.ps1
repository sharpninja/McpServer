$ErrorActionPreference = 'Continue'
$utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
Write-Output "UTC=$utc"
Write-Output '=== Invoke-McpPlugin Status ==='
try {
    & 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Status -WorkspacePath 'F:\GitHub\McpServer'
    Write-Output "STATUS_EXIT=$LASTEXITCODE"
} catch {
    Write-Output "STATUS_ERROR=$($_.Exception.Message)"
    Write-Output "STATUS_EXIT=$LASTEXITCODE"
}

Write-Output '=== Test-MarkerSignature / Invoke-FullBootstrap ==='
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sig = Test-MarkerSignature -MarkerFile $marker
Write-Output "Test-MarkerSignature=$sig"
$boot = Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer'
Write-Output "Invoke-FullBootstrap=$boot"
Write-Output "BOOT_EXIT=$LASTEXITCODE"
