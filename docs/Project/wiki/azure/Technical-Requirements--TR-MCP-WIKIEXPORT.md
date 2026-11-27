# TR-MCP-WIKIEXPORT

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-WIKIEXPORT-001

**docs/wiki.yaml export configuration loader and renderer integration** — Implement a typed YamlDotNet-backed docs/wiki.yaml loader, validation layer, and wiki renderer integration shared by database-backed and document-backed requirements services.
**Covered by:** FR: FR-MCP-WIKIEXPORT-001; TEST: TEST-MCP-WIKIEXPORT-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] RequirementsOptions exposes WikiConfigPath defaulting to docs/wiki.yaml, and services resolve it under the active workspace path or configured requirements root.
- [x] RequirementsWikiExportConfig models deserialize docs/wiki.yaml with YamlDotNet into typed objects, with no line-based or regex YAML parsing.
- [x] Validation rejects unsupported schema values, missing required fields, duplicate document ids, duplicate targets per platform, path traversal, reserved managed target files, unsupported generated sources, invalid platform names, missing source files, and navigation/document mismatches.
- [x] Both RequirementsDatabaseDocumentService.GenerateWikiAsync and RequirementsDocumentService.GenerateWikiAsync share the same config loader and renderer path.
- [x] The exporter validates all configured content before calling RequirementsDocumentExportWriter so invalid config leaves existing export files unchanged.
- [x] Generated platform side files remain managed by the exporter, including .mcp-requirements-manifest.json, GitHub _Sidebar.md and _Footer.md, and Azure .order files.
- [ ] An optional workspace Markdown footer template supplies exactly one visible footer per exported content page, including generated, static, DocFX and group pages. GitHub uses its managed footer and the legacy format renders it inline. Sidecars and non-page assets are preserved. The shipped default credits QBrain and links to https://github.com/sharpninja/McpServer.
- [ ] An absent footer setting preserves the legacy layout and uses the QBrain attribution in the GitHub footer. A configured missing, empty, non-Markdown, escaping, or delimiter-conflicting template fails before any export file changes.
- [ ] Repeated exports produce one footer without changing inputs, publication Home keeps the footer after documentation links, and wiki imports preserve canonical records, acceptance criteria and mappings even when footer text resembles requirement content.
- [ ] When enabled, deterministic, case-insensitive grouping is based on whole leading ID segments before the first digit-leading segment within each requirement kind. At least two distinct IDs are required. Invalid prefixes and singletons remain on canonical pages.
- [ ] Each qualifying group has one safe read-only child page per eligible platform, one parent link and a link back. Canonical parent filenames, original member records and existing anchors are retained.
- [ ] Manifests, navigation, ZIP and cleanup consume the same platform inventory. Collisions fail before writes and repeat exports remove obsolete group pages and links. Absent or disabled grouping preserves the existing layout.
- [ ] Group pages render full typed member records and criteria with the same mapping context as the parent. Group artifacts never supply imported requirements or affect source selection when canonical inputs and timestamps are unchanged.

## TR-MCP-WIKIEXPORT-002

**Marker writer default wiki.yaml serializer** — Extend MarkerFileService.WriteMarkerAsync with an idempotent default wiki.yaml writer that builds typed objects and serializes them with YamlDotNet before writing docs/wiki.yaml.
**Covered by:** FR: FR-MCP-WIKIEXPORT-002; TEST: TEST-MCP-WIKIEXPORT-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] MarkerFileService computes the default wiki config path as workspace-root/docs/wiki.yaml and creates the docs directory when needed.
- [x] MarkerFileService checks for an existing docs/wiki.yaml before writing and skips creation when the file already exists.
- [x] Default wiki config is represented by typed or dictionary objects and serialized with the existing YamlDotNet serializer.
- [x] The default config targets Home.md, Functional-Requirements.md, Technical-Requirements.md, Testing-Requirements.md, TR-per-FR-Mapping.md, and Requirements-Matrix.md on both platforms.
- [x] A failure to create the default wiki.yaml is logged through the marker writer warning path and does not leave a partially written file.

## TR-MCP-WIKIEXPORT-003

**Dump schema and export flag** — schemaVersion mcp-wiki-dump/v1. SHA-256 per table plus root hash. Flag default off. tables[] uses McpDbContext DbSet names.
**Covered by:** FR: FR-MCP-WIKIEXPORT-003; TEST: TEST-MCP-WIKIEXPORT-003
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Without dump flag, export unchanged. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] With flag, versioned JSON keyed by workspace. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Dump TODO rows and requirement links match store. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] SHA-256 matches canonical UTF-8 JSON. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Use the existing ISessionLogSanitizer.SanitizeString rules for text and a bounded recursive structured-value projector for non-session entities. Parse recognized JSON fields. Strip secret-key values. Decode bounded Base64 text before inspection. Omit uninspectable binary or malformed encoded payloads with non-echoing diagnostics. Record every allowed redaction as table, row identity hash, field path, and rule ID. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-MCP-WIKIEXPORT-004

**Import remap and dump parameter binding** — Bind --dump to WorkspaceController.CreateAsync. Remap workspace id, paths, FKs. Idempotent re-import. Reject malformed dump, version mismatch, missing tables, unsafe path.
**Covered by:** FR: FR-MCP-WIKIEXPORT-004; TEST: TEST-MCP-WIKIEXPORT-004
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Hydrates TODOs from dump not todo.yaml. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] After remap, old source workspace IDs are absent from every destination operational column identified by the portability policy registry. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Malformed dump rejected. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Idempotent reimport no duplicate TODOs. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Introduce durable WorkspaceImportReceipt and WorkspaceImportEntityMap storage. Reserve by destination canonical path plus dump root hash. Query an identical receipt before WorkspaceService.CreateDatabaseAsync duplicate handling or any fresh-import filesystem precondition. Stage receipt-owned import artifacts beneath <workspace>/.mcpServer/imports/<receiptId>. Place registration, hydration, remaps, entity maps, and transition to CommittedPendingActivation in one EF transaction. Run projection/init/start/notification as idempotent post-commit activation stages. Preflight only the specific service-owned generated/output paths each stage needs. Record stage, attempt, sanitized error code, and ownership plus final-byte hash for each generated path created by the receipt. Never overwrite an unknown or pre-existing marker/file, and never claim atomicity across the database, filesystem, and process manager. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Apply the portability matrix. Retain source keys only in immutable import provenance and explicitly historical audit fields. Remap destination operational IDs deterministically. Clear leases and executable ownership. Disable imported runtime configurations. Mark queued/replayable rows imported-inert. Require explicit post-import reconfiguration before any credentialed or executable feature can run. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-MCP-WIKIEXPORT-005

**todo.yaml archive and conflict diagnostics** — Discovery classifies generated/shadow/deprecated. Archive is copy then optional remove with report. Conflict diagnostic includes both paths.
**Covered by:** FR: FR-MCP-WIKIEXPORT-005; TEST: TEST-MCP-WIKIEXPORT-005
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] todo.yaml not source of truth when dump present. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Cleanup archives with evidence, no silent delete. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Dump and yaml conflict: dump wins, diagnostic names both. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
