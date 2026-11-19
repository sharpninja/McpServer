# Task007 candidate design and mock evidence

This packet is limited to current-definition design/mock readiness for TASK-FEDPH0-007 in PLAN-FEDDOLT-001/GRP-FEDPH0-001. It changes no Task state, requirement or AC. Parent integration/reconciliation precedes any Ready transition. ImplementationBarrier remains Closed; no native provider, service, database, measured-performance, certificate/credential operation or HV ran here.

Source base:0bc4009437dc84b1d99f36a1336f3163c7b24fab. Branch:codex/fedph0-task007-20261119. Runtime identity verified from this child's Codex transcript turn_context:model gpt-6.1-sol, effort xhigh. Child session Codex-20261119T153852Z-plugin-session, request req-20261119T154146Z-prompt-9158; parent Codex-20261116T174245Z-plugin-session, request req-20261119T152235Z-prompt-100c.

## Scope and outcomes

- B10: frozen workload/budgets; independent raw/report equality; all operation/repetition samples, failed attempts and signed growth retained; nearest-rank quantiles; truthful over-budget findings; exercised bounds, no unsupported SLA.
- B11: retained subscription-state/policy and review-operations contracts cover complete schema/epoch/generations, read-only polls, late acknowledgments, reconciled missed notifications, old-worker refusal,30s20percent jitter,1..60s backoff, immediate wake,24h stale display and15min/24h review-age/repeat policy.
- B12: retained physical candidate/review pins, full conflict artifacts and unresolved cleanup refusal through GC/reopen; older backup recovery and new ordered trace prove epoch-before-listeners, compatible components, old-report409, retained ancestry and audit resend; frozen public owner/issuer/root/SPKI/SAN/expiry/rotation/listeners; existing wrong-pin/SAN-before-bearer and13credential channels; preview/disclosure response refuses history and unsafe diagnostics without authorizing rewrite/reclone/force-push.
- The canonical three selected pairs remain FR-MCP-179/ac-HubRestoreFromOlderBackup, FR-MCP-184/ac-ReceiptsAndLogsContainNoCredentials and FR-MCP-185/ac-UnknownCertificatePinRejected. Supporting whole-definition outputs and existing tests remain explicit; their inclusion adds no new requirement.

## Commands and receipts

Executed on PAYTON-LEGION2 through this child's PowerShell.MCP console, in the isolated Windows worktree:

```powershell
./build.ps1 TestGroups --test-groups 'QBrainAi.Support.Mcp.Tests:Phase0OperationsReadinessMockGateTests' --test-run-id '20261119T095955-fedph0-task007-operations-mock'
./build.ps1 TestGroups --test-groups 'QBrainAi.Support.Mcp.Tests:Phase0RecoverySecurityMockGateTests,Phase0SubscriptionStateMockGateTests,Phase0SubscriptionPolicyMockGateTests,Phase0ReviewOperationsMockGateTests,Phase0CandidateRetentionMockGateTests,Phase0OperationsReadinessMockGateTests' --test-run-id '20261119T100134-fedph0-task007-definition-mocks'
```

- windows-operations-mock.trx:488passed,0failed,0skipped;14correct-response cases and474expected-negative cases. Each failure is an assertion rejection inside a passing control. Both host labels are independent test inputs. Restore positive tests each also exercise a generation gap; missing-evidence controls each check null/omitted/extra traces.
- windows-definition-mocks.trx:2726passed,0failed,0skipped. CandidateRetention1312; SubscriptionState321; RecoverySecurity287; ReviewOperations188; SubscriptionPolicy130; OperationsReadiness488. Class counts come from TRX TestDefinitions.
- Operations TRX SHA256:A2839BC1CE34FCD6E304EBFDD549A9B52253CF3120E0CF74127BD2FF593B4A8B.
- Definition TRX SHA256:DFE1A204DCDC77CD477394BE02D43D2C750D2497B60E239B6FC99D68FB163F98.
- Each original TestResults TRX was retained and its copy hash compared equal. code-verify-build.txt records the mandatory final source verification:Build succeeded,0warnings,0errors. An initial authored-test C#14 compile failure used reserved accessor identifier field; renaming it facet repaired the source before mock execution. No failing test result was hidden or accepted.
- task007-definition-and-requirements.json retains the live definition excluding only its derived status/eligibility line, and complete effective canonical FR/TR/TEST texts used. source-pins.json identifies plan, architecture, companion, sanitizer and all reused/new assertion/fixture SHA256 values.

## Limits and recommendation

This is Windows unit mock proof with LAB and LEGION labels. Linux execution of the new class remains unrun in this child packet. Parent integration can run the same six-class selector on the exact integrated head in its paired-host lane. No actual two-host execution, B10 measurements, native timing/persistence, GC/restore/TLS/credential feasibility, production binding, B-output acceptance, final-head phase gates or HV acceptance is implied by these mocks. Those implementation and exit obligations remain after the all-nine barrier; they are not invented native-proof prerequisites for design readiness. The candidate design/assertion packet is ready for parent review and integration, subject to the explicitly unrun paired-host mock check.

Final whitespace-only source correction: mandatory code-verify succeeded again (code-verify-final-build.txt,0warnings0errors). Targeted operations gate re-executed488/488,0failed0skipped with ./build.ps1 TestGroups --test-groups 'QBrainAi.Support.Mcp.Tests:Phase0OperationsReadinessMockGateTests' --test-run-id '20261119T100738-fedph0-task007-final-mock'. Final TRX windows-operations-final-mock.trx SHA256:7004D741D0A4F0172F31C0D693EAFAD3DF53AF2E6A5E6EAA8C88B510FB974F74. Original retained and copy verified equal. Source pins reflect the corrected files; earlier passing observations remain retained.
