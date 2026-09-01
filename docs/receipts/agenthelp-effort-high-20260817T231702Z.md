# Receipt: Agent Help grok effort is already high

TimestampUtc: 2026-08-17T23:17:02Z
WorkClass: user-directed general action (verify live Agent Help model effort)
Implementer: GrokCode
SessionId: GrokCode-20260817T120000Z-agent-help-grok-cli
RequestId: req-20260817T231702Z-004-set-agenthelp-effort-high

## Request

Set effort to high on the Agent Help model.

## Result

No live YAML change. Agent Help already invokes grok-cli with effort `high`.

## Why no config write

`AgentHelpOptions` has `HelperModel` and `DefaultExecutionStrategy`. It has no effort property. A live `AgentHelp.HelperEffort: high` key would be ignored.

`GrokCliAgentExecutionStrategy` hardcodes:

- `--effort high`
- `--reasoning-effort high`

Source: `src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs` (`HighestEffort = "high"`, `BuildGrokArgumentList`).
Test: `GrokCliAgentExecutionStrategyTests.BuildGrokArgumentList_ContainsExpectedFlagsInOrder`.

Live strategy is already `grok-cli` with model `grok-4.5` (`C:\ProgramData\McpServer\appsettings.yaml` AgentHelp section).

## Docs: high is the correct Grok 4.5 effort

- xAI reasoning docs: `grok-4.5` accepts `low` / `medium` / `high` (default `high`). `xhigh` is grok-4.6+; on 4.5 it is treated as `high`.
- Grok CLI user guide `/effort`: levels `low`, `medium`, `high`, `xhigh`.
- Grok CLI user guide headless: `--reasoning-effort` / `--effort` canonical levels include `high`.

## Deployed binary

`C:\ProgramData\McpServer\McpServer.Support.Mcp.exe` (single-file, LastWriteTimeUtc 2026-08-12T21:55:30Z, length 208607591):

- UTF-16 `--effort` hits: 1
- UTF-16 `--reasoning-effort` hits: 1
- metadata names `HighestEffort` and `GrokHighestEffort` present

## Not done

- Did not write an unbound YAML effort key.
- Did not add a new AgentHelp options property (that is product code; needs a BDPv4 plan if the operator wants a configurable knob).
- Did not change `~/.grok/config.toml` `default_reasoning_effort` (currently `xhigh` for interactive grok-4.6). Agent Help overrides via CLI flags.
