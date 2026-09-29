# Cross-platform UpdateService

Status: implementation present; full validation blocked. No completion or deployment approval. Operator explicitly requested implementation and then continued it.
Source baseline: develop / origin/develop `62e612cf1b6dacc392ea406b5e37716ef094212f`.
Requirements: FR-MCP-SERVICEUPDATE-001, TR-MCP-SERVICEUPDATE-001, TEST-MCP-SERVICEUPDATE-001.
TODO: IMPL-SERVICEUPDATE-001.

## Outcome and boundaries

The existing Nuke `UpdateService` target detects Windows or Linux. Windows retains its existing service name, install path, launcher, service-control, backup/restore, and health behavior. Linux updates an existing systemd service, defaulting to `mcpserver.service` and `/opt/mcpserver/app`. Unsupported operating systems/architectures fail before mutations. Linux supports x64 and arm64; the Windows target retains win-x64 compatibility. The existing Windows-only launcher is not published on Linux.

Linux first-time service/account provisioning is outside this update operation. It fails before stopping anything when the named unit is missing, its executable or working directory differs from the selected install directory, or required live configuration is missing. Existing unit, drop-ins, environment files, account, arguments and boot policy are preserved. This keeps local SQL secrets and dedicated service-account configuration intact.

No new package is required. Existing YamlDotNet supports object-based configuration reads. No raw MCP storage edits, force push, history rewrite, or unrelated plugin/cache changes are authorized by this implementation.

## Shared preservation policy

Use the current Windows preservation policy as the shared source: `appsettings.yaml` plus the entire configured root-level `DataFolder` (legacy fallback `Mcp:DataDirectory`); when DataFolder is the install directory, preserve the existing legacy directories and database-file patterns instead of recursively backing up the application itself. Resolve YAML structurally, including quoted/relative paths; malformed configuration fails closed. Windows retains ZIP backups and its existing restore layout.

The initial Linux updater deliberately supports one base `appsettings.yaml`, the service working directory equal to the selected install directory, and no instance/content-root/data-path overrides. Before stopping, reject live `appsettings.json`, environment-specific JSON/YAML overlays, `MCP_INSTANCE`, DataFolder/DataDirectory/instance overrides in inline environment, EnvironmentFile declarations or a running process environment, and `--instance`, data-path or content-root arguments. Inspect values privately and never include environment contents in logs/errors. An inactive existing unit may be updated using its configured environment files and unit properties; a running unit additionally has its process command line/environment checked. These explicit unsupported configurations fail closed rather than silently backing up the wrong data. Database connection environment variables remain supported and unchanged.

Linux collects those same live paths, plus the loaded unit fragment, drop-ins, and EnvironmentFile paths reported by systemd. A private timestamped directory under `/var/backups/mcpserver` holds a GNU tar archive with ownership, modes, ACLs, extended attributes and symbolic links preserved. Directories are mode0700 and the archive mode0600. Required missing environment/configuration paths fail before service stop; optional missing EnvironmentFile entries remain absent. Never print configuration/environment contents. Reject unsafe root paths, symlink preservation roots, backup/source overlap and publish/install overlap before mutation. Nested symlinks are archived without dereferencing.

The service updater preserves file-based data; external SQL databases are not exported or migrated by the backup helper. The server retains its existing startup migration behavior. A retained configuration/data archive is not a database backup or an automatic binary rollback.

## Execution contract

1. Resolve platform defaults and validate explicit parameters. Use shell-free argument lists and bounded subprocess execution. Linux requires effective UID0, systemctl and GNU tar, an installed systemd unit, matching apphost/working directory and a safe archive location. Acquire an exclusive lock keyed by the normalized installation path under a private `/run/lock/mcpserver-update` directory before preflight and hold it through failure restoration or successful verification; aliases therefore share the lock. A concurrent updater fails without service/file mutations. Lock/root/preservation paths and ancestors must not be symlinks. Private lock/archive directories must be root-owned. Reject systemd filesystem remapping, including private temporary directories, because configured paths would refer to different host files.
2. Prepare a unique publish stage before stopping the service (or validate `--skip-build --publish-source`). Validate the Linux apphost is ELF for the selected architecture and is executable. Honor `--skip-version-bump`; for this deployment use it to preserve the approved checkout version. Validate all preserved paths before stop.
3. Stop only the selected unit and verify it stopped. Archive and verify live config/data/systemd state before stale-file removal or copying. Require successful tar creation and listing; record archive SHA256 and verify it before each restore. The real tar smoke additionally verifies original bytes/modes/symlinks and ACLs.
4. Reject stage symlinks and copy only ordinary staged files/directories. Linux cleanup uses ordinal paths and traverses the install tree without following links; unlink stale symlink entries themselves, never recurse through them. Protect preserved roots from cleanup; recheck target paths before copying so an existing symlink cannot redirect writes outside install root. Copy the staged build and brain-slot runtime defaults, then restore preserved live configuration/data from the archive with metadata. Preserve executable mode on the new apphost. Do not rewrite the unit or environment file.
5. Reload systemd, start the selected unit, require active state and matching `/proc/<MainPID>/exe`, then run server and workspace health checks. Existing Windows behavior stays compatible. Linux requires successful health HTTP response, successful `/api-key` authentication bootstrap, successful workspace registry enumeration with at least one enabled workspace, and no enabled workspace failure. Treat unavailable/malformed auth/registry, zero checked/healthy workspaces or nonzero failure count as fatal; an existing helper returning `(0,0,0)` is not success. Write a deployment manifest containing the actual platform apphost hash.
6. Retain the durable archive on success and failure. If copying or validation fails after backup, stop the selected service if necessary and restore preserved configuration/data before throwing. Do not silently claim a binary rollback or success. Keep the publish/backup evidence for recovery on failure; clean only our temporary stage after success.

