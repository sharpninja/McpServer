<# .SYNOPSIS Grants the SQL encryption-status prerequisite and verifies the approved service restart. #>
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$true
if ((& /usr/bin/id -u) -ne '0') { throw 'Administrator required.' }
# Import only the reviewed SQL helper, without re-executing provisioning.
$tokens=$null; $errors=$null
$tree=[Management.Automation.Language.Parser]::ParseFile('/tmp/mcpserver-setup/install-local-service.ps1',[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Installer helper parse failed.' }
$function=$tree.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-ContainerSql'},$true)
if (-not $function) { throw 'SQL helper unavailable.' }
. ([scriptblock]::Create($function.Extent.Text))
$container=(& /usr/bin/docker inspect sql-server-developer | ConvertFrom-Json)[0]
$entry=$container.Config.Env | Where-Object { $_.StartsWith('MSSQL_SA_PASSWORD=') } | Select-Object -First 1
if (-not $entry) { throw 'Bootstrap credential unavailable.' }
$saPassword=$entry.Substring('MSSQL_SA_PASSWORD='.Length)
$container=$null; $entry=$null
try {
    $null=Invoke-ContainerSql 'sa' $saPassword 'master' 'GRANT VIEW SERVER SECURITY STATE TO [mcpserver_omarchy];'
    $proof=Invoke-ContainerSql 'sa' $saPassword 'McpServer_Omarchy' "SET NOCOUNT ON; EXECUTE AS LOGIN = 'mcpserver_omarchy'; SELECT DB_NAME() AS DatabaseName, SUSER_SNAME() AS LoginName, HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER SECURITY STATE') AS CanReadSecurityState; SELECT COUNT(*) AS AppliedMigrations FROM __EFMigrationsHistory; SELECT COUNT(*) AS AppTables FROM sys.tables; REVERT;"
    Write-Output $proof
    & /usr/bin/systemctl reset-failed mcpserver.service
    & /usr/bin/systemctl start mcpserver.service
    Start-Sleep -Seconds 15
    & /usr/bin/systemctl show mcpserver.service -p ActiveState -p SubState -p MainPID -p NRestarts -p Result
    & /usr/bin/ss -ltnp '( sport = :7147 )'
    $journal=& /usr/bin/journalctl -u mcpserver.service --since '-30 seconds' --no-pager -o cat
    $journal | Where-Object { $_ -match 'exception|ERR|fail|denied|Now listening|Application started|marker|ready|FATAL' } | Select-Object -Last 30 | ForEach-Object { $_.Replace($saPassword,'[REDACTED]') }
} finally { $saPassword=$null }
