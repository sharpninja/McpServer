# QuadBrain User Guide

QuadBrain is McpServer's multi-model decision engine. It combines four specialized "brains" into a
single committed answer and exposes that answer through an OpenAI-compatible chat-completions endpoint,
so any OpenAI client (including [QBAgent](QBAGENT.md)) can use QuadBrain as a drop-in model.

This guide covers what QuadBrain is, how to configure and provision the four brains, how to call it,
and how it behaves operationally.

## 1. What QuadBrain is

A QuadBrain decision runs four roles in sequence and reconciles them:

- **Creativity** - alternatives, pattern-level opportunities, and creative solution paths.
- **Logic** - structured decomposition, deterministic checks, logical reasoning, deduction, and validity.
- **CuriosityEngine** - missing evidence, challenged assumptions, research and gap detection.
- **ArbiterOfTruth (AoT)** - reconciles the three role outputs over the original input and returns the
  final, committed decision.

Each role is backed by a configurable "brain slot": a provider (OpenAI or OpenAI-compatible), a model id,
an endpoint, a credential reference, and an orchestration weight. The four slots together form the quad.

Two scoping facts matter:

- **Brain definitions are global.** There is one quad, shared by every workspace. You configure it once.
- **A running orchestration is an instance attached to a single session.** Multiple QuadBrain instances
  can run at once, each bound to its own session id; they share the global brain definitions but stay
  isolated for logging and transactions.

## 2. The OpenAI-compatible endpoint

QuadBrain is reached at:

```
POST {baseUrl}/v1/chat/completions
```

It accepts and returns the standard OpenAI chat-completions shape, so you can point any OpenAI SDK at
`{baseUrl}/v1` and set the model to `quadbrain` (the model field is echoed back; any value is accepted).

### Authentication

Authenticate with the workspace token as a Bearer credential (the `X-Api-Key` header is also accepted):

```
Authorization: Bearer <workspace-token>
```

The token is the per-workspace API key from `AGENTS-README-FIRST.yaml`. An invalid or missing token
returns `401`.

### Session attachment

Bind the call to a QuadBrain session/instance with request headers:

- `X-Session-Id: <session-id>` - the session this instance is attached to. Concurrent requests with
  different session ids are independent instances over the same global brains.
- `X-Turn-Id: <turn-id>` - optional, correlates the run's logging and turn transaction to a turn.

A request without `X-Session-Id` runs as an anonymous instance (logging that needs a session is a no-op).

### Example

```bash
curl -sS http://localhost:7147/v1/chat/completions \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Session-Id: my-session-1" \
  -H "Content-Type: application/json" \
  -d '{
        "model": "quadbrain",
        "messages": [
          {"role": "system", "content": "Be precise."},
          {"role": "user", "content": "Plan the change in detail."}
        ]
      }'
```

The full role-tagged transcript is folded into the orchestration input, so QuadBrain sees system context
and prior turns, not just the last user message. The assistant message in the response is the Arbiter's
committed decision (`finish_reason: stop`).

### Tools and tool calls

