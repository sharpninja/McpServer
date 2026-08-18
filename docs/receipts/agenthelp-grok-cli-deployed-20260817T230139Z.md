# Receipt: set Agent Help grok-cli on deployed service

TimestampUtc: 2026-08-17T23:01:39Z
WorkClass: user-directed general action (live service configuration)
Implementer: GrokCode
SessionId: GrokCode-20260817T120000Z-agent-help-grok-cli
RequestId: req-20260817T230026Z-001-set-agent-help-grok-cli

## Request

Set grok-cli as the agent configuration for the Agent Help endpoint in the deployed Windows service.

## Pre-change observations

- Live install path: `C:\ProgramData\McpServer`
- Live `appsettings.yaml` had no `AgentHelp` section (`HasAgentHelp=False` from object deserialize).
- `VoiceConversation.DefaultExecutionStrategy` was and remains `copilot-cli`.
- `Triage.ExecutionStrategy` was and remains `grok-cli`.
- Probe session `help-20260817230038-bfa76867835a41a89d560ca7415d2441` returned `executionStrategy=grok-cli` (code default) and `modelRequested=gpt-5.3-codex` / `modelResolved=gpt-5.3-codex` (code default `HelperModel`).

## Change

Object-first YAML mutation via `plugins/core/lib-ps/yaml-object-mutation.ps1` `Update-McpYamlObject` (script `docs/receipts/_set-agenthelp-grok-cli-20260817T230026Z.ps1`).

Wrote to `C:\ProgramData\McpServer\appsettings.yaml`:

- `AgentHelp.DefaultExecutionStrategy = grok-cli`
- `AgentHelp.HelperModel = auto`

`HelperModel=auto` is the companion value that keeps Grok CLI from receiving `--model gpt-5.3-codex`.

File LastWriteTimeUtc after write: 2026-08-17T23:01:22Z
File length after write: 58954

On-disk tail (re-read):

```
AgentHelp:
  DefaultExecutionStrategy: grok-cli
  HelperModel: auto
```

## Post-change live verification

Create-session `help-20260817230139-9115dbe08cdc45c683ff93e268139ded`:

- `executionStrategy`: grok-cli
- `modelRequested`: auto
- `modelResolved`: auto
- `status`: idle

This proves the running service reloaded `appsettings.yaml` (`reloadOnChange: true`).

## Not changed

- Repo `F:\GitHub\McpServer\appsettings.yaml` still has `AgentHelp.DefaultExecutionStrategy: one-shot-cli`.
- Repo `appsettings.Staging.yaml` has no `AgentHelp` section.
- `VoiceConversation.DefaultExecutionStrategy` remains `copilot-cli`.
- No product source, tests, or Nuke deploy. Live config only.

## Classification

Class 2: user-directed lab/ops. No plan-step done claim. No MCP TODO marked done.
