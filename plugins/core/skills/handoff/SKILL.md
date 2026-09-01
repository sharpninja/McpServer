---
name: handoff
description: Use when the user asks to ingest a handoff document, inspect a handoff run, or approve a generated MCP TODO draft.
version: 0.1.0
---

# Handoff Ingestion

Use the shared `workflow.handoff.*` REPL namespace or native `handoff_ingest` / `handoff_get` / `handoff_approve` tools. Every surface, including this skill's ingest/get/approve workflow, must go through `IHandoffIngestionService`. Do not create TODOs from parsed AI text yourself.

Do not edit TODO.yaml or handoff run rows directly.

## Ingest

```yaml
method: workflow.handoff.ingest
params:
  sourceKind: Path
  path: docs/handoffs/example.md
  mode: DraftOnly
```

`sourceKind` is `Path`, `Content`, or `Artifact`. `mode` is `DraftOnly` (default), `RequireReview`, or `CreateWhenConfident`.

DraftOnly never mutates TODO state. RequireReview stores an approvable run. CreateWhenConfident creates a TODO only when confidence is at least 0.75 and no error diagnostic exists.

## Inspect

```yaml
method: workflow.handoff.get
params:
  runId: handoff-run-001
```

## Approve

```yaml
method: workflow.handoff.approve
params:
  runId: handoff-run-001
  approved: true
  reviewer: operator
```

Approval revalidates the stored draft before calling the TODO service. ID collisions are never silently renamed.
