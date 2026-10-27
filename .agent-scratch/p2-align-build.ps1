$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build'
$names = @('_build.dll', '_build.pdb', '_build.deps.json')
foreach ($name in $names) {
  $bin = Join-Path $root ("bin\Debug\" + $name)
  $obj = Join-Path $root ("obj\Debug\" + $name)
  if (-not (Test-Path -LiteralPath $bin) -or -not (Test-Path -LiteralPath $obj)) {
    Write-Output ("SKIP " + $name)
    continue
  }
  $binItem = Get-Item -LiteralPath $bin
  $objItem = Get-Item -LiteralPath $obj
  $objItem.LastWriteTimeUtc = $binItem.LastWriteTimeUtc
  Write-Output ("ALIGNED " + $name + " to " + $binItem.LastWriteTimeUtc.ToString('o'))
}
