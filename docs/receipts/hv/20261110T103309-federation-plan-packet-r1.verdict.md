**Outcome: DISAGREE. Accuracy 86, completeness 78.** The plan is well structured and honest about what is unavailable. It still leaves several product and architecture decisions open, and some claims conflict with the supplied source.

The largest gaps:
1. Activation can happen before read gating exists.
2. The identity and credential model is undecided, and F2 depends on it.
3. Envelope and version checks are not scoped to a workspace or a proxy identity.
4. Recovery after a crash between the database rekey and the token rewrite is undefined.
5. The Nuke unit gate does not enforce zero skips.

A fully compliant final acceptance is impossible in this round regardless of scores, because of the audit limitations listed at the end.

Reviewer identity: Claude, model claude-opus-5-5. Effort level is not visible to me. Reviewed from the evidence packet only. No tools, MCP turn, memories, requirement store or TRX were available.

---

**Progress 10%, ETA: 4 of 4 surfaces remaining.** Surface 1: claims.

- **Supported claims:**
  - Build.Tests is excluded by `build/Build.Test.cs`.
  - TestGroups rejects empty, failed or skipped selections, per `Build.TestGroups.cs` CheckTrx and CheckClasses.
  - `EnsureFederatedWorkspaceRow` hard-codes TodoPath.
  - The federation reference names an obsolete hub hostname (PAYTON-DESKTOP).
  - Breaking migration status is consistent with D5 to D7.
- **Unsupported claim:** "P1.1-P1.4 delivered." The original plan section 4a records P1.1 to P1.3 at commit 8ca38d5d and P1.R delivered. P1.4 has only "As built" notes, with no commit, receipt or merge evidence.
- **Unsupported claim:** the current develop SHA, the worktrees, the PR83 state and the live TODO chain are all unverifiable here. The plan correctly labels them as prior reads.
- **Inaccurate claim (plan section 7, F2b):** "validate token owner." The supplied `FederationOptions.HubAccessToken` is one stable shared inter-server credential, so no per-proxy owner exists to validate.
- **Incomplete claim (plan section 2):** the plan says Test fails only because of Build.Tests. `Build.Test.cs` also never inspects TRX, so skipped cases pass on exit code 0.

**Progress 35%, ETA: 3 surfaces remaining.** Surface 2: workspace rules and BDPv4.

- **Passing rules:**
  - No tables and no em or en dashes in the plan.
  - PowerShell and Nuke only.
  - Mock-first cycle with negative controls before production edits.
  - Same assertions in Real bindings.
  - Independent different-family HV at phase exits.
  - No fabricated results.
  - Receipts and redaction.
- **Gaps:**
  - The zero-skip gate has no enforcement point for the unit lane.
  - TestGroups ignores UnitTestFilter, so it runs quarantined and AiReview cases.
  - Whether the current base passes is unknown. The historical baseline had 26 failures, including provider fixtures.
  - P1.R evidence shows 69 MockData cases against 67 Real cases, which may violate the "same assertions" rule.

**Progress 60%, ETA: 2 surfaces remaining.** Surface 3: requirements and AC traceability.

- FR-WSSYNC AC1 still says "55/55", which conflicts with plan F3c.
- FR-WSSYNC AC3 requires a drift surface on GET federation/workspaces. The plan never names it.
- FR-BACKFILL AC4, the per-workspace and per-domain status, is not mapped to an increment.
- TR-ROUTE "token valid only for that proxy's workspaces" cannot be met under the shared credential.
- The identity ACs are labels pending registration, which the plan correctly says.

**Progress 80%, ETA: 1 surface remaining.** Surface 4: whole plan and dependencies. I attacked crash and restart, replay, concurrency, identity isolation, schema upgrade, null and omitted fields, activation before gating, and rollback after writes. Findings below.

**Progress 100%.**

---

## Findings, ranked

