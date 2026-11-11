# Independent HV verdict: R3 Dolt federation plan packet (plan-readiness re-review after remediation)

- Reviewer: Claude, model claude-opus-4-8, effort max, read-only, independent fresh agent (not a fork).
- Plan author family: Codex; R2->R3 fixes applied by ClaudeCode at operator direction. Reviewer family differs from author (MEMORY-HV-001 / profile 7.1 compliant).
- Requester: ClaudeCode, session ClaudeCode-20261111T145103Z-plugin-session, turn req-20261111T145202Z-prompt-bb1e.
- Request receipt: docs/receipts/hv/20261111T093826-federation-r3-plan-readiness.request.jsonl
- Response stream: docs/receipts/hv/20261111T093826-federation-r3-plan-readiness.response.jsonl
- Markdown sources authoritative; stale PDF/HTML export not used.
- Requester note: both R3 findings (R3-N1 FR-184/Ph4, R3-N2 FR-183/Ph4) independently reconfirmed against source, and a full FR-phase-vs-test-allocation lint across all 99 tests / 16 FRs was run; after the R3->R4 fixes it reports 0 genuine mismatches.

## OVERALL VERDICT

DISAGREE. Accuracy 97. Completeness 97. Bar is 98/98.

All eight R2 findings genuinely and cleanly resolved, with NO new drift from the fixes (every change diffed against HEAD). Held just below the bar by ONE residual internal inconsistency that is a sibling of R2 Finding 4 and pre-existing in the R2 packet (an R2 blind spot), not introduced by the fix: FR-184's reconciled "Phase:" annotation reads Ph2/Ph3/Ph5/Ph6/Ph7 in all three normative sites, but Ph4 owns an FR-184 acceptance criterion (ac-CommitPreviewBlocksSecret, named as a Ph4 target at bdpv4:101), so the annotation is incomplete (missing Ph4). R2 Finding 4b prescribed "FR-184 += Ph6"; the correct propagation-audit result was "FR-184 += Ph4, Ph6." A weaker sibling (FR-183 annotation Ph7 vs StaleBranchReportedNotDeleted named in Ph4) rides the same line. Surgical remedy: one phrase per site.

## PER-TASK VERDICTS (Ph0-Ph8)

- Ph0: AGREE. Accuracy 98, Completeness 98. Dolt-selection record keeps feasibility gated by Ph0 B1 and "no package installed."
- Ph1: AGREE. 98/98. Header FR-186 only; untouched; cleanest phase.
- Ph2: AGREE. 98/97. Named Ph2 tests map correctly.
- Ph3: AGREE. 98/98. 91->99 fix lands; Ph3 header consistent.
- Ph4: DISAGREE. 97/96. Finding 4c fixed, but Ph4 names CommitPreviewBlocksSecret (FR-184) and StaleBranchReportedNotDeleted (FR-183) while FR-184 (Ph2/Ph3/Ph5/Ph6/Ph7) and FR-183 (Ph7) both omit Ph4 (R3-N1/N2).
- Ph5: AGREE. 98/98. Finding 4a fixed; header matches tests and matrix.
- Ph6: AGREE. 98/98. Finding 4b fixed; FR-187 in header; FR-184 += Ph6 propagated; Ph6 membership justified.
- Ph7: AGREE. 98/97. FR-183 plugin bindings owned here; convention intact.
- Ph8: AGREE. 98/97. DEPLOY HOLD intact; earlier-phase-contract carve-out intact.

## RESOLUTION STATUS OF THE 8 PRIOR FINDINGS

- Finding 1 (request_id_conflict): RESOLVED (current-contracts.md:157, Technical-Requirements.md:181; both lines identical).
- Finding 2 (audit_id_conflict): RESOLVED (same two lines).
- Finding 3 (409-form of waiting_local_work): RESOLVED with the dual-role note.
- Finding 4a (Ph5 header): RESOLVED (bdpv4:104).
- Finding 4b (Ph6 header / FR-184 Ph6): RESOLVED for its named scope (bdpv4:111; FR-184 += Ph6 in all three sites). Propagation incomplete re: Ph4 (see R3-N1).
- Finding 4c (Ph4 header FR-182): RESOLVED (bdpv4:99).
- Finding 5 / U1 / U2: RESOLVED in the packet (architecture.md:9, library-options.md:11,92); no contradiction with "no package installed" or DEPLOY HOLD.
- Finding 6 (already_applied): RESOLVED (defined once in each file; usage consistent).
- Finding 7 (stale 91): RESOLVED (reconciliation.md:7 -> 99).
- Finding 8 (D1 overstated): RESOLVED (dated R3 addendum; independent wire-code lint confirms the mapping is now exhaustive).

