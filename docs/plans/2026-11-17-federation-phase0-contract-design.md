# Phase 0 contract and real-binding design

Plan: PLAN-FEDDOLT-001. Group: GRP-FEDPH0-001. Source: sections 5 and 15.4 of `2026-11-10-federation-dolt-bdpv4.md`. This is the group's contract-step design, not a requirement amendment, accepted B output, deployment authorization or completed implementation. The approved plan remains authoritative. DEPLOY HOLD remains active.

## Boundaries and evidence

The 18 named FR/AC pairs assigned to tasks 001-008 are the union reviewed by task 009. The eight existing test assertion seams use scripted probes first. Real adapters must execute the specified operations and return observations through the same interfaces, preserving the mock-validated assertions. They live in a disposable integration harness, never in unit-test projects or a production package. Environment-dependent runs belong in an integration project and run through Nuke.

The assigned ACs do not exhaust the work described by B1-B12. Each task must also supply the source inventory, measurements, dependency selections and real-engine experiments required by its B outputs. Passing the narrow mocks does not discharge those outputs. Every output retains separate LEGION and LAB rows with owner, source head, exact versions, command, outcome, receipt paths, SHA256, limitations and dependent phases. Unknown is retained until measured; there is no default success.

Fixtures are run-scoped under `TestResults/<run-id>` and use no enrolled production project or live workspace database. FED-ATLAS-H/A/B have distinct processes, databases, loopback dynamic ports and data roots. Provision and stop operations are Nuke-owned. Service hosting uses only a concrete operator-approved alternative when the native host is absent or unsuitable. The WinSW proposal is pending and is not adoption. All original data and diagnostic evidence are retained.

## Task 001: exact dependency and native host resolution

Trace: FR-MCP-186 ac-FourProvidersResolve, TR-MCP-FEDERATION-013, TEST-MCP-234. `Phase0ProviderResolutionContract` checks resolved provider identities, mappings, versions and failure behavior from the resolution probe. Its negative controls exercise mismatched names and missing or incompatible metadata.

Real binding resolves SQLite, PostgreSQL, SQL Server and the operator-accepted EF10 MySQL-protocol provider against disposable databases. Candidate Dolt/provider/driver and image coordinates remain candidates until byte hashes, licensing, dependency constraints and actual compatibility pass. Preserve the repository EF10.0.0 baseline; patch a supported community fork when needed rather than introducing a bespoke provider. No package warning is ignored.

B1 also inventories actual native Windows SCM account, executable, start mode, recovery, ACL and child lifecycle, plus LAB `/var/lib/dolt` hosting. Do not infer absent service hosting from a Dolt-name-only query. Actual lifecycle tests include graceful stop, unexpected child exit, restart policy, process-tree cleanup and unrelated-service preservation. Deployment uses Nuke and remains separate from dependency metadata discovery.

## Task 002: database compatibility and commit boundaries

Trace: FR-MCP-186 ac-DoltBaselineAndUpgrade, FR-MCP-177 ac-PooledConnectionDoesNotLeakBranch, FR-MCP-175 ac-SaveDoesNotPublish, TR-MCP-FEDERATION-002/004/013, TEST-MCP-223/225/234.

`IPhase0MigrationProbe` supplies empty, schema1, CRUD, schema2, CRUD and repeat-schema2 observations. Fixed fixture expectations preserve decimal scale, timestamp precision, Unicode/null values, IDs, ordered migration history and zero changes on repeat. `IPhase0PooledReadProbe` alternates 100 explicit Atlas main/feature reads and checks identity plus value every time. `IPhase0SaveProbe` rereads a saved draft and observes unchanged Dolt and remote refs, zero history commits and zero publication intents.

Additional compatibility assertions must cover the complete current EF model, representative LINQ and parameterized raw SQL, concurrency tokens, rollback, mapped decimal/date/text/index/FK behavior and existing provider migration integrity. Expected rows and errors come from a fixed fixture specification, not from production output. Capture actual information_schema/model manifests and exact query results. A rejected query or unsupported DDL is a concrete feasibility finding; do not weaken the fixture or map silently to lossy types. Explicit SQL transaction commit and explicit Dolt history commit must be observed separately.

## Task 003: guarded publication and remote recovery

Trace: FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy, TR-MCP-FEDERATION-012, TEST-MCP-233. Existing authorization assertions check project-scoped publication and forbidden direct remote mutation through the probe.

The disposable real binding attempts raw remote operations before and after valid gateway authorization, with wrong project, wrong scope and revoked identity. Read canonical refs before and after every attempted bypass. Clean and dirty working sets, lost responses and process death at publication/ref-advancement boundaries require exact ref/ancestor and retained-intent observations. Neither an HTTP success flag nor a policy simulation proves the Dolt remote is protected. Worker-only remote procedures and backend credentials must be tested at the actual accessible surfaces.