Use one elevated PowerShell script for any final local deployment and verification. Build/test and publish preparation run as the operator first; the elevated script invokes the validated Nuke target with `--skip-build --skip-version-bump`, then verifies marker rotation, trust, listeners, effective memory and service identity. No manual copy-based deployment substitutes for the target.

## BDPv4 slices and validation

## Implementation interfaces

- `ServiceUpdatePlatform.Resolve(bool windows, bool linux, Architecture architecture)` returns a record with `IsWindows`, `DefaultServiceName`, `DefaultInstallPath`, `ExecutableName`, and `RuntimeIdentifier`; `Current` resolves actual runtime values.
- `WindowsServiceHelper.GetPreservedStatePaths(string installRoot)` exposes the shared preservation-path policy, while its existing Windows backup/restore APIs remain compatible. Its data-path resolver reads root DataFolder and legacy fallback structurally.
- `ServiceCommandResult(int ExitCode, string StandardOutput)` and a `Func<string, IReadOnlyList<string>, ServiceCommandResult>` injected into `LinuxServiceHelper` separate native effects from decisions. The production runner uses ProcessStartInfo.ArgumentList, drains both output streams, has a bounded timeout, and never includes command output/environment in errors.
- `LinuxServiceUpdateOptions(string ServiceName, string InstallPath, string PublishPath, string BackupRoot, string RuntimeIdentifier, string LockRoot)` carries explicit paths/RID; defaults are selected by the Nuke target. `LinuxServiceUpdateResult(string ArchivePath, string ArchiveSha256, int MainPid)` is the non-secret receipt.
- `LinuxServiceHelper.Update(LinuxServiceUpdateOptions options, Action copyRuntimeConfig, Action verifyHealth)` performs the locked lifecycle; injected actions only copy generated defaults inside install root and perform strict health verification. Unit properties/paths are parsed by internal static helpers so deterministic tests exercise actual parsing. Command doubles implement tar create/list/restore over test fixtures; a separate real tar smoke verifies the production archive commands/metadata.
- `WindowsServiceHelper.RequireHealthyWorkspaces(WorkspaceHealthResult result)` is a pure strict result validator used by Linux: Checked>0, Healthy>0, Failed==0. The existing Windows target retains its compatibility behavior.
- `Build.UpdateService` dispatches before calling Windows APIs. Linux publishes main server only, using its resolved RID and a unique stage, then invokes the helper. Existing parameter names remain; add Linux backup-root parameter. Linux lock root is fixed in production and overridable only through the internal options in tests.

All internal/public helpers and test methods receive XML documentation and requirement references. Stage creation/publishing must succeed before stop. No schema or database migration is added.

### Slice 1: requirements and tests

Persist FR/TR/TEST and acceptance criteria through the Codex plugin; map them and create the TODO. Independently review this plan before source implementation.

Add behavior tests first in `tests/Build.Tests/ServiceUpdateTests.cs`. A mock/reference harness covers ordering before exercising production code. Tests named by behavior cover:

- `PlatformSelection_PreservesWindowsDefaultsAndSelectsLinuxRid` and unsupported platforms/architectures.
- `PreservationPaths_UsesConfiguredDataFolderAndLegacyFallback`, including quoted relative paths and malformed YAML.
- `LinuxPreflight_RejectsMissingUnitMismatchedExecutableAndUnsafePaths` without stop/copy commands.
- `LinuxUnitState_ParsesDropInsAndOptionalEnvironmentFiles` with spaces and systemd escaping.
- `LinuxUpdate_ArchivesBeforeReplaceRestoresBeforeStartAndChecksHealth` with command doubles and temporary directories.
- `LinuxUpdate_BackupFailureNeverCopiesOrStarts`, `LinuxUpdate_CopyFailureRestoresAndRetainsArchive`, and `LinuxUpdate_HealthFailureStopsAndRetainsEvidence`.
- `LinuxArchive_UsesPrivateMetadataPreservingCommands` and `DeploymentManifest_HashesExtensionlessApphost`.
- `LinuxUpdate_ConcurrentInvocationFailsWithoutMutation`, `LinuxPreflight_RejectsUnsupportedConfigurationOverrides`, `LinuxReplacement_DoesNotFollowSymlinks`, and `WorkspaceHealth_UnavailableOrEmptyNeverPasses`.

