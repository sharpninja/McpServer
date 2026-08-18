# Hostile Validator Receipt

TimestampUtc: 2026-08-18T15:02:00Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: class 1 (project requirement work; MCP-PRODUCTS-001 Phase 2 / H2-green only). Not MCP-PRODUCTS-001 done. Not Phase 3-5. Not full ./build.ps1 Test.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md).
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.93.0; .version 1.93.0)
Marker: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
Marker signature: Test-MarkerSignature True (pwsh, MarkerFile param, marker-resolver.ps1)
Health (this review): nonce f60fc58e16c0412683764728f653bdf5 echoed exactly; status Healthy; version 1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e; storage=reachable
SessionId: GrokCode-20260818T150052Z-h2-green-products
RequestId: req-20260818T150052Z-001-hostile-h2-green-products
turnId: 41755
planFile: docs/plans/mcp-products-001.md
todoId: MCP-PRODUCTS-001
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-ran the focused Product filter and the official Product plus RequirementScopeLayerServiceTests filter, read ProductShareHelper.cs and GetProductEffectiveRequirementsQueryHandler, grepped IProductService / ProductShareHelper / productScope, confirmed Phase 3-5 files absent, queried todo_get plus FR/TR/TEST/mappings through native MCP tools, and re-read the approved plan plus H0 / H1-red / H1-green / H2-red receipts. Implementer chat was not trusted.

This review did not implement product features. This review wrote only this receipt pair, collector scripts under docs/receipts/_hv-h2-green-*, and the MCP review turn.

Accuracy rating: 95/100. Test counts, helper/handler wiring, IProductService absence, TODO Done=false, REST still local-only, and store ACs were re-verified on this pass.
Completeness rating: 94/100. Surfaces A-D and the named H2-green attacks were evaluated. Did not run the full unit suite (H2-green gate is Product plus Requirement effective/scope, not ./build.ps1 Test). Did not issue a live update-fr mutation (not a Phase 2 named case).

## Classification

Class 1. Phase 2 green implementation for MCP-PRODUCTS-001 (FR-MCP-PRODUCT-003, FR-MCP-PRODUCT-004 outsider-effective / no sibling mutation, TR-MCP-PRODUCT-SHARE-001, TEST-MCP-PRODUCT-002). Surface C applies. Byrd v4 is scored at this H2-green gate: H0 / H1-red / H1-green / H2-red AGREE already exist; this gate requires handler-owned share, provenance, named ACs green, and the Product plus Requirement effective/scope filter Failed 0 Skipped 0.

Prior H2-red AGREE: docs/receipts/hostile-validator-20260818T144836Z.md
Prior H1-green AGREE: docs/receipts/hostile-validator-20260818T143053Z.md
Prior H1-red AGREE: docs/receipts/hostile-validator-20260818T140630Z.md
H0 AGREE: docs/receipts/hostile-validator-20260818T132341Z.md

## Claims reviewed

### A Requested

A1. GetProductEffectiveRequirementsQueryHandler persists/reads via McpDbContext and ProductShareHelper (internal, handler-owned). No public IProductService.
Verdict: PASS
Evidence: Read src/McpServer.Support.Mcp/Products/Queries/GetProductEffectiveRequirementsQuery.cs (LastWriteUtc 2026-08-18T14:53:20.2228036Z). Handler is `GetProductEffectiveRequirementsQueryHandler(McpDbContext db)` and calls `ProductShareHelper.GetEffectiveAsync(db, caller, query.LayerKey, scope, context.CancellationToken)`. Read ProductShareHelper.cs: `internal static class ProductShareHelper` with handler-owned GetEffectiveAsync that queries McpDbContext (local rows, memberships, sibling origin layers). Grep ProductShareHelper on *.cs: only that file plus the handler. Grep IProductService on *.cs: 0 hits. AddProductCqrs now registers GetProductEffectiveRequirementsQueryHandler. Program.cs L455 and McpStdioHost.cs L302 still call AddProductCqrs(). Observation: the helper is internal, not a C# private class; same pattern H1-green accepted for ProductCqrsHelpers. It is not a public application facade.

