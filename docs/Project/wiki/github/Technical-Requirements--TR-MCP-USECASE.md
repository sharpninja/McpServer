# TR-MCP-USECASE

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-USECASE-001

**Use case EF storage multi-provider soft-delete** — Implement Use Case 4NF entities on McpDbContext with WorkspaceId max 1024, FK to Workspaces, soft-delete columns, Restrict/NoAction relationships, string FrId to RequirementEntity Kind=fr, global workspace query filters, and migrations for SQLite, PostgreSQL, and SQL Server named AddUseCaseSupport (timestamped). Audit emission for mutable entities per TR-MCP-DB-004.
**Covered by:** FR: FR-MCP-USECASE-001, FR-MCP-USECASE-002, FR-MCP-USECASE-003; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-002, TEST-MCP-USECASE-004, TEST-MCP-USECASE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-002

**Use case CQRS commands and queries** — All use case domain mutations and reads go through McpServer.Cqrs ICommand/IQuery handlers registered with Dispatcher. Controllers and MCP tools only dispatch; they do not open DbContext for domain logic.
**Covered by:** FR: FR-MCP-USECASE-001, FR-MCP-USECASE-002, FR-MCP-USECASE-003, FR-MCP-USECASE-004; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-002, TEST-MCP-USECASE-004, TEST-MCP-USECASE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-003

**Use Case REST controller** — Thin /mcpserver/usecases dispatching CQRS. Covered by FR-MCP-USECASE-001; TEST-MCP-USECASE-002. Controller unit tests map Result failures to HTTP status codes.
**Covered by:** FR: FR-MCP-USECASE-001; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-002, TEST-MCP-USECASE-004
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-004

**Use Case diagram service** — DI-owned pure generator; mermaid + one extra format. Covered by FR-MCP-USECASE-005; TEST-MCP-USECASE-003.
**Covered by:** FR: FR-MCP-USECASE-005; TEST: TEST-MCP-USECASE-003
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-005

**Use Case MCP tools, client, plugins** — usecase_* tools; UseCaseClient live JSON parity; plugin-core + skills; REPL client.UseCases. Covered by FR-MCP-USECASE-001, FR-MCP-USECASE-007; TEST-MCP-USECASE-004, TEST-MCP-USECASE-010.
**Covered by:** FR: FR-MCP-USECASE-001, FR-MCP-USECASE-007; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-002, TEST-MCP-USECASE-004, TEST-MCP-USECASE-008, TEST-MCP-USECASE-010, TEST-MCP-USECASE-011
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-006

**FR projection, coverage, audit** — linkedUseCases on FR get/list; coverage API; TR-MCP-DB-004 audit on mutable UC ops. Covered by FR-MCP-USECASE-003, FR-MCP-USECASE-006; TEST-MCP-USECASE-005, TEST-MCP-USECASE-007.
**Covered by:** FR: FR-MCP-USECASE-003, FR-MCP-USECASE-006; TEST: TEST-MCP-USECASE-001, TEST-MCP-USECASE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-007

**Use Case UI hosting** — Static /usecases/ assets REST-only. Covered by FR-MCP-USECASE-007; TEST-MCP-USECASE-008.
**Covered by:** FR: FR-MCP-USECASE-007; TEST: TEST-MCP-USECASE-008, TEST-MCP-USECASE-010, TEST-MCP-USECASE-011
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-008

**Approval and product API** — CQRS + REST for approval and ProductKey. Covered by FR-MCP-USECASE-008, FR-MCP-USECASE-009; TEST-MCP-USECASE-009.
**Covered by:** FR: FR-MCP-USECASE-008, FR-MCP-USECASE-009; TEST: TEST-MCP-USECASE-009
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-009

**Validation seams** — Docs matrix includes USECASE IDs; shared Realizes algorithm for DB findings. Covered by FR-MCP-USECASE-010; TEST-MCP-USECASE-005.
**Covered by:** FR: FR-MCP-USECASE-010; TEST: TEST-MCP-USECASE-005
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-010

**Use Case service deploy** — Nuke UpdateService only; config backup/restore; live health and routes. Covered by FR-MCP-USECASE-007; TEST-MCP-USECASE-011.
**Covered by:** FR: FR-MCP-USECASE-007; TEST: TEST-MCP-USECASE-008, TEST-MCP-USECASE-010, TEST-MCP-USECASE-011
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-011

**Graph storage** — JSON column graph storage; multi-provider migrations; soft-delete and workspace filters. ACs AC-T11-1..3.
**Covered by:** FR: FR-MCP-USECASE-012; TEST: TEST-MCP-USECASE-013, TEST-MCP-USECASE-014, TEST-MCP-USECASE-016
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-012

