BEGIN TRANSACTION;
ALTER TABLE [DataAuditLogs] ADD [PayloadEncodingVersion] int NULL;

ALTER TABLE [DataAuditLogs] ADD [PreviousSnapshotPayload] varbinary(max) NULL;

ALTER TABLE [DataAuditLogs] ADD [CurrentSnapshotPayload] varbinary(max) NULL;

ALTER TABLE [DataAuditLogs] ADD [DiffPayload] varbinary(max) NULL;

ALTER TABLE [DataAuditLogs] ADD [MetadataPayload] varbinary(max) NULL;

IF DATABASE_PRINCIPAL_ID(N'mcp_runtime') IS NULL
    CREATE ROLE [mcp_runtime] AUTHORIZATION [dbo];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [mcp_runtime];
DENY UPDATE, DELETE ON OBJECT::[dbo].[DataAuditLogs] TO [mcp_runtime];
DENY UPDATE, DELETE ON OBJECT::[dbo].[TodoAuditHistory] TO [mcp_runtime];
DENY INSERT, UPDATE, DELETE ON OBJECT::[dbo].[__EFMigrationsHistory] TO [mcp_runtime];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260929140000_VersionedAuditPayloads', N'10.0.0');

COMMIT;
GO
