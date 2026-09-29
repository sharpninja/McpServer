<#
.SYNOPSIS
Performs the approved first-time local MCP service installation in one elevated script.
.DESCRIPTION
Provisions the service account, scoped filesystem access, SQL database/login, protected
connection environment, staged configuration and systemd unit. Never prints credentials.
#>
[CmdletBinding()]
param(
    [string]$PublishPath = '/home/sharpninja/github/McpServer/artifacts/mcp-server',
    [string]$Workspace = '/home/sharpninja/github/McpServer',
    [switch]$ResumeAfterSqlBatchFailure
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
if ((& /usr/bin/id -u) -ne '0') { throw 'This script requires root via pkexec.' }
$app = '/opt/mcpserver/app'
$secretFile = '/etc/mcpserver/mcpserver.env'
$unit = '/etc/systemd/system/mcpserver.service'
$receiptPath = '/var/lib/mcpserver/setup-install-receipt.json'
$saPassword = $null
$loginPassword = $null

function Invoke-ContainerSql {
    <# .SYNOPSIS Runs SQL with password in process environment and query over stdin. #>
    param([string]$User, [string]$Password, [string]$Database, [string]$Query)
    $start = [Diagnostics.ProcessStartInfo]::new('/usr/bin/docker')
    $start.UseShellExecute = $false
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($arg in @('exec','-i','--env','SQLCMDPASSWORD','sql-server-developer','/opt/mssql-tools18/bin/sqlcmd','-S','localhost','-U',$User,'-d',$Database,'-C','-b','-h','-1','-W')) { $start.ArgumentList.Add($arg) }
    $start.Environment['SQLCMDPASSWORD'] = $Password
    $sql = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $sql.StandardOutput.ReadToEndAsync()
        $stderr = $sql.StandardError.ReadToEndAsync()
        $sql.StandardInput.WriteLine($Query)
        $sql.StandardInput.WriteLine('GO')
        $sql.StandardInput.Close()
        if (-not $sql.WaitForExit(60000)) { $sql.Kill(); throw 'SQL command timed out.' }
        $result = $stdout.Result + $stderr.Result
        foreach ($secret in @($Password,$saPassword,$loginPassword)) { if ($secret) { $result = $result.Replace($secret,'[REDACTED]') } }
        if ($sql.ExitCode -ne 0) { throw ('SQL command failed: ' + $result) }
        return $result.Trim()
    } finally { $sql.Dispose() }
}