**R-01, High, operator decision. Activation before read gating.**
- Source: plan section 9 steps 4 and 5 make isolation conditional. `FederationMiddleware.InvokeAsync` proxies every non-local product route once the feature is enabled and a target resolves. No traffic-isolation mechanism is in evidence.
- Edit: replace section 9 steps 4 and 5 with: "D1 rekeys and deploys with federation disabled on both hosts. Hub activation and spoke enrollment occur only in D2, after F3 and F4 are accepted, using the backed-up hub-first sequence."

**R-02, High, operator decision. D1 changed without an explicit amendment.**
- Source: original D1 says re-enroll once after the P1/P2 deploy. The new plan adds the F2I prerequisite and controlled activation.
- Edit: add to section 1: "Proposed amendment D1-A (requires operator approval): re-enrollment occurs once, after F1, F2I, F2, F3 and F4 acceptance, at D2. The intent of D1 is unchanged: identifiers change once, before any backfill write."

**R-03, High, source-resolvable. Credential ordering.**
- Source: `FederationOptions.HubAccessToken` is a single shared credential. `FederationEnvelopeSigner.ResolveSigningKey` uses a shared secret or falls back to the EnrollmentToken. Plan section 3 orders F2 before F2I.
- Edit: in section 3, change the execution order to "F1, F2I, F2". Add to F2b: "Token-owner validation and forged X-Mcp-Proxy-Id rejection bind to the F2I node credential."

**R-04, High, operator decision. Identity contract is predictably unmet.**
- Source: `IdentityServerConfig` uses static in-memory clients, one hard-coded `mcp-agent` secret, no node or owner claim, self-contained JWTs with a 1 hour lifetime, and ReUse sliding refresh. `IdentityServerExtensions` supports SQL Server only and falls back to LocalDB, which is unavailable on the Linux hub. Enabled defaults to false. A default admin password is seeded.
- Edit: add "Locked identity decisions" to section 8:
  - Per-node credential type and how it is provisioned.
  - Immediate revocation enforced by a hub-side node status check on every request.
  - Where the owning user subject is stored, as a schema addition requiring registration.
  - An explicit SQL Server connection for the identity store, with LocalDB fallback refused on Linux.
  - The seeded default admin password changed and seeding disabled before activation.
  - The fate of HubAccessToken and of the EnrollmentToken HMAC fallback.

**R-05, High, operator decision plus source-resolvable. Envelope identity isolation.**
- Source: `FederationEnvelopeSigner.Verify` does not bind SourceProxyId or Operation.ProxyId to the caller, does not track nonces, does not validate ApplyMode, and uses a shared key.
- Edit to F4b: "The hub rejects an envelope when SourceProxyId or Operation.ProxyId differs from the authenticated node, when GlobalWorkspaceId is not registered to that proxy, or when ApplyMode is not allowed on that endpoint. Backfill mode does not bypass conflict detection. Envelopes are signed at send time. Clock skew between hosts is part of the D1 inventory."
- Decision needed: per-node signing keys, rotated on revocation (recommended), or the shared key accepted in writing as residual risk.

**R-06, High, source-resolvable. Version and echo lookups are not workspace-scoped.**
- Source: `FederationTopologyService.ResolveHubVersionAsync(domain, resourceId)`, `IFederationStateAdapter.GetVersionAsync(resourceId)`, and the IsEcho path in `FederationOperationApplyService`.
- Edit to F4a: "Version lookup, echo detection, idempotency key and conflict detection are keyed by (canonical workspace, domain, resource id). A mandatory negative case uses the same TODO id in two workspaces. The adapter contract change is registered under TR-MCP-FED-BACKFILL-001."

**R-07, High, operator decision. D4 read-your-writes and outage behavior after switching.**
- Source: original D4. `AddFanoutRowsAsync` excludes the source proxy, so a forwarded write never reaches local reads. Plan F4a says "Test hub outage" with no expected result.
- Recommended lock: "Local reads during the incomplete interval carry X-Mcp-Stale-Read and X-Mcp-Stale-Read-Status naming the backfill state. A forwarded write is not visible in local reads. After switching, a hub outage serves local reads with X-Mcp-Stale-Read: true."
- The alternative is to fail closed with 503. The operator must choose.

