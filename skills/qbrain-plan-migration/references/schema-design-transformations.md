# Transform a legacy design to fit the BDPv4 plan schema

Use this guide with the exact [template](../../../docs/plans/BDPv4-QBrain-Implementation-Plan-Template.yaml) and [schema](../../../docs/plans/BDPv4-QBrain-Implementation-Plan.schema.json) selected for the migration. For each family below, compare the legacy design with the canonical rule and record the source anchor, selected transformation, destination path, affected scope, authority, and remaining evidence. Classify each as **preserve**, **representation-only**, **material change proposed**, or **unresolved**. Show what changes behavior, ownership, sequencing or acceptance; do not imply those changes were already approved. If a requirement conflicts with the canonical design, keep the legacy decision and evidence in the ledger, show the proposed canonical alternative and its tradeoff, and leave the candidate blocked until the right authority decides. Never edit the schema to make the legacy design pass.

This is a design migration, not implementation. Run schema, reference, ownership, and ledger checks as preflight. Do not prepare an HV packet or request HV until migration is complete and the exact candidate is pinned. Shape validation does not establish semantic readiness, human approval, or a passing gate.

## 1. Format, identity, and source location

<!-- plan: schemaVersion kind metadata identity -->
<!-- defs: nonEmptyText nullableText textList canonicalRef phaseId todoId artifactId gateId artifactVersionId sha digestOrRevision sliceRef -->
<!-- approval: 0 1 -->

- Separate a read-only v0.7 interpretation from a requested v0.8 conversion. Keep `BdpImplementationPlan` and the selected version. Normalize legacy TODO fields in memory first; a v0.8 conversion changes all definition, phase membership, edge, tracking and example references together. Never combine legacy and typed fields or rewrite historical receipt IDs.
- Pin the workspace and umbrella TODO to trusted readbacks. Reuse the actual repo-relative `docs/plans/*.yaml` or `.yml` destination and valid `PLAN-…-NNN` ID. Preserve the original path/ID as provenance if a new canonical path or ID is needed. Give a transformed plan its own revision; a branch name or current checkout is not a pin.
- Use stable phase IDs in the canonical uppercase/hyphen/three-digit form, lower-kebab gate IDs, canonical artifact IDs, distinct version IDs, and the real TODO ID or canonical lower-case Task Group UUID. A checklist ordinal, display title or example UUID is not a slice identity. Publish an alias/crosswalk instead of mutating legacy identifiers. Check collisions, internal syntax, exact ownership, and all inbound/outbound references separately. Deduplicate lists by identity without losing order or differences in meaning.
- Use null or an empty collection only where the draft schema permits it and it truthfully means unknown/not applicable; state which in the ledger. Never turn placeholder text into an ID or a pin. Approval-candidate checks require substantive metadata, not a convincing filename.

## 2. Outcome, ship boundary, and accountable roles

<!-- plan: intent roles transition -->
<!-- approval: 2 3 14 15 -->

- Split legacy narrative into problem, measurable behavior and success criteria, viability/value, named users, inclusions, exclusions, changed existing behavior, compatibility and external dependencies. Link scope and decisions to authority. Don't infer user groups, metrics, acceptance or release commitments from implementation notes.
- Select `implementation`, `validation`, or `release` using the approved delivery boundary. A “done” heading or a reference to staging does not expand the boundary. Keep downstream gates declared where canonical but not applicable when outside scope. Only a release candidate fills transition inputs: exact release artifact, environment readiness, migration and rollback, go/no-go authority, post-deploy verification and operations handoff. Keep these out of the unit-test or implementation gate.
- Name accountable human, implementation and validation owners and independent reviewer separately by authority; do not map every role to the source author or treat self-review as independence. For release, resolve a release owner. Missing accountable roles block the relevant candidate rather than becoming invented approvals.
- Classify UI impact as `changes-ui` or `none` based on observable behavior; null is an unresolved draft. For `none`, include the rationale reference; do not choose it to skip missing UI design. See the UI transformation below for changed screens or flows.

## 3. Authority, baselines, ownership, and policy references

<!-- plan: authority ownership references -->
<!-- defs: sourceBaseline -->
<!-- approval: 4 5 7 -->

