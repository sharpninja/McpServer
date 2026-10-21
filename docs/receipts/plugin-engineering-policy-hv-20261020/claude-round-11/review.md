# Round 11 plan-readiness receipt (ClaudeIndependentHostile, claude-opus-5-5/max)

**DISAGREE / NOT-READY**: confidence 90, accuracy 0.97, completeness 0.97; 10 PASS / 2 FAIL / 0 UNKNOWN. Commit `eec058b4`, plan SHA-256 `4C1CCAC9...057F` (from the manifest, not recomputed); 2403 lines read in full.

## Finding
- **R11-01 (Medium): the blind grading projection can expose the treatment.** §21.3 stores each run's raw CLI result, session export and DeliveryLedger and lists them as that run's observables. §18 makes observable outcomes grader-readable, and §19.1 puts ledger evidence into the projection. Integrated session exports contain the injected policy text, and ledger entries say Disabled or Delivered, so the grader can tell which condition each run belongs to. This contradicts 'without treatment labels' and §18's canary premise. **Fix:** keep captures as server-only validation and audit evidence and never project them. Make the projection an explicit allowlist: neutral task/rubric fields, anonymous run IDs, frozen outcome fields, and a neutral per-run validity summary (no ledger, policy, hook or status data). Refuse a non-allowlisted projection with 409 evaluation_projection_invalid before Ready, consuming no attempt. Extend the canary test so the canary is present in captures but absent from the projection and grader requests. This partly comes from my own R10-03 wording.

## R10 resolutions
- **R10-01: resolved.** §21.1 bounds grading attempts, persists the invalid states, enforces one lock per subject, requires the attested receipt to match the lock, and refuses activation on failing locks or unfinished protocols unless abandonment is signed.
- **R10-02: resolved.** HeldInactive goes through the normal 400 with a new remedy and status states. The exact nine-package BuildArtifactManifest is verified end to end.
- **R10-03: resolved as scoped,** but its observables wording causes R11-01. Per-run exact captures are now ingested with operator-attested remote binding. Every target freezes before runs and grades, locks and attests locally.

## Claims
- PASS: C1 C2 C3 C4 C5 C6 C7 C8 C9 C10
- FAIL: C11 (honest blind evidence), C12 (projection content left to implementers)

## Non-blocking observations
- The freeze-row and run-attempt registries are implied rather than named entities.
- The RecordResults request size and streaming are unspecified.
- §17.1 still describes observables as 'existing'.
- The drain update is assigned to HostPluginAdoption but is shared plugin-core code.
- WorkspaceId is a path, so targets can collide; this is benign.
- The unavailable-policy and overflow fixtures must stay valid under §15.5.

## Path to threshold
Add R11-01's correction, propagate it to §17.1, §18 and §19.1, then re-review. This complete read found no other blocking gap.

## Limitations
Read-only packet review; no tests or hash recomputation; source hashes match prior rounds; base architecture only grepped for WorkspaceId; operator-attested trust boundaries apply; timestamp is date-only.