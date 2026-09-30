# P2 unit gate (no PluginIntegration) — Legion run

- **Tip SHA:** `c4a3caed56d5e5da3abd4392f1c700e801bf4476` (`c4a3caed` includes post-run Pester staging/BOM fixes; gate artifacts below are from `bff4a8ce` tip at run start)
- **Machine:** PAYTON-LEGION2
- **Killed prior gate:** `p2-unit-legion-20260930T1430Z` (and overlapping `1445Z`) — confirmed no orphan testhost from this worktree; `.nuke\temp\build.log` unlocked
- **RunId:** `p2-unit-legion-20260930T1450Z`
- **Gate exit:** rejected by `CheckSessionLifeUnitGate` — `nonzero-command-exit` affected `unit:pester` (and build-tests also exit 1)
- **Do NOT claim HV. Do NOT merge.**

## Exclusion (deliberate)

PluginIntegration removed from SessionLife **unit** inventory:

- `build/Build.Test.cs` — `!p.Name.Contains("PluginIntegration")` alongside `IntegrationTests`
- `tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs` — asserts exclusion
- Live PI classes tagged `Category=Integration` (belt-and-suspenders); PI remains on `PluginSessionLogIntegration` / P6
- `tools/validation/Invoke-SessionLifeUnitGate.ps1` comments document the cut

## `unit/selected-projects.json` (confirm no PI)

7 projects; **PluginIntegration absent**:

1. McpServer.Support.Mcp.Tests
2. McpServer.Client.Tests
3. McpServer.Cqrs.Tests
4. McpServer.Launcher.Tests
5. McpServer.McpAgent.Tests
6. McpServer.Repl.Core.Tests
7. McpServer.QBAgent.Tests

## Counts

| Lane | Exit | Result |
|------|------|--------|
| Pester | 1 | total 193, passed 190, failed **3**, skipped 0 |
| Nuke Test | **0** | all 7 projects Passed (Support.Mcp 2877, Client 301, Cqrs 33, Launcher 20, McpAgent 63, Repl.Core 866, QBAgent 90) — **0 unit failures** |
| Build.Tests | 1 | total 321, passed 316, failed **5** |

Nuke Test duration ~3:25 (vs prior multi-host PI hang).

## Remaining red (non-PI)

### Pester (3)

1. `TEST-MCP-YAML-MUTATION-001` — staged skills beyond session/triage lacked YAML Mutation Rule (sync left extras). **Follow-up fix pushed in `c4a3caed`** (prune + append rule).
2. `omits metadata on durable reopen...` — FakeRepl `Add-Content -Encoding utf8` BOM / JSON parse. **Follow-up fix in `c4a3caed`** (AppendAllText no-BOM + TrimStart FEFF).
3. `refreshes same-path marker drift...` — `Invoke-WorkflowAppendActions` returned false. **Not fixed** (needs deeper marker/bootstrap mock diagnosis).

### Build.Tests (5) — pre-existing / out-of-scope for this cut

- `HandoffD3D5OverlayTests` / `DocsSyncG8OverlayTests` — `FR-MCP-SERVICEUPDATE-001` docs gap
- `SyncAgentPluginsChecksumTests` / `DocsSyncG8` checksum twin — missing sibling official plugin roots on this machine
- `TrimAnalysisWarningInventoryTests` — `McpServer.QBAgent` trim inventory drift

Documented rather than large rewrite per operator guidance.

## Ready for Grok HV?

**no** — Pester + Build.Tests still red; HV not claimed.