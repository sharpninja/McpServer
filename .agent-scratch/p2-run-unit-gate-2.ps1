$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$targets = Join-Path $wt 'build\Directory.Build.targets'
$targetsOwned = $false
# The MCP host locks build\bin\Debug\_build.dll. A response file is only read
# for the entry project, so tests/Build.Tests still copies the referenced host.
# Targets next to _build.csproj skip that copy and the incremental clean that
# deletes obj\Debug\ref\_build.dll. The file stays for the whole gate so the
# source manifest stays stable, then this script deletes it.
if (-not (Test-Path -LiteralPath $targets)) {
    $targetsText = @'
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.targets', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <CopyBuildOutputToOutputDirectory>false</CopyBuildOutputToOutputDirectory>
  </PropertyGroup>
  <Target Name="IncrementalClean" />
</Project>
'@
    [System.IO.File]::WriteAllText($targets, $targetsText.Replace("`n", "`r`n"))
    $targetsOwned = $true
}
$impl = Join-Path $wt 'build\obj\Debug\_build.dll'
$ref = Join-Path $wt 'build\obj\Debug\ref\_build.dll'
if (-not (Test-Path -LiteralPath $ref)) {
    if (-not (Test-Path -LiteralPath $impl)) {
        throw 'Locked build host has no obj\_build.dll to seed the missing reference assembly.'
    }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ref) | Out-Null
    Copy-Item -LiteralPath $impl -Destination $ref -Force
}
$pwsh = 'C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.6.0_x64__8wekyb3d8bbwe\pwsh.exe'
Set-Location -LiteralPath $wt
$head = (git rev-parse HEAD).Trim()
$shim = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe'
$shimAside = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe.p2gate-aside'
$shimMoved = $false
if (Test-Path -LiteralPath $shim) {
    if (Test-Path -LiteralPath $shimAside) { Remove-Item -LiteralPath $shimAside -Force }
    Move-Item -LiteralPath $shim -Destination $shimAside
    $shimMoved = $true
}
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$runId = "p2-unit-legion-$utc"
$logDir = Join-Path $wt 'docs\receipts\hv'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$log = Join-Path $logDir "$runId-gate.log"
$meta = Join-Path $env:USERPROFILE '.grok\p2-unit-gate-meta.json'
$gate = Join-Path $wt 'tools\validation\Invoke-SessionLifeUnitGate.ps1'
$header = @(
    "RUN_ID=$runId"
    "HEAD=$head"
    "STARTED_UTC=$utc"
    "GATE_SCRIPT=$gate"
    "PWSH=$pwsh"
    "SHIM_MOVED=$shimMoved"
) -join [Environment]::NewLine
Set-Content -LiteralPath $log -Value $header -Encoding utf8
$started = [DateTime]::UtcNow
$code = 1
try {
    & $pwsh -NoProfile -NonInteractive -File $gate -RunId $runId *>> $log
    $code = $LASTEXITCODE
    if ($null -eq $code) { $code = 0 }
} finally {
    if ($targetsOwned -and (Test-Path -LiteralPath $targets)) {
        Remove-Item -LiteralPath $targets -Force -ErrorAction SilentlyContinue
    }
    if ($shimMoved -and (Test-Path -LiteralPath $shimAside)) {
        if (Test-Path -LiteralPath $shim) { Remove-Item -LiteralPath $shim -Force -ErrorAction SilentlyContinue }
        Move-Item -LiteralPath $shimAside -Destination $shim
    }
    $shimBack = Test-Path -LiteralPath $shim
    $asideLeft = Test-Path -LiteralPath $shimAside
    Add-Content -LiteralPath $log -Value ("SHIM_RESTORED=" + $shimBack)
    Add-Content -LiteralPath $log -Value ("ASIDE_LEFT=" + $asideLeft)
}
$finished = [DateTime]::UtcNow
Add-Content -LiteralPath $log -Value ("GATE_EXIT=" + $code)
Add-Content -LiteralPath $log -Value ("FINISHED_UTC=" + $finished.ToString('yyyyMMddTHHmmssZ'))
$hash = (Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash
$doc = [ordered]@{
    runId = $runId
    head = $head
    startedUtc = $started.ToString('o')
    finishedUtc = $finished.ToString('o')
    gateExit = [int]$code
    logPath = $log
    logSha256 = $hash
    shimRestored = (Test-Path -LiteralPath $shim)
}
$doc | ConvertTo-Json | Set-Content -LiteralPath $meta -Encoding utf8
Write-Output ('RUN_ID=' + $runId)
Write-Output ('HEAD=' + $head)
Write-Output ('GATE_EXIT=' + $code)
Write-Output ('LOG=' + $log)
Write-Output ('LOG_SHA256=' + $hash)
Write-Output ('SHIM_RESTORED=' + (Test-Path -LiteralPath $shim))
exit $code
