# Federation completion: BDPv4 execution plan

Date: 2026-11-10. Author: Codex. Status: historical remediation draft, superseded for versioned federation by [the new Dolt BDPv4 plan](2026-11-10-federation-dolt-bdpv4.md) and [architecture](2026-11-10-federation-dolt-architecture.md). Retained for provenance; not implementation authority.

## 0. Operator clarification and implementation discrepancy

On 2026-11-10 the operator clarified the intended behavior: a local node does the same work it would as the hub, remains operational when disconnected, publishes its updates to the hub and receives hub updates. This is the planning authority. It is not evidence that the operator newly requested behavior absent from the original intent.

The current implementation differs: `FederationRegistry.ResolveTarget` selects the hub for LocalProxy; `FederationMiddleware.InvokeAsync` calls ProxyAsync instead of the local pipeline for routed requests; `FederationProxyService.TryQueueFailedWriteAsync` stores a pending operation and returns 202, without applying the domain mutation locally. `FederationFanoutSyncService` does apply hub updates locally. Thus some replication machinery exists, but the ordinary request path is forwarding-first. The October plan D4 explicitly forwards writes and switches reads to the hub after backfill.

Sections 1-16 below are the prior remediation draft retained for reconciliation, not authorization to implement its conflicting forwarding/read-switch behavior. Their forwarding-first D4 clarification, F2 request forwarding, F4 read switch and hub-outage read failure proposal are superseded by the operator's local-processing intent. Canonical requirements, phase contracts and acceptance cases need reconciliation before renewed implementation-ready HV. Do not silently classify this discrepancy as an operator scope change.

Target outcomes: shared local business behavior on hub and nodes; local durable results available to local reads during hub/network outage; durable publication after local commit; receiving authorized hub updates without duplicate side effects or endless echo; recovery and convergence after reconnection. Library selection must prove these outcomes. The operator selected Git-style merging with conflict review and included branches in the initial release (section 0.2). Offline identity expiry/revocation behavior remains an explicit decision, not permission for anonymous access. YARP is not required for this replication model. Reassess all replication candidates against branching and merge-history support before recommending adoption.

### 0.1 Independent operation and identifier reconciliation

The operator subsequently made independence mandatory: nodes absolutely must work independently of the hub, and identifier clashes are a primary design concern. Merely accepting requests into a queue for later execution does not satisfy this. A locally committed result must be available to subsequent local work and survive restart while the hub is unavailable. Loss of the only local storage device before replication is a separate durability concern; asynchronous replication cannot promise that unreplicated data survives that loss.

Observed schema at `68c2d900`: TODO keys are `(WorkspaceId, Id)` and requirement keys are `(WorkspaceId, Kind, Id)` in `McpDbContext.cs`; memory IDs are globally unique in `MemoryEntity.cs`. Current `CreateGlobalWorkspaceId` derives a key from proxy ID and local path. These facts require an explicit logical-workspace mapping and identifier migration review, not an assumption that existing human-readable IDs are globally collision-free or that identical repository paths identify one shared dataset.

Operator-proposed identity direction: each node receives a two-character tag, incorporated into locally generated IDs. This replaces the earlier UUID-based recommendation in this draft. No actual node tag assignments or database changes have been made.

Contract details to settle in the revised plan:

- A federation-unique, immutable two-character creator tag plus a durable local sequence separates new records from different nodes. Illustrative format: `FR-MCP-L2-001` and `FR-MCP-OM-001`; these are examples, not assigned tags. The allowed alphabet, tag placement for each ID family, and sequence namespace must be explicit. Uppercase ASCII letters and digits would provide 1,296 tags; this alphabet is a proposal.
- Allocate sequences atomically under concurrent writers and persist them with record creation through established database transaction/uniqueness facilities. Do not generate IDs by an unlocked max-plus-one query. Sequence values must not wrap at 999 merely because current examples use three digits. Define overflow and parser compatibility.
- Tags must be assigned uniquely before a node creates tagged records; subsequent ID generation does not contact the hub. Retire tags without reassigning them. A cloned or rolled-back writable node must not reuse its former tag/sequence range; recovery must prove a safe high-water mark or give the new writing instance a fresh tag. Avoid building a separate distributed sequence allocator.
- Replication and edits preserve the original record ID, including its creator tag. A receiving hub or node does not replace the tag with its own. Current IDs are both references and lookup keys, so update parsers, APIs, links, exports and foreign-key graphs together under an approved migration contract. Existing `RequirementsService.cs` contains an FR extraction regex that would not recognize the illustrated tagged form; compatibility is real work, not only a generator edit.
- Treat edits to the same replicated identity as version/content conflicts. Unique IDs cannot solve concurrent edits, delete-versus-update or uniqueness constraints in the domain. Do not use wall-clock last-write-wins as an implicit resolution policy.
- For preexisting rows, preserve origin, logical workspace, entity kind, original ID, histories and reference graphs in a reviewed mapping. A matching ID or matching text alone does not prove two rows are the same entity. Ambiguous matches require explicit reconciliation; imports must not merge or overwrite them silently.
- Choose a library based on demonstrated independent local commits, two-way catch-up, insert/insert collision handling, update/update and delete/update behavior, duplicate delivery, crash recovery, and reference preservation. Prefer its supported synchronization mechanisms over building parallel ones.

