# TR-MCP-NUKE

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-NUKE-001

**Non-interactive PowerShell hosts for Nuke automation** — The root Nuke PowerShell bootstrap and any build-owned pwsh.exe or powershell.exe child process SHALL include -NoLogo, -NoProfile, and -NonInteractive unless an invocation is explicitly documented as interactive. Live deployment guidance SHALL use the same flags.
**Covered by:** FR: FR-MCP-110, FR-MCP-BUILD-001; TEST: TEST-MCP-148
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-002

**TR-MCP-NUKE-002** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-002.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-005

**TR-MCP-NUKE-005** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-005.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-006

**TR-MCP-NUKE-006** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-006.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-007

**TR-MCP-NUKE-007** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-007.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-008

**TR-MCP-NUKE-008** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-008.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-009

**TR-MCP-NUKE-009** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-009.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-010

**TR-MCP-NUKE-010** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-010.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-011

**TR-MCP-NUKE-011** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-011.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-012

**TR-MCP-NUKE-012** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-012.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-015

**TR-MCP-NUKE-015** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-015.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-016

**TR-MCP-NUKE-016** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-016.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-017

**TR-MCP-NUKE-017** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-017.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-018

**TR-MCP-NUKE-018** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-018.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-019

**TR-MCP-NUKE-019** — Placeholder requirement backfilled for TODO link TR-MCP-NUKE-019.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+

## TR-MCP-NUKE-020

**Targeted test-group runs** — The Nuke target TestGroups runs only named groups of test classes, each group in one test project, and writes one TRX per project, so that a mock gate or binding after an edit runs the changed tests through Nuke instead of the full suite. The full gate set (Test, MigrationIntegrationTests, the build's infrastructure tests, ValidateTraceability) still runs once at phase exit.
**Covered by:** FR: FR-MCP-BUILD-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Nuke target TestGroups runs only the test groups its --test-groups array names. Each group names one test project and one or more test classes, selected by class name whatever their category. The target requires --test-run-id and writes one TRX per selected project under TestResults/<run id>. It fails when a group names an unknown project, when a group selects no case, or when any selected case fails or is skipped.
- [x] The group filters and TRX names come from the same composition MigrationIntegrationTests uses (TR-MCP-NUKE-019 AC-3), proved by the build's infrastructure tests over that composition. The run log names each filter it ran.
- [x] TestGroups never changes which cases Test or MigrationIntegrationTests run.
- [x] TestGroups excludes cases tagged for the other platform with the lane's host platform clause: on a Linux host Platform!=Windows, on a Windows host Platform!=Linux, untagged cases on both (TR-MCP-NUKE-019 AC-1). A group whose classes hold only other-platform cases selects no case and fails (operator 2026-11-02: "Add both ACs (Recommended)").
- [x] Every class a group names runs at least one case in its project's TRX; a named class that runs no case fails the target and the failure names that class (operator 2026-11-02: "Add both ACs (Recommended)"). A class counts as run only when an executed result's test, joined to its definition by test id, belongs to that class (its definition's class name is the class, as the whole name or its last segment). A definition with no executed result, a namespace segment, and a display name or argument that mentions the class do not count; a custom display name counts through its test's class (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)"; hostile validation finding R9-F1).

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
