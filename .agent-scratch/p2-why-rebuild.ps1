$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$binTime = (Get-Item (Join-Path $root 'build\bin\Debug\_build.dll')).LastWriteTimeUtc
Write-Output ("BIN=" + $binTime.ToString('o'))
$newer = Get-ChildItem (Join-Path $root 'build') -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' -and $_.LastWriteTimeUtc -gt $binTime }
$newer | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 30 | ForEach-Object {
  Write-Output ($_.LastWriteTimeUtc.ToString('o') + ' ' + $_.FullName.Substring($root.Length))
}
Write-Output '==== msbuild up-to-date ===='
$log = 'F:\GitHub\McpServer\.agent-scratch\p2-build-n.log'
if (Test-Path $log) { Remove-Item $log -Force }
& dotnet build (Join-Path $root 'build\_build.csproj') --no-restore -v:n -p:CopyBuildOutputToOutputDirectory=false -flp:logfile=$log`;verbosity=normal
Write-Output ("BUILD_EXIT=" + $LASTEXITCODE)
Select-String -Path $log -Pattern 'not up-to-date|CoreCompile|up-to-date|CopyFiles' |
  Select-Object -First 40 |
  ForEach-Object { $_.Line.Trim() }
