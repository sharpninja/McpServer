$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build'
$bin = Join-Path $root 'bin\Debug\_build.dll'
$obj = Join-Path $root 'obj\Debug\_build.dll'
foreach ($path in @($bin, $obj)) {
  $item = Get-Item -LiteralPath $path
  Write-Output ("{0} {1} {2}" -f $item.LastWriteTimeUtc.ToString('o'), $item.Length, $path)
}
try {
  $fs = [System.IO.File]::Open($bin, 'Open', 'ReadWrite', 'None')
  $fs.Dispose()
  Write-Output 'BIN_WRITABLE=True'
} catch {
  Write-Output ('BIN_WRITABLE=False ' + $_.Exception.Message)
}
$csproj = Get-Item (Join-Path $root '_build.csproj')
Write-Output ("CSPROJ " + $csproj.LastWriteTimeUtc.ToString('o'))
