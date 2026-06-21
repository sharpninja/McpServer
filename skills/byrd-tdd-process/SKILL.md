---
name: byrd-tdd-process
description: Use this skill when implementing any feature, fix, or change in the McpServer codebase so that work follows the Byrd Development Process v4 (acceptance unit tests first / Red, validate against mocks until green, implement / Green, refactor, keep all prior and current tests passing to exit a phase, integration tests after units pass, requirements drive tests, and every new FR/TR/TEST id is referenced in source doc-comments and validated by ./build.ps1 ValidateTraceability).
license: MIT
---

# Byrd Development Process v4 (TDD)

The canonical reference is `docs/Development-Process-draft-v4.md`. This process aligns with Martin Fowler's Red -> Green -> Refactor TDD cycle, augmented with Byrd-specific AI-safety gates (mocks-first validation, full-suite regression gates, and requirement traceability).

## When to Use

- Implementing any new functional or technical requirement in this repo.
- Fixing a bug, changing behavior, or adding to a public surface (types, methods, controllers, services).
- Writing or updating any implementation plan. Per the global rule, no plan is exempt regardless of size, urgency, or scope.

## When Not to Use

- Pure documentation edits, requirements wording, or session-log housekeeping that touch no code and no test.
- Mechanical, behavior-preserving renames or formatting where existing tests already fully cover the surface.
- Read-only investigation, diagnosis, or research where you produce no source change.

## Inputs

- The Functional Requirement(s) being satisfied: `FR-MCP-*` from `docs/Project/Functional-Requirements.md`.
- The Technical Requirement(s): `TR-MCP-*` from `docs/Project/Technical-Requirements.md`.
- The Testing Requirement(s): `TEST-MCP-*` from `docs/Project/Testing-Requirements.md`.
- The active iteration / slice scope (which tests must be green to exit) from MCP TODO state.
- The target test project (for example `tests/McpServer.Support.Mcp.Tests`) and the production project under change.

## Critical Rules

- Requirements drive tests. Tests are derived from functional and technical requirements and their acceptance criteria, never from implementation details. If a requirement is missing, capture it first (see Requirements step) before writing the test.
- Tests come first (Red). Write acceptance unit tests covering the full acceptance criteria BEFORE writing implementation code. The test must fail for the right reason before you implement.
- Validate with mocks (Byrd augmentation). Make the new tests pass against mocks/stubs first, proving the test and the contract are correct, before wiring real logic.
- Then implement (Green). Only after the mock-backed tests are correct do you write the real production code that makes them pass without mocks. Refactor tests and production code as part of the cycle to keep the design clean.
- All tests green to exit a phase. The entire unit-test suite for the current increment AND all prior work must pass before leaving an Implementation slice. Per `AGENTS.md` (Byrd Test Gate), skipped tests are not passing tests: the gate requires zero failures and zero skips in the executed scope. Deferred work belongs in MCP TODO/requirements state, never in a skipped-test placeholder.
- Integration tests come after units. Implement integration tests only after all unit tests pass across the codebase. The Nuke `Test` target excludes `*.IntegrationTests`; run those separately.
- Traceability is mandatory. Every new public type and member needs XML doc comments (`TreatWarningsAsErrors` + CS1591 fail the build), and every new `FR/TR/TEST` id must be referenced in source/test doc-comments and validated by `./build.ps1 ValidateTraceability`.
- Do not ship code you have not verified compiles and passes. Correctness over speed.
- Use `pwsh.exe` (PowerShell 7+) for all scripts. No em-dashes in any output, code comment, or commit message.
- Defective requirements are expected. If writing the test surfaces a paradox, ambiguity, or wrong rule, refine the requirement (and its FR/TR/TEST docs) rather than weakening the test.

## Workflow

