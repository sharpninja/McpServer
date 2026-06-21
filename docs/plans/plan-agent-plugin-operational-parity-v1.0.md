# Plan - Agent Plugin Operational Parity v1.0 (All 8 Plugins)

**Branch:** `feat/agent-plugin-parity-v1` (cut from `develop` after current release baseline)
**Depends on:** AGENT-PLUGIN-FEATURE-MATRIX.md (already created), AGENT-PLUGIN-AVAILABILITY.md, the shared REPL contract (stdio-tool-contract.json + workflow.* surfaces), and any open PRs touching marker / sessionlog / todo bootstrap.
**New FR:** FR-MCP-AGENT-PARITY-001 (drafted below)
**New TRs:** TR-MCP-AGENT-PARITY-010 through -030 (drafted below; to be inserted into docs/Project/Technical-Requirements.md)
**New Testing Requirements:** Additions to docs/Project/Testing-Requirements.md and a new contract test suite under tests/AgentPluginParity/

## Context (Why This Work Exists)

The McpServer value proposition is *durable, workspace-scoped continuity* across AI coding agents: every user message opens a session-log turn, every code edit is build-verified and recorded, TODOs and requirements stay in sync, GraphRAG knowledge accumulates, and outages never lose data (failsafe YAML cache + recovery).

Today this contract is only *partially* realized. The eight official plugins (`mcpserver-*-plugin`) have drifted:

- Some have rich automatic hooks + plan tracking (Claude Code, Copilot, Grok via manifests); others rely entirely on manual enforcement scripts that agents must be prompted to call (Cline, Codex, OpenCode).
- Enforcement script implementations have small but critical differences (scoped vs. global cache, self-heal logic, timeout handling, path resolution).
- Marker bootstrap + HMAC + nonce logic exists in both Bash/Pwsh and TypeScript ports with no shared source of truth or contract tests proving identical behavior.
- Offline cache/failsafe formats and flush semantics are similar but not guaranteed identical; recovery paths differ.
- Subagent/JSONL transcript capture (high-value for Codex-style agents) is present in only a subset.
- Native SKILL.md coverage and full tool surface exposure is inconsistent.
- Test coverage and documentation quality (ENFORCEMENT.md, READMEs, validation plans) vary widely.
- No single "parity harness" exists that can prove "if you run the same 100-turn workload through any plugin you get equivalent durable artifacts."

Result: Developers who switch agents (or teams that standardize on different ones) experience inconsistent audit quality, occasional lost turns on outage, weaker build gates, and incomplete TODO/requirements provenance. This directly threatens the viability of the entire McpServer continuity story.

Empirical confirmation (from feature matrix + component audit 2026-05-28):
- Only 4/8 plugins have full hook surfaces.
- Enforcement 3-phase scripts exist in 7/8 but with measurable drift (cache scoping, self-heal, REPL timeout).
- 3 plugins have zero or minimal SKILL.md.
- Subagent capture present in only 3.
- Device guidance only in codex.
- No cross-plugin contract tests.

V² check: This work is both viable (shared core + thin adapters is a well-understood pattern) and valuable (makes the continuity guarantee real for every user of the supported agents).

## Definition of Done for "Operational Parity v1.0"

When complete, the AGENT-PLUGIN-FEATURE-MATRIX.md will show "Full (or host-native equivalent)" with green check for every critical row across all 8 columns, backed by:

- A single, versioned, heavily tested shared enforcement + bootstrap + cache + REPL client core (published as npm package + PowerShell module where useful).
- Every plugin updated to the v1 core (or proven equivalent thin layer).
- Full 3-phase enforcement honored for 100% of user messages in every supported agent (via hooks where the host provides them, via mandatory prompt + script guidance otherwise).
- Identical observable behavior for marker trust, session turn lifecycle, build gates, failsafe write/flush/recovery, and core workflow.* calls.
- Subagent capture where the host SDK exposes the necessary events.
- Updated AGENTS-README-FIRST.yaml prompt language that names the exact minimum prompt additions required per agent.
- Passing parity harness (new test suite that drives each plugin through a canonical workload and asserts on the resulting session-log YAML, todo.yaml, requirements exports, and cache artifacts).
- All 8 plugins ship with matching version numbers, READMEs, ENFORCEMENT.md (or host equivalent), and Plugin-Validation-Testing-Plan.md.
- Zero regressions in any existing plugin-specific feature.

