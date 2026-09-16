#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$waitLog = Join-Path $out 'wait-tests.log'
function Write-Wait([string]$Message) {
    $line = ([DateTime]::UtcNow.ToString('o')) + ' ' + $Message
    Add-Content -LiteralPath $waitLog -Value $line -Encoding utf8
    Write-Output $line
}

Write-Wait 'WAIT_START'
$deadline = [DateTime]::UtcNow.AddMinutes(25)
do {
    $procs = @(Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and
        $_.CommandLine -match 'PluginIntegration' -and
        ($_.Name -match 'testhost|vstest' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'dotnet.exe\" test '))
    })
    $mine = @(Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and $_.CommandLine -match 'hv2-tests.ps1|hv2\\trx-p15|hv2/trx-p15'
    })
    $foreign = @($procs | Where-Object { $_.CommandLine -notmatch 'hv2' })
    Write-Wait ("FOREIGN_PLUGININT=" + $foreign.Count + " MINE=" + $mine.Count)
    if ($foreign.Count -eq 0) { break }
    Start-Sleep -Seconds 15
} while ([DateTime]::UtcNow -lt $deadline)

if (@(Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and
        $_.CommandLine -match 'PluginIntegration' -and
        ($_.Name -match 'testhost|vstest' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'dotnet.exe\" test ')) -and
        $_.CommandLine -notmatch 'hv2'
    }).Count -gt 0) {
    Write-Wait 'WAIT_TIMEOUT_FOREIGN_STILL_RUNNING'
} else {
    Write-Wait 'WAIT_CLEAR'
}

& pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2-tests.ps1'
Write-Wait ("TESTS_SCRIPT_EXIT=" + $LASTEXITCODE)
