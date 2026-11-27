# TEST-MCP-TRANSCRIPT

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-TRANSCRIPT

### TEST-MCP-TRANSCRIPT-001

Unit tests cover source detection and parser fixture coverage for Claude, Codex, Grok, Cline, Copilot, and OpenCode formats.

**Acceptance Criteria:**
- [x] Fixtures identify supported source kinds and reject ambiguous/unknown bundles. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: TranscriptFixtureInventoryTests; Detector_DiscoversEverySupportedRealTranscriptSource.)

### TEST-MCP-TRANSCRIPT-002

Unit tests verify neutral-to-session-log mapping preserves native values, keeps absent semantics absent, and marks deterministic derived IDs.

**Acceptance Criteria:**
- [x] Missing model, token, response, action, status, and timestamp fields are not invented. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: loss-aware normalization and unsupported/malformed diagnostics in TranscriptCorePipelineTests.)

### TEST-MCP-TRANSCRIPT-003

Unit tests cover malformed lines, unknown events, incomplete turns, mixed schemas, cancellation, limits, traversal, and path escape rejection.

**Acceptance Criteria:**
- [x] Strict mode rejects error diagnostics while lenient mode returns valid data plus diagnostics. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: malformed/unsupported/incomplete adapter tests and HTTP strict=false multi-status coverage.)

### TEST-MCP-TRANSCRIPT-004

Unit tests verify canonical YAML serialization is deterministic, redacted, LF-normalized, UTF-8 without BOM, and round-trips to equivalent objects.

**Acceptance Criteria:**
- [x] Repeated runs over identical normalized input produce byte-identical YAML. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: NormalizationWritesArtifactsWithoutSessionPersistence and canonical mapping checks.)

### TEST-MCP-TRANSCRIPT-005

Unit tests verify idempotent replay keys, richer-field preservation, pre-submit failsafe creation, and retention/deletion based on precise persistence receipts.

**Acceptance Criteria:**
- [x] Failsafe files are deleted only when persisted=true and degraded=false. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: HTTP path persistence/deletion; pending failsafe envelope; root-id failsafe naming; TEST-MCP-REPL-025 Pester.)

### TEST-MCP-TRANSCRIPT-006

Unit and contract tests verify transcript conversion is implemented only in the shared non-plugin transcript core and plugin packages contain no transcript ingestion helpers, skills, endpoint shortcuts, or parser forks.

**Acceptance Criteria:**
- [x] Plugin inventory tests prove transcript ingestion helper files, skills, and REPL endpoint shortcuts are absent. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: plugin Pester TEST-MCP-TRANSCRIPT-010 endpoint absence and legacy parser removal.)
- [x] Shared core transcript tests cover parser and projector behavior without relying on plugin-specific ingestion code. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: Support.Mcp transcript unit scope 60/0/0 and plugin endpoint absence.)

### TEST-MCP-TRANSCRIPT-007

Integration tests cover /mcpserver/sessionlog/ingest/path and /upload for defaults, folder discovery, multipart, ZIP limits, and security rejections.

**Acceptance Criteria:**
- [x] Invalid requests return 400, unauthorized paths 403, exceeded limits 413, and mixed folder runs 207 Multi-Status where appropriate. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: TranscriptIngestionControllerTests status mapping; SessionLogTranscriptIngestionControllerTests mixed ZIP/security.)

### TEST-MCP-TRANSCRIPT-008

Tests verify typed client, REPL commands, HTTP MCP discovery, stdio MCP discovery, and tool invocation for transcript ingestion and normalization.

**Acceptance Criteria:**
- [x] sessionlog_ingest_path and sessionlog_normalize_path require workspacePath and delegate through the shared service. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: TranscriptMcpTool/StdioHost; McpTransport transcript tools; Repl.Core dispatcher; Client ingest tests.)

### TEST-MCP-TRANSCRIPT-009

Tests cover Cline paired JSON/JSONL, Copilot events folders, OpenCode JSONL, and read-only OpenCode SQLite snapshot normalization.

**Acceptance Criteria:**
- [x] OpenCode SQLite tests prove consistent backup snapshot use and no source DB/WAL writes. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: OpenCodeSqliteTranscriptTests normalization without source writes and WAL snapshot capture.)

### TEST-MCP-TRANSCRIPT-010

