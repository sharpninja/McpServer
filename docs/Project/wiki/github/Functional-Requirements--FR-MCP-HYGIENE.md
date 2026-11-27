# FR-MCP-HYGIENE

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-HYGIENE-001 Read-only workspace validation service

A read-only workspace validation service uses a versioned rule registry and a shared result contract with rule code/version, severity, entity kind, workspace identity, record IDs, evidence, remediation, run timestamp, duration, and summary counts.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Result contract has rule code/version, severity, entity kind, workspace identity, record IDs, evidence, remediation, run timestamp, duration, summary counts. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Clean workspace produces zero findings. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Registry is versioned; unknown rule code is not silently skipped. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

## FR-MCP-HYGIENE-002 Traceability and missing-AC rules

Rules identify FR/TR/TEST with no AC rows, TR with no FR, FR lacking TR/TEST, TEST with no FR, and broken or duplicate mappings.
Scope: layer-1+
**Acceptance Criteria:**
- [x] FR/TR (and TEST unless excluded by a documented rule) with no AC rows is a finding with record id and evidence. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] TR mapped to no FR is a finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] FR lacking required TR or TEST coverage is a finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] TEST mapped to no FR is a finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Broken or duplicate mappings are findings. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

## FR-MCP-HYGIENE-003 TODO consistency rules

Rules identify done=true with incomplete tasks, done=false with all tasks complete, completion without doneSummary, remaining vs completion contradiction, missing dependency targets, and missing referenced requirement IDs.
Scope: layer-1+
**Acceptance Criteria:**
- [x] done=true with any incomplete implementation task is a finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] done=false with all implementation tasks complete is a finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] done=true without doneSummary is a finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] remaining text that contradicts completion is a finding. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] missing dependency target ids are findings. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] missing referenced requirement IDs are findings. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

## FR-MCP-HYGIENE-004 Stale turns and non-terminal triage

Turns in_progress more than 48h after authoritative timestamp (injectable UTC clock) are findings. Triage reports in live non-terminal states are findings. Override threshold is positive, at most 168h, authenticated, recorded.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Turns in_progress more than 48h after the authoritative timestamp (injectable UTC clock) are findings. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Triage reports in live non-terminal states from the domain model are findings; processing failure is distinct from pending. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Threshold override must be positive, at most 168h, authenticated, and recorded on the run. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Unauthenticated validate is rejected. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Cancellation is honored. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Large workspaces paginate rather than unbounded load. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

## FR-MCP-HYGIENE-005 Surface parity and no auto-repair

REST, Director, REPL, and plugin skill expose the same validation. Same rule codes, identities, severities, counts, and threshold. Never auto-repair. Director exit 1 on Error or Critical; exit 0 when empty or Warning-only.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Authenticated REST, Director command, REPL command, and plugin skill expose the same validation. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Parity: same rule codes, identities, severities, counts, and threshold. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Validation never auto-repairs or deletes records. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Director exit 1 when any finding has severity Error or Critical. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)
- [x] Director exit 0 when findings are empty or Warning-only; JSON is always written. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16 Passed 25 Failed 0 Skipped 0)