**R-08, High, operator decision. Pre-existing queue and outbox rows replay on activation.**
- Source: `ListPendingOperationsAsync` and `GetSyncItemsAsync`. Plan section 9 step 1 only inventories them.
- Edit to the activation step: "Before either role is enabled, export proxy FederationOperations rows in queued, replay_failed or replay_blocked status, and unacknowledged hub FederationOutbox rows. Activation is blocked until the operator approves a disposition for each row. No row is deleted."

**R-09, High, source-resolvable. Crash between the database commit and the token rewrite.**
- Source: original section 4c.3 step 5 and the D5 guard, which checks Workspaces.WorkspaceId only. Plan F1d names the test but not the mechanism.
- Edit to F1d: "The guard refuses startup if any converted column, or any token-store key, holds a Windows-shaped value containing a backslash. Each column check is an EXISTS query, and its startup cost is measured in F1e. The token rewrite writes a temporary file, flushes it, then atomically replaces the original after the backup. A rerun applies f(x) idempotently to both stores."

**R-10, High, UNKNOWN plus operator decision. Deployment hazard from shared develop.**
- Source: the P1.4a "As built" notes say the EF converter canonicalizes predicates, so backslash rows become invisible. Plan section 6 merges F1 to develop.
- Edit to F0: "Record each host's deployed commit. If a P1.4 build is running against an unconverted store, stop and report an incident."
- Edit to section 3: "From the F1 merge until D1 completes, no workstream deploys develop to either host. The operator records this freeze, or F1 merges to a federation integration branch instead."

**R-11, High, source-resolvable. Registration hijack.**
- Source: `UpsertWorkspaceAsync` trusts `request.GlobalWorkspaceId` and rewrites GlobalWorkspaceId and CanonicalWorkspaceId on every upsert.
- Edit: add F2b case `FederationWorkspaceRegistrationOwnership`: "Enroll, heartbeat and register reject a GlobalWorkspaceId owned by another proxy, and reject any id change for an existing (ProxyId, canonical path). Negative cases include hex case variants on every provider."

**R-12, High, source-resolvable. Default-workspace fallback without a hop header.**
- Source: original section 1.1 (no header returned the hub primary TODOs) and WorkspaceResolutionMiddleware Tier 2.
- Edit to F2b: "Requests authenticated with the inter-server or node credential fail closed without a resolvable workspace, whether or not X-Mcp-Federation-Hop is present."

**R-13, High, source-resolvable. Zero-skip and quarantine gate holes.**
- Source: `Build.Test.cs` has no TRX check. `Build.TestGroups.cs` runs classes "whatever their category" without UnitTestFilter.
- Edit to section 12: "Unit TRX is verified to be nonempty, with zero failed, zero skipped and zero not-executed cases per project. F0 adds this as a registered TR-MCP-NUKE-019 increment, recommended to include a Build.Tests route that applies UnitTestFilter. The gate stays blocked until then."

**R-14, Medium-High, source-resolvable. Current baseline and quarantine.**
- Edit to F0: "Run the full gate at the F1 base on both hosts. Any failure, skip or not-executed case blocks F1, and the operator decides who fixes it. List every quarantined case. Any quarantined case that traces to a federation, PATHNORM, identity or gate AC is un-quarantined, or its AC is counted as uncovered."

**R-15, Medium-High, source-resolvable. Rekey exclusivity.**
- Edit to F1c: "The command refuses to run unless the instance's service is verified stopped and provider-exclusive access is held for the whole run. A concurrent second invocation fails with no writes. Tests cover a concurrent writer and a concurrent second rekey."

**R-16, Medium-High, source-resolvable. Proxy source of GlobalWorkspaceId.**
- Source: `FederationEnrollmentResponse` carries no workspace ids. Original section 4c.1 shows queued operations store raw paths.
- Edit to F2b: "Enrollment and heartbeat responses return accepted workspace mappings, as additive registered fields, and the proxy persists them. The hub accepts either a registered id or a (ProxyId, path) pair that resolves to exactly one row. Anything else fails closed."

