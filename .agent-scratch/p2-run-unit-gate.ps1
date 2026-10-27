$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$required = '987cd8f39610c9fa301862e3b1d526d23e049469'
Set-Location -LiteralPath $wt
$head = (git rev-parse HEAD).Trim()
if ($head -ne $required) {
    throw "HEAD $head is not required tip $required"
}

# The unit gate prepends %USERPROFILE%\.dotnet\tools, where pwsh.exe is a
# dotnet apphost (process image is dotnet.exe). build.ps1 then re-executes
# that image with -NoLogo and fails as dotnet--NoLogo. Move that shim aside
# for this run so pwsh resolves to the Windows PowerShell 7 install, then restore it.
$shim = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe'
$shimAside = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe.p2gate-aside'
$shimMoved = $false
if (Test-Path -LiteralPath $shim) {
    if (Test-Path -LiteralPath $shimAside) {
        Remove-Item -LiteralPath $shimAside -Force
    }
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
    "SHIM_MOVED=$shimMoved"
    "SHIM_ASIDE=$shimAside"
) -join [Environment]::NewLine
Set-Content -LiteralPath $log -Value $header -Encoding utf8
$started = [DateTime]::UtcNow
$code = 1
try {
    & pwsh.exe -NoProfile -NonInteractive -File $gate -RunId $runId *>> $log
    $code = $LASTEXITCODE
    if ($null -eq $code) { $code = 0 }
} finally {
    if ($shimMoved -and (Test-Path -LiteralPath $shimAside)) {
        if (Test-Path -LiteralPath $shim) {
            Remove-Item -LiteralPath $shim -Force -ErrorAction SilentlyContinue
        }
        Move-Item -LiteralPath $shimAside -Destination $shim
    }
}
$finished = [DateTime]::UtcNow
Add-Content -LiteralPath $log -Value ("GATE_EXIT=" + $code)
Add-Content -LiteralPath $log -Value ("FINISHED_UTC=" + $finished.ToString('yyyyMMddTHHmmssZ'))
$doc = [ordered]@{
    runId = $runId
    head = $head
    startedUtc = $started.ToString('o')
    finishedUtc = $finished.ToString('o')
    gateExit = [int]$code
    logPath = $log
}
$doc | ConvertTo-Json | Set-Content -LiteralPath $meta -Encoding utf8
Write-Output ('RUN_ID=' + $runId)
Write-Output ('GATE_EXIT=' + $code)
Write-Output ('LOG=' + $log)
exit $code
