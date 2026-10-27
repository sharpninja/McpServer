$ErrorActionPreference = 'Continue'
$xmlPath = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T193045Z\pester\results.xml'
[xml]$xml = Get-Content -LiteralPath $xmlPath
$failed = $xml.SelectNodes('//test-case[@result="Failure"]')
Write-Output ('FAILED_CASES=' + $failed.Count)
foreach ($case in $failed) {
    Write-Output ('NAME=' + $case.name)
    $msg = $case.failure.message
    if (-not $msg) { $msg = $case.SelectSingleNode('failure/message').InnerText }
    if ($msg) {
        $text = [string]$msg
        if ($text.Length -gt 500) { $text = $text.Substring(0, 500) }
        Write-Output $text
    }
    Write-Output '---'
}
