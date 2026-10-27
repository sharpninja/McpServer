$ErrorActionPreference = 'Continue'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$files = @(
    (Join-Path $root 'tests\McpServer.Support.Mcp.Tests\Services\CliBrainSlotStrategyTests.cs'),
    (Join-Path $root 'src\McpServer.Support.Mcp\Services\CliBrainSlotChatClient.cs')
)
foreach ($f in $files) {
    Write-Output ('FILE ' + $f)
    Select-String -LiteralPath $f -Pattern 'class RecordingSpawner|CreateStartInfo|StartInfos|Environment' |
        Select-Object -First 30 |
        ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() }
}
Get-ChildItem -LiteralPath (Join-Path $root 'src\McpServer.Support.Mcp') -Recurse -Filter '*.cs' |
    Select-String -Pattern 'void CreateStartInfo|ProcessStartInfo CreateStartInfo|class RecordingSpawner' |
    ForEach-Object { $_.Path.Replace($root,'') + ':' + $_.LineNumber + ':' + $_.Line.Trim() }
