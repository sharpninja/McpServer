<!-- SPDX-License-Identifier: Apache-2.0 -->
# Handoff: Durable Multi-Agent Hostile Validation Architecture Amendment

- Prepared: 2026-10-16, using handoff timestamp 20261016T182551Z.
- Authoring agent: Cline.
- Workspace: `/home/sharpninja/github/McpServer`.
- Status: local continuity and blocker record only; NOT the architecture amendment, a canonical TODO, or a database audit receipt.
- Implementation authorization: none. The user authorized documentation and plan persistence, not implementation.

## 1. Start here

The user wants a rigorous architecture amendment for durable, multi-agent hostile validation (HV), incorporating the settled requirements below. Their earlier request to save the corrected plan as a high-priority TODO also remains unfulfilled.

Latest substantive request, verbatim:

> Create an amendment to the architecture document with the same level of rigor for diagrams, interfaces, AC, UC, and descriptive documentation.

Latest handoff request, verbatim:

> save your work so I can get a functioning agent to finish it.

No completed amendment text exists from this agent. This handoff preserves the consolidated requirements, delivery outline, source locations, necessary documentation coverage, verified blocker, and remaining work. Do not mistake it for a completed architecture deliverable.

Do not reopen the settled user decisions. Integration inspection, link verification, contract reconciliation, and plan persistence are agent responsibilities, not questions the user still needs to answer.

## 2. Exact state of work

### Completed or available

- The previous conversation recovered the HV draft and displayed a consolidated H0-H7 plan. The user then corrected several incorrectly presented open decisions; those corrections are incorporated below.
- The original architecture source, foundation plan, previous handoff, requirements matrix, and conversation-history file were confirmed to exist during this handoff.
- This agent inspected the foundation architecture's introductory conventions. It has NOT comprehensively reviewed the entire architecture or reconciled all contracts and requirement links.
- This agent independently verified the mandatory-plugin policy and both disabled plugin settings, as described in section 3.

### Not completed

- No HV architecture amendment has been created.
- No corrected HV plan has been persisted as a canonical TODO. There is no saved TODO ID to report.
- No successful canonical database audit was obtained for this work.
- No complete requirements/task-link verification has been performed.
- No HV implementation, implementation worktree, builds, tests, or commits have been performed by this agent.
- No existing architecture, foundation plan, requirements file, or TODO storage file was modified by this agent.

This handoff is the only new workspace file created for the handoff request. It is a local diagnostic/continuity artifact, not a workaround for canonical persistence or mandatory auditing.

## 3. Verified blocker and safety boundaries

The runtime instructions at `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml` currently require the matching agent plugin:

- Line 204 prohibits substituting raw REST, generic `mcpserver-repl`, helper modules, or another agent's plugin for normal session, TODO, requirements, import/export, or traceability operations. When the required plugin is unavailable, it permits only non-MCP local diagnosis.
- Lines 273-276 require a new persisted session-log turn before work and a final update with results and actions.
- Line 365 repeats the prohibition on bypassing the required plugin for session-log and TODO operations.

Older helper documentation at `/home/sharpninja/github/McpServer/docs/context/module-bootstrap.md` describes PowerShell helpers as supported, but that does not override the explicit runtime restriction. Merely finding those helpers does not establish a permitted alternate write route.

The following settings were independently read on 2026-10-16; both contain `mcpServers.mcpserver.disabled: true`:

- `/home/sharpninja/.cline/data/settings/cline_mcp_settings.json`
- `/home/sharpninja/.config/Code/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json`

The user explicitly prohibited invoking, re-enabling, or uninstalling the disabled plugin. Do not change that configuration, replay queues, or delete failsafe logs. The existing plugin process 1208370 was reported in the earlier conversation; its current runtime state was NOT independently checked. A disabled settings entry does not prove an already-running process has stopped.

Earlier session writes reportedly failed with `Session not found` and `no active session`, and HTTP fallbacks reportedly returned 404. Those are historical reports, not fresh observations by this handoff. Do not repeat that detour or infer that a different route works without evidence.

