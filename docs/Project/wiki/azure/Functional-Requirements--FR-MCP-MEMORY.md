# FR-MCP-MEMORY

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-MEMORY-001 Global and workspace memory storage

The MCP Server SHALL store raw-text memories with a stable memory ID, explicit scope, and exact text round-trip semantics. Supported scopes are Global and Workspace. Global memories are visible to every workspace, while Workspace memories are visible only to their owning workspace.
Scope: layer-1+
**Acceptance Criteria:**
- [x] A memory record has a stable ID, scope, category, raw text, version, author metadata, and created/updated timestamps.
- [x] Global memories are returned for every workspace and are not stamped with a workspace owner.
- [x] Workspace memories require an owning workspace and are not returned for any other workspace.
- [x] Removed memories are hidden from normal reads and lists without physically deleting the row.
- [x] Memory text round-trips exactly, including internal newlines and punctuation.

## FR-MCP-MEMORY-002 Memory CRUD tools

The MCP Server SHALL expose add, list, update, and remove operations for raw-text memories through REST, typed client, MCP stdio, and REPL workflow surfaces.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Add creates a memory and returns ID, text, scope, category, version, and timestamps.
- [x] List returns the effective memory set for the active workspace by default.
- [x] Update replaces raw text, increments version, and can change scope under validated rules.
- [x] Remove soft-deletes a memory by ID.
- [x] Invalid IDs, duplicate active IDs, invalid scopes, empty text, and unauthorized scope changes return clear errors.

## FR-MCP-MEMORY-003 Required memories injection format

Supported agents SHALL receive active memories at each host-supported request boundary in a deterministic `REQUIRED MEMORIES` block. Memory text SHALL be raw text and SHALL NOT be summarized, decorated, or rewritten.
Scope: layer-1+
**Acceptance Criteria:**
- [x] The injected block header is exactly `REQUIRED MEMORIES`.
- [x] Each memory renders as `- MEMORY-CATEGORY-001: Raw memory text`.
- [x] An empty memory set renders exactly `REQUIRED MEMORIES` followed by `- None`.
- [x] Scope labels, timestamps, source metadata, confidence, and summaries do not appear in the injected block.
- [x] Hosts without request-boundary injection document the limitation and expose the best available explicit memory-list fallback without claiming automatic injection.

## FR-MCP-MEMORY-004 Marker template memory guidance

Generated `AGENTS-README-FIRST.yaml` instructions SHALL explain MCP memories, the memory tools, the ID format, scope behavior, and the exact `REQUIRED MEMORIES` rendering contract.
Scope: layer-1+
**Acceptance Criteria:**
- [x] templates/prompt-templates.yaml contains an MCP Memories section in the default marker prompt. (evidence: templates/prompt-templates.yaml MCP Memories section.)
- [x] The section names memory_add, memory_list, memory_update, and memory_remove. (evidence: templates/prompt-templates.yaml MCP Memories tool list.)
- [x] The section documents Global versus Workspace scope and the MEMORY-{CATEGORY}-{NNN} ID format. (evidence: templates/prompt-templates.yaml scope and ID guidance.)
- [x] The section states that memory text is raw text, not a secret store, and must not be silently generated. (evidence: templates/prompt-templates.yaml memory rules.)
- [x] The section documents agent-local memory import safeguards, updatedBy attribution, and session-log action recording for memory mutations. (evidence: docs/context/memory.md and templates/prompt-templates.yaml import and attribution rules.)
- [x] A marker-template contract test fails if the required section, tools, render format, import safeguards, or attribution rules are missing. (evidence: MemoryContractArtifactTests.MemoryContextDocumentation_IncludesImportAndAttributionRules.)

## FR-MCP-MEMORY-005 REPL memory namespace

The REPL SHALL expose typed `workflow.memory.*` operations for memory add, list, update, and remove.
Scope: layer-1+
**Acceptance Criteria:**
- [x] `workflow.memory.add`, `workflow.memory.list`, `workflow.memory.update`, and `workflow.memory.remove` dispatch through typed workflow code.
- [x] Valid REPL memory commands return standard result envelopes.
- [x] Invalid memory method names return the normal method-not-found envelope.
- [x] Invalid request payloads return standard structured error envelopes without crashing the REPL process.

## FR-MCP-MEMORY-006 Memory scope management and ordering

The MCP Server SHALL allow memories to be marked Global or Workspace and SHALL return Global memories before Workspace memories, with each group sorted by ID ascending.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Add supports explicit `Scope` and defaults to Workspace only when omitted.
- [x] Update can change a memory between Global and Workspace under validated rules.
- [x] List APIs, REPL methods, stdio tools, and required-memory injection all use Global-first grouping.
- [x] Each scope group sorts by ID ascending using ordinal string comparison.
- [x] Global rows never appear after workspace rows, and workspace rows from other workspaces are excluded.

## FR-MCP-MEMORY-007 Agent plugin memory integration

Official QBrainAi agent plugins SHALL expose memory tools and SHALL inject the `REQUIRED MEMORIES` block at host-supported request boundaries, or SHALL document a host limitation and expose an explicit memory-list fallback.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline v1, Cline v2, and OpenCode plugin lanes validate memory tool availability. (evidence: Shell plugin Bats memory tests and TypeScript memory tests.)
- [x] Plugins with request-boundary injection hooks render REQUIRED MEMORIES on every supported user prompt boundary. (evidence: Plugin validation and marker memory injection coverage.)
- [x] Plugins without a usable request-boundary injection hook document the host limitation without claiming automatic injection. (evidence: Plugin docs and validation expectations.)
- [x] Plugin validation identifies the host hook or fallback, workspace marker path, and example REQUIRED MEMORIES output. (evidence: Plugin validation coverage.)
- [x] Plugin memory mutations append session-log actions and clear local failsafe entries after server acknowledgement. (evidence: Shell Bats and TypeScript memory tests for mutation action append and failsafe cleanup.)

## FR-MCP-MEMORY-008 Versioned workspace memory federation

Workspace-scoped portable memories and their versions are committed project data; local edits work offline and explicit version commits trigger exchange. Global/machine-specific memory remains operational. Conflicting updates/deletes require review. Preserve valid MEMORY-* identity, explicit scope and sanitizer boundaries. LocalProxy forwarding and signed command replay are superseded for this mode.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given workspace memory M1 edited offline, when saved, then the local read changes but remote C0 does not; after explicit commit and sync the peer receives the version.
- [ ] Given A deletes and B edits M1, when merged, then needs_review preserves both histories.
- [ ] Given a global memory containing machine configuration, when cloning a project, then that memory is absent.

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
