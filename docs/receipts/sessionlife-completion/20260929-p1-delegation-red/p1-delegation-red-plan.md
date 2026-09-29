# P1 Delegation Red

## Status and authority

- Phase: minimal same-request exactly-once delegation from `Build.ValidateSessionLifeUnitGate` to `ISessionLifeUnitGateValidator.Validate`.
- Base: `cursor/sessionlife-p1-behavior-r2-6fe1` at `3b8945b5` (code tip `f4d8e656`, HV receipt `3b8945b5`).
- Branch: `cursor/sessionlife-p1-delegation-red-edc4`.
- Predecessor: P1 behavior Red R2 AGREE. Accuracy 99. Completeness 99. `nextBoundary` authorizes this delegation and then validator behavior for the accepted fixtures. It does not authorize Green inside the R2 review commit.
- Governing records remain `FR-MCP-107`, `TR-MCP-PLAN-001`, and `TEST-MCP-143`. All 35 batch TODOs stay open. No acceptance-criterion closure.
- `docs/receipts/sessionlife-completion/20260928-p1-behavior-red/history-r1/` stays byte-for-byte unchanged.
- `AGENTS-README-FIRST.yaml` is absent in this checkout, so this checkpoint does not call the MCP session log.

## Authorized change

`Build.ValidateSessionLifeUnitGate` forwards the injected validator and the same request reference in one call:

- the dependency is the `ISessionLifeUnitGateValidator` argument;
- the operation is `Validate`;
- the argument is the request instance the caller passed;
- the call happens once.

`SessionLifeUnitGateValidator.Validate` still throws `NotImplementedException`. This checkpoint does not classify rejections.

## Contract preserved

- `SessionLifeUnitGateValidationRequest` still exposes `Scope`, `RunId`, `ResultsRoot`, `RunStartedAtUtc`, and `SourceManifestPath`.
- `ISessionLifeUnitGateValidator` still exposes only `Validate`.
- Consumer tests still use NSubstitute. No NuGet package is added. No public exception type is added.

## Tests that must fail when delegation is wrong

`SessionLifeUnitGateConsumerTests` keeps the R2 oracle and adds a second mock-path fact.

- No call: `Received(1)` fails.
- Multiple calls: `Received(1)` and `Assert.Single` on that validator's received calls fail.
- Different request instance: `ReferenceEquals` against the caller instance fails, including a value-equal copy held by the test.
- Wrong dependency: a second validator that was not passed in must have an empty received-call list, and the injected validator must be the one that received the request.

Behavior oracles stay Red. Positive controls still see `NotImplementedException`. Negative controls still require `InvalidDataException` with the case `reason=` and `affected=` tokens.

## Out of scope

- Report-validator Green and the `InvalidDataException` matrix.
- Nuke inventory producers and `Invoke-SessionLifeUnitGate.ps1` orchestration.
- Closing any of the 35 batch TODOs or their acceptance criteria.
- Rewriting `history-r1`.
- P1 exit, provider Green, and P2 through P7.

## Focused command

```powershell
dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter "FullyQualifiedName~SessionLifeUnitGate" --logger "trx;LogFileName=p1-delegation-red.trx" --results-directory "docs/receipts/sessionlife-completion/20260929-p1-delegation-red/test-results"
```

Expected split after this change: the consumer facts pass on the mock path, and the physical behavior facts stay failed against the `NotImplementedException` scaffold. Discovery must succeed with zero skips. Compile or restore failure is not an accepted result.
