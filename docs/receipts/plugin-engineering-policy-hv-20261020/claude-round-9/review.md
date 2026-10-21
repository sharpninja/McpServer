# Claude independent hostile receipt: plan readiness, round 9

**Identity:** ClaudeIndependentHostile, a continued independent session separate from the Codex author. Requested: ClaudeCode / claude-opus-5-5 / max.
**Snapshot:** commit 5a38235f97df07dfe490887aacce9b7faf283228; plan SHA-256 7D99AD570A92F8460775811436015BAD5189908CAB4C11C1F7B95765BE8D5501. I read the plan in full (L1-2081). Source hashes are unchanged.
**Verdict:** DISAGREE / NOT-READY. Accuracy 0.97, completeness 0.97, confidence 86. Claims: 8 PASS, 4 FAIL (C6, C10, C11, C12).

R8-02 is fully resolved: the Disabled ledger state, the graderContext status channel, and shape-only startup validation. R8-01 is resolved in design: one blind grader, integer per-run grades, a map committed at freeze and stored broker-only, exact server-side rational aggregates, and recomputation at attestation. Two input and rollout gaps remain.

## Findings

**R9-01 Medium: the grade lock has no defined inputs.** LockBlindGrades (§19.1 L2004-2008) must resolve a 'broker-stored capturedResponseArtifactId from 16.1' together with its launch, materialization and CLI identity. It must also verify that every grader prompt was Disabled. However:
- No operation creates that capture. §16.1 L1721-1725 only says 'broker capture stores it', and there is no launch record.
- The Disabled evidence sits in the grader host's plugin-local turn-state ledger (§13.4 L904-909) with no upload path.
- AttestReview's raw-receipt upload cannot fill the gap, because attestation requires the lock first.

*Fix:* add an operator-only review-materialization captures operation. It stores the raw CLI stream, the session export and the ledger snapshot, binds them to the materialization, parses CLI identity, and returns capturedResponseArtifactId; captures are idempotent and immutable. LockBlindGrades then checks a Disabled entry for every grader turn the server minted. Add tests.

**R9-02 Low: activation is undefined.** §19.1 L2042-2043 and L2052 require activation-time re-checks and refusal. No activation operation or code exists: §14.5 L1256-1258 and §15.4 L1638-1642 omit the evaluation, and default-on guidance has no change event to gate.

*Fix:* on staging/production instances with a signed EvaluationGateRequired setting, reconciliation refuses the first EnforcementEnabled=true or BDPv4 change with engineering_activation_evaluation_required unless a current Evaluation attestation (matching policy and build) has a passing recomputed aggregate. The plugin release checklist cites the same attestation. Add tests.

## Claim verdicts
- **PASS:** C1, C2, C3, C4, C5, C7, C8, C9.
- **C6 FAIL:** no capture or evidence producer (R9-01).
- **C10 FAIL:** R9-02.
- **C11 FAIL:** R9-01 and R9-02.
- **C12 FAIL:** decisions remain open.

## Path to threshold
Apply the two fixes above, then run a fresh review. No other consequential gap was found. With both edits and no new contradictions introduced, all 12 claims are projected to PASS.

## Limitations
- Read-only review; no execution or hash recomputation.
- Base and source files were reused from earlier rounds because their hashes are unchanged.
- No live behavior was observed.
- TimestampUtc carries the session date only.

## Commands
Read and Grep only, as listed in Receipt.CommandsExecuted. No shell, build or test commands were run.