A2. Named share ACs work: union with origin WorkspaceId; productScope=local hides siblings; collision two origins; origin layer miss excludes; leave drops sibling; outsider local-only; local sibling-only id remains; zero-product stays local.
Verdict: PASS
Evidence: Read tests/McpServer.Support.Mcp.Tests/Products/GetProductEffectiveRequirementsQueryHandlerTests.cs (LastWriteUtc 2026-08-18T14:54:00.1063128Z). All eight methods still present, no Skip attributes, and they now construct `new GetProductEffectiveRequirementsQueryHandler(db)`. Independent focused run passed all eight. Helper implements: product scope walks memberships and unions sibling effective rows; local scope skips that walk; collision keeps both FR-SHARE-001 rows with distinct WorkspaceId; layer-2 query skips a sibling that has no layer-2 catalog key (excludes FR-LAYER-MISS); AfterLeave calls RemoveProductMember then effective; outsider and zero-product stay local. Quality caveat (not a missing-named-case FAIL): LocalDelete still does not call a delete or update-fr command; it asserts the owner workspace has no local FR-SIB-001 row, then that effective still contains the sibling row. Collision asserts FrEntry.WorkspaceId, not a separate originWorkspaceId property. Matches H2-red adjudication and TEST-MCP-PRODUCT-002 Condition.

A3. `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~McpServer.Support.Mcp.Tests.Products|FullyQualifiedName~McpServer.Support.Mcp.Tests.Storage.ProductEntityTests|FullyQualifiedName~McpServer.Support.Mcp.Tests.Storage.ProductMigrationApplyTests` Failed 0 Passed 32 Skipped 0.
Verdict: PASS
Evidence: Independent re-run 2026-08-18T15:01:05.2758284Z to 15:01:24.1647692Z. Command as claimed. Compiled. Summary: Passed! Failed: 0, Passed: 32, Skipped: 0, Total: 32, Duration: 5 s. FOCUSED_EXIT=0. --list-tests listed exactly those 32 cases (24 Phase 1 plus the eight share cases). This review's own run is the receipt.

A4. MCP-PRODUCTS-001 Done=false. Phase 3-5 not claimed.
Verdict: PASS
Evidence: Native todo_get via /mcp-transport tools/call with workspacePath. Id=MCP-PRODUCTS-001 Done=false CompletedDate=null DoneSummary=null. All five ImplementationTasks Done=false. Remaining still says "H0 hostile required before Phase 1. No product implementation started." (stale note, not a done-state lie). ABSENT: ProductsController.cs, ProductClient.cs, ProductsControllerTests.cs, ProductClientTests.cs, ProductRequirementContextTests.cs, FwhMcpTools.Products.cs. Client EffectiveRequirementsResult still has no ProductKeys property (Phase 3).

Honesty notes (scored, not ignored):
- RequirementsDatabaseDocumentService.GetEffectiveRequirementsAsync is still local-only: CONFIRMED. File LastWriteUtc 2026-08-08T07:05:43.4023460Z. Method still reads only the current workspace context rows and returns EffectiveRequirementsResult without ProductKeys. No product membership walk.
- productScope on REST is Phase 3: CONFIRMED. RequirementsController.GetEffectiveRequirementsAsync still takes only layerKey (LastWriteUtc 2026-07-20T14:32:20.3659871Z). Grep productScope under src: only the CQRS query and ProductShareHelper.
- Share is the CQRS query: CONFIRMED.
- EffectiveRequirementsResult.ProductKeys is populated when products contribute: CONFIRMED. Helper returns null when productKeys.Count==0, otherwise ordered keys. Union test asserts ProductKeys contains PROD-MCPSERVER.

### B Workspace rules

B1-honesty. Claims match artifacts.
Verdict: PASS
Rule: AGENTS.md honesty; profile accuracy-first-verify-sources.
Evidence: Handler/helper wiring, focused 32/0/0, TODO Done=false, Phase 3-5 absence, and REST local-only were re-checked. Implementer did not claim Phase 3-5, full suite, or MCP-PRODUCTS-001 done. REST local-only and CQRS-only share were disclosed.

B2-receipts. Machine-verifiable evidence re-run.
Verdict: PASS
Rule: Always bring the receipts.
Evidence: This review's --list-tests, focused 32/0/0 transcript, official 37/0/0 transcript, file reads, greps, todo_get, requirements_list, Test-MarkerSignature, health nonce, tools/list (106 tools), sessionlog_query proof.

B3-MCP-only storage.
Verdict: PASS
Rule: MCP is the only interface to TODO/session/requirements.
Evidence: TODO, requirements, and session log used native sessionlog_*/todo_*/requirements_* via Streamable HTTP /mcp-transport after initialize. This review did not read or write docs/todo.yaml or session-log storage files. First begin_turn calls omitted required planFile/todoId (live tool requires them; stale mcps JSON omitted them) and failed; retry with planFile=docs/plans/mcp-products-001.md and todoId=MCP-PRODUCTS-001 succeeded. That is validator invocation, not an implementer storage bypass.

