$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$xmlPath = Join-Path $wt 'TestResults\p2-unit-legion-20261002T202158Z\pester\results.xml'
Write-Output ('XML=' + (Test-Path -LiteralPath $xmlPath))
[xml]$xml = Get-Content -LiteralPath $xmlPath
$cases = $xml.SelectNodes('//test-case')
Write-Output ('CASES=' + $cases.Count)
$needles = @('AppendDialog','failsafe','Failsafe','not_found','None','degraded persistence','queued')
foreach ($c in $cases) {
    $name = [string]$c.name
    $hit = $false
    foreach ($n in $needles) { if ($name -like "*$n*") { $hit = $true; break } }
    if ($hit) { Write-Output ($c.result + ' ' + $name) }
}