- Extract the applicable BDP and workspace policy revisions plus deliberate tailoring decisions from the source. If requirement text is inline, link each authoritative catalog item and its revision or a verified snapshot and digest. Propose uncatalogued requirements in the owning source; a migrated copy is not the requirement authority.
- Pin architecture document references and repository commits (full supported SHA), with build and unit-test configuration refs. Resolve the exact revisions, content and applicability independently. Keep design tools, process precedents and standards as policy references, not evidence that a project complies with them. If a source uses a floating URL, identify the immutable equivalent or record a blocker.
- Assign one source of truth for plan, requirements, architecture, UI design, artifact DAG, TODOs/native work, QBrain session logs, versioned outputs and projections. Transform duplicate prose into links. Don't overwrite source-of-truth requirements from a plan or feed a generated snapshot back into live task state.
- An old plan approval, approval-like status or completed checklist belongs to its original revision. Leave the new approval unset until the owner approves the exact new candidate. The execution-candidate shape only tests for an approval reference; verify authority and actual revision match.

## 4. Re-express architecture as C4 and apply CQRS only where authorized

<!-- plan: architectureTailoring -->
<!-- defs: c4Model -->
<!-- approval: 6 -->

- Translate existing diagrams and architecture prose to one versioned **Structurizr DSL** model: actors and external systems in System Context, execution/deployable units and stores in Container, Component views where warranted, and supplemental views for needed flows. Give elements/views stable keys, preserve boundaries and dependency directions, link affected requirements and slices to elements, and record model/workspace revision, tool version, rendered views and coverage mapping. Convert semantics, not just the picture. A Mermaid/Visio image is useful input, not a Structurizr workspace or render receipt. Before closing the C4 gate, validate the exact revision and confirm the linked views render.
- If the approved architecture uses QBrain CQRS, split each application unit of work into one command/query and handler with versioned request/response/error and endpoint contracts; preserve transaction and ownership boundaries, link required activity/sequence diagrams, and define shared rules once. A delivery slice may group related units. Propose material restructures (e.g., breaking a multi-purpose handler) with their impact; do not force unrelated legacy architectures to adopt CQRS to populate this conditional field.

## 5. Convert changed UI into the canonical artifact set

<!-- defs: uiDesign -->
<!-- approval: 13 -->

- Register the actual use-case catalog and stable IDs, make SVG wireframes for the states/screens rather than renaming raster files, and turn prose or screenshots into an AssetReview `_manifest.md` with stable storyboard IDs and reviewable frames. Preserve alternate/error/empty and permission branches and navigation, not only the happy path. Trace each affected use-case step/branch to its storyboard frame, wireframe, FR/AC and owning slice; path-walk the required flows.
- Record UI design revision and operator approval of the reviewed scope separately from use-case lookup and frame/branch trace. A tool status such as Draft neither proves approval nor creates a separate use-case approval gate. Record missing approval, use-case identity, SVG validity, manifest/frame/branch link and path-walk evidence as distinct gaps. Preserve verified original assets and links in the ledger; unresolved UI work blocks the affected gate, not the creation of a draft.

## 6. Recut phases and slices around verifiable behavior

<!-- plan: delivery -->
<!-- defs: phase slice acceptanceTrace -->
<!-- approval: 8 9 -->

- Recut milestones into phases with a canonical ID, objective, risk retired and explicit exit gates. Keep one inline `defaultSlice` for an unsplit phase or a nonempty version-appropriate explicit list referencing top-level definitions for meaningful subdivisions; never both. Omit the top-level list if all phases are inline. Do not split solely to manufacture progress or flatten real dependencies. Each slice needs an owner, bounded behavior and its own gate list.
- In v0.7, retain TODO-only identities; in v0.8, resolve exactly one TODO or native Task Group identity and reuse it everywhere. Verify existing groups by workspace/umbrella and membership. If native tools are unavailable, follow [TODO-backed provisioning](todo-provisioning.md) with exact readback and operator-authorized creates only. A phase charter can be drafted while unbound but is not a schema-ready slice. Preserve the meaning of legacy checklist rows and any split/merge through the ledger.
- Separate functional, technical, test and use-case refs; attach versioned architecture, C4 elements and UI trace where applicable. Qualify each criterion by owning requirement and map it to test requirement IDs, unit tests and separate validation refs. Rename or split a criterion only in its owning authority. Preserve the expected behavior and negative paths; do not substitute a test name or evidence from a different scope.

