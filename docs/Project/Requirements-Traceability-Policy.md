# Requirements Traceability Policy

Date: 2026-03-05

This policy defines how requirement IDs are represented and validated across project documents.

## Canonical Sources

- Functional requirements: `Functional-Requirements.md`
- Technical requirements: `Technical-Requirements.md`
- Testing requirements: `Testing-Requirements.md`
- FR-to-TR mapping: `TR-per-FR-Mapping.md`
- Status matrix: `Requirements-Matrix.md`

## Matrix Row Strategy

- Every FR entry must have an explicit row in `Requirements-Matrix.md`.
- TR and TEST entries may use either:
  - explicit per-ID rows, or
  - normalized range rows (for example `TR-MCP-DATA-001–003`) when the range is contiguous and same-status.
- Planned requirements without implementation evidence must include backlog linkage in matrix/source notes.

## Validation Rule

- Use `scripts/Validate-RequirementsTraceability.ps1` to validate:
  - FR coverage in mapping and matrix
  - TR coverage in matrix via explicit IDs or range rows
  - TEST coverage in matrix via explicit IDs
- Default mode fails on FR coverage gaps and reports TR/TEST gaps as warnings; use `-StrictTrAndTestCoverage` to fail on TR/TEST gaps.

## Intentional Numbering Gaps

Some TR series begin at `-002` or skip a sequence number. These gaps are intentional and SHALL be left in place rather than backfilled with stub entries:

- **TR-MCP-WS-001** is intentionally absent; the workspace series begins at `TR-MCP-WS-002` (Workspace Service). The numbering was chosen at series introduction (commit 4866e5e, 2026-02-21) and there is no historical TR-MCP-WS-001.
- **TR-MCP-TODO-001** is intentionally absent; the TODO series begins at `TR-MCP-TODO-002`. Original TODO scaffolding TRs were folded into higher-numbered entries before the doc shipped.
- **TR-PLANNED-013** is intentionally represented only by `TR-PLANNED-013A` (SessionLog ProblemDetails Factory). The non-suffixed `TR-PLANNED-013` was absorbed by the `TR-MCP-DIR-*` series when Director requirements moved to `Requirements-Director.md` (commit 3029287, 2026-03-06).

The traceability validator must NOT treat these IDs as missing. Authors SHOULD continue numbering future entries from the next unused integer in the relevant series rather than backfilling.
