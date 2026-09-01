#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Set-Location -LiteralPath 'F:\GitHub\McpServer'
$utc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.fffffffZ')
Write-Output ('UTC=' + $utc)

$paths = @(
    'src/McpServer.Support.Mcp/Controllers/ProductsController.cs',
    'src/McpServer.Client/ProductClient.cs',
    'src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs',
    'tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs',
    'tests/McpServer.Client.Tests/ProductClientTests.cs',
    'tests/McpServer.Repl.Core.Tests/GenericClientPassthroughValidClientNamesTests.cs',
    'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs',
    'src/McpServer.Client/McpServerClient.cs',
    'src/McpServer.Repl.Core/GenericClientPassthrough.cs',
    'src/McpServer.Support.Mcp/Controllers/RequirementsController.cs',
    'docs/plans/mcp-products-001.md'
)
foreach ($rel in $paths) {
    $full = Join-Path (Get-Location) $rel
    if (Test-Path -LiteralPath $full) {
        $item = Get-Item -LiteralPath $full
        Write-Output ('EXISTS ' + $rel + ' LastWriteUtc=' + $item.LastWriteTimeUtc.ToString('o') + ' Len=' + $item.Length)
    }
    else {
        Write-Output ('ABSENT ' + $rel)
    }
}

$planSha = Get-FileHash -LiteralPath 'docs/plans/mcp-products-001.md' -Algorithm SHA256
Write-Output ('PLAN_SHA256=' + $planSha.Hash)

$sessionPlanCandidates = @(
    '.grok/plans/plan.md',
    'plan.md',
    'C:\Users\kingd\.grok\plans\plan.md'
)
$foundSessionPlan = $false
foreach ($candidate in $sessionPlanCandidates) {
    if (Test-Path -LiteralPath $candidate) {
        $sessionSha = Get-FileHash -LiteralPath $candidate -Algorithm SHA256
        Write-Output ('SESSION_PLAN=' + $candidate + ' SHA256=' + $sessionSha.Hash)
        $foundSessionPlan = $true
    }
}
if (-not $foundSessionPlan) {
    Get-ChildItem -Path 'C:\Users\kingd\.grok' -Recurse -Filter 'plan.md' -ErrorAction SilentlyContinue |
        Select-Object -First 8 |
        ForEach-Object {
            $sessionSha = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
            Write-Output ('SESSION_PLAN=' + $_.FullName + ' SHA256=' + $sessionSha.Hash)
        }
}

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$sig = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
Write-Output ('MARKER_SIGNATURE=' + $sig)

$nonce = 'h3red' + [guid]::NewGuid().ToString('N')
Write-Output ('NONCE=' + $nonce)
$healthUrl = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
try {
    $health = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 30
    $health | ConvertTo-Json -Depth 8 -Compress | ForEach-Object { Write-Output ('HEALTH_JSON=' + $_) }
    $echo = $null
    if ($health.PSObject.Properties.Name -contains 'nonce') { $echo = [string]$health.nonce }
    elseif ($health.PSObject.Properties.Name -contains 'Nonce') { $echo = [string]$health.Nonce }
    Write-Output ('HEALTH_NONCE_ECHO_MATCH=' + ($echo -eq $nonce))
}
catch {
    Write-Output ('HEALTH_ERROR=' + $_.Exception.Message)
}
