$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location $wt
Write-Output 'UTC'
Write-Output ([DateTime]::UtcNow.ToString('o'))
Write-Output 'HEAD'
git rev-parse HEAD
Write-Output 'LOG'
git log -5 --oneline
Write-Output 'STATUS_SHORT'
git status --short --branch
$md = Join-Path $wt 'docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T234447Z.md'
$js = Join-Path $wt 'docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T234447Z.json'
Write-Output ('MD=' + (Test-Path $md))
Write-Output ('JSON=' + (Test-Path $js))
if (Test-Path $md) {
    Get-Content -LiteralPath $md -TotalCount 60
}
Write-Output 'PR'
gh pr view 72 --repo sharpninja/McpServer --json headRefOid,isDraft,mergedAt,state,title,url,baseRefName
$shim = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe'
$aside = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe.p2gate-aside'
Write-Output ('SHIM=' + (Test-Path $shim))
Write-Output ('ASIDE=' + (Test-Path $aside))
$targets = Join-Path $wt 'build\Directory.Build.targets'
$rsp = Join-Path $wt 'build\Directory.Build.rsp'
Write-Output ('TARGETS=' + (Test-Path $targets))
Write-Output ('RSP=' + (Test-Path $rsp))