Proposed named acceptance cases for canonical requirement reconciliation: `NodeWorksWithHubUnavailable`, `LocalCommitSurvivesOfflineRestart`, `LocalReadSeesCommittedWrite`, `DistinctNodeTagsPreventCreateCollision`, `ConcurrentLocalAllocation`, `RestoreOrCloneCannotReuseIdentifiers`, `TaggedIdParserAndReferenceRoundTrip`, `ReplicatedIdentityRemainsStable`, `LegacyCollisionPreservesReferenceGraphs`, `ConcurrentEditPreservesConflictEvidence`, `DeleteUpdateConflict`, `ReconnectConvergesWithoutDuplicateEffects`, and `OfflineAuthorizationBoundary`. These are draft case labels, not registered requirement IDs or passing tests. No dependency or database change is authorized by this section.

The node-tag proposal addresses new-record collisions. It does not retroactively distinguish legacy rows or decide which concurrent edit wins. Resolve those independently using explicit migration decisions and the selected replication library's supported conflict mechanisms.

### 0.2 Initial release boundary: branches and reviewed merges, no pull requests

Operator decisions, 2026-11-10:

- Nodes must execute and persist ordinary workspace work independently of LAB-OMARCHY. The hub is the shared synchronization point, not a required participant in each local operation.
- Two-character node tags participate in ID generation as described in section 0.1.
- Concurrent edits use Git-style three-way merging. Conflicts must be surfaced at the receiving node and require review to resolve; neither side silently wins.
- Git-style branches ARE in scope for the initial release. A node can retain divergent work and later review and merge it. A node is an execution location; a branch is a line of history. Replication preserves record identity across branches.
- Pull requests are explicitly OUT OF SCOPE for the initial release. No PR entity, PR lifecycle, automated-review orchestration or forge integration belongs on its critical path. The initial conflict review/resolution surface must work without a PR mechanism. PRs remain a future possibility only; the operator's discussion of that possibility was not authorization to include them now.

Implementation choices still requiring evidence and approval:

1. Evaluate actual Git as the history, branch and merge engine before inventing equivalents. `git merge-file` is an available candidate for three-way text content merges; this is not by itself a database replication engine or Git's complete tree-merge behavior. Compare native Git and suitable maintained bindings against the intended record representation and supported hosts. No dependency adoption is implied.
2. Retain enough common-ancestor history to merge local and incoming versions. Missing ancestry must be surfaced rather than substituted with a guessed base. A conflict review retains base/local/incoming revisions and preserves the last valid local state until resolution. Repeated delivery must not create duplicate review items or duplicate side effects.
3. Candidate clean merges must pass schema, authorization, uniqueness and relationship checks before application. Textual merge success alone cannot prove a structured database graph valid. Concurrent delete/update and colliding independent creations require explicit handling.
4. A reviewed merge must identify the revisions it resolves; if either changes, revalidate before applying. Preserve and replicate the resulting history so another node can recognize the resolution. Concurrent resolutions must not silently overwrite one another. Unrelated work remains available while a conflict awaits review.
5. Define branch creation, selection, local reads/writes, synchronization and merge behavior with explicit workspace isolation. Prove that work on one branch does not leak into another. Inventory which domain data is branchable before choosing storage representation; do not assume host configuration, credentials or operational audit are ordinary branch contents.

Additional draft acceptance cases: `BranchLocalWorkSurvivesHubOutage`, `BranchWorkspaceIsolation`, `SyncPreservesBranchAndRecordIdentity`, `ThreeWayMergeUsesCommonAncestor`, `IncomingConflictRequiresReview`, `ConflictReviewSurvivesRestart`, `CleanMergeStillValidatesGraph`, `StaleResolutionRequiresRevalidation`, and `ResolvedMergeReplicatesWithoutRepeatConflict`. These are proposed contract labels pending canonical requirement reconciliation, not implementation or test-pass claims.

