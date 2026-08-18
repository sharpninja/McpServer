# Hostile Validator Receipt

TimestampUtc: 2026-08-18T13:37:28Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: class 1 (project requirement work; MCP-PRODUCTS-001 Phase 1 / H1-red only). Not green implementation. Not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md).
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.93.0; .version 1.93.0)
Marker: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
Marker signature: Test-MarkerSignature True (pwsh, MarkerFile param, marker-resolver.ps1)
Health (this review): nonce e20383457988435b912a2711756c3c46 echoed exactly; status Healthy; version 1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e; storage=reachable
SessionId: GrokCode-20260818T133259Z-h1-red-products
RequestId: req-20260818T133259Z-001-hostile-h1-red-products
turnId: 41696
planFile: docs/plans/mcp-products-001.md
todoId: MCP-PRODUCTS-001
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass independently re-ran the Product test filter, grepped ProductEntity/IProductService/AddProducts, read the four Phase 1 test files plus stub handlers, queried todo_get and FR/TEST/mappings through MCP/plugin, and re-read the plan and H0 receipt. Implementer chat was not trusted.

This review did not implement product features. This review did not add migrations. This review wrote only this receipt pair and the MCP review turn.

Accuracy rating: 93/100. Test counts, failure messages, stub bodies, greps, TODO Done=false, and store ACs were re-verified. Plugin getFr createdAt fell in the get-call window (same serializer artifact as H0).
Completeness rating: 92/100. Surfaces A-D and the named H1-red attacks were evaluated. Did not run the full unit suite (H1-red gate is the Product filter). Did not spin PostgreSQL/SQL Server harnesses because those Product tests do not exist.

## Classification

Class 1. Phase 1 red tests for MCP-PRODUCTS-001 (FR-MCP-PRODUCT-001, FR-MCP-PRODUCT-002, TEST-MCP-PRODUCT-001, TEST-MCP-PRODUCT-005). Surface C applies. Byrd v4 phase-order is scored at this H1-red gate: AC-covering red tests must exist before green implementation.

## Claims reviewed

### A Requested

A1. Stub CQRS CreateProductCommand/Handler and AddProductMemberCommand/Handler return Failure("not implemented").
Verdict: PASS
Evidence: Read src/McpServer.Support.Mcp/Products/Commands/CreateProductCommand.cs and AddProductMemberCommand.cs. Both handlers return Task.FromResult(Result<ProductDto>.Failure("not implemented")). Live test output used that exact error string.

A2. Tests exist: CreateProductCommandHandlerTests, AddProductMemberCommandHandlerTests, ProductEntityTests, ProductMigrationApplyTests.
Verdict: PASS
Evidence: On disk under tests/McpServer.Support.Mcp.Tests/Products/ and Storage/. All four compiled into McpServer.Support.Mcp.Tests.dll.

A3. `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~Product` is Failed 10, Passed 1, Skipped 0.
Verdict: PASS
Evidence: Independent re-run 2026-08-18T13:34:05.5505715Z to 13:34:28.0462312Z. EXIT=1. Summary: Failed 10, Passed 1, Skipped 0, Total 11. The one pass is pre-existing UseCaseExpandedScopeTests.ProductKey_AssignAndListByProduct (FR-MCP-USECASE-009 hook), listed by `--list-tests`. Not a Phase 1 product persist test.

A4. Red on valid create, invalid key 400, membership, duplicate 409, Products table after migrate. Red for the right reason (not compile errors).
Verdict: PASS
Evidence: Projects compiled (Support.Mcp.Tests.dll built). Failures:
- HandleAsync_ValidProdMcpserverKey_ReturnsOwnerAndKey: Assert.True IsSuccess, message "not implemented"
- HandleAsync_InvalidKey_Fails (empty, mcpserver, prod-mcpserver, MCP-SERVER): Assert.Contains "400" in "not implemented"
- HandleAsync_OwnerAddsRegisteredWorkspace_IncludesMember: "not implemented"
- HandleAsync_UnknownWorkspace_Fails: Assert.Contains "400" in "not implemented"
- HandleAsync_NonOwner_FailsForbidden: Assert.Contains "403" in "not implemented"
- Create_DuplicateProdMcpserverKey_FailsConflict: first.IsSuccess false, message "not implemented"
- Migrate_FromEmpty_CreatesProductTables: Assert.True TableExists Products expected True actual False (after real Database.Migrate())

