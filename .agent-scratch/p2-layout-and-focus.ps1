$ErrorActionPreference = 'Stop'
$pwsh = 'C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.6.0_x64__8wekyb3d8bbwe\pwsh.exe'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
Write-Output ('HEAD=' + (git rev-parse HEAD))
Write-Output ('BRANCH=' + (git branch --show-current))

$link = 'F:\GitHub\McpServer\.worktrees\mcpserver-codex-plugin'
$target = 'F:\GitHub\mcpserver-codex-plugin'
$script = Join-Path $link 'Invoke-CodexMcpPlugin.ps1'
if (Test-Path -LiteralPath $script) {
    Write-Output ('CODEX_SCRIPT_ALREADY=' + $script)
} else {
    if (Test-Path -LiteralPath $link) {
        throw "Path exists but script is missing: $link"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $target 'Invoke-CodexMcpPlugin.ps1'))) {
        throw "Real plugin script missing: $target"
    }
    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    Write-Output ('JUNCTION_CREATED=' + $link)
}
$item = Get-Item -LiteralPath $link -Force
Write-Output ('LINK_ATTRIBUTES=' + $item.Attributes)
Write-Output ('LINK_TARGET=' + $item.Target)
Write-Output ('SCRIPT_EXISTS=' + (Test-Path -LiteralPath $script))

$env:PATH = 'C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.6.0_x64__8wekyb3d8bbwe;' + $env:PATH
$pester = Join-Path $wt 'plugins\core\test-fixtures\pester'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-focus-pester.xml'
& $pwsh -NoLogo -NoProfile -NonInteractive -Command @'
$ErrorActionPreference = "Stop"
Import-Module Pester -ErrorAction Stop
$cfg = New-PesterConfiguration
$cfg.Run.Path = "F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\test-fixtures\pester\SessionLogP2Contracts.Tests.ps1","F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\test-fixtures\pester\SessionLogReviewRegression.Tests.ps1"
$cfg.Filter.FullName = "*covers primary queued and lost outcomes for every session verb*","*exposes exactly one*"
$cfg.Output.Verbosity = "Detailed"
$cfg.Run.PassThru = $true
$cfg.TestResult.Enabled = $true
$cfg.TestResult.OutputPath = "F:\GitHub\McpServer\.agent-scratch\p2-focus-pester.xml"
$result = Invoke-Pester -Configuration $cfg
Write-Output ("PESTER_TOTAL=" + $result.TotalCount + " PASSED=" + $result.PassedCount + " FAILED=" + $result.FailedCount)
if ($result.FailedCount -gt 0) { exit 1 }
exit 0
'@
Write-Output ('PESTER_EXIT=' + $LASTEXITCODE)
