# FR-MCP-TRANSCRIPT

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-TRANSCRIPT-001 Transcript source ingestion

The system shall ingest agent transcript bundles from supported source formats with deterministic source detection and diagnostics.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Auto detection identifies exactly one supported source or rejects ambiguous and unsupported input with diagnostics. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by TranscriptCorePipelineTests.Detector_DiscoversEverySupportedRealTranscriptSource and RealTranscriptFixtureIntegrationTests.RealTranscriptManifestCoversEveryAgent.)
- [x] Folder, multipart, and ZIP ingestion discover independent session bundles without mixing sessions. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by SessionLogTranscriptIngestionControllerTests.IngestUploadAsync_MixedZipBundleReturnsMultiStatusWhenStrictFalse, IngestUploadAsync_ZipClaudeFixturePersistsAndDeletesStagingRun, and controller path/upload tests.)

## FR-MCP-TRANSCRIPT-002 Faithful loss-aware normalization

The system shall normalize supported transcripts into a neutral event model without silently discarding malformed, unknown, partial, or unpaired records.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Native identity, ordering, timestamps, roles, content blocks, reasoning, tool calls/results, usage, failures, workspace metadata, subagents, and provenance are preserved when present. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by IngestionService_NormalizesRealTranscriptFixtures and RealJsonlFixturesContainProviderSpecificEvents across Claude, Codex, Grok, Cline, Copilot, and OpenCode fixtures.)
- [x] Missing semantic values remain absent and derived required IDs are deterministic and explicitly marked. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by unsupported/malformed/incomplete diagnostics tests including IngestionService_CodexUnsupportedRecordsEmitDiagnostics, ClineMalformedMessagesEmitDiagnostics, and OpenCodeJsonlIncompleteStepEmitsDiagnostic.)

## FR-MCP-TRANSCRIPT-003 Canonical session log projection

The system shall project neutral transcript events directly into canonical Session Log YAML using the existing session-log serializer and model.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Canonical Session Log YAML is produced directly from neutral events through the existing session-log serializer/model. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by IngestionService_NormalizationWritesArtifactsWithoutSessionPersistence, IngestionService_PersistWritesRunArtifactsAndPendingFailsafeEnvelope, and TranscriptSessionLogPersisterTests.PersistAsync_SubmitsUnifiedSessionLogThroughSessionLogService.)
- [x] Compatibility profile output is optional and caller-selected for Claude, Codex, or Grok. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by IngestionService_EmitsCompatibilityJsonlWhenProfileRequested and Repl.Core NormalizeTranscripts tests.)

## FR-MCP-TRANSCRIPT-004 Idempotent import and recovery

The system shall import transcript-derived Session Log YAML through the existing persistence path with write-ahead recovery and idempotent replay.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Repeated imports use deterministic idempotency data and persist through the existing session-log path. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by TranscriptSessionLogPersisterTests.PersistAsync_SubmitsUnifiedSessionLogThroughSessionLogService and HTTP ingest path persistence tests.)
- [x] A failsafe importRecovery envelope is retained unless persistence explicitly reports persisted=true and degraded=false. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by IngestionService_PersistWritesRunArtifactsAndPendingFailsafeEnvelope, IngestionService_PersistNamesFailsafeDocumentsByRootIdWithoutOverwrite, and TEST-MCP-REPL-025 plugin Pester failsafe tests.)

## FR-MCP-TRANSCRIPT-005 Model-owned session log writing for Claude Codex Grok

Claude, Codex, and Grok models shall write MCP Session Log entries themselves through the normal session-log APIs; plugins shall not expose transcript ingestion or normalization endpoints, helpers, skills, or parser forks.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Claude, Codex, and Grok plugins expose no transcript ingestion or normalization helper, skill, endpoint shortcut, or parser fork. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by plugin Pester TEST-MCP-TRANSCRIPT-010 does not expose transcript ingestion endpoints through plugins and removes legacy Codex JSONL parser helpers.)
- [x] Models continue to write session-log turns, actions, decisions, and completions through the existing workflow.sessionlog tools instead of relying on automatic transcript ingestion. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by plugin Pester marker prompt/sessionlog tests and live Codex turn req-20260711T022759Z-prompt-c4f0 using workflow.sessionlog appendActions/appendDialog/CompleteTurn path.)

