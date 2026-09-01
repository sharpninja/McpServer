# Handoff Ingestion

Handoff ingestion turns a workspace-scoped handoff document into a structured MCP TODO draft. Every public surface delegates to `IHandoffIngestionService`.

## Sources

- `Path`: a workspace-contained Markdown, text, JSON, or YAML file.
- `Content`: caller-supplied text.
- `Artifact`: an MCP document id or a file under `.mcpServer/artifacts/`.

Missing, unsupported, oversized (over 8 MiB), traversal, external, and reparse-escaping sources fail closed and never create a TODO.

## Modes

- `DraftOnly` (default): extract and persist a run. TODO state is never mutated.
- `RequireReview`: persist an approvable run.
- `CreateWhenConfident`: create a TODO only when confidence is at least 0.75 and no error diagnostic exists.

Approval revalidates the stored draft, then calls `ITodoService.CreateAsync`. ID collisions require review and are never renamed. Replay of the same workspace, content hash, and prompt version `handoff-todo-draft/v1` returns the existing receipt unless `force=true`.

## Surfaces

- REST: `POST /mcpserver/handoff/ingest`, `GET /mcpserver/handoff/runs/{runId}`, `POST /mcpserver/handoff/runs/{runId}/approve`
- Client: `IngestHandoffAsync`, `GetHandoffRunAsync`, `ApproveHandoffAsync`
- REPL: `workflow.handoff.ingest`, `workflow.handoff.get`, `workflow.handoff.approve`
- Director: `handoff-ingest`, `handoff-get`, `handoff-approve`
- MCP tools: `handoff_ingest`, `handoff_get`, `handoff_approve`
- Plugin skill: `plugins/core/skills/handoff/SKILL.md`

Raw source content and credentials are not stored on the run or copied into logs.
