# Case-identity reconciliation supplement

Recorded 2026-11-09T09:53:00-06:00 by Codex for IMPL-UNITGREEN-001.

This supplement explains the single old display identity reported gone once on each host by reconciliation-r15-03eea747-vs-da11faa2.txt. It does not alter that receipt. The original receipt remains RESULT FAIL gone=2, retains SHA-256 A847021888360DBC7470D5510A0A7B0FC6B3B1886F1327CA160D9B167E8C9CC1, and is preserved beside this supplement byte-for-byte.

## Decision

Accept the old IntegrationLaneProjectsMockGateTests.WrongSelector_IsRejected(fault: DropsBuildTests) row as the predecessor of the current DropsNukeBuildIntegrationTests row for task mapping. This is not a name-only equivalence:

- Commit 6e8371e0a8c7300e7f7ad39e1b820d868863643c removed infrastructure-owned Build.Tests from the integration lane, introduced integration-owned Build.IntegrationTests, changed the omission fault from DropsBuildTests to DropsBuildIntegrationTests, and added KeepsBuildTests to reject accidental retention of the infrastructure project.
- Commit 99b9c4e748cc3cc6f734ed5c5a2cc38ef38bc7ad renamed Build.IntegrationTests to NukeBuild.IntegrationTests to match its root namespace, changed the omission fault to DropsNukeBuildIntegrationTests, and added KeepsPreRenameNames to reject the superseded project names.
- The current source retains both transitions in dated XML documentation in tests/Build.Tests/IntegrationLaneProjectsMockGateTests.cs.

The semantic contract therefore evolved from "reject omission of the lane's Build.Tests project" to "reject omission of the lane's NukeBuild.IntegrationTests project" while separately rejecting retention of infrastructure-owned Build.Tests. Count equality alone is not used as proof.

## Current execution evidence

The successor row occurs exactly once and passes in each retained infrastructure TRX:

- LAB-OMARCHY: docs/receipts/hv/unitgreen-r15-trx-03eea747/ug-r14f4-final-linux-buildtests-2/ug-r14f4-final-linux-buildtests-2.trx, SHA-256 D3EC5046ED656EDD97BD0AF622B1BC01AF1BF99D8D6F8C0EBAF363B85124B58D.
- PAYTON-LEGION2: docs/receipts/hv/unitgreen-r15-trx-03eea747/ug-r14f4-final-win-buildtests-2/Build.Tests.trx, SHA-256 A3065916BA61F2C7445776FA1DB34C30BBE40588D5EDAE9DF0B1A214EC891984.

Each TRX reports outcome Passed for NukeBuild.Tests.IntegrationLaneProjectsMockGateTests.WrongSelector_IsRejected(fault: DropsNukeBuildIntegrationTests).

## Scope

This supplement closes the unexplained rename/ownership mapping required by the approved PR87 P0 plan. It does not change the original reconciliation verdict, satisfy TR-MCP-NUKE-019, accept the final gate set, or claim full hostile validation.