B4-lab PowerShell / no Python.
Verdict: PASS
Rule: no-python-lab; pwsh.exe only.
Evidence: pwsh.exe -NoProfile path for signature, health, inventory, test runs, MCP transport client, and JSON parse. No python/py invocation.

B5-look-before-delete.
Verdict: PASS
Evidence: No deletes. Review-only plus receipt and collector-script create.

B6-Byrd v4 phase-order at H2-green.
Verdict: PASS
Rule: hostile-phase-gates; implementation only after red tests are correct; score at the inter-phase gate; full suite for this phase is the named H2-green filter, not ./build.ps1 Test.
Evidence: H0 AGREE (20260818T132341Z), H1-red AGREE (20260818T140630Z), H1-green AGREE (20260818T143053Z), and H2-red AGREE (20260818T144836Z) exist. Share helper and handler LastWriteUtc 2026-08-18T14:53:20Z is after H2-red 14:48:36Z. H2-red independently showed the eight share tests red for stub "not implemented". This pass shows those tests green. Official plan gate "Support.Mcp.Tests Product* + Requirement*effective/scope filters" independently re-run as FullyQualifiedName~Product|FullyQualifiedName~RequirementScopeLayerServiceTests: Failed 0 Passed 37 Skipped 0 EXIT=0 (2026-08-18T15:01:24.1658543Z to 15:01:42.9737101Z). The extra five cases are four RequirementScopeLayerServiceTests plus UseCaseExpandedScopeTests.ProductKey_AssignAndListByProduct. Full ./build.ps1 Test remains the H5-done gate.

### C Requirements

C1. FR/TR/TEST exist for this work.
Verdict: PASS
Evidence: Native requirements_list. FR_TOTAL=277 TR_TOTAL=406 TEST_TOTAL=422. PRODUCT subset present: FR-MCP-PRODUCT-001..005; TR-MCP-PRODUCT-API/AUTH/CTX/MODEL/SHARE-001; TEST-MCP-PRODUCT-001..006.

C2. Structured AC exist on the Phase 2 FRs and remain unsatisfied (correct; slice is not a done claim).
Verdict: PASS
Evidence: FR-003 ac-1..ac-3 isSatisfied=False (union; productScope local; collision two origins). FR-004 ac-1..ac-3 isSatisfied=False (outsider get 404 already Phase 1; outsider effective; update-fr does not change sibling). TR/TEST still have empty structured AC arrays. TEST-MCP-PRODUCT-002 Condition names union, productScope=local, collision two origins, origin layer miss, leave, outsider cannot share, file GetProductEffectiveRequirementsQueryHandlerTests. TR-MCP-PRODUCT-SHARE-001 body AC: union; missing origin layer excludes; productScope=local. Store statuses were not flipped to completed.

C3. Phase 2 AC-covering tests exist and are green (H2-green bar).
Verdict: PASS
Evidence:
- FR-003 ac-1 / TEST-002 union: HandleAsync_ProductScope_UnionsSiblingRows (passed; also asserts ProductKeys PROD-MCPSERVER)
- FR-003 ac-2 / TR-SHARE local: HandleAsync_LocalScope_HidesSiblings (passed)
- FR-003 ac-3 collision: HandleAsync_Collision_ReturnsTwoOrigins (passed; FR-SHARE-001, two WorkspaceId values)
- TR-SHARE layer miss: HandleAsync_OriginLayerMiss_ExcludesSiblingRow (passed)
- TEST-002 leave: HandleAsync_AfterLeave_DropsSibling (passed)
- FR-004 ac-2 outsider effective: HandleAsync_Outsider_IsLocalOnly (passed)
- Extra (plan/session AC, not the locked H2-red six): HandleAsync_ZeroProductWorkspace_StaysLocal; HandleAsync_LocalDeleteSiblingOnlyId_LeavesSiblingRow (both passed)
FR-004 ac-3 "update-fr on sibling id" is not a named Phase 2 / TEST-002 attack. The LocalDelete method remains a weaker no-mutation proxy. Not used as an H2-green FAIL.