Required canonical database auditing is mandatory. Audit failure is a hard stop; local files, queued acknowledgements, or failsafe records do not substitute. This handoff does not claim compliance with, or successful completion of, that audit gate.

The next agent must establish a genuinely permitted, functioning audit/persistence route before resuming canonical work. Do not silently reinterpret the handoff request as permission to enable the disabled plugin or bypass the policy.

## 4. Authoritative source locations and provenance

### Foundation architecture: primary design source

`/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/architecture/SessionTurn-Review-AgentContext-Architecture.md`

This is the architecture the user specified as the foundation. Build on it and incorporate required session-log schemas and contracts from `session-20261009`. Do not overwrite or replace the foundation with an unrelated HV design.

Its introduction states that architecture is authoritative and the plan derives from it. It defines a unit of work as exactly one CQRS command or query and handler, distinguishes units from policies and orchestration processes, requires complete activity and sequence diagrams, and defines shared components once for reuse. It includes grouped entity diagrams and explicit use cases. Changes after plan approval require renewed hostile validation under that document's governance.

### Supporting plan and prior handoff

- `/home/sharpninja/github/McpServer/docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md`
- `/home/sharpninja/github/McpServer/docs/handoffs/sessionturn-review-plan-20261009T022141Z.md`

The architecture introduction references the plan identifier `PLAN-SESSIONTURNREVIEW-001`, while the plan filename contains `PLAN-SESSIONTURN-SESSIONREVIEW-001`. Verify the canonical task identity and references; do not assume these strings are interchangeable or allocate a duplicate.

### Recovered HV draft and subsequent corrections

`/home/sharpninja/.cline/data/sessions/1792165626722_56f98/1792165626722_56f98.messages.json`

The prior context summary locates the original draft in message 213. Inspect all text blocks rather than assuming `content[0]`; establish the file's actual message indexing before extracting. Later user messages contain binding corrections. This agent confirmed the file exists but did not re-extract the original draft during this handoff.

### Repository policy and tracking references

- `/home/sharpninja/github/McpServer/AGENTS.md`
- `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml`
- `/home/sharpninja/github/McpServer/.github/copilot-instructions.md`
- `/home/sharpninja/github/McpServer/docs/Development-Process-draft-v4.md`
- `/home/sharpninja/github/McpServer/docs/Project/Requirements-Matrix.md`
- `/home/sharpninja/github/McpServer/docs/Project/Functional-Requirements.md`
- `/home/sharpninja/github/McpServer/docs/Project/Technical-Requirements.md`
- `/home/sharpninja/github/McpServer/docs/Project/Testing-Requirements.md`
- `/home/sharpninja/github/McpServer/docs/Project/TR-per-FR-Mapping.md`
- `/home/sharpninja/github/McpServer/docs/context/todo-schema.md`
- `/home/sharpninja/github/McpServer/docs/context/session-log-schema.md`
- `/home/sharpninja/github/McpServer/docs/context/module-bootstrap.md`

The earlier context noted a targeted read of technical requirements lines 1190-1228. Recheck current contents rather than treating those line numbers as immutable.

Protect `/home/sharpninja/.mcpServer/failsafe/cline-v2/` and its session snapshots. Do not clear it or replay it as part of this task. Never copy marker API keys or other credentials into the amendment, handoff, or TODO.

## 5. Settled HV requirements: preserve all of these

### Foundation and authorization

1. A future implementation worktree must be created from `develop`, not from the foundation worktree. Do not create that implementation worktree merely to write the amendment.
2. Build on the specified foundation architecture and incorporate the necessary session-log schema/contracts from `session-20261009`.
3. Checking integration requirements and validating links are agent preparation, not unresolved user decisions.
4. Current authorization covers documentation and plan persistence only. Saving either must not be presented as implementing or completing HV.

### Independence, reviewers, and scheduling

