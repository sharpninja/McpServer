<# .SYNOPSIS Rotates the exposed workspace token by restarting and verifies the local service in one administrator run. #>
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$true
$marker='/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml'
$oldTokenLine=@(Get-Content $marker | Where-Object { $_ -match '^apiKey:' })[0]
if (-not $oldTokenLine) { throw 'Current marker token field absent.' }
& /usr/bin/systemctl restart mcpserver.service
Start-Sleep -Seconds 12
$newTokenLine=@(Get-Content $marker | Where-Object { $_ -match '^apiKey:' })[0]
if (-not $newTokenLine -or $newTokenLine -ceq $oldTokenLine) { throw 'Token rotation not verified.' }
$oldTokenLine=$null; $newTokenLine=$null
& /usr/bin/systemctl show mcpserver.service -p ActiveState -p SubState -p MainPID -p User -p Group -p NRestarts -p Result
& /usr/bin/systemctl is-enabled mcpserver.service
$servicePid=(& /usr/bin/systemctl show mcpserver.service -p MainPID --value).Trim()
$exe=(& /usr/bin/readlink "/proc/$servicePid/exe").Trim()
if ($exe -ne '/opt/mcpserver/app/QBrainAi.Support.Mcp') { throw 'Runtime executable mismatch.' }
& /usr/bin/ss -ltnp '( sport = :7147 )'
& /usr/bin/runuser -u sharpninja -- /usr/lib/powershell-7/pwsh -NoLogo -NoProfile -NonInteractive -File /tmp/mcpserver-setup/verify-plugin.ps1
$receipt=[ordered]@{timestamp=[DateTime]::UtcNow.ToString('o');tokenRotated=$true;mainPID=$servicePid;executable=$exe;configurationSha256=(Get-FileHash /opt/mcpserver/app/appsettings.yaml).Hash;binarySha256=(Get-FileHash $exe).Hash;productVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo('/opt/mcpserver/app/QBrainAi.Support.Mcp.dll').ProductVersion;codexTrustedBootstrap='passed';codexTodoQuery='result';codexMemoryList='result'}
$receipt | ConvertTo-Json -Depth 5 | Set-Content /tmp/mcpserver-setup/final-service-receipt.json
$receipt | ConvertTo-Json -Depth 5
