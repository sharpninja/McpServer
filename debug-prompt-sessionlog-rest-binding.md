# Debug prompt: `/mcpserver/sessionlog` REST endpoint binding + retrieval bugs

## Symptom

Two bugs encountered while a ClaudeCode agent tried to maintain a workspace session log against `http://PAYTON-LEGION2:7147` from an external workspace (`F:\GitHub\FeatureFlags`) on 2026-05-16.

### Bug 1: POST shape is undocumented and self-contradictory

Every shape produced a different error; only one worked, and the working shape is the opposite of what existing session records imply.

#### Attempt A: top-level fields, no `dto` wrapper, `workspace` as string

```bash
curl -X POST 'http://PAYTON-LEGION2:7147/mcpserver/sessionlog' \
  -H 'Content-Type: application/json' \
  -H 'X-Api-Key: <key>' \
  -d '{
    "sessionId":"ClaudeCode-20260516T154337Z-test",
    "sourceType":"ClaudeCode",
    "agent":"ClaudeCode",
    "model":"claude-sonnet-4-6",
    "title":"test",
    "workspace":"F:/GitHub/FeatureFlags"
  }'
```
Response (HTTP 400):
```json
{
  "errors": {
    "dto": ["The dto field is required."],
    "$.workspace": ["The JSON value could not be converted to McpServer.Support.Mcp.Models.WorkspaceInfoDto."]
  }
}
```
Two contradictions surface here: a `dto` wrapper is allegedly required, yet `workspace` IS already being parsed at the top level (it fires a deserializer error against `WorkspaceInfoDto`). The binder cannot be both requiring a wrapper and parsing un-wrapped fields.

#### Attempt B: top-level fields, `workspace` as object

```json
{"sessionId":"...","sourceType":"ClaudeCode","agent":"ClaudeCode","title":"...","workspace":{"path":"F:/GitHub/FeatureFlags","name":"FeatureFlags"}}
```
Response: `{"error":"SourceType is required."}`

Now the framework-level `errors.dto` complaint is gone, and a domain-level error fires. So when `workspace` becomes a valid `WorkspaceInfoDto`, the binder somehow stops complaining about the missing `dto` wrapper, yet still refuses to bind `sourceType` despite "ClaudeCode" being a known enum value (it appears in existing records).

#### Attempt C: `dto`-wrapped, fields inside

```json
{"dto":{"sessionId":"...","sourceType":"ClaudeCode","agent":"ClaudeCode","title":"...","workspace":{"path":"...","name":"..."}}}
```
Response: `{"error":"SourceType is required."}` (no framework errors; only the domain-level message)

Tried with PascalCase keys (`SessionId`, `SourceType`, ...) inside the wrapper — same result.
Tried with `sourceType` as the integer enum value `1` — same result.

#### Attempt D: minimal payload, top-level fields only, no `workspace` at all

```bash
curl -X POST 'http://PAYTON-LEGION2:7147/mcpserver/sessionlog' \
  -H 'Content-Type: application/json' \
  -H 'X-Api-Key: <key>' \
  -d '{"sourceType":"ClaudeCode","sessionId":"ClaudeCode-20260516T154337Z-release-v1-audit-001","agent":"ClaudeCode","title":"RELEASE-V1-AUDIT-001 end-to-end remediation"}'
```
Response (HTTP 200): `{"id":278,"sourceType":"ClaudeCode","sessionId":"ClaudeCode-20260516T154337Z-release-v1-audit-001"}`

So the working shape is **no `dto` wrapper, omit `workspace` entirely**. But:
- Attempts A and C strongly imply the binder expects a `dto` wrapper.
- Attempt B implies `workspace` must be an object.
- Yet D, which violates both implications, succeeds.

The model binder appears to silently fall through different paths depending on which fields are present, and the diagnostic messages do not describe the actually-accepted shape.

### Bug 2: Created session is not retrievable

After the successful Attempt D returns `{"id":278,"sourceType":"ClaudeCode","sessionId":"ClaudeCode-20260516T154337Z-release-v1-audit-001"}`:

```bash
curl 'http://PAYTON-LEGION2:7147/mcpserver/sessionlog/ClaudeCode-20260516T154337Z-release-v1-audit-001'
# HTTP 404, empty body
```

Same for `/278` (integer id form):
```bash
curl 'http://PAYTON-LEGION2:7147/mcpserver/sessionlog/278'
# HTTP 404, empty body
```

And the list endpoint does not return it either, even filtered by sourceType:
```bash
curl 'http://PAYTON-LEGION2:7147/mcpserver/sessionlog?limit=50&sourceType=ClaudeCode'
# returns 10 records, none of which is the one just POSTed
```

So the POST either:
- silently dropped the record after returning a `200 OK` with an id, or
- routed it into a write-only audit table not exposed by the list/get endpoints, or
- the storage write succeeded but a per-row tenant/workspace filter on read excludes it (no `workspace` was supplied).

### Bug 3 (secondary): turn-append endpoints all 404

Tried both pluralizations and a PUT variant — all 404. Either the endpoint paths differ from convention or they are not exposed via REST at all (only via the REPL workflow methods).