## 7. Transform implicit dependencies into a versioned artifact DAG

<!-- plan: artifactDependencies -->
<!-- defs: artifactMap artifact artifactVersion edge -->
<!-- approval: 10 11 12 -->

- Register each logical artifact once with owner and applicable kind (requirements, architecture, C4, use case, wireframe, storyboard, code, test, package, evidence, or justified other). Template placeholder kinds are not real design selections. Separate artifact identity from version; a baseline has a verified immutable revision/digest, while a planned slice output records its producer and is not available until verified.
- Translate “uses” and “delivers” statements into typed consumes/produces edges for the exact version, slice, gate and purpose. Put edges at the gate that genuinely needs or produces that version. Use one producer per output, check producer-before-consumer, cycles and dangling refs, and supersede only within the same artifact. A change consumes the prior version and produces a distinct version; derive downstream invalidations from the graph.
- Keep TODO/Task dependencies in QBrain. Artifact versions and readiness are not aliases for mutable task state. A mixed TODO consumer of native output needs the exact artifact plus producer group completion and current final HV at execution entry. Don't put group UUIDs in TODO `dependsOn`; a pending-group exception is narrowly scoped and expiring, cannot override failed HV, native prerequisites or phase exit.

## 8. Rebuild gates and test proof instead of copying checkpoints

<!-- plan: gates reviewPolicy -->
<!-- defs: gate reviewPolicy -->
<!-- gates: c4-model-exit ui-artifacts-exit plan-ready authorized-to-start slice-entry mocks-validated execution-entry implementation-exit phase-exit validation-exit release-exit -->

- Convert the legacy checklist into named plan/phase/slice/release gates with correct kinds, prerequisite IDs, applicability, commands/check refs, evidence source and specific pass conditions. Keep the canonical graph: C4 and changed-UI exits feed plan-ready; exact-revision human authorization follows it; slice-entry feeds mocks; implementation feeds phase exit, then validation and scoped release. Keep `execution-entry` in the v0.8 implementation chain (mocks-validated → execution-entry → implementation-exit) and in each applicable slice's own gate list; don't retrofit v0.7 slices. Verification-only slices have reviewed criteria/inputs and omit implementation gates; do not remove a real implementation gate to make evidence look complete.
- Classify dependencies by what design/mock work actually needs versus what execution needs. Apply the execution prerequisites immediately before a native Task starts; preserve the group-wide design/mock barrier. Split integration, human verification and deployed checks from AC unit evidence. For applicable ACs preserve a test-owned fake run (zero failures and skips) and an explicit wrong-fake run rejected by the same assertions for the intended reason; later bind the same cases, inputs and assertions to production and run full unit regression separately. CQRS handler evidence may cover a whole non-UI criterion, not behavior assigned to the UI.
- Keep independent JSONL HV at each applicable stage exit: AGREE, accuracy and completeness each at least 98 on a 100-point scale (higher authorized floor allowed), zero FAIL/UNKNOWN/missing required claims. Recheck scope, reviewer independence and exact inputs. Preserve earlier verdicts as history. At plan-ready require only planning and due design evidence; mock/production/full-unit/output receipts are future gate obligations. **Do not assemble or request HV during migration**, nor treat a score as human approval or an AC acceptance result.

## 9. Fit the execution design to trusted QBrain workflow

<!-- plan: qbrain -->
<!-- defs: taskWorkflow -->

- Map the legacy task system to trusted workspace bootstrap, persistence, enforcement and resume. Bind the turn to the canonical plan and actual slice TODO (umbrella for plan/native work), plus actual native Task/turn where applicable. Resume from exact versions, current identity and authoritative receipt/last checkpoint. If access or trust fails, keep work local and mark durable state unknown; do not advance a controlled gate or infer that unsupported lookup means no records.
- For native groups, keep design and applicable test-owned mock work parallel before implementation; enforce group-wide barrier and task/group execution prerequisites at Ready → Started. Map final independent group HV to exactly one last Task depending on every other member. Verify the required reviewer independence/model-family separation and final success before marking the group complete. Keep group HV distinct from slice/phase HV and use audited corrections for frozen successful work.
- Replace “run everything in parallel” with independent groups, one Started Task and one assignment per group, owned shared-file integration, separate worktrees/outputs/fixtures, disk checks, one Nuke invocation per worktree and recorded collisions/reruns. Put the last verified checkpoint, revision, host, receipt and next unmet AC on each update. Document the policy in the plan; don't suggest schema fields can override it.

