$ErrorActionPreference = 'Continue'
$path = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\src\McpServer.Support.Mcp\Services\CliBrainSlotChatClient.cs'
Select-String -LiteralPath $path -Pattern 'CompleteAsync|LaunchGrok|DisableGrok|GROK_PLUGIN' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() }
Write-Output '=== WRAP ==='
Select-String -LiteralPath $path -Pattern 'void WrapWindowsCommandShim|void CreateStartInfo|ApplyAll' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() }
