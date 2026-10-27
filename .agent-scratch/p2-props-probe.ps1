$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '==== Directory.Build files ===='
Get-ChildItem -LiteralPath $wt -Recurse -Force -Include 'Directory.Build.props','Directory.Build.targets','Directory.Build.rsp' -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' } |
  ForEach-Object { $_.FullName.Substring($wt.Length) }
Write-Output '==== Build.Tests project refs ===='
Select-String -LiteralPath (Join-Path $wt 'tests\Build.Tests\Build.Tests.csproj') -Pattern 'ProjectReference|_build|CopyBuild' |
  ForEach-Object { $_.Line.Trim() }
Write-Output '==== root props head ===='
$rootProps = Join-Path $wt 'Directory.Build.props'
if (Test-Path -LiteralPath $rootProps) {
  Get-Content -LiteralPath $rootProps -TotalCount 40
} else {
  Write-Output 'NO_ROOT_PROPS'
}
