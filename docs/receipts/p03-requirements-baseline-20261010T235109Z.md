<!-- SPDX-License-Identifier: Apache-2.0 -->
# P0.3 requirements baseline (20261010T235109Z)

PLAN-SESSIONTURNREVIEW-001 step 3, recorded as its PlanningDecision. Worktree .claude/worktrees/session-20261009 at ab86fcb6; live store of the main workspace (/home/sharpninja/github/McpServer) read through the plugin wrapper (workflow.requirements.getFr/getTr/getTest).

## Id families are free

All 69 ids that P0.4 creates (FR 21, TR 25, TEST 23; the catalog's 21/25/23) returned not_found; none was already present and no response was unexpected. Each matches the createBatch id grammar (three-digit suffix).

Ids: FR-MCP-SESSIONCLASS-001, FR-MCP-SESSIONCLASS-002, FR-MCP-SESSIONCLASS-003, FR-MCP-SESSIONCLASS-004, FR-MCP-SESSIONREVIEW-001, FR-MCP-SESSIONREVIEW-002, FR-MCP-SESSIONREVIEW-003, FR-MCP-SESSIONREVIEW-004, FR-MCP-SESSIONREVIEW-005, FR-MCP-SESSIONREVIEW-006, FR-MCP-SESSIONREVIEW-007, FR-MCP-SESSIONREVIEW-008, FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-002, FR-MCP-SESSIONTURN-003, FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-005, FR-MCP-SESSIONTURN-006, FR-MCP-SESSIONTURN-007, FR-MCP-SESSIONTURN-008, TEST-MCP-SESSIONCLASS-001, TEST-MCP-SESSIONCLASS-002, TEST-MCP-SESSIONCLASS-003, TEST-MCP-SESSIONCLASS-004, TEST-MCP-SESSIONREVIEW-001, TEST-MCP-SESSIONREVIEW-002, TEST-MCP-SESSIONREVIEW-003, TEST-MCP-SESSIONREVIEW-004, TEST-MCP-SESSIONREVIEW-005, TEST-MCP-SESSIONREVIEW-006, TEST-MCP-SESSIONREVIEW-007, TEST-MCP-SESSIONREVIEW-008, TEST-MCP-SESSIONREVIEW-009, TEST-MCP-SESSIONTURN-001, TEST-MCP-SESSIONTURN-002, TEST-MCP-SESSIONTURN-003, TEST-MCP-SESSIONTURN-004, TEST-MCP-SESSIONTURN-005, TEST-MCP-SESSIONTURN-006, TEST-MCP-SESSIONTURN-007, TEST-MCP-SESSIONTURN-008, TEST-MCP-SESSIONTURN-009, TR-MCP-SESSIONCLASS-001, TR-MCP-SESSIONCLASS-002, TR-MCP-SESSIONCLASS-003, TR-MCP-SESSIONCLASS-004, TR-MCP-SESSIONREVIEW-001, TR-MCP-SESSIONREVIEW-002, TR-MCP-SESSIONREVIEW-003, TR-MCP-SESSIONREVIEW-004, TR-MCP-SESSIONREVIEW-005, TR-MCP-SESSIONREVIEW-006, TR-MCP-SESSIONREVIEW-007, TR-MCP-SESSIONREVIEW-008, TR-MCP-SESSIONREVIEW-009, TR-MCP-SESSIONTURN-001, TR-MCP-SESSIONTURN-002, TR-MCP-SESSIONTURN-003, TR-MCP-SESSIONTURN-004, TR-MCP-SESSIONTURN-005, TR-MCP-SESSIONTURN-006, TR-MCP-SESSIONTURN-007, TR-MCP-SESSIONTURN-008, TR-MCP-SESSIONTURN-009, FR-SUPPORT-016, TR-SUPPORT-CORE-016, TEST-SUPPORT-024, TR-MCP-NUKE-003, TR-MCP-NUKE-004.

## ValidateTraceability baseline (before P0.4 creates anything)

- `./build.ps1 ValidateTraceability`: UseCaseFrLinks coverage source: none (no SQLite path resolved); Traceability validation passed.; ValidateTraceability    Succeeded     < 1sec.
- `./build.ps1 ValidateTraceability --strict-tr-and-test-coverage`: UseCaseFrLinks coverage source: none (no SQLite path resolved); Traceability validation passed.; ValidateTraceability    Succeeded     < 1sec.

Baseline findings: none in either mode (the validator reports "passed" without the coverage-warnings variant). UseCaseFrLinks coverage was not evaluated here because no SQLite path resolves for the worktree; P0.9 judges use-case coverage with client.UseCases.GetCoverageAsync against the live store.
