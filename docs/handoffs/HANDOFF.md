# Handoff (branch: develop)

The single current handoff for this branch, replaced in place as state changes. It stays lean: where things stand, where to resume, and pointers to the plan and receipts that hold the detail. Per operator convention (2026-11-11): one handoff at a time in a branch; anything with more detail than this is a plan, not a handoff.

Updated: 2026-11-11.

## Current priority: Dolt versioned federation (operator's top priority)

- **State:** plan-readiness passed independent hostile validation at AGREE 98/98 (rounds R2-R5; reviewer Claude Opus, different family from the Codex author). NOT implementation-ready: DEPLOY HOLD is in force and not lifted, every Ph0 B1-B12 feasibility output is unproven, and nothing is installed or activated.
- **Resume at:** the Ph0/B1 provider-enablement spike (make the accepted EF Core 10 driver - the Pomelo EF10 port or the Microting fork, patched and contributed upstream as needed - drive a pinned Dolt image on LAB Linux and LEGION Windows, proven against the real schema/query/transaction/migration/remote contracts). Then a reduced first increment: one project, both real hosts, one versioned domain end-to-end. Two residual go/no-go, both Ph0 evidence: Dolt's own MySQL-compatibility gaps (Go-side) and crash-safe native Windows Dolt hosting on LEGION (B1/B9). Pending operator call: the test-tagging mechanism scope (general build infra under FR-MCP-BUILD-001 vs federation-first).
- **Plan (detail lives here):** `docs/plans/2026-11-10-federation-dolt-architecture.md` (authoritative), `-bdpv4.md`, `-three-system-workflows.md`, `-current-contracts.md`, `-reconciliation.md`, `-library-options.md`; correctness evaluation `docs/plans/2026-11-11-federation-correctness-evaluation.md`. Contracts of record: `docs/Project/federation-dolt/`.
- **Receipts:** `docs/receipts/hv/20261111T090739|093826|100429|114239-federation-r{2,3,4,5}-plan-readiness.*` (convergence 95/94 -> 97/97 -> 98/98 -> 98/98).
- **Locked decisions:** Dolt selected as the versioned-storage engine; cell-level relational merge with mandatory conflict review accepted as Git-style merging; EF Core 10 prerelease/fork provider accepted (all in Workspace memory `MEMORY-PREFERENCE-002`). xUnit v3 is the test standard (`MEMORY-BESTPRACTICE-001` section 10). The compiled `docs/plans/exports/Federation-2026-11-10-R2.*` is a stale pre-fix rendering and needs recompiling before use.

## Standing duties

- Keep LAB-OMARCHY and PAYTON-LEGION2 Global MCP memories in sync by hand until federation ships (the drift federation removes).
- HV reviewer must differ from the author's agent family and model family (`docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md`).

## Superseded

This replaces the dated per-effort handoff `federation-dolt-handoff-20261111T120903.md` (removed) and the 2026-10-16 forwarding-first `federation-fixes-handoff-20261016T121400.md` for the versioned-federation mode. Explicit DirectProxy forwarding remains a separate supported feature. Older dated handoffs under `docs/handoffs/` are history (see `CATALOG.md`); going forward there is one handoff per branch, here.
# Requirements Reconciliation Plan and Status

Status date: 2026-11-16

This section is the active continuation point. The older federation handoff below is retained unchanged as historical and downstream context. Requirements reconciliation and the requirement-backed Pester remediation must be completed before resuming that older implementation sequence.

## Objective and gate

Reconcile the McpServer workspace requirements on PAYTON-LEGION2 and LAB-OMARCHY with provenance for every governing choice. The reconciled requirements and acceptance criteria then become the authority for evaluating the failing Pester tests. Valid current tests drive implementation remediation. Obsolete tests are removed. Tests whose criteria became invalid because of an approved requirement change are repaired and rerun against current code. Do not remediate implementation code against an unreconciled or obsolete test contract.

## Locked operator decisions

- FR-MCP-USECASE-016 and FR-MCP-USECASE-017 stay deleted on LAB-OMARCHY and are deleted on PAYTON-LEGION2. Their linked TR and TEST records are not authorized for deletion. References are repointed to FR-MCP-USECASE-001 and the TR each retired FR became.
- For the 40 field-row exact namespace or route rename matches, use the QBrain wording and update the McpServer wording.
- For ID mismatches, retain the newer canonical ID.
- Preserve the named PAYTON-LEGION2 acceptance-criterion IDs when LAB-OMARCHY displays blank AC IDs.
- Keep canonical TRIAGE requirement IDs and remove the two `-REKEY-CANONICAL-20261115` placeholders.
- Remove the malformed technical-requirement placeholder whose literal ID is `[]`.
- Add records missing from either host rather than dropping unique requirement data.
- Use the stored SQL timestamps to establish edit order where they are comparable. A timestamp is a lead to the governing decision, not permission to discard contrary evidence.
- For every remaining mismatch, search the session log of the system where the divergent decision was made. If that does not identify the decision, search Codex and Claude transcripts on both systems. Then match the wording to the plan, commit, or pull request that best explains its origin.
- If the evidence still does not establish a governing version, do not mutate the record. Add it to the operator-review table with both values, both SQL timestamps, evidence searched, candidate plan or commit or PR, and the exact unresolved question.

## Evidence baseline

The retained pre-mutation comparison identified:

- 1,118 divergent requirement or mapping entries.
- 3,160 divergent field rows.
- 157 ID-mismatch field candidates, including 145 acceptance-criterion ID cases and 12 rows belonging to two rekey placeholders.
- 40 exact namespace or route rename field candidates.
- 280 field rows missing on PAYTON-LEGION2, represented by 19 source-only requirement records in the completed import pass.
- 31 raw field rows missing on LAB-OMARCHY. Twelve belonged to the rekey group, leaving 19 primary missing fields represented by two source-only requirement records in the completed import pass.
- 2,664 field rows that required individual provenance adjudication in the pre-mutation grouping.

