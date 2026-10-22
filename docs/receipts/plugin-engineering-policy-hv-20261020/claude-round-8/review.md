# Claude independent hostile receipt: plan readiness, round 8

**Identity:** ClaudeIndependentHostile, a continued independent session separate from the Codex author. Requested: ClaudeCode / claude-opus-5-5 / max.
**Snapshot:** commit 5c31b6633a66855db87c70a1d63320dc743c0f69; plan SHA-256 16CC55878F90BD48163E6F84A5E754C28F7AD1A6B157FF8E709511F5922AB808. I read the plan in full (L1-1958). Source hashes are unchanged.
**Verdict:** DISAGREE / NOT-READY. Accuracy 0.97, completeness 0.96, confidence 84. Claims: 9 PASS, 3 FAIL (C6, C11, C12).

R7-01 is resolved in substance. §18 adds signed suppression valid only for Evaluation graders, a neutral signed context, fresh sessions, a blinded projection, Disabled on every prompt, the single shared rerun, and canary tests across nine adapters. Consistent edits appear in §3, §5, §10, §13.5, §14.5, §15.1, §16.1 and §17.1. Two gaps remain.

## Findings

**R8-01 Medium: lock-then-unblind mechanics are unspecified.** §18 L1926-1933 (with §13.5 L1002) says the grader sees the full protocol and map only after quality scores are signed and hashed. It then requires an EV protocol/aggregate audit after unblinding, forbids revising the locked grades, and lists a locked-score immutability test. Nothing defines how this works:
- No lock record exists. evaluation-artifacts has only FreezeProtocol and RecordResults (§17.1 L1810-1815), and the receipt schema has no per-run grades (§14.3 L1167-1170).
- There is no post-lock input path: §16.1 L1700-1702 allows one materialization per review workspace.
- No actor is named for the post-unblinding audit.
- No AttestReview check ties the final aggregates to the locked grades.

*Minimal fix:* make grading a single blind session whose captured response carries per-run grades, stored as an EvalBlindGrades artifact (the lock). The server recomputes per-condition pooled ratios from those grades plus a withheld EvalUnblindingMap (its hash fixed in the protocol) and stores an EvalAggregate artifact for AttestReview and activation. No grader runs after unblinding (or specify an audit materialization). Add tests.

**R8-02 Low: plumbing gaps in §18.**
- (a) The §13.4 L906 ledger has only the states Reserved, Delivered and Unavailable, but §15.5 and §18 (L1903, L1936, L1950) require per-prompt Disabled entries. This part was latent since round 5 and I missed it.
- (b) No status or read channel delivers the neutral profile, memories and hash that the plugin must inject and verify (§3 L124-132).
- (c) Validating that selected memory IDs exist at startup (L1883-1884, L1896-1897) means a deleted memory could fail server startup.

*Fix:* add the Disabled state and its writer, add a graderContext read, and validate existence and hash at materialization, launch and dispatch.

## Claim verdicts
- **PASS:** C1, C2, C3, C4, C5, C7, C8, C9, C10.
- **C6 FAIL:** no lock or audit records (R8-01).
- **C11 FAIL:** R8-01 and R8-02.
- **C12 FAIL:** decisions remain open.

## Path to threshold
Apply the two fixes above, then run a fresh review. No other consequential gap was found. With both edits and no new contradictions introduced, all 12 claims are projected to PASS.

## Limitations
- Read-only review; no execution or hash recomputation.
- Base and source files were reused from earlier rounds because their hashes are unchanged.
- No live host behavior was observed; findings rest on the plan text.
- TimestampUtc carries the session date only.

## Commands
Read and Grep only, as listed in Receipt.CommandsExecuted. No shell, build or test commands were run.