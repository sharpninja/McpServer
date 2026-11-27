# TEST-HANDOFF

[Back to Testing Requirements](Testing-Requirements)

## TEST-HANDOFF

### TEST-HANDOFF-001

Cover documented request, result, draft, provenance, and diagnostic contracts, including typed-client serialization.

**Acceptance Criteria:**
- [x] All public handoff contract types round-trip through the typed client JSON context. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Required contract types exist with complete XMLDocs and the documented enum values. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-HANDOFF-002

Cover bounded readers, workspace containment, and rejection of missing, unsupported, oversized, traversal, external, and reparse-escaping sources.

**Acceptance Criteria:**
- [x] Missing, unsupported, oversized, traversal, external, and reparse-escaping paths fail with diagnostics and no TODO. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Markdown, text, JSON, and YAML inputs are accepted when contained and within 8 MiB. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-HANDOFF-003

Cover versioned HandoffTodoDraft extraction and strict JSON parsing, including malformed AI output.

**Acceptance Criteria:**
- [x] Extraction is invoked with AgentPoolOneShotContext.HandoffTodoDraft and a versioned prompt. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Malformed or compatibility JSON produces diagnostics and never creates a TODO. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-HANDOFF-004

Cover draft validation, field-specific diagnostics, DraftOnly, RequireReview, CreateWhenConfident, low confidence, and ambiguous handoffs.

**Acceptance Criteria:**
- [x] Invalid or conflicting draft fields produce field-specific diagnostics. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] DraftOnly never mutates TODOs, RequireReview persists an approvable run, and CreateWhenConfident honors the 0.75 confidence and no-error gates. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-HANDOFF-005

Cover exclusive TODO-service persistence, exact-one creation, deterministic replay, ID collisions, approval races, and TODO-service failure.

**Acceptance Criteria:**
- [x] Successful creation produces exactly one TODO through ITodoService. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Replay of the same workspace, content hash, and prompt version returns the existing receipt unless force=true. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] ID collisions require review and are never silently renamed. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-HANDOFF-006

Cover API, client, REPL, Director, MCP-tool, and plugin-skill inventory and invocation parity, including workspace isolation.

**Acceptance Criteria:**
- [x] Ingest, get, and approve exist on API, client, REPL, Director, MCP tools, and plugin skill. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Every surface delegates to IHandoffIngestionService and applies workspace isolation. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-HANDOFF-007

Cover normalized run storage, diagnostic persistence, cancellation, and the prohibition on logging credentials or raw source content.

**Acceptance Criteria:**
- [x] Persisted runs retain run ID, source kind and locator, SHA-256 hash, extraction time, prompt version, agent, model, confidence, mode, review state, diagnostics, and created TODO ID. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Persisted runs and logs do not contain raw credentials or source content. (evidence: tests/McpServer.Client.Tests/HandoffClientTests.cs; tests/Build.Tests/HandoffD3D5OverlayTests.cs; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
