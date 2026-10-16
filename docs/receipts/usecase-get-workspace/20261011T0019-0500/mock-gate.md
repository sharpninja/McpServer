# BDPv4 Mock Gate: Use-Case Get Workspace Identity

Timestamp: 2026-10-11T00:22:50-05:00
Requirements: FR-MCP-USECASE-001, TR-MCP-USECASE-002, TEST-MCP-USECASE-002
Production implementation state: not changed for this fix
Pre-production HEAD: 5010795fe67abe3b98d294fce15794b6d2bff8e5
Production source SHA-256: C5605DCC2F901B9CB4534277AEEB855A9CABA383DB35C67622006B07F8F76F2D

## Contract artifact

- Path: tests/QBrainAi.Support.Mcp.Tests/Services/UseCaseGetWorkspaceResolutionContractTests.cs
- SHA-256: 6CFFB377EDEB48407731D72B02AAE4C9D6AE3B6D8EA80544488DC8EC8552F9F6
- Correct mock: canonical workspace result for use case 125
- Negative control: raw-path lookup failure is rejected by the same success, identity, workspace, and title assertions
- Initial test-only run detected the too-broad expected xUnit exception type. The test was corrected to require the exact TrueException before this green gate.

## Exact command

dotnet test tests/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.csproj --no-restore --filter FullyQualifiedName~UseCaseGetWorkspaceResolutionContractTests --logger trx;LogFileName=usecase-get-workspace-mock-gate.trx --results-directory docs/receipts/usecase-get-workspace/20261011T0019-0500 --nologo

## Green result

- Failed: 0
- Passed: 2
- Skipped: 0
- Total: 2
- Duration: 44 ms
- TRX: docs/receipts/usecase-get-workspace/20261011T0019-0500/usecase-get-workspace-mock-gate.trx
- TRX SHA-256: 121ECB621FD70E72E651BC62DD61565E1E46B64743467AAE88FD68514C3AD421
