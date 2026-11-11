# Independent HV verdict: R2 Dolt federation plan packet (plan-readiness)

- Reviewer: Claude, model claude-opus-4-8, effort max, read-only, independent (fresh agent, not a fork).
- Plan author family: Codex (reviewer family differs; compliant with MEMORY-HV-001 / profile 7.1).
- Requester: ClaudeCode, session ClaudeCode-20261111T145103Z-plugin-session, turn req-20261111T145202Z-prompt-bb1e.
- Request receipt: docs/receipts/hv/20261111T090739-federation-r2-plan-readiness.request.jsonl
- Response stream: docs/receipts/hv/20261111T090739-federation-r2-plan-readiness.response.jsonl (full subagent JSONL transcript)
- Review kind: plan-readiness (no implementation exists; B1-B12 unproven; profile 7.9 TRX requirement not applicable).
- Requester verification: Findings 1, 2, 4a, 4b, 4c, and 7 independently reconfirmed against source by the requester before reporting (grep receipts in the session log turn).

## OVERALL VERDICT

DISAGREE. Accuracy 95. Completeness 94. Bar is 98/98; either score below 98 forces DISAGREE.

Strong, largely decision-complete packet. Held below the bar by provable internal inconsistencies in its own normative wire contract and traceability metadata, plus one author disposition (D1) that overstates its remediation. None is a blocker; together they would mislead a less-capable implementer building strictly from the canonical contract and phase headers, so the packet is not yet decision-complete at the operator's bar.

## PER-TASK VERDICTS (Ph0-Ph8)

- Ph0 (feasibility/B1-B12): AGREE. Accuracy 97, Completeness 96.
- Ph1 (fourth provider + migrations): AGREE. Accuracy 98, Completeness 97. Cleanest phase.
- Ph2 (routing/boundaries/editing ownership): AGREE. Accuracy 97, Completeness 96.
- Ph3 (seed migration + recovery export): DISAGREE. Accuracy 96, Completeness 95.
- Ph4 (explicit commits/branch/publication intent): DISAGREE. Accuracy 95, Completeness 95.
- Ph5 (hub control table + publication): DISAGREE. Accuracy 94, Completeness 93.
- Ph6 (receiving/review/join/recovery): DISAGREE. Accuracy 94, Completeness 93.
- Ph7 (plugin/REPL/MCP workflows): AGREE-leaning. Accuracy 97, Completeness 96.
- Ph8 (two-host acceptance + held cutover): AGREE. Accuracy 97, Completeness 96.

## FINDINGS

Finding 1 (HIGH). The "Stable failure mapping" that TR-MCP-FEDERATION-010 declares the locked single source of truth omits `request_id_conflict`. Used with 409 at current-contracts.md:159 and Technical-Requirements.md:183, absent from the enumerated 409 list at current-contracts.md:157 / Technical-Requirements.md:181. Fix: add `request_id_conflict` to the 409 list (and its mirror).

Finding 2 (HIGH). Same mapping omits `audit_id_conflict`, used with 409 at current-contracts.md:155,176,180,312; architecture.md:179; three-system-workflows.md:61; Technical-Requirements.md:9,12,198. Fix: add `audit_id_conflict` to the 409 list.

Finding 3 (MEDIUM). The 409 form of `waiting_local_work` is absent from the 409 list; current-contracts.md:159 states both the 200-outcome role and "A requested live mutation blocked by drafts returns 409 waiting_local_work." Fix: enumerate `waiting_local_work` in the 409 list with a one-line note on its dual role.

Finding 4 (MEDIUM). Three bdpv4 phase headers disagree with their own named tests and with the Requirements-Matrix (traceability drift):
- 4a. Ph5 header (bdpv4:105 "FR-MCP-176,178,179,181,185") omits FR-182 and FR-184, yet Ph5 tests SimultaneousHumanIdReservation (FR-182) and AuditReachesHub/AuditRetryIdempotent (FR-184) (bdpv4:105). Matrix lists both in Ph5.
- 4b. Ph6 header (bdpv4:111 "FR-MCP-178,180,181,186,188,189") omits FR-187, yet Ph6 owns join (MigrationJoin* = FR-187, bdpv4:115). Ph6 also tests AuditBackfillVerified (FR-184, bdpv4:117), but FR-184's phase list is Ph2/Ph3/Ph5/Ph7 everywhere (excludes Ph6).
- 4c. Ph4 header (bdpv4:99 "FR-MCP-175,177,182,183,184,189") lists FR-182, but Ph4 names no FR-182 test and the matrix excludes Ph4 for FR-182.
Fix: reconcile the three phase headers with the named tests and the matrix, then re-run mapping lint.

Finding 5 (MEDIUM; observation + inference). Engine-selection framing conflicts with the known-good anchor. architecture.md:9 states "The operator additionally requires Dolt as the fourth supported database... This document adopts that planning direction," and the whole 16-contract/99-AC surface is Dolt-specific; but library-options.md:11 still lists Dolt as one unadopted candidate among F/G/H/I and says "No option is adopted," and MEMORY-PREFERENCE-002 states no engine has been adopted and libraries must be surfaced for approval before adoption. The packet never installs/adopts and gates feasibility to Ph0 B1, so it does not mechanically violate "never already-adopted"; but a reader of only the architecture would treat Dolt as settled. Fix: add an explicit Ph0 blocking output "operator selects the versioned-storage engine (Dolt vs F/H/I)" OR state plainly, with a receipt, that the operator already selected Dolt. See UNKNOWN U1.

