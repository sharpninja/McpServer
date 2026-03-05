# AUDIT-REQS-001 Report

Date: 2026-03-05

Scope:
- Requirements documents under `docs/Project/`
- As-built code under `src/` and `tests/`
- Recent session-log activity for requirement-impacting work

Method:
- Reviewed canonical requirements docs (`Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`, `Requirements-Matrix.md`)
- Validated implementation presence via targeted code scans
- Cross-checked recent session logs for feature-delivery drift
- Classified findings into redundancy, gap, completion drift, and compliance hotspots

## Executive Result

Audit completed. The core issue is not missing architecture coverage in many areas; it is traceability drift between requirements artifacts and as-built/project-state evidence. The DI/SSOT hotspot identified during this audit has now been remediated and validated.

## Findings

1) Requirements status drift (high)
- `Requirements-Matrix.md` marks `FR-MCP-046` as planned while `VoiceController` and `VoiceConversationService` are implemented in `src/McpServer.Support.Mcp/`.
- Multiple pool-related requirements (`FR-MCP-052` through `FR-MCP-058`) are marked complete in matrix, while corresponding FR/TR narratives in functional/technical docs still contain planned language in places, causing mixed signals.
- Recent session logs show completed delivery activity in these areas, reinforcing matrix/narrative drift.

2) Mapping and matrix coverage gaps (high)
- Inventory scan result: 63 FR headings, 61 FR mapping rows.
- Missing from FR-to-TR mapping: `FR-SUPPORT-010`, `FR-LOC-001`.
- Inventory scan result: 63 FR headings, with `FR-SUPPORT-010` missing from `Requirements-Matrix.md`.
- Matrix traceability is partially compressed through range rows (for example `TR-MCP-DATA-001–003`), which is human-readable but weak for machine-verifiable one-row-per-requirement audits.

3) Planned features with no implementation evidence (medium)
- No as-built class evidence found for:
  - `FR-MCP-033` / `TR-MCP-POL-001` (`PolicyManagementTool`)
  - `FR-MCP-036` / `TR-MCP-AUDIT-001` (`AuditedCopilotClient`)
  - `FR-MCP-039` / `TR-MCP-CTX-001` explicit implementation artifact
- These remain valid planned items but should be explicitly tied to backlog TODOs and target milestones.

4) DI/SSOT compliance hotspot remediated (closed)
- Runtime service-construction anti-patterns were removed from:
  - `Program.cs` (replaced provider selection with DI-owned `ITodoServiceFactory`)
  - `McpStdioHost.cs` (same replacement in STDIO composition root)
  - `TodoServiceResolver.cs` (replaced manual `new` construction with factory-owned workspace creation)
- Validation evidence: `dotnet build src/McpServer.Support.Mcp -c Debug --no-restore` and targeted resolver-dependent tests passed.
- `FR-MCP-059` / `TR-MCP-ARCH-002` remains traceability-relevant, but this specific TODO-service construction violation is resolved.

5) Test traceability partial (medium)
- `Requirements-Matrix.md` already flags `TEST-MCP-080` as partial.
- Evidence exists for event-stream integration tests, but non-matching-category negative-path completeness should be finished and linked directly in requirement traceability.

## Completion Assessment

- Audit execution checklist: completed.
- Requirements redundancy/gap/completion audit: completed.
- Code compliance hotspot identification: completed.
- Session-log cross-check: completed.
- Remediation implementation: DI/SSOT follow-up executed and validated for TODO service construction paths.

## Actionable Remediation Plan Summary

A dedicated follow-up TODO has been created to execute remediation from these findings. Priority order:
1. Reconcile requirement statuses across FR/TR narrative docs and matrix.
2. Close mapping/matrix structural traceability gaps (`FR-SUPPORT-010`, `FR-LOC-001`, explicit row strategy).
3. Track planned-but-unimplemented requirement IDs to explicit TODO delivery items.
4. Execute DI/SSOT remediation for TODO service creation paths.
5. Close TEST-MCP-080 partial coverage and link evidence in matrix.
