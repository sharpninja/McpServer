# Handoff Ingestion

Handoff ingestion turns a workspace-scoped handoff document into a structured MCP TODO draft. Every public surface delegates to `IHandoffIngestionService`. Do not create TODOs from parsed AI text. Do not edit TODO.yaml or handoff run rows directly.

Worked Path-source sample: `docs/handoffs/example.md`. Ingest that file with `mode: DraftOnly`.

FR-HANDOFF-001 through FR-HANDOFF-007, TR-HANDOFF-*, and TEST-HANDOFF-001 through TEST-HANDOFF-007 live in the MCP requirements store. Markdown under `docs/Project/` is a projection.

## Sources

- `Path`: a workspace-contained Markdown, text, JSON, or YAML file.
- `Content`: caller-supplied text.
- `Artifact`: an MCP document id or a file under `.mcpServer/artifacts/`.

Missing, unsupported, oversized (over 8 MiB), traversal, external, and reparse-escaping sources fail closed and never create a TODO.

## Modes

- `DraftOnly` (default): extract and persist a run. TODO state is never mutated.
- `RequireReview`: persist an approvable run.
- `CreateWhenConfident`: create a TODO only when confidence is at least 0.75 and no error diagnostic exists.

Approval revalidates the stored draft, then calls `ITodoService.CreateAsync`. ID collisions require review and are never renamed.

Replay of the same workspace, content hash, and effective prompt identity returns the existing receipt unless `force=true`. The default prompt identity is `handoff-todo-draft/v1`. Custom `promptTemplateId` values are rejected. Provenance stores extractor-reported `promptVersion` and `templateVersion` and recomputes replay identity from that effective prompt identity.

## Surfaces

- REST: `POST /mcpserver/handoff/ingest`, `GET /mcpserver/handoff/runs/{runId}`, `POST /mcpserver/handoff/runs/{runId}/approve`
- Client: `McpServerClient.Handoff` methods `IngestHandoffAsync`, `GetHandoffRunAsync`, `ApproveHandoffAsync`
- REPL: `workflow.handoff.ingest`, `workflow.handoff.get`, `workflow.handoff.approve`
- Director: `handoff-ingest`, `handoff-get`, `handoff-approve`
- MCP tools: `handoff_ingest`, `handoff_get`, `handoff_approve`
- Plugin skill: `plugins/core/skills/handoff/SKILL.md` plus sibling `invoke.ps1`. The invoke script deserializes the skill YAML examples into objects, serializes params with `ConvertTo-Yaml`, and dispatches `workflow.handoff.ingest`, `workflow.handoff.get`, and `workflow.handoff.approve`.

## Examples

### REPL ingest (DraftOnly)

```yaml
method: workflow.handoff.ingest
params:
  sourceKind: Path
  path: docs/handoffs/example.md
  mode: DraftOnly
```

### REPL inspect

```yaml
method: workflow.handoff.get
params:
  runId: handoff-run-001
```

### REPL approve

```yaml
method: workflow.handoff.approve
params:
  runId: handoff-run-001
  approved: true
  reviewer: operator
```

### Typed client

```csharp
var run = await client.Handoff.IngestHandoffAsync(new HandoffIngestionRequest
{
    SourceKind = HandoffSourceKind.Path,
    Path = "docs/handoffs/example.md",
    Mode = HandoffIngestionMode.DraftOnly,
});

var inspect = await client.Handoff.GetHandoffRunAsync(run.Provenance!.RunId);

var approved = await client.Handoff.ApproveHandoffAsync(
    inspect.Provenance!.RunId,
    new HandoffApprovalRequest
    {
        Approved = true,
        Reviewer = "operator",
    });
```

### REST

```http
POST /mcpserver/handoff/ingest
Content-Type: application/json
X-Api-Key: <token>
X-Workspace-Path: F:\GitHub\McpServer

{"sourceKind":"Path","path":"docs/handoffs/example.md","mode":"DraftOnly"}
```

```http
GET /mcpserver/handoff/runs/{runId}
POST /mcpserver/handoff/runs/{runId}/approve
```

## Durability and provenance

- Processing leases renew while extraction or approval is in flight. Heartbeat updates require the current owner, Processing state, and StateVersion. A stolen owner or version cannot persist a terminal update.
- Persisted provenance includes run ID, source kind and locator, SHA-256 content hash, extraction time, prompt/template version, agent, model, confidence, mode, review state, diagnostics, and created TODO ID.
- Raw source content and credentials are not stored on the run or copied into logs.