## FR-MCP-TRANSCRIPT-006 HTTP path and upload ingestion

The system shall expose HTTP transcript ingestion for allowlisted server-local files/folders, multipart uploads, multi-file uploads, and ZIP uploads.
Scope: layer-1+
**Acceptance Criteria:**
- [x] POST /mcpserver/sessionlog/ingest/path accepts allowlisted file and folder paths and rejects unauthorized paths. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by IngestPathAsync_WorkspaceRelativeCodexFixturePersistsAndDeletesFailsafe, IngestPathAsync_RealFixtureForEachAgentNormalizesThroughHttp, and unauthorized path tests.)
- [x] POST /mcpserver/sessionlog/ingest/upload accepts multipart files and ZIP bundles within documented limits. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by IngestUploadAsync_ZipClaudeFixturePersistsAndDeletesStagingRun, IngestUploadAsync_RejectsDuplicateZipPaths, and controller ZIP traversal tests.)

## FR-MCP-TRANSCRIPT-007 REPL and MCP ingestion parity

The system shall expose transcript ingestion and normalization through typed client, REPL, and native MCP tools with equivalent behavior.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Typed client and REPL expose equivalent ingest and normalize operations. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by SessionLogClientTests.IngestTranscriptPathAsync_PostsPathContract, IngestTranscriptUploadAsync_PostsMultipartUploadContract, and Repl.Core transcript workflow/dispatcher tests.)
- [x] MCP calls retain required workspacePath while hosted and REPL agents resolve workspace ownership from their bound client. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by TranscriptMcpToolTests.SessionLogIngestPath_DelegatesWorkspaceBoundRequest, SessionLogNormalizePath_DelegatesProfileProjectionWithoutPersistenceByDefault, and McpTransport transcript tool tests.)

## FR-MCP-TRANSCRIPT-008 Secondary provider normalization

The system shall normalize Cline, Copilot, and OpenCode transcripts and native stores into canonical Session Log YAML and optional Claude, Codex, or Grok compatibility JSONL.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Cline paired JSON/JSONL, Copilot event folders, and OpenCode JSONL/SQLite snapshots normalize into canonical Session Log YAML. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by IngestionService_NormalizesRealTranscriptFixtures, OpenCodeSqliteTranscriptTests, and real manifest coverage for Cline, Copilot, and OpenCode.)
- [x] Secondary sources can emit optional Claude, Codex, or Grok compatibility JSONL without reparsing it for canonical YAML. (evidence: Current validation 2026-07-10 21:33-21:37 CDT: Support.Mcp transcript unit 60 passed/0 failed/0 skipped; Support.Mcp transcript integration+McpTransport 22/0/0; Repl.Core transcript 4/0/0; Client IngestTranscript 2/0/0; clean plugin Pester 47/0/0. Covered by IngestionService_EmitsCompatibilityJsonlWhenProfileRequested and Repl.Core normalization profile tests.)

## FR-MCP-TRANSCRIPT-009 Transcript ingestion size ceilings raised to Int32.MaxValue

Transcript ingestion must accept transcript sources whose per-file size, per-JSONL-line size, record count, upload request size, and expanded archive size are bounded only by Int32.MaxValue (2,147,483,647). Operators ingesting large agent transcripts (notably Claude Code JSONL, where a single line can carry a full tool result) must not be rejected by the previous 256 MiB per-file, 8 MiB per-line, 2,000,000-record, 512 MiB request, or 2 GiB expanded-content ceilings. Guards that defend against hostile archives rather than large transcripts are retained unchanged: archive entry count, compression ratio, ZIP symlink rejection, and path-traversal rejection. Supersedes the ceiling values asserted by FR-MCP-TRANSCRIPT-002 and TR-MCP-TRANSCRIPT-001; the bounded-reader obligation itself is unchanged, only the bound values move.
Scope: layer-1+

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
