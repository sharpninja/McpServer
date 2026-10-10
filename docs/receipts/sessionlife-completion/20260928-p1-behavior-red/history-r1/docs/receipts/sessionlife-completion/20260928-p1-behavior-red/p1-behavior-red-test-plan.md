# P1 Typed Consumer and Gate Behavior Red Test Plan

## Status and authority

- Phase: P1 typed-consumer and physical gate behavior Red only.
- Candidate: `F:\GitHub\McpServer\.mcpServer\worktrees\sessionlife-acceptance-20260928`.
- Branch: `codex/sessionlife-acceptance-20260928`.
- Candidate base and current HEAD: `6a565d8762072ed040469047791a57c80dbf1e88`.
- Governing records: `FR-MCP-107`, `TR-MCP-PLAN-001`, and `TEST-MCP-143`.
- Approved plan: `docs/plans/sessionlife-batch-completion-20260927.md`, especially the machine-readable gate at lines 223-229 and ordered P1 clarification at line 390.
- Accepted predecessor in the primary-checkout evidence root: `docs/receipts/sessionlife-completion/20260928-p1-contract-red-r4/p1-contract-red-r4-acceptance.json`, SHA-256 `E3D20EB91B6DF4F1041EBD75F2B63DE3AB311E85D2D3988C908C359DE509084A`.
- Accepted review response in the primary-checkout evidence root: `docs/receipts/hv/20260928T065454Z-sessionlife-p1-contract-red-r4.response.jsonl`, SHA-256 `FF332A64054BBDB9C5D69C9C5BDF4746882D5A2AD3137E7C9591FA62517C688A`.
- Frozen R4 contract-test source SHA-256: `0F0D14853AAB587502289C80B8F6BF6CDDB4DFCAC4704DCBBF7E627F26772B08`.
- Frozen R4 contract plan SHA-256: `6675786A59D0C0428757752D582EFB33C830BBA87D02337F9F322FF3F5D15D2D`.
- P1 remains open. This Red does not authorize report validation, delegation, orchestration, Nuke report plumbing, or any done-state change.

## Bounded production scaffold

Add only the declarations needed for compiled behavior tests:

1. Internal `SessionLifeUnitGateScope` with distinct `Unit` and `Provider` values.
2. Internal `SessionLifeUnitGateValidationRequest` with exactly the five reviewed public instance properties: `Scope`, `RunId`, `ResultsRoot`, `RunStartedAtUtc`, and `SourceManifestPath`.
3. Internal `ISessionLifeUnitGateValidator` with only `void Validate(SessionLifeUnitGateValidationRequest)`.
4. Internal static `Build.ValidateSessionLifeUnitGate` with the reviewed signature and an intentionally empty body.
5. Internal `SessionLifeUnitGateValidator` shell accepting the repository root and an internal tool-version probe delegate, with `Validate` throwing `NotImplementedException`.
6. `DynamicProxyGenAssembly2` receives internal visibility solely so the existing NSubstitute dependency can proxy the internal validator interface. No production type becomes public and no package is added.

The frozen R4 plan, contract tests, and evidence are not modified.

## Typed consumer Red

`SessionLifeUnitGateConsumerTests` uses NSubstitute to create `ISessionLifeUnitGateValidator`. It passes one concrete request to `Build.ValidateSessionLifeUnitGate` and requires:

- exactly one validator call in total;
- the called member is `Validate`; and
- the exact same request instance is passed.

The empty Build stub makes this a real consumer Red. No report or filesystem behavior is asserted in this test.

## Ownership contract

- The concrete validator receives the canonical repository root from Nuke's `RootDirectory`; repository root is not a sixth request property.
- `ResultsRoot` is the canonical absolute run directory `<repositoryRoot>\TestResults\<RunId>`.
- `SourceManifestPath` is the pre-run source/tool manifest beneath `ResultsRoot`.
- `Scope` selects the unit or provider lane. It never supplies a project list.
- The unit target materializes its existing filtered project sequence once, writes that same sequence to `unit\selected-projects.json`, and executes it. The provider target does the same with its existing two-project sequence at `provider\selected-projects.json`.
- The validator consumes the Nuke-produced inventory. Tests and orchestration do not maintain a competing production project list.
- Unit validation requires no provider directory. Provider validation requires no unit, Pester, or Build.Tests directory.

## Physical artifact fixtures

