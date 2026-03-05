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
