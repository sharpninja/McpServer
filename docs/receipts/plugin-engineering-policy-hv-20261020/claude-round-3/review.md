# ClaudeIndependentHostile plan-readiness receipt, round 3

**Identity:** ClaudeIndependentHostile. Requested ClaudeCode / claude-opus-5-5 / max; actual identity is for the coordinator to confirm.
**Snapshot:** commit 3a68ad224a4efbc88003f769e25b3a5beb49e780; plan SHA-256 8C2DEE675794F6A26D438D61E0E008E7C584C536E15EC8F853A8B3A18F0AC7FC (taken from the manifest); all 998 lines read.
**Verdict:** DISAGREE / NOT-READY. Accuracy 0.88, completeness 0.79, confidence 38. PASS 2, FAIL 10, UNKNOWN 0.

Section 13 resolves most R2 items. Fourteen findings remain; each either leaves an implementation-defining choice open or conflicts with the base architecture.

## Findings (ranked)
1. **EP-R3-01 High:** Attempt is supplied by the agent and no rule says which attempt satisfies a gate, so retrying until green can pass a zero-fail gate. Fix: assign attempts on the server; require every finished attempt to be Pass; allow one recorded rerun for infrastructure failures.
2. **EP-R3-02 High:** the cross-host producer path (registration, dispatch, bundle transfer, authentication, failure mapping) is undefined, yet Linux and Windows receipts are mandatory.
3. **EP-R3-03 High:** the TODO-exit gate never says which TODOs need SliceExit; a TODO has no declared class or opening snapshot.
4. **EP-R3-04 Medium:** RequirementsDigest, the AC revision, the in-scope ACs, the Slice subject and the diff digest have no canonical inputs; the base Requirement hash excludes Format with no stated amendment.
5. **EP-R3-05 Medium:** reviewer profile and override undefined; §9 and §13.3 disagree on the Claude substitution scope; no receipt-to-subject binding; the evaluator receipt cannot be attested (no Evaluation SubjectKind, profile conflict); eval baseline not pinned.
6. **EP-R3-06 Medium:** OpeningSnapshotBoundUtc and the deployment-ID fields become non-null with no default, which breaks binary rollback.
7. **EP-R3-07 Medium:** the deployment-controller census has no owner or interface.
8. **EP-R3-08 Medium:** judging G5Traceability by process exit conflicts with the base strict-run pass rule; Red is not allowed on TestDesign TODOs.
9. **EP-R3-09 Medium:** exact-byte digests conflict with the base en/em-dash normal form.
10. **EP-R3-10 Medium:** runner, admin and producer configuration missing; signed-manifest format and validator dependency undecided.
11. **EP-R3-11 Low/Medium:** incomplete amendment list; owners contradict arch §8.3; no QB-AGENT-001 ACK for the C8/C9 change.
12. **EP-R3-12 Low/Medium:** precedence between the 409 and 400 refusal channels and the drain handling are unspecified.
13. **EP-R3-13 Low/Medium:** 'may advance' certified pointer; late mints bind the newer pointer.
14. **EP-R3-14 Low/Medium:** contended-lock outcome, retry trigger and per-request injection undefined.

The edits that resolve each finding are listed in path_to_threshold.

## R2 verification
Resolved: R2-07, R2-10. Mostly resolved: R2-02, R2-06, R2-09, R2-12. Partial: R2-01, R2-03, R2-04, R2-05, R2-08, R2-11.

## Claim verdicts
C1 PASS. C2 FAIL (EP-R3-09, -11). C3 PASS. C4 FAIL (-05, -10). C5 FAIL (-14). C6 FAIL (-02, -06, -07, -09, -10). C7 FAIL (-01, -03, -05, -12). C8 FAIL (-01, -02, -04, -09, -13). C9 FAIL (-01, -08). C10 FAIL (-06, -07). C11 FAIL (-05). C12 FAIL (aggregate).

## Evidence checked
- ValidateTerminalTurnCompliance exists (SessionLogService.cs L1329).
- The sync script copies lib-ps byte-for-byte with SHA-256 hashes; the PowerShell-only content rule covers .json.
- TestRunId produces TRX (Build.Test.cs L27-36).
- Base 1.120.0, four-attempt and C8/C9 rules match the plan.
- Base normal form (arch L165), contended lock (L221), drain quarantine (L1221-1222), §8.3 owners and base plan L189 are the conflicts cited above.

## Limitations
Read-only packet with no transcript; targeted base reading; files outside the packet not inspected; hashes not recomputed; no builds or tests run; advisory note on LF protection for the policy asset.