# TEST-MCP-TRIAGEPLUGIN

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-TRIAGEPLUGIN

### TEST-MCP-TRIAGEPLUGIN-001

Pester proves background openSession does not rebind root, cache replace resolves or named drift, profile cwd uses hook workspace path, beginTurn timeout is degraded queued, and completeTurn after sessionId rebind clears failsafe.

**Acceptance Criteria:**
- [ ] Pester proves background openSession does not rebind root, cache replace resolves or named drift, profile cwd uses hook workspace path, beginTurn timeout is degraded queued, and completeTurn after sessionId rebind clears failsafe.

### TEST-MCP-TRIAGEPLUGIN-002

ReplacePluginCache retains the current cache or rebinds to a replacement. If no replacement exists it emits a named version-drift error.

**Acceptance Criteria:**
- [ ] ReplacePluginCache retains the current cache or rebinds to a replacement. If no replacement exists it emits a named version-drift error.

### TEST-MCP-TRIAGEPLUGIN-003

Resolve-McpCacheDir with the hook workspace path succeeds when cwd is the user profile and env is empty.

**Acceptance Criteria:**
- [ ] Resolve-McpCacheDir with the hook workspace path succeeds when cwd is the user profile and env is empty.

### TEST-MCP-TRIAGEPLUGIN-004

beginTurn persist timeout after failsafe returns degraded/queued and retains failsafe.

**Acceptance Criteria:**
- [ ] beginTurn persist timeout after failsafe returns degraded/queued and retains failsafe.

### TEST-MCP-TRIAGEPLUGIN-005

completeTurn persist identity prefers current-turn sessionId after sessionId rebind.

**Acceptance Criteria:**
- [ ] completeTurn persist identity prefers current-turn sessionId after sessionId rebind.
