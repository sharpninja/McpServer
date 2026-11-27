# TR-MCP-TRANSCRIPT

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-TRANSCRIPT-001

**Bounded transcript reader and parser registry** — Implement streaming transcript readers with size, line, record, timeout, and cancellation bounds plus deterministic source adapter selection.
**Covered by:** FR: FR-MCP-TRANSCRIPT-001, FR-MCP-TRANSCRIPT-002; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-003, TEST-MCP-TRANSCRIPT-007, TEST-MCP-TRANSCRIPT-002, TEST-MCP-TRANSCRIPT-009
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Readers enforce byte, line, record, timeout, cancellation, recursion, and archive limits with diagnostics. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: IngestionService_RejectsOversizedJsonlLine; upload ZIP/security tests.)

## TR-MCP-TRANSCRIPT-002

**Source adapters and bundle detection** — Implement adapters for Claude, Codex, Grok, Cline, Copilot, and OpenCode plus bundle detection for native files, folders, ZIPs, and SQLite snapshots.
**Covered by:** FR: FR-MCP-TRANSCRIPT-001, FR-MCP-TRANSCRIPT-002, FR-MCP-TRANSCRIPT-008; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-003, TEST-MCP-TRANSCRIPT-007, TEST-MCP-TRANSCRIPT-002, TEST-MCP-TRANSCRIPT-009, TEST-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-011, TEST-MCP-TRANSCRIPT-012
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Each supported provider has verified fixtures and adapter-specific diagnostics. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: real fixture manifest; RealTranscriptFixtureIntegrationTests; adapter diagnostics for all six providers.)

## TR-MCP-TRANSCRIPT-003

**Neutral event model and projectors** — Define loss-aware neutral transcript events and Claude, Codex, and Grok compatibility projectors while preserving provenance and derived-value markers.
**Covered by:** FR: FR-MCP-TRANSCRIPT-002, FR-MCP-TRANSCRIPT-003, FR-MCP-TRANSCRIPT-008; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-002, TEST-MCP-TRANSCRIPT-003, TEST-MCP-TRANSCRIPT-009, TEST-MCP-TRANSCRIPT-004, TEST-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-011, TEST-MCP-TRANSCRIPT-012
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Projectors preserve provenance, tool pairing, reasoning, usage, and derived-value markers. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: IngestionService_NormalizesRealTranscriptFixtures; compatibility projector and diagnostics tests.)

## TR-MCP-TRANSCRIPT-004

**Canonical YAML and write-ahead persistence** — Use existing SessionLogIngestor, UnifiedSessionLog DTOs, and IYamlSerializer to write deterministic importRecovery YAML and persist through the current session-log path.
**Covered by:** FR: FR-MCP-TRANSCRIPT-003, FR-MCP-TRANSCRIPT-004; TEST: TEST-MCP-TRANSCRIPT-002, TEST-MCP-TRANSCRIPT-004, TEST-MCP-TRANSCRIPT-005
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Failsafe pending artifacts are atomically written before submission and deleted only after non-degraded persistence success. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: PersistWritesRunArtifactsAndPendingFailsafeEnvelope; PersistNamesFailsafeDocumentsByRootIdWithoutOverwrite; TEST-MCP-REPL-025 Pester failsafe tests.)

## TR-MCP-TRANSCRIPT-005

**REPL import and model-owned session logging** — Expose shared transcript conversion/import through non-plugin typed server, client, MCP, and REPL surfaces while keeping plugin packages free of transcript ingestion endpoints, helpers, skills, handwritten YAML, and parser forks.
**Covered by:** FR: FR-MCP-TRANSCRIPT-005; TEST: TEST-MCP-TRANSCRIPT-006, TEST-MCP-TRANSCRIPT-010
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Claude, Codex, and Grok plugin packages contain no transcript ingestion helper or skill and instruct models to use workflow.sessionlog tools for live session logging. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: plugin Pester TEST-MCP-TRANSCRIPT-010 endpoint absence, marker prompt model-authored logging, legacy parser removal.)

## TR-MCP-TRANSCRIPT-006

