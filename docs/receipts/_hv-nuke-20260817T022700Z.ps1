#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$log = 'F:\GitHub\McpServer\docs\receipts\_hv-nuke-20260817T022700Z.log'
if (Test-Path -LiteralPath $log) {
    Remove-Item -LiteralPath $log -Force
}

function Write-Log {
    param([string]$Message)
    $line = '[{0:yyyy-MM-ddTHH:mm:ss.fffZ}] {1}' -f (Get-Date).ToUniversalTime(), $Message
    Add-Content -LiteralPath $log -Value $line
    Write-Output $line
}

function Invoke-NukeTarget {
    param([string]$Name)
    Write-Log "BEGIN $Name"
    Write-Log ("./build.ps1 {0}" -f $Name)
    & pwsh.exe -NoProfile -NonInteractive -File '.\build.ps1' $Name 2>&1 | Tee-Object -FilePath $log -Append
    Write-Log ("END {0} EXIT={1}" -f $Name, $LASTEXITCODE)
}

Write-Log 'hostile nuke rerun start'
Invoke-NukeTarget -Name 'Compile'
Invoke-NukeTarget -Name 'Test'
Invoke-NukeTarget -Name 'ValidateTraceability'
Invoke-NukeTarget -Name 'SyncAgentPlugins'
Write-Log 'hostile nuke rerun complete'
exit 0