**Diagram graph CQRS** — GetGraph/PutGraph/Export handlers; thin controller. ACs AC-T12-1..2.
**Covered by:** FR: FR-MCP-USECASE-012; TEST: TEST-MCP-USECASE-013, TEST-MCP-USECASE-014, TEST-MCP-USECASE-016
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-013

**Diagram graph REST** — GET/PUT diagram-graph; GET diagram kind=usecase format mermaid|plantuml; sequence remains. ACs AC-T13-1..3.
**Covered by:** FR: FR-MCP-USECASE-012, FR-MCP-USECASE-013, FR-MCP-USECASE-014; TEST: TEST-MCP-USECASE-013, TEST-MCP-USECASE-014, TEST-MCP-USECASE-016, TEST-MCP-USECASE-012
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-014

**UML serialization service** — Pure DI service graph to Mermaid and PlantUML. ACs AC-T14-1..2.
**Covered by:** FR: FR-MCP-USECASE-013, FR-MCP-USECASE-014; TEST: TEST-MCP-USECASE-012, TEST-MCP-USECASE-014
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-015

**Canvas UI hosting** — Static /usecases/ canvas assets; palette; umlCanvas; REST-only. ACs AC-T15-1..2.
**Covered by:** FR: FR-MCP-USECASE-011; TEST: TEST-MCP-USECASE-015, TEST-MCP-USECASE-017
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-016

**Graph put audit** — Put graph emits DataAuditLog. AC-T16-1.
**Covered by:** FR: FR-MCP-USECASE-012; TEST: TEST-MCP-USECASE-013, TEST-MCP-USECASE-014, TEST-MCP-USECASE-016
**Status:** pending
Scope: layer-1+

## TR-MCP-USECASE-017

**Handle-relative containment, bounded providers, canonical identity, and hermetic evidence** — Normalize durable identifier strings before computing derived hashes. Use pinned-directory-relative Windows NT and POSIX operations for contained creation, open, replacement, rollback deletion, and empty-directory removal. Resolve physical paths through finite cancellable native operations used asynchronously by persistence and federation. Snapshot only regular files with no-follow nonblocking stat enforcement. Scope SQLite open and query work with exact restoration of managed, EF, connection-string, native timeout, and handler state; preserve real provider exceptions. Discover repository roots from stable assembly or source metadata and bound process plus pipe-drain lifetime for full process trees.
**Covered by:** FR: FR-MCP-USECASE-015; TEST: TEST-MCP-USECASE-018
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] McpDbContext normalizes all durable identity fields before computing hashes, and every derived hash matches the exact final persisted value.
- [ ] WorkspaceContainedFileSystem performs Windows creation/open/deletion and Linux contained mutation relative to pinned parents, validates the opened object, and never follows a reparse or symbolic link.
- [ ] SaveChangesAsync and federation upsert await finite cancellation-aware physical-resolution APIs whose native implementations do not use abandoned Task.Run work, accumulating threads, or repeated uncancellable Directory.Exists calls.
- [ ] Linux file open uses no-follow and nonblocking semantics plus descriptor stat/type checks so FIFOs, devices, sockets, and directories cannot block or enter snapshots.
- [ ] SQLite scoped operations preserve and restore exact connection, EF, connection-string, native busy-timeout, and busy-handler state without deriving native state from DefaultTimeout.
- [ ] SQLite opening is performed by a bounded owned worker/lifetime mechanism that covers pre-handle blocking and proves no worker, handle, pooled-state, callback, or interrupt leaks across cancellation and repeated calls.
- [ ] Cancellation classification is based on provider cancellation evidence and exception type/state rather than token state alone for SQLite, SQL Server, and PostgreSQL.
- [ ] Evidence tests resolve a validated root from assembly/source anchors without MCP_REPOSITORY_ROOT and process execution bounds parent exit, both stream drains, and the descendant process tree.
- [ ] Evidence inventories are regenerated only after finalization and verified by an independent parsing pass with exact command-to-output and TRX outcome accounting.
- [ ] Historical migration bytes and previously approved trust, preflight, cleanup, non-Windows, trailer, and line-ending guarantees remain unchanged.

## TR-MCP-USECASE-018

