# PR87 hostile validation Round 15

The terminal Claude Code review completed at 2026-11-09T14:44:41-06:00.
Timestamps later than that in Claude's streamed prose were unmeasured estimates and
are superseded by the runner timestamps in
`20261109T142334-unitgreen-r15.done.json`.

- Verdict: DISAGREE
- Accuracy: 95
- Completeness: 95
- Confidence: 90
- Reviewer: Claude Code 2.1.318, `claude-opus-5-5`, high effort
- Reviewed records head: `234ffce4af81ba8a5b05610bec934be633cae262`
- Reviewed code/test head: `a1e19d7331a2e9804c66ef96a27ee50b2365510c`
- Claims: 55 PASS, 5 FAIL, 1 UNKNOWN, 1 OVERRIDDEN, 1 OUT OF SCOPE
- Regression summary: none

## Findings

### R15-F1: real-discovery ownership guard

`RecordedCaseInventory` in `tests/Build.Tests/TestProjectOwnershipTests.cs`
uses hard-coded per-project counts and synthesized case names. It still asserted
4,835 unit and 777 integration cases while the current Linux unit gate discovered
4,847 cases. A unit-owned project can therefore acquire an integration-category
case that is filtered from Test and not selected by the integration lane while the
ownership binding remains green.

Remediation required by Claude:

- Append TR-MCP-NUKE-019 AC-7 so ownership uses real discovered cases and fails
  on role/category drift.
- Add `OwnershipInventory_ComesFromDiscoveredCases_NotRecordedCounts`.
- Add `UnitOwnedProject_WithDiscoveredIntegrationCase_FailsTestTarget`.
- Add `IntegrationOwnedProject_WithDiscoveredUnitCase_FailsLane`.
- Add `RecordedCountDrift_IsRejected`.

### R15-F2: undeclared and scratch-only evidence

The first final-head Build.Tests attempt ran 530 cases with 93 failures because a
TMPDIR under `.mcpServer` caused fixture-root resolution to fall back to
`/QBrainAiGateTemp`. Two earlier filtered Build.Tests attempts also exited 1.
The records did not declare those attempts. Several fixture-cleanup and Windows
receipts cited by the P1 record existed only in ignored scratch storage.

### R15-F3: stale launch records

The brief named the code head as the launch head instead of records head
`234ffce4`, named nonexistent stable JSONL paths and an obsolete task snapshot,
repeated 72 tasks instead of 75, retained a literal launch-stamp token, and used
boilerplate sentences where exact values were required.

### R15-F4: cleanup negative controls and red evidence

The cleanup retry commit cited a compile failure as its red gate. Its tests cover
success after retry but do not prove the 20th persistent IOException propagates
or that a non-IOException is not retried.

Remediation required by Claude:

- Append TR-MCP-EMBEDDING-001 `test-owned-fixture` with bounded transient-IO
  retry and final/non-IO propagation behavior.
- Add `DeleteOwnedRootAsync_PersistentIOException_ThrowsAfterBoundedAttempts`.
- Add `DeleteOwnedRootAsync_NonIOException_IsNotRetried`.
- Record a valid scripted red gate before the green implementation.

### R15-F5: task text versus Linux-only publication

Tasks 72 and 74 still required both-host final gates after the operator explicitly
narrowed PR87 publication to Linux. The operator-directed resolution is to amend
those tasks to Linux-only final publication evidence while keeping the green
03eea747 two-host baseline and the Windows unit composition evidence as history.

## Per-task verdicts

AGREE 98/98: 8, 10, 14, 15, 21, 22, 23, 24, 25, 29, 30, 40, 60,
63, 65, 66, 67, 68, 69, 70, 73.

DISAGREE: 62 at 98/96; 64 at 96/96; 71 at 96/96; 72 at 95/95; 74 at
96/95.

Out of scope and still open: 12, 13, 35, 75.

Regression checks with no regression: 1, 9, 16, 26, 27, 37, 38, 39, 41, 42,
43, 44, 45, 47, 53, 57, 58, 59, 61.

## Operational disclosures

Claude recorded its Bash-launcher deviations, a stray Default plugin-session turn,
incomplete live-memory listing, and timestamp-hook failures. Reading the complete
marker placed the live workspace key into six streamed JSON fields. Codex replaced
all six occurrences with `[REDACTED:api-key]`; the request and response JSONL then
parsed with zero errors and contained zero live-key matches.

The complete reviewer output is preserved in the paired request and response
JSONL. The structured verdict is
`hostile-validator-unitgreen-r15-20261109T142334.json`.
