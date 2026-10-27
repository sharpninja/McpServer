$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '==== not_found / retryable in pester and src (names) ===='
$files = Get-ChildItem -Path (Join-Path $wt 'plugins\core'), (Join-Path $wt 'tests'), (Join-Path $wt 'src') -Recurse -Include *.ps1,*.cs -File -ErrorAction SilentlyContinue
$hits = foreach ($f in $files) {
  $i = 0
  foreach ($line in [System.IO.File]::ReadLines($f.FullName)) {
    $i++
    if ($line -match 'not_found|retryable') {
      [pscustomobject]@{ File = $f.FullName.Substring($wt.Length); Line = $i; Text = $line.Trim() }
    }
  }
}
$hits | Select-Object -First 80 | ForEach-Object { Write-Output ("{0}:{1}: {2}" -f $_.File, $_.Line, $_.Text) }
Write-Output ("HIT_COUNT=" + @($hits).Count)