## Task 004: complete source classification, topology and identities

Trace: FR-MCP-184 ac-BackgroundWritersDoNotDirtyVersionedBranch; FR-MCP-182 ac-TwoNodesCreateDistinctIds/ac-LegacyIdsRemainValid; TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.

The identity/isolation seam checks distinct node-created IDs, preserved legacy identifiers and background-writer version-history boundaries. B2 additionally enumerates every EF entity, property, relationship, automatic column and interactive/background/raw-SQL writer from the pinned source. Every row has an explicit disposition and rationale. Entity name searches are inventory seeds, not proof of completeness. Automatic session capture remains unversioned; only explicit promoted snapshots enter project history. Mixed entities need per-column/scope dispositions rather than a blanket table classification.

B3 records standalone McpDbContext versus project/control contexts, additive migration ownership and validators for cross-database soft references. B4 specifies every generated and human-assigned ID family, two-character node-tag grammar, parser/sort/display/plugin/traceability compatibility and restore/allocator behavior. Source-derived candidate grammar is not accepted merely because a mock can generate it. Preserve all legacy IDs and links and test restored allocators for collision rejection.

## Task 005: actual existing identity platform

Trace: FR-MCP-185 ac-RevokedNodeCannotPublish/ac-LocalAppUserCannotPush; TR-MCP-FEDERATION-012; TEST-MCP-233; linked MCP-FEDIDENTITY-001.

The authorization seam must bind to the exact restored Duende7.4 packages and matching source/API tags. Exercise real issuance, introspection and revocation with client-per-node and single-user ownership, reference tokens and project scopes. An ordinary local application user cannot obtain backend Dolt publication authority. Verify revoked nodes fail closed at the gateway and raw remote, including token-cache effects. Secrets are consumed from protected backend channels and never emitted into arguments, receipts or versioned state.

## Task 006: merge, schema, migration and ownership safety

Trace: FR-MCP-180 ac-SameCellConflictNeedsReview; FR-MCP-186 ac-ContractBlockedUntilLocalBranchesAttested; FR-MCP-187 ac-SeedPhaseRejectsJoin; FR-MCP-189 ac-LeaseLostDuringWriteRejected; TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237.

Existing safety assertions check preserved local/incoming conflict evidence, all-local-branch contract attestation, no join in seed phase and ownership/fence behavior. Expiry alone does not invent rollback: preserve the approved in-flight completion semantics before a new fence takes ownership.

B6 probes real Dolt same-cell and constraint conflicts, conflict transactions, unrelated histories and retained review refs. Determine whether conflict persistence requires dolt_allow_commit_conflicts and prove it never enables unresolved conflicts on live project branches. B7 covers startup migration exclusion, single-author expand/contract distribution, mandatory pre-swap draft disposition, all local branches, offline old-schema commits and protocol negotiation. B8 inventories the complete seed/join/export manifest, audit backfill, duplicate aliases, canonical hashes and immutable migration/final-main observations. Legacy divergence requires explicit scoped review with no wildcard acceptance. B9 exercises every worker live-branch mutation under the write gate, working-set hashing, requestId trailers and crash reconciliation.

## Task 007: operations, restore and credential containment

Trace: FR-MCP-179 ac-HubRestoreFromOlderBackup; FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials; FR-MCP-185 ac-UnknownCertificatePinRejected; TR-MCP-FEDERATION-006/011/012; TEST-MCP-227/232/233.

The recovery seam observes restored older hub state, a fresh epoch, rejection of the old report, retained node work and recovery without a destructive reset. TLS probes reject wrong pins/SANs before sending bearer credentials. An invocation-local generated canary must be absent from every required capture channel and every additional returned key/value. Negative controls must not leak the canary through their own failure diagnostics. The thirteen required channels are arguments, receipts, logs, chat payloads, audit payloads, memory stores, generated files, staged Git blobs, new Git commits, Dolt working sets, new Dolt history, scanner diagnostics and rejected-commit previews.

B10 still needs measured fixture-scale latency/storage/history/resource budgets and explicit supported limits. B11 still needs polling, jitter, backoff, stale/review-age and epoch/generation observations under the exact approved contract. B12 still needs actual local/remote ref retention, GC and restore tests plus loopback/CA/pin configuration, disclosure response and real credential-safety scans. Mocks or estimated throughput cannot substitute for those measurements.

## Task 008: coverage, provisioning and operator acceptance