## Draft Functional Requirements (to insert into docs/Project/Functional-Requirements.md and wiki copies)

## FR-MCP-AGENT-PARITY-001
**Agent Plugin Operational Parity v1.0** — Every officially supported agent plugin (claude-code, claude-cowork, cline, cline-v2, codex, copilot, grok, opencode) SHALL deliver equivalent fidelity to the full AGENTS-README-FIRST contract and the McpServer workflow surfaces (sessionlog, todo, requirements, graphrag, workspace) so that developers obtain the same session continuity, build verification, outage resilience, and provenance guarantees regardless of which supported agent they use for a given workspace.

The server, REPL, and plugin ecosystem SHALL treat divergence in observable contract outcomes (missing turns, lost cache entries on reconnect, bypassed build gates, incomplete TODO/FR linkage, failed marker trust) as a first-class defect.

**Status:** 🔴 Planned (this plan)

**Covered by:** TR-MCP-AGENT-PARITY-010 (shared core), -011 (enforcement state machine), -012 (marker bootstrap contract), -013 (cache/failsafe parity), -020..-028 (per-plugin adapters), -030 (parity harness), updates to existing FR-MCP-035 (Agent Values), FR-MCP-0xx (marker), and the REPL tool contract.

## FR-MCP-AGENT-PARITY-002
**Shared Core for Agent Plugin Behaviors** — A single, versioned, cross-language (TypeScript primary + shell shims) core library SHALL encapsulate the marker trust handshake, per-turn enforcement protocol state machine, failsafe cache write/flush/recovery, and REPL client with bounded retries and circuit-breaker semantics. All eight plugins SHALL depend on (or vendor with identical behavior) the same core version for v1.0 and beyond. Plugin-specific code SHALL be limited to host SDK adapters, manifest declarations, prompt guidance, and packaging.

## Draft Technical Requirements (to insert into docs/Project/Technical-Requirements.md)

## TR-MCP-AGENT-PARITY-010
**Shared Enforcement + Bootstrap Core Library (v1.0)** — A new package `@sharpninja/mcpserver-agent-core` (or equivalent under the McpServer org) SHALL implement:
- Marker discovery (upward search for AGENTS-README-FIRST.yaml)
- HMAC-SHA256 signature verification using the workspace apiKey
- Nonce health challenge against /health?nonce=
- The canonical three-phase enforcement state machine (beginTurn on first user message of a turn, appendActions + code-verify after every source edit, completeTurn + stop-gate before emitting final response)
- Scoped per-workspace/per-session cache layout under `.mcpServer/failsafe/<agent>/` or `cache/workspaces/<key>/...`
- YAML pending queue + bounded retry flush (3 attempts, then failed/)
- ReplBridge with single-line JSON envelope, timeout, cancellation, and event streaming support
- Self-healing completeTurn when the agent could not call the workflow surface directly

The core SHALL be 100% covered by contract tests that any consumer (TS plugin or shell script) can invoke. Shell shims SHALL be thin wrappers that delegate to the same state machine logic (or call the TS core via a small host process when necessary).

All previous ad-hoc implementations in the eight plugins SHALL be replaced by (or proven bit-for-bit behaviorally equivalent to) this core in the v1.0 release.

## TR-MCP-AGENT-PARITY-011
**Per-Turn Enforcement Protocol State Machine** — The three phases defined in existing ENFORCEMENT.md documents (claude-code, cline, codex, etc.) SHALL be formalized as a single state machine in the core with explicit states (NoTurn, TurnOpen, EditsInProgress, TurnComplete, BlockedOnBuild, BlockedOnMissingComplete) and transitions that are identical across all hosts. The stop-gate decision output format SHALL be standardized.

Hosts without native hook points (Cline, Codex, OpenCode) SHALL document the exact minimal prompt text an agent must receive so it reliably calls the three script entrypoints. Hosts with hooks SHALL wire the scripts/hooks to the same state machine.

