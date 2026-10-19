# Task Workflow Use Cases

Plan: `PLAN-TODOTASK-001`.

Architecture: [TODO Plans, Tasks, Task Groups, and Session Work Dispatch](../architecture/Task-Workflow-Architecture.md). Requirement definitions and ACs: [FR](Functional-Requirements.md), [TR](Technical-Requirements.md), [TEST](Testing-Requirements.md), and [traceability mappings](TR-per-FR-Mapping.md).

Canonical source: workspace MCP use-case records. The UC-TASKFLOW keys below are document labels; native numeric UC IDs identify the saved records. All use cases remain Draft. Requirements are defined and pending implementation. Detailed `-002` records and TASKTURN/TASKAPI records resolve the earlier baseline notes that left policies unspecified.

Scope: 16 FRs, 17 TRs, 17 TEST requirements and 213 saved structured acceptance criteria, including the retained superseded provider criterion. Each FR has TR/TEST mappings and at least one Realizes use-case link.

Provider correction: TR-MCP-TASKAPI-002 explicitly supersedes the SQLite-only and retained-YAML clauses of TR-MCP-TASKAPI-001 (including its AC-002) and the corresponding old test assumptions. The workflow follows the existing authoritative database provider: SQLite, SQL Server or PostgreSQL. The deprecated sqlite TODO-storage alias maps to database; YAML storage remains removed. Prior AC rows were preserved.

The additional lifecycle, queue, error-handling and migration policies are authored specification decisions made under the request to complete this plan; they are not represented as separately confirmed operator choices.

## UC-TASKFLOW-001 — Compose a Plan and trace its requirement coverage

Native ID: UC-14. Status: Draft.

Actor: Planning agent.

Realizes: FR-MCP-TODOPLAN-001, FR-MCP-TODOPLAN-002.

Preconditions: An open agent session and an existing workspace are available; referenced requirements and documentation exist.

Postconditions: A TODO with ItemKind=plan owns explicit scope, documentation and ordered child Tasks; FR/AC trace queries expose the same mappings in both directions.

**Basic flow**

1. Create or specialize a TODO as a Plan; select explicit FR/TR and documentation references; create child Tasks with valid FR/AC coverage; read Plan details and filter coverage by FR or owning-FR/AC identity; follow a Task back to its parent.
2. Retain the TODO identity, store explicit scope separately from derived coverage, validate child ownership/order and return matching Tasks/groups with mapped versus successfully evidenced AC coverage and bounded pagination.

**Alternative flow**

1. Use an ordinary TODO as the parent, or retain an ungrouped Task during planning.
2. Apply the same coverage rules; expose ungrouped work but require a group before dispatch. Keep standard and plan ItemKind values distinct.

**Exception flow**

1. Attempt a kind change after execution/assignment, cross-parent membership, invalid AC ownership, deletion of a populated TODO or an invalid reorder.
2. Return the classified validation/conflict result; preserve the existing TODO, child Tasks, mappings and ordinals atomically.

## UC-TASKFLOW-002 — Define a Task with FR AC coverage and TR constraints

Native ID: UC-15. Status: Draft.

Actor: Planning agent.

Realizes: FR-MCP-TODOTASK-001, FR-MCP-TODOTASK-002.

Preconditions: A TODO parent, at least one FR with ACs, optional applicable TRs and an open agent session exist.

Postconditions: A valid Planning or Ready Task exists with stable identity, one parent and complete per-FR AC mappings.

**Basic flow**

1. Create a Task with a title and parent, select at least one FR and an AC of each selected FR, attach applicable TR constraints, choose optional same-parent group and order, then mark the reviewed Task Ready.
2. Validate the whole request, create a Planning Task, preserve normalized requirement links and permit Planning->Ready only for a valid definition.

**Alternative flow**

1. Edit a Ready Task definition or supply no TR constraints because none apply.
2. Return the edited Task to Planning; allow an empty TR set while enforcing every supplied TR reference. Do not permit missing FR/AC coverage even during Planning.

**Exception flow**

1. Use an unknown FR/TR, an AC from the wrong FR, no AC for a mapped FR, a stale version or an unsupported status transition.
2. Reject the request without creating partial Task/mapping rows or changing the existing version.

## UC-TASKFLOW-003 — Edit acceptance criteria without replacing their rows

Native ID: UC-16. Status: Draft.