`SessionLifeUnitGateValidatorTests` creates one GUID-owned temporary parent with sibling `repository` and `tools` directories, then serializes fixture artifacts through `System.Text.Json` and `System.Xml.Linq`. Tool executables are outside the synthetic repository so tool-byte drift cannot become source drift or require a production `.tools` exclusion. Cleanup deletes only that owned parent. Every assertion invokes the production `SessionLifeUnitGateValidator`; fixtures do not contain a parallel validator.

Canonical artifacts:

- `<ResultsRoot>\unit\selected-projects.json` or `<ResultsRoot>\provider\selected-projects.json` contains schema version, run ID, scope, generated timestamp, and the projects actually selected by Nuke.
- Lane TRX files use `<ResultsRoot>\<scope>\<project>\<project>.trx`.
- `<ResultsRoot>\pester\results.xml` is the NUnit-compatible report and `<ResultsRoot>\pester\native-run.json` carries `Total`, `Passed`, `Failed`, `Skipped`, `Inconclusive`, `NotRun`, `FailedBlocks`, and `FailedContainers` from Pester's native result.
- `<ResultsRoot>\build-tests\Build.Tests.trx` is the fixed explicit Build.Tests artifact, outside Nuke's selected-project inventory.
- `<ResultsRoot>\command-results.json` records the required command exits and timestamps.
- The source manifest records exact repository-relative source paths, byte lengths, SHA-256 hashes, and fixed tool path, version, and executable-byte SHA-256 observations captured before the run.

The future validator must independently enumerate and hash current source bytes and re-probe fixed tools. It may not trust caller-declared after-hashes. Report writes under excluded result paths do not change source identity; edits, additions, or deletions of included source do.

## Producer-real TRX contract

TRX fixtures use the observed Visual Studio Test 2010 namespace. Required counters are only the plan-backed `total`, `executed`, `passed`, `failed`, and `notExecuted`. They must be present, parseable, nonnegative, and internally consistent. The run requires `total > 0`, complete execution, zero failures, zero not-executed tests, valid start/finish timestamps, and `Completed` outcome.

Optional adverse counters are validated only when present and must be zero. This includes the nonstandard `skipped` counter. No TRX XSD exists in the observed environment, so these tests make no XSD-required claim and do not introduce a second TRX parser.

Pester failed blocks and containers come from the native result artifact; XML alone cannot prove them. The bundled Pester NUnit 2.5 XSD is not promoted into a new P1 acceptance requirement.

## Negative fixture matrix

The real-artifact theory covers each approved rejection category with a single mutation from a valid baseline:

- missing inventory, missing report, duplicate inventory project, duplicate report file, wrong run ID, scope mismatch, stale inventory, stale report, and project/report disagreement;
- missing, malformed, negative, or inconsistent required TRX counters; zero discovery; failed or not-executed tests; optional nonzero skipped/adverse counters; invalid times; and incomplete outcome;
- missing Pester native counters, failed/skipped/inconclusive/not-run tests, failed blocks, and failed containers;
- missing or nonpassing Build.Tests report;
- missing or nonzero required command exit;
- edited, added, or deleted included source; tool path, version, or executable-byte-hash drift; and the control case that report-only writes do not drift source identity;
- unit success with no provider directory and provider success with no unit/Pester/Build.Tests artifacts.

The 28 shared inventory, TRX, command, source, and tool mutations run once against Unit and once against Provider. The 10 Pester and explicit Build.Tests mutations remain Unit-only because those artifacts are not provider prerequisites. The focused inventory is 73 tests: 3 frozen contract tests expected to pass, plus 1 consumer Red and 69 physical behavior Reds expected to fail with zero skips.

The validator shell intentionally throws `NotImplementedException`. Positive cases require no exception. Rejection cases require a non-`NotImplementedException` failure from production validation, so the shell cannot satisfy them accidentally.

## Intended focused Red command

Run only after all planned files are listed and frozen by the parent:

```powershell
dotnet test tests\Build.Tests\Build.Tests.csproj -c Debug --filter "FullyQualifiedName~SessionLifeUnitGate" --logger "trx;LogFileName=p1-behavior-red-r1.trx" --results-directory "docs\receipts\sessionlife-completion\20260928-p1-behavior-red\test-results"
```

Accepted Red requires successful restore, build, and discovery; the three frozen contract tests passing; the NSubstitute consumer test failing on absent delegation; physical validator tests failing on the `NotImplementedException` shell; and zero skips. Build, analyzer, restore, or discovery failure is not an accepted Red.

The parent owns native-object pre-test, result, delta, and freeze receipts. Stop for independent Red review before implementing delegation, parsing, report validation, inventory writing, orchestration, or Nuke parameters.
