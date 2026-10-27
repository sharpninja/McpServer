$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build'
$binDll = Get-Item (Join-Path $root 'bin\Debug\_build.dll')
$older = $binDll.LastWriteTimeUtc.AddSeconds(-5)
foreach ($name in @('_build.dll', '_build.pdb')) {
  $obj = Get-Item (Join-Path $root ("obj\Debug\" + $name))
  $obj.LastWriteTimeUtc = $older
  Write-Output ("OBJ " + $name + " " + $obj.LastWriteTimeUtc.ToString('o'))
}
$log = 'F:\GitHub\McpServer\.agent-scratch\p2-build-normal.log'
if (Test-Path $log) { Remove-Item $log -Force }
& dotnet build (Join-Path $root '_build.csproj') --no-restore -v:n
Write-Output ("BUILD_EXIT=" + $LASTEXITCODE)
# capture is not file-logged; rely on console. Re-run is already printed.
