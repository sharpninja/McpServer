$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build'
$bin = Join-Path $root 'bin\Debug\_build.dll'
$obj = Join-Path $root 'obj\Debug\_build.dll'
$binHash = (Get-FileHash -LiteralPath $bin -Algorithm SHA256).Hash
$objHash = (Get-FileHash -LiteralPath $obj -Algorithm SHA256).Hash
Write-Output ("BIN_HASH=" + $binHash)
Write-Output ("OBJ_HASH=" + $objHash)
Write-Output ("SAME=" + ($binHash -eq $objHash))
$binItem = Get-Item -LiteralPath $bin
$objItem = Get-Item -LiteralPath $obj
Write-Output ("BIN_TIME=" + $binItem.LastWriteTimeUtc.ToString('o') + " LEN=" + $binItem.Length)
Write-Output ("OBJ_TIME=" + $objItem.LastWriteTimeUtc.ToString('o') + " LEN=" + $objItem.Length)
$objItem.LastWriteTimeUtc = $binItem.LastWriteTimeUtc
$pdbObj = Get-Item (Join-Path $root 'obj\Debug\_build.pdb')
$pdbBin = Get-Item (Join-Path $root 'bin\Debug\_build.pdb')
$pdbObj.LastWriteTimeUtc = $pdbBin.LastWriteTimeUtc
Write-Output 'TIMESTAMPS_EQUALIZED'
