# FR-MCP-HOSTILEREVIEW

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-HOSTILEREVIEW-001 Enqueue bounded hostile review request

An authenticated agent can enqueue a bounded hostile review request scoped to the active workspace. The request includes target type, mode, scope statement, artifact links, requester identity, workspace identity, and optional acceptance criteria. Oversized payloads are rejected with no queue row. Foreign workspace is 403.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Request includes target type, mode, scope statement, artifact links, requester identity, workspace identity, optional AC list. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Serialized request JSON greater than 1048576 bytes is rejected with no queue row. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Authenticated workspace scope: foreign workspace is 403 and no queue row. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Valid request creates a QueueItem with stable id and status queued. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## FR-MCP-HOSTILEREVIEW-002 Artifact links resolve through MCP APIs

Artifact links resolve only through supported MCP APIs (TODO, FR/TR/TEST, session log, triage, plans, files, commits, generated docs). Missing, unauthorized, stale, or ambiguous links produce diagnostics, not silent omission.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Missing link produces a diagnostic, not silent omit. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Unauthorized or stale or ambiguous link produces a diagnostic, not silent omit. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Successful resolve stores the resolved artifact identity and hash when available. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Authorize each artifact at submit and reauthorize it at every dispatch. Resolve only through supported workspace-scoped services. Store only normalized identity, type, version/hash, and sanitized diagnostic metadata. Permit no raw artifact or assembled prompt in queue rows, notifications, logs, receipts, or result metadata. Reject more than 64 links, an artifact over 1 MiB decoded, aggregate resolved input over 8 MiB, or unsupported/binary content that cannot be inspected safely. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## FR-MCP-HOSTILEREVIEW-003 Reviewer run records model effort identity

Each reviewer execution records model, effort, agent identity, plugin/source, prompt template/version, run id, timestamps, and token/runtime metadata when available. Missing metadata is omitted with a diagnostic, never fabricated.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Execution row records model string, effort level, agent identity, plugin/source surface. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Execution row records prompt template id and version, run id, start/end UTC. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Token/runtime metadata stored when provided; omitted with diagnostic when not provided, not fabricated. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] A server-owned worker uses the existing agent infrastructure with effective model gpt-6-astra and effort xhigh. Queue state is durable. Request, attempt, pool-job, model-execution, and accepted-terminal-result identities are distinct. Model execution is bounded at-least-once across process loss. Lease loss, repeated model execution, or duplicate completion cannot produce more than one accepted terminal result. Cancellation is internal to host/workspace lifecycle handling and existing authorized AgentPool cancellation, never a public hostile-review mutation. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## FR-MCP-HOSTILEREVIEW-004 Normalized findings and request quality

Normalized findings distinguish defects, risks, missing tests, unclear requirements, insufficient disclosure, out-of-scope, and uncertainty. Verdicts are AGREE, DISAGREE, or UNKNOWN. Request-quality scores disclosure, scope clarity, objective clarity, confidence, and enough-context.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Findings distinguish defects, risks, missing tests, unclear requirements, insufficient disclosure, out-of-scope, uncertainty. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Verdicts are AGREE, DISAGREE, or UNKNOWN only. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Request-quality scores disclosure, scope clarity, objective clarity, confidence, enough-context as explicit fields. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## FR-MCP-HOSTILEREVIEW-005 Query failure modes by dimensions

Query reports filter by workspace, model, effort, requesting agent, reviewer agent, target type, artifact type, severity, and category. Combined filters AND. Empty match is empty list.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Query with each dimension returns only matching runs. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Combined filters AND. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Empty match is empty list, not error. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

## FR-MCP-HOSTILEREVIEW-006 Default queue-and-record no auto mutation

Default behavior is queue-and-record. Completing a review does not edit product files, requirements, or TODOs. Surfaces expose submit/status/get/query only.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Completing a review does not edit product files, requirements, or TODOs. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] REST, REPL, Director, plugin skill expose submit/status/get/query only. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] No hidden repair/apply endpoint is registered in this TODO. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] status, get, and query are read-only. submit is the only public mutation. No cancel, repair, apply, file edit, TODO edit, or requirement edit behavior is hidden in any read operation. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 rereview SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

