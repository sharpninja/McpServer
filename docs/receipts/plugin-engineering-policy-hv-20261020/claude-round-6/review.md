# Claude independent hostile receipt: plan readiness, round 6

**Identity:** ClaudeIndependentHostile, a continued independent reviewer session separate from the Codex author. Requested: ClaudeCode / claude-opus-5-5 / max.
**Snapshot:** commit d3371a5f3daf8ff58c96feec9647d4ba689d1b89; plan SHA-256 EDE98A25B693363EC9DEAB37783D0839A71F5764FF31A8002513B55A4BCFFDCD (manifest value). I read the plan in full (L1-1777). Source hashes are unchanged, so earlier source reads were reused.
**Verdict:** DISAGREE / NOT-READY. Accuracy 0.97, completeness 0.95, confidence 80. Claims: 7 PASS (C1, C2, C3, C4, C5, C7, C9), 5 FAIL (C6, C8, C10, C11, C12).

All four round-5 findings are resolved. Two new gaps remain, both in the §16.1 review-materialization contract.

## Findings

**R6-01 Medium: Baseline/Inventory review snapshots and evaluation artifacts have no creating operation.** §16.1 L1687-1688 materializes a 'Baseline proposed snapshot/diff reference' and an 'Evaluation immutable harness-manifest artifact ID'. Nothing creates either one:
- Snapshots are captured only by agent evidence, validation jobs, completion preflight, or RecordBaseline, and RecordBaseline already requires the attestation (§13.1 L727-736). §13.3 L836's 'captured snapshot' has no capturing operation.
- No operator operation stores evaluation protocols, run manifests or results, and the ArtifactsJson kinds (§13.4 L954) cover none of them.
- The plan never says which snapshot becomes source/ for an Inventory review, or what the Evaluation source/ packet contains.

As a result, the bootstrap baseline that enforcement requires (§14.5 L1243-1244), post-merge adoptions and the evaluation attestation cannot be produced. This gap was latent in earlier rounds and I missed it; §16.1's explicit subject-reference list exposes it.

*Fix:*
- Baseline/Inventory materialization captures the checkout and input repositories via §6/§14.4 and persists the snapshot. For Baseline, the old snapshot is the certified pointer and the manifest records the diff and ReviewedDiffSha256.
- RecordBaseline recaptures and must match, otherwise 409 engineering_baseline_conflict.
- Add an operator-only evaluation-artifacts admin operation with new artifact kinds. Define the Evaluation source/ packet and the workspace that holds its attestation. Add owners and tests.

**R6-02 Low: the secret scan is undefined.** §16.1 L1700-1701 refuses materialization when a secret scan fails but defines no scanner, rules or credential set. Reusing the pattern-based SecretRedactor (base arch §3 L87) could flag fixture credentials and permanently refuse reviews of this repository, with no allowlist or acknowledgement path.

*Fix:* refuse only on exact matches to known live credentials (source and review marker/API keys and other configured secrets), mirroring the marker-key scan at base plan L198. Record pattern hits as non-blocking findings without redaction. Add tests.

## Round-5 verification
- **R5-01:** resolved by §16.1 (endpoint, entity, errors, materializationId binding, operator launch, VERDICT-block receipt, explicit base-protocol amendment).
- **R5-02:** resolved by §9 L541-544 and §16.2 (capability>=1 boundary, package metadata, refusal to capability-0 targets).
- **R5-03:** resolved by §15.4 and §16.3 (ingressReplay object; top-level boundAtUtc is 400).
- **R5-04:** resolved by §13.3, §14.1, §14.3, §15.1, §15.2 and §16.3 (supersession ID array, four-claim Slice profile, authoritative failure-memory tuple).

## Claim verdicts
- **C1 PASS:** §1, §3, §5, §11.
- **C2 PASS:** §1 L22-37; §9 L541-544; §15.1 and §16.1 amendments and owners.
- **C3 PASS:** §2 is unchanged and matches sync-plugin-core.ps1.
- **C4 PASS:** §3, §13.1, §14.5, §15.4.
- **C5 PASS:** §5, §13.4, §14.7, §15.4.
- **C6 FAIL:** R6-01 (missing interfaces and artifact kinds).
- **C7 PASS:** §15.2 and §16.3 bindings, consistent with §14.1.
- **C8 FAIL:** R6-01 and R6-02.
- **C9 PASS:** §9, §13.2, §14.6, §15.3.
- **C10 FAIL:** no bootstrap or adoption capture path (R6-01).
- **C11 FAIL:** evaluation artifacts (R6-01) and the scan risk (R6-02).
- **C12 FAIL:** approvals are retained, but R6-01 and R6-02 remain open.

## Path to threshold
Apply the two fixes above, then run a fresh review. No other consequential gap was found. With these fixes and no new contradictions introduced, all 12 claims are projected to PASS.

## Limitations
- Read-only review; no execution or hash recomputation.
- Base and source files were reused from earlier rounds because their hashes are unchanged.
- Whether repository fixtures contain credential-shaped strings could not be checked, because test sources are outside the packet.
- TimestampUtc carries the session date only.

## Commands
Read and Grep only, as listed in Receipt.CommandsExecuted. No shell, build or test commands were run.