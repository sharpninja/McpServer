# FR-TRIAGE

[Back to Functional Requirements](Functional-Requirements)

## FR-TRIAGE-001 Triage dashboard inspection

Users can inspect triage queue contents, grouped report queues, and AI triage run history with result details and current status for Director and MCP Web UI dashboards.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A read-only dashboard contract exposes triage queue, report group queue, and run history without inferring Agent Pool data.
- [ ] Group details include status, title, summary, report count, quiet deadline, created TODO id, last error, linked report summaries, and latest AI run results when available.
- [ ] The API supports workspace-scoped queries so Director and MCP Web can use the active workspace path.

## FR-TRIAGE-002 Triage-created TODO index

Users can query TODO item IDs created by triage, including the datetime each TODO anchor was created, so Director and MCP Web can link triage outcomes to backlog items.
Scope: layer-1+
**Acceptance Criteria:**
- [x] A read-only triage endpoint returns TODO IDs produced by triage and the TODO creation datetime. (evidence: TriageServiceTests.QueryCreatedTodosAsync_ReturnsTodoIdsCreatedAtUtcAndTriageContext)
- [x] The endpoint supports workspace-scoped queries and does not leak TODO IDs across workspaces. (evidence: TriageServiceTests.QueryCreatedTodosAsync_ReturnsTodoIdsCreatedAtUtcAndTriageContext)
- [x] The endpoint includes enough triage context to connect each TODO ID back to its group and research run when available. (evidence: TriageServiceTests.QueryCreatedTodosAsync_ReturnsTodoIdsCreatedAtUtcAndTriageContext)

