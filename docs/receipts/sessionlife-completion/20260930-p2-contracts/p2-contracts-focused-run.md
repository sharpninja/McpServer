# P2 Contracts Focused Run

Recorded at `2026-09-30T00:32:26Z` on `cursor/sessionlife-p2-contracts-5cb2` from `f80e9af6f57029af57b6a3db388002cbdafedb4e`.

Tools: PowerShell `7.6.6`, Pester `6.2.0`, .NET SDK `10.0.401`. `AGENTS-README-FIRST.yaml` is absent (`MCP_UNTRUSTED`). `plugins/core/.staged-plugin` is absent. `tools/validation/Invoke-SessionLifeUnitGate.ps1` is absent.

## SessionLog and runtime Pester

Command, from `/workspace`:

```powershell
Import-Module Pester -MinimumVersion 5.0
Invoke-Pester -Path @(
  'plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1',
  'plugins/core/test-fixtures/pester/SessionLogLifecycle.Tests.ps1',
  'plugins/core/test-fixtures/pester/SessionLogLifecycleMetadata.Tests.ps1',
  'plugins/core/test-fixtures/pester/SessionLogTurnContextBeginTurn.Tests.ps1',
  'plugins/core/test-fixtures/pester/SessionLogAuditReconcile.Tests.ps1',
  'plugins/core/test-fixtures/pester/SessionLogQuarantineRepair.Tests.ps1',
  'plugins/core/test-fixtures/pester/SessionLogP2Contracts.Tests.ps1'
) -Output Detailed
```

- Duration: 63.24s.
- Passed: 153. Failed: 9. Skipped: 0. Inconclusive: 0. NotRun: 0.
- Console SHA-256 `c2f1c8899c14c1d9507a99715537428cad7119ce5e9598112775b3b81b148c6b`.
- Copy: `test-results/p2-pester-runtime-and-sessionlog.txt` (gitignored by `test-results/`).

The 13 `SessionLogP2Contracts.Tests.ps1` facts and the other SessionLog files are inside the 153 passes. An earlier SessionLog-only run, before the markerless failsafe fallback, was 36 passed, 0 failed, 0 skipped in 40.09s. The contracts file was re-run after the fallback and stayed 13 passed.

## Pre-existing failures, not P2 contract regressions

These nine `PluginPowerShellRuntime.Tests.ps1` facts fail because this checkout has no staged plugin and no repo-root marker. They do not exercise the P2 outcome matrix.

- `TEST-MCP-PLUGIN-PSONLY-001 FR-MCP-PLUGIN-PSONLY-003 fails closed before MCP work when the runtime is refused` — `pwsh -File` exit 64 because `plugins/core/.staged-plugin/lib/plugin-hook.ps1` is missing.
- `TEST-MCP-PLUGIN-PSONLY-002 generated wrappers invoke PowerShell hook entrypoint` — missing `plugins/core/.staged-plugin/hooks/scripts/session-end.ps1`.
- `TEST-MCP-PLUGIN-PSONLY-002 status entrypoint returns JSON status without mutation` — same missing staged root.
- `TEST-MCP-PLUGIN-PSONLY-002 synced manifest contains PowerShell runtime files only` — missing `CORE-MANIFEST.yaml`.
- `TEST-MCP-BUGTRIAGE-027 core integrity checker exits 0 on success` — checker invoked with the missing staged root, exit 1.
- `TEST-MCP-TRANSCRIPT-010 does not expose transcript ingestion endpoints through plugins` — missing `.staged-plugin/skills/session/SKILL.md`.
- `TEST-MCP-YAML-MUTATION-001 all staged plugin skills teach object-first YAML mutation` — missing `.staged-plugin/skills`.
- `TEST-MCP-BUGTRIAGE-050 triage skill documents schema-valid REPL status methods` — missing `.staged-plugin/skills/triage/SKILL.md`.
- `TEST-MCP-BUGTRIAGE-051 marker refresh writes sessionId before reporting verified` — `Get-MarkerFileSnapshot` from `/workspace` throws because `AGENTS-README-FIRST.yaml` is absent.

`TEST-MCP-PLUGIN-HEADER-005` and `TEST-MCP-REPL-029` failed before the markerless `failsafe-pending` fallback and pass in this run.

## Build.Tests SessionLifeUnitGate

This is the validator already on the branch. It is not the cumulative unit inventory.

```powershell
dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter "FullyQualifiedName~SessionLifeUnitGate" --logger "trx;LogFileName=p2-sessionlife-unit-gate.trx" --results-directory "docs/receipts/sessionlife-completion/20260930-p2-contracts/test-results"
```

The recorded run used `/tmp/p2-sessionlife-unit-gate` and was copied into `test-results/`.

- SDK: `10.0.401`.
- Exit code: 0.
- TRX counters: total 95, executed 95, passed 95, failed 0, skipped 0, notExecuted 0.
- TRX SHA-256 `dae13c97343ca9e6c987f969a81f34a3cd79b709eee4e684b5993c5b9b8942ec`.

## Gate blocker for P2 exit

`tools/validation/Invoke-SessionLifeUnitGate.ps1` is not in the tree. Nuke `Test` does not emit a `--test-run-id` selected-project inventory. P1 left those producers out of scope. This slice does not invent a competing project list and does not treat the focused Pester total as the machine-readable cumulative unit gate. P2 item 7 stays open until that orchestration exists and a fresh run reports zero failures and zero skips.
