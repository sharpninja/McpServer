# FR-MCP-WIKIEXPORT

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-WIKIEXPORT-001 Configurable requirements wiki export tree

Requirements wiki export must optionally discover docs/wiki.yaml in the active workspace and use it as the authoritative wiki document tree, navigation tree, optional home template, and flattened document list for GitHub and Azure wiki exports while preserving current output when the file is absent.
Scope: layer-1+
**Acceptance Criteria:**
- [x] When docs/wiki.yaml is absent, GenerateWikiAsync produces the same canonical Home, requirements documents, manifests, GitHub _Sidebar.md and _Footer.md, and Azure .order output as before.
- [x] When docs/wiki.yaml is present, the documents collection defines the flattened base export list and every configured navigation document reference resolves to exactly one declared document. Enabled requirement grouping adds derived child pages to the computed platform inventory.
- [x] Declared documents can use generated requirement sources and workspace-relative Markdown source files, and their target paths are written under each eligible platform output folder.
- [x] The optional home template renders generatedAtUtc, navigation, and documents tokens; absent home templates fall back to a generated home page derived from the navigation tree.
- [x] Platform filters control whether declared documents appear in GitHub, Azure, or both, and generated manifests list only files exported for that platform.
- [x] Invalid docs/wiki.yaml content fails with actionable validation errors before any export file is modified.
- [x] GitHub sidebar and Azure order files are generated from the complete navigation tree, including nested folder navigation.
- [ ] An optional workspace Markdown footer template supplies exactly one visible footer per exported content page, including generated, static, DocFX and group pages. GitHub uses its managed footer and the legacy format renders it inline. Sidecars and non-page assets are preserved. The shipped default credits QBrain and links to https://github.com/sharpninja/McpServer.
- [ ] An absent footer setting preserves the legacy layout and uses the QBrain attribution in the GitHub footer. A configured missing, empty, non-Markdown, escaping, or delimiter-conflicting template fails before any export file changes.
- [ ] Repeated exports produce one footer without changing inputs, publication Home keeps the footer after documentation links, and wiki imports preserve canonical records, acceptance criteria and mappings even when footer text resembles requirement content.
- [ ] When enabled, deterministic, case-insensitive grouping is based on whole leading ID segments before the first digit-leading segment within each requirement kind. At least two distinct IDs are required. Invalid prefixes and singletons remain on canonical pages.
- [ ] Each qualifying group has one safe read-only child page per eligible platform, one parent link and a link back. Canonical parent filenames, original member records and existing anchors are retained.
- [ ] Manifests, navigation, ZIP and cleanup consume the same platform inventory. Collisions fail before writes and repeat exports remove obsolete group pages and links. Absent or disabled grouping preserves the existing layout.
- [ ] Group pages render full typed member records and criteria with the same mapping context as the parent. Group artifacts never supply imported requirements or affect source selection when canonical inputs and timestamps are unchanged.

## FR-MCP-WIKIEXPORT-002 Default wiki export configuration during marker generation

When MCP Server writes a workspace marker file, it must also create a default docs/wiki.yaml if the workspace does not already provide one, so wiki export has an explicit document tree for future GitHub and Azure exports.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Marker generation creates docs/wiki.yaml under the workspace root when that file is absent.
- [x] Marker generation leaves an existing docs/wiki.yaml byte-for-byte unchanged.
- [x] The default wiki.yaml uses schema mcp-wiki-export/v1 and declares a complete flattened document list for generated home, functional, technical, testing, mapping, and matrix documents.
- [x] The default navigation tree references every declared document exactly once and is valid for both GitHub and Azure wiki export.
- [x] The generated default config is written through object serialization, not line-based YAML construction.

## FR-MCP-WIKIEXPORT-003 Optional wiki export JSON dump

Optional --include-dump on wiki export emits versioned JSON dump of MCP tables keyed by source workspace, including TODO rows and requirement links. No-flag behavior is unchanged.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Without flag, export is byte-equivalent to current wiki zip/tree behavior (no dump file). (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] With flag, dump has schemaVersion, exportedAtUtc, sourceWorkspaceKey, sourceWorkspacePath, tables[], todos[], todoRequirementLinks[], SHA-256 hashes, diagnostics. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Dump TODO rows and requirement links match store query for that workspace. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] The dump policy registry covers every live McpDbContext DbSet and fails when model and registry differ. Output excludes unrelated workspace rows and global secrets. Scalar, nested JSON, headers, encoded textual payloads, diagnostics, prompts, outputs, commands, and free text are sanitized in the outbound projection without mutating stored rows. Integrity hashes are computed over the final canonical UTF-8 bytes after redaction. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-MCP-WIKIEXPORT-004 add-workspace dump hydration

add-workspace --dump on WorkspaceClient.CreateAsync / POST /qbrainai/workspace hydrates TODOs from dump, remaps workspace identity, rejects malformed dumps with no partial write.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Hydrated TODOs come from dump rows. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Old source workspace IDs and paths do not appear in destination operational ownership, authorization, routing, active-process, replay, lease, executable-owner, or operational foreign-key columns. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Malformed dump, version mismatch, missing tables, unsafe path: rejected, no partial write. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] An identical durable receipt is resolved before active-workspace duplicate rejection and before fresh-import filesystem-collision rules. Workspace registration and all hydrated database rows commit in one transaction. Cancellation or failure before commit leaves no workspace or imported domain row. Projection, workspace initialization, marker creation, process start, and change notification occur only after commit. A post-commit failure remains CommittedPendingActivation or ActivationFailed and is durably resumable. An existing nonempty repository checkout is a valid destination. Every pre-existing repository and user file is preserved. Only conflicting service-owned generated/output paths block or defer activation. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Immutable non-operative import provenance may retain the original source workspace ID/path, is explicitly historical, and cannot participate in destination authorization, routing, replay, ownership, or execution. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-MCP-WIKIEXPORT-005 todo.yaml deprecation

todo.yaml is deprecated as TODO source of truth. When dump and todo.yaml both exist, dump wins and a diagnostic names both. Cleanup archives to .mcpServer/archive/todo-yaml/ with evidence. Never silent delete.
Scope: layer-1+
**Acceptance Criteria:**
- [x] todo.yaml is not used as TODO source when dump is present. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Cleanup archives to .mcpServer/archive/todo-yaml/ with evidence; never silent delete. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Conflict diagnostic names both paths. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; WikiDumpG7OverlayTests.cs; DocsSyncG8OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

