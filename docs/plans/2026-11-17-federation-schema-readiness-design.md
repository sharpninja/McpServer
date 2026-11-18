# Phase 0 B7 schema-readiness assertion design

Plan PLAN-FEDDOLT-001, Task TASK-FEDPH0-006. Trace FR-MCP-186, TR-MCP-FEDERATION-013, TEST-MCP-234: ac-DirtyDraftBlocksBinaryUpgrade, ac-ContractBlockedUntilLocalBranchesAttested and ac-DeploymentPreflightBlocksDirtyOrUnattestedBranch. Worker negotiation also derives from the approved B7/TR body. This is a proposed test/contract-step design, not an added requirement or public DTO, readiness transition, accepted B output or deployment. The implementation barrier remains Closed and DEPLOY HOLD remains active.

## Observation boundary

The disposable probe prepares a fixed Atlas fixture, independently reads its current complete node/local-branch inventory, attempts the named guarded operation, then independently reads it again, reopens the processes and reads it once more. The same assertions later bind to actual supported maintenance/protocol surfaces and retained native execution receipts. No production implementation is added during the mock step.

Each snapshot records ProjectId, current maintenance epoch, inventory fingerprint, current binary/schema support, worker and plugin contract versions, current public head, all local branch rows, all node attestation rows, native side-effect counts and a nonblank protected-state fingerprint. Branch rows bind node, exact branch name, head, schema, working-set fingerprint, dirty state, write-open state and required expand-head ancestry. Attestation rows bind node, reachability, epoch, inventory fingerprint and explicit attested branch names. These are internal assertion observations, not an invented public wire schema.

Readiness is evaluated over the owning node's complete local inventory, including unpublished branches. A public main at S1 cannot substitute for an unpublished feature. A stale epoch or fingerprint cannot authorize a later binary swap or schema contract. An offline node has no inferred acknowledgement. Creating or restoring an old-schema branch after readiness invalidates that earlier inventory; the captured branch must remain intact when deployment is refused. Existing approved fencing prevents such a branch from becoming writable during the transition.

The command reply must use the existing status/code/retryability mapping. Refusal alone is insufficient: binary swaps, service changes, EF migrations, history commits, remote object transfers and public head changes must all remain absent in these refusal fixtures. Each retained local branch, row/draft fingerprint, attestation binding and protected-state fingerprint must match the independent before-state after refusal and after process reopen. The real adapter derives operation completion from retained native/HTTP receipts and new process identity, never just method entry or a hardcoded successful response.

## Fixed discriminating fixtures

1. Dirty local branch: public main and local main are at schema 2/S1; an unpublished schema 1 feature has a draft fingerprint. Nuke deployment preflight returns 409 maintenance_not_ready, retryable false. No binary/service/schema/history change; the draft and feature head survive reopen.
2. Unattested unpublished feature: public and main are current, but a clean local schema 1 feature is not attested. Contract migration returns 409 maintenance_not_ready and preserves all state.
3. Offline subscriber: main is current, another enrolled node is offline and its unpublished feature has no current attestation. Contract migration is refused; no success is inferred from the public repository or another node's acknowledgement.
4. Stale inventory: an earlier attestation covered main; the independent current inventory also contains a restored old-schema feature. Deployment is refused using the earlier fingerprint. The feature remains closed to writes and its history/draft are preserved.
5. Stale maintenance epoch: a fully named branch inventory is attested for the prior epoch. Contract migration is refused even though the branch list itself matches and public main is current.
6. Unknown worker contract: a worker advertises contract 2 while the fixture hub supports 1 and plugin contract 1. Negotiation returns 409 unsupported_contract with no history/object/data side effects. Plugin compatibility cannot silently stand in for worker compatibility.

A branch/attestation row-order variation is valid. Deliberately missing, extra, duplicate, misbound or corrupted branches/attestations, changed fingerprints/epochs/versions/heads, nonzero side effects, absent replies or wrong codes, and missing operation/reopen evidence must reject through the same assertions. Negative controls are expected rejections within a passing xUnit v3 harness, never skipped or deliberately failing production tests.

## Real-binding evidence and remaining B7 work

After the whole group mock barrier opens, run these same fixtures through Nuke-owned disposable host processes on both machines. Keep native command/HTTP receipts, actual before/after inventories and fingerprints, unchanged binaries/service configuration/schema/refs, and restart identity evidence. The reviewer receives exact TRX and logs. These mocks do not prove native preflight, filesystem/service safety, Dolt/EF semantics or authenticated attestation correctness.

This seam covers readiness refusal and protocol separation only. B7 still needs single-author maintenance schema commits, startup migration exclusion, expand/contract history behavior, old-schema unpublished merge, feature catch-up and contract-fencing concurrency with the actual engine and supported providers. Existing task006 mocks, source inventories and this design do not complete the task or the group. Final-head gates, separate LAB/LEGION evidence, B-output operator acceptance and independent task/overall HV remain required.

## Mock checkpoint and fixture aliases

The Windows focused Nuke run `20261117T230125-fedph0-schema-readiness-mock` passed 1,290 cases (12 valid fixture/order combinations, 1,278 expected negative controls), zero failures and zero skips. Exact copied/hashed TRX and Nuke output, source hashes and canonical criteria are under `docs/receipts/federation/20261117-phase0-schema-readiness`. The assertions and mocks compile; LAB execution remains pending at this checkpoint. No real preflight, host, service, database, migration, token or deployment ran.

Literal S1/F1/F2, binary-2, inventory/working/row fingerprints and process-before/after are fixture identities. A real disposable adapter must bind them through a pinned run manifest to actual hashes, native binary versions and distinct process instances, retaining the source receipts. Merely returning these strings or incrementing counters is not real evidence. The refusal fixtures do not prove the ready/success path, concurrent contract fencing or the rest of B7.