## TR-MCP-AGENT-PARITY-012
**Marker Bootstrap + Trust Contract (identical across languages)** — The full bootstrap sequence (FindMarker → VerifySignature → HealthNonce → Set MCP_* env vars → Open initial session if possible) SHALL have a single source of truth (the TS implementation in the core) with a machine-readable contract test that the Bash/Pwsh ports produce identical side effects (env vars written, cache dir created, error codes on failure, MCP_UNTRUSTED logging).

## TR-MCP-AGENT-PARITY-013
**Failsafe Cache Format, Flush, and Recovery Semantics** — Pending writes, flush triggers (next call, session end, explicit flush command), retry limits (3), failure directory layout, and recovery/import behavior SHALL be identical for all plugins. The cache SHALL be scoped by workspace key (Base64URL of absolute path) and agent identifier. Recovery SHALL be idempotent and produce identical session-log and TODO artifacts as direct REPL calls.

## TR-MCP-AGENT-PARITY-020 through -027 (one per plugin)
Each existing plugin SHALL be updated to:
- Depend on (or vendor at identical version) the v1 core.
- Wire its host-specific entry points (hooks.json, AgentPlugin setup, package.json manifests, .claude-plugin, .codex-plugin, etc.) to the core enforcement + bootstrap.
- Add or update any missing native SKILL.md files so the full 5 core surfaces + workspace are available when the host supports SKILL.md.
- Add or complete ENFORCEMENT.md (or host-equivalent) that references the shared protocol.
- Implement (or document why impossible) subagent/JSONL capture using the host event if available.
- Pass the new parity harness for that plugin.
- Update version to the common v1.x parity release line and shipping manifests.

Specific per-plugin gaps (from 2026-05-28 audit) are enumerated in the Phases section below.

## TR-MCP-AGENT-PARITY-030
**Parity Harness + Golden Workload Tests** — A new test suite (tests/AgentPluginParity or equivalent) SHALL drive a canonical 100-turn workload (mix of TODO create/update, requirements, graphrag ingest/query, code edits that succeed and fail, simulated outage, subagent simulation where applicable) through each plugin's public surface (or a thin test harness that invokes the same scripts/hooks the real agent would). The harness SHALL assert:
- Identical session-log turn count and required fields (requestId format, decision categories, action types)
- Identical TODO canonical IDs and state after the workload
- Successful build-gate blocks on injected compile failures
- Successful cache write + flush + recovery with no lost mutations
- Marker trust success and MCP_UNTRUSTED behavior on bad signature

The harness SHALL be runnable against any plugin checkout and SHALL be part of each plugin's CI.

## Detailed TDD Test Plan (Byrd Process — Tests First)

This plan follows Martin Fowler's canonical definition of Test-Driven Development (https://martinfowler.com/bliki/TestDrivenDevelopment.html), using the Red → Green → Refactor cycle plus maintaining a running list of test cases.

Byrd adds specific process constraints on top of Fowler TDD to mitigate risks when using AI agents:
- Tests for the next small increment of behavior are written first.
- These tests are made green using only mocks/stubs before the corresponding real implementation is written.
- Refactoring occurs as part of the cycle.
- A phase is only exited when the relevant test suite (current work + prior iterations) remains fully green.

This section serves as the authoritative TDD guidance for the parity effort. Tests are derived from the FRs and TRs but are implemented in small, incremental slices per Fowler.

**Byrd Augmentations to Canonical TDD (Fowler):**
While following the Red → Green → Refactor cycle, Byrd adds AI-specific process gates for safety and auditability:
- Within a phase, write small, focused tests for the next increment of behavior (following Fowler).
- Validate those tests green using mocks/stubs before writing the corresponding real implementation code.
- Only after the mocks-validated tests pass do we implement the production behavior.
- Before a phase is considered complete, the full relevant test suite (current increment + prior work) must be green. This protects against regression across iterations.

### Test Strategy Overview
- **Unit / Contract Layer (Phase 1 priority):** Pure core library (marker, enforcement state machine, cache, ReplBridge). 100% coverage required before any plugin adoption.
- **Equivalence Layer:** Prove shell shims == TS core behavior.
- **Integration / Harness Layer (TR-030):** The Parity Harness is the primary integration test. It is the "golden workload" that all plugins must pass identically.
- **Per-Plugin Layer:** Thin adapter + host-specific surface tests + full harness execution.
- **Regression / Matrix Layer:** CI matrix on every core change.
- **Human Validation Layer:** Final acceptance (not automated unit tests).

