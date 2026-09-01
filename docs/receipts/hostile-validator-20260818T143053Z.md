# Hostile Validator Receipt

TimestampUtc: 2026-08-18T14:30:53Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: class 1 (project requirement work; MCP-PRODUCTS-001 Phase 1 / H1-green only). Not Phase 2-5. Not MCP-PRODUCTS-001 done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md).
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.93.0; .version 1.93.0)
Marker: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
Marker signature: Test-MarkerSignature True (pwsh, MarkerFile param, marker-resolver.ps1)
Health (this review): nonce 35cf4b82c264402b84bcf234eebd9a1e echoed exactly; status Healthy; version 1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e; storage=reachable
SessionId: GrokCode-20260818T142439Z-h1-green-products
RequestId: req-20260818T142439Z-001-hostile-h1-green-products
turnId: 41727
planFile: docs/plans/mcp-products-001.md
todoId: MCP-PRODUCTS-001
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-ran the focused Product filter and the official FullyQualifiedName~Product gate, grepped ProductEntity/IProductService/AddProductsStorage/SessionLogs, read entities, McpDbContext, every product handler, ProductCqrsHelpers, both host registrations, the three non-Designer AddProductsStorage migrations, and the Phase 1 test files, queried todo_get plus FR/TR/TEST/mappings through native MCP tools, and re-read the approved plan plus the H0/H1-red receipts. Implementer chat and the implementer product-unit.txt copy were not trusted.

This review did not implement product features. This review wrote only this receipt pair and the MCP review turn.

Accuracy rating: 95/100. Test counts, greps, handler persist, migration sources, TODO Done=false, and store ACs were re-verified on this pass.
Completeness rating: 93/100. Surfaces A-D and the named H1-green attacks were evaluated. Did not run the full unit suite (H1-green gate is the Product filter). Did not spin live PostgreSQL/SQL Server harnesses; H1-green all-provider bar remains compiled three-provider sources plus SQLite empty+predecessor apply.

## Classification

Class 1. Phase 1 green implementation for MCP-PRODUCTS-001 (FR-MCP-PRODUCT-001, FR-MCP-PRODUCT-002, FR-MCP-PRODUCT-004 isolation/outsider, TR-MCP-PRODUCT-MODEL-001, TR-MCP-PRODUCT-API-001 CQRS-only slice, TR-MCP-PRODUCT-AUTH-001 auth slice, TEST-MCP-PRODUCT-001, TEST-MCP-PRODUCT-005). Surface C applies. Byrd v4 is scored at this H1-green gate: H0 AGREE and H1-red AGREE already exist; this gate requires the Phase 1 tests green with zero skips and no public IProductService facade.

Prior H1-red AGREE: docs/receipts/hostile-validator-20260818T140630Z.md
Prior H1-red DISAGREE: docs/receipts/hostile-validator-20260818T133728Z.md
H0 AGREE: docs/receipts/hostile-validator-20260818T132341Z.md

## Claims reviewed

### A Requested

A1. Phase 1 green is implemented: ProductEntity and ProductWorkspaceMembershipEntity exist; McpDbContext has Products and ProductWorkspaceMemberships DbSets with NO Workspace query filter; unique Key filtered where IsDeleted is false.
Verdict: PASS
Evidence: Read src/McpServer.Storage/Entities/ProductEntity.cs and ProductWorkspaceMembershipEntity.cs. McpDbContext.cs L246-249 DbSets. Product configuration L942-963: unique index on Key with HasFilter(ProductKeyUniqueIndexFilter()). ProductKeyUniqueIndexFilter L1074-1081 returns "IsDeleted" = FALSE / [IsDeleted] = 0 / "IsDeleted" = 0 by provider. Workspace HasQueryFilter list L965-1016 does not include ProductEntity or ProductWorkspaceMembershipEntity. ProductEntities_DoNotHaveWorkspaceQueryFilters asserts filter key "Workspace" is absent. SoftDelete named filter is present via ApplySoftDeleteQueryFilters; that is not a Workspace filter.

A2. CQRS handlers under src/McpServer.Support.Mcp/Products/ persist via McpDbContext. No public IProductService facade. Private helper ProductCqrsHelpers is used only by those handlers. AddProductCqrs registers handlers on HTTP Program.cs and STDIO McpStdioHost.
Verdict: PASS
Evidence: Every mutating handler (Create/Update/Delete/AddMember/RemoveMember) injects McpDbContext and calls SaveChangesAsync. Get/List/ListMembers query the same context. Grep IProductService on *.cs: zero hits. ProductCqrsHelpers is internal static; src grep hits only that file plus the eight command/query handlers. AddProductCqrs in ProductServiceCollectionExtensions.cs registers the eight handlers. Program.cs L455 and McpStdioHost.cs L302 both call AddProductCqrs(). Observation: the helper is internal, not a C# private class; it is still not a public application facade.

