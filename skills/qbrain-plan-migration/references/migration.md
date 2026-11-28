# Legacy plan mapping and verification

## Source inventory and ledger

Use the plan plus authoritative requirements, architecture, approved owner decisions and exact QBrain records. Resolve explicit supersession by date and authority; recency alone does not turn a suggestion into a decision. Assign every substantive source item one disposition:

| Disposition | Meaning |
|---|---|
| Carried | Represented by a target path or canonical ID; include the source anchor. |
| Linked | Already owned elsewhere; link to the exact version rather than copying. |
| Superseded | Preserve provenance and link the decision authorizing the replacement or removal. |
| Unresolved | Record missing or conflicting information, impact, known owner and next action. |

Use one companion ledger (for example, the plan basename followed by `.migration.md`). Suggested columns: source revision and anchor, item, target path or ID, disposition, evidence or blocker. Do not add unrecognized top-level fields to the plan to contain this ledger. Keep private source references and sensitive material in the intended workspace.

## Field mapping

| Source signal | Canonical destination / handling |
|---|---|
| Objective, business value, boundaries, exclusions and compatibility | `intent`; preserve useful measures and recognized uncertainty. |
| Requirements and acceptance criteria | `authority.requirementBaseline`, slice requirement refs and `acceptanceTrace`; qualify an AC by its owning requirement. Keep authoritative wording in the catalog. Unapproved requirements remain proposals. |
| Architecture, published contracts, decisions and alternatives | `authority.architectureBaseline`, C4 model refs, linked decisions and slice architecture refs. Compare against actual policy; not every diagram is an approved design. |
| UI behavior or an affected flow | Explicit `intent.uiImpact`; use-case IDs, renderable SVGs, AssetReview manifest/frames and trace links. Do not clear the gate with screenshots or an inferred no-UI judgment. |
| Phase, milestone, work package, and external prerequisite | `delivery.phases`, `delivery.slices`, external dependencies. Reuse existing phase IDs and real TODO or native Task Group IDs; preserve the actual dependency order. Use an inline default slice for a single unsplit phase; preserve a meaningful multi-slice breakdown with explicit references. If one legacy item maps to several slices, record the split once in the ledger. |
| Inputs, deliverables, or versioned changes | Register the logical artifact once, versions separately, and slice edges with gate timing in `artifactDependencies`. To revise: consume prior version, produce a distinct version of that same artifact, and record supersession. Derive reverse impact from edges. |
| Approved exceptions, named risks, blockers and decisions | Use the matching risk/decision/exception fields or their owning records. Keep rationale, scope, owner and approval/expiry when applicable. |
| Checklists, test outputs, HV scores, and release claims | Distinguish planned checks, reported history, and verified evidence. Link historic receipts in the ledger; only current matching receipts may support a gate. Require both passing fake-suite and wrong-fake rejection receipts before production; bind identical cases, inputs and assertions to the real implementation after code. Deployed checks never count toward mock-first coverage. |
| Deployment, migration, rollback and handoff | Populate transition only when release is in scope. Preserve an implementation-only boundary if that is what was authorized. |

If the source has only umbrella checklist rows or native lookup is unavailable, follow [TODO-backed provisioning](todo-provisioning.md). Preserve unresolved edges and identity-free phase charters in the ledger; report a blocked draft rather than modifying the schema to fit missing identities.

## Before handing off

1. Use a YAML parser that rejects duplicate keys and a Draft 2020-12 JSON Schema validator for the parsed data. The schema root accepts draft nulls. Use `#/$defs/approvalCandidate` to check populated planning data and `#/$defs/executionCandidate` to check for an approval reference; passing either does not verify approval.
2. Check uniqueness by canonical ID, stage and slice references, phase membership, cross-workspace identity, one producer per planned version, valid same-artifact supersession, acyclicity and producer-before-consumer ordering. Compare inferred cross-slice precedence with QBrain TODO or Task Group dependencies; for each native group, verify its workspace and umbrella TODO and ensure no other slice defines the same UUID. Do not synchronize discrepancies unless authorized.
3. Reconcile all in-scope FR/TR/TEST/AC and affected use-case steps, design references and outputs. A legacy check with no command, scope or input revision is reported history, not proof. Reuse validated receipts only when the actual gate, inputs, candidate and governing policy still match; schedule independent HV at the applicable stage only after migration is complete; never carry a verdict over by renaming the stage.
4. Review differences from the source and identify blockers for the operator: missing canonical IDs, unresolved decisions, absent artifacts or stale evidence. Keep the new approval reference unset until the owner approves that exact revision. Report draft, approval candidate, and actual approval as different states.

### Example: the legacy plan says “export complete”

A checked item and an old smoke screenshot do not establish BDPv4 coverage. Map export to the existing QBrain TODO and requirement/AC if verified, keep the screenshot as historical integration context, and flag the missing passing mock, negative-control, full real-binding regression and current HV receipts. If a later slice consumes the export contract, pin the contract version at the consuming gate. If the real TODO ID or contract version cannot be verified, record the unresolved mapping instead of fabricating either.

