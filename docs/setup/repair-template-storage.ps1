<# .SYNOPSIS Seeds shipped prompt templates into the dedicated service data directory and checks startup. #>
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$true
if ((& /usr/bin/id -u) -ne '0') { throw 'Administrator required.' }
$source='/opt/mcpserver/app/templates/prompt-templates.yaml'
$target='/var/lib/mcpserver/templates/prompt-templates.yaml'
if (-not (Test-Path $source)) { throw 'Shipped templates absent.' }
if (Test-Path $target) { throw 'Data templates already exist; inspect instead of overwriting.' }
& /usr/bin/systemctl stop mcpserver.service
& /usr/bin/install -d -m 0750 -o mcpserver -g mcpserver /var/lib/mcpserver/templates
& /usr/bin/install -m 0640 -o mcpserver -g mcpserver $source $target
if ((Get-FileHash $source).Hash -ne (Get-FileHash $target).Hash) { throw 'Template copy mismatch.' }
& /usr/bin/runuser -u mcpserver -- /usr/bin/test -r $target
& /usr/bin/systemctl reset-failed mcpserver.service
& /usr/bin/systemctl start mcpserver.service
Start-Sleep -Seconds 15
& /usr/bin/systemctl show mcpserver.service -p ActiveState -p SubState -p MainPID -p NRestarts -p Result
& /usr/bin/ss -ltnp '( sport = :7147 )'
$entries=& /usr/bin/journalctl -u mcpserver.service --since '-20 seconds' --no-pager -o cat
$entries | Where-Object { $_ -match 'Unhandled exception|Now listening|Application started|using file:|CRITICAL:|denied|backfill failed' } | Select-Object -Last 20
[pscustomobject]@{MarkerExists=(Test-Path '/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml');TemplatesSha256=(Get-FileHash $target).Hash} | ConvertTo-Json -Compress