A3. Product keys validate ^PROD-[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$. Invalid keys 400. Duplicate non-deleted key 409. Owner add/remove, member self-leave, member cannot remove another (403). Non-owner update/delete/add 403. Outsider get 404. Soft-delete hides from get and default list and stops membership reads. Removed member get is 404.
Verdict: PASS
Evidence: ProductCqrsHelpers.ProductKeyRegex is exactly that pattern. Independent focused run asserted all named cases green (see A5). Handler tests now seed via ProductHandlerTestContext (in-memory SQLite + registered workspaces) and RemovedMember actually calls Remove before Get. Status tokens are Result.Error prefixes 400:/403:/404:/409: (Phase 1 has no REST adapter yet).

A4. AddProductsStorage migrations exist for Sqlite (20260818142008), SqlServer (20260818142028), PostgreSql (20260818142039). Those *AddProductsStorage.cs files (non-Designer) create only Products + ProductWorkspaceMemberships and do not mention SessionLogs.
Verdict: PASS
Evidence: Read all three non-Designer files. Each Up() CreateTable Products then ProductWorkspaceMemberships, unique Key index with IsDeleted filter, then Down() drops those two tables. Grep SessionLogs in each file: no matches. Filenames match the claimed timestamps.

A5. Focused filter Failed 0, Passed 24, Skipped 0. Copy at C:\Users\kingd\AppData\Local\Temp\grok-goal-18747a5af710\implementer\product-unit.txt was not trusted.
Verdict: PASS
Evidence: Independent re-run 2026-08-18T14:25:16.1916634Z to 14:25:39.9977679Z. Command: `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~McpServer.Support.Mcp.Tests.Products|FullyQualifiedName~McpServer.Support.Mcp.Tests.Storage.ProductEntityTests|FullyQualifiedName~McpServer.Support.Mcp.Tests.Storage.ProductMigrationApplyTests`. Summary: Test Run Successful. Total tests: 24 Passed: 24. EXIT=0. No Skipped line (zero skips). --list-tests listed exactly those 24 cases. No Skip attributes in the four Phase 1 test files. Implementer copy LASTWRITE 2026-08-18T14:22:05.9181453Z also said Failed 0 Passed 24 Skipped 0; this review's own run is the receipt.

A6. MCP-PRODUCTS-001 remains Done=false. Phase 2+ (effective share, REST/MCP/client, product-requirements context, full suite) is not claimed complete.
Verdict: PASS
Evidence: Native todo_get via /mcp-transport tools/call. Id=MCP-PRODUCTS-001 Done=false CompletedDate=null DoneSummary=null. All five ImplementationTasks Done=false. Remaining still says "H0 hostile required before Phase 1. No product implementation started." (stale note, not a done-state lie). Phase 2+ files ABSENT: GetProductEffectiveRequirementsQueryHandlerTests.cs, ProductsControllerTests.cs, ProductClientTests.cs, ProductRequirementContextTests.cs, ProductsController.cs, ProductClient.cs, FwhMcpTools.Products.cs.

Honesty notes (scored, not ignored):
- Handler tests now inject an in-memory SQLite fixture: CONFIRMED (ProductHandlerTestContext).
- Empty-migrate test no longer asserts SessionLogs.AgentExecutablePath: CONFIRMED (Migrate_FromEmpty only asserts Products tables/columns).
- Predecessor migrate still EnsureColumn + asserts the column remains: CONFIRMED (ProductMigrationApplyTests L50-67).
- Live PostgreSQL/SQL Server Migrate() apply was not run: CONFIRMED. Plan fallback is compiled three-provider sources plus SQLite empty+predecessor apply.
- Full ./build.ps1 Test was not run: CONFIRMED. Not required to exit H1-green.
- Audit rows come from McpDbContext durable-entity SaveChanges: CONFIRMED (AppendAuditRows / IsDurableAuditableEntry includes *Entity types with IsDeleted, including ProductEntity). No dedicated product audit helper.

### B Workspace rules

B1-honesty. Claims match artifacts.
Verdict: PASS
Rule: AGENTS.md honesty; profile accuracy-first-verify-sources.
Evidence: Entity/DbSet/filter/regex/handler persist/migration/test-count/TODO claims re-checked. Implementer did not claim Phase 2+, full suite, live PG/SQL Server apply, or MCP-PRODUCTS-001 done.

B2-receipts. Machine-verifiable evidence re-run.
Verdict: PASS
Rule: Always bring the receipts.
Evidence: This review's two dotnet test transcripts, --list-tests, greps, file reads, todo_get, requirements_list, Test-MarkerSignature, health nonce, tools/list (106 tools).

B3-MCP-only storage.
Verdict: PASS
Rule: MCP is the only interface to TODO/session/requirements.
Evidence: TODO, requirements, and session log used native sessionlog_*/todo_*/requirements_* via Streamable HTTP /mcp-transport after initialize. This review did not read or write docs/todo.yaml or session-log storage files.

B4-lab PowerShell / no Python.
Verdict: PASS
Rule: no-python-lab; pwsh.exe only.
Evidence: pwsh.exe -NoProfile path for signature, health, inventory, test runs, MCP transport client, and JSON parse. No python/py invocation.

B5-look-before-delete.
Verdict: PASS
Evidence: No deletes. Review-only plus receipt create.

B6-Byrd v4 phase-order at H1-green.
Verdict: PASS
Rule: hostile-phase-gates; implementation only after AC/tests are correct; phase suite green to exit; score at the inter-phase gate.
Evidence: H0 AGREE (20260818T132341Z) exists. H1-red AGREE (20260818T140630Z) exists. AddProductsStorage timestamps 20260818142008/42028/42039 are after that H1-red receipt. Focused 24/0/0 and official plan gate FullyQualifiedName~Product 25/0/0 are green. Full ./build.ps1 Test is the H5-done gate, not H1-green.

### C Requirements

C1. FR/TR/TEST exist for this work.
Verdict: PASS
Evidence: Native requirements_list. FR_TOTAL=277 TR_TOTAL=406 TEST_TOTAL=422. PRODUCT subset present: FR-MCP-PRODUCT-001..005; TR-MCP-PRODUCT-API/AUTH/CTX/MODEL/SHARE-001; TEST-MCP-PRODUCT-001..006.

C2. Structured AC exist on the Phase 1 FRs and remain unsatisfied (correct; slice is not product-done).
Verdict: PASS
Evidence: FR-001 ac-1..ac-5 isSatisfied=false. FR-002 ac-1..ac-4 isSatisfied=false. FR-004 ac-1 outsider get-product is 404 isSatisfied=false. FR-003/005 and FR-004 ac-2/ac-3 remain Phase 2+ and out of H1-green scope. TR/TEST still have empty structured AC arrays; AC text lives in TR body and TEST Condition (same as H0/H1-red).

C3. Phase 1 AC-covering tests exist and are now green (H1-green bar).
Verdict: PASS
Evidence: Same mapping as H1-red C3, now passing:
- FR-001 ac-1: HandleAsync_ValidProdMcpserverKey_ReturnsOwnerAndKey
- FR-001 ac-2: HandleAsync_InvalidKey_Fails (empty, mcpserver, prod-mcpserver, MCP-SERVER)
- FR-001 ac-3: Create_DuplicateProdMcpserverKey_FailsConflict
- FR-001 ac-4: HandleAsync_NonOwnerUpdate_FailsForbidden, HandleAsync_NonOwnerDelete_FailsForbidden
- FR-001 ac-5 / TEST-001: HandleAsync_SoftDelete_HidesFromDefaultList (also hide-from-get and stop-membership-reads)
- FR-002 ac-1: HandleAsync_UnknownWorkspace_Fails (400). Implementation also rejects !IsEnabled; disabled-but-registered is still not a named Phase 1 case (H1-red lock).
- FR-002 ac-2: HandleAsync_OwnerAddsRegisteredWorkspace_IncludesMember
- FR-002 ac-3: HandleAsync_MemberLeavesSelf_RemovesOnlyCaller
- FR-002 ac-4: HandleAsync_RemovedMember_LosesProductReads (now seeds, removes, then Get 404)
- TEST-001 isolation: ProductEntities_DoNotHaveWorkspaceQueryFilters
- FR-004 ac-1: HandleAsync_OutsiderGet_IsNotFound
- TEST-005 empty: Migrate_FromEmpty_CreatesProductTables
- TEST-005 production-shaped: Migrate_FromPredecessor_WithSessionLogsAgentColumns_CreatesProductTables
- TEST-005 three-provider sources: AddProductsStorageMigration_Source_ExistsForAllProviders_AndHasNoSessionLogsTableOps
- TR-MODEL unique filtered Key + composite membership PK: McpDbContext + all three migrations
TR-AUTH body also says "degraded transactions reject create/add-member". That sentence is not in the H1-green attack list, was not a named H1-red case, and ITurnTransactionCoordinator in this codebase lives on adapters (Phase 3), not CQRS handlers. Not scored as an H1-green gap.

C4. Mappings FR to TR/TEST exist.
Verdict: PASS
Evidence: requirements_list type=mapping:
- FR-001 -> TR API, AUTH, MODEL + TEST 001,003,004,005
- FR-002 -> TR AUTH, MODEL + TEST 001,003
Matches the approved plan sets. TEST-003/004 remain Phase 3 files and are not required to exist at H1-green.

C5. New product behavior has FR/TR/TEST (not deferred). Implementer did not mark FR/TR/TEST completed.
Verdict: PASS
Evidence: Store IDs from H0 remain pending. TODO still links FR-001..005 and the five TRs. Status fields were not flipped to completed.

### D Plan holistically

D1. H1-green checkpoint is complete. Full MCP-PRODUCTS-001 DoD is not claimed.
Verdict: PASS
Evidence: Approved plan docs/plans/mcp-products-001.md SHA256 E233F9E34BCA0A7176284FB0DE0E11BF2A186D04F479CF7C8E2CC089F72FB805 (unchanged since H0/H1-red). Phase 1 green text: entities, migrations, CQRS handlers, private helper only, no public IProductService, audit via handler SaveChanges. H1-green attack text: handlers not a public service facade; key validation; owner/member rules; migrations; Phase 1 gate green with zero skips. Met.
Official plan gate `FullyQualifiedName~Product` independently re-run 2026-08-18T14:27:48.5211227Z to 14:28:19.6739000Z: Test Run Successful. Total tests: 25 Passed: 25 EXIT=0 (the extra case is UseCaseExpandedScopeTests.ProductKey_AssignAndListByProduct, the pre-existing FR-MCP-USECASE-009 hook).
Session goal plan.md SHA256 0D73B5C6B754DEC494F4EAB445AD2A6EEB73D2F2923260366E07D4A9351FD92C is unchanged. Its task checkboxes remain `[ ]`. It still says MCP-PRODUCTS-001 must stay Done=false until H5-done AGREE. Stale lines ("handlers still return not-implemented", "15 Product-filter tests") are leftover finish-plan text, not an implementer H1-green done claim.

D2. Did not start Phase 2+ or mark the TODO done.
Verdict: PASS
Evidence: A6.

## H1-green named attacks

- Handlers are not a public IProductService facade: PASS
- Key validation PROD-* accept/reject: PASS
- Owner/member rules (add/remove/self-leave/member-cannot-remove-other/non-owner 403/outsider 404/removed-member 404): PASS
- Migrations exist for three providers and do not touch SessionLogs: PASS
- Phase 1 gate green with zero skips: PASS (tight 24/0/0 and official Product filter 25/0/0)

## Explicit FAIL list

None.

## UNKNOWN / unevaluated

- Full `./build.ps1 Test` not run. Not required to exit H1-green.
- Live PostgreSQL/SQL Server Database.Migrate() apply not run. H1-green bar is compiled three-provider source files plus SQLite empty+predecessor apply.
- TR-AUTH degraded-transaction fail-closed is not implemented on product handlers and has no Phase 1 test. Deferred to adapter/Phase 3 unless a later gate reopens it. Not used as an H1-green FAIL.
- TODO Remaining text still mentions H0 / no implementation. Not used as a done-state claim.
- Disabled-but-registered add is implemented but still untested. Same H1-red lock.

## Session-log persistence proof

Native MCP tools over POST http://PAYTON-LEGION2:7147/mcp-transport (initialize, tools/list, tools/call). Agent GrokCode, workspace F:\GitHub\McpServer. tools/list unique name count 106 including sessionlog_open, sessionlog_begin_turn, sessionlog_dialog, sessionlog_complete_turn, sessionlog_query, todo_get, requirements_list.

- initialize protocolVersion 2025-03-26
- sessionlog_open GrokCode-20260818T142439Z-h1-green-products created=true
- sessionlog_begin_turn requestId req-20260818T142439Z-001-hostile-h1-green-products turnId=41727 status=in_progress
- sessionlog_dialog success totalDialogItems=4
- sessionlog_replace_section actions replaced=true (7 actions)
- sessionlog_replace_section designDecisions failed (server expects string items; decisions are in dialog category=decision and actions type=design_decision)
- sessionlog_complete_turn success turnId=41727 status=completed
- Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer todoId=MCP-PRODUCTS-001 limit=10. First item: sessionId GrokCode-20260818T142439Z-h1-green-products, turnCount=1, requestId req-20260818T142439Z-001-hostile-h1-green-products, turn status=completed, response starts with OverallVerdict AGREE, 7 actions, 4 dialog items (one category=decision). Session-level status remains in_progress (expected; session not closed). Query by text=sessionId returned empty; todoId and from=2026-08-18T14:20:00Z both returned this session.

## Files written by this review

- docs/receipts/hostile-validator-20260818T143053Z.md
- docs/receipts/hostile-validator-20260818T143053Z.json
