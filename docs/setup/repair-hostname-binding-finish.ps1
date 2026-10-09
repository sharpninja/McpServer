<#
.SYNOPSIS
Finishes repair-hostname-binding.ps1 after its 2026-10-09T13:49:07Z run stopped at step 4.
.DESCRIPTION
The first run completed backups, the database host change (PAYTON-OMARCHY,1433), the federation.env
scan (no IP literals) and the systemd drop-in swap (binding.conf added, ethernet.conf removed), then
failed because root could not load the user-scoped powershell-yaml module. This run adds that module
path for the elevated process, replaces Kestrel:Endpoints in appsettings.yaml with a single
http://0.0.0.0:7147 endpoint (YAML object mutation, backup first), reloads systemd, restarts the
service, and verifies the listener, the regenerated marker and the /health nonce. Never prints credentials.
Run: pkexec /usr/bin/pwsh -NoProfile -NonInteractive -File <this file>
#>
[CmdletBinding()]
param(
    [string]$ReceiptDir = '/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/ops',
    [string]$YamlHelper = '/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/plugins/core/lib-ps/yaml-object-mutation.ps1',
    [string]$UserModules = '/home/sharpninja/.local/share/powershell/Modules'
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
if ((& /usr/bin/id -u) -ne '0') { throw 'Administrator required; run with pkexec.' }
if ((Get-Content /etc/hostname -Raw).Trim() -ne 'LAB-OMARCHY') { throw 'Unexpected host; refusing.' }
if (-not (Test-Path '/etc/systemd/system/mcpserver.service.d/binding.conf')) { throw 'First run state missing (binding.conf); refusing.' }

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$appSettings = '/opt/mcpserver/app/appsettings.yaml'
$marker = '/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml'
$backupDir = "/etc/mcpserver/backup-$stamp-finish"
$receipt = [ordered]@{ timestampUtc = $stamp; host = 'LAB-OMARCHY'; continues = 'hostname-binding-20261009T134907Z'; backupDir = $backupDir; steps = [ordered]@{} }

try {
    & /usr/bin/install -d -m 0700 $backupDir
    Copy-Item -LiteralPath $appSettings -Destination (Join-Path $backupDir 'appsettings.yaml')
    $receipt.steps.backup = [ordered]@{ file = 'appsettings.yaml'; sha256 = (Get-FileHash (Join-Path $backupDir 'appsettings.yaml')).Hash }

    # 4. appsettings.yaml Kestrel endpoints -> single all-interfaces endpoint.
    $env:PSModulePath = $UserModules + [IO.Path]::PathSeparator + $env:PSModulePath
    . $YamlHelper
    Import-McpYamlSerializer
    # Mutate a staged copy, prove only Kestrel:Endpoints changed, then install it.
    $staged = Join-Path $backupDir 'appsettings.staged.yaml'
    Copy-Item -LiteralPath $appSettings -Destination $staged
    $original = Read-McpYamlObject -Path $appSettings
    $before = @($original['Kestrel']['Endpoints'].Keys | ForEach-Object { "$_=" + $original['Kestrel']['Endpoints'][$_]['Url'] })
    Set-McpYamlObjectValue -Path $staged -KeyPath @('Kestrel', 'Endpoints') -Value ([ordered]@{ AllInterfaces = [ordered]@{ Url = 'http://0.0.0.0:7147' } }) | Out-Null
    $stagedDoc = Read-McpYamlObject -Path $staged
    $endpointsAfter = $stagedDoc['Kestrel']['Endpoints']
    $after = @($endpointsAfter.Keys | ForEach-Object { "$_=" + $endpointsAfter[$_]['Url'] })
    if (($after -join ',') -ne 'AllInterfaces=http://0.0.0.0:7147') { throw "Staged endpoints unexpected: $($after -join ',')" }
    $original['Kestrel'].Remove('Endpoints'); $stagedDoc['Kestrel'].Remove('Endpoints')
    if ((ConvertTo-Json $original -Depth 50 -Compress) -ne (ConvertTo-Json $stagedDoc -Depth 50 -Compress)) { throw 'Staged appsettings differs outside Kestrel:Endpoints; refusing to install.' }
    Copy-Item -LiteralPath $staged -Destination $appSettings -Force
    & /usr/bin/chown root:root $appSettings
    & /usr/bin/chmod 0644 $appSettings
    $receipt.steps.appsettings = [ordered]@{ endpointsBefore = $before; endpointsAfter = $after; otherContentIdentical = $true; sha256After = (Get-FileHash $appSettings).Hash }

    # 5. Reload, restart, verify (bounded waits: 90 s for active+marker, then 5 s settle).
    & /usr/bin/systemctl daemon-reload
    $PSNativeCommandUseErrorActionPreference = $false
    $verifyOut = (& /usr/bin/systemd-analyze verify /etc/systemd/system/mcpserver.service 2>&1 | Out-String).Trim()
    $receipt.steps.systemdVerify = [ordered]@{ exitCode = $LASTEXITCODE; output = $verifyOut }
    $PSNativeCommandUseErrorActionPreference = $true
    $startUtc = [DateTime]::UtcNow
    & /usr/bin/systemctl reset-failed mcpserver.service
    & /usr/bin/systemctl restart mcpserver.service
    $deadline = (Get-Date).AddSeconds(90)
    do {
        Start-Sleep -Seconds 3
        $state = (& /usr/bin/systemctl show mcpserver.service -p ActiveState --value)
        $markerFresh = (Test-Path $marker) -and ((Get-Item $marker).LastWriteTimeUtc -ge $startUtc)
    } until (($state -eq 'active' -and $markerFresh) -or (Get-Date) -gt $deadline)
    Start-Sleep -Seconds 5
    $show = & /usr/bin/systemctl show mcpserver.service -p ActiveState -p SubState -p MainPID -p NRestarts -p Result -p ExecMainStartTimestamp -p NeedDaemonReload
    $listen = & /usr/bin/ss -ltnH '( sport = :7147 )'
    $health = $null
    try {
        $nonce = 'nonce-' + [guid]::NewGuid().ToString('N')
        $h = Invoke-RestMethod -Uri "http://PAYTON-OMARCHY:7147/health?nonce=$nonce" -TimeoutSec 10
        $health = [ordered]@{ status = $h.status; storage = $h.storage; nonceMatch = ($h.nonce -eq $nonce); version = $h.version }
    } catch { $health = [ordered]@{ error = $_.Exception.Message } }
    $journal = @(& /usr/bin/journalctl -u mcpserver.service --since ('@' + [DateTimeOffset]::new($startUtc).ToUnixTimeSeconds()) --no-pager -o cat |
        Where-Object { $_ -match 'Now listening|Application started|exception|fail|error|marker|bind|address|Overriding' } | Select-Object -Last 25 |
        ForEach-Object { $_ -replace '(?i)(password|pwd)=[^;"'' ]*', '$1=[REDACTED]' })
    $receipt.verification = [ordered]@{ systemctl = $show; listeners = $listen; markerExists = (Test-Path $marker); markerFresh = $markerFresh; markerWriteUtc = $(if (Test-Path $marker) { (Get-Item $marker).LastWriteTimeUtc.ToString('o') } else { $null }); health = $health; journal = $journal }
}
finally {
    & /usr/bin/install -d -m 0755 $ReceiptDir
    $path = Join-Path $ReceiptDir "hostname-binding-finish-$stamp.json"
    $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path
    & /usr/bin/chown sharpninja:sharpninja $path
    & /usr/bin/chmod 0644 $path
    Write-Output "Receipt: $path"
    $receipt | ConvertTo-Json -Depth 8
}