5. Self-review grading is independent of HV. Its presence, absence, grade, and status must not influence HV admission, execution, or verdict.
6. Configure five reviewers: Cline, Copilot, Claude, Codex, and Grok.
7. Select eligible reviewers round-robin. Normalize identities and aliases and exclude the submitter and every alias of the submitter.
8. Not every request requires all five reviewers. Do not silently turn the configurable reviewer pool into a mandatory five-reviewer quorum.
9. NONCE means checking an agent's availability and ability to execute HV. It is not merely a liveness ping or a repetition of the server health nonce handshake.
10. Scheduling and work ownership must survive restarts: durable queue, leases, fencing, and restart-safe round-robin state.

### Scope and evaluation

11. Support individual turns, inclusive turn ranges, and session snapshots captured at admission time.
12. Produce per-turn assessments and a separate holistic evaluation. The holistic evaluation is not a mathematical aggregation of the turn scores.
13. Preserve the complete accepted requests and complete response JSONL logs without silent truncation.

### Evidence, retention, and retrieval

14. Store result logs GZIP-compressed at rest as `.jsonl.gz`, losslessly.
15. Validate compression/decompression round trips and reject corrupt or incomplete evidence before finalization.
16. Support compressed retrieval without permanent uncompressed duplicates and preserve live streaming.
17. Retain evidence until explicitly told to remove it. No automatic expiry or retention-window cleanup is allowed.

### Audit, access, finalization, and completion

18. There are no access roles. Do not introduce HV-specific role-based gates. Actor names in use cases describe participants, not authorization roles.
19. Required canonical database auditing is mandatory. Audit failure is a hard stop; local or failsafe logging is not a substitute.
20. Finalization is atomic. Final results and reviewed turns are immutable; remediation creates new work rather than rewriting reviewed history.
21. HV verdicts must never automatically complete TODOs.
22. Link verification and plan persistence are agent responsibilities. Do not return them as outstanding user questions.

## 6. Consolidated delivery outline

Required dependency order:

**H0 -> H1 -> H2 -> H3 -> H7 -> H4 -> H5/H6 -> independent validation**

### H0: Foundation and integration baseline

- Inspect the foundation architecture, required session-log changes, current code, and existing task/requirement mappings.
- Reconcile interfaces, schemas, boundaries, and requirement ownership; verify audit readiness.
- Persist the architecture-derived plan and high-priority TODO through the permitted canonical mechanism.
- Do not overwrite the foundation plan or create duplicate tasks.

### H1: Reviewer configuration and readiness

- Define reviewer configuration, identity normalization and alias exclusion, and eligibility.
- Define NONCE readiness checks and execution adapters for Cline, Copilot, Claude, Codex, and Grok.

### H2: Admission and scheduling

- Define immutable admission snapshots for individual turns, inclusive ranges, and session selections.
- Define durable queueing, leases, fencing, and restart-safe round-robin scheduling.

### H3: Execution lifecycle

- Define execution lifecycle, complete output capture, cancellation, and timeout handling.
- Capture receipts and provenance; reject stale-worker actions and stale-fence writes.

### H7: Evidence and finalization, before external result surfaces

- Require complete per-turn assessments and a separate holistic evaluation.
- Define GZIP-at-rest storage and round-trip/completeness validation.
- Define canonical grading contracts without making self-review a prerequisite or verdict input.
- Define atomic immutable finalization and reliable notifications.

### H4: REST surfaces

- Define submission, queries, results/output retrieval, cancellation, and reviewer status.
- Report unknown ETA honestly rather than inventing estimates.

### H5: Client and tooling parity

- Define typed client, REPL, Director, MCP/helper parity, and documentation.
- Follow the foundation's PowerShell-only direction for scripts and script tools.
- Documenting future MCP interfaces does not authorize invoking the currently disabled plugin.

### H6: Operations and recovery

- Define reconnectable SSE, incidents, recovery, NONCE checks, and canonical audit operations.

