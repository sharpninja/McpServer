# TEST-TRIAGE

[Back to Testing Requirements](Testing-Requirements)

## TEST-TRIAGE

### TEST-TRIAGE-001

Unit and contract tests cover triage dashboard bucketing data, group/report/result rendering inputs, empty/error states, workspace filtering, and typed client dispatch for Director and MCP Web consumers.

**Acceptance Criteria:**
- [ ] Service tests cover dashboard queue composition, run history/result mapping, workspace isolation, and empty data.
- [ ] Controller tests cover dashboard, run query, run detail, and not-found/error envelopes.
- [ ] Client tests cover new typed triage methods and query-string dispatch.

### TEST-TRIAGE-002

Unit and client tests cover triage-created TODO listing, workspace filtering, missing TODO anchors, group/run context mapping, and typed client dispatch.

**Acceptance Criteria:**
- [x] Service tests verify TODO ID and CreatedAtUtc values come from TodoRecordEntity and remain workspace-scoped. (evidence: TriageServiceTests.QueryCreatedTodosAsync_ReturnsTodoIdsCreatedAtUtcAndTriageContext)
- [x] Controller tests verify the read-only endpoint returns the service result. (evidence: TriageControllerTests.QueryCreatedTodosAsync_ReturnsCreatedTodoIndex)
- [x] Client tests verify the typed triage TODO method calls the expected URL with workspace filters. (evidence: TriageClientTests.QueryCreatedTodosAsync_SendsWorkspaceFilter)
