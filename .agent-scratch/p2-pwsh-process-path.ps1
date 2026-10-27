$ErrorActionPreference = 'Continue'
$shim = 'C:\Users\kingd\.dotnet\tools\pwsh.exe'
$store = 'C:\Users\kingd\AppData\Local\Microsoft\WindowsApps\pwsh.exe'
function Show-HostPath([string]$Exe) {
    Write-Output ('--- ' + $Exe + ' ---')
    $item = Get-Item -LiteralPath $Exe
    Write-Output ('LENGTH=' + $item.Length)
    Write-Output ('LASTWRITE=' + $item.LastWriteTimeUtc.ToString('o'))
    $out = & $Exe -NoProfile -NonInteractive -Command '(Get-Process -Id $PID).Path'
    Write-Output ('PROCESS_PATH=' + ($out | Out-String).Trim())
    Write-Output ('EXIT=' + $LASTEXITCODE)
}
Show-HostPath $shim
Show-HostPath $store
