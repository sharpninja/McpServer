# BDPv4 Plan: Integrate Karpathy and Ponytail Across the Plugins

Status: proposed implementation plan; saving and committing this document does not approve implementation.

## Summary and decisions

Deliver one shared engineering policy combining Karpathy's decision discipline,
Ponytail's preference for the smallest complete solution, and workspace-selected
BDPv4. Include authoritative server enforcement of required completion evidence.

Decisions:

- Engineering guidance is **enabled by default**.
- BDPv4 applies where the workspace explicitly selects it.
- Cover all nine plugins: Claude Code, Claude Cowork, Cline, Cline v2, Codex,
  Copilot, Grok, Grok Bot, and OpenCode.
- Implement shared runtime behavior in PowerShell; host adapters translate events
  and results.
- Extend the existing session-turn/review/context architecture. Its required
  foundations are prerequisites for enforcement.
- Preserve documented host limitations; a capability is not "supported" without
  delivery evidence.

The transcript is the intent source: GraphRAG document
`adhoc-ffa040f73459499ba82ad8912e4046f3`, referenced by `MEMORY-PROJECT-001`.
Its source key is
`transcripts/compare-kaparthy-ponytail-bdpv4/0446942c2aac9ac4`, and its SHA-256 is
`0446942C2AAC9AC436017F51BE9F12B0F631FB233BB9F0482ACF5967267AF20C`.
Its cancelled hostile-validation attempt provides no approval or validation
evidence.

Repository inspection at `5187b7b6142427549a3c24f894c537a84b6792a1` found shared
distribution infrastructure and local completion checks, but not the documented
`TurnWritePipeline`, classification, review, and agent-context foundations. The
architecture amendment must precede implementation.

## 1. Policy and architecture contract

Use the [existing architecture document](../architecture/SessionTurn-Review-AgentContext-Architecture.md)
as the authority. Add this integration to its component, interface,
request/response, activity-diagram, sequence-diagram, and rollout definitions
before changing code. Apply [BDPv4](../Development-Process-draft-v4.md) throughout
delivery.

### Canonical policy

Maintain one versioned policy asset under `plugins/core`, containing baseline
rules and planning, implementation, review, and BDPv4 sections. Include its
version, content hash, source references, pinned upstream commits, and
attribution. Distribution copies are generated and checksum-protected.

Adopt these reconciled rules:

- Establish observable success criteria and surface ambiguity that changes scope,
  interfaces, or consequential behavior.
- Resolve routine implementation choices autonomously and record material
  assumptions.
- Inspect affected callers, tests, fixtures, configuration, and interfaces.
- Prefer existing project capabilities, then platform facilities and installed
  dependencies.
- Make the smallest change satisfying every acceptance criterion.
- Preserve required interfaces, validation, security, accessibility, and data
  integrity.
- Permit refactoring needed for the current increment; track unrelated cleanup
  separately.
- Apply the workspace's required verification process. A small self-check cannot
  replace BDPv4 acceptance coverage or cumulative gates.

