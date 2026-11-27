# TR-MCP-HYGIENE

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-HYGIENE-001

**Validation registry and result contract** — IWorkspaceValidationService returns a DTO with the required fields. Unknown rule codes emit diagnostics. Clean workspace returns zero findings.
**Covered by:** FR: FR-MCP-HYGIENE-001; TEST: TEST-MCP-HYGIENE-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Result contract has required fields. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Clean workspace zero findings. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Unknown rule code diagnostic. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

## TR-MCP-HYGIENE-002

**Traceability rule implementations** — Each rule emits a finding with record id and evidence. TEST exclusion only if a documented rule says so.
**Covered by:** FR: FR-MCP-HYGIENE-002; TEST: TEST-MCP-HYGIENE-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Missing AC finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] TR with no FR orphan. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] FR missing TR or TEST orphan. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] TEST with no FR orphan. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Broken or duplicate mapping finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

## TR-MCP-HYGIENE-003

**TODO consistency rule implementations** — Each mismatch emits a finding with the TODO id and evidence of the contradicting fields.
**Covered by:** FR: FR-MCP-HYGIENE-003; TEST: TEST-MCP-HYGIENE-003
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Done true incomplete tasks. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Done false all tasks complete. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Done without doneSummary. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Remaining contradicts completion. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Missing dependency target. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Missing referenced requirement id. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

## TR-MCP-HYGIENE-004

**Stale-turn clock and triage domain enum** — IUtcClock is injectable. Threshold default 48h. Triage states come from the live domain model. Auth, cancellation, and pagination are enforced.
**Covered by:** FR: FR-MCP-HYGIENE-004; TEST: TEST-MCP-HYGIENE-004
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] In-progress turn older than 48h with injected clock. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Triage non-terminal uses live domain enum. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Override bounded authenticated recorded. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Auth required. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Cancellation honored. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Large workspace paginates. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

## TR-MCP-HYGIENE-005

**Hygiene surface adapters and Director exit codes** — Four surfaces call the same service. Director maps Error/Critical to exit 1 and Warning-only to exit 0. JSON always written. No repair methods.
**Covered by:** FR: FR-MCP-HYGIENE-005; TEST: TEST-MCP-HYGIENE-005
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Parity same rule codes and counts. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Director exit 1 when Error present. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Director exit 0 when Warning-only. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Validation never auto-repairs. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