These are baseline counts, not current counts. Partial reconciliation has changed both stores. Regenerate the two-host comparison before reporting any remaining denominator.

Primary evidence root:

`F:/GitHub/McpServer/docs/receipts/requirements-reconciliation/20261115T140309`

Important retained artifacts include:

- `decision-ledger.json`
- `review-field-rows-grouped-v1.csv`
- `review-groups-v1.csv`
- `unreconcilable-entries-for-review-v7.csv`
- `legion2-sql-timestamps.json`
- `omarchy-sql-timestamps.json`
- the Codex and Claude transcript-hit and timestamp-correlation receipts for both hosts
- `operator-decision-usecase-016-017.md`

## Completed reconciliation work

- Deleted FR-MCP-USECASE-016 and FR-MCP-USECASE-017 on PAYTON-LEGION2 through the supported requirements interface. Live readback showed both absent with zero mappings on both hosts.
- Imported 19 requirements missing on PAYTON-LEGION2 and 43 associated mappings through the required plugin route, with source-host readback.
- Imported or restored FR-WF-005 and TEST-MCP-HELP-009 on LAB-OMARCHY and verified readback.
- Removed the approved PAYTON-LEGION2 placeholders FR-MCP-TRIAGE-002-REKEY-CANONICAL-20261115, TR-MCP-TRIAGE-004-REKEY-CANONICAL-20261115, and the malformed `[]` technical-requirement placeholder. Canonical IDs remained active.
- Repaired four TODO requirement-link sets so placeholder links are inactive. SQL readback found no active placeholder links.
- Applied and read back 36 of the 37 QBrain rename-matched requirement records, covering the 40 baseline rename field rows except the legacy FR-SUPPORT-010C record.
- Restored the named PAYTON-LEGION2 AC IDs for 65 of the 145 baseline AC-ID cases while preserving criterion text, evidence, order, satisfaction state, and the other requirement fields.
- Retained SQL timestamps, two-host requirement exports, session candidates, transcript correlations, grouping files, and the decision ledger under the evidence root.
- The reconciliation scripts used so far passed PowerShell parser checks and PSScriptAnalyzer at Warning and Error severity and were dry-run with `-WhatIf` before their state-changing executions.

## Current unresolved work

- Regenerate a fresh two-host requirements, AC, mapping, and soft-delete-state comparison. Do not reuse 1,118, 3,160, or 2,664 as current counts.
- Complete the remaining 80 named AC-ID cases from the baseline 145-case group, preserving the named PAYTON-LEGION2 IDs.
- Resolve FR-SUPPORT-010C. Its QBrain content is selected by operator decision, but the current supported requirement route rejected the legacy ID format. Repair the supported route or use an already-supported exact-ID operation. Do not rename or drop the record merely to bypass validation.
- Build a per-entry provenance ledger for every fresh content mismatch. For each entry, record SQL edit order, originating-host session evidence, Codex or Claude transcript evidence, and the best matching plan, commit, or PR.
- Apply only evidence-backed governing versions through the supported requirement interfaces. Read back every write from both hosts. Do not use raw database writes to perform the reconciliation.
- Produce the final operator-review table for any entry that remains unresolved after all provenance sources are exhausted.
- Verify the retired FRs, rekey placeholders, malformed placeholder, and placeholder TODO links remain absent or inactive after the final pass.
- Verify mapping integrity, acceptance-criterion identifiers, satisfaction state, soft-delete state, and cross-host record counts after convergence.
- Refresh the repository requirement projections from the authoritative stores, reconcile the currently modified requirement documents, and retain the final comparison and hashes.
- Commit and synchronize only after the reconciliation evidence and projections are internally consistent and the intended tracked and untracked artifacts have been classified.

## Execution sequence

1. Capture fresh read-only exports from both hosts and compute a new semantic and field-level diff.
2. Finish the locked deterministic cases: remaining named AC IDs and FR-SUPPORT-010C.
3. For each remaining mismatch, follow the evidence order: SQL timestamp, originating-host session log, Codex and Claude transcripts on both systems, then matching plan, commit, and PR.
4. Append every governing conclusion and its evidence to the decision ledger before mutating the requirement stores.
5. Apply the selected versions through the supported requirement API, one idempotent batch at a time, with immediate two-host readback and retained receipts.
6. Regenerate the diff after each completed group. Stop blind retries on ambiguous writes and follow the supported baseline-read, conditional-replay, and delayed-readback procedure.
7. Present all still-unreconcilable entries to the operator. Do not adjudicate them by similarity, timestamp alone, or convenience.
8. After requirements converge, classify every failing Pester test against the current acceptance criteria. Remove obsolete tests, repair invalid criteria in retained tests, and remediate implementation defects exposed by valid tests.
9. Run the changed test classes through the Nuke `TestGroups` target. At the final head, run the complete applicable zero-failure, zero-skip gate set required by the active plan.
10. Refresh projections and documentation, retain receipts, commit the coherent result, and synchronize the authorized remotes. Do not mark the requirements, test remediation, plan step, or TODO complete without the required acceptance evidence.

## Current stop point

Requirement-store mutation stopped after the approved placeholder cleanup and memory correction work. The provenance adjudication and fresh post-mutation diff have not been completed. Pester remediation has not resumed. The local McpServer checkout contains the partially reconciled requirement projections, plugin/profile-reload work, and the retained reconciliation evidence set. Preserve those changes until their provenance and ownership are verified.

---
