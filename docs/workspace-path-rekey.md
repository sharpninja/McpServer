# Workspace path rekey (P1.5)

The deployment hold remains in force. This document describes the maintenance interface; this implementation session does not apply it to a live database or operate any running server.

P1.4 canonicalizes workspace query parameters. A database that still stores Windows backslash keys must be rekeyed before those queries run. HTTP and STDIO startup therefore inspect raw stored workspace ids before migrations/backfills and refuse legacy data with a reference to `--workspace-path-rekey`.

## Rehearsal

Prepare a separate, consistent database copy with the usual provider backup/restore tools. The command does not create the rehearsal copy. Select the configured instance and run:

```text
dotnet QBrainAi.Support.Mcp.dll --workspace-path-rekey --instance <instance> --dry-run --copy-database <existing-copy> --report-path <new-report.json>
```

For SQLite, the copy is an existing file distinct from the configured file; the command opens it read-only and refuses reparse paths. For SQL Server and PostgreSQL, the copy is a different catalog on the configured server. PostgreSQL dry-run also sets the connection's transaction default to read-only. The selected copy replaces the configured database before any connection is opened. Supply a real copy, never another live catalog or a filesystem alias of a live file.

Dry-run performs raw reads and emits only workspace-key mappings, table row counts, per-workspace row counts, and the number of retained manual or otherwise non-reproducible triage groups. It does not create a backup or access the configured token file. Reports omit connection strings, credentials, token payloads, and arbitrary row contents.

## Offline apply

Apply is a separately authorized operator operation after the deployment hold is lifted. The database and token store must be offline for the entire operation. This command never stops or starts services.

```text
dotnet QBrainAi.Support.Mcp.dll --workspace-path-rekey --instance <instance> --backup-path <new-backup> --report-path <new-report.json>
```

An explicit `--copy-database` can select a rehearsal copy for apply. Copy rehearsals leave the configured token file alone. A new backup destination is mandatory for each apply invocation, including an idempotency check; database rows are unchanged on a second run.

- SQLite uses the online-backup API and verifies `integrity_check`. Existing backup targets are refused. Encrypted SQLite remains blocked until the repository's SEE runtime is provisioned.
- SQL Server uses a new server-side backup path, `BACKUP ... COPY_ONLY, CHECKSUM`, and `RESTORE VERIFYONLY ... CHECKSUM`. The account needs backup, verification, catalog metadata and constraint-management permissions.
- PostgreSQL requires `pg_dump` and `pg_restore` on PATH. The command creates a custom-format archive, verifies its table-of-contents and signature, and fails before updates on any backup error. Credentials travel through the child environment and native output is withheld.

Policy A reuses P1.4 lexical Windows normalization, removes device prefixes, resolves lexical dot segments, and preserves case. POSIX, opaque and global identifiers remain unchanged. Distinct old keys with identical normalized targets abort during preflight. SQL inventory and updates use binary comparisons to preserve each stored spelling.

Preflight includes reproducible triage/handoff/federation keys. The engine replans inside a serializable transaction, keeps unique indexes active, defers or suspends foreign keys, restores and validates constraints, and validates table and workspace counts before commit. Failure or cancellation rolls back the database transaction. A second apply changes zero rows.

Manual triage GroupKey/GroupId values are retained because their random GUID input was not stored. Other triage groups are rehashed only if a stored report fingerprint reproduces the old digest and id. Explicit/custom federation identities are retained unless their old digest is reproducible; corresponding workspace columns still migrate.

The encrypted token file is preflighted before database apply, then rewritten after commit with an exact verified `.pre-rekey` backup. Ciphertext is never decrypted. A token/report failure after commit returns an error that explicitly says the database committed; retain the backups, resolve the file error and retry. Keep the server offline until both stores are ready.

## Validation scope

`WorkspacePathRekeyTests` uses disposable SQLite databases and independent literal/hash expectations. Historical migration tests also exercise rekeying actual provider schema before canonical EF queries. SQL Server/PostgreSQL copy selection is unit-tested; native backup/DDL rehearsals on real server copies remain an operator validation step under the deployment hold.