Pass OpenAI `tools` in the request and QuadBrain may elect to call one. When it does, the response carries
OpenAI `tool_calls` (`finish_reason: tool_calls`) for your client to execute, with one exception:
**MCP-internal tools run server-side** (see [section 6](#6-how-a-decision-is-produced)). The response
`usage` block is a best-effort estimate (QuadBrain does not surface real provider token counts).

## 3. Configuring the brains

QuadBrain configuration lives under `Mcp:BrainSlots` in `appsettings.yaml`. It is **disabled by default**.

```yaml
Mcp:
  BrainSlots:
    ExecutionEnabled: false        # master gate; must be true to invoke providers
    AllowLoopbackEndpoints: false  # allow 127.0.0.1/localhost/private endpoints
    AllowedEndpointHosts: []       # explicit allowlist of custom endpoint hosts
    DefaultTimeoutSeconds: 30
    MaxTimeoutSeconds: 300
    Slots: []                      # the four brain definitions (see below)
```

Each entry in `Slots` defines one brain:

```yaml
    Slots:
      - SlotId: brain-slot-creativity
        Role: Creativity           # Creativity | Logic | CuriosityEngine | ArbiterOfTruth
        DisplayName: Creativity
        ProviderKind: OpenAICompatible # OpenAI | OpenAICompatible
        ModelId: my-model
        Endpoint: http://127.0.0.1:8312/v1   # optional; required for OpenAICompatible
        CredentialReference: env:MY_CREATIVITY_API_KEY
        Enabled: true
        TimeoutSeconds: 180
        MaxOutputTokens: 4096
        SystemPrompt: You are the Creativity brain slot. Generate alternatives and explore creative solution paths...
        OrchestrationWeight: 1.0
        ReplaceExisting: true
```

A ready-to-use template for all four roles ships at
`config/brain-slots/quad-brain-slot-assignments.yaml`; copy its `Slots` into your config and set the
credential environment variables it references.

### Credentials are referenced, never inlined

`CredentialReference` is a safe reference, never a raw key. Supported schemes:

- `env:NAME` - read from the environment variable `NAME`.
- `config:Some:Key` - read from configuration at `Some:Key`.
- `file:/path/to/secret` - read from a file.

### Endpoint policy

For `OpenAICompatible` providers with a custom `Endpoint`, the host must be permitted: add it to
`AllowedEndpointHosts`, or set `AllowLoopbackEndpoints: true` for loopback/private endpoints. Disallowed
endpoints are rejected before any provider call.

### Live execution also requires turn transactions

Every brain invocation commits through the turn transaction coordinator. To run the live loop, transactions
must be enabled and not degraded:

```yaml
Mcp:
  TurnTransactions:
    Enabled: true
    RequiredForMutations: true
```

When `ExecutionEnabled` is false, or transactions are disabled/degraded, invocations fail closed (no
provider is called).

## 4. Provisioning the quad

You can provision the four brain slots two ways.

### Startup seeding (recommended)

When `Mcp:BrainSlots:ExecutionEnabled` is true and `Mcp:BrainSlots:Slots` is populated, the server seeds
the global quad on startup. Seeding is idempotent (keyed by `SlotId`) and never aborts startup if one slot
definition is invalid.

### REST API

Manage slots at runtime under `/mcpserver/brain-slots` (requires the workspace token):

- `PUT /mcpserver/brain-slots/{slotId}` - create or update a slot (body is the upsert request).
- `POST /mcpserver/brain-slots/{slotId}/enable?replaceExisting=true` - enable a slot.
- `POST /mcpserver/brain-slots/{slotId}/disable` - disable a slot.
- `GET /mcpserver/brain-slots` - list slots (credentials never returned).
- `GET /mcpserver/brain-slots/{slotId}` - get one slot.
- `DELETE /mcpserver/brain-slots/{slotId}` - soft-delete a slot.
- `GET /mcpserver/brain-slots/status` - quad readiness.

The same operations are available over the MCP STDIO/transport surface and the typed
`SharpNinja.McpServer.Client` (`BrainSlotClient`).

### Readiness

`GET /mcpserver/brain-slots/status` reports whether the quad is ready:

```json
{
  "quadReady": true,
  "roleReadiness": { "Creativity": true, "Logic": true,
                     "CuriosityEngine": true, "ArbiterOfTruth": true },
  "missingRoles": [], "disabledRoles": [], "validationErrors": []
}
```

A role is ready when exactly one enabled slot serves it with a valid provider, model, credential reference,
endpoint, and an active trusted-party signing key. The quad is ready only when all four roles are ready.

## 5. Direct orchestration and weight controls

Besides the OpenAI surface, the quad can be driven directly:

- `POST /mcpserver/brain-slots/orchestrate` - run the full four-role loop and return the structured
  `QuadBrainOrchestrationResponse` (status, final `output`, per-role results, transaction ids).
- `POST /mcpserver/brain-slots/aot/reconcile` - run Arbiter-of-Truth reconciliation over supplied role
  evidence.
- `POST /mcpserver/brain-slots/{slotId}/invoke` - invoke a single brain slot.
- `POST /mcpserver/brain-slots/weights/update` - apply a durable, audited orchestration-weight update.

Weight updates are safety-gated: the request must carry `aotApproved`, `adminApproved`,
`safetyGatesPassed`, a `reasonText`, and (optionally) `expectedVersions` for optimistic concurrency. The
update commits through a transaction, increments each role's weight version, and writes audit rows; a
version mismatch is rejected.

## 6. How a decision is produced

1. Creativity, Logic, then CuriosityEngine are each invoked with a role-specific prompt; each
   must commit a non-empty output or the loop rejects.
2. ArbiterOfTruth reconciles the three committed outputs over the original input and commits the final
   decision.
3. If the Arbiter elects tools, **MCP-internal tools** (named `mcp_*` - TODO, repo, and FR/TR/TEST
   requirements mutations) run **server-side** through the transaction-gated services and are stripped from
   the response; **external tools** are emitted to the caller as OpenAI `tool_calls`. Internal-tool failures
   are surfaced as an assistant note and recorded to the session log, never emitted as tool commands.
4. The full prompt and output of every brain interaction are logged (best-effort, secret-redacted) to the
   attached session's log.

## 7. Security model

- **Containment.** QuadBrain reaches only the brain providers you configure. There is no implicit fallback.
- **Credential references only.** Raw secrets are never stored or returned; only `env:`/`config:`/`file:`
  references are persisted.
- **Endpoint allowlist.** Custom endpoints must be permitted by host allowlist or the loopback gate.
- **Transaction gating.** Every invocation and weight update commits through the turn transaction
  coordinator with a trusted-party signed manifest; degraded transactions fail closed.
- **Audit.** Invocations and weight updates write hashed audit rows; full-text dialog is captured in the
  session log.

## 8. Troubleshooting

- **`quadReady` is false.** Check `GET /mcpserver/brain-slots/status` `validationErrors`/`missingRoles`.
  Common causes: a role has no enabled slot, an unresolved credential reference, a disallowed endpoint, or a
  missing trusted-party signing key.
- **`/v1` returns an empty assistant message.** The loop rejected (for example the quad is not ready, a role
  produced no output, or execution is disabled). Use `POST /mcpserver/brain-slots/orchestrate` to see the
  rejection `reason`.
- **`/v1` returns `500` with `{"error":{"type":"server_error"}}`.** An orchestration/provider/storage error
  occurred; check the server log for the inner exception.
- **A provider is never called.** Confirm `Mcp:BrainSlots:ExecutionEnabled: true` and that
  `Mcp:TurnTransactions` is enabled and not degraded.
- **Inter-brain logging is empty.** Send `X-Session-Id` (and `X-Turn-Id`) so the run is attached to a
  session; logging that needs a session is a no-op without it.

## See also

- [QBAgent User Guide](QBAGENT.md) - an agent that uses QuadBrain as its model and executes its tool calls.
- [User Guide](USER-GUIDE.md) - the full McpServer configuration and REST reference.
