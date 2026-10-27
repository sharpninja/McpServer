$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$bytes = [System.IO.File]::ReadAllBytes($p)
$crlf = 0; $lf = 0
for ($i = 0; $i -lt [Math]::Min($bytes.Length, 5000); $i++) {
  if ($bytes[$i] -eq 10) { $lf++ ; if ($i -gt 0 -and $bytes[$i-1] -eq 13) { $crlf++ } }
}
Write-Output ("LF=$lf CRLF=$crlf BOM=$($bytes[0])")
$text = [System.IO.File]::ReadAllText($p)
$node = [System.Text.Json.Nodes.JsonNode]::Parse($text)
$opt = New-Object System.Text.Json.JsonSerializerOptions
$opt.WriteIndented = $true
$out = $node.ToJsonString($opt) + "`n"
$tmp = 'F:\GitHub\McpServer\.agent-scratch\manifest-roundtrip.json'
[System.IO.File]::WriteAllText($tmp, $out)
Write-Output ("ORIG=$($text.Length) OUT=$($out.Length)")
# compare semantic equality
$a = [System.Text.Json.Nodes.JsonNode]::Parse($text).ToJsonString()
$b = [System.Text.Json.Nodes.JsonNode]::Parse($out).ToJsonString()
Write-Output ("SEMANTIC_EQUAL=" + ($a -eq $b))
