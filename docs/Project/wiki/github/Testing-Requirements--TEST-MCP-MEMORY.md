# TEST-MCP-MEMORY

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-MEMORY

### TEST-MCP-MEMORY-001

Storage isolation tests SHALL prove Global memories and Workspace memories in two workspaces list as Global plus current workspace only, and that update/remove by ID cannot mutate another workspace-local memory.


### TEST-MCP-MEMORY-002

CRUD behavior tests SHALL prove add, list, update, remove, soft-delete omission, scope preservation, scope changes, invalid ID, invalid text, invalid category, and invalid scope failures.


### TEST-MCP-MEMORY-003

ID generation tests SHALL prove MEMORY-{CATEGORY}-{NNN} IDs are globally unique across Global and Workspace scopes, category counters are independent, and manually supplied duplicate active IDs are rejected.


### TEST-MCP-MEMORY-004

REST/client contract tests SHALL prove MemoryController and MemoryClient route, serialize, and deserialize memory scope, text, ID, and version correctly, and that McpServerClient.Memory propagates workspace and auth settings.


### TEST-MCP-MEMORY-005

REPL contract tests SHALL prove workflow.memory.add, workflow.memory.list, workflow.memory.update, and workflow.memory.remove dispatch through typed REPL workflow code, support scope values where applicable, and return standard result/error envelopes.


### TEST-MCP-MEMORY-006

Marker-template contract tests SHALL prove default-marker-prompt contains the MCP Memories section, exact REQUIRED MEMORIES header, memory tool names, ID format, scope guidance, no-secrets guidance, agent-local import safeguards, updatedBy attribution, and session-log action guidance.

**Acceptance Criteria:**
- [x] Contract tests fail if the marker prompt omits the MCP Memories section, REQUIRED MEMORIES header, memory tool names, ID format, scope guidance, or no-secrets guidance. (evidence: MemoryContractArtifactTests.GeneratedMarkerPrompt_IncludesMemoryInstructions.)
- [x] Contract tests fail if memory context documentation omits agent-local import safeguards, updatedBy attribution, or session-log action guidance. (evidence: MemoryContractArtifactTests.MemoryContextDocumentation_IncludesImportAndAttributionRules.)

### TEST-MCP-MEMORY-007

YAML schema and stdio contract tests SHALL prove valid workflow.memory.* envelopes pass; add without text fails; add/update with invalid scope fails; update/remove without ID fails; invalid MEMORY ID fails; unknown memory methods fail; and docs/stdio-tool-contract.json includes all memory tools.


### TEST-MCP-MEMORY-008

Scope ordering tests SHALL prove list surfaces and required-memory injection return Global memories first sorted by ID and Workspace memories second sorted by ID, excluding workspace rows from other workspaces.


### TEST-MCP-MEMORY-009

Agent plugin validation SHALL prove each supported plugin exposes memory tools and either injects REQUIRED MEMORIES with Global-first ordering at supported request boundaries or documents the host limitation with an explicit memory-list fallback.
