# TR-MCP-TRIAGE

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-TRIAGE-001

**Durable triage storage** — Durable EF entities store reports, groups, research runs, statuses, idempotency keys, and workspace filters.
**Covered by:** FR: FR-MCP-TRIAGE-001; TEST: TEST-MCP-TRIAGE-001
**Status:** planned
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Triage reports, groups, and research runs persist in the MCP database and are query-filtered by workspace.

## TR-MCP-TRIAGE-002

**Deterministic triage grouping** — The grouping service uses workspace, dedupeKey, component, path, symbol, error signature, normalized title tokens, and McpServer workspace routing for MCP Server core and plugin bugs.
**Covered by:** FR: FR-MCP-TRIAGE-002; TEST: TEST-MCP-TRIAGE-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Matching reports in one workspace share a group; matching reports across workspaces do not unless routed to the registered McpServer workspace by MCP Server bug detection.
- [ ] MCP Server core and plugin bug reports target the registered McpServer workspace only when the workspace registry contains it.

## TR-MCP-TRIAGE-003

**Async triage worker** — A background worker handles quiet-period expiry, configured agent execution, prompt rendering, and timeouts.
**Covered by:** FR: FR-MCP-TRIAGE-003; TEST: TEST-MCP-TRIAGE-003
**Status:** planned
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The worker dispatches only after the configured quiet period unless a group is manually flushed.

## TR-MCP-TRIAGE-004

**Triage schema and TODO creation** — Triage research output is schema-validated and converted idempotently into BUG-TRIAGE TODOs.
**Covered by:** FR: FR-MCP-TRIAGE-004; TEST: TEST-MCP-BUGTRIAGE-043, TEST-MCP-TRIAGE-004, TEST-MCP-TRIAGE-005, TEST-MCP-TRIAGE-006
**Status:** planned
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Valid research output creates one backlog TODO and failed output creates none.

## TR-MCP-TRIAGE-005

**Triage Grok runner omits CLI-rejected flags** — The triage Grok CLI runner SHALL NOT pass --model auto or --effort max, both of which the Grok CLI rejects at startup. AC1: BuildGrokArgumentList omits --model when the model is the 'auto' sentinel. AC2: effort is pinned to 'high' (HighestEffort), never 'max'. Guards BUG-TRIAGE-076 via TEST-MCP-BUGTRIAGE-043.
**Covered by:** FR: FR-MCP-TRIAGE-004; TEST: TEST-MCP-BUGTRIAGE-043, TEST-MCP-TRIAGE-004, TEST-MCP-TRIAGE-005, TEST-MCP-TRIAGE-006
**Status:** pending
Scope: layer-1+

