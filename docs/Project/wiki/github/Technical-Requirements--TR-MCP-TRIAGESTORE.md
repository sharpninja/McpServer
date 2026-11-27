# TR-MCP-TRIAGESTORE

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-TRIAGESTORE-001

**Session-log merge tags replace supersede** — SessionLogService SameAction identity is order plus type plus filePath plus description. SessionLogEntity stores session tags. ReplaceTurn missing requestId is 404. Plugin superseded persist stamps planFile and todoId None and uses UpsertTurn. Canceled status persists and is documented.
**Covered by:** FR: FR-MCP-TRIAGESTORE-001; TEST: TEST-MCP-TRIAGESTORE-001, TEST-MCP-TRIAGESTORE-002, TEST-MCP-TRIAGESTORE-003, TEST-MCP-TRIAGESTORE-004, TEST-MCP-TRIAGESTORE-005, TEST-MCP-TRIAGESTORE-006, TEST-MCP-TRIAGESTORE-007
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] SessionLogService SameAction identity is order plus type plus filePath plus description. SessionLogEntity stores session tags. ReplaceTurn missing requestId is 404. Plugin superseded persist stamps planFile and todoId None and uses UpsertTurn. Canceled status persists and is documented.

## TR-MCP-TRIAGESTORE-002

**Five second intake and submit storage budget** — TriageService.SubmitReportAsync and session-log SaveChanges used by beginTurn persist use a 5 second connect and command budget. Timeouts map to backend_unavailable. No partial triage rows.
**Covered by:** FR: FR-MCP-TRIAGESTORE-002; TEST: TEST-MCP-TRIAGESTORE-007
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] TriageService.SubmitReportAsync and session-log SaveChanges used by beginTurn persist use a 5 second connect and command budget. Timeouts map to backend_unavailable. No partial triage rows.

