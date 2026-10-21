# Round 12 plan-readiness receipt (ClaudeIndependentHostile, claude-opus-5-5/max)

**DISAGREE / NOT-READY**: confidence 90, accuracy 0.97, completeness 0.97; 10 PASS / 2 FAIL / 0 UNKNOWN. Commit `ff306098`, plan SHA-256 `A176796D...F075` (from the manifest, not recomputed); 2466 lines read in full.

## Finding
- **R12-01 (Medium): a valid low grading can still be thrown away.** The grading-attempt limits, the one-lock rule and 'no regrade after a valid low score' (§21.1) apply per Evaluation SubjectDigest, and that digest includes the results digests. Nothing makes a protocol's complete RecordResults packet final, so re-recording results with any byte change creates a fresh subject whose lock satisfies 'frozen protocol has a lock'. AbandonProtocol refuses only locked protocols, so a protocol with an accepted but unlocked grading can be abandoned. Activation never refuses on accepted gradings that were not locked. Because the operator holds the map, they can see an unfavorable result before locking and discard it.
- **Fix:** (a) once a grading attempt exists, the protocol's complete packet is final (409 evaluation_results_locked; identical repeat idempotent; corrections only before any attempt) and accepted EvalResult bytes are immutable; (b) refuse AbandonProtocol once a grading capture is accepted; (c) activation refuses while any accepted capture is unlocked. Add tests for each.
- I missed this in round 11 and in my R10-01 correction.

## R11-01 resolution
Resolved on all four points. §22 keeps captures audit-only, enforces a strict projection allowlist with neutral summaries, refuses invalid projections before Ready without consuming an attempt, and extends the canary test.

## Claims
- PASS: C1 C2 C3 C4 C5 C6 C7 C8 C9 C10
- FAIL: C11, C12 (both R12-01)

## Non-blocking observations
- observableArtifactIds has no producer after §22, and §17.1 still calls them 'existing'.
- The frozen final-diff definition should exclude host and plugin state, and the leak test should add ledger-state markers.
- EV1 evidence is thin.
- finalResponse should be cross-checked against the capture.
- Carry-overs from round 11.

## Path to threshold
Add R12-01's three rules and tests to §21.1, align §17.1, then re-review. This complete read found no other blocking gap.

## Limitations
Read-only packet review; no execution or hash recomputation; source hashes match prior rounds; resolve-cache-dir.ps1 is outside the packet; operator-attested trust boundaries apply; timestamp is date-only.