Trace: FR-MCP-186 ac-RealHostAcceptanceMatrix, TR-MCP-FEDERATION-013, TEST-MCP-234. `IPhase0FixtureProvisioningProbe` checks three isolated peer identities and resources, correct start/stop observations, retained diagnostics and unchanged unrelated-project fingerprints.

The Ph0 prerequisite is actual Nuke provisioning and teardown on both hosts, not the full Ph8 fault matrix. The complete RealHostAcceptanceMatrix remains bound to Ph8 and tests partitions, restarts and exact readbacks across component boundaries. Do not count Ph0 fixture mocks as satisfaction of that whole AC.

U01-U15 are registered Draft in the UC store from the approved workflow text. Retain exact aggregate readbacks and FR links. All core and retained criteria are allocated to phases in all-requirement-phase-bindings.json; 392 criterion rows across86 approved definitions match current live active-layer IDs/texts. Build.Tests has44 source-derived test classes, with an exact all-class TestGroups invocation retained; actual runtime case discovery and final gate execution remain pending. Task008 obtains explicit operator acceptance for all twelve host-separated B outputs. A missing acceptance is not inferred from permission to investigate.

## Task 009: independent group-exit gate

Trace: the exact18 FR/AC union and TR/TEST constraints in plan section15.4 task009. This member needs its own designed, discriminating mock gate before the group implementation barrier may open. Its structural evidence probe observes real review artifacts later; it does not launch a reviewer or judge semantic sufficiency in unit tests.

Acceptance assertions require the exact reviewed source head, the nine unique task verdicts and overall verdict, finite accuracy/completeness in98..100, and no substantive FAIL/UNKNOWN or missing access to required evidence. Operational-only infrastructure findings are retained separately and do not alone force DISAGREE, per current Global HV rule6. Each verdict remains independently adjudicated; a high overall score cannot hide a failing task.

Final Nuke Compile, Test, MigrationIntegrationTests, complete Build.Tests and ValidateTraceability must be represented for both hosts at the reviewed head. Executed test lanes have positive case totals, failed0, skipped0 and retained TRX. Non-test gates have native exit0 and their exact logs. Missing/duplicate/stale/wrong-host rows reject. All B1-B12 outputs need their operator acceptance records, separate from test metadata.

The independent reviewer runs on LAB-OMARCHY using verified GPT-5.6-SOL Max runtime, reads the supplied complete test evidence without rebuilding/rerunning, and adjudicates all nine tasks plus the overall group. Preserve byte-complete request/response JSONL, full verdict body and reviewer session/turn, with hash-verified persisted/readback evidence. Mock controls corrupt each relevant field, omit individual tasks/hosts/gates and corrupt receipt bytes, proving rejection rather than merely a false Boolean. The report schema checks cannot prove the underlying evidence truthful; the independent hostile review must judge that evidence and the whole scoped plan.

## Readiness and execution order

Do not infer Ready from this document. At this checkpoint eight mock suites have focused receipts on both hosts, including task009's structural gate. Broader B-output assertions and real bindings remain work. These are historical focused runs, not a cumulative final-head phase gate. Each member's design and complete current-version mock proof must be recorded before its simulated Planning-to-Ready transition. The group barrier opens only when all nine meet that rule. Dependencies still control which Ready task can start actual disposable implementation.

After the barrier opens, implement only disposable harness adapters, preserving the test assertions. Resolve genuine provider/hosting feasibility failures using the plan's operator decision path. Produce and obtain acceptance for B1-B12, run full final-head gates on both hosts, then run task009 HV. No goal, task, requirement or AC becomes completed from this design or its mocks. Phase1 production integration and held live cutover remain separately gated.

## B2/B3 source and convention inventory checkpoint

The receipt folder `docs/receipts/federation/20261117-phase0-source-catalog` pins the 1,279-file syntax scan and three existing compiled EF10 model inspections at source head ac07503d3a4b62e0ceb77717c24a7927226ec139. The source catalog has 81 declared Mcp/transaction entity candidates and 852 declared properties including navigations and nonmapped members. Framework inheritance adds 7 authentication types. The metadata inspections account for 88 entity types, 1,079 mapped scalar properties, 276 convention-added shadow properties and 126 model foreign keys. These denominators describe different inventories and must not be interchanged.

The metadata inspection uses the already-restored EF InMemory provider only, never opens a database, executes SQL, creates schema or calls SaveChanges. Relational table names, SQL types, indexes/constraints enforcement and provider-specific differences remain unverified. Initial attempts to load relational providers in the inspection host failed; no dependency or product fix was made to conceal that limitation. InMemory model evidence is useful for finding convention omissions and is not provider-feasibility acceptance.

