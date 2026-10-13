# Use case diagram canonical workspace correction receipt

Timestamp: 2026-10-11T11:52:07-05:00

## Scope

- Canonical workspace resolution for usecase_diagram.
- Canonical workspace resolution for diagram graph get and put.
- Canonical workspace resolution for use case approval.
- Scoped UML use-case output from the MCP usecase_diagram tool.
- Aggregate-derived UML graph when no saved canvas graph exists.

## Mocks-first gate

Initial negative-control execution found the test expected the exact TrueException type while xUnit emitted ContainsException. Production code was not changed before correcting that test harness defect.

Corrected mocks-first execution:

dotnet test tests/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.csproj --no-restore --filter FullyQualifiedName~UseCaseDiagramUmlContractTests.MockGate --property:DisableGitVersionTask=true --nologo

Result: Passed 2, Failed 0, Skipped 0.

## Focused production gate

Filter:

FullyQualifiedName~UseCaseDiagramUmlContractTests|FullyQualifiedName~UseCaseCqrsTests.GetUseCaseDiagram|FullyQualifiedName~UseCaseDiagramGraphCqrsTests

Result: Passed 10, Failed 0, Skipped 0.

## Complete UseCase-focused suite

dotnet test tests/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.csproj --no-restore --filter FullyQualifiedName~UseCase --property:DisableGitVersionTask=true --logger trx;LogFileName=usecase-diagram-canonical-green.trx --results-directory artifacts/test-results --nologo

Result: Passed 82, Failed 0, Skipped 0.

TRX SHA-256: EC631E7A2BBF982F13DA137141F74A4869C6674284982544C096995F36CE2AAA