Actor: Requirements editor.

Realizes: FR-MCP-ACIDENTITY-001, FR-MCP-ACIDENTITY-002.

Preconditions: A persisted requirement has stable AC identities; zero or more Tasks may reference its criteria.

Postconditions: Updated criteria retain original persisted keys and Task links; explicitly added or removed unreferenced criteria are the only inserted/deleted rows.

**Basic flow**

1. Submit text, evidence, satisfaction or order edits addressed by existing criterion ID; optionally add new IDs or explicitly request removal; repeat through single, batch, import and copy-from-TODO entry points.
2. Match original rows, mutate their fields in place, insert only new criteria, validate explicit removals and commit the entire update including requirement metadata atomically.

**Alternative flow**

1. Send a metadata-only update, omit acceptanceCriteria or omit existing members from a supplied criterion list.
2. Preserve all existing AC rows; omission alone never deletes criteria. Generate an ID once for a genuinely new criterion whose ID is omitted.

**Exception flow**

1. Remove a referenced AC/requirement, supply duplicate/empty IDs, rename an identified criterion, or encounter unresolved legacy duplicate IDs.
2. Reject and preserve the pre-request rows and links; report duplicates for explicit repair. Never delete/recreate or remap rows to simulate an edit.

## UC-TASKFLOW-004 — Organize a Task Group and observe calculated status

Native ID: UC-17. Status: Draft.

Actor: Planning agent.

Realizes: FR-MCP-TASKGROUP-001, FR-MCP-TASKGROUP-002.

Preconditions: A TODO and eligible never-started Tasks exist; the group has not yet been dispatched.

Postconditions: Group identity/key and member order are stored, and every status read derives from the complete member/dependency state.

**Basic flow**

1. Create a group key on the TODO, add same-parent Tasks, submit their order and request group details as member states change.
2. Persist stable Group ID and parent-scoped key/order. Compute Failing first, then Blocked, then all-success Completed, then empty/all-not-started Unstarted, otherwise Incomplete.

**Alternative flow**

1. Observe an empty group, a mix of Success and Planning/Ready, or both Failure and blocked members.
2. Return Unstarted, Incomplete and Failing respectively. Continue to permit later eligible Tasks despite unresolved Failure.

**Exception flow**

1. Set group status manually, duplicate a parent group key, move a successful Task or change membership/order after first dispatch.
2. Reject the mutation atomically; group status stays calculated and sealed membership stays unchanged.

## UC-TASKFLOW-005 — Define dependencies and inspect incomplete prerequisites

Native ID: UC-18. Status: Draft.

Actor: Planning agent.

Realizes: FR-MCP-TASKDEP-001, FR-MCP-TASKDEP-002.

Preconditions: Planning Tasks and same-workspace prerequisite Tasks/Groups exist, potentially under different TODOs.

Postconditions: A valid acyclic dependency graph exists and the agent can identify every unmet prerequisite by Task ID or Group ID.

**Basic flow**

1. Add zero or more typed Task/Group dependencies; call dependency inspection for a Task or group; request transitive paths if needed; attempt Task dispatch after prerequisite changes.
2. Apply AND semantics with Task Success and Group Completed as completion predicates. Report incomplete direct prerequisites with affected Tasks, or paged deduplicated transitive nodes/paths. Dispatch rechecks the full set.

**Alternative flow**

1. Use an empty prerequisite group or inspect a group with repeated shared prerequisites across members.
2. Treat the empty group as incomplete; return deduplicated dependency nodes with all affected member IDs. A successful dependent Task is not reopened.

**Exception flow**

1. Add a self/group-containing-self/cyclic/cross-workspace edge or introduce a cycle by group membership changes; encounter a missing prerequisite row.
2. Reject invalid graph mutations atomically. Report missing stored targets as blockers and prevent dispatch rather than treating them as completed.

## UC-TASKFLOW-006 — Delegate multiple Task Groups to an agent session

Native ID: UC-19. Status: Draft.

Actor: Coordinating agent.

Realizes: FR-MCP-TASKASSIGN-001, FR-MCP-TASKASSIGN-002.

Preconditions: Assigning and receiving sessions are open in the workspace; each group is nonempty, non-Completed and not actively owned by another session.

Postconditions: All assigned groups are retained in the receiving session queue with durable receipt order and assigning/receiving Session references.

**Basic flow**

