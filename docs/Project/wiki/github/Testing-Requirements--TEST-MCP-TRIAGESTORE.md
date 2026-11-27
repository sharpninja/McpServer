# TEST-MCP-TRIAGESTORE

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-TRIAGESTORE

### TEST-MCP-TRIAGESTORE-001

SessionLogService tests cover identical actions no duplicate, session tags round-trip, replace missing requestId not found, canceled status round-trip, and superseded persist with None sentinels without 500.

**Acceptance Criteria:**
- [ ] SessionLogService tests cover identical actions no duplicate, session tags round-trip, replace missing requestId not found, canceled status round-trip, and superseded persist with None sentinels without 500.

### TEST-MCP-TRIAGESTORE-002

TriageService tests prove unreachable SQL fails within about 5 seconds with storage-unavailable, health liveness unchanged in contract, and no partial rows.

**Acceptance Criteria:**
- [ ] TriageService tests prove unreachable SQL fails within about 5 seconds with storage-unavailable, health liveness unchanged in contract, and no partial rows.

### TEST-MCP-TRIAGESTORE-003

ReplaceTurn of a missing requestId throws KeyNotFoundException / classified 404. It does not upsert a new turn and does not emit an untyped EF save.

**Acceptance Criteria:**
- [ ] ReplaceTurn of a missing requestId throws KeyNotFoundException / classified 404. It does not upsert a new turn and does not emit an untyped EF save.

### TEST-MCP-TRIAGESTORE-004

Turn status canceled and cancelled persist via submit and re-query with the same status.

**Acceptance Criteria:**
- [ ] Turn status canceled and cancelled persist via submit and re-query with the same status.

### TEST-MCP-TRIAGESTORE-005

SQLITE_BUSY and deadlock persistence failures classify as persistence_error retryable true with details.inner.

**Acceptance Criteria:**
- [ ] SQLITE_BUSY and deadlock persistence failures classify as persistence_error retryable true with details.inner.

### TEST-MCP-TRIAGESTORE-006

Superseded hook persist with omitted planFile/todoId writes None sentinels and status canceled.

**Acceptance Criteria:**
- [ ] Superseded hook persist with omitted planFile/todoId writes None sentinels and status canceled.

### TEST-MCP-TRIAGESTORE-007

Session-log SaveChanges and triage intake fail within about 5 seconds as backend_unavailable when storage is unreachable.

**Acceptance Criteria:**
- [ ] Session-log SaveChanges and triage intake fail within about 5 seconds as backend_unavailable when storage is unreachable.
