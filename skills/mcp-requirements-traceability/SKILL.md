---
name: mcp-requirements-traceability
description: Use this skill when adding, updating, mapping, or auditing MCP Server FR/TR/TEST requirements and acceptance criteria, or when making ./build.ps1 ValidateTraceability pass; it covers the workflow.requirements.* plugin methods, the requirement ID grammar, and the rule that every FR must map to at least one TR and one TEST and that all IDs appear in docs/Project/*.md.
license: MIT
---

# MCP Requirements Traceability

Manage MCP Server functional (FR), technical (TR), and test (TEST) requirements,
their FR -> TR/TEST mappings, and structured acceptance criteria. The requirements
database is the source of truth; the Markdown files under `docs/Project/` are
import/export projections that `./build.ps1 ValidateTraceability` reads. Keep both
in sync so the build stays green.

## When to Use

- Adding or editing FR / TR / TEST records, or their acceptance criteria.
- Linking an FR to its TRs and TESTs (the traceability mapping).
- Fixing a failing `./build.ps1 ValidateTraceability` run.
- Auditing coverage (which FR lacks a TR or a TEST).
- Regenerating the requirements Markdown / wiki projections from the database.

## When Not to Use

- Managing TODO items or session logs: use the `todo` and `session` skills instead.
  Requirement IDs are distinct from TODO IDs (`PLAN-AREA-###`) and Session IDs.
- Editing the requirement Markdown files in `docs/Project/` by hand. Never hand-edit
  `Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`,
  `TR-per-FR-Mapping.md`, or `Requirements-Matrix.md`. Mutate records through the
  plugin and regenerate the docs.

## Inputs

- The workspace marker / API key from `AGENTS-README-FIRST.yaml` (read once per session).
- A requirement `area` (and `subarea` for TR), plus `title`, `description`, `priority`.
- For mappings: an `frId` and the `trIds` / `testIds` to link.
- For validation: the five Markdown files under `F:\GitHub\McpServer\docs\Project\`.

## Critical Rules

- The database is authoritative. Markdown under `docs/Project/` is a generated
  projection. After mutating records, regenerate the docs with
  `workflow.requirements.generateDocument` so the IDs land in the files the
  validator scans. Do NOT hand-edit those files.
- ID grammar (uppercase, validated server-side):
  - FR: `FR-<AREA>-###` (e.g. `FR-MCP-001`). Area segment only.
  - TR: `TR-<AREA>-<SUBAREA>-###` (e.g. `TR-MCP-ARCH-001`). Both area and subarea required.
  - TEST: `TEST-<AREA>-###` (e.g. `TEST-MCP-001`). Area segment only.
- Coverage rule the validator and CI enforce: every FR should map to at least one TR
  and at least one TEST, and every FR/TR/TEST ID must be present in the matrix.
- All requirements work goes through the plugin `workflow.requirements.*` namespace
  (the Claude Code plugin wrapper, surfaced over REPL YAML/STDIO). Do not substitute
  raw REST or another agent's plugin for normal requirements work.
- Capture requirements as they emerge during a session; do not defer. Record the new
  IDs in the session log turn tags.

## How ValidateTraceability Enforces It

`./build.ps1 ValidateTraceability` runs the `ValidateTraceability` Nuke target
(`build/Build.ValidateTraceability.cs`), which calls `TraceabilityValidator.Validate`
(`build/TraceabilityValidator.cs`). It reads exactly five files under
`docs/Project/`: `Functional-Requirements.md`, `Technical-Requirements.md`,
`Testing-Requirements.md`, `TR-per-FR-Mapping.md`, and `Requirements-Matrix.md`.

It extracts IDs and reports four gaps:

- FR heading IDs missing from `TR-per-FR-Mapping.md` (`MissingFrInMapping`).
- FR heading IDs missing from `Requirements-Matrix.md` (`MissingFrInMatrix`).
- TR heading IDs missing from `Requirements-Matrix.md` (`MissingTrInMatrix`).
- TEST IDs missing from `Requirements-Matrix.md` (`MissingTestInMatrix`).

Failure semantics: FR gaps (either of the first two) ALWAYS fail the build.
TR and TEST gaps are warnings only, and fail the build solely when you pass
`--strict-tr-and-test-coverage` (the `StrictTrAndTestCoverage` parameter). FR IDs
must appear as `## FR-...` headings; TR IDs as `## TR-...` headings; TEST IDs are
matched anywhere in `Testing-Requirements.md`. The matrix supports range tokens
like `FR-MCP-001-003`, which expand to each ID in the inclusive range.

## Workflow

1. Read `AGENTS-README-FIRST.yaml` once per session for the key/endpoints, and
   ensure the session is bootstrapped per the workspace plugin before any
   state-changing call. Begin a session log turn before mutating requirements.
2. Discover existing state. List with `workflow.requirements.listFr`,
   `workflow.requirements.listTr`, `workflow.requirements.listTest`, filtering by
   `area` (and `subarea` for TR). Fetch one with `getFr` / `getTr` / `getTest`.
3. Create or update records. Use `workflow.requirements.createFr` (required params:
   `id`, `title`, `description`, `priority`, `area`; `notes` optional), `createTr`
   (also requires `subarea`), and `createTest`. Use `updateFr` / `updateTr` /
   `updateTest` to change `status` (`pending`, `in_progress`, `completed`,
   `deferred`) or `notes`. For many records, prefer the atomic batch methods
   `createFrBatch` / `updateFrBatch` / `createTrBatch` / `updateTrBatch` /
   `createTestBatch` / `updateTestBatch`, or the mixed `createBatch` / `updateBatch`
   (include `kind: fr|tr|test` per record). A batch fails entirely if any record is invalid.
4. Attach acceptance criteria. Pass an `acceptanceCriteria` array (each entry has
   `id`, `text`, `isSatisfied`, optional `evidence`) on create/update, or copy from a
   TODO with `workflow.requirements.copyAcceptanceCriteriaFromTodo`
   (`kind`, `id`, `todoId`).
5. Create the traceability mapping so coverage is satisfied. Call
   `workflow.requirements.createMapping` with `frId`, `trIds: [...]`,
   `testIds: [...]`, and optional `notes`. Ensure each new FR has at least one TR
   and one TEST linked. Remove a link with `workflow.requirements.deleteMapping`.
   Inspect with `workflow.requirements.listMappings` (optionally filtered by `frId`).
6. Regenerate the Markdown projections so the IDs reach the validator's files:
   call `workflow.requirements.generateDocument` with `format: markdown` and the
   relevant `docType` (`functional`, `technical`, `testing`, `matrix`, or `all`).
   Use `format: wiki` with `docType: all` to refresh `docs/Project/wiki/azure` and
   `docs/Project/wiki/github`.
7. Validate. Run `./build.ps1 ValidateTraceability` for FR-gap enforcement, or
   `./build.ps1 ValidateTraceability --strict-tr-and-test-coverage` to also fail on
   TR/TEST gaps. Resolve any reported missing IDs by creating the record/mapping and
   regenerating, then re-run.
8. Record the new requirement IDs in the session log turn (tags / requirementsDiscovered)
   via the session plugin, and complete the turn.

## Validation Checklist

- Every new FR is reachable as a `## FR-...` heading in `Functional-Requirements.md`
  and as a row in both `TR-per-FR-Mapping.md` and `Requirements-Matrix.md`.
- Every new TR is a `## TR-...` heading in `Technical-Requirements.md` and a row in
  `Requirements-Matrix.md`; its ID has area AND subarea segments.
- Every new TEST ID appears in `Testing-Requirements.md` and in `Requirements-Matrix.md`.
- Each FR maps to at least one TR and at least one TEST via `createMapping`.
- The Markdown was regenerated after the last database mutation (no manual edits).
- `./build.ps1 ValidateTraceability` reports "Traceability validation passed"
  (and passes under `--strict-tr-and-test-coverage` when full coverage is required).

## Common Pitfalls

- Creating records but forgetting to regenerate the docs: the database is correct yet
  the validator (which only reads Markdown) still fails. Always run
  `generateDocument` before `ValidateTraceability`.
- Hand-editing the `docs/Project/*.md` files: edits are overwritten on the next
  generate and drift from the database. Mutate via the plugin instead.
- Using a TR ID without a subarea (e.g. `TR-MCP-001`): the server rejects it with
  `invalid_requirement_id`. TR requires `TR-<AREA>-<SUBAREA>-###`.
- Assuming TR/TEST gaps fail the build by default: they are warnings unless
  `--strict-tr-and-test-coverage` is supplied. Missing FR mappings always fail.
- Creating a mapping that references a non-existent FR/TR/TEST: returns
  `invalid_mapping`. Create the FR, TR, and TEST records first, then map them.
- Confusing requirement IDs with TODO IDs (`PLAN-AREA-###`) or Session IDs. They live
  in separate namespaces and separate plugin methods.
- Re-creating an existing ID returns `requirement_already_exists`; use the matching
  `update*` method instead of `create*`.

## YAML Mutation Rule

When YAML must be changed, deserialize the complete document into an object, mutate the object, serialize the object, and save the result. Do not append YAML snippets, replace YAML lines, remove YAML lines, or build YAML payloads as strings. For PowerShell work, use `plugins/core/lib-ps/yaml-object-mutation.ps1` and call `Set-McpYamlObjectValue` or `Update-McpYamlObject`.