All new tests must be added under `tests/AgentPluginParity/` (harness + golden workloads) and `tests/AgentPluginCore/` (shared core).

### Test Mapping to Requirements
- FR-MCP-AGENT-PARITY-001 (equivalent fidelity across agents) → Primarily validated by the Parity Harness + per-plugin integration tests + human validation.
- FR-MCP-AGENT-PARITY-002 (shared core) → Core Contract Tests + Shell/TS Equivalence.
- TR-MCP-AGENT-PARITY-010 (core library features + 100% contract tests) → Core Contract Tests.
- TR-MCP-AGENT-PARITY-011 (formalized state machine) → Enforcement state machine tests inside Core Contract Tests.
- TR-MCP-AGENT-PARITY-012 (identical bootstrap across languages) → Marker + bootstrap contract tests + equivalence tests.
- TR-MCP-AGENT-PARITY-013 (identical cache semantics) → Cache manager contract + recovery tests.
- TR-MCP-AGENT-PARITY-030 (Parity Harness) → The entire `tests/AgentPluginParity/` harness.
- TR-MCP-AGENT-PARITY-020..027 (per-plugin adoption) → Per-plugin integration tests + harness passage.

### Detailed Test Definitions with Acceptance Criteria

**1. Core Contract Tests (tests/AgentPluginCore/ — Phase 1, written first)**
   Frameworks: Jest (TS) + supporting PowerShell/bats for shell shim parity where needed.
   Mocks: Mock filesystem (fs-extra or memfs), mock child_process for REPL, mock http for /health nonce, in-memory cache.

   - MarkerResolver tests
     - AC: Correctly finds AGENTS-README-FIRST.yaml walking upward.
     - AC: Succeeds on valid HMAC signature.
     - AC: Fails with MCP_UNTRUSTED on bad signature (exact error code and message).
     - AC: Performs nonce challenge and validates response.

   - EnforcementStateMachine tests
     - AC: Transitions correctly through all defined states (NoTurn → TurnOpen → EditsInProgress → TurnComplete, BlockedOnBuild, BlockedOnMissingComplete).
     - AC: Blocks on `lastBuildStatus: failed` after code edits.
     - AC: Blocks on `status: in_progress` at stop-gate.
     - AC: Self-heal path for completeTurn works when agent cannot call the surface.
     - AC: No "escape hatch" allows closing a turn with failed build (mutation/property tests).

   - CacheManager tests
     - AC: Writes pending entries with correct scoped layout (workspace key + agent).
     - AC: Flush logic (opportunistic, session-end, manual) succeeds within retry limit (3).
     - AC: Recovery replays writes identically to direct REPL calls (golden comparison).
     - AC: Outage simulation: writes survive simulated server down, flush on reconnect.

   - ReplBridge tests
     - AC: Sends single-line JSON envelopes.
     - AC: Handles streaming events, cancellation, timeouts, and error envelopes correctly.
     - AC: Bounded retries and circuit-breaker behavior on repeated failures.

   **Acceptance Gate for Phase 1:** 100% line + branch coverage on all above. All tests green against mocks only. No real REPL or filesystem calls in these tests.

**2. Shell / TS Equivalence Contract Tests (Phase 1)**
   - For every public behavior in the TS core, there is a corresponding test that invokes the equivalent shell shim and asserts identical side effects (stdout, files written, env vars, error codes, cache contents).
   - AC: Marker resolution, health nonce, enforcement phases, cache flush, and recovery produce bit-for-bit or semantically identical results.

