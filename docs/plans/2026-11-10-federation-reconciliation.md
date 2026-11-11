# Federation current-scope coverage

This index covers the new Dolt federation release. The architecture, phase plan and U01-U15 workflows define scope. Earlier plans were consulted to recover applicable requirements; their original text and unrelated requirements are not part of this plan.

## Release contracts and acceptance criteria

FR-MCP-174 through 189 map in order to TR-MCP-FEDERATION-001 through 016 and TEST-MCP-222 through 237. The workflow inventory names each contract, its phase, workflows and acceptance tests. Together these define 99 current Given/When/Then cases. The [current contract and acceptance specification](2026-11-10-federation-current-contracts.md) includes the full schemas, endpoint/error contracts and Given/When/Then criteria once in this packet, with canonical FR/TR/TEST IDs. Applicable gap-check criteria and the migration fixture matrix are included there too. All criteria remain pending implementation evidence.

U01-U15 cover enrollment, local/hub commits, concurrent work, conflicts, disconnect/reconnect, drafts, branches, retries, migration, schema upgrades, multiple agents, audit delivery, duplicate identity and hub restore. Their preconditions, steps, success outcomes and exception paths are in the workflow document. Ph0 registers these workflows in the UC store and binds their requirement links; no pre-existing federation UC was found in either queried workspace.

## Applicable requirements recovered by the gap check

- **Autonomous execution and compatibility:** FR-MCP-103, TR-MCP-FED-001 and TEST-MCP-136 now require local execution with committed-history exchange. FR-MCP-077 preserves existing explicitly selected DirectProxy behavior as a regression boundary. Ph2/Ph5; FR-MCP-174/178/185.
- **Permanent audit reaches the hub:** FR-MCP-AUDIT-001, TR-MCP-AUDIT-002/003/004 and TEST-MCP-AUDIT-001/002/003 cover append-only storage, protected runtime permissions, secret exclusion, delivery receipts and migration backfill. Ph2/Ph3/Ph5; FR-MCP-184/187.
- **Existing identity platform:** MCP-FEDIDENTITY-001 supplies user management, single-user node ownership and integrated IdentityServer authentication. Per-node credentials, project scopes and revocation are in FR-MCP-185. Ph2/Ph5/Ph7.
- **Complete migration:** FR-MCP-FED-BACKFILL-001, TR-MCP-FED-BACKFILL-001 and TEST-MCP-FED-BACKFILL-001 require domain inventory, child/link completeness, counts, hashes, idempotent resume and audit verification. Ph3 seed/recovery export and Ph6 reviewed join; FR-MCP-187.
- **Project routing and path identity:** FR-MCP-FED-ROUTE-001, TR-MCP-FED-ROUTE-001, TEST-MCP-FED-ROUTE-001, TR-MCP-FED-PATH-001, TEST-MCP-FED-PATH-001, TR-MCP-PATHNORM-001 and TEST-MCP-PATHNORM-001 retain normalized local paths, project isolation, foreign-path rejection and explicit backed-up source rekey where required. Ph0/Ph2/Ph3/Ph5; FR-MCP-176/182/185.
- **Configuration boundaries:** FR-MCP-FED-WSSYNC-001, TR-MCP-FED-WSSYNC-001 and TEST-MCP-FED-WSSYNC-001 preserve portable configuration, local host settings and protected hub-owned requirement-layer policy with drift reporting. Ph2/Ph5; FR-MCP-184.
- **Memory preservation:** FR-MCP-MEMORY-008, TR-MCP-FED-MEMORY-001 and TEST-MCP-MEMORY-FED-001 preserve memory IDs, versions, ownership, scope and deletion semantics in versioned project history. Ph2/Ph3/Ph6; FR-MCP-184/187.
- **Session graph and sanitization:** TR-MCP-FED-SESSIONLOG-001, TEST-MCP-SESSIONLOG-FED-001 and the applicable SESSIONLOGCTX/SESSIONLOGSAN contracts preserve complete captured graphs, PlanFile/TodoId and sanitizer parity. Automatic audit uses unversioned delivery; explicit promoted snapshots may be versioned. Ph2/Ph3/Ph5; FR-MCP-184/187.
- **No external-effect replay:** FR-MCP-125, FR-MCP-173 and TR-MCP-TXNCOMPAT-001 preserve separation of general federation from QuadBrain-only transaction gating. Import never replays business effects. Ph2/Ph6; FR-MCP-188.
- **Execution gates:** current and prior unit coverage, MigrationIntegrationTests, Build.Tests, provider integration and ValidateTraceability run through Nuke on both hosts at phase exit, with zero failures and zero skips. Ph0-Ph8.

## Scope boundary

The new design replaces hub-authoritative ordinary reads/writes, LocalProxy business-command forwarding/replay, post-backfill read switching and general-federation keyserver gating. Those mechanisms are not work items for this release. Required compatibility and source-data migration are limited to the explicit contracts above.

Pull requests, automatic hub election, arbitrary file/source-code synchronization, live cross-provider CDC, unrelated worktree cleanup and unrelated historical requirements are outside this release. Old examples, review transcripts, host-by-host copies and superseded AC wording remain audit evidence only; they do not expand scope or create phase-exit obligations. The canonical current criteria and this plan govern acceptance.