End-to-end plugin tests verify Claude, Codex, and Grok plugin packages do not expose transcript ingestion while model-run logging continues through normal workflow.sessionlog tools and non-plugin transcript ingestion remains externally verifiable.

**Acceptance Criteria:**
- [x] Claude, Codex, and Grok plugin packages expose no transcript ingestion skill, helper, or endpoint shortcut. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: clean plugin Pester TEST-MCP-TRANSCRIPT-010 endpoint absence.)
- [x] Representative model sessions can write turns, actions, and completions through workflow.sessionlog without automatic transcript import. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: clean plugin Pester model-authored logging checks; live Codex turn req-20260711T022759Z-prompt-c4f0. External-client sampling remains the separate deployment TODO gate.)

### TEST-MCP-TRANSCRIPT-011

Codex transcript adapter coverage for real rollout record classes (validates TR-MCP-TRANSCRIPT-002 and TR-MCP-TRANSCRIPT-003). Unit tests in tests/McpServer.Support.Mcp.Tests/Ingestion/CodexTranscriptAdapterCoverageTests.cs verify: function_call and custom_tool_call records normalize to assistant tool-call events with call_id/name/status metadata; function_call_output and custom_tool_call_output records normalize to tool-role events preserving output text and call pairing; reasoning records with recoverable summary text normalize to assistant reasoning events while encrypted-only reasoning is skipped and reported through one aggregate info diagnostic (codex_encrypted_reasoning); event_msg records are skipped as UI mirrors of response_item records with one aggregate info diagnostic (codex_event_msg_skipped) and no warnings; turn_context records contribute session model and workspace path without diagnostics; world_state and compacted records are skipped with one aggregate info diagnostic (codex_nonconversation_skipped); unknown top-level record types and unknown response_item payload types warn once per distinct type with occurrence counts (codex_unknown_record, codex_unknown_response_item). Evidence 2026-07-14: 9/9 tests red against prior adapter, green after fix; real 2599-line rollout normalizes to 1174 events with 0 warnings (previously 166 events with 2432 warnings).


### TEST-MCP-TRANSCRIPT-012

Imported-session lifecycle semantics (validates TR-MCP-TRANSCRIPT-004). Unit tests in tests/McpServer.Support.Mcp.Tests/Services/SessionLogImportedSessionDeleteTests.cs and SessionLogResubmissionReviveTests.cs verify: turn-level keyed operations (DeleteTurnAsync, ReplaceTurnSectionAsync, DeleteTurnItemAsync) accept provider-native identifiers persisted by transcript imports (UUID session ids, tool-call request ids) so turns can be repaired by resubmission; DeleteSessionAsync remains canonical-only by policy, rejecting imported session ids (sessions are soft-delete only and never deletable for imports); SubmitAsync revives a soft-deleted session that still holds the unique (WorkspaceId, SourceType, SessionId) key by restoring its row graph (session, turns, child rows) with only the SoftDelete named query filter bypassed (Workspace tenancy filter stays active) and then applying the resubmitted turn data; whitespace identifiers stay rejected. Evidence 2026-07-14: revive tests red with InvalidOperationException (disappeared after UNIQUE constraint failure) before fix, 7/7 green after; full suite green (build.ps1 Test exit 0); live recovery of session 019f2580-48c8-7912-b6a9-27f61b18d0d3 in F:\GitHub\MouseKeyProxy from tombstone to 1174 corrected turns via re-ingest plus 28 turn-level deletes of stale duplicates.


### TEST-MCP-TRANSCRIPT-013

Validates FR-MCP-TRANSCRIPT-009 and TR-MCP-TRANSCRIPT-010. Unit: a JSONL transcript line larger than the former 8 MiB ceiling is ingested without an InvalidDataException, replacing the assertion in IngestionService_RejectsOversizedJsonlLine which is retargeted to prove the int.MaxValue ceiling is still an enforced bound rather than an absent one. Unit: the streaming reader returns identical records to the previous ReadAllLinesAsync implementation for an existing multi-record fixture, proving no regression in parse behavior. Unit: peak allocation while reading a many-small-line transcript stays proportional to the largest line rather than to file size. Integration: retained hostile-archive guards still reject their fixtures at the unchanged values, specifically archive entry count above 10,000, compression ratio above 20, ZIP symlink entries, and path-traversal entries, each returning 413 or 400 as previously mapped.
