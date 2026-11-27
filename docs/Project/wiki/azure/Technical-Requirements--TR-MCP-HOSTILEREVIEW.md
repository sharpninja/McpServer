# TR-MCP-HOSTILEREVIEW

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-HOSTILEREVIEW-001

**Hostile review request schema and storage** — Persist ReviewRequest and QueueItem in EF with three-provider migrations. Reject serialized request JSON over 1048576 bytes before insert. Enforce workspace isolation on submit.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-001; TEST: TEST-MCP-HOSTILEREVIEW-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] HostileReviewEntity round-trips on Sqlite, PostgreSQL, and SQL Server. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Valid submit creates a queued row with stable id. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Payload over 1048576 bytes is rejected with no queue row. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Foreign workspace is 403 with no row. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## TR-MCP-HOSTILEREVIEW-002

**Hostile review artifact resolver** — Resolver calls supported MCP APIs only. Diagnostics are stored on the request. Successful resolve stores identity and hash when available.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-002; TEST: TEST-MCP-HOSTILEREVIEW-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Missing link returns a diagnostic and does not omit silently. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Stale or unauthorized link returns a diagnostic. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Ambiguous link returns a diagnostic. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Add AgentPoolOneShotContext.HostileReview. Extend OneShotSensitivePromptPolicy to protect both Handoff and hostile review. Hold raw resolved text only in a transient dispatch buffer and release it after the one-shot submission. Hash and delimit every artifact as untrusted input. Fail closed on changed hash, revoked access, workspace mismatch, secret-detection timeout, or unsafe content. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## TR-MCP-HOSTILEREVIEW-003

**Hostile review execution capture** — ReviewExecution entity stores model string, effort, agent, plugin/source, template id/version, run id, start/end UTC. Token counts are optional and must not be invented.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-003; TEST: TEST-MCP-HOSTILEREVIEW-003
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Execution row records model, effort, agent, template, run id. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Missing token counts are omitted, not fabricated. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Implement the locked worker state machine and defaults. Use atomic compare-and-swap claim and fencing. Use a process-local attempt-to-pool-job correlation around the existing in-memory IAgentPoolService. Do not claim durable or exactly-once pool admission. Reuse a known live pool job only within the same process. After restart, truthfully orphan an expired nonterminal attempt and create a new fenced attempt only within the retry bound. Clean up process-local correlations and terminal pool jobs. Retry only classified transient failures. Do not replay terminal, cancelled, exhausted, unauthorized, or malformed requests. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## TR-MCP-HOSTILEREVIEW-004

**Findings taxonomy and request-quality schema** — Finding.category is an enum of the seven classes. Verdict is AGREE|DISAGREE|UNKNOWN. RequestQualityAssessment has five explicit numeric or ordinal fields.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-004; TEST: TEST-MCP-HOSTILEREVIEW-004
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Get returns taxonomy-complete findings. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Request-quality scores the five dimensions. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## TR-MCP-HOSTILEREVIEW-005

**Hostile review query indexes** — Query API applies AND across provided dimensions and returns empty list on no match, not an error.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-005; TEST: TEST-MCP-HOSTILEREVIEW-005
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Query by model and effort returns only matching runs. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Requester and target type AND. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] No match is empty list. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## TR-MCP-HOSTILEREVIEW-006

**No auto-mutation and surface parity** — No apply/repair endpoint is registered. REST, REPL, Director, and plugin skill expose the four verbs only and do not write product files from verdicts.
**Covered by:** FR: FR-MCP-HOSTILEREVIEW-006; TEST: TEST-MCP-HOSTILEREVIEW-006
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Default complete does not mutate product files. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] REST REPL Director plugin skill parity for submit/status/get/query. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
