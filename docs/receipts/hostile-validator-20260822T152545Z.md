# Hostile validator: PLAN-PLUGINHANDOFF-001 Phase G-RED

TimestampUtc: 2026-08-22T15:25:45Z
ValidatorIdentity: GrokSubagentHostile
WorkClass: 1 (project implementation, G-RED phase gate only)
add-profile: executed yes. Read 18 non-skill profile markdown files under C:\Users\kingd\.claude\profile\ (excluded add-profile.grok.md). Also read C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md.
E-red / D4: out of scope.

Active plan: docs/plans/PLAN-PLUGINHANDOFF-001.md section 11
Requirement IDs: FR-MCP-WIKIEXPORT-003 / 004 / 005, TR-MCP-WIKIEXPORT-003 / 004 / 005, TEST-MCP-WIKIEXPORT-003 / 004 / 005
Worktree: C:\Users\kingd\.grok\worktrees\github-mcpserver\subagent-01a029d1-24e7-7453-b027-93176ddfd033
MAIN receipts: F:\GitHub\McpServer\docs\receipts

Implementer receipt (not treated as proof): docs/receipts/g-red-20260822T143512Z.md
G0 (re-read, bound=no): docs/receipts/g0-dump-binding-20260822T132429Z.md

## Trust / session

- Marker F:\GitHub\McpServer\AGENTS-README-FIRST.yaml HMAC-SHA256 Test-MarkerSignature: True
- Health nonce b0e083c1a711474f986212582d190b14 echoed exactly. status Healthy.
- Plugin mcpserver-grok-plugin 1.105.0 from F:\GitHub\mcpserver-grok-plugin\.version and .claude-plugin\plugin.json
- Isolated CacheRoot F:\GitHub\McpServer\.mcpServer\grok-subagent-hostile-g-red (did not reuse GrokCode hourly cache)
- Intended sessionId GrokSubagentHostile-20260822T152013Z-g-red-hostile
- Persisted plugin sessionId GrokCode-20260822T152016Z-plugin-session (plugin host remaps grok plugin agent to GrokCode)
- Turn requestId req-20260822T152013Z-001-g-red-hostile-validate
- queryHistory: session title "Hostile G-red validation Phase G named tests", turnCount 1, tags include GrokSubagentHostile and FR-MCP-WIKIEXPORT-003/004/005, filesModifiedCount 3, lastUpdated 2026-08-22T15:24:13Z
- Local current-turn.yaml status completed, auditActions 3, auditDialog 3, auditDecisions 1

Evidence: docs/receipts/_hostile-g-red-20260822T151548Z/trust.json, session-bootstrap.json, session-complete.json, hostile-g-red.trx, hostile-run-summary.json, hostile-dotnet-test.txt

## A. Requested validation

### A1. All 11 section-11 named tests plus CreateAsync_DumpParam_BoundOnWorkspaceCreateRequest exist
PASS.

Worktree file tests/McpServer.Support.Mcp.Tests/Services/WikiDumpPhaseGTests.cs. All 12 names present as [Fact] methods. NamedCount 12, FactCount 12. MAIN workspace grep WikiDumpPhaseGTests in *.cs: no matches (worktree-only).

Section 11 names:
- WikiExport_WithoutDumpFlag_UnchangedBehavior
- WikiExport_WithDumpFlag_WritesVersionedJsonKeyedByWorkspace
- Dump_ContainsTodoRowsAndRequirementLinksMatchingStore
- Dump_Sha256_MatchesCanonicalUtf8Json
- AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml
- Import_RemapsWorkspaceIdAndPaths_OldIdAbsent
- Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected
- Import_IdempotentReimport_NoDuplicateTodos
- TodoYaml_NotSourceOfTruth_WhenDumpPresent
- TodoYaml_Cleanup_ArchivesWithEvidence_NoSilentDelete
- DumpAndTodoYaml_Conflict_DumpWins_DiagnosticNamesBoth

G0 extra:
- CreateAsync_DumpParam_BoundOnWorkspaceCreateRequest

### A2. Independent rebuild+rerun of those 12 names: Failed 12 Passed 0 Skipped 0
PASS.

Hostile command (not the implementer script): pwsh.exe -NoProfile -NonInteractive -File docs/receipts/_hostile-g-red-20260822T151548Z/hostile-run-named.ps1
- Worktree project tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj -c Debug
- dotnet build --no-incremental BuildExit=0
- Filter: FullyQualifiedName~ each of the 12 names joined with |
- TRX F:\GitHub\McpServer\docs\receipts\_hostile-g-red-20260822T151548Z\hostile-g-red.trx
- Counters: total=12 executed=12 passed=0 failed=12 error=0 timeout=0 aborted=0 inconclusive=0 notExecuted=0 skipped=0
- TestExit=1
- VSTest log: Total tests: 12 Failed: 12

Failure messages (independent, not copied from implementer):
- Four wiki tests: IRequirementsDocumentService.GenerateWikiAsync must bind includeDump (wiki --include-dump).
- Eight CreateAsync/import/todo.yaml tests: Service WorkspaceCreateRequest.Dump must bind add-workspace --dump for CreateAsync.

Mocks: tests use Data Source=:memory: sqlite plus fake filesystem dump. No live 7147 in these unit tests.

