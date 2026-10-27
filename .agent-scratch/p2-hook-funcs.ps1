$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\lib-ps\plugin-hook.ps1'
$lines = [System.IO.File]::ReadAllLines($path)
for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -match '^function |hookEventName|turn-opened|dialog|Stop hook|Invoke-PluginRepl') {
    Write-Output ("{0}: {1}" -f ($i + 1), $lines[$i].Trim())
  }
}
