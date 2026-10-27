$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$delays = @(0, 4, 8, 16, 32)
$ok = $false
for ($i = 0; $i -lt $delays.Count; $i++) {
    if ($delays[$i] -gt 0) { Start-Sleep -Seconds $delays[$i] }
    git push -u origin cursor/sessionlife-p2-contracts-5cb2
    if ($LASTEXITCODE -eq 0) { $ok = $true; break }
    Write-Output ("PUSH_FAIL attempt=" + ($i + 1))
}
if (-not $ok) { throw 'push failed' }
Write-Output 'PUSH_OK'
Write-Output 'HEAD'
git rev-parse HEAD
gh pr view 72 --repo sharpninja/McpServer --json headRefOid,isDraft,mergedAt,state,title,url,baseRefName
$shim = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe'
$aside = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe.p2gate-aside'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output ('SHIM=' + (Test-Path $shim))
Write-Output ('ASIDE=' + (Test-Path $aside))
Write-Output ('TARGETS=' + (Test-Path (Join-Path $wt 'build\Directory.Build.targets')))
Write-Output ('UTC=' + [DateTime]::UtcNow.ToString('o'))
