$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$run = 'p2-unit-legion-20261002T193045Z'
$nuke = Join-Path $wt "TestResults\$run\logs\nuke-test.log"
Write-Output '=== NUKE FAILED SUMMARIES ==='
Select-String -LiteralPath $nuke -Pattern 'Failed!|Passed!|error|Test Run Failed|PluginIntegration.Tests.dll' |
    ForEach-Object { $_.Line.Trim() } |
    Where-Object { $_ -match 'Failed!|Passed!|Test Run Failed|PluginIntegration.Tests.dll \(' }
Write-Output '=== SELECTED PROJECTS ==='
$sel = Join-Path $wt "TestResults\$run\selected-projects.json"
if (-not (Test-Path $sel)) {
    Get-ChildItem -LiteralPath (Join-Path $wt "TestResults\$run") -Recurse -Filter 'selected-projects.json' | ForEach-Object { $_.FullName }
} else {
    Get-Content -LiteralPath $sel -Raw
}
Write-Output '=== PESTER FAIL NAMES ==='
[xml]$xml = Get-Content -LiteralPath (Join-Path $wt "TestResults\$run\pester\results.xml")
foreach ($case in $xml.SelectNodes('//test-case[@result="Failure"]')) {
    $name = $case.GetAttribute('name')
    $msgNode = $case.SelectSingleNode('failure/message')
    $msg = if ($msgNode) { ($msgNode.InnerText -replace '\s+', ' ').Trim() } else { '' }
    if ($msg.Length -gt 280) { $msg = $msg.Substring(0, 280) }
    Write-Output ('FAIL=' + $name)
    Write-Output ('MSG=' + $msg)
}
Write-Output '=== SUPPORT TRX ==='
Get-ChildItem -LiteralPath (Join-Path $wt "TestResults\$run") -Recurse -Filter '*.trx' | ForEach-Object { $_.FullName + ' ' + $_.Length }
Write-Output '=== GIT STATUS SHORT ==='
Set-Location $wt
git status --short | Select-Object -First 40
Write-Output '=== PR ==='
gh pr view 72 --repo sharpninja/McpServer --json number,state,isDraft,mergedAt,headRefOid,headRefName,url