Expected red: production contract absent or unsupported Linux behavior. Mocks-first behavior tests must pass before adding the production implementation. Independent red gate records actual expected failures; no skipped placeholders.

### Slice 2: implementation and regression

Add platform resolution and Linux lifecycle helpers with injected command execution for deterministic tests; dispatch from the existing target. Reuse the preservation policy and health checks; retain Windows defaults and launcher behavior. XML-document all new APIs/tests. Validate each changed source project compiles through the Codex code-verification hook.

Run the complete Build.Tests unit scope (all current and prior build tests, excluding tests already explicitly categorized Integration/AiReview) with zero failures and zero skips, plus the repository Nuke Test unit scope before deployment. The initial unfiltered baseline currently reports pre-existing plugin sibling checksum/missing-Copilot failures; resolve test prerequisites in an isolated validation checkout if needed, without modifying installed agent plugins or excluding failed unit tests. Report baseline defects separately and never call a failing gate passed. Any hang must be diagnosed and bounded, not omitted silently.

Run a temporary-directory Linux tar preservation smoke with real GNU tar, verifying original config/data bytes, mode and symlink behavior after overwrite/restore. Run Nuke target discovery/help to confirm the target and parameters. Independent implementation review must reach both98% thresholds and no applicable FAIL/UNKNOWN before acceptance/deployment.

Exact gates (using `/opt/mcpserver/dotnet` on PATH): `dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter FullyQualifiedName~ServiceUpdateTests --logger trx` for mock/red/green receipts; `dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter 'Category!=Integration&Category!=AiReview' --logger trx` for the entire build unit scope; `pwsh.exe -NoProfile -NonInteractive -File ./build.ps1 Test` for the repository unit scope; `pwsh.exe -NoProfile -NonInteractive -File ./build.ps1 --help` for target discovery. Parse each TRX and require Failed0/NotExecuted0/skipped0 and executed>0. Tests are not removed or selectively filtered for prior failures. The isolated prerequisite environment must retain all test source and genuine generated sibling plugin copies from the canonical core. Record the exact preparation and source hashes.

### Slice 3: local service verification

After the gates pass, use the new Nuke Linux path for the existing service under the prior redeploy authorization. Verify config/environment hashes unchanged, preserved data and metadata, archive existence/protection, expected published binary hash/version, active/enabled state, zero unexpected restarts, loopback listeners, trusted marker/nonce, workspace health and BDPv4 effective memory. Native-agent login/session checks remain waived by the operator. If validation fails, retain backups and do not mark the TODO or requirements complete.

Update this plan, MCP TODO/requirements, generated requirement projections and session log after each meaningful gate. Completion requires full retained test/review/runtime receipts; no commit/push is implied.

## Execution evidence, 2026-09-29 UTC

- Planning and tests-first reviews: AGREE99/99, no outstanding review findings. Durable receipts are under `docs/receipts/hv/20260928-serviceupdate-plan.*` and `20260929-serviceupdate-red.*`.
- Initial red: 35 expected failures, 2 mock passes, zero skips. Review-driven regressions were also shown red before fixes.
- Focused final updater scope: 66 passed, zero failures/skips, including real GNU tar restoration after changing data bytes, file modes, symlinks, ACLs and an extended attribute. Owner/group identity is checked in the temporary fixture. Special-file tests verify that FIFO stage/apphost/configuration/environment/unit inputs fail before stop or blocking reads. See `docs/receipts/tests/serviceupdate/serviceupdate-green.trx`.
- Interim code review: AGREE99/99, 8 review PASS, zero FAIL/UNKNOWN, but explicitly no completion/deployment approval. See `docs/receipts/hv/20260929-serviceupdate-implementation.verdict.txt`.
- Unchanged-head Build.Tests baseline in an isolated checkout: 165 passed, 1 failed, zero skipped. Failure: QBAgent IL2026 inventory expected36, actual60. Canonical plugin sibling copies and full solution restore supplied test prerequisites without altering installed plugins. Disabling reusable MSBuild/compiler processes resolved a pipe-EOF hang identified from a test dump.
- Final complete Build.Tests unit scope with the implementation: 221 passed, 1 failed, zero skipped (222 executed). The sole failure is the same QBAgent inventory mismatch as the unchanged baseline. Source hashes match the validation mirror. See `docs/receipts/tests/serviceupdate/serviceupdate-full-build.trx` and `validation-source.json`.
- Repository Nuke Test: Compile succeeded; the server unit suite had2791 passed,59 failed,zero skipped. Nuke stopped there, so later test projects were not run by that invocation. Full failure names and log are in `docs/receipts/tests/serviceupdate/validation-blockers.json` and `nuke-test.log`. Incidental triage report: `triage-report-fa28239db64d4a74a986b82f74c6c07c`.
- No running-service update, restart, commit or push. Full unit and runtime gates remain unsatisfied; requirement/TODO completion is withheld.