## 10. Reconcile risks, open decisions, exceptions and later changes

<!-- plan: risks openDecisions exceptions changeControl -->
<!-- defs: decisionOrRisk exception -->

- Convert unresolved questions or conflicts to a scoped decision with owner, impact, resolution/evidence reference if available. Convert uncertainty to a scoped risk, not an unstated assumption. A string ref may point to an authoritative record; otherwise keep a structured ID, summary, owner, affected scopes and resolution/evidence fields. Record authorized tailoring as an exception only with approval, limits, expiry and evidence; an old skip or informal note is not authorization.
- Derive direct/transitive impact from versioned artifact edges. Change rules and contracts at their owning source, regenerate affected C4/UI evidence, mark affected receipts stale, preserve the history, rerun affected gates and seek fresh approval if scope or approved baselines materially change. Don't invalidate unrelated slices by default.

## 11. Convert completion and receipts into verifiable state

<!-- plan: evidence completionTracking receiptTracking completion -->
<!-- defs: utcStamp receiptId trackingScope receiptArtifactVersion receiptIndexRecord completionRecord milestoneStatus completionSnapshot completionTracking receiptTracking -->
<!-- approval: 16 -->

- Move old done flags, output snippets and approvals to source-addressable ledger history. Map actual receipts to canonical scope (plan, phase or version-appropriate slice), gate, plan revision, stable receipt ID, source kind/ref, UTC timestamp, outcome, freshness and exact artifact/version pairs. Use original QBrain turns or immutable evidence; keep check/HV/approval/release receipts tied to a gate. Link superseded receipts rather than replacing historical records or squeezing logs into the plan.
- Record receipt inputs and proof: workspace/plan/TODO/turn/gate, policy/requirement/source revisions, command, executed scope, result/counts, timestamp and output, plus applicable native task, artifact, reviewer identity/model/effort and exact HV request/claims/scores. Keep secrets out. Missing, stale, inconclusive or wrong-scope evidence is not current proof.
- Leave the canonical plan's completion snapshot null and receipt records empty. Live state comes from QBrain and versioned results; any generated projection is read-only with a UTC as-of time, exact plan revision and source refs. Map pending/in-progress/blocked/failed/stale/not-applicable distinctly; use verified only with current matching receipt IDs, verification time and no outstanding blocker for that record. Give required reasons for blocked, failed, inapplicable or stale statuses. Track implementation, validation and release separately; use not-in-scope for the appropriate milestone rather than asserting delivery. Never close the umbrella from copied checklist marks.

## 12. Strip examples and decide what can be handed off

<!-- plan: example -->
<!-- defs: example plan approvalCandidate executionCandidate -->

- Keep example paths, IDs, tests, artifacts and slice layouts as teaching aids only, or omit the optional example; never count them as project scope or evidence. If retained, use the corresponding version's identity form.
- Run root validation on the draft, then the selected `approvalCandidate` checks when populated. Confirm source fidelity, reference ownership, design proof and gate applicability separately. Unsupported references or missing required design remain current blockers, even if the parser accepts the file. Do not populate release or receipt fields merely to meet an unrelated validation path.
- When complete, hand off the exact saved YAML, matching schema and reconciled migration ledger; use the [plan-ready handoff](migration.md#after-migration-plan-ready-handoff) for pinned Git and task-number crosswalk. A material design change still needs its own owner decision, and execution still requires exact-revision human approval.

**Worked example.** A source says “build export UI, test in staging, ship together” but authorizes implementation only. Keep export scope and explicit release exclusion, assign the unsplit phase a verified default slice, trace format/error behavior to ACs and fake/real unit tests, and convert UI prose into catalogued use cases, SVG states and AssetReview paths. Model the UI, handler and data store in Structurizr, then version the export contract as an output and bind each consumer at the gate it needs. A staging smoke test remains separate validation; it is not proof of unit behavior or release authority. If the required storyboard branch or TODO identity is missing, hand back the draft with that blocker rather than starting HV.
