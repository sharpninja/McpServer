# TEST-MCP-HEALTH

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-HEALTH

### TEST-MCP-HEALTH-002

WorkspaceReadinessHealthCheck returns Healthy when an enabled primary workspace is registered and has a seeded token; returns Unhealthy when the token subsystem is uninitialized, no enabled workspace is registered, or the primary workspace has no seeded token.

**Acceptance Criteria:**
- [x] Returns Healthy when enabled primary workspace is registered with seeded token.
- [x] Returns Unhealthy when token subsystem is uninitialized.
- [x] Returns Unhealthy when no enabled workspace is registered.
- [x] Returns Unhealthy when primary workspace has no seeded token.

### TEST-MCP-HEALTH-003

Integration test with the data layer up: /mcpserver/todo returns 200 with a valid token and no X-Workspace-Path; unknown or missing keys return 401; /ready returns 200 Healthy with workspace-ready check listed.

**Acceptance Criteria:**
- [x] /mcpserver/todo returns 200 with valid token and no X-Workspace-Path.
- [x] Unknown or missing API keys return 401.
- [x] /ready returns 200 Healthy with workspace-ready check.
