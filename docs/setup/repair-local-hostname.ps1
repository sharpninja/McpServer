<# .SYNOPSIS Resolves this machine's generated MCP marker hostname to its loopback-only service. #>
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$true
if ((& /usr/bin/id -u) -ne '0') { throw 'Administrator required.' }
$machine=(& /usr/bin/hostname).Trim()
if ($machine -ne 'PAYTON-OMARCHY') { throw 'Unexpected host; refusing hosts-file change.' }
$path='/etc/hosts'
$lines=[IO.File]::ReadAllLines($path)
$mapping=@($lines | Where-Object { ($_ -split '#',2)[0] -match '(?i)(^|\s)PAYTON-OMARCHY(\s|$)' })
if ($mapping.Count -gt 0) { throw 'An explicit host mapping already exists; inspect it before changing.' }
$backup="$path.mcpserver-"+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Copy-Item $path $backup
[IO.File]::AppendAllText($path,"`n127.0.0.1 PAYTON-OMARCHY # local MCP service hostname`n")
& /usr/bin/resolvectl flush-caches
& /usr/bin/getent ahostsv4 PAYTON-OMARCHY
& /usr/bin/runuser -u sharpninja -- /usr/lib/powershell-7/pwsh -NoLogo -NoProfile -NonInteractive -File /tmp/mcpserver-setup/verify-plugin.ps1
Write-Output "Hosts backup: $backup"