These are adaptations of the
[Karpathy guidelines](https://github.com/multica-ai/andrej-karpathy-skills/blob/main/skills/karpathy-guidelines/SKILL.md)
and
[Ponytail rules](https://github.com/DietrichGebert/ponytail/blob/main/skills/ponytail/SKILL.md).
Preserve applicable notices when distributing adapted material; do not introduce
Ponytail personas, response formats, or session modes as separate plugin behavior.

### Policy resolution and delivery

- Resolve engineering guidance independently from development-process selection.
- Represent process selection explicitly as `None` or `BDPv4`, using the workspace
  configuration workflow. Existing workspaces default to `None`; register this
  workspace's declared BDPv4 selection during rollout.
- Deliver baseline guidance plus the applicable activity section once per
  invocation. Use the existing request classification: `Code`, `Docs`, and
  `Chore`; use planning/review context where the existing workflow provides it.
- A fresh invocation or context restoration receives actual instructions. A
  previously recorded policy ID is insufficient.
- Preserve required memories, hook fields, deadlines, and exit behavior.
- A policy read failure reports unavailable delivery and prevents a successful
  delivery claim. It does not silently select a weaker policy.
- Extend status with effective policy version/hash, selected process, delivery
  outcome, and host capability limitations.

## 2. Authoritative completion enforcement

Extend the existing `CompletionGate` inside `TurnWritePipeline`; do not create a
parallel completion service.

### Additive interfaces and records

Define and document:

- `EngineeringPolicyDescriptor`: version, hash, selected process, and applicable
  sections.
- `EngineeringEvidence`: policy reference, source snapshot,
  requirement/acceptance references, validation receipts, and existing review
  references.
- `EngineeringGateResult`: `Pass`, `Fail`, or `Unknown`, with ordered findings and
  remediation references.

Expose these through existing typed client, REPL, MCP, session-log, and status
surfaces. Store validation receipts and their links using the architecture's
existing evidence/relationship infrastructure.

### Gate behavior

- Preserve existing classification, review, write-origin, and terminal-state
  rules.
- For a completed `Code` turn in a BDPv4 workspace, require acceptance coverage,
  current cumulative validation evidence, and the applicable independent review.
- Require zero failed tests and zero skipped tests in every required executed
  scope.
- Validate receipt contents against raw runner artifacts: command, scope, exit
  code, totals, artifact hashes, and source snapshot. Model-authored "passed"
  flags alone are insufficient.
- Identify a source snapshot using the repository commit plus content hashes of
  affected working-tree files, including deletions and relevant untracked files.
  Tests and reviews must reference that same snapshot.
- Changes to covered code, tests, policy, requirements, or acceptance criteria
  make dependent evidence stale.
- Missing, stale, inaccessible, or contradictory evidence yields `Unknown` or
  `Fail` and blocks a completion claim.
- Preserve the canonical failed/cancelled-turn rules. Reporting a failure must
  not require inventing successful test or review evidence.
- Red-stage failures remain valid intermediate evidence. Enforcement occurs at
  the completion transition, not while the agent is proceeding from red to green.
- Apply the gate consistently to every external writer, including bulk submit,
  PATCH, PUT, completion tools, and recovery replay. Preserve the architecture's
  trusted import/replication exemptions.
- Reject atomically using the existing `session_turn_completion_gate` error
  contract, naming the missing evidence and affected request IDs.

Extend the existing reviewer criteria to assess completeness, justified
dependencies and abstractions, affected callers, preserved boundaries, and
evidence quality. Deterministic gates establish evidence sufficiency; independent
review assesses judgment.

## 3. BDPv4 execution phases

At implementation start, create the MCP parent TODO and dependency-linked slices
for this saved plan.

### Phase 1 - Requirements and architecture

- Query effective requirements for the active workspace layer.
- Register the engineering policy, delivery, evidence, enforcement, distribution,
  and evaluation requirements as linked FR/TR/TEST records with structured
  acceptance criteria.
- Amend the existing architecture and prerequisite TODOs. Reuse its shared
  components, review thresholds, and transport contracts.
- Freeze upstream source revisions and complete license review.
- Define the required validation inventory and source-snapshot algorithm.

**Exit:** complete traceability, documented interfaces and diagrams, independent
hostile validation, and operator approval. The cancelled review in the transcript
does not satisfy this gate.

### Phase 2 - Interfaces and compiling stubs

- Add the documented models, persistence mappings, configuration, PowerShell
  interfaces, and transport contracts.
- Add forward/backward-compatible migrations for new records.
- Extend distribution to package and verify the canonical policy.
- Include Grok Bot in discovery, generation, validation, and distribution
  inventories; the inspected build inventory currently omits it.

**Exit:** interfaces compile, serialization and migration contracts pass, and the
approved design matches the implemented public surfaces.

### Phase 3 - Unit tests validated with mocks

Write acceptance tests against the documented seams, validate them with mocks,
and record the real implementation behavior they will require.

Named test groups:

- `EngineeringPolicyResolutionTests`
- `EngineeringPolicyDeliveryTests`
- `EngineeringEvidenceValidationTests`
- `EngineeringCompletionGateTests`
- `EngineeringPolicyDistributionTests`
- `EngineeringPolicyHostContractTests`

**Exit:** complete acceptance coverage against mocks and a green cumulative unit
suite. Deferred implementation scenarios remain in MCP state; do not add skipped
placeholders.

### Phase 4 - Implementation slices

Execute in this order:

1. Canonical policy, source attribution, resolver, and status.
2. PowerShell delivery, context restoration, and stateless-package integration.
3. Evidence persistence, receipt validation, and staleness.
4. Server completion enforcement, after the existing completion/review foundations
   land.
5. All nine host adapters, packaging, integrity, and capability reporting.
6. Integration validation, behavioral evaluation, documentation, and rollout
   preparation.

For each slice: add the next real-behavior test, observe its expected failure,
implement to green, refactor, then run the cumulative gate before starting the
next slice. Update MCP requirements, TODOs, decisions, and evidence after
meaningful changes.

## 4. Validation and acceptance

Required scenarios:

- BDPv4 selected and unselected workspaces.
- Material ambiguity versus routine choices.
- Trivial edits, required interfaces, and changes spanning multiple callers.
- Red-stage tests, incomplete acceptance coverage, failures, and skips.
- Missing receipts, changed source snapshots, stale reviews, and policy changes.
- Duplicate hook registration, compaction, stateless execution, unavailable
  services, and deadline exhaustion.
- Every completion transport and write path, including attempted gate bypasses.
- Equivalent policy content across nine plugins, with documented host
  limitations.
- Migration Up/Down/Up and preservation of historical audit records.

Run repository gates through `pwsh.exe`: `./build.ps1 Compile`,
`./build.ps1 Test`, required integration/migration targets, and
`./build.ps1 PluginSessionLogIntegration`. Run the complete shared and host Pester
suites, distribution integrity checks, and PowerShell-only validation. Run
Build.Tests directly when build tooling changes.

Each receipt records the exact command, repository revision/source snapshot,
executed scope, totals, exit code, and artifacts. Run native host validation on
Linux and Windows. Every required gate has zero failures and zero skips.

Compare baseline and integrated plugins on the same representative tasks and
model settings. Assess correctness, completeness, and boundary preservation
first; then report rework, dependency additions, change size, cost, and latency
without assuming efficiency gains.

**Acceptance:** every requirement has current test and review evidence; all
supported hosts receive the same effective policy; BDPv4 completion refuses
insufficient evidence through every authoritative path.

## 5. Rollout, rollback, and assumptions

- Prepare compatible server changes first; keep new enforcement disabled during
  compatibility validation.
- Deploy server support, distribute one coordinated plugin version, and verify
  installed content against checksums.
- Enable enforcement on isolated development instances, then staging, then
  approved production workspaces.
- Inventory open turns before enforcement activation. Require genuine missing
  evidence before those turns complete; do not backfill passing receipts.
- Roll back enforcement through its configuration switch while retaining records
  and read access. Restore plugin packages together; use only tested migration
  rollback paths.

Planning defaults: extend the existing architecture rather than rebuilding its
foundations independently; keep guidance default-on; preserve workspace process
selection and existing review independence.

Research provider: Perplexity unavailable - primary-source fallback used. This
plan requires architecture review and approval before implementation; no test or
hostile-validation result is claimed by the plan itself.