A5. No ProductEntity, no DbSet, no migration, no public IProductService.
Verdict: PASS
Evidence: grep class ProductEntity / DbSet Product / IProductService / AddProductsStorage / ProductWorkspaceMembershipEntity on *.cs: no production types. McpDbContext has no Products DbSet. SqliteMigrations has no AddProducts hits. Only the test class name ProductEntityTests and a comment "until the AddProductsStorage migration exists".

A6. H0 already AGREE. Did not start Phase 2+.
Verdict: PASS
Evidence: docs/receipts/hostile-validator-20260818T132341Z.md OverallVerdict AGREE. grep GetProductEffectiveRequirements, ProductsController, ProductClient: 0 hits. No Phase 2-4 test files.

A7. MCP-PRODUCTS-001 still Done=false.
Verdict: PASS
Evidence: mcpserver__todo_get id=MCP-PRODUCTS-001. Done=false. CompletedDate=null. All five ImplementationTasks Done=false. Remaining still says "H0 hostile required before Phase 1" (stale note, not a done-state lie).

### B Workspace rules

B1-honesty. Claims match artifacts.
Verdict: PASS
Rule: AGENTS.md honesty; profile accuracy-first-verify-sources.
Evidence: Stub text, file names, 10/1/0 counts, no-entity greps, Done=false, and H0 AGREE all re-checked. Implementer did not claim owner-remove/self-leave/soft-delete tests exist.

B2-receipts. Machine-verifiable evidence re-run.
Verdict: PASS
Rule: Always bring the receipts.
Evidence: This review's dotnet test transcript, greps, todo_get, plugin getFr/getTest/listMappings, Test-MarkerSignature, health nonce.

B3-MCP-only storage.
Verdict: PASS
Rule: MCP is the only interface to TODO/session/requirements.
Evidence: TODO and requirements read via mcpserver__todo_get, mcpserver__requirements_list, and plugin workflow.requirements.getFr/getTest/listMappings. This review did not read or write docs/todo.yaml or session-log storage files.

B4-lab PowerShell / no Python.
Verdict: PASS
Rule: no-python-lab; pwsh.exe only.
Evidence: pwsh MCP invoke_expression and native MCP tools. No python/py invocation.

B5-look-before-delete.
Verdict: PASS
Evidence: No deletes. Review-only.

B6-Byrd v4 phase-order at H1-red.
Verdict: FAIL
Rule: hostile-phase-gates; tests covering full acceptance criteria before implementation; score at the inter-phase gate.
Evidence: H1-red is the test gate for Phase 1 (FR-001, FR-002, TEST-001, TEST-005). TEST-MCP-PRODUCT-001 body requires isolation, soft-delete, owner add/remove, and self-leave. Plan Phase 1 named cases require the same plus soft-delete hides product. Those tests are absent. Green implementation has not started, but the H1-red claim (tests exist for the phase) is incomplete. This is the defect this gate is supposed to catch.

### C Requirements

C1. FR/TR/TEST exist for this work.
Verdict: PASS
Evidence: requirements_list type=fr: FR-MCP-PRODUCT-001..005 present. Plugin getFr FR-001/002 returned records. getTest TEST-MCP-PRODUCT-001 and TEST-MCP-PRODUCT-005 returned records.

C2. Structured AC exist and are unsatisfied.
Verdict: PASS
Evidence: FR-001 ac-1..ac-5 isSatisfied=false. FR-002 ac-1..ac-4 isSatisfied=false.

C3. Phase 1 AC-covering tests exist (H1-red bar).
Verdict: FAIL
Evidence: Present and red: FR-001 ac-1 create key+owner; ac-2 invalid 400; ac-3 duplicate 409; FR-002 ac-1 unknown/not-registered 400; ac-2 owner+member ids on add success; FR-002 non-owner add 403 (auth). Absent:
- FR-001 ac-5 / TEST-001 / plan named case: soft-delete hides from default list
- FR-002 ac-3 / TEST-001 / plan named case: leave removes only the caller
- TEST-001 / plan named case: owner remove
- TEST-001: isolation (host-global, no workspace query filter)
- FR-001 ac-4: non-owner update/delete 403 (Phase 1 CRUD AC; no Update/Delete handler tests)
- TEST-005: only empty SQLite migrate is present. No production-shaped DB case. No PostgreSQL or SQL Server harness methods. The existing migrate test is correctly red (Products table missing) and does assert SessionLogs.AgentExecutablePath.