**3. Parity Harness (tests/AgentPluginParity/ — TR-030, Phase 2, the crown jewel)**
   Single harness that any plugin can target (via thin adapter or direct script invocation).

   Golden workload (100 turns):
   - Mix of workflow.todo.*, workflow.requirements.*, workflow.sessionlog.*, workflow.graphrag.*, and workspace calls.
   - Deliberate successful and failing code edits (triggers build verification).
   - Simulated outage (core writes to cache only).
   - Subagent simulation where the plugin supports it.

   Assertions (must pass for every plugin):
   - Identical session-log turn count and required fields (requestId format, decision categories, action types, appendActions).
   - Identical TODO canonical IDs and final state.
   - Build gates correctly blocked on injected failures and allowed after fixes.
   - Cache write + flush + recovery produced exactly the same final artifacts as direct REPL path (no lost mutations).
   - Marker trust success + correct MCP_UNTRUSTED behavior on bad signature.

   AC: The harness is runnable from any plugin checkout (via env var or CLI flag) and is executed in each plugin's CI on every change to the shared core.

**4. Per-Plugin Enforcement Integration Tests**
   For each of the 8 plugins:
   - Thin test that drives the host's entry point (hook simulation, AgentPlugin setup, script invocation, etc.).
   - Must exercise the full three-phase protocol end-to-end.
   - Must pass the full Parity Harness with zero divergent artifacts from the golden run.
   - AC: For hook-rich agents (Claude Code, Copilot, Grok), the hooks produce the same observable MCP side-effects as manual script invocation.
   - AC: For enforcement-script agents (Cline, Codex, OpenCode), the three scripts produce correct state and call the core correctly.

**5. End-to-End Harness Matrix & Regression (Phase 11 + ongoing)**
   - CI job that runs the full Parity Harness matrix against all 8 plugins on every push to the core.
   - AC: Zero divergent artifacts. Any divergence fails the build.

**6. Human Validation (Phase 11 — final acceptance, not unit test)**
   - Run the golden workload manually in at least Claude Code, Cline, Codex, Grok, and Copilot.
   - Inspect resulting session-log, todo.yaml, requirements, and cache artifacts.
   - AC: Audit quality and resilience feel indistinguishable across agents. Recorded sign-off in session log.

### Test Implementation Order (Tied to Byrd Phases)
- **Phase 0 (now):** Add all failing test skeletons (marked skip or NotImplemented) for Core Contract Tests, Parity Harness skeleton, and one "parity gap" test per plugin.
- **Phase 1:** Write and make green (mocks only) the entire Core Contract Tests + Equivalence tests before touching any real core implementation.
- **Phase 2:** Write and validate the Parity Harness against a mock core/plugin.
- **Phases 3-10:** For each plugin, add the per-plugin integration tests and make them pass the harness before merging the plugin adoption PR.
- **Phase 11:** Add/execute the full CI matrix and human validation.

All tests for the current phase + every previous phase must be green (plus the full existing McpServer test suite) before any implementation code for that phase is written.

This TDD plan, combined with the Test Acceptance Criteria already in the TODO technicalDetails, constitutes the complete testing strategy for the parity effort.

## Phases (Byrd Process — Tests First, Mocks Validated, All-Tests-Green Gates)

**Rule for every phase exit:** The entire test suite (new tests for this phase + all previous phases + the full existing McpServer + plugin suites) must be green. No implementation code for a phase is written until its acceptance-criteria unit tests pass against mocks/stubs.

### Phase 0 — Planning Artifacts + Failing Test Stubs + TODO Bootstrap (PLAN phase of this work)

