$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$pwsh = Join-Path $env:ProgramFiles 'WindowsApps\Microsoft.PowerShell_7.6.6.0_x64__8wekyb3d8bbwe\pwsh.exe'
$shim = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe'
$shimAside = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe.p2gate-aside'
$log = Join-Path $env:USERPROFILE '.grok\p2-focused-gaps.log'
$shimMoved = $false
if (Test-Path -LiteralPath $shim) {
    if (Test-Path -LiteralPath $shimAside) { Remove-Item -LiteralPath $shimAside -Force }
    Move-Item -LiteralPath $shim -Destination $shimAside
    $shimMoved = $true
}
$code = 1
try {
    Set-Location -LiteralPath $wt
    Set-Content -LiteralPath $log -Value ("PWSH_RESOLVED=" + (Get-Command pwsh -ErrorAction Stop).Source) -Encoding utf8
    Add-Content -LiteralPath $log -Value ("SHIM_MOVED=" + $shimMoved)
    & $pwsh -NoProfile -NonInteractive -Command {
        $ErrorActionPreference = 'Stop'
        Import-Module Pester -ErrorAction Stop
        Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
        $config = New-PesterConfiguration
        $config.Run.Path = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\test-fixtures\pester\SessionLogP2Contracts.Tests.ps1'
        $config.Run.PassThru = $true
        $config.Filter.FullName = @(
            '*missing-turn dialog recovery*'
        )
        $config.Output.Verbosity = 'Detailed'
        $result = Invoke-Pester -Configuration $config
        "PESTER_PASSED=$($result.PassedCount)"
        "PESTER_FAILED=$($result.FailedCount)"
        if ($result.FailedCount -gt 0) { exit 1 }
        exit 0
    } *>> $log
    $pester = $LASTEXITCODE
    if ($null -eq $pester) { $pester = 0 }
    Add-Content -LiteralPath $log -Value ("PESTER_EXIT=" + $pester)
    $code = $pester
} finally {
    if ($shimMoved -and (Test-Path -LiteralPath $shimAside)) {
        if (Test-Path -LiteralPath $shim) { Remove-Item -LiteralPath $shim -Force -ErrorAction SilentlyContinue }
        Move-Item -LiteralPath $shimAside -Destination $shim
    }
    $shimBack = Test-Path -LiteralPath $shim
    $asideLeft = Test-Path -LiteralPath $shimAside
    Add-Content -LiteralPath $log -Value ("SHIM_RESTORED=" + $shimBack)
    Add-Content -LiteralPath $log -Value ("ASIDE_LEFT=" + $asideLeft)
    Add-Content -LiteralPath $log -Value ("FOCUSED_EXIT=" + $code)
}