try {
    if (-not (Test-Path "$PublishPath/McpServer.Support.Mcp")) { throw 'Verified publish apphost missing.' }
    if (-not (Test-Path '/tmp/mcpserver-setup/appsettings.yaml')) { throw 'Staged config missing.' }
    if (Test-Path $unit) { throw 'A systemd unit already exists; inspect before replacing it.' }
    $PSNativeCommandUseErrorActionPreference = $false
    $existingAccount = & /usr/bin/getent passwd mcpserver
    $PSNativeCommandUseErrorActionPreference = $true
    if ($ResumeAfterSqlBatchFailure) {
        if (-not $existingAccount -or $existingAccount -notmatch ':mcpserver:|^mcpserver:') { throw 'Expected service account is absent.' }
        $accountFields = $existingAccount.Split(':')
        if ($accountFields[5] -ne '/var/lib/mcpserver' -or $accountFields[6] -ne '/usr/bin/nologin') { throw 'Existing service account differs from installer state.' }
        foreach ($requiredPath in @($secretFile, '/etc/mcpserver/workspace-acl-before.txt', $app)) {
            if (-not (Test-Path $requiredPath)) { throw "Expected recovery artifact absent: $requiredPath" }
        }
        if (Get-ChildItem $app -Force | Select-Object -First 1) { throw 'Application directory is not empty; recovery state differs from failed SQL phase.' }
        if ((& /usr/bin/stat -c '%U:%a' $secretFile) -ne 'root:600') { throw 'Recovery secret permissions differ from expected root:600.' }
    } elseif ($existingAccount) { throw 'Service account already exists; inspect before adopting it.' }
    $container = (& /usr/bin/docker inspect sql-server-developer | ConvertFrom-Json)[0]
    $passwordEntry = $container.Config.Env | Where-Object { $_.StartsWith('MSSQL_SA_PASSWORD=') } | Select-Object -First 1
    if (-not $passwordEntry) { throw 'Existing bootstrap credential is unavailable.' }
    $saPassword = $passwordEntry.Substring('MSSQL_SA_PASSWORD='.Length)
    $container = $null; $passwordEntry = $null
    $exists = Invoke-ContainerSql 'sa' $saPassword 'master' "SET NOCOUNT ON; SELECT CASE WHEN SUSER_ID(N'mcpserver_omarchy') IS NULL THEN 0 ELSE 1 END;"
    if ($exists.Trim() -ne '0') { throw 'SQL login already exists; credentials must be reconciled without password reset.' }
    $databaseExists = Invoke-ContainerSql 'sa' $saPassword 'master' "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'McpServer_Omarchy') IS NULL THEN 0 ELSE 1 END;"
    if ($databaseExists.Trim() -ne '0') { throw 'Named database already exists; inspect its ownership, schema and data before adopting it.' }
    if (-not $ResumeAfterSqlBatchFailure) {
    if (Test-Path $secretFile) { throw 'Secret file already exists; refusing to overwrite.' }
    & /usr/bin/useradd --system --user-group --home-dir /var/lib/mcpserver --no-create-home --shell /usr/bin/nologin mcpserver
    & /usr/bin/install -d -m 0755 /opt/mcpserver $app
    & /usr/bin/install -d -m 0750 -o mcpserver -g mcpserver /var/lib/mcpserver /var/log/mcpserver
    & /usr/bin/install -d -m 0700 /etc/mcpserver

    # Save ACLs before changing only the paths this service needs.
    $aclBackup = @(& /usr/bin/getfacl -p /home/sharpninja $Workspace "$Workspace/.gitignore")
    foreach ($path in @("$Workspace/.mcpServer", "$Workspace/docs/Project")) { if (Test-Path $path) { $aclBackup += & /usr/bin/getfacl -p -R -- $path } }
    $aclBackup | Set-Content '/etc/mcpserver/workspace-acl-before.txt'
    & /usr/bin/setfacl -m u:mcpserver:--x /home/sharpninja
    & /usr/bin/setfacl -m u:mcpserver:rwx $Workspace
    & /usr/bin/setfacl -m d:u:sharpninja:rwx,d:u:mcpserver:rwx,d:o::--- $Workspace
    & /usr/bin/setfacl -m u:mcpserver:rw- "$Workspace/.gitignore"
    foreach ($path in @("$Workspace/.mcpServer", "$Workspace/docs/Project")) {
        if (Test-Path $path) {
            & /usr/bin/setfacl -R -m u:mcpserver:rwX -- $path
            Get-ChildItem $path -Directory -Recurse -Force | ForEach-Object { & /usr/bin/setfacl -m d:u:sharpninja:rwx,d:u:mcpserver:rwx -- $_.FullName }
            & /usr/bin/setfacl -m d:u:sharpninja:rwx,d:u:mcpserver:rwx -- $path
        }
    }

    $loginPassword = 'Aa1!' + [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(36))
    $connection = "Server=127.0.0.1,1433;Database=McpServer_Omarchy;User ID=mcpserver_omarchy;Password=$loginPassword;Encrypt=True;TrustServerCertificate=True;Application Name=McpServer_Omarchy;"
    "Mcp__Database__SqlServer__ConnectionString=$connection" | Set-Content $secretFile
    & /usr/bin/chmod 0600 $secretFile
    } else {
        $secretText = (Get-Content $secretFile -Raw).Trim()
        $prefix = 'Mcp__Database__SqlServer__ConnectionString='
        if (-not $secretText.StartsWith($prefix)) { throw 'Recovery environment key mismatch.' }
        $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
        $builder.set_ConnectionString($secretText.Substring($prefix.Length))
        if ($builder['Server'] -ne '127.0.0.1,1433' -or $builder['Database'] -ne 'McpServer_Omarchy' -or $builder['User ID'] -ne 'mcpserver_omarchy') { throw 'Recovery connection identity mismatch.' }
        $loginPassword = [string]$builder['Password']
        if ([string]::IsNullOrWhiteSpace($loginPassword)) { throw 'Recovery password is absent.' }
        $secretText = $null; $builder.Clear()
    }
    # Separate batches ensure the database exists before SQL Server compiles USE/user DDL.
    $null = Invoke-ContainerSql 'sa' $saPassword 'master' 'CREATE DATABASE [McpServer_Omarchy];'
    $quotedPassword = $loginPassword.Replace("'", "''")
    $null = Invoke-ContainerSql 'sa' $saPassword 'master' "CREATE LOGIN [mcpserver_omarchy] WITH PASSWORD=N'$quotedPassword', CHECK_POLICY=ON;"
    $provision = @"
IF USER_ID(N'mcpserver_omarchy') IS NULL CREATE USER [mcpserver_omarchy] FOR LOGIN [mcpserver_omarchy];
ALTER ROLE [db_owner] ADD MEMBER [mcpserver_omarchy];
"@
    $null = Invoke-ContainerSql 'sa' $saPassword 'McpServer_Omarchy' $provision
    $provision = $null; $quotedPassword = $null
    $dbProof = Invoke-ContainerSql 'mcpserver_omarchy' $loginPassword 'McpServer_Omarchy' "SET NOCOUNT ON; SELECT DB_NAME() + '|' + ORIGINAL_LOGIN();"
    if ($dbProof -notmatch 'McpServer_Omarchy\|mcpserver_omarchy') { throw 'SQL identity verification failed.' }
    Write-Output ('SQL identity: ' + $dbProof)

    Copy-Item "$PublishPath/*" $app -Recurse -Force
    # Remove only generated deployment JSON configs, never the repository source.
    Get-ChildItem $app -File -Filter 'appsettings*.json' | Remove-Item
    Get-ChildItem $app -File -Filter 'appsettings*.yaml' | Remove-Item
    Copy-Item '/tmp/mcpserver-setup/appsettings.yaml' "$app/appsettings.yaml"
    & /usr/bin/chown -R root:root $app
    & /usr/bin/chmod 0755 "$app/McpServer.Support.Mcp"
    # Host wrappers require pwsh.exe even on Linux; supply the approved binary alias.
    if (-not (Test-Path '/usr/local/bin/pwsh.exe')) { & /usr/bin/ln -s /usr/lib/powershell-7/pwsh /usr/local/bin/pwsh.exe }

    $unitText = @'
[Unit]
Description=MCP Server for PAYTON-OMARCHY
After=network-online.target docker.service
Wants=network-online.target docker.service
StartLimitIntervalSec=60
StartLimitBurst=3

[Service]
Type=simple
User=mcpserver
Group=mcpserver
WorkingDirectory=/opt/mcpserver/app
ExecStart=/opt/mcpserver/app/McpServer.Support.Mcp --urls http://localhost:7147
EnvironmentFile=/etc/mcpserver/mcpserver.env
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=HOME=/var/lib/mcpserver
StateDirectory=mcpserver
LogsDirectory=mcpserver
UMask=0027
Restart=on-failure
RestartSec=5
TimeoutStopSec=30
NoNewPrivileges=true

[Install]
WantedBy=multi-user.target
'@
    $unitText | Set-Content $unit
    & /usr/bin/chmod 0644 $unit
    & /usr/bin/systemd-analyze verify $unit
    & /usr/bin/runuser -u mcpserver -- /usr/bin/test -r "$app/appsettings.yaml"
    & /usr/bin/runuser -u mcpserver -- /usr/bin/test -w $Workspace
    & /usr/bin/runuser -u mcpserver -- /usr/bin/test -r "$Workspace/AGENTS.md"
    & /usr/bin/systemctl daemon-reload
    & /usr/bin/systemctl enable --now mcpserver.service
    [ordered]@{ timestamp=[DateTimeOffset]::UtcNow.ToString('o'); database='McpServer_Omarchy'; login='mcpserver_omarchy'; sqlIdentity=$dbProof; tls='Encrypt=True;TrustServerCertificate=True; local loopback'; service='mcpserver.service'; account='mcpserver'; executable="$app/McpServer.Support.Mcp"; binarySha256=(Get-FileHash "$app/McpServer.Support.Mcp").Hash; configurationSha256=(Get-FileHash "$app/appsettings.yaml").Hash; verification='Started; health and agent verification pending' } | ConvertTo-Json -Depth 5 | Set-Content $receiptPath
    & /usr/bin/chmod 0644 $receiptPath
    & /usr/bin/systemctl show mcpserver.service -p ActiveState -p SubState -p MainPID -p User -p ExecStart
} finally {
    $saPassword=$null; $loginPassword=$null; $connection=$null
}
