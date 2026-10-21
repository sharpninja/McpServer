# Round 13 plan-readiness receipt (ClaudeIndependentHostile, claude-opus-5-5/max)

**DISAGREE / NOT-READY**: confidence 94, accuracy 0.98, completeness 0.98; 10 PASS / 2 FAIL / 0 UNKNOWN. Commit `df97c824`, plan SHA-256 `298973F2...948C` (from the manifest, not recomputed); 2513 lines read in full.

## Finding
- **R13-01 (Low): an accepted grading can still be regraded.** §21.1 records GradingInvalid for any second session or distinct capture, and for any LockBlindGrades 400. That includes attempts whose capture is already accepted, and the operator can see those results before locking. GradingInvalid then permits Attempt1, so a valid low score can be followed by a regrade through a legitimate-looking call (re-capture, second session or malformed request). This contradicts 'No regrade follows a valid low score' (L2242) and §23. Only the post-lock case is protected.
- **Fix:** apply the post-lock rule from the moment a capture is accepted: record the event, grant no Attempt1, and refuse activation for that workspace/policy/build. After acceptance, only LockBlindGrades content failures may record GradingInvalid. State the abort-before-termination residual as operator-attested, and add tests.
- My R12-01 wording ('or GradingInvalid') left this open, and in round 11 I had judged the path acceptable.

## R12-01 resolution
Resolved as scoped. §23 makes the results packet final once an attempt exists, with serialized and disclosed corrections. AbandonProtocol is refused after an accepted capture, and activation is blocked while any accepted capture is unlocked, across all subjects.

## Claims
- PASS: C1 C2 C3 C4 C5 C6 C7 C8 C9 C10
- FAIL: C11, C12 (both R13-01)

## Non-blocking observations
- Name the protocol-binding and run-attempt entities.
- Clarify the §19.1 retry wording in light of §23 finality.
- Outcome artifacts have no ingestion path ('opaque existing' in §17.1).
- Exclude host/plugin state from the final diff and add ledger-state markers to the leak test.
- EV1 evidence is thin.
- Cross-check finalResponse against the capture.
- Carry-overs from earlier rounds.

## Path to threshold
Apply R13-01's two-to-three-sentence correction and tests, then re-review. This full read found no other blocking gap.

## Limitations
Read-only packet review; no execution or hash recomputation; source hashes match prior rounds; operator-attested trust boundaries apply; timestamp is date-only.