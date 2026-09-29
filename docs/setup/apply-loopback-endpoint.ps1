<# .SYNOPSIS Applies explicit loopback listener configuration and verifies the service lifecycle. #>
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$true
$target='/opt/mcpserver/app/appsettings.yaml'
$backup=$target+'.before-loopback-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Copy-Item $target $backup
& /usr/bin/systemctl stop mcpserver.service
$oldMarkerPresent=Test-Path '/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml'
& /usr/bin/install -m 0644 -o root -g root /tmp/mcpserver-setup/appsettings.yaml $target
& /usr/bin/systemctl start mcpserver.service
Start-Sleep -Seconds 12
& /usr/bin/systemctl show mcpserver.service -p ActiveState -p SubState -p MainPID -p User -p NRestarts -p Result
& /usr/bin/systemctl is-enabled mcpserver.service
$servicePid=(& /usr/bin/systemctl show mcpserver.service -p MainPID --value).Trim()
& /usr/bin/readlink "/proc/$servicePid/exe"
& /usr/bin/ss -ltnp '( sport = :7147 )'
& /usr/bin/runuser -u sharpninja -- /usr/bin/test -r /home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml
[pscustomobject]@{MarkerRemovedOnStop=(-not $oldMarkerPresent);MarkerReadableByOperator=$true;ConfigSha256=(Get-FileHash $target).Hash;MainPID=$servicePid} | ConvertTo-Json -Compress