**Recursive discovery upload extraction and path security** — Implement recursive bundle discovery, upload extraction, ZIP defenses, allowlisted roots, traversal rejection, and symlink/reparse escape prevention.
**Covered by:** FR: FR-MCP-TRANSCRIPT-001, FR-MCP-TRANSCRIPT-006; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-003, TEST-MCP-TRANSCRIPT-007, TEST-MCP-TRANSCRIPT-008
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Traversal, duplicate canonical paths, links, external absolute paths, reparse escapes, and decompression ratio violations are rejected. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: PersistRejectsTraversalOutsideWorkspaceAndProviderRoots; RejectsDuplicateZipPaths; RejectsZipTraversal.)

## TR-MCP-TRANSCRIPT-007

**HTTP client MCP and REPL contracts** — Add typed REST request/result models, SessionLogClient ingestion methods, native MCP tools, and REPL commands for path ingestion and normalization.
**Covered by:** FR: FR-MCP-TRANSCRIPT-006, FR-MCP-TRANSCRIPT-007; TEST: TEST-MCP-TRANSCRIPT-007, TEST-MCP-TRANSCRIPT-008
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] HTTP, stdio MCP, typed client, and REPL expose matching option defaults and receipt fields. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: Client ingest tests; Repl.Core transcript tests; TranscriptMcpTool/StdioHost; McpTransport transcript tools.)

## TR-MCP-TRANSCRIPT-008

**Provider normalization and native stores** — Support secondary provider normalization for Cline paired JSON, Copilot event streams, OpenCode JSONL exports, and read-only OpenCode SQLite snapshots.
**Covered by:** FR: FR-MCP-TRANSCRIPT-008; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-009, TEST-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-011, TEST-MCP-TRANSCRIPT-012
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Cline, Copilot, and OpenCode native storage tests verify loss-aware normalization and OpenCode snapshot read-only behavior. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: real fixture normalization; OpenCodeSqliteTranscriptTests no DB/WAL writes and WAL snapshot capture.)

## TR-MCP-TRANSCRIPT-009

**Codex transcript ingestion normalizes tool-call, reasoning, and turn_context records** — Codex transcript ingestion SHALL normalize tool-call, reasoning, and turn_context rollout records so known record classes do not emit codex_missing_role or codex_unknown_record warnings. AC1: ingesting a Codex rollout fixture produces 0 warnings for these known classes and the full event count (vs the old ~2432 warnings). Guards BUG-TRIAGE-080 via TEST-MCP-TRANSCRIPT-011 and TEST-MCP-TRANSCRIPT-012.
**Covered by:** FR: FR-MCP-TRANSCRIPT-008; TEST: TEST-MCP-TRANSCRIPT-001, TEST-MCP-TRANSCRIPT-009, TEST-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-011, TEST-MCP-TRANSCRIPT-012
**Status:** pending
Scope: layer-1+

## TR-MCP-TRANSCRIPT-010

**Int32.MaxValue transcript ceilings with streaming JSONL reader** — Implements FR-MCP-TRANSCRIPT-009. (1) TranscriptUtilities.ReadJsonLinesAsync raises maxSourceFileBytes from 256 MiB, maxLineBytes from 8 MiB, and maxRecords from 2,000,000 to int.MaxValue. (2) SessionLogTranscriptIngestionController raises MaxUploadRequestBytes from 512 MiB, MaxSourceFileBytes from 256 MiB, and MaxExpandedUploadBytes from 2 GiB to int.MaxValue; MaxArchiveEntries (10,000) and MaxCompressionRatio (20.0) are unchanged. RequestSizeLimit remains a compile-time constant attribute and continues to work because int.MaxValue is a constant. (3) ReadJsonLinesAsync streams lines through a StreamReader instead of calling File.ReadAllLinesAsync, so a ceiling of int.MaxValue does not convert a rejection into an out-of-memory kill. IMPLEMENTED 2026-07-20. Evidence: red gate IngestionService_AcceptsJsonlLineAboveFormerCeiling failed with "Transcript JSONL line exceeds the 8 MiB limit" at TranscriptUtilities.cs:104 (1 failed, 1 passed); green gate Support.Mcp.Tests 1692/0/0, Repl.Core.Tests 810/0/0, Client.Tests 259/0/0, transcript integration subset 10/0/0. Deployed to the McpServer Windows service (1.4.20, pid 39404) and verified live: sessionlog_ingest_path accepted a 9,437,285-byte single-line JSONL returning totalSessions 1 with zero diagnostics.
**Covered by:** FR: FR-MCP-TRANSCRIPT-009; TEST: TEST-MCP-TRANSCRIPT-013
**Status:** pending
Scope: layer-1+

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