C4. Mappings FR to TR/TEST exist.
Verdict: PASS
Evidence: listMappings FR-001: TR API, AUTH, MODEL + TEST 001,003,004,005. FR-002: TR AUTH, MODEL + TEST 001,003. Matches the plan sets. Missing unit methods are a C3 FAIL, not a missing mapping.

C5. New product behavior has FR/TR/TEST (not deferred).
Verdict: PASS
Evidence: Store IDs from H0 remain; TODO still links FR-001..005 and the five TRs.

### D Plan holistically

D1. H1-red checkpoint and Phase 1 named cases are complete.
Verdict: FAIL
Evidence: Plan Phase 1 named cases: accept PROD-MCPSERVER; reject mcpserver/empty/missing prefix; unique key; owner add/remove; self-leave; reject unknown workspace; soft-delete hides product; non-owner cannot add. Present: accept, invalid-key rejects, unique key, owner add, unknown, non-owner add. Missing: owner remove, self-leave, soft-delete hide. H1-red attack also requires "membership ACs"; FR-002 ac-3 leave is a membership AC with no test. Full MCP-PRODUCTS-001 DoD is not claimed; this FAIL is the H1-red step only.

D2. Did not start Phase 2+ or mark the TODO done.
Verdict: PASS
Evidence: A6 and A7. Plan H1-green / Phase 2 files absent. TODO Done=false.

## H1-red named attacks

- Tests cover PROD-MCPSERVER accept and invalid-key reject: PASS (CreateProductCommandHandlerTests)
- Membership ACs add-registered, unknown 400, non-owner 403: PASS (AddProductMemberCommandHandlerTests)
- Tests are red for the right reason (not compile errors): PASS
- No green implementation of persist/membership/migrations: PASS
- Plan/TEST-001 named cases isolation, owner-remove, self-leave, soft-delete: FAIL
- TEST-005 all-provider and production-shaped migrate tests: FAIL (only empty SQLite)

## Explicit FAIL list

- B6-byrd: H1-red AC-covering tests are incomplete; this is the phase gate.
- C3: Phase 1 AC / TEST-001 / TEST-005 coverage gaps listed above.
- D1: Plan Phase 1 named cases owner-remove, self-leave, and soft-delete hide are missing.

## UNKNOWN / unevaluated

- Plugin getFr/getTest createdAt/updatedAt values fell in the get-call window (2026-08-18T13:36:29Z+). Records already existed in requirements_list and H0. Treated as a serializer/read artifact.
- Grok plugin failsafe drain printed failed=1 then later quarantined=1 while invoking getFr/listMappings. Results still returned. Not a product-implementation defect.
- Full `./build.ps1 Test` not run. Not required to exit H1-red.
- Disabled-but-registered member rejection is not a named Phase 1 case; unknown-path 400 is what exists. Not scored as an extra FAIL.

## Session-log persistence proof

Native MCP tools (mcpserver__sessionlog_*), agent GrokCode, workspace F:\GitHub\McpServer:

- sessionlog_open GrokCode-20260818T133259Z-h1-red-products created=true
- sessionlog_begin_turn requestId req-20260818T133259Z-001-hostile-h1-red-products turnId=41696 status=in_progress
- sessionlog_dialog + complete_turn with actions and designDecisions
- Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer todoId=MCP-PRODUCTS-001 limit=10. First item: sessionId GrokCode-20260818T133259Z-h1-red-products, turnCount=1, requestId req-20260818T133259Z-001-hostile-h1-red-products, turn status=completed, response starts with DISAGREE, 7 actions, 2 designDecisions, 3 dialog items. Session-level status remains in_progress (expected; session not closed).

## Files written by this review

- docs/receipts/hostile-validator-20260818T133728Z.md
- docs/receipts/hostile-validator-20260818T133728Z.json
