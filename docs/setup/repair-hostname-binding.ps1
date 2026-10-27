<#
.SYNOPSIS
Makes the local MCP service use hostnames instead of IP literals, in one elevated run.
.DESCRIPTION
Operator directives (2026-10-09): "mcpserver must use hostname", "bind mcpserver to 0.0.0.0:7147",
and "you write and run script".
1. Database connection: Server=PAYTON-OMARCHY,1433 (PAYTON-OMARCHY is 127.0.0.1 in /etc/hosts; the
   sql-server-developer container publishes 0.0.0.0:1433 so PAYTON-LEGION2 can also connect).
2. HTTP: one Kestrel endpoint, http://0.0.0.0:7147, replacing localhost:7147, 127.0.0.2:7147 and the
   IP-bound Ethernet endpoint (10.42.0.214:7147), which crash-looped the service when Ethernet had no lease.
3. federation.env: IP literals replaced by their /etc/hosts names.
Backs up every touched file, never prints credentials, writes a redacted receipt, and verifies the
service, the 0.0.0.0:7147 listener, the regenerated marker, and the /health nonce.
Run: pkexec /usr/bin/pwsh -NoProfile -NonInteractive -File <this file>
#>
[CmdletBinding()]
param(
    [string]$ReceiptDir = '/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/ops',
    [string]$YamlHelper = '/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/plugins/core/lib-ps/yaml-object-mutation.ps1'
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
if ((& /usr/bin/id -u) -ne '0') { throw 'Administrator required; run with pkexec.' }
if ((Get-Content /etc/hostname -Raw).Trim() -ne 'LAB-OMARCHY') { throw 'Unexpected host; refusing.' }

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$envFile = '/etc/mcpserver/mcpserver.env'
$fedFile = '/etc/mcpserver/federation.env'
$dropDir = '/etc/systemd/system/mcpserver.service.d'
$ethernetConf = "$dropDir/ethernet.conf"
$bindingConf = "$dropDir/binding.conf"
$appSettings = '/opt/mcpserver/app/appsettings.yaml'
$marker = '/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml'
$backupDir = "/etc/mcpserver/backup-$stamp"
$secret = $null
$receipt = [ordered]@{ timestampUtc = $stamp; host = 'LAB-OMARCHY'; backupDir = $backupDir; steps = [ordered]@{} }

try {
    # 0. Backups (root only).
    & /usr/bin/install -d -m 0700 $backupDir
    foreach ($f in @($envFile, $fedFile, $ethernetConf, "$dropDir/federation.conf", $appSettings)) {
        if (Test-Path -LiteralPath $f) { Copy-Item -LiteralPath $f -Destination (Join-Path $backupDir ([IO.Path]::GetFileName($f))) }
    }
    & /usr/bin/chmod -R go-rwx $backupDir
    $receipt.steps.backup = @(Get-ChildItem $backupDir | ForEach-Object { [ordered]@{ file = $_.Name; sha256 = (Get-FileHash $_.FullName).Hash } })

    # 1. Database connection host -> PAYTON-OMARCHY,1433 (password untouched, never printed).
    $prefix = 'Mcp__Database__SqlServer__ConnectionString='
    $lines = [System.Collections.Generic.List[string]]::new([string[]](Get-Content -LiteralPath $envFile))
    $index = -1
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].StartsWith($prefix)) { $index = $i } }
    if ($index -lt 0) { throw 'Connection string key not found.' }
    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $builder.set_ConnectionString($lines[$index].Substring($prefix.Length))
    $secret = [string]$builder['Password']
    $serverBefore = [string]$builder['Server']
    $builder['Server'] = 'PAYTON-OMARCHY,1433'
    $lines[$index] = $prefix + $builder.ConnectionString
    [IO.File]::WriteAllLines($envFile, $lines)
    & /usr/bin/chown root:root $envFile
    & /usr/bin/chmod 0600 $envFile
    $builder.Clear()
    $receipt.steps.database = [ordered]@{ serverBefore = $serverBefore; serverAfter = 'PAYTON-OMARCHY,1433'; mode = '0600 root:root' }

    # 2. federation.env IP literals -> hostnames from /etc/hosts.
    $map = [ordered]@{ '10.42.0.214' = 'LAB-OMARCHY'; '10.42.0.143' = 'PAYTON-LEGION2'; '192.168.1.119' = 'LAB-LEGION2'; '127.0.0.1' = 'PAYTON-OMARCHY' }
    $fedChanges = @(); $fedUnmapped = @()
    if (Test-Path -LiteralPath $fedFile) {
        $fed = [string[]](Get-Content -LiteralPath $fedFile)
        for ($i = 0; $i -lt $fed.Count; $i++) {
            $key = ($fed[$i] -split '=', 2)[0]
            foreach ($ip in $map.Keys) {
                $rx = '(?<![\d.])' + [regex]::Escape($ip) + '(?![\d.])'
                $count = [regex]::Matches($fed[$i], $rx).Count
                if ($count -gt 0) { $fed[$i] = [regex]::Replace($fed[$i], $rx, $map[$ip]); $fedChanges += [ordered]@{ key = $key; from = $ip; to = $map[$ip]; count = $count } }
            }
            if ($fed[$i] -match '(?<![\d.])\d{1,3}(\.\d{1,3}){3}(?![\d.])') { $fedUnmapped += $key }
        }
        [IO.File]::WriteAllLines($fedFile, $fed)
        & /usr/bin/chmod 0600 $fedFile
    }
    $receipt.steps.federation = [ordered]@{ changes = $fedChanges; keysWithUnmappedIpLiterals = $fedUnmapped; keys = @(if (Test-Path $fedFile) { Get-Content $fedFile | ForEach-Object { ($_ -split '=', 2)[0] } }) }

    # 3. systemd: one URL on all interfaces; remove the IP-bound Ethernet drop-in.
    $bindingText = @(
        '[Service]'
        'ExecStart='
        'ExecStart=/opt/mcpserver/app/McpServer.Support.Mcp --urls http://0.0.0.0:7147'
    ) -join "`n"
    Set-Content -LiteralPath $bindingConf -Value $bindingText
    & /usr/bin/chmod 0644 $bindingConf
    if (Test-Path -LiteralPath $ethernetConf) { Remove-Item -LiteralPath $ethernetConf }
    $receipt.steps.systemd = [ordered]@{ added = $bindingConf; removed = $ethernetConf; execStart = '--urls http://0.0.0.0:7147' }

    # 4. appsettings.yaml Kestrel endpoints -> single all-interfaces endpoint (YAML object mutation).
    . $YamlHelper
    Import-McpYamlSerializer
    $endpointsBefore = @((Read-McpYamlObject -Path $appSettings)['Kestrel']['Endpoints'].Keys)
    Set-McpYamlObjectValue -Path $appSettings -KeyPath @('Kestrel', 'Endpoints') -Value ([ordered]@{ AllInterfaces = [ordered]@{ Url = 'http://0.0.0.0:7147' } }) | Out-Null
    $endpointsAfter = (Read-McpYamlObject -Path $appSettings)['Kestrel']['Endpoints']
    & /usr/bin/chown root:root $appSettings
    & /usr/bin/chmod 0644 $appSettings
    $receipt.steps.appsettings = [ordered]@{ endpointsBefore = $endpointsBefore; endpointsAfter = @($endpointsAfter.Keys | ForEach-Object { "$_=" + $endpointsAfter[$_]['Url'] }) }

    # 5. Restart and verify.
    & /usr/bin/systemctl daemon-reload
    & /usr/bin/systemd-analyze verify /etc/systemd/system/mcpserver.service
    $startUtc = [DateTime]::UtcNow
    & /usr/bin/systemctl reset-failed mcpserver.service
    & /usr/bin/systemctl restart mcpserver.service
    $deadline = (Get-Date).AddSeconds(90)
    do {
        Start-Sleep -Seconds 3
        $state = (& /usr/bin/systemctl show mcpserver.service -p ActiveState --value)
        $markerFresh = (Test-Path $marker) -and ((Get-Item $marker).LastWriteTimeUtc -ge $startUtc)
    } until (($state -eq 'active' -and $markerFresh) -or (Get-Date) -gt $deadline)
    $show = & /usr/bin/systemctl show mcpserver.service -p ActiveState -p SubState -p MainPID -p NRestarts -p Result -p ExecMainStartTimestamp
    $listen = & /usr/bin/ss -ltnH '( sport = :7147 )'
    $health = $null
    try {
        $nonce = 'nonce-' + [guid]::NewGuid().ToString('N')
        $h = Invoke-RestMethod -Uri "http://PAYTON-OMARCHY:7147/health?nonce=$nonce" -TimeoutSec 10
        $health = [ordered]@{ status = $h.status; storage = $h.storage; nonceMatch = ($h.nonce -eq $nonce); version = $h.version }
    } catch { $health = [ordered]@{ error = $_.Exception.Message } }
    $journal = @(& /usr/bin/journalctl -u mcpserver.service --since ('@' + [DateTimeOffset]::new($startUtc).ToUnixTimeSeconds()) --no-pager -o cat |
        Where-Object { $_ -match 'Now listening|Application started|exception|fail|error|marker|bind|address' } | Select-Object -Last 25 |
        ForEach-Object { if ($secret) { $_.Replace($secret, '[REDACTED]') } else { $_ } })
    $receipt.verification = [ordered]@{ systemctl = $show; listeners = $listen; markerExists = (Test-Path $marker); markerFresh = $markerFresh; markerWriteUtc = $(if (Test-Path $marker) { (Get-Item $marker).LastWriteTimeUtc.ToString('o') } else { $null }); health = $health; journal = $journal }
}
finally {
    $secret = $null
    & /usr/bin/install -d -m 0755 $ReceiptDir
    $path = Join-Path $ReceiptDir "hostname-binding-$stamp.json"
    $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path
    & /usr/bin/chown sharpninja:sharpninja $path
    & /usr/bin/chmod 0644 $path
    Write-Output "Receipt: $path"
    $receipt | ConvertTo-Json -Depth 8
}
