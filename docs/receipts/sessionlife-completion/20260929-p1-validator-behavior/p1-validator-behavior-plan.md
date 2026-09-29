# P1 Validator Behavior

## Status and authority

- Phase: validator behavior limited to the accepted P1 behavior-Red R2 fixture matrix.
- Base: `cursor/sessionlife-p1-delegation-red-edc4` at `d60379cba8413eb1307b175c5b728fde37fd18ef`.
- Branch: `cursor/sessionlife-p1-validator-behavior-2908`.
- Predecessor: P1 delegation Red AGREE. Accuracy 99. Completeness 99. `nextBoundary` authorizes validator behavior for the accepted fixtures and does not authorize producers, orchestration, P1 exit, or TODO closure.
- Governing records remain `FR-MCP-107`, `TR-MCP-PLAN-001`, and `TEST-MCP-143`. All 35 batch TODOs stay open. No acceptance-criterion closure.
- `docs/receipts/sessionlife-completion/20260928-p1-behavior-red/history-r1/` stays byte-for-byte unchanged.
- `AGENTS-README-FIRST.yaml` is absent in this checkout and the `mcpserver-box` plugin namespace failed discovery. This checkpoint records `MCP_UNTRUSTED` and does not call the MCP session log.

## Authorized change

`SessionLifeUnitGateValidator.Validate` classifies the accepted physical fixtures.

- Negatives throw `InvalidDataException` whose message contains the ordinal substrings `reason=<stable-reason>` and `affected=<affected-token>` from the R2 matrix.
- Positives return without throwing, including a valid unit lane, a valid provider lane, a post-manifest report under `TestResults`, and the same cases when the repository has a `.mcpServer` ancestor.
- Included-source drift stays repository-relative. Exclusions stay the manifest globs. The validator does not treat an ancestor named `.mcpServer` as part of the repository root.
- Tool observations stay `path|version|hash` and are compared through the injected probe.
- Rejection uses `System.IO.InvalidDataException`. No new public exception type is added. No NuGet package is added.

## Contract preserved

- `Build.ValidateSessionLifeUnitGate` still forwards the injected validator and the same request reference in one call.
- `SessionLifeUnitGateValidationRequest` still exposes `Scope`, `RunId`, `ResultsRoot`, `RunStartedAtUtc`, and `SourceManifestPath`.
- `ISessionLifeUnitGateValidator` still exposes only `Validate`.

## Out of scope

- Nuke inventory producers and `Invoke-SessionLifeUnitGate.ps1` orchestration.
- Closing any of the 35 batch TODOs or their acceptance criteria.
- Rewriting `history-r1`.
- P1 exit, provider Green beyond this fixture matrix, and P2 through P7.

## Focused command

```powershell
dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter "FullyQualifiedName~SessionLifeUnitGate" --logger "trx;LogFileName=p1-validator-behavior.trx" --results-directory "docs/receipts/sessionlife-completion/20260929-p1-validator-behavior/test-results"
```

Discovery must succeed with zero skips. The consumer delegation facts must stay Green. The behavior matrix is the oracle for this checkpoint.