Finding 6 (LOW-MEDIUM). Outcome-token drift: `already_applied` (current-contracts.md:308, Functional-Requirements.md:263) vs `already_received` (audit channel, current-contracts.md:155,176) are unreconciled; `already_applied` is not in the enumerated outcome-field list and traces to the superseded completion plans. Fix: reuse `already_received`, or define `already_applied` explicitly as the migration-journal outcome and add it to the vocabulary.

Finding 7 (LOW-MEDIUM). Stale count. reconciliation.md:7 says "91 current Given/When/Then cases," but the packet has 99 release criteria (matches validation.json currentUniqueCriteria=99, dispositions A6, Requirements-Matrix.md, bdpv4:139). The "91" predates the restoration of 8 criteria. Fix: update reconciliation.md:7 to 99.

Finding 8 (MEDIUM; attack-surface-1 refutation). Disposition D1 overstates its remediation. 20261110-revision-audit-dispositions.md:39 frames the error-code issue as resolved; the specific items the audit named ARE fixed, but the mapping declared "locked"/normative is itself incomplete (Findings 1-3), so error-code consistency is not fully resolved as D1 implies. A true-but-incomplete disposition claim is a planning defect under the HV rules. Fix: close Findings 1-3, then restate D1 accurately.

## EXPLICIT FAIL LIST

- request_id_conflict (409) absent from the normative stable failure mapping (Finding 1).
- audit_id_conflict (409) absent from the normative stable failure mapping (Finding 2).
- 409-form of waiting_local_work absent from the mapping's 409 enumeration (Finding 3).
- bdpv4 Ph5 header omits FR-182/FR-184 it tests (Finding 4a).
- bdpv4 Ph6 header omits FR-187 (join) it owns; AuditBackfillVerified tested outside FR-184's phase list (Finding 4b).
- bdpv4 Ph4 header lists FR-182 with no FR-182 test and matrix excludes Ph4 (Finding 4c).
- reconciliation.md "91 current GWT cases" stale; actual 99 (Finding 7).
- disposition D1 claims the error-code issue resolved while the normative mapping is incomplete (Finding 8).

## EXPLICIT UNKNOWN LIST

- U1: Whether the operator has already selected Dolt as THE engine (architecture says "requires Dolt"; options doc and anchor say no engine adopted). Needs operator confirmation; not resolvable from the packet (Finding 5).
- U2: Whether the operator accepts Dolt's cell-level relational merge as satisfying the requested "Git-style merging." Packet flags it for approval (library-options.md:92; architecture.md:9); approval not evidenced in the packet.
- U3: PDF-only properties (87-page render, captions, A2 diagram sizing, page bounds). Reviewer hash-verified all 10 sources against the manifest; validation.json is the author self-check (independentHv: "Not rerun"); PDF-specific assertions unverified by the reviewer. Markdown authoritative; PDF unavailability is a limitation, not a blocker.
- U4 (low): whether `already_applied` is intentional per-domain vocabulary or drift (Finding 6) - leans drift.

## STRENGTHS (independently confirmed, recorded for balance)

All 10 manifest source hashes MATCH (packet integrity intact); FR->TR->TEST 1:1 ordering clean; ac-FullProjectRecoveryPreservesAllDomains genuinely covers every B2 domain + unpublished branch + dirty draft + audit reconcile + retained Dolt archive + a negative control; idempotency/recovery design matches classic patterns (requestId commit trailer, publication-intent reconstruction, migration journal, expected-head/fence, HubEpoch/WorkerEpoch) and explicitly disclaims exactly-once; conflict review forbids last-writer-wins and catches clean-merge domain violation (dependency_cycle); EF10+MySQL-provider+Dolt compatibility is NOT silently assumed (architecture.md:112,116; B1 names the failure path); HubRestoreFromOlderBackup first-ownership is unambiguously Ph5 (D8 genuinely fixed); join correctly deferred to Ph6 after Ph4/Ph5; DoltLite Beta not relied upon; DEPLOY HOLD integrity consistent.

## LIMITATIONS (recorded separately; none lowered a score)

- add-profile ran and PROFILE-MERGED.md was read in full; the live MCP memory full-text listing through the plugin was not performed in the read-only subagent (relied on the auto-memory index plus the verbatim MEMORY-HV-001 rules and MEMORY-PREFERENCE-002 anchors supplied in the brief). Infrastructure limitation per MEMORY-HV-001 section 6, not a review blocker.
- Read-only: no build, tests, migrations, or PDF render. Planning review; no TRX expected; unproven B1-B12 not counted as a planning defect.
- Superseded plans treated as non-authoritative historical evidence.
- Mechanical lint run in PowerShell only (PSScriptAnalyzer clean).

## REMEDIATION PATH

Close Findings 1-4 and 7-8 with minimal edits (add 3 codes to the mapping; reconcile 3 phase headers and FR-184's phase list against the matrix; update 91->99; restate D1), obtain operator confirmation on U1/U2, then re-run HV. Close to the bar; surgical fixes, not structural redesign.
