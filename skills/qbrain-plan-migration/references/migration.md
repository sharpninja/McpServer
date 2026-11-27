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
| Phase, milestone, work package, and external prerequisite | `delivery.phases`, `delivery.slices`, external dependencies. Reuse existing phase IDs and real TODO IDs; preserve the actual dependency order. If one legacy item maps to several slices, record the split once in the ledger. |
| Inputs, deliverables, or versioned changes | Register the logical artifact once, versions separately, and slice edges with gate timing in `artifactDependencies`. To revise: consume prior version, produce a distinct version of that same artifact, and record supersession. Derive reverse impact from edges. |
| Approved exceptions, named risks, blockers and decisions | Use the matching risk/decision/exception fields or their owning records. Keep rationale, scope, owner and approval/expiry when applicable. |
| Checklists, test outputs, HV scores, and release claims | Distinguish planned checks, reported history, and verified evidence. Link historic receipts in the ledger; only current matching receipts may support a gate. Deployed checks never count toward mock-first coverage. |
| Deployment, migration, rollback and handoff | Populate transition only when release is in scope. Preserve an implementation-only boundary if that is what was authorized. |

## Before handing off

1. Use a YAML parser that rejects duplicate keys and a Draft 2020-12 JSON Schema validator for the parsed data. The schema root accepts draft nulls. Use `#/$defs/approvalCandidate` to check populated planning data and `#/$defs/executionCandidate` to check for an approval reference; passing either does not verify approval.
2. Check uniqueness by canonical ID, stage and slice references, phase membership, cross-workspace identity, one producer per planned version, valid same-artifact supersession, acyclicity and producer-before-consumer ordering. Compare inferred cross-slice precedence with QBrain TODO dependencies; do not synchronize discrepancies unless authorized.
3. Reconcile all in-scope FR/TR/TEST/AC and affected use-case steps, design references and outputs. A legacy check with no command, scope or input revision is reported history, not proof. Reuse validated receipts only when the actual gate, inputs, candidate and governing policy still match; record independent HV on the applicable migrated stage per the template, never carry a verdict over by renaming the stage.
4. Review differences from the source and identify blockers for the operator: missing canonical IDs, unresolved decisions, absent artifacts or stale evidence. Keep the new approval reference unset until the owner approves that exact revision. Report draft, approval candidate, and actual approval as different states.

### Example: the legacy plan says “export complete”

A checked item and an old smoke screenshot do not establish BDPv4 coverage. Map export to the existing QBrain TODO and requirement/AC if verified, keep the screenshot as historical integration context, and flag the missing mock/full-regression and current HV receipts. If a later slice consumes the export contract, pin the contract version at the consuming gate. If the real TODO ID or contract version cannot be verified, record the unresolved mapping instead of fabricating either.