## Read or convert a v0.7 TODO-backed plan

- For inspection, normalize legacy definition `todoId`, phase list `sliceTodoIds`, and edge/receipt/completion `sliceTodoId` to `{ kind: todo, todoId: … }` in memory. Keep the exact TODO, plan revision, receipt IDs and evidence sources. Resolve using the pair (kind, identifier), not display names or a second delivery ID.
- Convert only when requested. Write a new draft revision with `schemaVersion: "0.8"`, the corresponding `sliceRef` / `sliceRefs` fields, and no legacy fields in those locations. Preserve inline versus explicit layout, receipt IDs, original source references, and the legacy plan; do not carry approval or passing evidence forward as if it covered the new revision.
- Treat a TODO-to-Task-Group change as an explicit mapping decision, not an ID alias. Verify the group in the target workspace under the umbrella TODO and map all affected phase lists and versioned edges. Never relabel historical receipt evidence or create a mirror TODO.

## Preserve gate timing across mixed slices

- Map inputs to when they are used, not simply to the first gate. An unfinished execution predecessor does not block otherwise eligible design or mock work. A missing input actually used by a design or contract case does block that affected work.
- For each required native output consumed by a TODO slice, follow the exact version's producing edge to the group. The migration ledger must retain the artifact revision/digest evidence and the native completion/final HV evidence as distinct checks at `execution-entry`. Do not turn a group UUID into a legacy TODO dependency. Check native prerequisites again at `Ready -> Started`.
- Retain an early handoff only with a current human approval for the exact producer, consumer, artifact revision and allowed actions, plus evidence and expiry. It can cover pending group HV, not failed HV or native prerequisites. Never migrate that exception as a passing phase exit: all native groups must be Completed with current final HV, and the phase has its own independent review.

## After migration: plan-ready handoff

1. **Close migration before HV.** Finish the saved YAML, companion schema validation, source reconciliation and ledger/readback checks before assembling an HV packet or invoking a reviewer. A valid draft with unresolved required bindings or incomplete migration is not an HV input. Report current blockers and stop. These deterministic checks are preflight, not HV. Never carry over a legacy verdict as a current result.
2. **Export the roster without changing history.** If the authoritative source establishes a complete, zero-based legacy roster, export a one-based packet view with `packetTaskNumber = legacyTaskNumber + 1`. Retain each original number, canonical phase ID, exact read-back TODO ID, and source anchor. For example, Tasks 0–12 map to packet Tasks 1–13. Apply the same crosswalk to new packet-scoped references; keep original task numbers, stable IDs, and historical receipts intact. Label reference number basis explicitly, resolve historical Task 0 to packet Task 1 in the new view, and reject missing, duplicate, or ambiguous mappings. Do not renumber a roster whose numbering basis was not verified.
3. **Separate design checks.** Record operator UI design approval against the exact reviewed scope/revision, independently of linked use-case identity and storyboard proof. A use-case status label by itself is not a separate approval gate. For the selected gate, report missing frames, decision-branch links and path walks as specific evidence gaps. Verify exact-revision Structurizr validation and rendered C4 artifacts separately against the architecture gate.
4. **Verify an immutable candidate before packet assembly.** The intended Git commit must contain the exact saved plan, ledger, and matching schema; verify that cited evidence is resolvable by the reviewer. Report missing or untracked candidate inputs as blockers, not proof that the observed checkout HEAD contains them. Do not stage, commit or otherwise alter Git without authorization.
5. **Evaluate evidence only at the selected gate.** At `plan-ready`, check scope and migration fidelity, traceability, due design prerequisites, and evidence already cited. Preserve future passing fake/negative-control receipts, production binding, full-unit results, and output-availability obligations in the plan and require them at their declared execution gates. Future obligations are not present `plan-ready` failures.

The read-only [PowerShell handoff module](../scripts/PlanMigrationHandoff.psm1) supports structured binding reconciliation and legacy roster projection.

- Import the module with PowerShell 7. Pass the complete, independently verified source slice keys to Compare-PlanMigrationBinding as ExpectedSliceKeys, in source order. Supply Ledger, SavedBindings and VerifiedReadbacks separately. Derive expected keys from the source even when the draft or ledger is missing a slice.
- Keep missing and conflicting bindings and evidence for unexpected slice keys in the current blocker list. Apply superseded pending entries only where the source slice, ledger TODO, saved binding and exact verified readback agree. Preserve the original ledger history and unrelated blockers. Verify TODO ownership and workspace scope before supplying a readback.
- For a verified zero-based roster, call ConvertTo-PlanMigrationTaskCrosswalk with the source rows and verified source count. Use Resolve-PlanMigrationPacketTask with an explicit legacy or packet number basis.

The module operates on the supplied records. It does not write QBrain records, change Git, assemble a packet, or run HV.
