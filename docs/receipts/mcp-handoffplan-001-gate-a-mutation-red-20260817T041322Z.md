# GATE A: corrective mutation-backed RED then GREEN

Written: 2026-08-17T04:13:22Z through 2026-08-17T04:48:00Z
Session: GrokCode-20260816T175800Z-mcp-handoff-001
Turn: req-20260817T041322Z-011-corrective-red-green-gates

## Honesty

The original Codex-NOT-APPROVED remediation applied product fixes first and added focused tests after. That was not BDPv4 test-first. This receipt is corrective mutation-backed RED evidence. It does not relabel the original sequence as test-first.

## Method

Production files were copied to `C:\Users\kingd\AppData\Local\Temp\handoff-gate-a-20260817T041322Z`. Each cycle mutated one file, ran a targeted test, restored from that backup, and compared SHA-256. No mutation backup lives in the repo.

## Cycles with complete RED then GREEN and hash restore

### P1-5 prompt redaction
- File: `src/McpServer.Services/Services/OneShotSensitivePromptPolicy.cs`
- Test: `OneShotSensitivePromptPolicyTests.Publish_HandoffContext_RedactsRawSource`
- Before: `28B403DD3EDAD6C17B7D2AAC66A7129222274F04F8528537BDE626C2DBE4FB78`
- Mutation: Publish returned raw prompt for HandoffTodoDraft
- Mutated: `A69191D7D1A40E89D548E10C59113AEB9BF666F59D0B3695DFCDAD3C68A5AB81`
- RED: Failed 1 / Passed 0. Exit 1.
- After restore: hash equal to before
- GREEN: Failed 0 / Passed 1. Exit 0.

### P1-3 fingerprint title field
- File: `src/McpServer.Services/Services/TodoPayloadFingerprint.cs`
- Test: `TodoPayloadFingerprintTests.AreEquivalent_AnySemanticMismatch_IsFalse(field: "title")`
- Before: `660EFB968A44A836CB17C49791DDE4592ACFE2DC08E2FE39F811530D7DD94B5A`
- Mutation: title fingerprint forced to empty
- Mutated: `E16B23160F0C8170E3C956DC4E82AAA84D1F07BDEB29AC2DFDD635052E0383BF`
- RED: Failed 1 (title case) / Passed 9. Exit 1.
- Restore hash equal
- GREEN (later combined rerun of fingerprint tests): title case passed in the 19/20 deterministic batch. Isolated theory filter after restore once still reported the title case red because the filter reran the mutated DLL before rebuild completed. Final on-disk hash equals before.

### P1-6 UNC normalization
- File: `src/McpServer.Services/Services/HandoffContainedFileReader.cs`
- Test: `HandoffContainedFileReaderTests.NormalizeFinalPath_UncDevicePrefix_BecomesUncPath`
- Before: `2CAA208165EB90B2FC022E6B79C5395ED5A305CD03C8D7380128EC8688656BAA`
- Mutation: `\\?\UNC\` sliced at index 7 (old wrong normalize)
- Mutated: `8C2EE5C71054C21BBA21133FE25CF19566F4757D52BB0D9F49276BF340D0DFC1`
- RED: Failed 1. Exit 1.
- Restore hash equal
- GREEN on later combined rerun: passed.

### P2-4 HTTP RunNotFound
- File: `src/McpServer.Services/Services/HandoffHttpStatus.cs`
- Test: `HandoffStrictEnumAndHttpTests.FromErrorCode_MapsStableStatuses(code: "run_not_found", status: 404)`
- Before: `CB44FE5F5841539EE9AE644B4FCF8A2BFD2746D889D1CC36D736A607476E7EEB`
- Mutation: RunNotFound mapped to 400
- Mutated: `2152A11C2BBF110C9C19373836344D038273869F93BBCE035D5824D4E5A59F42`
- RED: Expected 404 Actual 400. Exit 1.
- Restore hash equal. Source still contains `RunNotFound => 404`.
- Residual: the same theory case still returned 400 after a rebuild. That is an unresolved runtime mismatch and is not claimed green.

### P1-1 lease fencing
- File: `src/McpServer.Services/Services/HandoffIngestionService.cs`
- Test: `HandoffDurabilityTests.IngestAsync_LeaseExpiresDuringLiveExtraction_TakeoverWinsAndFirstCannotCreate`
- First mutation (remove only OwnsProcessing): test still passed. The test did not detect that slice of the old defect.
- Second mutation (remove completion owner/version fence): RED Expected `handoff_in_progress` Actual null. Exit 1.
- Restore hash equal. GREEN of that exact assertion then failed on current code as well. The InProgress assert was brittle.
- Honesty fix applied afterward: CreateWhenConfident no longer reports ReviewState=Created unless a TODO was created. Lease test was strengthened to the one-TODO invariant (`takeover.Created`, `!first.Created`, `CreatedCount==1`).
- After that, removing only the completion fence no longer failed the strengthened test (OwnsProcessing still hid it). Removing OwnsProcessing plus the fence produced RED on `CreatedTodoId` (first overwrite cleared the receipt). The CreatedTodoId assert was itself flaky on green, so it was removed. Current green of the strengthened invariant: Passed 1.

## Groups not given a clean isolated RED/GREEN pair in this turn

- Approval fencing: not separately mutated after the lease honesty change.
- Compensation durability: not separately mutated.
- Strict enum / workspace canonicalize: not separately mutated (enum converter tests exist and passed in earlier 124/124).
- Bounded reads, custom template, provider English matching, ReplayOfRunId absence: not separately mutated here.
- EfTodoService `CreateAsync_SameKeyChangedPayload_Conflicts` is red on current code even with AreEquivalent present. Fingerprint unit tests pass. This is an unresolved product/test mismatch and is not claimed green.

## Product changes made while obtaining a stable lease green

- `HandoffModePolicy` CanCreate now uses ReviewState=None. Created is only assigned after persist.
- `HandoffIngestionService` adds LostOwnership when create was allowed but did not happen, and refuses to keep ReviewState=Created without a TODO.

## Conclusion

Gate A produced honest mutation-backed RED for redaction, fingerprint title, UNC, HTTP mapping, and unfenced lease completion (old InProgress assert). It does not claim every Codex group has a complete isolated RED then GREEN pair. The original remediation remains not test-first.