1. Assign groups A, B and C to the receiving session in that order; retrieve their assignment receipts and verify that the session holds all three.
2. Append each new assignment with an increasing session-local receipt sequence; preserve prior assignments and enforce one active owner per group.

**Alternative flow**

1. Assign a group to the same receiving session again, self-assign as a standalone agent, or explicitly release and reassign a group with no Started member.
2. Return the original active assignment without changing its position for a duplicate; permit self-assignment; retain release history and append a genuinely new assignment at its new destination queue tail.

**Exception flow**

1. Assign to another owner or closed/missing/ambiguous session, assign an empty/Completed group, or release a group with Started work.
2. Return a classified conflict/validation result and leave ownership, sequence and Task states unchanged.

## UC-TASKFLOW-007 — Request new work by session ID in FIFO order

Native ID: UC-20. Status: Draft.

Actor: Worker agent.

Realizes: FR-MCP-TASKASSIGN-001, FR-MCP-TASKASSIGN-002.

Preconditions: The requesting session is open; zero or more groups are queued for that session; the plugin has a logical operation ID.

Postconditions: One oldest queued assignment is dispatched with its Task Group ID, or NoWork is recorded; retries reproduce the same result.

**Basic flow**

1. Request work using the session ID; retain the returned assignment and group IDs; make a distinct new work request when another group is desired.
2. Atomically dispatch queued groups in receipt order, seal first-dispatched group membership and persist the operation response. Return taskGroupId, assignmentId and receiptSequence without changing Task or computed Group status.

**Alternative flow**

1. Retry after a transport failure, request another group while prior dispatched work is unfinished, or request work from an empty queue.
2. Replay the same operation result; permit later distinct FIFO dispatches; return NoWork with null taskGroupId for an empty queue and do not automatically claim unassigned groups.

**Exception flow**

1. Reuse the operation ID with a different payload, use a closed/wrong session, or request a queued head whose member Tasks are currently blocked.
2. Reject mismatched/closed-session requests without dequeuing. A blocked head still dispatches as a group in FIFO order, while next-Task enforces the member dependency gates.

## UC-TASKFLOW-008 — Sequence eligible Tasks and record a successful attempt

Native ID: UC-21. Status: Draft.

Actor: Worker agent.

Realizes: FR-MCP-TODOTASK-001, FR-MCP-TODOTASK-002, FR-MCP-TASKGROUP-002, FR-MCP-TASKTURN-001.

Preconditions: A group is dispatched to the current open session; a current turn exists and is not attributed to another Task.

Postconditions: One eligible Task has a traceable attempt and, after validated completion, immutable Success evidence; skipped members retain their own states.

**Basic flow**

1. Request the next Task for the group; inspect its FR/AC/TR constraints and snapshots; perform work; record evidence for every mapped AC and submit a success summary.
2. Resume an existing Started member or select the earliest Ready/unblocked member, atomically start it with attempt/turn attribution, then atomically validate and freeze the successful outcome.

**Alternative flow**

1. Earlier members are Failed, blocked, Planning or already successful; another call resumes the same Started member; no eligible member remains.
2. Skip ineligible members without changing them, return a resume result without a second attempt, or return no-task with calculated status and eligibility reasons.

**Exception flow**

1. Dependencies are incomplete, the turn belongs to another Task, evidence is missing, the version is stale or two requests try to start different members concurrently.
2. Prevent invalid starts/outcomes and partial writes; permit at most one Started member. Require a new turn before starting a different Task.

## UC-TASKFLOW-009 — Remediate Failure while preserving successful work

Native ID: UC-22. Status: Draft.

Actor: Worker agent.

Realizes: FR-MCP-TODOTASK-001, FR-MCP-TODOTASK-002, FR-MCP-TASKGROUP-001.

Preconditions: A Task has a recorded Failure or Success; the agent can read its execution history.

Postconditions: A failed Task can enter a new planning/attempt cycle with prior history intact; a successful Task remains unchanged.

**Basic flow**

1. Record a Started attempt as Failure with a reason; continue later eligible Tasks if desired; initiate remediation with a reason; revise in Planning, mark Ready, dispatch a new attempt and record its outcome.
2. Keep the failed attempt and all its evidence/snapshots, return the Task to Planning for remediation and increment the attempt number only on a new start. Group status remains Failing while any member is Failure.

