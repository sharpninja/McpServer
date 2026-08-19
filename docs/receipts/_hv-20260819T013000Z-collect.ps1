$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T013000Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$paths = @(
  'F:\GitHub\McpServer\src\McpServer.Services\Services\SessionLogService.cs',
  'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\SessionLogServiceTurnContextTests.cs',
  'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Plugins\InvokeWorkflowBeginTurnTests.cs',
  'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\McpStdio\SessionLogLifecycleToolErrorTests.cs',
  'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\SessionLogTriageStoreTests.cs',
  'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\SessionLogTurnContextValidatorTests.cs',
  'C:\Users\kingd\AppData\Local\Temp\grok-goal-01353e344a72\implementer\nuke-test.log',
  'C:\Users\kingd\AppData\Local\Temp\grok-goal-01353e344a72\implementer\validate-traceability.log'
)
$meta = foreach ($p in $paths) {
  $i = Get-Item -LiteralPath $p
  [pscustomobject]@{
    Path = $p
    LastWriteTimeUtc = $i.LastWriteTimeUtc.ToString('o')
    Length = $i.Length
  }
}
$meta | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outDir 'file-mtimes.json') -Encoding utf8

Push-Location 'F:\GitHub\McpServer'
try {
  git status --porcelain --untracked-files=all -- src/McpServer.Services/Services/SessionLogService.cs tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTurnContextTests.cs tests/McpServer.Support.Mcp.Tests/Plugins/InvokeWorkflowBeginTurnTests.cs tests/McpServer.Support.Mcp.Tests/McpStdio/SessionLogLifecycleToolErrorTests.cs tests/McpServer.Support.Mcp.Tests/Services/SessionLogTriageStoreTests.cs | Set-Content -LiteralPath (Join-Path $outDir 'git-status-slice.txt') -Encoding utf8
  git diff --stat -- src/McpServer.Services/Services/SessionLogService.cs tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTurnContextTests.cs tests/McpServer.Support.Mcp.Tests/Plugins/InvokeWorkflowBeginTurnTests.cs tests/McpServer.Support.Mcp.Tests/McpStdio/SessionLogLifecycleToolErrorTests.cs | Set-Content -LiteralPath (Join-Path $outDir 'git-diffstat-slice.txt') -Encoding utf8
} finally {
  Pop-Location
}

$factCount = (Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\**\*.cs' -Pattern '\[Fact\]' | Measure-Object).Count
$theoryCount = (Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\**\*.cs' -Pattern '\[Theory\]' | Measure-Object).Count
[pscustomobject]@{
  Fact = $factCount
  Theory = $theoryCount
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'support-attr-count.json') -Encoding utf8

Write-Output 'COLLECT_META_OK'
Write-Output ('FACTS=' + $factCount)
Write-Output ('THEORIES=' + $theoryCount)
