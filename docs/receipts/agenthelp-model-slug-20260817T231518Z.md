# Receipt: validate Grok model slug and set live Agent Help model

TimestampUtc: 2026-08-17T23:15:18Z
WorkClass: user-directed general action (live service configuration + docs lookup)
Implementer: GrokCode
SessionId: GrokCode-20260817T120000Z-agent-help-grok-cli
RequestId: req-20260817T231236Z-003-validate-grok-model-slug

## Question

What is the exact model slug to use for the operator request "set the model to grok-4.5-high"?

## Verdict

`grok-4.5-high` is not a valid model id.

Official Grok 4.5 slug: `grok-4.5`

`high` is a reasoning-effort setting, not part of the model slug. Agent Help grok-cli already pins `--effort high` and `--reasoning-effort high`.

## Sources re-read

1. https://docs.x.ai/developers/models.md
   - Catalog lists `grok-4.6`, `grok-4.5`, `grok-4.3`, and other ids.
   - No `grok-4.5-high`.
   - Current recommended text/code model is Grok 4.6 (`grok-4.6`).

2. https://docs.x.ai/developers/models/grok-4.5
   - Model name: `grok-4.5`
   - Aliases: `grok-4.5-latest`, `grok-build-latest`

3. https://docs.x.ai/developers/model-capabilities/text/reasoning
   - `grok-4.5` supports `reasoning_effort`: `low` / `medium` / `high` (default).
   - `high` is a parameter value, not a model suffix.

4. Local CLI: `grok models` (exit 0)
   - Default model: `grok-4.6`
   - Available models: `grok-4.6` (default), `grok-4.5`
   - No `grok-4.5-high`

## Live config change (unfinished prior request)

Object-first mutation of `C:\ProgramData\McpServer\appsettings.yaml`:

- Before: `AgentHelp.DefaultExecutionStrategy=grok-cli`, `HelperModel=auto`
- After: `AgentHelp.DefaultExecutionStrategy=grok-cli`, `HelperModel=grok-4.5`

On-disk re-read:

```
AgentHelp:
  DefaultExecutionStrategy: grok-cli
  HelperModel: grok-4.5
```

Live create-session `help-20260817231518-ef612ec964cc4be998bf30ce1c8b9f0f`:

- `executionStrategy`: grok-cli
- `modelRequested`: grok-4.5
- `modelResolved`: grok-4.5

## Not changed

- Did not set `HelperModel` to `grok-4.5-high` (invalid slug).
- Did not switch to `grok-4.6` (current CLI default / docs recommendation). Operator asked for Grok 4.5.
- Repo `appsettings.yaml` still has `AgentHelp.DefaultExecutionStrategy: one-shot-cli`.
- No product source or Nuke deploy.