**Cross-platform bounded native I/O and deterministic evidence** — Use searchable Linux directory modes and descriptor-relative restoration. Isolate potentially blocking physical identity and SQLite pre-handle opens behind finitely owned platform cancellation mechanisms. Classify cancellation from direct operation evidence, and resolve repository fixtures from deterministic assembly metadata rather than mapped caller paths.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Linux mkdirat restoration uses an owner-searchable mode and verifies descriptor-relative nested directory and regular-file recreation with exact expected contents and applicable permission bits.
- [ ] Physical identity native work is performed outside the caller, has an owned finite lifetime, observes cancellation across native-open timing gaps, and cannot accumulate blocked probes or workers.
- [ ] Cross-platform SQLite pre-handle open cancellation terminates the blocking native operation or its isolation boundary within deadline and reclaims every resource.
- [ ] Cancellation translation requires a provider- or operation-specific cancellation marker and never relies on token state alone for IOException, ObjectDisposedException, or provider exceptions.
- [ ] Deterministic CI builds embed a stable repository-root anchor consumed by external repository and Agent Help fixtures when CallerFilePath is mapped to /_/ and no root environment variable exists.
- [ ] Platform-specific native tests use explicit applicable cases and assertions without runtime early-return or skip paths.
- [ ] Unicode-dash persistence history is audited; any possibly deployed noncanonical identity receives an idempotent collision-diagnostic repair, otherwise evidence proves no deployment occurred.

## TR-MCP-USECASE-019

**Race-safe physical I/O, causal cancellation, and immutable evidence** — Resolve and validate physical filesystem identity through interruptible native handles or isolated workers whose public deadlines and accounting remain truthful; bind SQLite opening to the validated object without lexical races; attribute PostgreSQL cancellation to the actual caller cancel request; compose platform-native tests and process containment without pass-through fallbacks; and generate exact-final-HEAD evidence through a mechanically verified immutable workflow.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Physical path resolution revalidates the physical target and mount identity at the operation boundary, rejects unsafe FUSE, NFS, and equivalent mounts reached through aliases or replacement, and exposes active worker state until the underlying native work has really terminated.
- [ ] Windows physical resolution has a finite public and cleanup deadline even when CancelSynchronousIo fails, performs no unbounded join, and closes check-to-open races by keeping subsequent native blocking work cancellable or fail-fast within contract.
- [ ] Every public physical-identity consumer, including use-case CQRS helpers, uses cancellable asynchronous resolution end to end; no public synchronous API performs uncancellable physical traversal.
- [ ] SQLite path validation binds opening to a physically validated no-follow object or equally race-safe isolation boundary; cancellation and timeout cleanup have finite joins, accurate active-worker and handle accounting, and original state and exception causality.
- [ ] PostgreSQL caller cancellation translation requires operation-scoped evidence that the caller cancellation mechanism actually issued or completed the cancel affecting that command; SQLSTATE 57014 alone plus token state is never sufficient.
- [ ] Platform-native test projects or discovery filters expose only applicable executable cases and an inventory contract checks exact expected cases and results without source-string substitutes; containment race tests distinguish narrowly expected transients and require successful contained I/O.
- [ ] Windows evidence helpers place the child in a kill-on-close job at creation or behind an equivalent suspended-start assignment boundary before user code can spawn descendants; all stream drains and cleanup joins share finite deadlines and non-Windows process groups remain correct.
- [ ] Evidence uses an immutable two-phase candidate or retests the final commit after the last content and evidence change; every command cell is directly executable or fully reconstructable, SHA-256 inventories and TRX totals are independently checked, and historical receipts receive explicit non-destructive supersession notices.
- [ ] Historical SQL Server migration bytes remain identical to the approved base. The real LocalDB provider harness applies semantics-preserving bounded execution-plan hints only at test execution time, without extending production timeouts; the existing 300-second allowance remains finite and test-only.
- [ ] Opaque logical workspace identifiers that are not rooted filesystem paths, including ws:test-style storage keys, are normalized lexically and never sent to native physical traversal; rooted filesystem inputs continue through cancellable physical identity resolution.

## TR-MCP-USECASE-020

**Identity-preserving use-case component update pipeline** — Implement workspace- and parent-scoped CQRS mutations for existing actor, flow, and step components, exposed consistently through REST, typed client, source-generated JSON, and MCP tool surfaces.
**Covered by:** FR: FR-MCP-USECASE-018; TEST: TEST-MCP-USECASE-021
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CQRS update commands scope every actor, flow, and step lookup by workspace and complete parent identity, use tracked in-place mutations, and return not-found for mismatches.
- [ ] Actor updates validate name and actor type, flow updates validate flow type, name, and positive sequence, and step updates validate positive order, nonempty action, and an actor associated with the same use case when ActorId is supplied.
- [ ] REST routes, the typed client, generated JSON metadata, and MCP tools expose update-actor, update-flow, and update-step with cancellation and classified failures.
- [ ] Each successful component update advances UseCaseEntity.UpdatedAtUtc, emits the established use-case mutation audit signal, and leaves approval/version and unrelated aggregate data unchanged.

