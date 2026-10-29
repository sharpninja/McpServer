# Claude independent hostile receipt: plan readiness, round 5

**Identity:** ClaudeIndependentHostile, a continued independent Claude reviewer session separate from the Codex author. Requested: ClaudeCode / claude-opus-5-5 / max.
**Snapshot:** commit b4f324576d78e384dbc55946f4f13e8e28f8fb35; plan SHA-256 A30805F7DE2A240D70A248BA814546DE4AA63A6F8F75671435283CC051932117 (manifest value). I read the plan in full (L1-1672). Source-reference hashes are unchanged from round 4, so my round-4 source analysis was reused.
**Verdict:** DISAGREE / NOT-READY. Accuracy 0.95, completeness 0.93, confidence 74. Claims: 5 PASS (C1, C3, C4, C5, C9), 7 FAIL, 0 UNKNOWN.

Section 15 resolves the round-4 High finding and most of the Mediums. Four narrow gaps remain, two of them introduced by the new text.

## Findings

**R5-01 Medium: review-source materialization is undefined.** §15.1 L1461-1463 has reviewers work over a read-only materialization of the exact bundle, and §14.2 L1124-1125 adds before/after bundles. No operation, owner, path or hash check produces them; the only bundle route serves producer jobs (§14.4 L1197-1200). The plan also makes the operator the launcher and removes receipt writes without saying how the reviewer emits its receipt. Its amendment list omits the base rule that the implementing agent launches the run (base plan L196-197).

*Fix:* add an operator-only review-materializations admin operation that reuses the §14.4 materializer for read-only source/ and before/ plus a manifest, verifies hashes, and binds a materializationId into the brief and attestation. State that the operator launches the reviewer and that the receipt is emitted only in the captured final response, and add both to the amendment list. Add tests.

**R5-02 Medium: the base fold-in left stale ownership and an ambiguous rollback threshold.** §15.5 L1645-1647 moves the Format column, Plain backfill and DTO field into base, but §9 L541-542 still assigns the AC-format migrations to this plan. Base binaries now carry the column without structured handling, so 'format support' (§11 L645-647) and 'format-aware' (§14.6) no longer identify the rollback targets the census guard must refuse.

*Fix:* correct §9, and define format support as acceptanceCriteriaFormatVersion>=1, with base fold-in binaries counting as older.

**R5-03 Low: the mint request cannot tell live from replay.** §15.4 L1602-1603 rejects a live caller sending boundAtUtc but accepts it on replay. Live and drain mints share one request shape (arch §4.2 L633-637).

*Fix:* add an ingressReplay member carrying boundAtUtc, and reject any boundAtUtc outside it.

**R5-04 Low: residual attestation and failure-memory contradictions.**
- (a) A single SupersedesAttestationId (§13.3 L817, L826) conflicts with the several prior IDs in §15.2 L1500-1501.
- (b) Slice reuse for a baseline (§15.1 L1484-1485) adds Scope/Safety/Provenance but not the Baseline profile's Completeness claim (§13.3 L836), and the §14.3 Slice default (L1140-1141) is not updated.
- (c) Failure memory is keyed by InventoryDigest (§15.2 L1514-1515), yet L1518 and §14.1 L1025-1026 require a changed snapshot.

*Fix:* use a supersession ID array, give the Slice profile all four baseline claims, and make the key authoritative.

## EP-R4 check
- Resolved: 03, 06, 07, 09.
- Resolved with a residual: 01 (R5-01, R5-04b), 02 (R5-04a), 04 (R5-04c), 05 (R5-03), 08 (R5-02).

## Claim verdicts
- **C1 PASS:** §1, §3, §5, §11.
- **C2 FAIL:** R5-02 and the launcher amendment in R5-01.
- **C3 PASS:** §2 is unchanged and matches sync-plugin-core.ps1.
- **C4 PASS:** §15.4 adds the manifest channel and status selection.
- **C5 PASS:** delivery rules are unchanged; status is read-only per session.
- **C6 FAIL:** R5-03, R5-04a, and the missing materialization endpoint (R5-01).
- **C7 FAIL:** §15.1 and §15.2 close the deadlock and the binding. Fails on R5-04b and R5-04c.
- **C8 FAIL:** the §15.3 layout is executable (consistent with Build.SyncAgentPlugins.cs). Fails on R5-01 and R5-04c.
- **C9 PASS:** §15.3 makes the G6 and PowerShellOnly gates runnable.
- **C10 FAIL:** R5-02 (rollback threshold) and R5-03.
- **C11 FAIL:** the evaluation rules are bounded and explicit, but the source for independent review is undefined (R5-01).
- **C12 FAIL:** approvals are retained, but R5-01 to R5-04 remain open.

## Path to threshold
Apply the four fixes above, then run a fresh review. No other consequential gap was found. With these fixes and no new contradictions introduced, all 12 claims are projected to PASS.

## Limitations
- Read-only review; no builds, tests or hash recomputation.
- Base and source files were not re-read; hash-identical round-4 reads were reused.
- Files outside the packet were not inspected: build.ps1, Build.UpdateService.cs, TodoExecutionService, the RequirementAcceptanceCriterionEntity, resolve-cache-dir.ps1 and the marker ignore status, and whether the gate-started-at-utc parameter exists.
- TimestampUtc carries the session date only.

## Commands
Read, Grep and Glob only. No shell, build or test commands were run.