Primary sources: [Git merge-file](https://git-scm.com/docs/git-merge-file) and [Git merge-base](https://git-scm.com/docs/git-merge-base). Read-only local capability receipt: `git --version` returned 2.56.0 and `git merge-file -h` exposed three-way input, diff3/zdiff3 and diff-algorithm options on LAB-OMARCHY. No merge command was executed against project data.

### 0.3 Synchronization trigger clarification

Operator correction, 2026-11-10: local version-control commits trigger synchronization; local changes do not. For the Dolt candidate, this means a successful Dolt history commit, not an EF Core `SaveChanges()` or ordinary SQL transaction commit. Uncommitted working changes remain local. Receiving a hub synchronization request must not implicitly commit those changes just to make them publishable. A commit made offline leaves synchronization pending for reconnection, even when the hub has no queued request for that node. This clarification does not approve Dolt adoption or select an automatic commit policy.

Draft acceptance cases: `LocalVersionCommitTriggersSync`, `UncommittedChangesDoNotTriggerSync`, `HubSyncRequestDoesNotAutoCommitLocalChanges`, and `OfflineVersionCommitSyncsAfterReconnect`. These remain proposed cases pending canonical requirement reconciliation.

### 0.4 Hub control table and node polling

Operator direction, 2026-11-10: the inbound control queue can be a durable table on LAB-OMARCHY that registered nodes check periodically or on demand. No separate message broker is required by this direction. Nodes retrieve their pending requests, execute the requested synchronization and persist an outcome; reading a request alone is not completion. Local version-control commits independently trigger synchronization as specified in section 0.3. Unavailability of the hub delays synchronization without preventing local work.

Proposed implementation details for reconciliation: scope each request to its authorized node, workspace and branch; use stable request IDs and durable completion/review state for retry-safe recovery. Keep control metadata outside the business branches being synchronized, so polling does not depend on first merging those branches. Exact table schema, access interface and polling interval remain to be specified. Dolt adoption remains an option pending approval and compatibility evidence.

## 1. Outcome and authority

Finish federation with LAB-OMARCHY as Hub and PAYTON-LEGION2 as the first LocalProxy. The operator confirmed that federation is currently disabled. Keep lab federation disabled through F1, F2I, F2, F3 and F4. Enable it only at D2 after implementation acceptance, then admit routine traffic only after backfill parity and operational acceptance. Development integration tests use isolated stores and fixture hosts, not enabled lab services.

This plan refines [the original federation plan](2026-10-07-federation-fixes.md). D2-D7 remain in effect, with the explicit D4 clarification below. Proposed amendment D1-A: re-enroll once at D2 after F1, F2I, F2, F3 and F4 acceptance, instead of immediately after P1/P2 deployment. Identifiers still change once before any backfill write. All choices in this proposal are concrete recommendations for operator plan approval, not claims of already approved product changes. F0 registers their FR/TR/TEST/AC mappings before implementation. A rejection changes the proposal and triggers renewed review; it is not left to the implementer to choose an alternative.

Proposed D4 clarification: incomplete-workspace local reads explicitly report stale status and do not promise visibility of writes forwarded only to the hub. After a workspace switches to hub reads, a hub outage returns 503 for reads rather than silently returning its obsolete local copy. Supported offline mutations retain the existing durable-queue/202 contract. No routine traffic is admitted during initial backfill.

BDPv4 authority: [Development Process v4](../Development-Process-draft-v4.md), the operator's subsequent green-mock-first clarification, and the current AGENTS.md zero-failure/zero-skip gate. Historical red-first execution and baseline-relative allowances are evidence of past practice, not permission for this work.

Breaking migration: YES. Workspace keys, derived identifiers, token keys, and enrollment associations can change. There is no legacy dual-key fallback. Database and token-file recovery must be proved together. Public registration payload additions must remain compatible with supported older proxies.

Authorization for this turn is planning only. This file neither enables federation nor authorizes deletion of existing enrollment records.

Operator direction, 2026-11-10: do not reinvent federation. Evaluate existing working solutions and surface suitable libraries for approval or denial before adopting them. Section 16 governs this selection. Mechanism-specific proposals below are provisional where library selection affects them; they must be reconciled before final plan HV. Required outcomes, workspace isolation, ownership, recovery and BDPv4 gates remain applicable.

## 2. Evidence and unresolved prerequisites

- Current local develop was read at `68c2d9001e490f7cb76d60eb56032cbfea389f0c`. The root has concurrent changes; preserve them.
- P1.4 checkpoint `c0e4146f` is an ancestor of current HEAD (`git merge-base --is-ancestor c0e4146f HEAD`, exit 0, 2026-11-10). PR79 merge `ebf84590998d28045ec432298907b3fea3c593e0` records the federation branch merge. This proves ancestry, not fresh cross-host gate success. Recheck on the execution base; if absent, import those accepted changes with their provenance before F1 and rerun the gates.
- P1.5 donor: `d22a1119`, available in `.worktrees/pr83-hv-20261017`; [PR83](https://github.com/sharpninja/McpServer/pull/83) is the historical review source. Its branch/review status must be refreshed before porting.
- Earlier analysis found the open chain `IMPL-FEDPATH-001 -> IMPL-FEDROUTE-001 -> IMPL-FEDWSSYNC-001 -> IMPL-FEDBACKFILL-001`, plus `PLAN-PATHNORM-001` and `MCP-FEDIDENTITY-001`. These are prior live-query results, not fresh reads in this turn.
- Initial drafting encountered an absent marker. During remediation the marker and supported plugin route recovered; the live identity TODO was read successfully in turn `req-20261110T165906Z-prompt-0e7a`. Historical failures do not prove current unavailability. No claim is made that all requirements have been registered or all live scopes loaded for a reviewer.
- The operator's disabled-state statement is authoritative for planning. Actual role configuration, enrollment rows, queue, conflicts, database providers, and backup sizes still require inspection.
- [Federation reference](../context/federation.md) contains an obsolete hub hostname. LAB-OMARCHY is the operator-confirmed intended hub; hostnames are configuration, never product constants.
- The current `build/Build.Test.cs` excludes Build.Tests. `Test` alone cannot prove the required full unit gate. `build/Build.TestGroups.cs` provides named-class gates with TRX and rejects empty, failed, or skipped selections.

No timing estimate is asserted: provider rehearsal size and current Windows readiness have not been measured.

## 3. Critical path and work ownership

Execution order:

1. F0: select reusable federation components with operator approval (section 16), then reconcile requirements, plan, audit access, and gate inventory.
2. F1: port and repair P1.5; accept the complete P1 scope.
3. F2I: federation identity, followed by F2: hub routing bound to that identity.
4. D1: backed-up rekey/deploy with federation disabled on both lab hosts.
5. F3: canonical workspace synchronization.
6. F4: domain backfill, resumability, and read gating.
7. D2: final parity, operational acceptance, and routine traffic admission.

One execution owner maintains the dependency chain and receipts. The operator selected Claude for plan HV; the review skill selects Opus, and high effort is requested explicitly. Verify effective model/effort from runtime metadata, not the model's self-description. Review runs remain independent of Codex.

Create `.worktrees/federation-completion-20261110` from current accepted develop after checking the path is unused. Use a dedicated `integration/federation-completion-20261110` branch; phase PRs F1, F2I, F2, F3, F4 target this branch in order. This avoids requiring an unrelated-workstream deployment freeze on develop. F4 subincrements stay within its PR and do not acquire premature done states. Before D2, reconcile current develop into the integration branch, run the complete final gates and HV, and deploy that accepted SHA. Merge the aggregate to develop only after D2 acceptance. Any change after a gate invalidates affected evidence and requires renewed verification/HV.

Use PR83 as a source of contracts and implementation fragments, not as a wholesale merge. Do not import its dependency changes, generated requirement exports, or unrelated test expectation edits without individual justification against the current base. Preserve the original donor and worktrees.

Unrelated C4-model `p4/*` worktrees, generic namespace/analyzer work, and historical clean worktrees are outside this critical path. Worktree cleanup is not a prerequisite.

Prepare identity requirements and provider environments during F1 development when independent. Do not start later phase production work while an earlier phase awaits its required exit review. No implementation edits occur during an active or paused HV round.

## 4. Mandatory BDPv4 cycle for every implementation increment

1. Pin the approved FR/TR/TEST/AC versions and define the public contract. Map every AC to named tests, expected output, side effects, and failure behavior. Coverage denominator is all approved ACs for the increment, not the tests conveniently available.
2. Write contract tests and independent scripted fixtures. Use literal requirement-derived data, not results computed by production code. Test-only seams may be added; production behavior is not ported yet.
3. Run the MockData binding. For every assertion, include expected negative controls that reject wrong values, missing effects, incorrect interactions, and applicable exceptions. Negative controls pass by detecting rejection. Gate: every required case executed, zero failures and zero skips.
4. Retain mock TRX, command, host, commit/tree hash, test/fixture hashes, AC mapping, and negative-control results before the first production edit.
5. Implement or port the increment; bind the same inputs, assertions, expectations, and interactions to Real. Fix implementation rather than weakening tests. Run changed classes through Nuke TestGroups after edits.
6. Increments are internal steps of the enclosing Byrd implementation phase, not separately accepted slices. Run changed MockData/Real classes and required provider bindings through TestGroups after edits. Do not represent a focused run as phase acceptance.
7. Before leaving each Byrd phase, run the entire current/prior unit inventory and all required integration, provider, Linux/Windows, traceability and runtime gates at the final code/test head. Zero failures, skips or unexecuted required cases. Per live MEMORY-BESTPRACTICE-001 section 2, run the full gate set once at phase exit, not after every edit. If fixes change that head, run changed groups and then the full gates once at the new exit head. HV begins only when no code is pending.

Do not create skipped tests for future increments. Track deferred scope in MCP. Existing quarantine remains governed by its existing approvals; no new quarantine or exclusion may hide a federation AC. Enumerate all excluded cases and their approved ownership in the gate manifest.

## 5. F0: reconciliation and entry gate

Deliverables:

- Restore trusted plugin operation through the normal workspace bootstrap procedure, then load Global and this Workspace's memories excluding MEMORY-PROCESS-007. Recover pending audit records and verify persisted history. Do not fabricate a marker or reuse another workspace's key.
- Read the current federation TODOs, all mapped FR/TR/TEST/ACs, and outstanding PR83 findings. Record source IDs and versions in the execution receipt.
- Attach this approved plan to the existing TODO chain. Keep P1/PATHNORM open until F1 acceptance. Record P1.1-P1.4 as delivered subwork with their receipts, without claiming P1 done.
- Preserve existing requirement families: `FR/TR/TEST-MCP-FED-PATH-001`, `FR/TR/TEST-MCP-PATHNORM-001`, `FR/TR/TEST-MCP-FED-ROUTE-001`, `FR/TR/TEST-MCP-FED-WSSYNC-001`, and `FR/TR/TEST-MCP-FED-BACKFILL-001`.
- Reconcile the identity TODO with existing Identity Server requirements before allocating new IDs. Register missing identity FR/TR/TEST/ACs through the plugin; this file's case names are planning labels, not registered requirement IDs.
- Register additional negative/security/recovery ACs below by strengthening existing requirements where applicable. Export required docs and mappings through the supported workflow. Never directly edit requirement storage.
- Pin a unit-project inventory including Build.Tests and all prior iteration cases, both hosts' integration inventories, provider versions, and exact commands. Confirm Nuke can execute every required class and produce TRX; missing test transport blocks the gate rather than silently reducing coverage.
- Record both hosts' deployed versions and actual provider/schema states. If P1.4 is deployed against unconverted keys, report the observed incident before maintenance; do not assume the current service is safe or silently rewrite live data.
- Reconcile the existing baseline and approved quarantine inventory on both hosts. Any federation/PATHNORM/identity/gate AC whose only test is quarantined remains uncovered. Do not add exclusions. Run the baseline before first production work; an unhealthy environment or failing required test blocks that phase until corrected within approved scope.
- Reconcile historical P1.R 69 MockData versus 67 Real cases by case ID; every unmatched mock case must be a documented assertion-validation negative control, otherwise add its missing Real binding.
- Amend WSSYNC AC1 to use the current inventoried workspace denominator instead of the historical 55/55. Map drift to GET federation/workspaces and backfill per-domain status to GET federation/status. Correct touched FR-MCP-082/083/085 source references using live canonical IDs; update the obsolete hub example in docs/context/federation.md. Require XML docs and AC IDs on all new test classes/methods.
- Obtain operator approval of this plan and its concrete proposed ACs, including D1-A, the D4 clarification, and the identity contract below. Approval is required before implementation, not before reviewing the proposal.

Exit: trustworthy store receipts; approved mappings with 100% planned AC coverage; baseline gate evidence; no unresolved behavior decision needed to start F1. Environment facts are measured entry predicates, not discretionary architecture choices.

## 6. F1: P1.5 salvage and complete P1 acceptance

Primary source surfaces: `QBrainAi.Services/Database/WorkspacePathRekeyGuard.cs`, `QBrainAi.Support.Mcp/DatabaseMaintenance/WorkspacePathRekey*.cs`, migration coordinator, Program, token store, and provider-owned test projects. These are donor paths; confirm their current locations before edits.

Perform these increments in order, each using section 4:

- F1a, identity mapping and inventory. Contract `RekeyMapping`: drive, UNC, device-prefix, slash variants, case-equivalent values, POSIX, opaque IDs, empty/global IDs, already-canonical IDs. Preserve explicitly assigned federation IDs; recompute only identifiers whose provenance proves they are derived. Detect converging keys before mutation. `RekeyInventory` covers every converted workspace-key column, dependent hashes, and legacy schemas without CanonicalWorkspaceId. Unknown unsupported schema fails with no writes.
- F1b, provider execution. Contract `RekeyProviderTransaction`: run SQLite, SQL Server, PostgreSQL fixtures; verify mapping, unchanged row counts, unique indexes, all composite FKs, restored constraint state, interruption/rollback, and second-run no-op. Any constraint validation failure rolls back. Test provider-specific behavior on the actual provider, not EF InMemory.
- F1c, command and file consistency. Contracts `RekeyCommand`, `RekeyTokenStore`: resolve the configured instance using the canonical environment variable and precedence shared with normal startup; resolve runtime data-directory/token paths identically; reject nonexistent SQLite sources and source/copy aliases including symlink or equivalent-path aliases. Dry-run does not mutate source. Enforce backup/report requirements and restricted token-file permissions. Detect convergence using the token store's actual comparer. Test token Get/Upsert/Delete after rewrite.
- F1d, startup and interruption safety. Contract `RekeyStartupGuard`: inspect every applicable converted column, not only Workspaces.WorkspaceId. Test database success followed by token-write failure, process restart, incomplete report/backup, failed restore, legacy schema, and partially completed maintenance. The server must not serve with inconsistent database/token identity; rerun or verified restore recovers it. The database transaction does not imply filesystem atomicity.
- F1e, provider rehearsals and P1 regression. Restore independently verified copies of both actual stores and token files. Run dry-run, apply, validation, repeat apply, and restore verification. Run all P1.1-P1.4 path/host/native-boundary regressions and the complete P1 gate on Linux and Windows.

The nine previously reported PR83 findings map to F1a (explicit IDs, legacy schemas), F1c (runtime token path, SQLite alias, instance environment, permissions, comparer convergence, missing source), and F1d (guard coverage). Refresh and resolve any further findings too; nine is not a cap.

F1 exit: complete provider receipts, retained verified backups, full gates and independent AGREE at >=98% accuracy and completeness. Merge F1 into the federation integration branch. Record implementation acceptance; close a parent TODO only if its canonical ACs require no outstanding deployment proof, otherwise retain it open until D2. Apply this rule to all phase TODOs. Do not deploy the rekey as an incidental PR merge action.

## 7. F2: hub routing

Requirements: FED-ROUTE family; work surfaces: workspace middleware, federation middleware/proxy service, token scope, topology lookup, and middleware/controller contracts.

- F2a `FederatedWorkspaceResolution`: proxy ID plus canonicalized local path and GlobalWorkspaceId resolve to the same registered canonical row. Include identical paths owned by two different proxies, drive/UNC variants, unknown IDs, and an ordinary non-federated request regression.
- F2b `FederationForwardingAuthorization`: hop-bearing requests with absent, ambiguous, conflicting, or foreign-workspace identifiers fail with diagnostic 4xx; no primary-workspace fallback. Validate token owner against the requested proxy/workspace. Forward the real GlobalWorkspaceId. Reject forged proxy headers, invalid hop counts/loops, and unsupported routes without leaking another workspace's data.
- F2c integration: seed distinguishable data into two proxies' workspaces and hub primary. Prove GET and permitted writes address only the intended workspace; exercise REST and MCP transport forwarding with existing transport contracts.

F2 exit: full gates, traceability, HV, merged PR, and IMPL-FEDROUTE-001 acceptance. Identity remains a separate dependency for deployment even after routing acceptance.

## 8. F2I: identity before enrollment

Scope from MCP-FEDIDENTITY-001: user management through the existing Identity Server integration, single-user federation nodes, and node authentication through the hub Identity Server. Do not build a parallel identity service.

Proposed acceptance cases, to register and reconcile in F0:

- `FederationNodeOwner`: an enrolled node has one authenticated owning user; attempts by a second unrelated user cannot take over the node or its workspaces. A node's single-user restriction does not limit the hub to one user across all nodes.
- `FederationHubAuthentication`: valid credentials issued by the configured hub identity integration authenticate the intended node. Wrong issuer/audience, expired/revoked credentials, mismatched node/user, missing credentials, and forged ownership fail closed.
- `FederationIdentityLifecycle`: supported existing user-management operations retain their existing authorization rules. Re-enrollment preserves authorized ownership and canonical mappings; revocation blocks future authenticated operations and replay. Identity outage cannot turn into anonymous access.
- `FederationIdentityRestart`: configured ownership and valid enrollment survive restart; credential material is never included in status, reports, or receipts.

Reuse of the existing Duende IdentityServer is the proposed identity option in section 16. Grant type, owning-user authentication and node credential binding are not yet locked. Client credentials alone do not authenticate the owning human. Resolve those contracts using the library's supported facilities before final plan acceptance. Do not treat the current shared HubAccessToken as a node identity or add a parallel token/revocation service.

Use the section 4 sequence for owner binding, authentication, then lifecycle integration increments. Full cross-host gates and HV precede merge and identity TODO completion.

## 9. D1: rekey/deploy while disabled

Entry: F1, F2, and F2I accepted; backup/restore rehearsals passed. Persist operational evidence through the plugin, with its supported failsafe for outages. Routine traffic remains outside the enabled federation until D2.

1. Inventory both hosts through trusted wrappers: configured/effective role, hub URL, proxy IDs, enrolled workspace mappings, queue/fanout/conflicts, stale-read state, provider/schema versions, data/token locations, and service version. A disabled setting does not imply empty tables.
2. Quiesce writers; record the durable maintenance cutoff. Copy and verify database, identity-database, token and configuration backups, retain originals, and prove restore to isolated targets. Store secret-bearing backups under restricted access. Record row counts and mappings without secrets. This maintenance cutoff is distinct from the D2 backfill snapshot cutoff.
3. Preserve the original plan's binary migration order: PAYTON-LEGION2 first, then LAB-OMARCHY. Run reviewed maintenance commands against the explicitly identified instance, deploy only via the repository's Nuke UpdateService target, and verify startup while federation remains disabled.
4. Stage LAB-OMARCHY Hub and PAYTON-LEGION2 LocalProxy configuration using the approved identity contract, with federation disabled. Do not enroll or start replay workers yet.
5. Verify canonical keys, startup guard, restart behavior and disabled status. Keep routine local work under the existing standalone contract until D2; no partially implemented federation routing is enabled.

Rollback before accepting new writes: keep federation disabled, restore the paired verified database/token/configuration/identity snapshots, deploy the compatible prior binary through Nuke, and verify identities/constraints. After the first write to a rekeyed store, default recovery is a forward fix with federation disabled, not an automatic binary downgrade. A restore that loses writes requires explicit operator approval naming the loss. Preserve post-backup data for reconciliation. Failed restore or unresolved conflicts leaves affected service unavailable for routine traffic.

Operational receipt and independent acceptance review close D1; software phase TODOs are not used as substitutes for deployment proof.

## 10. F3: canonical workspace synchronization

Requirements: FED-WSSYNC family; surfaces: registration DTOs, typed metadata, topology service, heartbeat, client contracts.

- F3a `WorkspaceMetadataRoundTrip`: enroll/create and heartbeat/update preserve every approved proxy-owned field: TodoPath, RunAs, AgentPath, PromptTemplate, StatusPrompt, ImplementPrompt, PlanPrompt, IsPrimary, DataDirectory, TunnelProvider, plus existing Name/IsEnabled behavior. Test non-default values individually and together, null/omitted versus explicit clearing, and old-proxy payloads. Foreign host paths remain metadata and must not cause hub filesystem access or execution under proxy RunAs.
- F3b `WorkspaceOwnershipAndDrift`: CurrentRequirementLayerKey and hub-maintained IDs, timestamps and backfill state survive enroll/heartbeat unchanged. GET federation/workspaces exposes `driftFields` (field names) and `hasDrift`; confidential values are not returned. Repeat delivery and restart do not duplicate rows or erase newer accepted configuration. Payload presence and ordering semantics must be reconciled with the selected library before final plan acceptance; section 16 records the pending choice.
- F3c integration: for the current enrolled-workspace inventory, prove every proxy-owned field equals its expected value and every hub-owned field retains its hub value. Historical 55-workspace counts are not a current denominator.

Exit: full gates, fixture-inventory parity on both hosts, HV, integration-branch merge, and implementation acceptance for IMPL-FEDWSSYNC-001. Live inventory parity remains a D2 requirement; this avoids making F3 acceptance depend on pre-F4 lab activation. Deploy through Nuke while federation remains disabled.

## 11. F4: backfill and read gating

Requirements: FED-BACKFILL family. Preserve workspace-scoped apply and approved version/conflict outcomes. Existing signed envelopes remain the compatibility baseline; evaluate whether the selected library replaces their transport or persistence internals. Any change to the original D2 signed-envelope contract requires explicit approval. Do not claim exactly-once delivery or build a second retry/checkpoint engine alongside a selected library.

- F4a `BackfillWorkspaceScope` and `BackfillReadGate`: set canonical workspace context before adapter access. Reject unowned or unresolved targets. Incomplete workspaces serve reads locally per D4; writes use the existing hub/queue contract. One workspace completing must not switch another workspace's reads. Test hub outage and restart. Build this gate before admitting backfill activity.
- F4b `BackfillEnvelopeAndCursor`: enumeration with durable per-workspace/domain progress; signed applyMode=backfill; duplicate/reordered delivery; lost acknowledgement; crash before/after apply and cursor persistence; restart and rerun; malformed/expired/tampered envelope; denied domain. Progress advances only after durable acknowledgement. Backfill emits zero new fanout. A checkpoint must not skip unacknowledged records.
- F4c graph adapters, group 1: requirements including layers, ACs and traceability; memory plus versions; todo including task/list/link/document metadata and audit history; full session-log graph. Allocate one implementation increment and section 4 gate per domain. Discover entity dependencies before selecting domain order; missing parents must fail or defer without lost children.
- F4d graph adapters, group 2: use cases; documents/chunks; triage; tool buckets/definitions; agents; brain slots; handoff runs; actors. Again one increment per domain. Any explicitly local-only domain needs an approved reason and visible exclusion in coverage/parity reports. Snapshot-only support is not apply support. Preserve existing coordinator requirements for brain-slot operations.
- F4e `BackfillConcurrentWrite` and `BackfillParity`: newer hub versions cannot be overwritten silently by stale backfill. Exercise concurrent write, deletion/tombstone, partial graph, retry, and conflict resolution. Any unresolved conflict or unverified domain keeps the workspace incomplete. Compute independent per-domain/per-workspace counts and deterministic checksums over canonical replicated fields at a documented consistent cutoff. Differences must identify records and fields. Repeated complete backfill reports already_applied and no duplicates.

Every adapter's contract covers ids, versions, timestamps, supported history, relationships, ownership, enumeration boundaries, empty domain, deletion behavior, and failures. Enumerate exact entity/field coverage from the current model during its contract step; a generic round-trip count is insufficient.

Exit: all approved domains either proved or explicitly approved local-only, full unit/provider/transport gates on both hosts, traceability, HV, merge, IMPL-FEDBACKFILL-001 acceptance. Runtime completion is separately gated by D2.

## 12. Gate execution and evidence

All commands run in PowerShell 7 (`pwsh.exe`). Operator correction 2026-11-10: LEGION2's preferred connection is SSH as `kingd`, using the supplied key; this supersedes older WSMan/PowerShell.MCP routing guidance. Resolve the configured key privately without printing it. Windows interactive integration checks run in the existing interactive user context required by the Nuke lane; an SSH session alone does not prove that precondition. Script automation passes parsing and ScriptAnalyzer; state-changing scripts support and first run WhatIf. Build and deployment use Nuke.

Per-increment named bindings use the existing command form:

```powershell
./build.ps1 TestGroups --test-groups '<project>:<Subject>MockDataTests' --test-run-id '<local-time>-<increment>-mock'
./build.ps1 TestGroups --test-groups '<project>:<Subject>RealTests' --test-run-id '<local-time>-<increment>-real'
```

Replace angle-bracket parameters from the approved test manifest before execution. They are templates, not claims that those classes already exist.

Full gate command set at the phase's final code/test head on Linux and Windows:

```powershell
./build.ps1 Compile
./build.ps1 Test --test-run-id '<local-time>-<phase>-<host>-unit'
./build.ps1 MigrationIntegrationTests --test-run-id '<local-time>-<phase>-<host>-integration'
./build.ps1 ValidateTraceability
./build.ps1 ValidateConfig
```

Additionally run Build.Tests through TestGroups with an explicit list of every concrete eligible test class discovered in that project, retaining the invocation and inventory. Split class lists across deterministic batches if needed; verify union coverage against discovery. TestGroups intentionally selects all categories within its named classes: keep it unchanged. If an approved excluded case shares a selected class, the full gate includes that case and must pass it; do not silently claim an exclusion was applied. Any missing project/class or unsupported platform case blocks the relevant host gate until its ownership is resolved.

Nuke Test currently trusts runner exit status without validating every TRX counter. The acceptance verifier must inspect actual TRX: nonempty required inventory, executed=total, passed=total, failed=0, notExecuted=0 and no skipped result. Compare test identities to discovery, not only totals. Implement this small receipt-verifier in PowerShell under the gate requirement, with `GateTrxContract` MockData/Real tests for empty, missing, duplicate, failed, skipped and inventory-mismatch reports before using it for acceptance. This does not require changing TestGroups filtering or adding a general Nuke route. The existing full suite runs through Nuke; the verifier only checks its receipts. No known failed baseline is waived.

Receipt root: `docs/receipts/federation-completion/<local-time>-<gate>/`. Keep commands, host, base/head/tree hash, runtime/provider versions, test inventories, raw TRX paths/hashes, passing/failing/skipped/not-executed counts, mock chronology, rehearsal/restore reports, and mapping/coverage evidence. Retain original raw artifacts if compressed copies are needed. Logs must redact keys, credentials, and token values.

## 13. Independent validation and completion rules

HV checkpoints in execution order: F1, F2I, F2, D1, F3, F4, D2; no intermediate subincrement is marked done independently. For implementation review, supply actual TRX for every unit project and all required integration runs at the reviewed head. The reviewer reads evidence and sources; it does not build, restore, or run tests. Inadequate tests produce named AC/test findings.

Before each round, read the whole plan and all documents under review, perform the per-AC what-if pass, traceability/propagation lint, dependency and activation audit, and comparison with operator decisions. Persist that checklist before launching.

Use the operator-selected different-family reviewer with verified model/effort. Reviewer loads add-profile and opens its own audit turn. Persist the actual request and byte-complete response JSONL in `docs/receipts/hv/<local-time>-<gate>.request.jsonl` and `.response.jsonl`. Require ongoing progress entries with completion percentage and reviewer ETA. Do not time-bound or size-bound the review; only a stream stalled for more than 30 minutes may be stopped and relaunched, with the stopped run retained as non-verdict evidence.

Store the entire verdict, every finding, scores, identities, and receipt paths in MCP and verify persistence. Acceptance requires OverallVerdict AGREE, accuracy >=98%, completeness >=98%, and no outstanding applicable substantive FAIL/UNKNOWN. Return a requirements-to-task/change mapping and individual verdict/scores for each adjudicated task, per live MEMORY-HV-001. Show all failures/unknowns to the operator. Correct findings between rounds and rerun affected gates before renewed review. Infrastructure failures alone do not force DISAGREE, lower scores or block acceptance when sufficient evidence exists (MEMORY-HV-001 section 6 supersedes older profile wording). Retain full local receipts, report operational failures separately and retry persistence through the supported failsafe. Name genuinely missing criterion evidence; never invent it.

## 14. D2: final operational acceptance

1. Take fresh paired database/identity/token/configuration backups and verify recovery; deploy the accepted integration SHA through Nuke. Apply the reviewed provider schema migrations while services and federation writers are stopped. Before enabling either role, export pending proxy queue rows and unacknowledged hub outbox rows. Replay only entries whose identity mapping, owner and payload are verified; block activation on any ambiguous/conflicted record and obtain its disposition. No automatic deletion or discard is authorized.
2. Quiesce product writes on both stores for the initial snapshot/backfill window; retain the existing maintenance boundary until parity. Internal backfill and audit writes remain permitted. Capture consistent per-domain snapshots, row counts, and a backup/cutoff identifier. Enable LAB-OMARCHY Hub, then authenticate/enroll PAYTON-LEGION2 once. If there is no existing enrollment this is initial enrollment; otherwise preserve the validated ownership mapping. Routine traffic remains unavailable during this bounded-by-completion maintenance operation.
3. Capture both hosts' status and current workspace/domain inventory. Run backfill and restart/resume checks against the pinned snapshots. Prove exact per-domain/workspace parity, no duplicate data, and preserved relationships. A conflict or changing source invalidates completion and retains the maintenance boundary. Test concurrent writes separately on isolated fixtures; do not weaken production parity to ignore unexplained differences.
4. At the acceptance checkpoint, require queueDepth=0, fanoutDepth=0, conflictCount=0, and the existing clear wire value `staleReadStatus=none`; report role/configuredRole, hubBaseUrl, proxyId, proxyCount, and hostedWorkspaceCount per host. Also require every workspace/domain completion record accepted. The signed-envelope controller already changes accepted operations to applied/already_applied; F4 tests this with one proxy and zero recipients, plus lost-ack retry. Never use unsigned operations intake for backfill. Queued/replay-blocked work cannot be hidden by a queueDepth projection that omits it; inspect the inventory as well.
5. Verify ordinary local/hub/spoke REST and MCP workflows using controlled validation data, cross-workspace denial, identity revocation, restart, and supported offline queue behavior. Recompute parity after validation mutations. Test raw MCP transport according to its existing non-queueable contract.
6. Obtain final independent AGREE with the entire plan and operational evidence. Admit routine traffic only after acceptance; take a new accepted baseline snapshot. Update TODO/requirements completion through MCP, export mappings/status, and preserve restoration/runbook evidence.

Final done means the intended hub and spoke operate correctly with synchronized approved data and complete audit/test/HV receipts. Merged code, successful enrollment, or an empty queue alone is insufficient.

## 15. Approval and present status

All execution steps above are pending. This proposal makes concrete the implementation order, contract-test cases, migration impact, PR boundaries, and gates. It does not claim fresh store registration, implementation validation, or hostile acceptance.

Pending before execution: operator library selection, reconciliation of affected contracts, plan/AC approval and canonical requirement registration, plus current environment inventory and provider/schema rehearsal. Claude is selected for review. The last plan verdict is R1 DISAGREE, accuracy 86%, completeness 78%; this revision has not received a new verdict. The frenemy probe's profile permission, audit-tool and stream limitations are operational issues, not automatic substantive rejection grounds. Apply section 13 and live MEMORY-HV-001 section 6. This plan does not authorize repairing frenemy as a side task.

## 16. Reuse-first selection before final plan acceptance

The concrete options and primary-source receipts are in [Federation library options](2026-11-10-federation-library-options.md). No dependency has been added or selected by writing this proposal.

Recommended direction for operator decision: reuse the integrated Duende issuer, evaluate YARP for forwarding, and evaluate Wolverine for durable domain operations over HTTP. Compare Dotmim.Sync as an alternative that could replace more of the database synchronization machinery. SymmetricDS is an external replication-engine alternative with a different deployment and licensing footprint. These are options, not interchangeable layers to install together.

F0 produces a selection record for each concern: candidate/version/license, exact work it replaces, required adapters, retained application policy, provider/runtime evidence, migration of existing queue/checkpoint data, unresolved limitations, and operator approve/deny decision. A library must remove custom infrastructure rather than introduce a competing authoritative queue or synchronizer. A rejected candidate does not automatically authorize custom implementation: document the unmet outcome and alternatives for the operator.

After selection, reconcile F2I authentication/user ownership, F2 transport behavior, F3 metadata presence/order, and F4 persistence/backfill/replay with the chosen component contracts. Identify explicitly any amendment to original D2-D7. Pin packages and source versions; verify dependency compliance and existing Duende entitlement. Where source review cannot prove compatibility, propose an isolated BDPv4 evaluation with named mock-first tests and real provider fixtures. No live data or services are changed by evaluation.

Only after these choices and evidence are incorporated can the plan claim decision completeness and seek final holistic Claude HV at 98/98. Read-only research and unaffected R1 corrections can continue while the operator decides; no architecture-dependent implementation is authorized.