1. Identify scope and requirements. Confirm the `FR-MCP-*`, `TR-MCP-*`, and `TEST-MCP-*` ids this slice satisfies. If any is missing or wrong, capture or correct it: append entries to `docs/Project/Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, plus `TR-per-FR-Mapping.md` and `Requirements-Matrix.md`. Do not edit `docs/Project/TODO.yaml` directly; route TODO operations through the MCP plugin tools (`mcp_todo_*`) and log a session-log turn via the plugin (`workflow.sessionlog.beginTurn`) before starting work.
2. Write acceptance unit tests first (Red). In the appropriate `tests/*.Tests` project, write tests that cover the acceptance criteria for the next small increment of behavior. Each test class and method needs XML docs stating what is tested, the data/fixtures used, and the requirement ids validated (for example `/// <summary>TEST-MCP-042: ...</summary>`).
3. Validate against mocks (Byrd gate). Using the stack's mocking tools, make the new tests pass with mocks/stubs only. Run the targeted tests, for example:
   `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~YourNewTests"`
   Confirm the tests fail without the contract and pass with the mock, proving they assert the right behavior.
4. Implement real logic (Green). Write the minimum production code that makes the mock-backed tests pass against real implementations. Reference the requirement ids in production doc-comments (for example `/// <summary>TR-MCP-058: ...</summary>`).
5. Refactor. Clean up both tests and production code while keeping them green. Remove duplication, improve names, and tighten the design (DRY, SOLID, existing conventions).
6. Run the full unit suite (exit gate). Run `./build.ps1 Test` (Nuke, excludes `*.IntegrationTests`). The current increment plus all prior unit tests must be green with zero failures and zero skips before leaving the slice. If a prior test breaks, fix it now; that is regression detection working as intended.
7. Integration tests after units pass. Once all unit tests are green across the codebase, add/run integration tests, for example:
   `dotnet test tests/McpServer.Support.Mcp.IntegrationTests -c Debug`
8. Validate config and traceability. Run `./build.ps1 ValidateConfig` and `./build.ps1 ValidateTraceability` to confirm appsettings validity and that every `FR/TR/TEST` id maps correctly across requirements docs and source doc-comments.
9. Record and close. Update the session-log turn through the plugin (`workflow.sessionlog.appendActions` / `workflow.sessionlog.completeTurn`) with interpretation, status, actions (type/status/filePath), filesModified, designDecisions, and requirementsDiscovered. Commit only when asked; log commits as actions of type `commit`.

## Validation Checklist

- New unit tests existed and failed (Red) before implementation was written.
- New tests passed against mocks/stubs before real logic was wired.
- Production code now makes the tests pass without mocks (Green); both were refactored clean.
- `./build.ps1 Test` is fully green: zero failures and zero skips in the current plus prior scope.
- Integration tests were added/run only after unit tests passed, via the `*.IntegrationTests` projects.
- Every new public type/member has XML doc comments (no CS1591 build break).
- Every new `FR-MCP-*` / `TR-MCP-*` / `TEST-MCP-*` id is referenced in source/test doc-comments.
- `./build.ps1 ValidateConfig` and `./build.ps1 ValidateTraceability` both pass.
- Session-log turn updated through the plugin with actions, files modified, and decisions; no direct edits to `TODO.yaml` or session-log files.

## Common Pitfalls

- Writing implementation before the test (skips Red): you lose proof the test asserts the right behavior. Always start failing.
- Skipping the mocks-first gate: jumping straight to real logic removes the Byrd AI-safety check that the contract and test are correct.
- Marking a stubborn test `[Skip]`/`[Ignore]` to "keep moving": this violates the Byrd Test Gate. Skips are not passes. Move deferred work into MCP TODO/requirements state instead.
- Deriving tests from implementation details rather than from FR/TR acceptance criteria, which makes tests brittle and circular.
- Forgetting XML docs on new public surface, breaking the build via CS1591 (`TreatWarningsAsErrors`).
- Adding a new requirement id in code without updating the requirements docs, so `ValidateTraceability` fails.
- Running integration tests before the unit suite is green, hiding unit-level regressions behind integration noise.
- Editing `docs/Project/TODO.yaml` or session-log files directly instead of routing through the MCP plugin tools.
- Treating a requirement defect surfaced by a test as a test problem and weakening the assertion, instead of refining the requirement.