### Validation and completion gate

- Follow: failing behavioral test -> mocks -> implementation/refactor -> all required tests and applicable integration/parity checks -> independent verification.
- The complete required unit suite covers the current iteration and previous iterations.
- Zero failures, zero skips, and no omitted required tests. Deferred work belongs in TODO/requirements state, not skipped placeholders.
- Stop implementation slices at `Validating`; the coordinator decides completion.
- Do not prematurely use the new HV pipeline to validate itself.
- These are future delivery requirements, not claims that implementation or validation has occurred.

## 7. Architecture-amendment completion brief

This section is a coverage checklist for the next agent, not an already-approved concrete interface design. Derive actual names, fields, endpoints, errors, IDs, and transaction boundaries from the foundation and verified repository contracts. Do not present invented APIs as existing APIs.

### Descriptive architecture and decisions

- State amendment scope, status, relationship to the foundation, and precedence for any explicitly amended clause.
- Describe problem domains, component ownership, data ownership, invariants, trust boundaries, lifecycle, and failure/recovery behavior.
- Explain the rationale and alternatives for design choices. Separate settled user requirements from proposed design resolutions.
- Reconcile self-review independence with any existing grading, review, completion-gate, or agent-context dependencies.
- Explain how atomic database finalization relates to externally stored compressed evidence and reliable notifications. Identify crash windows and recovery behavior rather than assuming a single transaction covers unrelated storage systems.
- Define snapshot membership and reviewed-turn immutability precisely, including remediation and concurrent mutation handling.

### Models, interfaces, and unit-of-work catalog

- Inventory every model and every CQRS command/query and its handler; distinguish handlers from policies and orchestration processes.
- Define every request, response, validation rule, error contract, and endpoint in scope, including internal units needed by workers and recovery.
- Cover reviewer configuration/readiness, identity normalization, submission/admission, scheduling/lease ownership, execution/capture, cancellation/timeouts, evidence verification, grading, finalization, result retrieval, streaming/reconnect, notifications, and audit operations.
- For each interface, specify workspace routing, required/optional fields, invariants, version/concurrency checks, idempotency, retry semantics, cancellation, and terminal/nonterminal results where applicable.
- Specify storage/provenance references and completeness checks for accepted requests, JSONL output, per-turn assessments, holistic evaluation, and execution receipts.
- Keep public REST, typed client, REPL, Director, and MCP/helper contracts consistent. Define exact error mappings after inspecting existing patterns.
- Define unknown ETA behavior explicitly. Do not fabricate endpoint availability or deployment status.

### Diagrams: match the foundation's rigor

- Include component/context and lifecycle diagrams sufficient to explain the full design.
- Include grouped entity diagrams and the relationships between groups.
- Provide one complete Mermaid activity diagram for every unit of work, with meaningful labels, all decisions, and every typed success/error exit.
- Define shared-component activities once and use explicit calls from unit diagrams rather than duplicating inconsistent logic.
- Provide sequence diagrams for submitting each request type and processing every response branch, including validation errors, unavailable reviewers, cancellation, timeout, stale ownership, audit failure, corrupt/incomplete evidence, and recovery where applicable.
- Distinguish unit-of-work diagrams from policy and orchestration diagrams.

### Use cases (UC)

- Give each use case a stable, verified identifier and descriptive name.
- Define participants, triggers, preconditions, main flow, alternate/error flows, postconditions, and invariant preservation.
- Cover successful and negative paths for each selection type, reviewer exclusion/readiness, fair scheduling, retries/restarts, cancellation, complete evidence, finalization, retrieval, SSE reconnection, notification recovery, and explicit retention/removal decisions.
- Explicitly cover missing or failed self-review not blocking HV, audit failure stopping progress, and verdicts never completing TODOs.
- Do not turn participants into access roles or add unrequested role-based gates.

### Acceptance criteria (AC) and traceability

