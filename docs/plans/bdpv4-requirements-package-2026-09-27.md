# BDPv4 requirements package

Date: 2026-09-27

This pass organizes `docs/Project/` so Payton's rule holds in the requirement documents: every functional requirement has a use case, acceptance criteria, and a `TEST-MCP-*` link. Technical requirements that lacked the same structure were filled from their own text. New narratives are Unverified. Nothing in this pass is marked Complete unless that claim was already in the source.

## Counts

- Functional requirements: 296.
- Use cases before: 0. Use cases added: 296.
- Acceptance criteria before: 123. Acceptance criteria added: 173. After: 296.
- Mapping rows that already cited `TEST-MCP-*`: 181. Rows that gained a `TEST-MCP-*` link: 115. After: 296.
- New `TEST-MCP-BDP-*` records: 63. Status on each new record: Planned. Evidence: Unverified.
- Technical requirement headings that gained a use case: 425 (this count includes the existing duplicate heading `TR-MCP-AGENT-PARITY-020`).
- Technical requirements that gained acceptance criteria: 320.
- Unmapped technical requirements that received their own Planned test record: 19. The numeric stubs `TR-01` through `TR-14` share `TEST-MCP-BDP-PLACEHOLDER`.

Where an existing `TEST-MCP-*` entry already stated the scenario, the mapping now points at that entry and no second test was invented. Legacy `TEST-HANDOFF-*`, `TEST-SUPPORT-*`, and `TEST-TRIAGE-*` ids stay on the mapping row next to the new `TEST-MCP-*` id.

## Gaps closed

- Every `## FR-*` section now has `Use Cases`, acceptance criteria, and `Test Requirements` citing at least one `TEST-MCP-*` id.
- Every `## TR-*` section now has the same three fields. Test links are inherited from the parent functional requirement when the mapping names that technical requirement.
- `TR-per-FR-Mapping.md` no longer leaves a functional requirement on `*(Planned)*` with no test id.
- `Requirements-Matrix.md` has a Planned row for each new test id.
- Wiki copies of the functional, technical, and mapping documents were updated because they matched the canonical files. The divergent testing wiki received only the new appendix. The matrix wiki received the new rows.

## Left in place

- Intentional numbering gaps: `TR-MCP-WS-001`, `TR-MCP-TODO-001`, and unsuffixed `TR-PLANNED-CORE-013`. They were not backfilled.
- `docs/Project/TODO.yaml` was not edited.
- Requirements that already had acceptance criteria were not rewritten. The use case and test link were appended.
- Checked acceptance criteria that already cited evidence were left checked. Criteria added in this pass are unchecked.
- The duplicate heading `TR-MCP-AGENT-PARITY-020` was not renumbered.
- The matrix row whose id cell is `[]` was not rewritten.

## Validation

`./build.ps1 ValidateTraceability` was not run. This environment does not have `pwsh` or `dotnet`, so the Nuke target could not start.

The same document checks implemented by `scripts/Validate-RequirementsTraceability.ps1` and `build/TraceabilityValidator.cs` were run directly:

- Missing FR in `TR-per-FR-Mapping.md`: 0
- Missing FR in `Requirements-Matrix.md`: 0
- Missing TR in `Requirements-Matrix.md`: 0
- Missing TEST in `Requirements-Matrix.md`: 0

`ValidateTraceability` also reports UseCaseFrLinks Realizes findings when a workspace SQLite database is present. No database was available here, so that warning path was not executed. Those findings do not fail the default target.

## Remaining

- The 63 new test records still need acceptance tests written and executed. Until that happens their status stays Planned.
- Linked older tests were not re-run in this pass. A use-case narrative added on 2026-09-27 is not execution evidence.
- Some technical requirements are covered by a parent functional test rather than a test that names the technical id alone.
- Runtime use-case rows (`FR-MCP-USECASE-*`) are specified in the documents. This pass did not create database Realizes links.
