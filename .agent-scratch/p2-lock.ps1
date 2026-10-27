$ErrorActionPreference = 'Stop'
$p = Get-Process -Id 47004 -ErrorAction SilentlyContinue
if (-not $p) { Write-Output 'PID 47004 gone'; exit 0 }
Write-Output ("Name=" + $p.ProcessName)
Write-Output ("Start=" + $p.StartTime.ToUniversalTime().ToString('o'))
Write-Output ("Path=" + $p.Path)
$cim = Get-CimInstance Win32_Process -Filter "ProcessId=47004"
Write-Output ("Parent=" + $cim.ParentProcessId)
Write-Output ("Cmd=" + $cim.CommandLine)
$parent = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $cim.ParentProcessId) -ErrorAction SilentlyContinue
if ($parent) { Write-Output ("ParentCmd=" + $parent.CommandLine) }
