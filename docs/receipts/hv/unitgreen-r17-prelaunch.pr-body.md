# PR87: publish the Linux unit suite with runtime ownership discovery

PR87's tested Linux evidence head is `bbd248b94168bb0f50ba86a14c4b9021ffbabf1c`; remediation code is commit `287bdcb34e035049d8e15e7f9ce9d0167cbe417d`. Publication is Linux-only by operator direction. Prior Windows evidence remains historical regression context, not a current publication gate.

## Current Linux gates

- Test: 4,847 passed, 0 failed, 0 skipped across seven projects.
- MigrationIntegrationTests: 748 passed, 0 failed, 0 skipped across six projects.
- Build.Tests raw infrastructure runner: 540 passed, 0 failed, 0 skipped.
- ValidateTraceability: passed; no TRX by contract.
- Immutable evidence: `docs/receipts/hv/unitgreen-r17-trx-bbd248b9/manifest.json`, 14 hash-verified TRX, 11 source pins and per-project counts; SHA-256 `3EDBAD5D60F5FE07D6E232EE8B2C01A3EA527D5E01B8D2834DE3E1175D386968`.

## Round 16 remediation

- Ownership decision tests now use explicitly synthetic inventories instead of recorded current-layout totals, with synthetic quarantine cases carrying the real 26-and-3 counts required by task 73.
- Five fail-closed controls pin empty discovery, duplicate identities, filtered identities absent from the all-case set, missing list headers and the unindented parser boundary.
- The targeted gate passed 24/0/0; the final 540-case Build.Tests run includes those controls.
- The first Round 17 lane attempt used an invalid in-checkout TMPDIR and was stopped after two artifact-boundary controls correctly failed. No code changed. The corrected external-TMPDIR lane passed 748/0/0.

## Scope and retained limits

The 68 classification-only tests and 1,915 uncited methods remain outside PR87 under PLAN-TESTCITATIONS-001. The scanner remains under the operator's no-more-scanner-work override and does not resolve alias chains or `using static` through an alias. The startup-health wait remains outside D-B and unsatisfied: probe-count timing is not a fixed ten-minute bound and its constants/call are not fully test-bound. GitHub review comments are superseded by the full Claude Code Opus 5.5 high-effort hostile-validation workflow. The reviewer does not build or run tests. Merge remains a separate operator authorization.

## Independent hostile validation

Claude Code 2.1.318, `claude-opus-5-5`, high effort completed the full Round 17 review with **AGREE, accuracy 98, completeness 98 and confidence 92**. It reported 31 PASS, 0 FAIL, 0 UNKNOWN, 3 OVERRIDDEN and 4 OUT OF SCOPE, with no regression, and independently recomputed every supplied TRX, source-pin, receipt and manifest hash without building or running tests. The paired request/response JSONL and compact verdict are committed under `docs/receipts/hv/`.

The live TODO readback after verdict processing is 71 done of 75. Tasks 12 (full-HV record), 13 (merge), 35 (Windows child branch outside the Linux-only scope) and 75 (verdict processing) remain open; the TODO remains not done. Exactly 54 mapped criteria across 23 live requirement records were set satisfied after an atomic dry-run/write/readback sequence. Merge remains a separate operator authorization and has not been performed.
