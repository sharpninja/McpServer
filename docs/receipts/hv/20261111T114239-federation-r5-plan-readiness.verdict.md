# Independent HV verdict: R5 (scoped delta re-review of the Dolt federation packet)

- Reviewer: Claude, model claude-opus-4-8, effort max, read-only, independent fresh agent (not a fork).
- Plan author family: Codex; delta authored by ClaudeCode at operator direction. Reviewer family differs from author (profile 7.1 compliant).
- Requester: ClaudeCode, session ClaudeCode-20261111T145103Z-plugin-session.
- Request receipt: docs/receipts/hv/20261111T114239-federation-r5-plan-readiness.request.jsonl
- Response stream: docs/receipts/hv/20261111T114239-federation-r5-plan-readiness.response.jsonl
- Scope: the delta committed at c945fc01 on top of the R4-AGREE packet (R4 verdict 20261111T100429, AGREE 98/98). Markdown authoritative; stale PDF/HTML exports not used.
- Delta is mechanically isolated: git show --name-only c945fc01 = exactly two files, three lines (architecture.md 1 line, bdpv4.md 2 lines). Every R2-R4 regression anchor lives in a file the delta did not touch.

## OVERALL VERDICT

AGREE. Accuracy 98. Completeness 98. Bar is 98/98 and is met.

Both amendments are present, internally consistent, and introduce no contradiction with the gated structure. The B1 provider amendment relaxes only which providers are ELIGIBLE at B1 (a prerelease or community-fork EF10 provider, patched by this project, is now accepted for the Dolt engine context); it preserves DEPLOY HOLD, the B1-B12 unproven-feasibility gating, the "no package is installed" stance, Ph0 as a real go/no-go, B1 as a provider-enablement spike (not an assumption the provider works), and the honest guards (silent EF downgrade and bespoke from-scratch provider still forbidden; report alternatives if reasonable patching cannot make it drive Dolt). The xUnit v3 amendment is a test-framework standard orthogonal to the zero-skip / never-runtime-skipping gate. No R2-R4 finding regressed.

## CONSISTENCY VERDICT PER AMENDMENT

Amendment 1 (B1 provider): CONSISTENT. Present at architecture.md:116 (section 8) and bdpv4.md:72 (section 5); both cite operator decision 2026-11-11 / MEMORY-PREFERENCE-002, scope the relaxation to the Dolt engine context, and keep the guards. Verified intact and untouched: DEPLOY HOLD; B1-B12 unproven gating (bdpv4:57); "no package installed" (architecture:9, bdpv4:3); Ph0 real go/no-go (architecture:116); B1 still a gate not an assumption (bdpv4:59; correctness-evaluation:91/98); honest guards verbatim at both sites. The provenance/GA-maintenance relaxation is within the operator's authority to relax a workspace rule (profile 2.9), recorded as MEMORY-PREFERENCE-002.

Amendment 2 (xUnit v3 test standard): CONSISTENT. Present at bdpv4.md:41. Does not contradict the same-section gate guidance (bdpv4:39/43 "zero failures and zero skips"; :45 container classes excluded by category, never runtime skipping, missing containers a gate failure): one clause picks the sanctioned skip API, the gate sets the exit outcome; orthogonal. Referent substance corroborated by commit 30466238 and PR 88 (xUnit v3 standardization). The exact "section 10" numbering of MEMORY-BESTPRACTICE-001 not independently read in the read-only subagent (see UNKNOWN).

## REGRESSION STATEMENT (R2-R4): INTACT

Delta touched only architecture.md and bdpv4.md. Every anchor is in an untouched file and matches the R4-resolved values:
- 409 mapping: request_id_conflict, audit_id_conflict, waiting_local_work all present (current-contracts.md:157; Technical-Requirements.md:181, identical). already_applied present and distinct from already_received (:159/:183). D1 register documents the R3 addendum (dispositions.md:39). Intact.
- FR-183 "Ph4/Ph7" and FR-184 "Ph2/Ph3/Ph4/Ph5/Ph6/Ph7": identical across all three sites (current-contracts.md:143/:172; three-system-workflows.md:142/:148; Requirements-Matrix.md:14/:15), with Ph4 justifications present. Intact.
- Reconciliation 99 count: intact (reconciliation.md:7). Dispositions D1: intact. U1/U2: recorded consistently (architecture:9; correctness-evaluation:38).
- The eight R2 findings and the R3/R4 phase-annotation reconciliation remain the AGREE-state content; none sits in a delta-touched line.

## NEW FINDINGS

None that lower a score. Two transparency observations (examined and cleared, not findings, not introduced by the delta):
- OBS-1 (LOW; architecture.md:9; cleared): section-1 overview still says "The exact maintained EF Core provider/package version... must pass the compatibility gate." A community fork/prerelease port is still "maintained" by its maintainer, and the sentence reaffirms "no package is installed" and "gated by Ph0 B1," so it is not a contradiction. Optional tidy: "maintained" -> "selected" for exactness.
- OBS-2 (LOW; correctness-evaluation.md; cleared): this is explicitly a DRAFT decision-support doc (line 3), committed at f7655d8d (not by the delta). Its sections 3.2/4.1 state the pre-decision posture; its Section 11 (lines 83-98) explicitly supersedes 4.1/6/7, records the operator override, and even named "a between-rounds plan edit, deferred until the running HV round completes" - the action the delta fulfilled. Self-consistent via Section-11 supersession. Optional tidies: add "superseded by Section 11" pointers at 3.2/4.1; update Section 11's "deferred until the running HV round completes" to past tense now that c945fc01 landed.

Propagation note (positive): a read-only grep for old-bar language across docs/plans and docs/Project/federation-dolt found NO stale old-provider-bar statement in the normative requirements store (Technical-Requirements.md, Requirements-Matrix.md).

## FAIL AND UNKNOWN LISTS

- FAIL: none.
- UNKNOWN (none lowers a score; infrastructure per MEMORY-HV-001 section 6): MEMORY-BESTPRACTICE-001 section 10 exact text/numbering not independently read (corroborated by commit 30466238 + PR 88); U1/U2 and MEMORY-PREFERENCE-002 live-memory texts not independently read (recorded consistently in the packet); B1-B12 feasibility remains unproven (plan-readiness, not a defect); PDF/HTML exports still carry pre-amendment wording (out of scope, known-stale).

## LIMITATIONS (separate from scores)

- add-profile executed; PROFILE-MERGED.md read in full. Live MCP memory full-text listing via the plugin not performed in the read-only subagent (relied on the verbatim profile carrying the MEMORY-HV-001 / MEMORY-PREFERENCE-002 / MEMORY-BESTPRACTICE-001 anchors, the auto-memory index, and the brief). Infra limitation, not a blocker.
- Read-only plan-readiness review: no build, tests, migration, activation, or PDF render. profile 7.9 TRX not applicable. Read-only grep and git used solely to locate/verify evidence; no state changed, no files edited.

## REMEDIATION PATH

None required. The packet is at the bar. The delta is clean, both amendments present and internally consistent, no R2-R4 regression, and DEPLOY HOLD plus B1-B12 feasibility remain separate operator-owned gates untouched by this verdict. The two optional tidies (OBS-1, OBS-2) are cosmetic and non-blocking. AGREE 98/98 for plan-readiness.
