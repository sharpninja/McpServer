# P1 Behavior Red R2 Test Plan

## Status and authority

- Phase: corrective tests and XMLDoc attribution for the rejected P1 behavior Red. Fresh Red evidence comes after this plan. This document does not accept the gate.
- Base: `develop` at `c195837a` (Merge pull request #68). Branch: `cursor/sessionlife-p1-behavior-r2-6fe1`.
- Restored working set: `docs/receipts/sessionlife-completion/20260928-p1-behavior-red/history-r1/` (`history-manifest.json` SHA-256 `B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4`). Those archive bytes stay unchanged.
- Predecessor verdict: DISAGREE. Accuracy 97. Completeness 92. Six FAIL findings P1BR-F01 through P1BR-F06.
- Rejection receipt: `docs/receipts/sessionlife-completion/20260928-p1-behavior-red/p1-behavior-red-r1-rejection.json`.
- Review response: `docs/receipts/hv/20260928T081146Z-sessionlife-p1-behavior-red-r1.response.jsonl`.
- Governing records remain `FR-MCP-107`, `TR-MCP-PLAN-001`, and `TEST-MCP-143`. All 35 batch TODOs stay open. No acceptance-criterion closure.

## Boundary

Corrective work is tests plus production XMLDoc attribution.

- `SessionLifeUnitGateValidator.Validate` still throws `NotImplementedException`.
- `Build.ValidateSessionLifeUnitGate` still has an empty body and does not delegate.
- The request type still exposes exactly five properties: `Scope`, `RunId`, `ResultsRoot`, `RunStartedAtUtc`, and `SourceManifestPath`.
- `ISessionLifeUnitGateValidator` still exposes the single operation `Validate`.
- The consumer test still uses NSubstitute against that internal interface. `DynamicProxyGenAssembly2` is the only `_build.csproj` addition, merged beside the existing `Build.Tests` friend assembly.
- No new NuGet package. No new public exception type. Classified rejection uses `System.IO.InvalidDataException`.
- Nuke inventory producers and `Invoke-SessionLifeUnitGate.ps1` orchestration stay out of this checkpoint.
- This plan does not authorize validator Green or delegation Green.

The R4 contract reflection tests are not in the frozen r1 archive and are not on `develop`. This checkpoint preserves the R4 shape in the scaffold and the consumer test. It does not recreate those reflection tests from memory.

## Diagnostic contract (P1BR-F01)

Every negative case applies its mutation before `Validate`. A setup exception fails the test as setup and cannot satisfy the oracle.

`Validate` must throw `InvalidDataException`. The message must contain both exact ordinal substrings:

- `reason=<stable-reason>`
- `affected=<affected-token>`

Affected tokens use forward slashes. Project-scoped reports use `{project}|{results-relative-report}`. Command defects use `{lane}:{command}`. Source drift uses the repository-relative path. Tokens do not embed `.mcpServer` or an absolute path.

Positive controls still require no exception: valid unit without a provider directory, valid provider without unit/Pester/explicit Build.Tests artifacts, and a post-manifest report-only write. Against the current scaffold those controls stay Red.

## Command exits (P1BR-F02)

`Validate_RequiredCommandDefect_IsRejected` parameterizes lane, command, and defect.

- Unit commands, in order: `pester`, `nuke-test`, `build-tests`.
- Provider command: `nuke-provider`.
- Defects for each command: missing entry, nonzero `exitCode` of 1, missing `exitCode`, and `exitCode` value `not-an-integer`.
- Every unaffected command keeps `exitCode` 0. When a later command exists, the fixture asserts that later command is still present and successful before `Validate`. That keeps earlier-failure masking visible. It does not implement orchestration.

Stable reasons: `missing-required-command`, `nonzero-command-exit`, `missing-command-exit-code`, `malformed-command-exit-code`.

## Freshness and run binding (P1BR-F03)

Shared mutations, with companion artifacts left on `sessionlife-red-001`:

- `WrongRunSourceManifest` changes only `source-manifest.json` `runId` to `different-run`. Reason `wrong-run-source-manifest`, affected `source-manifest.json`.
- `WrongRunCommandResults` changes only `command-results.json` `runId`. Reason `wrong-run-command-results`, affected `command-results.json`.
- Existing `WrongRunId` still changes only `selected-projects.json`.

Unit-only freshness, each with a fresh companion:

- `StaleBuildTestsReport` rewrites `build-tests/Build.Tests.trx` times and last-write to before run start. Reason `stale-build-tests-report`, affected `Build.Tests|build-tests/Build.Tests.trx`.
- `StalePesterReport` moves NUnit `results.xml` `date`/`time` and last-write five minutes before run start. Native `RunId` and `ExecutedAt` stay on the run. Reason `stale-pester-report`, affected `pester/results.xml`.
- `StalePesterNativeResult` moves native `ExecutedAt` and last-write five minutes before run start. The XML `date`/`time` stay on the run. Reason `stale-pester-native-result`, affected `pester/native-run.json`.

Pester freshness uses fields Pester already emits or that this gate already treats as provenance. XML uses the NUnit 2 result attributes `date` (`yyyy-MM-dd`) and `time` (`HH:mm:ss`) as UTC. Native JSON uses `RunId` and `ExecutedAt` (round-trip ISO-8601). Counters stay `Total`, `Passed`, `Failed`, `Skipped`, `Inconclusive`, `NotRun`, `FailedBlocks`, and `FailedContainers`. This plan adds no XSD requirement and does not promote the bundled Pester NUnit 2.5 schema.

## Present project/report identity (P1BR-F04)

`ProjectReportMismatch` moves the first selected project's well-formed TRX to `{lane}/{project}/Wrong.Tests.trx` and updates that inventory `reportPath` to the same relative path. The moved file exists. The canonical `{project}.trx` path is the moved file, so the case does not add a second report. Inventory count and lane TRX count stay equal to the selected project count. Missing and duplicate cases remain separate mutations.

Reason `project-report-identity`. Affected token `{project}|{lane}/{project}/Wrong.Tests.trx`.

## Nested `.mcpServer` ancestor (P1BR-F05)

`CreateUnderMcpServerAncestor` places the repository at `<temp>/McpServer-SessionLifeUnitGate/<guid>/.mcpServer/worktrees/candidate`. Tool executables stay in the sibling `<guid>/tools` directory. Disposal deletes only the GUID-owned parent.

On that root the plan requires:

- valid unit acceptance;
- report-only write under `TestResults` accepted;
- edited, added, and deleted included source rejected with repository-relative affected paths.

The manifest exclusions remain `TestResults/**` and `docs/receipts/sessionlife-completion/**/test-results/**`. Tests assert those exclusions are relative and do not name `.mcpServer`.

## Production attribution (P1BR-F06)

`build/SessionLifeUnitGate.cs` XMLDocs cite `FR-MCP-107` and `TR-MCP-PLAN-001` on the scope enum, request type, validator interface, validator type, and `Build.ValidateSessionLifeUnitGate`. Behavior of the scaffold is unchanged. The frozen r1 copy of this file is not rewritten.

## Focused inventory

- 5 positive controls, including 2 nested-root controls.
- 28 shared mutations on Unit and 28 on Provider.
- 13 unit-only Pester and Build.Tests mutations.
- 16 parameterized command defects (3 unit commands and 1 provider command, 4 defects each).
- 3 nested included-source rejections.
- 1 consumer delegation test.

Focused count: 94. Discovery must succeed with zero skips. Against the scaffold, positive controls fail because `Validate` throws `NotImplementedException`, negative controls fail because that exception is not `InvalidDataException` with the named reason and affected token, and the consumer test fails because the Build method does not delegate. Compile, restore, or discovery failure is not an accepted Red.

## Focused command

```powershell
dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter "FullyQualifiedName~SessionLifeUnitGate" --logger "trx;LogFileName=p1-behavior-red-r2.trx" --results-directory "docs/receipts/sessionlife-completion/20260929-p1-behavior-red-r2/test-results"
```

Stop after this Red. Delegation, report parsing, inventory production, and orchestration wait for a later accepted checkpoint.
