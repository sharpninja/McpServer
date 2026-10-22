# Claude independent hostile receipt: plan readiness, round 7

**Identity:** ClaudeIndependentHostile, a continued independent reviewer session separate from the Codex author. Requested: ClaudeCode / claude-opus-5-5 / max.
**Snapshot:** commit 1c21bc5ab1653c09a7681f68fe6de7a7e981906e; plan SHA-256 F9EBC2EEF5BEBD42CB5D5271FB205848572AA4FF398857FD87B719112A706ED7 (manifest value). I read the plan in full (L1-1856). Source hashes are unchanged.
**Verdict:** DISAGREE / NOT-READY. Accuracy 0.97, completeness 0.97, confidence 86. Claims: 10 PASS, 2 FAIL (C11, C12).

Both round-6 findings are resolved. One finding remains: it was latent since round 5 and I missed it earlier.

## Finding

**R7-01 Medium: the evaluation grader is exposed to the policy it is grading.** The plan promises a condition-blind grader that receives only the task, rubric and observable outcomes (§10 L614; §13.5 L993-994). However:
- The grader must be a plugin-enabled Claude Code session that records its own Chore turn, in a review workspace on the owner (§15.1 L1463-1471; §16.1 L1691; §17.1 L1822-1823).
- GuidanceEnabled is an instance-only, default-on setting (§3 L96), and the policy is delivered on every prompt (§5 L280-281).
- So every grader prompt carries the engineering-policy baseline and activity text, which is the treatment under evaluation.
- §15.1 L1472 also applies the global/operator profile to grader sessions.

This contradicts the stated blinding of an activation-blocking evaluation and leaves suppression to implementers.

*Fix:*
- Add an operator-signed EngineeringGuidanceSuppressed flag for Evaluation grader review workspaces only (DevelopmentProcess=None). Status then reports guidanceEnabled=false and the plugin records Disabled; HV review workspaces are unchanged.
- Treat grading as infrastructure-invalid unless every grader prompt's ledger outcome is Disabled, under the single paired-rerun rule.
- Reconcile §13.5 with §15.1: grader context is the task, rubric and outcomes plus an operator profile that does not describe the evaluated policy, conditions or fixtures.
- Add a test that a policy canary never reaches the grader context.

## Round-6 verification
- **R6-01 resolved:** §17.1 has Baseline/Inventory materializations capture snapshots directly (double capture, budgets, persisted row), with a bootstrap path that needs no prior evidence. RecordBaseline recaptures and must match exactly, otherwise 409. A new mTLS evaluation-artifacts operation has a two-mode DTO, an immutable frozen protocol and new artifact kinds (§13.4 L954). The Evaluation source/ is the anonymized packet, and the attestation is recorded on the workspace being activated. §16.1 references are corrected.
- **R6-02 resolved:** §17.2 refuses only on exact matches to known live credentials, read in broker memory with a catalog recheck. Pattern hits are non-blocking findings, bytes are never redacted, and 'Ready' is honestly bounded. The 16.1 wording is corrected.

## Claim verdicts
- **C1-C10 PASS.** Notably: C6 (17.1 endpoint, DTO and kinds), C8 (17.1 capture, 17.2 scan), C10 (bootstrap and adoption path now exist).
- **C11 FAIL:** R7-01.
- **C12 FAIL:** R7-01 leaves a validation-design choice open.

## Non-blocking observations
- The protocolId-to-hash binding has no named table, though its invariant is specified.
- Evaluation observations would be embedded in resultJson.
- The harness manifest and the anonymous run manifest share the EvalRunManifest kind.

## Path to threshold
Apply the R7-01 fix above. No other consequential gap was found. With this edit and no new contradictions introduced, all 12 claims are projected to PASS.

## Limitations
- Read-only review; no execution or hash recomputation.
- Hash-identical base and source reads were reused from earlier rounds.
- R7-01 relies on the plan's own statements about hooks and instance-wide guidance; real host behavior was not executed.
- TimestampUtc carries the session date only.

## Commands
Read and Grep only, as listed in Receipt.CommandsExecuted. No shell, build or test commands were run.