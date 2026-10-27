$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output ("TARGETS_NOW=" + (Test-Path (Join-Path $wt 'build\Directory.Build.targets')))
$obj = Join-Path $wt 'build\obj\Debug'
Write-Output '==== obj Debug top ===='
if (Test-Path $obj) {
  Get-ChildItem $obj -Force | Select-Object -First 30 | ForEach-Object { $_.Name }
  $refDir = Join-Path $obj 'ref'
  Write-Output ("REF_DIR=" + (Test-Path $refDir))
  if (Test-Path $refDir) {
    Get-ChildItem $refDir -Force | ForEach-Object { $_.Name + ' ' + $_.Length }
  }
}
Write-Output '==== bin dll ===='
$bin = Join-Path $wt 'build\bin\Debug\_build.dll'
if (Test-Path $bin) { $i = Get-Item $bin; Write-Output ($i.Length.ToString() + ' ' + $i.LastWriteTimeUtc.ToString('o')) }
