# Example Handoff: Document a bounded ingest-to-TODO sample

This file is the worked Path-source example for `docs/Handoff-Ingestion.md` and
`plugins/core/skills/handoff/SKILL.md`. Ingest it with `mode: DraftOnly`. Do not
run `CreateWhenConfident` against it on a live workspace.

## Proposed TODO

- Id: EXAMPLE-HANDOFF-001
- Title: Sample handoff ingest for operator documentation
- Section: MCP Server
- Priority: low
- Estimate: 1h

## Description

- Demonstrate Path-source handoff ingestion using a workspace-contained Markdown file.
- Keep the sample bounded so agents can inspect a run without mutating TODO state.

## Technical details

- Source kind: Path
- Path: docs/handoffs/example.md
- Mode: DraftOnly
- Public surfaces: REST `/mcpserver/handoff/*`, typed `McpServerClient.Handoff`,
  REPL `workflow.handoff.*`, Director `handoff-*`, MCP tools `handoff_*`, plugin
  skill `plugins/core/skills/handoff/SKILL.md` plus sibling `invoke.ps1`.

## Implementation tasks

- [ ] Ingest this file with `workflow.handoff.ingest` mode DraftOnly.
- [ ] Inspect the run with `workflow.handoff.get`.
- [ ] Do not approve this sample on a live workspace.

## Dependencies

None.

## Requirement links

- FR-HANDOFF-001
- FR-HANDOFF-007
- TR-HANDOFF-SURFACE-001
- TEST-HANDOFF-006

## Risks

- Approving this sample would create EXAMPLE-HANDOFF-001. Keep DraftOnly.
