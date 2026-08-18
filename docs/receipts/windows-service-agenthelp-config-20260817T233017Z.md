# Receipt: update Windows service Agent Help config

TimestampUtc: 2026-08-17T23:30:17Z
WorkClass: user-directed general action (live Windows service appsettings)
Implementer: GrokCode
SessionId: GrokCode-20260817T120000Z-agent-help-grok-cli
RequestId: req-20260817T232801Z-005-update-windows-service-config

## Service

- Name: McpServer
- State: Running
- StartMode: Auto
- StartName: LocalSystem
- PathName: C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147
- Config file: C:\ProgramData\McpServer\appsettings.yaml

## Change

Object-first mutation via `plugins/core/lib-ps/yaml-object-mutation.ps1`.

Before:

- AgentHelp.DefaultExecutionStrategy=grok-cli
- AgentHelp.HelperModel=grok-4.5

After:

- AgentHelp.DefaultExecutionStrategy=grok-cli
- AgentHelp.HelperModel=grok-4.5
- AgentHelp.Enabled=true

On-disk (re-read):

```
AgentHelp:
  DefaultExecutionStrategy: grok-cli
  HelperModel: grok-4.5
  Enabled: true
```

LastWriteTimeUtc: 2026-08-17T23:30:09.0404870Z
Length: 58975

## Live verify

Create-session `help-20260817233017-0bf8ab01a3af4e92a0c6c38ab8dba245`:

- executionStrategy: grok-cli
- modelRequested: grok-4.5
- modelResolved: grok-4.5
- status: idle

## Not written

- No AgentHelp effort key. AgentHelpOptions has no effort property. grok-cli already passes `--effort high` and `--reasoning-effort high`.

## Not changed

- Windows SCM registration (PathName, StartName, StartMode)
- Repo F:\GitHub\McpServer\appsettings.yaml (still DefaultExecutionStrategy: one-shot-cli)
- VoiceConversation.DefaultExecutionStrategy remains copilot-cli
- No Nuke UpdateService / binary deploy
