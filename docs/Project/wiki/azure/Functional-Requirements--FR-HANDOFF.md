# FR-HANDOFF

[Back to Functional Requirements](Functional-Requirements)

## FR-HANDOFF-001 Ingest workspace-scoped handoff documents

Ingest workspace-scoped handoff documents from a contained file path, caller-supplied content, or MCP artifact reference.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Supports Markdown, text, JSON, and YAML inputs. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Rejects missing, unsupported, oversized, traversal, external, and reparse-escaping paths. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Maximum decoded input is 8 MiB. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-HANDOFF-002 Extract structured MCP TODO drafts via one-shot agents

Use the existing one-shot agent system to extract a structured MCP TODO draft.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Extraction uses a versioned prompt and strict JSON contract. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Malformed output produces diagnostics and never creates a TODO. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Unknown or missing source information is not silently discarded. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-HANDOFF-003 Validate and normalize generated TODO drafts

Validate and normalize generated TODO drafts.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Validate ID, title, section, priority, estimate, description, technical details, implementation tasks, dependencies, and requirement links. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Invalid or conflicting values produce field-specific diagnostics. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-HANDOFF-004 Support DraftOnly, RequireReview, and CreateWhenConfident modes

Support DraftOnly, RequireReview, and CreateWhenConfident modes.
Scope: layer-1+
**Acceptance Criteria:**
- [x] DraftOnly is the default and never mutates TODO state. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] RequireReview persists an approvable run without creating a TODO. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] CreateWhenConfident creates only when confidence is at least 0.75 and no error diagnostic exists. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-HANDOFF-005 Persist approved TODOs exclusively through the TODO service

Persist approved TODOs exclusively through the existing TODO service.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Successful creation produces exactly one TODO. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Replay of the same workspace, content hash, and prompt version returns the existing receipt. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] ID collisions require review and are never silently renamed. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-HANDOFF-006 Preserve auditable provenance and diagnostics

Preserve auditable provenance and diagnostics for every run.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Retain run ID, source kind and locator, SHA-256 content hash, extraction time, prompt/template version, agent, model, confidence, mode, review state, diagnostics, and created TODO ID. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Raw credentials or source content are not copied into logs. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-HANDOFF-007 Provide equivalent public handoff surfaces

Provide behaviorally equivalent API, client, REPL, Director, MCP-tool, and plugin-skill entrypoints.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Every surface delegates to the same service, returns the same result contract, applies workspace isolation, and exposes ingest, inspect, and approval workflows. (evidence: the handoff client unit tests and the handoff D5 overlay tests of the build's infrastructure suite, which cite this requirement in their XML documentation; overlay G3 D5 AGREE + G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
