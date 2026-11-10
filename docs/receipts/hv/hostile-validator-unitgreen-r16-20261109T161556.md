# PR87 hostile validation Round 16

Claude Code completed the review at 2026-11-09T16:28:59-06:00. The verdict header's earlier stamp was not measured with the timestamp guard; the runner timestamps above are authoritative.

- Verdict: DISAGREE
- Accuracy: 97
- Completeness: 96
- Confidence: 90
- Reviewer: Claude Code 2.1.318, `claude-opus-5-5`, high effort
- Reviewed records head: `b164cf23fa930b98b6a48bb442dad4d412dd5962`
- Reviewed code/test head: `167afdcb1bbe4ce8e54770fb5882cc8a48d82b01`
- Claims: 34 PASS, 2 FAIL, 0 UNKNOWN, 1 OVERRIDDEN, 4 OUT OF SCOPE
- Regression summary: none

## Findings

### R16-F1: stale recorded-layout ownership assertions

`TestProjectOwnershipTests` still used `RecordedCaseInventory` and asserted historical totals of 4,835 unit and 777 integration cases as current exactly-once coverage even though the final Test gate ran 4,847 cases. The production target wiring correctly used runtime discovery, but the stale binding continued to cite TR-MCP-NUKE-019 AC-7 and AC-8.

Required remediation: restate the ownership and quarantine decision tests over an explicitly synthetic scripted inventory, retain the 29-case quarantine assertions, remove current-layout count claims, and correct the class documentation.

### R16-F2: discovery safety branches lacked negative controls

The empty all-case result, duplicate identity, filtered identity absent from all-case discovery, missing test-list header and unindented parser boundary were implemented but not individually pinned by tests.

Required controls:

- `Discover_EmptyAllCaseResult_FailsTarget`
- `Discover_DuplicateIdentity_FailsTarget`
- `Discover_FilteredIdentityAbsentFromAll_FailsTarget`
- `DotNetListTestDiscovery_MissingHeader_Throws`
- `DotNetListTestDiscovery_UnindentedLineEndsList`

Claude recommended appending AC-7 so those conditions fail the target before tests run and so any AC-7/AC-8 ownership test uses runtime-discovered input or explicitly synthetic input rather than recorded current-layout counts.

## Per-task result

- AGREE 98/98: 62 and 74.
- DISAGREE 97/95: 64 and 71 on R16-F1/R16-F2.
- DISAGREE 98/96: 72 because remediation moves the final head.
- Round 15 acceptances retained with regression checks and no regression: 8, 25, 29, 30, 63, 65 through 70 and 73.
- Other selected completed tasks had no regression; out of scope and still open: 12, 13, 35 and 75.

## Verified evidence

Claude recomputed every one of the 14 TRX hashes, 10 source pins, the 9,450,351-byte total and four receipt hashes without running tests. Every TRX had zero failures and zero not-executed results. It verified all five Round 15 remediation items, runtime target wiring, the 75-task map, launch paths and the embedding cleanup contract.

## Operational disclosures

Claude used its Bash tool for read-only inspections because the launcher's write prohibition prevented creating the PowerShell scripts its own ruling expected. It also emitted no numeric progress checkpoint, and the plugin attributed its review turn to the Default session instead of ClaudeCode; Claude filed triage report `triage-report-161b55eda47b4599b42a7c430fe49c17`. Reading the complete marker placed the live workspace key into six streamed fields. Codex replaced every occurrence with `[REDACTED:api-key]`, then verified both JSONL files parse with zero errors and contain zero live-key matches.

The complete output is preserved in `20261109T161556-unitgreen-r16.request.jsonl` and `.response.jsonl`. Merge remains a separate operator decision.
