# Prompt: Claude Code Use Workspace McpServer Plugin With Requirement Layers

Copy the block below into a fresh Claude Code session in a workspace that contains
`AGENTS-README-FIRST.yaml`.

---

You are Claude Code running with the McpServer Claude Code plugin.

Use the workspace-synced plugin checkout, not a stale cached plugin copy:

- Expected workspace-generated plugin package:
  `F:\GitHub\McpServerManager\lib\McpServer\plugins\core\.staged-plugin`
- Expected canonical source: `F:\GitHub\McpServerManager\lib\McpServer`
- Optional sibling checkout when explicitly synced:
  `F:\GitHub\mcpserver-claude-code-plugin`
- If your active plugin path is a sibling checkout or cache directory, verify its
  `CORE-MANIFEST.yaml` matches the workspace-generated plugin package after the
  latest `SyncAgentPlugins` run.
- If the plugin path or version is stale, stop MCP plugin usage and report the
  stale plugin path/version instead of continuing with old tools.

## Required Startup

1. Read `AGENTS-README-FIRST.yaml` from the active workspace.
2. Verify the marker signature and call `/health` with a nonce.
3. Confirm the active Claude plugin root is the workspace-generated
   `.staged-plugin` package, a sibling checkout synced from it, or a cache
   refreshed from it.
4. Open a session-log turn through the Claude plugin/session tools.
5. Use plugin/session/requirements tools for MCP mutations. Do not use raw REST
   for normal session, TODO, or requirements work unless the plugin is unavailable
   and you are only doing read-only diagnosis.

## Requirement Layer Surfaces To Use

The synced plugin must expose requirement layer management and effective
requirements through the plugin wrappers:

- PowerShell plugin status should report these entries under
  `requirementMethods` and `requirementClientMethods`.
- Layer catalog:
  - `workflow.requirements.listLayers`
  - `workflow.requirements.createLayer`
  - `workflow.requirements.updateLayer`
- Effective requirements:
  - `workflow.requirements.effective`
- Typed-client fallback names:
  - `client.Requirements.ListRequirementLayersAsync`
  - `client.Requirements.CreateRequirementLayerAsync`
  - `client.Requirements.UpdateRequirementLayerAsync`
  - `client.Requirements.GetEffectiveRequirementsAsync`
- Node/plugin tool aliases when available:
  - `req_list_layers`
  - `req_create_layer`
  - `req_update_layer`
  - `req_effective`

FR, TR, and TEST create/update calls must preserve requirement scope fields when
provided:

```yaml
scopeStartLayerKey: layer-2
scopeEndLayerKey: layer-4
```

## Validation To Run

Run this smoke validation with the active plugin surface:

1. List requirement layers:

```yaml
method: workflow.requirements.listLayers
params: {}
```

2. Query effective requirements for the active layer:

```yaml
method: workflow.requirements.effective
params: {}
```

3. If the workspace allows a non-destructive preview layer query, run:

```yaml
method: workflow.requirements.effective
params:
  layerKey: layer-1
```

4. Verify the plugin recognizes scoped requirement fields by preparing, but not
   applying unless explicitly asked, a create/update payload that includes both
   `scopeStartLayerKey` and `scopeEndLayerKey`.

5. Report the exact plugin root, plugin version, session id, request id, command
   names used, `CORE-MANIFEST.yaml` source, and whether the layer/effective calls
   succeeded.

## Expected Result

Claude Code should be operating through the workspace-synced plugin and should
be able to call the layer/effective requirement surfaces without falling back to
stale cached plugin code. If any requirement layer command is missing, report the
active plugin path and version as stale and do not claim validation passed.
