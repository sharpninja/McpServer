# Permanent local audit storage on SQL Server

The audit ledgers `DataAuditLogs` and `TodoAuditHistory` remain local to the
database. TODO federation snapshots use audit history only to calculate a
local version; they do not include audit records. Wiki dumps mark both audit
tables `OMIT-LOCAL` and do not serialize their rows.

## Payload migration

Migration `20260929140000_VersionedAuditPayloads` adds four nullable
`varbinary(max)` payload columns and a nullable encoding version. Historical
JSON text columns remain unchanged and readable. New audit rows write
GZip-compressed UTF-8 payloads with version 1. There is no retention purge,
historical rewrite, or table shrink. The generated SQL Server script is in
`docs/receipts/audit-payload-sqlserver-preview.sql`. Apply it only through
the provider migration workflow with a schema administrator after an
independently verified backup.

For design-time migration using an administrator connection, set
`MCP_EF_PROVIDER=sqlserver`,
`MCP_EF_MIGRATIONS_ASSEMBLY=McpServer.Storage.SqlServerMigrations`, and
`MCP_EF_CONNECTION_STRING` in the administrator's process environment, then
run:

```powershell
dotnet ef database update 20260929140000_VersionedAuditPayloads --project src/McpServer.Storage.SqlServerMigrations --startup-project src/McpServer.Support.Mcp --context McpDbContext
```

Do not put the administrator credential in source control or shell history.

## Runtime SQL principal

The SQL Server migration creates the `mcp_runtime` database role. It grants
ordinary DML on `dbo` and denies UPDATE and DELETE on both audit ledgers.
It also denies writes to `__EFMigrationsHistory`. A SQL Server `sysadmin`
or `db_owner` principal defeats the intended least-privilege boundary.

After the migration, provision a distinct service login through the
environment's secret management process, map its database user, and add
that user only to `mcp_runtime`:

```sql
CREATE USER [mcp_service_user] FOR LOGIN [mcp_service_login];
ALTER ROLE [mcp_runtime] ADD MEMBER [mcp_service_user];
```

Set the service's SQL Server connection string to that login and set
`Mcp:Database:AutoMigrate: false` (or
`Mcp__Database__AutoMigrate=false`). The HTTP and stdio hosts then check
for pending migrations at startup and fail if the administrator has not
applied them. The default remains `true` for existing deployments.

Verify the service connection under the service identity:

```sql
SELECT USER_NAME() AS database_user,
       IS_SRVROLEMEMBER('sysadmin') AS is_sysadmin,
       HAS_PERMS_BY_NAME('dbo.DataAuditLogs', 'OBJECT', 'INSERT') AS can_insert_audit,
       HAS_PERMS_BY_NAME('dbo.DataAuditLogs', 'OBJECT', 'UPDATE') AS can_update_audit,
       HAS_PERMS_BY_NAME('dbo.DataAuditLogs', 'OBJECT', 'DELETE') AS can_delete_audit;
```

The expected values are `0` for sysadmin, `1` for audit INSERT, and
`0` for audit UPDATE and DELETE. Repeat the permission check for
`dbo.TodoAuditHistory`. An administrator can still change the role or
data; operational controls and backups remain necessary for permanent
retention.