C4. Mappings FR to TR/TEST exist.
Verdict: PASS
Evidence: requirements_list type=mapping:
- FR-003 -> TR API, SHARE + TEST 002, 004
- FR-004 -> TR AUTH, SHARE + TEST 002, 003
Matches the approved plan sets. TEST-003/004 remain Phase 3 files and are not required to exist at H2-green.

C5. New product behavior has FR/TR/TEST. Implementer did not mark FR/TR/TEST completed.
Verdict: PASS
Evidence: Store IDs from H0 remain pending. TODO still links FR-001..005 and the five TRs. Status fields were not flipped to completed.

### D Plan holistically

D1. H2-green checkpoint is complete. Full MCP-PRODUCTS-001 DoD is not claimed.
Verdict: PASS
Evidence: Approved plan docs/plans/mcp-products-001.md SHA256 E233F9E34BCA0A7176284FB0DE0E11BF2A186D04F479CF7C8E2CC089F72FB805 (unchanged since H0/H1/H2-red). H2-green attack text: "effective share is handler-owned; provenance; no sibling mutation; gate green." Met: helper is internal and only called from the handler; rows keep WorkspaceId and ProductKeys populate on union; leave/local-isolation tests passed; official 37/0/0 gate green. Phase 3 (controller/client/MCP) is not claimed.
Session goal plan.md SHA256 0D73B5C6B754DEC494F4EAB445AD2A6EEB73D2F2923260366E07D4A9351FD92C is unchanged. It still says MCP-PRODUCTS-001 must stay Done=false until H5-done AGREE.
Plan Phase 2 also says "Extend existing effective-requirements tests so a workspace with zero products is unchanged." That extension is still absent from RequirementScopeLayerServiceTests; zero-product is covered in the new handler-test file and the official RequirementScope tests still pass. Not claimed. Not an H2-green checkpoint FAIL.

D2. Did not start Phase 3-5 or mark the TODO done.
Verdict: PASS
Evidence: A4.

## H2-green named attacks

- Effective share is handler-owned: PASS (internal ProductShareHelper, only the query handler calls it; no IProductService)
- Provenance: PASS (WorkspaceId on union/collision rows; ProductKeys when products contribute)
- No sibling mutation: PASS with the same LocalDelete caveat as H2-red (no update-fr command; isolation plus leave are the named tests)
- Gate green: PASS (focused 32/0/0 and official 37/0/0, Skipped 0)

## Explicit FAIL list

None.

## UNKNOWN / unevaluated

- Full `./build.ps1 Test` not run. Not required to exit H2-green.
- HandleAsync_LocalDeleteSiblingOnlyId_LeavesSiblingRow does not perform a delete or update-fr. FR-004 ac-3 remains untested. Not a Phase 2 named attack.
- Existing RequirementScopeLayerServiceTests were not extended for zero-product. Covered in the new file instead; official RequirementScope tests still passed.
- Client EffectiveRequirementsResult has no ProductKeys. Phase 3.
- REST GET /mcpserver/requirements/effective has no productScope. Phase 3. Disclosed.

## Session-log persistence proof

Native MCP tools over POST http://PAYTON-LEGION2:7147/mcp-transport (initialize, tools/list, tools/call). Agent GrokCode, workspace F:\GitHub\McpServer. tools/list unique name count 106 including sessionlog_open, sessionlog_begin_turn, sessionlog_dialog, sessionlog_complete_turn, sessionlog_query, todo_get, requirements_list.

- initialize protocolVersion 2025-03-26
- sessionlog_open GrokCode-20260818T150052Z-h2-green-products success=true
- sessionlog_begin_turn without planFile/todoId failed (live tool requires both). Retry with planFile=docs/plans/mcp-products-001.md and todoId=MCP-PRODUCTS-001: success turnId=41755 status=in_progress
- sessionlog_dialog success totalDialogItems=4
- sessionlog_replace_section actions replaced=true (8 actions)
- sessionlog_complete_turn success turnId=41755 status=completed
- Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer agent=GrokCode todoId=MCP-PRODUCTS-001 from=2026-08-18T15:00:00Z limit=10. totalCount=1. First item: sessionId GrokCode-20260818T150052Z-h2-green-products, sourceType GrokCode, turnCount=1, requestId req-20260818T150052Z-001-hostile-h2-green-products, turn status=completed, response starts with OverallVerdict AGREE, 8 actions, 4 dialog items (one category=decision), designDecisions present. Session-level status remains in_progress (expected; session not closed).

## Files written by this review

- docs/receipts/hostile-validator-20260818T150200Z.md
- docs/receipts/hostile-validator-20260818T150200Z.json
