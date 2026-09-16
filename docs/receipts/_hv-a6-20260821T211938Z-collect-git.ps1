$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-a6-20260821T211938Z'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location 'F:\GitHub\McpServer'
git status --short | Set-Content (Join-Path $out 'git-status.txt') -Encoding utf8
git diff --stat | Set-Content (Join-Path $out 'git-diff-stat.txt') -Encoding utf8
git log -8 --oneline | Set-Content (Join-Path $out 'git-log.txt') -Encoding utf8

$repl = 'plugins\core\lib-ps\repl-invoke.ps1'
Select-String -Path $repl -Pattern 'return 2' -Context 5,5 | ForEach-Object { $_.ToString() } | Set-Content (Join-Path $out 'repl-return-2.txt') -Encoding utf8
Select-String -Path $repl -Pattern 'REPL_FAILSAFE_DRAIN_TIMEOUT|ReplFailsafeDraining|Get-ReplMethodTimeoutSeconds' | ForEach-Object { $_.ToString() } | Set-Content (Join-Path $out 'repl-drain-symbols.txt') -Encoding utf8

Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -match 'HostileReview|WorkspaceValidation|WikiDump|Hygiene' -and
        $_.FullName -notmatch '\\docs\\receipts\\' -and
        $_.FullName -notmatch '\\docs\\plans\\' -and
        $_.FullName -notmatch '\\.git\\'
    } |
    Select-Object -ExpandProperty FullName |
    Set-Content (Join-Path $out 'product-name-hits.txt') -Encoding utf8

Write-Output 'git collect done'