## NEW FINDINGS

R3-N1 (MEDIUM; traceability-metadata drift; residual sibling of R2 Finding 4; pre-existing, NOT introduced by the fix). bdpv4:101 names ac-CommitPreviewBlocksSecret as a Ph4 target; it is an FR-184/TEST-MCP-232 AC (current-contracts.md:186); yet FR-184's Phase annotation (current-contracts.md:172, three-system-workflows.md:146, Requirements-Matrix.md:15) omits Ph4. By the packet's own adjudicated convention (FR annotation lists every phase owning one of its ACs), FR-184 must include Ph4. Fix: FR-184 phase -> Ph2/Ph3/Ph4/Ph5/Ph6/Ph7 in the three sites; re-run the mapping lint.

R3-N2 (LOW; related sibling; lower confidence). bdpv4:101 names ac-StaleBranchReportedNotDeleted (FR-183/TEST-MCP-231) as a Ph4 case; FR-183's annotation is Ph7 only. Mitigated by the convention "Ph7 owns plugin bindings; earlier phases own server behavior" (current-contracts.md:163): StaleBranchReportedNotDeleted's server behavior is Ph4, plugin binding Ph7. Fix: include Ph4 in FR-183 (Ph4/Ph7), or move the naming out of Ph4 with a server-dependency note.

Verified clean (no new drift): error-mapping edits did not duplicate or misplace any code and the two mapping blocks match exactly; every wire code used anywhere is enumerated; already_applied defined once and consistent; Ph5 += FR-182/184 and Ph6 += FR-187 backed by named tests; Ph4 -= FR-182 correct; per-workflow annotations correctly left as-is (U13 excludes Ph6 legitimately; U10 carries no trailing phase set); FR->TR->TEST 1:1 clean; operator-decision records consistent with DEPLOY HOLD and "no package installed."

## EXPLICIT FAIL LIST

- R3-N1: FR-184 phase annotation omits Ph4 while Ph4 owns ac-CommitPreviewBlocksSecret (FR-184).
- R3-N2 (secondary, lower confidence): FR-183 phase annotation (Ph7) omits Ph4 while Ph4 names ac-StaleBranchReportedNotDeleted (FR-183).

## EXPLICIT UNKNOWN LIST

- None that lowers a score. U1 (Dolt selected) and U2 (cell-merge accepted) recorded as operator-resolved and internally consistent; live MEMORY-PREFERENCE-002 v3 not independently read by the reviewer (infra limitation).
- U3 (PDF-only properties) unverifiable and out of scope; markdown authoritative; stale export excluded.

## LIMITATIONS (recorded separately; none lowered a score except the genuine content defects found)

- add-profile ran; PROFILE-MERGED.md read in full (389 lines). Live MCP memory full-text listing via plugin not performed in the read-only subagent (relied on the auto-memory index plus the verbatim profile and the MEMORY-HV-001/MEMORY-PREFERENCE-002 anchors supplied in the brief). Infra limitation per MEMORY-HV-001 section 6, not a blocker.
- Read-only plan-readiness review: no build, tests, migrations, or PDF render. No implementation; profile 7.9 TRX not applicable; unproven B1-B12 not counted as planning defects.
- Read-only Bash git-diff and grep used solely to locate evidence and verify changes against HEAD; no scripts run against data; no state changed; no files edited.

## REMEDIATION PATH

Close R3-N1 (FR-184 += Ph4) and R3-N2 (FR-183 += Ph4) in the three normative sites; re-run the FR-phase/test-allocation lint across all 16 FRs; re-HV. One phrase per site; no structural change. The packet is otherwise at the bar.