**Alternative flow**

1. Discover a needed correction after Task Success.
2. Create a separate correction Task referencing the successful Task; retain the original definition, mappings, order and evidence permanently.

**Exception flow**

1. Attempt to edit/delete/reorder a successful Task, replace a failed outcome silently or bypass Planning during remediation.
2. Reject the request; preserve immutable data and attempt history. An identical completed operation retry remains a no-op.

## UC-TASKFLOW-010 — Attribute session turns and inspect Task execution history

Native ID: UC-23. Status: Draft.

Actor: Worker agent.

Realizes: FR-MCP-TODOTASK-001, FR-MCP-TASKTURN-001.

Preconditions: A Task and one or more session turns exist in the workspace; the caller acts in the owning session context.

Postconditions: Turns carry idempotent Task FKs and Task history queries resolve turns and distinct Sessions through those persisted links.

**Basic flow**

1. Associate an unassigned current turn with the Task; repeat the same association; link later turns during work or remediation; query the Task history across sessions.
2. Apply null-or-same compare-and-set semantics, preserve each turn Session FK and return stable paged turn/Session history without a Task-side duplicate turn list.

**Alternative flow**

1. Append a late audit turn after Success or read legacy turns whose Task linkage was never recorded.
2. Allow the external audit association without changing the frozen Task; keep legacy links null and do not infer them from TodoId or titles.

**Exception flow**

1. Try to assign a different Task to an attributed turn, use another workspace/session or race two conflicting associations.
2. Return TurnTaskConflict or scoped validation failure and preserve the first association and all other turn data.

## UC-TASKFLOW-011 — Load TODO or Plan details with inline GraphRAG context

Native ID: UC-24. Status: Draft.

Actor: Planning or worker agent.

Realizes: FR-MCP-TODOGRAPH-001, FR-MCP-TODOGRAPH-002.

Preconditions: A standard TODO or Plan stores valid ordered workspace GraphRAG references; targets may subsequently become missing or unavailable.

Postconditions: TODO details remain readable with reference identities and bounded, classified inline context results.

**Basic flow**

1. Attach Document/Chunk/Entity/Relationship references to the TODO and request its details using default hydration.
2. Resolve references through supported services, preserve their order/identity and return current content, provenance and per-reference statuses within the specified count, content and time budgets.

**Alternative flow**

1. Request hydrateGraphRag=false, use the list endpoint, or encounter content larger than the hydration budget.
2. Return reference metadata without lookups for opt-out/list; return Truncated and explicit metadata for bounded content, preserving Unicode boundaries.

**Exception flow**

1. A source is missing, retrieval times out or fails, or the request contains a duplicate/cross-workspace/arbitrary-URL reference.
2. Existing unresolved references return Missing/Unavailable without failing TODO details. Reject invalid reference mutations atomically; never fabricate content or mutate source graph data.

## UC-TASKFLOW-012 — Upgrade and explicitly promote legacy checklist work

Native ID: UC-25. Status: Draft.

Actor: Workspace maintainer.

Realizes: FR-MCP-TASKAPI-001.

Preconditions: A workspace contains legacy TODO implementationTasks and may contain old session/requirement data.

Postconditions: Existing data and clients remain compatible; explicitly selected checklist entries can become valid new Planning Tasks with traceability and without fabricated history.

**Basic flow**

1. Apply additive migrations with stable AC reconciliation first; verify legacy TODO reads; explicitly promote a checklist entry while supplying FR/AC mappings and optional TR/group data; call the workflow through server, client and shared plugin surfaces.
2. Preserve old checklist IDs/text/done state; create a separate Planning Task and return an idempotent source-entry mapping. Apply common validation and classified errors consistently across transports.

**Alternative flow**

1. Keep using legacy checklist operations or use the deprecated TodoStorage sqlite alias while selecting SQL Server or PostgreSQL as the configured database backend.
2. Keep existing operations available and normalize the alias to database, using the configured backend. Apply TR-MCP-TASKAPI-002 provider compatibility; TODO.yaml remains a projection.

**Exception flow**

1. Promotion lacks FR/AC mappings, migration detects duplicate AC identities, a removed YAML provider is configured, or a transport/migration/provider validation gate has failures or skips.
2. Reject partial promotion or invalid provider configuration and do not synthesize success or session links; preserve data for explicit repair and do not advance the Byrd release gate.
