---
name: qbrain-plan-create
description: Create a new BDPv4 implementation plan in Codex Plan mode using the QBrain YAML template and schema. Use to plan new work from a request, not to migrate a legacy plan, implement changes, or deploy.
---

# Create a new QBrain plan

Produce a decision-complete proposal where the evidence supports it. Leave genuine blockers visible. Planning is not implementation, approval, or authorization to change QBrain.

Use the user-selected or current authoritative template and schema together. If unavailable, use the repository’s [v0.6 template](../../docs/plans/BDPv4-QBrain-Implementation-Plan-Template.yaml) and [matching schema](../../docs/plans/BDPv4-QBrain-Implementation-Plan.schema.json). Follow the template's agent header; this skill governs the new-plan workflow and Plan mode boundary.

## Plan mode boundary

- Inspect the relevant checkout, instructions, requirements, architecture, tests and approved constraints using read-only discovery. Use the workspace's supported trusted QBrain read path. Do not edit the repo, create TODOs, advance gates, write receipts to QBrain, or start implementation while Plan mode forbids those changes. Do not switch modes yourself or use another tool to evade the boundary.
- Ask the user only for a material decision you cannot resolve from authoritative sources: scope, a public contract, an important tradeoff, or authority to proceed. Present concrete choices and a recommendation when useful. Keep evidence and inference distinct.
- Put the proposal in the host's plan output format. When a file cannot be written, provide the canonical YAML in the plan response or as a clearly labeled handoff for the authorized write step; state where it belongs. Never claim a proposed path, ID reservation, approval, or receipt was persisted.

## Build the plan

1. **Ground scope.** Capture the problem, intended users, measurable outcome, exclusions, changed behavior, viability/value and ship boundary. Assign known owners. Pin the governing BDPv4/workspace rules, requirements and source revisions, and current architecture. Link authority instead of repeating it.
2. **Close material design decisions.** Define public contracts and boundaries before scheduling implementation. Identify the versioned Structurizr workspace and required C4 views. Explicitly classify UI impact. For changed UI, plan the required use cases, SVG wireframes and AssetReview storyboards, tracing use-case steps to frames and the owning FR/AC. Missing required artifacts become readiness blockers, not promises of validation.
3. **Sequence the work.** Define phase objectives and bounded slices with verifiable exits. Reuse verified canonical IDs. New artifact and phase IDs may be proposed and checked for collision; a slice's real ID is its QBrain TODO ID. If no TODO exists, identify proposed slice work as pending in the handoff; do not invent a persisted ID or present a placeholder as ready. Map exact artifact versions and their producing/consuming gates; keep live task dependencies in QBrain.
4. **Design the proof.** Trace in-scope FR/TR/TEST, qualified ACs and use cases to slices and tests. Put mock-first validation before production code, with separate full unit regression, integration/human validation and optional release checks. Require independent HV AGREE with at least 98 accuracy and 98 completeness at every applicable stage exit, not just overall. State what invalidates evidence and what must be rerun. Keep approval unset until the exact revision is approved.
5. **Verify and hand off.** Parse the YAML with duplicate keys rejected, run the companion schema when available, and independently check ID/link resolution, ownership, completeness and dependency order. Report draft schema validity separately from approval readiness. The canonical draft keeps live completion snapshots and receipt entries empty; state the authoritative tracking source or flag it as pending. Never claim a stage passed without current authoritative evidence.

Report the target path, decisions and rationale, expected artifacts and checks, and unresolved blockers. When mode and permissions later allow, persist the proposed records through the supported QBrain tools, verify their identities, and revalidate the exact plan revision before requesting approval. Do not start implementation merely because the plan was written.
