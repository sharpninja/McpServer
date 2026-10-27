$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-list-astra-files.txt'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv'
$lines = [System.Collections.Generic.List[string]]::new()
Get-ChildItem -LiteralPath $root -Force | Where-Object { $_.Name -like '*20261003T011854Z*' -or $_.Name -like '*hostile-validator-sessionlife-p2-20261003T011854Z*' } | ForEach-Object {
    if ($_.PSIsContainer) {
        $count = @(Get-ChildItem -LiteralPath $_.FullName -Recurse -File).Count
        $lines.Add(('DIR ' + $_.Name + ' files=' + $count))
    } else {
        $lines.Add(('FILE ' + $_.Name + ' bytes=' + $_.Length))
    }
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