**R-17, Medium-High, source-resolvable. F3 null, omitted and ordering semantics are deferred.**
- Source: plan F3b says "Define out-of-order ... before implementation." The registration DTO uses nullable strings plus legacy MetadataJson.
- Edit: lock these rules:
  - A schema version field (proposed, requires registration).
  - Version 1 payloads read legacy MetadataJson and leave other fields unchanged.
  - Version 2 payloads send every proxy-owned field. Null clears the field to its default. An omitted property returns 400.
  - A proxy-monotonic ConfigurationRevision is applied only when greater than the stored value. An equal value is idempotent.
  - A unique-key violation from a concurrent upsert is retried once after re-reading.

**R-18, Medium, source-resolvable plus evidence. Hub use of proxy paths.**
- Source: `PopulateContext` sets DataDirectory, TodoFilePath and derived paths. Copilot instructions mention a YAML TODO backend.
- Edit to F3a: "Federated rows get no host file paths, and file-backed services refuse them. Federated TODO routes require the database TODO backend."

**R-19, Medium, source-resolvable. AC mapping gaps.**
- Edits:
  - F0 lists the WSSYNC AC1 amendment that removes the 55/55 denominator.
  - F3b names drift fields on GET federation/workspaces for WSSYNC AC3.
  - F4a maps BACKFILL AC4 to federation/status fields.
  - Add the completion predicate: "The hub records an acknowledged final cursor for every approved domain, zero open conflicts and a passing parity check. The proxy switches reads only after reading hub-confirmed completion, and persists the switch."

**R-20, Medium, source-resolvable. Rekey outcomes left open.**
- Edits to F1a:
  - "Converging keys abort before any write, report every colliding set and exit nonzero."
  - "A federation id is derived only if it equals the hash of (ProxyId, legacy storage key computed from the pre-rekey WorkspacePath). Any other id is preserved and reported."

**R-21, Medium, evidence needed. Migration and guard ordering.**
- Edit to F1d: "The rekey command runs against the enumerated pre-migration schemas. The new binary runs the guard before applying any migration."
- F3 and F4 schema additions get provider migrations for SQLite, SQL Server and PostgreSQL, run through MigrationIntegrationTests, with a D2 backup taken before migrating.

**R-22, Medium, operator decision. Rollback after writes.**
- Edit to section 9: "After the first write to a rekeyed store, no binary downgrade is allowed, because D7 forbids a fallback. Recovery is a forward fix with federation disabled. Restoring a pre-rekey backup needs operator approval that names the writes to be lost."

**R-23, Medium, source-resolvable. Parity and graph rules.**
- Edits to F4e and F4c:
  - Cutoff equals LocalProxy activation.
  - Hub records with a newer version are reported as superseded, not as mismatches.
  - A missing parent defers the record: it stays unacknowledged, the cursor does not advance, and the workspace stays incomplete.
  - Domain order is parents before children.

**R-24, Medium, evidence needed. D2 queueDepth reachability.**
- Source: `GetQueueStatusAsync` counts "accepted" rows. With a single proxy, no outbox rows are created.
- Edit to D2 step 4: "Capture status per host. F4 proves which transition moves an accepted operation forward. If no such transition exists, amend the AC rather than waive it."

**R-25, Medium, source-resolvable. Backup scope.**
- Edit to section 9 step 2: include the identity store database. Configuration backups hold secrets and are stored with restricted access.

**R-26, Medium, source-resolvable. P1.R case-count mismatch.**
- Edit to F1: "Reconcile the 69 MockData cases against the 67 Real cases. Each mock case gets a Real binding, or is documented as a mock-only negative control."

**R-27, Low. Ancestry failure path.**
- Edit to section 2: "If P1.1 to P1.4 are not ancestors of develop, F1 carries them with their retained receipts and reruns all gates."