### A3. No dump/import product code under src/ (still unbound Dump / includeDump). FAIL if G-green mixed in
PASS. Not G-green mixed in.

Independent src scan in the worktree (*.cs):
- includeDump / IncludeDump / include-dump / mcp-wiki-dump / DumpParam / archive/todo-yaml: 0 hits
- Client WorkspaceCreateRequest (WorkspaceModels.cs): no Dump property
- Service WorkspaceCreateRequest (IWorkspaceService.cs): no Dump property
- IRequirementsDocumentService.GenerateWikiAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default) : no includeDump
- RequirementsWikiExportRequest record: no IncludeDump
- IGenerateDocumentParams: Format, DocType, WorkspacePath only
- RequirementsWorkflow.GenerateDocumentAsync(string format, string docType, string? workspacePath = null, ...): no includeDump

Worktree git src diffs exist for other phases (ReplCommandDispatcher.cs, HandoffIngestionService.cs). Grep dump/Dump/includeDump in those files: 0 hits. Those are not wiki dump/import product code.

### A4. PLAN and MCP-WIKIEXPORT-001 remain done:false
PASS.

workflow.todo.get (plugin, not TODO.yaml):
- PLAN-PLUGINHANDOFF-001 done: false (requestId req-20260822T152030Z-4025)
- MCP-WIKIEXPORT-001 done: false (requestId req-20260822T152039Z-7d68)

### A5. FederationClient.RegisterWorkspaceAsync not used as the hydration bind
PASS.

- FederationClient.RegisterWorkspaceAsync still posts mcpserver/federation/proxies/{proxyId}/workspaces
- FederationWorkspaceRegistrationRequest: globalWorkspaceId, workspaceName, workspacePath, isEnabled, version, metadataJson. No Dump.
- WikiDumpPhaseGTests hydrates via WorkspaceService.CreateAsync(ServiceWorkspaceCreateRequest) after asserting Dump on that request type. No RegisterWorkspaceAsync call in the G-red tests.

## B. Workspace rules

### B1. Byrd v4 for this G-RED phase
PASS for this phase gate.

Requirements 003-005 exist in the MCP store with structured AC (isSatisfied false). Named tests exist and independently fail. No dump/import implementation under src/. This review is the G-red inter-phase hostile gate, not a done/green claim.

### B2. Receipts
PASS. Independent rebuild, TRX, todo_get, getFr/getTr/getTest/listMappings, src greps. Implementer receipt was not accepted as proof.

### B3. MCP-only storage
PASS. TODOs and requirements read through plugin workflow.todo.get / workflow.requirements.*. No direct TODO.yaml or session-log file edits for store mutation.

### B4. PowerShell / no Python
PASS. Hostile automation used pwsh.exe -NoProfile -NonInteractive only.

### B5. Honesty
PASS. Implementer G-red counts match independent TRX. Failures match unbound includeDump / Dump, consistent with G0 bound=no.

## C. Requirements

PASS. Class 1. Store (workflow.requirements.getFr/getTr/getTest/listMappings):

- FR-MCP-WIKIEXPORT-003 pending, AC1-AC3 isSatisfied false. Mapped to TR-MCP-WIKIEXPORT-003 and TEST-MCP-WIKIEXPORT-003.
- FR-MCP-WIKIEXPORT-004 pending, AC1-AC3 isSatisfied false. Mapped to TR-MCP-WIKIEXPORT-004 and TEST-MCP-WIKIEXPORT-004.
- FR-MCP-WIKIEXPORT-005 pending, AC1-AC3 isSatisfied false. Mapped to TR-MCP-WIKIEXPORT-005 and TEST-MCP-WIKIEXPORT-005.
- TEST-MCP-WIKIEXPORT-003/004/005 AC: named tests exist. Those names are on disk as Facts.

Tests encode the FR AC (byte-equivalent no-flag, schema fields, store match, hydrate from dump, remap, reject malformed, idempotent reimport, yaml not source, archive evidence, conflict diagnostic). They fail before those asserts because bindings are absent. That is G-RED, not missing AC coverage.

## D. Current plan holistically

PASS for the claimed G-RED step. Not a plan-complete or done:true claim.

Section 11: write every named G test first; hostile G-red AGREE that they are failing; then implement dump/import/deprecation. Do not implement import or deprecation before G-red. Bind hydration to WorkspaceClient.CreateAsync / POST /mcpserver/workspace. Federation proxy register stays out of scope.

Evidence: 11 section-11 names plus G0 CreateAsync consumer exist and fail. No dump/import product code. TODOs remain done:false. Plan DoD for later G-green / Phase H / closeout is not claimed and is not granted.

## FAIL list

None.

## UNKNOWN list

None that block the G-RED claims. Note only: plugin persist remapped sourceType to GrokCode; validator identity in tags and this receipt is GrokSubagentHostile. queryHistory session row remains status in_progress (session-level) while local current-turn.yaml is completed.

## Counts

PASS: 12 (A1 A2 A3 A4 A5 B1 B2 B3 B4 B5 C D)
FAIL: 0
UNKNOWN: 0

## OverallVerdict

AGREE

G-RED phase gate only. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-WIKIEXPORT-001 done. Do not start G-green in this review.
