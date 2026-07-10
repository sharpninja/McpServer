# MCP-TRANSCRIPT-001 BDPv4 Test Plan

## Scope

This plan governs the multi-agent transcript ingestion implementation for `MCP-TRANSCRIPT-001`. It extends the existing `SessionLogIngestor` and persistence path; it does not create a second session-log store.

## Public Contracts

- `TranscriptSourceKind`: `Auto`, `Claude`, `Codex`, `Grok`, `Cline`, `Copilot`, `OpenCode`.
- `TranscriptCompatibilityProfile`: `None`, `Claude`, `Codex`, `Grok`.
- Shared contracts: `ITranscriptBundleDetector`, `ITranscriptSourceAdapter`, `ITranscriptProfileProjector`, `ITranscriptIngestionService`.
- HTTP path ingestion: `POST /mcpserver/sessionlog/ingest/path`.
- HTTP upload ingestion: `POST /mcpserver/sessionlog/ingest/upload`.
- Typed client methods: `IngestTranscriptPathAsync` and `IngestTranscriptUploadAsync`.
- REPL methods: `repl.sessionlog.ingestTranscripts` and `repl.sessionlog.normalizeTranscripts`.
- MCP tools: `sessionlog_ingest_path` and `sessionlog_normalize_path` with required `workspacePath`.

## Neutral Event Model Requirements

The neutral model must preserve native identity, ordering, timestamps, source roles, content blocks, reasoning, tool calls, tool results, model usage, failures, workspace metadata, subagent references, and source provenance. Missing semantic fields remain absent and produce diagnostics. Deterministic derived IDs must be marked as derived.

## Sanitized Fixture Inventory

The initial fixture set lives under `tests/McpServer.Support.Mcp.Tests/Fixtures/Transcripts` and is intentionally small so Slice 1 tests can stay fast.

- Claude: `claude/basic.jsonl` covers user/assistant turns and a tool-use/tool-result pair.
- Codex: `codex/basic.jsonl` covers request/response events, tool call/result, reasoning, usage, and workspace metadata.
- Grok: `grok/basic.jsonl` covers message events, reasoning text, and a failed tool result.
- Cline: `cline/session.json`, `cline/messages.json`, and `cline/export.jsonl` cover paired native JSON plus export JSONL.
- Copilot: `copilot/session-001/metadata.json` and `copilot/session-001/events.jsonl` cover a session folder with event stream metadata.
- OpenCode: `opencode/export.jsonl` and `opencode/store-schema.sql` cover JSONL export and read-only SQLite snapshot schema planning.

## Slice Gates

Slice 1 creates shared discovery, bounded readers, diagnostics, neutral events, and mocked orchestration. It must start with consumer contract tests and one failing real-service fixture test. It may close only when the complete current-plus-prior transcript unit scope has zero failures and zero skips.

Slice 2 through Slice 8 add source adapters and projectors one provider at a time, preserving the same red, green, refactor loop. Each source slice must cover valid fixtures, malformed records, unknown events, incomplete turns, ordering, tool pairing, diagnostics, and deterministic output.

Slice 9 adds HTTP, upload, typed client, MCP tool, and REPL transport contracts. It cannot start until the core service exposes stable request and receipt models.

Slice 10 verifies Claude, Codex, and Grok plugins do not expose transcript ingestion helpers, skills, or endpoint shortcuts. Models must write session logs through the normal session-log tools themselves; transcript import stays on non-plugin server/client/REPL surfaces. Legacy parser code cannot be removed until non-plugin typed parity is proven by tests.

Slice 11 updates generated documentation, REPL inventory, plugin docs, prompt templates, and `AGENTS-README-FIRST.yaml` through object-first mutation and regeneration only.

## Security Acceptance Criteria

- Server-local path ingestion accepts only workspace-contained paths or configured provider transcript roots.
- Absolute paths outside the workspace/provider roots, traversal, symlink/reparse escapes, unsupported file types, duplicate canonical archive paths, ZIP traversal, links, and decompression ratios above 20:1 are rejected.
- Upload limits are enforced before parsing: 512 MiB request, 2 GiB expanded content, 10,000 archive entries, 256 MiB per source file, 8 MiB per JSONL line, 2,000,000 records per bundle, recursion depth 32.
- OpenCode SQLite ingestion uses a consistent backup snapshot and never writes to the source database or WAL files.

## Persistence Acceptance Criteria

- Canonical Session Log YAML is projected directly from the neutral model.
- Compatibility JSONL outputs are optional and are not reparsed to create the Session Log.
- Every import writes an `importRecovery` envelope under `{workspace}/.mcpServer/{agent}/failsafe/pending` before persistence, named from the source root ID plus source hash (`<rootId>.<sourceHash>.importRecovery.yaml`) so multi-root runs cannot overwrite pending recovery data.
- The failsafe file is deleted only after `persisted=true` and `degraded=false`.
- Run outputs and redacted artifacts are stored under `{workspace}/.mcpServer/{agent}/transcripts/runs/{runId}`.
- Raw upload staging is deleted after completion.

## Validation Gates

Before development deployment, run focused transcript tests, affected Repl.Core tests, Client tests, Support.Mcp tests, integration tests, `./build.ps1 Compile`, `./build.ps1 Test`, `./build.ps1 ValidateTraceability`, and `./build.ps1 SyncAgentPlugins`. Development deployment uses `./build.ps1 UpdateService`; staging and production require explicit human approval.