```bash
curl -X POST 'http://PAYTON-LEGION2:7147/mcpserver/sessionlog/<sessionId>/turn' ...   # 404
curl -X POST 'http://PAYTON-LEGION2:7147/mcpserver/sessionlog/turn' ...                # 404
curl -X PUT  'http://PAYTON-LEGION2:7147/mcpserver/sessionlog/<sessionId>' ...         # 404
```

### Bug 4 (secondary): REPL auth refuses marker-file pickup

`mcpserver-repl --agent-stdio` (installed as a dotnet tool at `C:\Users\kingd\.dotnet\tools\mcpserver-repl`) was launched from the workspace root containing a valid `AGENTS-README-FIRST.yaml` with a live API key. The REPL bootstrapped successfully, but every subsequent `workflow.sessionlog.*` call returned:

```yaml
type: error
payload:
  code: method_invocation_error
  message: 'Authentication required: no credential is configured on this client. Set BearerToken (for interactive users via OIDC) or ApiKey (for agents via the AGENTS-README-FIRST.yaml marker file) before calling any endpoint.'
```

Attempts that did not work:
- Sending `apiKey` in the `hello` envelope payload.
- Setting `MCP_API_KEY`, `MCPSERVER_API_KEY`, `MCP_BASE_URL` env vars.
- Running from the repo root so the marker file is in CWD.

Either the marker-file discovery is silently failing, or there is an undocumented env var / argument the REPL needs.

## Repro environment

- Server: `http://PAYTON-LEGION2:7147` (the same Kestrel host as the working `/mcpserver/todo` REST endpoints — which DO accept the same `X-Api-Key`)
- Agent workspace: `F:\GitHub\FeatureFlags` (has `AGENTS-README-FIRST.yaml`, marker signature verified, `/health` nonce verified)
- Caller: PowerShell 7 / Git Bash on Windows
- Existing record sourceTypes observed via GET: `ClaudeCode`, `ClaudeCowork`, `Codex`
- An existing `ClaudeCode-20260515T195705Z-featureflags-import` record is queryable, proving the storage path is real and the GET endpoint works for *some* records.

## What "good" looks like

1. POST `/mcpserver/sessionlog` either documents the accepted shape in its `400` error responses, or accepts a single canonical shape. Recommended: top-level fields, no `dto` wrapper. If a wrapper is needed, the rejection message must say so unambiguously and stop firing when the wrapper is absent OR when an unrelated field is malformed.
2. After a successful POST returning `{"id": N, "sessionId": "X"}`, both `GET /mcpserver/sessionlog/X` and `GET /mcpserver/sessionlog?sourceType=…` must return that record.
3. REST endpoints for appending a turn, dialog, and actions either exist and are documented (e.g. `POST /mcpserver/sessionlog/{sessionId}/turn`), or `405 Method Not Allowed` is returned with an `Allow` header naming the supported verbs and an explanation that the REPL workflow methods are the only write path.
4. `mcpserver-repl --agent-stdio` picks up the API key from `AGENTS-README-FIRST.yaml` in CWD (or an env var); if neither resolves, the failure message must say which paths it searched.

## Suggested investigation order

1. **Locate the POST handler.** Grep for the action method behind `POST /mcpserver/sessionlog` in `src/McpServer.Web/Controllers/` (or wherever Kestrel routing lives). Look for a `[FromBody]` parameter binding to either a `dto` property or the request body directly.
2. **Trace why Attempt D succeeds.** With only `sourceType`, `sessionId`, `agent`, `title` at the top level, the action method must be using a different binder (perhaps a custom `IModelBinder` or a fallback) than the one that fires the `errors.dto` complaint. The two code paths need to converge or the rejected paths need to produce coherent diagnostics.
3. **Trace why id 278 is not retrievable.** Inspect the storage write in the success path of the POST handler. If it goes through a different repository than `GET`, find the asymmetry. Likely candidates:
   - workspace-scoped row-level filtering on read (POST omitted `workspace`)
   - the integer id is auto-generated but a separate `sessionId` index lookup is unindexed
   - the POST writes into a staging table that requires a follow-up commit call
4. **Inspect REPL credential resolution.** Grep `mcpserver-repl` source for `AGENTS-README-FIRST` / marker resolution; check if it requires the workspace root to be passed as a CLI arg or env var rather than discovered from CWD.

## Acceptance

Write a regression test for each bug:
- 4 xUnit tests asserting POST accepts each of: minimal top-level shape, top-level with `workspace` object, `dto`-wrapped shape, and rejects an actually-malformed payload with a coherent message.
- 1 xUnit test asserting POST → GET by sessionId round-trip returns the same record.
- 1 xUnit test asserting POST → list endpoint includes the new record.
- 1 xUnit test for the REPL credential pickup from `AGENTS-README-FIRST.yaml` in CWD.

When the four tests are green, the FeatureFlags workspace agent can drop its local `.claude/audit-session-log.md` fallback and write through the MCP session-log API the way the `mcpserver:session` skill documents.

## Evidence trail

The full repro session is in `F:\GitHub\FeatureFlags\.claude\audit-session-log.md` (local fallback log) plus the transcript at `C:\Users\kingd\.claude\projects\F--GitHub-FeatureFlags\cde7699b-17ba-4b3d-9db0-3f62e697d7a5.jsonl`. Each curl invocation and response shown above came from that session.
