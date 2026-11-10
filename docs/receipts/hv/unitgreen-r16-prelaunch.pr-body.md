# PR87: make the unit suite green and bind ownership to runtime discovery

This branch is at final code/test head `167afdcb1bbe4ce8e54770fb5882cc8a48d82b01`. PR87 publication is Linux-only by operator direction; prior Windows evidence remains historical regression context.

## Current Linux gates

- Test: 4,847 passed, 0 failed, 0 skipped.
- MigrationIntegrationTests: 748 passed, 0 failed, 0 skipped.
- Build.Tests: 535 passed, 0 failed, 0 skipped.
- ValidateTraceability: passed.
- Immutable evidence: `docs/receipts/hv/unitgreen-r16-trx-167afdcb/manifest.json` with 14 hash-verified TRX and 10 source pins; SHA-256 `72B8BF052F25B9418BCA953E7F568891A83DDFA13703D2F4644C34E90D8D40FB`.

## Round 15 remediation

- Test and MigrationIntegrationTests now derive ownership from runtime-discovered case identities instead of recorded counts, with drift and foreign-category negative controls.
- The test-owned embedding fixture retries only bounded transient IOException cleanup and proves persistent IOException plus non-IOException propagation.
- The current review map has 75 fixed positions; tasks 72 and 74 now reflect Linux-only publication while retaining the earlier two-host baseline as history.
- Failed infrastructure attempts, ignored targeted raw TRX, the deliberate mutation run, and duplicate-run lock contention are disclosed in plan section 27.49 and the targeted receipt.

## Scope

The 68 classification-only tests and 1,915 uncited methods remain outside PR87 under PLAN-TESTCITATIONS-001. GitHub review comments are superseded by the full Claude Code Opus 5.5 high-effort hostile-validation workflow. The reviewer does not build or run tests. Merge remains a separate operator authorization.

Round 16 records commit `ebdcd34285dfc297a68bdb0c8e024e4684bb028e` was pushed and read back exactly. PR87 is ready for the independent full HV launch; a DISAGREE returns it to draft.
