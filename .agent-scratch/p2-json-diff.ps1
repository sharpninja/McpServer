$ErrorActionPreference = 'Stop'
$orig = [System.IO.File]::ReadAllText('F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json')
$out = [System.IO.File]::ReadAllText('F:\GitHub\McpServer\.agent-scratch\manifest-roundtrip.json')
$a = $orig -replace "`r`n", "`n"
$b = $out -replace "`r`n", "`n"
if (-not $b.EndsWith("`n")) { $b += "`n" }
if (-not $a.EndsWith("`n")) { $a += "`n" }
Write-Output ("NORM_EQUAL=" + ($a -eq $b))
Write-Output ("A_END=" + [int][char]$a[$a.Length-1] + " B_END=" + [int][char]$b[$b.Length-1])
# find first difference
$n = [Math]::Min($a.Length, $b.Length)
$idx = -1
for ($i = 0; $i -lt $n; $i++) {
  if ($a[$i] -ne $b[$i]) { $idx = $i; break }
}
Write-Output ("FIRST_DIFF=$idx LEN_A=$($a.Length) LEN_B=$($b.Length)")
if ($idx -ge 0) {
  $s = [Math]::Max(0, $idx - 80)
  Write-Output '--- A ---'
  Write-Output $a.Substring($s, [Math]::Min(200, $a.Length - $s))
  Write-Output '--- B ---'
  Write-Output $b.Substring($s, [Math]::Min(200, $b.Length - $s))
}