`proposed-storage-classification.json` records an explicit proposed policy for every inventoried entity and scalar property, each of the 126 model relationships, 214 syntax writer-call candidates and 64 assignment candidates. No default entity classification is permitted. The proposal retains all non-versioned data and separates authored content from automatic state. It includes conditional Workspace-memory and parent-version scope, operational co-retrieved edge weights, derived embedding status, operational TODO file-projection markers, explicit complete session-snapshot promotion, and portable workspace prompts versus local machine and protected policy values. The current active-layer selector remains protected hub-owned policy; versioned requirement scope definitions do not grant nodes authority to change that selector.

The proposed standalone context remains the existing full model with immutable historical migrations. The Dolt-only project context contains approved content projections and immutable ProjectId, not host-path authority. A separate unversioned control context owns control/audit/enrollment/receipt/allocator state with additive provider migrations. Existing authentication and QuadBrain security databases keep their operational boundaries. Cross-store references use stable ids and application validators rather than database foreign keys or unsupported cross-database LINQ joins. Legacy standalone FKs remain intact.

The writer proposal states required future routing; it does not claim that 214 call sites already route correctly. Required closure work includes every enclosing call flow and indirect/raw-SQL writer, actual relational model comparison, IdentityServer configuration/grant storage, generated identity grammar and provider-specific constraints. B2/B3 remain proposed and unaccepted, with no Planning-to-Ready transition or implementation authorization from these inventories.

## Indirect writers and B4 proposal checkpoint

`writer-identity-boundaries.json` adds five source-traced flows with 25 source hashes, all 87 Mcp primary-key components and the 42 generated numeric keys. Triage reaches the selected transaction-gated TODO creator; handoff persists an operational intent then calls the selected TODO service. Consolidation has a writable job entry point with dry-run default, which is not evidence that an unattended scheduler is registered. Startup YAML and legacy SQLite importers directly create content. Enrolled-project automatic work retains operational proposals/plans and cannot acquire an editing lease or dirty a branch; explicit application enters the owning project's fence/write gate. Existing standalone behavior remains intact. These are proposed routing changes, not claims about current runtime compliance.

The identity/isolation mock seam now exercises eight operational/proposal kinds and independent lease readbacks as well as heads, working-set fingerprints and durable rows. The focused Windows receipt under `20261117-phase0-writer-closure` has 270 passing cases (4 positive, 266 expected-negative controls), zero failures and zero skips. This replaces the older 136-case Windows source-version evidence for the changed seam; the old receipts remain historical. LAB-OMARCHY repeated the same 270 cases at f524f54ff935048991e17bec758f3be1ce8c7c19 with zero failures/skips and matching LF-normalized bytes for all sixteen mock files; exact Linux TRX/log and structured evidence are retained in the same receipt folder. The count does not establish full B2-B4 coverage.

[The B4 identity proposal](2026-11-17-federation-identity-design.md) specifies per-family tagged canonical strings, durable allocation, aliases, numeric public-contract compatibility, composite-key treatment and sort/display rules. It remains unaccepted; shared API/capability and exhausted/unsafe allocator-error dispositions and the full parser/plugin consumer closure remain open. No current IDs, DTOs or requirement definitions were changed.

## Identity lifecycle assertions (2026-11-17 partial mock gate)

The reusable `Phase0IdentityLifecycleContract` adds FR-MCP-182 / TR-MCP-FEDERATION-009 / TEST-MCP-230 coverage for `ac-BranchRevertDoesNotReuseSequence`, `ac-SimultaneousHumanIdReservation`, and `ac-OfflineHumanIdPublicationDenied`. Independent scripts allow either reservation winner and valid allocation gaps, hold both initial reservation replies until both requests arrive, and separately expose durable rows, allocator state, local content and remote head. The canonical protocol specifies HTTP200 for an exact winning retry; the loser response is constrained by HTTP409 `human_id_reserved`, without inventing a reservation-body requirement.

Windows Nuke TestGroups run `20261117T195618-fedph0-identity-lifecycle-mock` passed137 cases (5 positive,132 expected negative), with0 failed and0 skipped. Exact original-preserving TRX/Nuke log copies, source hashes and fresh canonical criteria are in `docs/receipts/federation/20261117-phase0-identity-lifecycle/`. Linux parity has not yet run for these new assertions. This is partial mock evidence only; no real allocator, hub, publication or provider binding is established. Collision/reference migration, duplicate review, all-plugin diagnostics, full B4 consumer closure and the rest of B1-B12 remain pending. Every task remains open, implementation barrier Closed and DEPLOY HOLD active.
