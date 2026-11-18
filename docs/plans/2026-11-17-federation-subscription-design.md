# Phase 0 B11 subscription and timing design

PLAN-FEDDOLT-001; TASK-FEDPH0-007; B11. Proposed contract-step design under the approved plan and current contracts sections6,8,10. Not an accepted B output or runtime result. Group implementation barrier Closed and DEPLOY HOLD active.

## Subscription ownership and durability

Unversioned SyncSubscription is keyed by NodeId, ProjectId and Branch. HubEpoch is a UUID; published, desired, reported and worker generations are Int64. Generation comparisons occur only after equal HubEpoch. Preserve PublishedHead, AcceptedHead, Outcome and timestamps. Only an authorized report acknowledges actual observed work; poll is read-only. A report for7 arriving after desired8 advances reported to7 while desired8 remains pending. Reconciliation of remote A1 against stale control C0 records the actual head and advances generation without creating a Dolt commit. Repeated reconciliation does not invent another generation once the same head is recorded. Both a publication receipt and the derived subscription state must be recoverable; an empty queue is not success evidence.

These assertions preserve complete unrelated branch/project subscriptions and local/public refs. Use independent database queries and actual history counts in the later real adapter; never fill readbacks from the operation's response or expected fixture. The current three-criterion mock seam and paired receipts remain under `20261117-phase0-subscription-state/`.

## Scheduling design

Use an injected monotonic clock, timer and random source. The normal polling delay proposal is30s with bounded20percent jitter: `24 + 12*u` seconds for an injected sample `u` in[0,1]. Fixture samples0,0.25,0.5,0.75,1 produce24,27,30,33,36. The test oracle uses those literal expectations, not a call into production scheduling. This linear mapping is a proposed implementation choice; the mocks do not establish randomness quality or actual timer behavior.

Transient-failure backoff is1,2,4,8,16,32,60,60... seconds. Commit, reconnect and on-demand wake requests are immediately eligible at delay0, including while the background loop is backing off. No scheduling operation commits history or mutates project business data. A real adapter must run actual scheduling against a controlled clock and record scheduled-versus-observed dispatch; the eventual disposable-host experiment also measures normal transport and process restart behavior. No real-time latency SLA is inferred from virtual-clock tests. Permanent authorization, validation or unsupported-contract errors are not turned into blind transient retries; exact error mapping remains the current-contract appendix.

## Worker fencing

FR-MCP-181/TR-MCP-FEDERATION-008/TEST-MCP-229 ac-OldWorkerEpochCannotReportSuccess: after takeover4to5, an epoch4 success report returns409 stale_worker_epoch with retryable=false. The current receipt and complete desired state remain unchanged through independent reads and reopen. Protect the selected subscription and its refs; permanent security/audit records may still record the refused attempt. Do not require an already admitted transaction to roll back on lease expiry; that is a separate write-gate contract. Old HubEpoch reports retain their existing409 hub_epoch_changed semantics and recovery seam.

## Stale visibility

FR-MCP-183/TR-MCP-FEDERATION-010/TEST-MCP-231 ac-StaleBranchReportedNotDeleted: observations at age86401s and172800s expose stale=true and the exact original observedUtc. Status display preserves branch identity, drafts and refs through reopen. No stale-age policy deletes or silently changes the branch. This is a generic selected status seam; Ph7 binds the same assertions to actual installed plugins. Review-age warning900s, critical86400s and at-most-hourly repeats retain their separate review-operations contract and permit zero repeats.

## Remaining evidence

Timing, worker-fence and stale-visibility assertion mocks are next. Actual durable control schema, provider indexes/concurrency, polling loop integration, all epoch transitions, restart/transport timing, all-plugin bindings and host-specific measurements remain unproven. B10 budgets, B12 physical ref retention/GC and the other Phase0 outputs retain their own obligations. No task, requirement or acceptance state changes follow from this design.

## Partial Windows mock checkpoint

`Phase0SubscriptionPolicyMockGateTests` passed130 cases (4 valid,126 expected negative) through native Windows Nuke TestGroups run `20261117T221205-fedph0-subscription-policy-mock`, with0failures/skips. Exact receipts and canonical records are in `docs/receipts/federation/20261117-phase0-subscription-policy/`. Its stale-age assertions independently verify the controlled clock. These are policy selections and scripted protected-state observations, not actual timer dispatch, random distribution, durable control storage, transport or installed plugins. Linux repetition and the remaining real evidence are pending; no acceptance state changed.