- Create this plan document (done).
- Insert the drafted FR-MCP-AGENT-PARITY-001/002 and TR-MCP-AGENT-PARITY-010..-030 blocks into the authoritative docs/Project/Functional-Requirements.md, Technical-Requirements.md, Testing-Requirements.md, TR-per-FR-Mapping.md, and the wiki/azure copies.
- Create the PLAN-AGENTPARITY-001 (this overall effort) and child TODOs (one per major phase or per-plugin gap closure) using the proper McpTodo interface from a trusted workspace that passes marker signature + nonce (or record the handoff YAML if untrusted).
- Cut the feature branch.
- Phase 0 (setup only):
  - Create directory structure under `tests/AgentPluginCore/` and `tests/AgentPluginParity/`.
  - Add initial failing test list / skeleton files (following Fowler's practice of writing out test cases first). Real incremental test writing occurs in Phase 1.
- Phase 1 (the real TDD work, following Fowler Red-Green-Refactor, done before real implementation):
  - Write small, focused tests for the next increment of core behavior (per the Detailed TDD Test Plan).
  - Validate them green using mocks/stubs from the beginning.
  - Only after those increment tests are green against mocks do we implement the corresponding production code.
  - Refactor as needed while keeping tests green.
  - Add equivalence tests proving shell shims match the TS core behavior.
- Commit: "plan(agent-parity): Byrd PLAN artifacts + failing stubs for FR-MCP-AGENT-PARITY-001 (TR-010..030)"
- Gate: Plan reviewed and approved; all stub tests "fail as expected".

### Phase 1 — Shared Core Library (Implementation of TR-010, -011, -012, -013)

- Write the full unit test suite for the core (marker verification with good/bad signatures, health nonce happy/sad paths, enforcement state machine transitions including build-failure and missing-complete blocks, cache write/flush/recovery with outage simulation, ReplBridge envelope/cancellation/timeout).
- Validate 100% of those tests using mocks (mock filesystem, mock child_process for REPL, mock http for health).
- Only after all core tests are green against mocks: implement the TS core library.
- Implement (or port with identical behavior) the shell shims (marker-resolver.sh, repl-invoke.sh, cache-*.sh, the three enforcement scripts) as thin callers into the core or proven-equivalent logic.
- Add contract tests that prove shell behavior == TS core behavior.
- Publish the core as npm package (alpha) and update the PowerShell module if needed.
- Gate: All core + contract tests green; no plugin code changed yet.

### Phase 2 — Cross-Cutting Updates (AGENTS-README-FIRST prompt, parity harness, docs)

- Implement the full parity harness (TR-030) against the new core (first target: a mock plugin).
- Update the AGENTS-README-FIRST.yaml template/prompt text (in McpServer) with the exact minimum per-agent prompt additions required for enforcement in hook-poor hosts.
- Update AGENT-PLUGIN-AVAILABILITY.md and the feature matrix with "Parity v1 target" columns.
- Add golden workload YAML artifacts under tests/AgentPluginParity/goldens/.
- Gate: Harness runs and produces consistent artifacts against the core mock; docs updated.

### Phases 3–10 — Per-Plugin Adoption (one phase per plugin or logical grouping)

For each plugin in priority order (suggested: codex + grok first because of subagent value, then the two clines, copilot, claude-code, claude-cowork, opencode):

- Update the plugin to depend on the exact v1 core version (or copy the proven shims).
- Replace or wrap all ad-hoc marker/cache/enforcement logic with calls to the core.
- Add any missing native SKILL.md files or tool registrations so the full 5 core surfaces + workspace are present.
- Add/complete ENFORCEMENT.md (or host equivalent) that points at the shared protocol.
- Implement host-specific subagent capture (if the SDK provides events) using the core recovery helpers.
- Wire hooks.json / AgentPlugin setup / .*-plugin manifests to the core entrypoints.
- Update packaging, version, and shipping scripts.
- Write or expand the per-plugin integration tests that exercise the three phases + outage recovery using the real (or mocked) host surface.
- Run the full parity harness against this plugin; all assertions must pass.
- Update the plugin's Plugin-Validation-Testing-Plan.md with parity v1 checklist.
- Specific per-plugin work items (examples — final list lives in the TODOs):
  - **Codex**: Adopt core for its scoped cache layout + JSONL enrichment; add device/workflow guidance as thin wrappers over core; ensure session-start.sh etc. delegate to core.
  - **Cline + Cline v2**: Make the 3 lib/*.sh scripts thin shims over the core (or call the TS core); ensure V2 plugin registers the enforcement tools if they become MCP-callable; complete workspace + full surface parity.
  - **Claude Code / Grok / Copilot**: Replace their current lib/ scripts with the v1 shims; ensure subagent-import and plan-*.sh call the core; verify hooks.json still produces identical side-effects.
  - **Claude Cowork**: Ensure its special handoff + .mcp.json stdio path still honors the identical cache and enforcement state machine; update the cowork-contract.md.
  - **OpenCode**: Port or wrap its enforcement (currently weakest) to the core; add the three script entrypoints or equivalent TS hooks; produce ENFORCEMENT.md.
- Gate for each plugin phase: That plugin's full test suite + parity harness run = 100% green; no regressions in its unique features (subagent for Codex/Grok, Cowork packaging, etc.); matrix row for that column now shows Full for all critical features.

### Phase 11 — End-to-End Validation + Release Preparation

- Execute the parity harness matrix in CI against all 8 updated plugins on the same golden workload; all must produce byte-compatible (or semantically identical) session-log, todo, requirements, and cache artifacts.
- Human validation: 2–3 developers run real workloads in at least 4 different agents against the same workspace; sign off that audit quality and resilience feel identical.
- Update all 8 plugin READMEs, version numbers, and changelogs with "Operational Parity v1.0".
- Cut release tags / publish the core package + all plugin packages.
- Update AGENT-PLUGIN-FEATURE-MATRIX.md and AVAILABILITY.md with final "v1.0 Achieved" status and links to the parity harness results.
- Gate: All tests green across the entire ecosystem; human sign-off recorded in session log; no open P1/P2 parity defects.

### Phase 12 — Iteration Closeout + Next Planning

- Create the retrospective TODOs / issues for any remaining drift or host-specific enhancements (e.g. "make enforcement tools first-class MCP-callable for Cline v2").
- Archive the golden artifacts and harness results.
- Begin planning v1.1 (e.g. enforcement as pure MCP tools where hosts allow, richer subagent schemas, etc.).

## Traceability and TODO IDs

All work items SHALL use canonical IDs:
- PLAN-AGENTPARITY-001 (overall)
- PLAN-AGENTPARITY-010 (core library)
- PLAN-AGENTPARITY-020 (claude-code adoption)
- ... through 027 for the others
- IMP-AGENTPARITY-xxx for implementation sub-tasks once in the implementation phase
- TEST-AGENTPARITY-xxx for test work

These will be created in the MCP TODO system (via proper McpTodo.psm1 / skill / REPL) at the start of Phase 0 from a fully trusted workspace.

**Note (2026-05-28):** Per user request, all detailed child work items were consolidated into the single master TODO `PLAN-AGENTPARITY-001`. The parent now contains a rich `implementationTasks` list covering every phase and per-plugin item. All other `PLAN-AGENTPARITY-*` items were marked done with a consolidation note. Only one active TODO remains.

## Risk Mitigation / Rollback

- The shared core is introduced behind the v1 version line; old plugin versions continue to work.
- Each plugin adoption is a separate PR that can be reverted independently.
- The parity harness acts as a permanent regression detector.
- If a host SDK change breaks a thin adapter, the plugin can temporarily vendor the last-known-good core version while the adapter is repaired.

## Success Metrics (measurable at Phase 11 gate)

- 8/8 plugins pass the full parity harness on the golden workload with zero divergent artifacts.
- Zero "missing turn" or "unverified build shipped" defects reported from any of the 8 agents for 30 days post-release.
- Developer feedback (in session logs or surveys) that "switching agents no longer changes my audit story."

## Related Documents

- AGENT-PLUGIN-FEATURE-MATRIX.md (baseline and target)
- AGENT-PLUGIN-AVAILABILITY.md
- Individual plugin ENFORCEMENT.md / READMEs / GROK-USAGE.md / cowork-contract.md
- docs/context/session-log-workflow-api.md and todo-schema.md
- stdio-tool-contract.json (REPL surfaces that must be honored identically)
- Existing ENFORCEMENT.md files in the plugin trees (to be unified by reference to the core)

---

**This plan was created following the Byrd Development Process (Planning phase artifacts first, tests-first implementation in subsequent phases, mock validation before real code, all-tests-green gates, requirements-driven, MCP TODO/session traceability).**

**Next action for executor (in a trusted workspace that passes marker + nonce):** Initialize McpTodo, create the PLAN-AGENTPARITY-00x TODOs from this document, then begin Phase 0 execution.
## Proposed Wording Alignment with Fowler TDD (Added 2026-05-28)
See docs/plans/Proposed-TDD-Wording-Changes.md for specific before/after text changes made to the core Byrd document and this plan to reduce drift from Martin Fowler's canonical definition while preserving Byrd's AI-safety intent (mocks gate + regression protection).