**R-28, Low. Host and tooling wording.**
- Edits:
  - Section 4 step 6 names the host for increment exit.
  - Section 12 says "pwsh 7".
  - Section 12 notes the interactive precondition for the Windows lane.
  - Section 12 defines the "PowerShell.MCP route."

**R-29, Low. Carry-forward items.**
- Carry forward the FR-MCP-082/083/085 comment fix.
- Update the hub default in `docs/context/federation.md`.
- Require XML docs on test classes that cite AC IDs.

**R-30, Low, UNKNOWN. Undefined terms.**
- "add-profile" and the exclusion of MEMORY-PROCESS-007 have no source in this packet.

## Ranked path to 98/98

1. **Operator decisions, one batch:** R-01, R-02, R-04, R-05 (key model), R-07, R-08, R-10, R-13 (gate route), R-22.
2. **Apply the source-resolvable edits:** R-03, R-06, R-09, R-11, R-12, R-14 to R-20, R-23, R-25 to R-29.
3. **Supply the evidence requests below,** then resolve R-18, R-21, R-24 and R-30.
4. **Round 2** with a diff against this round.

=== VERDICT JSON ===
```json
{
  "OverallVerdict": "DISAGREE",
  "accuracy": 86,
  "completeness": 78,
  "PASS": ["no tables or em/en dashes in plan", "PowerShell and Nuke only", "BDPv4 mock-first cycle with negative controls defined", "same assertions in Real bindings", "independent different-family HV at phase exits", "breaking migration declared", "D3 D5 D6 D7 retained", "PR83 donor handling", "hostnames as configuration", "no fabricated store, test or approval claims", "done definition excludes merge-only or enrollment-only completion"],
  "FAIL": ["R-01","R-02","R-03","R-04","R-05","R-06","R-07","R-08","R-09","R-11","R-12","R-13","R-14","R-15","R-16","R-17","R-19","R-20","R-22","R-23","R-25","R-26","R-27","R-28","R-29"],
  "UNKNOWN": ["R-10","R-18","R-21","R-24","R-30","P1.4 delivered claim","current base gate state","PR83 current findings"],
  "findings": [
    {"id":"R-01","severity":"High","source":"plan s9 steps 4-5; FederationMiddleware.InvokeAsync","exact_edit":"Defer hub activation and spoke enrollment to D2 after F3/F4 acceptance; D1 runs with federation disabled."},
    {"id":"R-02","severity":"High","source":"original plan D1; plan s1 s5 s9","exact_edit":"Add 'Proposed amendment D1-A' to s1: re-enroll once at D2 after F1, F2I, F2, F3, F4; D1 intent unchanged."},
    {"id":"R-03","severity":"High","source":"FederationOptions.HubAccessToken; FederationEnvelopeSigner.ResolveSigningKey; plan s3, s7 F2b","exact_edit":"Execution order F1, F2I, F2; F2b token-owner and forged-header checks bind to the F2I node credential."},
    {"id":"R-04","severity":"High","source":"IdentityServerConfig.GetClients; IdentityServerExtensions.ResolveIdentityConnectionString; IdentityServerOptions","exact_edit":"Add 'Locked identity decisions' to s8: per-node credential, revocation via hub node-status check on every request, owner subject storage, explicit SQL Server identity store with LocalDB refused on Linux, default admin rotated and seeding disabled, fate of HubAccessToken and EnrollmentToken HMAC fallback."},
    {"id":"R-05","severity":"High","source":"FederationEnvelopeSigner.Verify","exact_edit":"F4b: reject an envelope whose SourceProxyId or Operation.ProxyId differs from the authenticated node, whose workspace is not owned by that proxy, or whose ApplyMode is not allowed on the endpoint; sign at send time; operator selects per-node keys (recommended) or accepts the shared key as residual risk."},
    {"id":"R-06","severity":"High","source":"FederationTopologyService.ResolveHubVersionAsync; IFederationStateAdapter.GetVersionAsync; FederationOperationApplyService IsEcho path","exact_edit":"F4a: key version, echo, idempotency and conflict checks by (workspace, domain, resource id); add a same-TODO-id-in-two-workspaces negative case; register the adapter TR change."},
    {"id":"R-07","severity":"High","source":"original D4; FederationTopologyService.AddFanoutRowsAsync excludes the source proxy; plan F4a","exact_edit":"Lock incomplete-interval local reads with X-Mcp-Stale-Read headers and documented non-visibility of forwarded writes; lock post-switch hub-outage behavior (operator choice: stale local read or 503)."},
    {"id":"R-08","severity":"High","source":"FederationTopologyService.ListPendingOperationsAsync, GetSyncItemsAsync; plan s9 step 1","exact_edit":"Export pre-existing queued, replay_failed and replay_blocked proxy operations and unacknowledged hub outbox rows; block activation until the operator approves a disposition per row; delete nothing."},
    {"id":"R-09","severity":"High","source":"original s4c.3 step 5 and D5; plan F1d","exact_edit":"Guard checks every converted column and every token-store key for backslash Windows-shaped values; token rewrite via temp file and atomic replace after backup; idempotent rerun on both stores."},
    {"id":"R-10","severity":"High","source":"original P1.4a As built (EF converter); plan s6 F1 merge","exact_edit":"F0 records each host's deployed commit and raises an incident if a P1.4 build runs against an unconverted store; freeze develop deploys from F1 merge until D1, or merge F1 to a federation integration branch."},
    {"id":"R-11","severity":"High","source":"FederationTopologyService.UpsertWorkspaceAsync","exact_edit":"Add F2b case FederationWorkspaceRegistrationOwnership: reject a GlobalWorkspaceId owned by another proxy and any id change for an existing (ProxyId, path); include hex case variants on every provider."},
    {"id":"R-12","severity":"High","source":"original s1.1 probe; WorkspaceResolutionMiddleware Tier 2","exact_edit":"F2b: inter-server or node-credential requests fail closed without a resolvable workspace, with or without the hop header."},
    {"id":"R-13","severity":"High","source":"build/Build.Test.cs (no TRX check); build/Build.TestGroups.cs (runs all categories, no UnitTestFilter)","exact_edit":"s12: verify unit TRX has zero failed, skipped and not-executed cases; F0 adds a registered TR-MCP-NUKE-019 increment including a Build.Tests route that applies UnitTestFilter; gate blocked until then."},
    {"id":"R-14","severity":"Medium","source":"original s2a 26-test baseline; Build.Test.cs quarantine filter","exact_edit":"F0 runs the full gate at the F1 base on both hosts; any failure blocks F1; un-quarantine any case tracing to a federation-related AC, or count that AC as uncovered."},
    {"id":"R-15","severity":"Medium","source":"plan s9 step 2; F1c","exact_edit":"Rekey requires the service verified stopped and provider-exclusive access for the run; a concurrent invocation fails with no writes; add concurrent-writer and concurrent-rekey tests."},
    {"id":"R-16","severity":"Medium","source":"FederationEnrollmentResponse; FederationMiddleware passes WorkspacePath; original s4c.1","exact_edit":"Hub returns workspace mappings in enrollment and heartbeat responses (additive, registered); proxy persists them; hub accepts a registered id or a uniquely resolvable (ProxyId, path); anything else fails closed."},
    {"id":"R-17","severity":"Medium","source":"plan F3a/F3b; FederationWorkspaceRegistrationRequest","exact_edit":"Lock payload schema versions (v1 legacy MetadataJson; v2 full fields, null clears, omitted returns 400), a monotonic ConfigurationRevision, and a single re-read retry on unique-key violation."},
    {"id":"R-18","severity":"Medium","source":"WorkspaceResolutionMiddleware.PopulateContext; copilot-instructions TODO backend","exact_edit":"F3a: federated rows get no host file paths; file-backed services refuse them; federated TODO routes require the database backend."},
    {"id":"R-19","severity":"Medium","source":"FR-WSSYNC AC1 and AC3; FR-BACKFILL AC4","exact_edit":"Amend AC1 denominator; name drift fields on GET federation/workspaces; map AC4 status fields; lock the hub-owned completion predicate and the proxy's persisted switch."},
    {"id":"R-20","severity":"Medium","source":"plan F1a","exact_edit":"Converging keys abort before any write and report every collision; a federation id is derived only if it equals hash(ProxyId, legacy storage key), otherwise preserved and reported."},
    {"id":"R-21","severity":"Medium","source":"plan F1d; Program and migration coordinator not supplied","exact_edit":"Rekey runs against enumerated pre-migration schemas; the guard runs before migrations; F3/F4 schema changes get migrations on all three providers plus a D2 backup."},
    {"id":"R-22","severity":"Medium","source":"plan s9 rollback; D7","exact_edit":"No binary downgrade after the first post-rekey write; recovery is a forward fix with federation disabled; restoring a backup requires operator approval naming the lost writes."},
    {"id":"R-23","severity":"Medium","source":"plan F4c, F4e","exact_edit":"Cutoff equals LocalProxy activation; newer hub versions reported as superseded; a missing parent defers the record without advancing the cursor; parents before children."},
    {"id":"R-24","severity":"Medium","source":"FederationTopologyService.GetQueueStatusAsync and AddFanoutRowsAsync","exact_edit":"Capture status per host; F4 proves the transition out of 'accepted', otherwise amend the AC rather than waive it."},
    {"id":"R-25","severity":"Medium","source":"plan s9 step 2","exact_edit":"Back up the identity store database; keep configuration backups that contain secrets in restricted storage."},
    {"id":"R-26","severity":"Medium","source":"original s4b P1.R status (69 MockData vs 67 Real)","exact_edit":"F1 reconciles mock-only cases: add Real bindings, or document each as a negative control."},
    {"id":"R-27","severity":"Low","source":"plan s2","exact_edit":"If P1.1 to P1.4 are not ancestors of develop, F1 carries them with retained receipts and reruns all gates."},
    {"id":"R-28","severity":"Low","source":"plan s4 step 6, s12; AGENTS.md rule 7; Build.TestDependencies IntegrationLanePrecondition","exact_edit":"Name the host for increment exit; say pwsh 7; note the Windows interactive lane precondition; define the PowerShell.MCP route."},
    {"id":"R-29","severity":"Low","source":"original s5; docs/context/federation.md; copilot-instructions","exact_edit":"Carry forward the FR comment fix; update the federation.md hub default; require test XML docs that cite AC IDs."},
    {"id":"R-30","severity":"Low","source":"plan s5, s13","exact_edit":"Cite sources for 'add-profile' and the MEMORY-PROCESS-007 exclusion, or remove them."}
  ],
  "evidenceRequests": ["file defining FederationRegistry (ResolveTarget behavior for LocalProxy and Hub)", "file defining FederationProxyService", "federation controller handling operations, envelopes and ack", "proxy replay, heartbeat and enrollment background services", "WorkspaceService.GetAsync implementation", "Program.cs startup and migration coordinator", "build files defining TestQuarantine, TestProjectRoles, IntegrationLaneExecution.CheckTrx", "solution test project list", "docs/Development-Process-draft-v4.md", "MCP-FEDIDENTITY-001 and existing Identity Server requirements", "FederationWorkspaceEntity and FederationProxyEntity", "staleReadStatus producer", "memory and todo adapter GetVersionAsync implementations", "FileGitHubWorkspaceTokenStore", "PR83 current findings", "P1.4 commit and receipts plus docs/receipts/federation-fixes-20261007/*", "deployed service versions on both hosts"],
  "auditLimitations": ["no tool access", "no MCP audit turn created", "live Global and Workspace memories not loaded", "requirement and TODO store not read", "no TRX available", "commit 68c2d900, worktrees and PR83 not verified", "this verdict not persisted per plan s13", "reviewer effort not recorded", "commit-sync pending", "these limitations prevent a fully compliant final acceptance until resolved"]
}
```