- Use testable criteria with explicit Given/When/Then or equally precise precondition/action/outcome structure.
- Map each settled requirement to use cases, units/interfaces, diagrams, acceptance criteria, and required tests.
- Cover alias exclusion, round-robin persistence, snapshot consistency, stale-worker rejection, cancellation/finalization races, evidence completeness, lossless GZIP round trips, corrupt/truncated input rejection, and audit failures.
- Cover separate per-turn and holistic outputs, indefinite retention, no permanent raw duplicate, streaming preservation, immutable results/turns, and remediation as new work.
- Prove that varying self-review presence, grade, or status cannot change HV admission, execution, or verdict.
- Prove that no verdict path automatically completes a TODO.
- Verify requirement and task identifiers against their canonical records. Do not invent populated mappings or claim unverified links are valid.
- Preserve the zero-failure/zero-skip gate, independent validation, and coordinator-controlled completion.

## 8. Pending high-priority TODO

The earlier request was: **write plan to high todo**.

When a permitted, audited route is available:

1. Query existing tasks to avoid duplicating the HV plan or overwriting the foundation task.
2. Verify foundation dependencies and requirement links before assigning them.
3. Persist the complete corrected plan with priority `high`, linked to the architecture amendment once created.
4. Keep implementation unfinished. Neither saving documentation nor an HV verdict authorizes marking the implementation TODO complete.
5. Read the saved record back and compare priority, full plan text, dependencies, links, and every settled correction, especially GZIP, retention, auditing, and self-review independence.
6. Report the exact canonical TODO ID and retrieval/storage location only after durable persistence and verification succeed.

`/home/sharpninja/github/McpServer/docs/context/todo-schema.md` explicitly warns not to edit `/home/sharpninja/github/McpServer/docs/Project/TODO.yaml` directly. Repository instructions describe pluggable TODO storage, so that file's presence does not establish it as the authoritative active backend.

The session-log schema requires a durable receipt with `persisted: true`, `degraded: false`, and `queued: false`; a missing persistence flag or an optimistic queued acknowledgement is not durable-write confirmation. Revalidate the applicable runtime contract rather than assuming the installed service/client matches the documentation.

No TODO ID has been reserved by this handoff. No canonical requirements have been allocated or modified by this handoff.

## 9. Resume sequence

1. Read this handoff and the current workspace instructions. Establish a permitted, healthy canonical audit path without invoking or altering the disabled plugin contrary to the user's direction. If unavailable, report the precise blocker; do not claim persistence.
2. Read the full foundation architecture, original HV draft and corrections, foundation plan, relevant session-log contracts, and existing requirements/tasks.
3. Establish the amendment's correct location and numbering from the existing document structure. Preserve the foundation and other agents' work.
4. Create the documentation-only amendment with the scope and rigor in sections 5-7. Do not create an implementation worktree or implementation code.
5. Verify diagrams, references, UC/AC coverage, interface consistency, and requirement/task mappings. Use an independent review path that does not rely on the unfinished HV pipeline.
6. Persist or update the requested high-priority TODO through the permitted audited route, with complete corrected plan text and verified amendment/dependency links; read it back.
7. Report exact files changed, validation actually performed, canonical TODO ID if saved, and any remaining blockers. Do not mark implementation complete.

## 10. Handoff verification and limitations

- Source existence, the runtime policy text, and disabled flags were checked directly during this handoff.
- Historical plugin backups reportedly use suffix `.backup-disable-mcpserver-<UTC timestamp>`; backup contents and process shutdown were not reverified.
- The full original HV draft and full architecture were not reread; this handoff preserves the supplied consolidated context and explicitly records that limitation.
- No plugin invocation, canonical API write, queue replay, failsafe removal, YAML mutation, implementation worktree creation, code change, or commit was performed for this handoff.
- No canonical database audit receipt exists for this handoff. Do not describe it as a session-log substitute or as proof that the mandatory audit gate passed.
- Build/test results are not available: this is a local Markdown continuity record, not implementation.
