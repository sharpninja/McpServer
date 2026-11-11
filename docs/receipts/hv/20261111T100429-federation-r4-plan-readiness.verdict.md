# Independent HV verdict: R4 Dolt federation plan packet (plan-readiness re-review after R3 remediation)

- Reviewer: Claude, model claude-opus-4-8, effort max, read-only, independent fresh agent (not a fork).
- Plan author family: Codex; R3->R4 fixes applied by ClaudeCode at operator direction. Reviewer family differs from author (profile 7.1 compliant).
- Requester: ClaudeCode, session ClaudeCode-20261111T145103Z-plugin-session.
- Request receipt: docs/receipts/hv/20261111T100429-federation-r4-plan-readiness.request.jsonl
- Response stream: docs/receipts/hv/20261111T100429-federation-r4-plan-readiness.response.jsonl
- Markdown sources authoritative; stale PDF/HTML export not used.

## OVERALL VERDICT

AGREE. Accuracy 98. Completeness 98. Bar is 98/98 and is met.

The single class of defect that held R3 at 97/97 (an FR phase annotation omitting a phase whose bdpv4 section names one of that FR's acceptance criteria) is closed. R3-N1 and R3-N2 are both resolved identically across all three normative sites. An independent mechanical read-only lint across all 16 FRs and all three sites confirms the three sites agree exactly for every FR, and no FR annotation omits any phase that tokens one of its ACs, the only annotation-omitted phases being the explicitly allowed non-ownership carve-outs (Ph8 re-execution; Ph6 for HubRestoreFromOlderBackup). The eight original R2 findings remain resolved and none regressed. No new findings. Unproven B1-B12 and infrastructure limits are not planning defects (MEMORY-HV-001 section 6 / profile 7.1) and did not lower a score.

## PER-TASK VERDICTS (Ph0-Ph8)

- Ph0: AGREE 98/98. Feasibility stays gated by B1-B12 and "no package installed."
- Ph1: AGREE 98/98. Header FR-186 only; clean.
- Ph2: AGREE 98/98. Header FRs all carry Ph2; named tests map correctly.
- Ph3: AGREE 98/98. Seed/export tests map to FR-187; 91->99 fix stands.
- Ph4: AGREE 98/98 (was DISAGREE 97/96 in R3). FR-184 and FR-183 now include Ph4 for CommitPreviewBlocksSecret and StaleBranchReportedNotDeleted; CommitRetryReturnsSameHash maps to FR-189; FR-182 correctly excluded.
- Ph5: AGREE 98/98. Header FRs all carry Ph5; all named tests map.
- Ph6: AGREE 98/98. Header FRs all carry Ph6; FR-184 += Ph6 present; HubRestore extension carve-out intact.
- Ph7: AGREE 98/98. FR-183 owned (Ph4/Ph7); the six Plugin* plugin-binding carve-out documented in three places.
- Ph8: AGREE 98/98. DEPLOY HOLD intact and not released by plan approval or score; Ph8 re-execution carve-out intact.

## RESOLUTION STATUS OF R3-N1 AND R3-N2

- R3-N1 (FR-184 missing Ph4): RESOLVED. FR-184 now "Phase: Ph2/Ph3/Ph4/Ph5/Ph6/Ph7" identically at current-contracts.md:172, three-system-workflows.md:148 and Requirements-Matrix.md:15. Ph4 justified by CommitPreviewBlocksSecret (FR-184 AC).
- R3-N2 (FR-183 missing Ph4): RESOLVED. FR-183 now "Phase: Ph4/Ph7" identically at current-contracts.md:143, three-system-workflows.md:142 and Requirements-Matrix.md:14. Ph4 justified by StaleBranchReportedNotDeleted (FR-183 AC); consistent with current-contracts.md:163.

No-new-drift: a read-only git diff vs HEAD shows the only annotation changes are exactly the FR-183/FR-184 lines; no other FR annotation changed or became wrong.

## INDEPENDENT FULL-PACKET SWEEP

CLEAN. 41 AC-token naming hits scanned; zero genuine mismatches. Three-site annotation consistency: all 16 FRs match across the three sites. The only annotation-omitted phases are the allowed carve-outs (Ph8 real-host re-execution of ReceiptsAndLogsContainNoCredentials, SqlPortUnreachableAcrossLink, UnknownCertificatePinRejected; Ph6/Ph8 HubRestoreFromOlderBackup extension). The six Plugin* diagnostic ACs are absent as exact tokens in every bdpv4 phase section and are the documented "Ph7 binds each plugin to earlier server contracts" carve-out.

## NEW FINDINGS

None.

Examined and cleared (transparency, not a finding): FR-180 and FR-184 carry Ph7 for their plugin surfaces while FR-176/181/182/185/188/189 omit Ph7 despite each having one Plugin* diagnostic AC whose binding Ph7 owns. This is a curated asymmetry documented in three places (current-contracts.md:163, Requirements-Matrix.md:26, revision-audit-dispositions.md:30), consistent with the packet's convention; it does not lower a score.

## FAIL AND UNKNOWN LISTS

- FAIL: none.
- UNKNOWN (none lowers a score): B1-B12 remain unproven (plan-readiness, not a defect); U1/U2 recorded as operator-resolved (live memory not independently read in the read-only subagent); PDF/HTML export properties out of scope (stale rendering; markdown authoritative).

## LIMITATIONS (separate from scores)

- add-profile ran; PROFILE-MERGED.md read in full. Live MCP memory full-text listing via plugin not performed in the read-only subagent (relied on the verbatim profile + auto-memory index + MEMORY-HV-001/MEMORY-PREFERENCE-002 anchors in the brief). Infra limitation per MEMORY-HV-001 section 6, not a blocker.
- Read-only plan-readiness review: no build, tests, migrations, activation or PDF render. profile 7.9 TRX not applicable; unproven B1-B12 not counted as planning defects.
- Read-only PowerShell (Get-Content, Select-String, git diff HEAD) used solely to locate/verify evidence; PSScriptAnalyzer run before execution; no state changed, no files edited.
- Commit-state observation (not a defect): the corrected FR-183/FR-184 annotations exist in the uncommitted working tree while HEAD still shows the earlier R2-era FR-184 value. The working-tree markdown is the authoritative packet for this review and is correct and internally consistent.

## REMEDIATION PATH

None required. The packet is at the bar. R3-N1 and R3-N2 closed in all three sites with no new drift and no remaining sibling; the eight R2 findings remain resolved. AGREE 98/98 for plan-readiness. DEPLOY HOLD and B1-B12 feasibility remain separate operator-owned gates, untouched by this verdict.
